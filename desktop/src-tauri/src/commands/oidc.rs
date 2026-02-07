use base64::{engine::general_purpose::URL_SAFE_NO_PAD, Engine};
use rand::Rng;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::collections::HashMap;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::TcpListener;

const CALLBACK_PORT: u16 = 23847;
const AUTH_TIMEOUT_SECS: u64 = 300; // 5 minutes

#[derive(Debug, thiserror::Error)]
pub enum OidcError {
    #[error("Discovery failed: {0}")]
    DiscoveryFailed(String),
    #[error("Authentication failed: {0}")]
    AuthFailed(String),
    #[error("Token exchange failed: {0}")]
    TokenExchangeFailed(String),
    #[error("Request failed: {0}")]
    RequestFailed(String),
    #[error("Timeout waiting for authentication")]
    Timeout,
}

impl serde::Serialize for OidcError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

#[derive(Serialize, Deserialize)]
pub struct ServerDiscovery {
    pub discovery: serde_json::Value,
    pub oidc_config: Option<OidcConfig>,
}

#[derive(Serialize, Deserialize, Clone)]
pub struct OidcConfig {
    pub authority: String,
    pub client_id: String,
    pub scopes: Option<String>,
}

#[derive(Serialize, Deserialize)]
pub struct TokenResponse {
    pub access_token: String,
    pub id_token: Option<String>,
    pub refresh_token: Option<String>,
    pub expires_in: Option<u64>,
    pub token_type: Option<String>,
}

#[derive(Serialize, Deserialize)]
pub struct UserProfile {
    pub id: String,
    pub email: String,
    pub display_name: Option<String>,
}

#[derive(Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ApiKeyResponse {
    pub id: String,
    pub name: String,
    pub api_key: String,
    pub scopes: Option<Vec<String>>,
    pub created_at: String,
}

#[derive(Deserialize)]
struct OpenIdConfiguration {
    authorization_endpoint: String,
    token_endpoint: String,
}

fn get_client() -> reqwest::Client {
    reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(30))
        .build()
        .unwrap_or_default()
}

fn generate_code_verifier() -> String {
    let mut rng = rand::thread_rng();
    let bytes: Vec<u8> = (0..32).map(|_| rng.gen::<u8>()).collect();
    URL_SAFE_NO_PAD.encode(&bytes)
}

fn generate_code_challenge(verifier: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(verifier.as_bytes());
    let hash = hasher.finalize();
    URL_SAFE_NO_PAD.encode(hash)
}

fn generate_state() -> String {
    let mut rng = rand::thread_rng();
    let bytes: Vec<u8> = (0..16).map(|_| rng.gen::<u8>()).collect();
    URL_SAFE_NO_PAD.encode(&bytes)
}

fn parse_query_params(query: &str) -> HashMap<String, String> {
    query
        .split('&')
        .filter_map(|pair| {
            let mut parts = pair.splitn(2, '=');
            let key = parts.next()?;
            let value = parts.next().unwrap_or("");
            Some((
                urlencoding_decode(key),
                urlencoding_decode(value),
            ))
        })
        .collect()
}

fn urlencoding_decode(s: &str) -> String {
    let mut result = String::new();
    let mut chars = s.bytes();
    while let Some(b) = chars.next() {
        if b == b'%' {
            let hi = chars.next().unwrap_or(b'0');
            let lo = chars.next().unwrap_or(b'0');
            let hex = format!("{}{}", hi as char, lo as char);
            if let Ok(byte) = u8::from_str_radix(&hex, 16) {
                result.push(byte as char);
            }
        } else if b == b'+' {
            result.push(' ');
        } else {
            result.push(b as char);
        }
    }
    result
}

fn urlencoding_encode(s: &str) -> String {
    let mut result = String::new();
    for byte in s.bytes() {
        match byte {
            b'A'..=b'Z' | b'a'..=b'z' | b'0'..=b'9' | b'-' | b'_' | b'.' | b'~' => {
                result.push(byte as char);
            }
            _ => {
                result.push_str(&format!("%{:02X}", byte));
            }
        }
    }
    result
}

#[tauri::command]
pub async fn discover_server(server_url: String) -> Result<ServerDiscovery, OidcError> {
    let client = get_client();

    // Fetch API discovery
    let discovery_url = format!("{}/api/discovery", server_url);
    let discovery_resp = client
        .get(&discovery_url)
        .send()
        .await
        .map_err(|e| OidcError::DiscoveryFailed(format!("Failed to reach server: {}", e)))?;

    if !discovery_resp.status().is_success() {
        return Err(OidcError::DiscoveryFailed(format!(
            "Server returned HTTP {}",
            discovery_resp.status()
        )));
    }

    let discovery: serde_json::Value = discovery_resp
        .json()
        .await
        .map_err(|e| OidcError::DiscoveryFailed(format!("Invalid discovery response: {}", e)))?;

    // Fetch OIDC config from config.json
    let config_url = format!("{}/config/config.json", server_url);
    let oidc_config = match client.get(&config_url).send().await {
        Ok(resp) if resp.status().is_success() => {
            let config: serde_json::Value = resp
                .json()
                .await
                .map_err(|e| OidcError::DiscoveryFailed(format!("Invalid config response: {}", e)))?;

            // Extract OIDC settings from config
            let authority = config
                .get("oidcAuthority")
                .or_else(|| config.get("oidc_authority"))
                .and_then(|v| v.as_str())
                .map(|s| s.to_string());
            let client_id = config
                .get("oidcClientId")
                .or_else(|| config.get("oidc_client_id"))
                .and_then(|v| v.as_str())
                .map(|s| s.to_string());
            let scopes = config
                .get("oidcScopes")
                .or_else(|| config.get("oidc_scopes"))
                .and_then(|v| v.as_str())
                .map(|s| s.to_string());

            match (authority, client_id) {
                (Some(authority), Some(client_id)) if !authority.is_empty() => {
                    Some(OidcConfig {
                        authority,
                        client_id,
                        scopes,
                    })
                }
                _ => None,
            }
        }
        _ => None,
    };

    Ok(ServerDiscovery {
        discovery,
        oidc_config,
    })
}

#[tauri::command]
pub async fn start_oidc_auth(
    authority: String,
    client_id: String,
    scopes: Option<String>,
) -> Result<TokenResponse, OidcError> {
    let client = get_client();

    // Fetch OpenID Configuration
    let openid_config_url = format!(
        "{}/.well-known/openid-configuration",
        authority.trim_end_matches('/')
    );
    let openid_resp = client
        .get(&openid_config_url)
        .send()
        .await
        .map_err(|e| OidcError::DiscoveryFailed(format!("Failed to fetch OIDC config: {}", e)))?;

    let openid_config: OpenIdConfiguration = openid_resp
        .json()
        .await
        .map_err(|e| OidcError::DiscoveryFailed(format!("Invalid OIDC config: {}", e)))?;

    // Generate PKCE parameters
    let code_verifier = generate_code_verifier();
    let code_challenge = generate_code_challenge(&code_verifier);
    let state = generate_state();

    // Bind to fixed port only - fail loudly if unavailable
    // Using a random port would break OIDC flows that require pre-registered redirect URIs
    let listener = TcpListener::bind(format!("127.0.0.1:{}", CALLBACK_PORT))
        .await
        .map_err(|e| OidcError::AuthFailed(
            format!("Port {} unavailable. Close other apps using this port. Error: {}", CALLBACK_PORT, e)
        ))?;

    let local_addr = listener
        .local_addr()
        .map_err(|e| OidcError::AuthFailed(format!("Failed to get listener address: {}", e)))?;

    // Verify we got the expected port (defensive check)
    if local_addr.port() != CALLBACK_PORT {
        return Err(OidcError::AuthFailed(
            format!("Expected port {} but got {}", CALLBACK_PORT, local_addr.port())
        ));
    }

    let actual_redirect_uri = format!("http://127.0.0.1:{}/auth/callback", local_addr.port());

    // Build authorization URL
    let scope = scopes.unwrap_or_else(|| "openid profile email".to_string());
    let auth_url = format!(
        "{}?response_type=code&client_id={}&redirect_uri={}&scope={}&state={}&code_challenge={}&code_challenge_method=S256",
        openid_config.authorization_endpoint,
        urlencoding_encode(&client_id),
        urlencoding_encode(&actual_redirect_uri),
        urlencoding_encode(&scope),
        urlencoding_encode(&state),
        urlencoding_encode(&code_challenge),
    );

    // Open browser
    open::that(&auth_url)
        .map_err(|e| OidcError::AuthFailed(format!("Failed to open browser: {}", e)))?;

    // Wait for callback with timeout
    let (code, received_state) = tokio::time::timeout(
        std::time::Duration::from_secs(AUTH_TIMEOUT_SECS),
        wait_for_callback(&listener),
    )
    .await
    .map_err(|_| OidcError::Timeout)?
    .map_err(|e| OidcError::AuthFailed(e))?;

    // Validate state
    if received_state != state {
        return Err(OidcError::AuthFailed("State mismatch".to_string()));
    }

    // Exchange code for tokens
    let token_params = [
        ("grant_type", "authorization_code"),
        ("code", &code),
        ("redirect_uri", &actual_redirect_uri),
        ("client_id", &client_id),
        ("code_verifier", &code_verifier),
    ];

    let token_resp = client
        .post(&openid_config.token_endpoint)
        .form(&token_params)
        .send()
        .await
        .map_err(|e| OidcError::TokenExchangeFailed(format!("Token request failed: {}", e)))?;

    if !token_resp.status().is_success() {
        let status = token_resp.status();
        let body = token_resp.text().await.unwrap_or_default();
        return Err(OidcError::TokenExchangeFailed(format!(
            "Token endpoint returned HTTP {}: {}",
            status, body
        )));
    }

    let tokens: TokenResponse = token_resp
        .json()
        .await
        .map_err(|e| OidcError::TokenExchangeFailed(format!("Invalid token response: {}", e)))?;

    Ok(tokens)
}

async fn wait_for_callback(listener: &TcpListener) -> Result<(String, String), String> {
    let (mut stream, _) = listener
        .accept()
        .await
        .map_err(|e| format!("Failed to accept connection: {}", e))?;

    let mut buf = vec![0u8; 4096];
    let n = stream
        .read(&mut buf)
        .await
        .map_err(|e| format!("Failed to read request: {}", e))?;

    let request = String::from_utf8_lossy(&buf[..n]).to_string();

    // Parse the request line to get the path and query
    let first_line = request.lines().next().unwrap_or("");
    let path = first_line
        .split_whitespace()
        .nth(1)
        .unwrap_or("");

    // Extract query string
    let query = path
        .splitn(2, '?')
        .nth(1)
        .unwrap_or("");

    let params = parse_query_params(query);

    let code = params.get("code").cloned();
    let state = params.get("state").cloned().unwrap_or_default();
    let error = params.get("error").cloned();

    // Send response to browser
    let (status_line, body) = if code.is_some() {
        (
            "HTTP/1.1 200 OK",
            "<html><body><h1>Authentication successful!</h1><p>You can close this window and return to Relate Mail.</p><script>window.close()</script></body></html>",
        )
    } else {
        (
            "HTTP/1.1 400 Bad Request",
            "<html><body><h1>Authentication failed</h1><p>Please try again in the application.</p></body></html>",
        )
    };

    let response = format!(
        "{}\r\nContent-Type: text/html\r\nContent-Length: {}\r\nConnection: close\r\n\r\n{}",
        status_line,
        body.len(),
        body
    );

    let _ = stream.write_all(response.as_bytes()).await;
    let _ = stream.flush().await;

    match (code, error) {
        (Some(code), _) => Ok((code, state)),
        (None, Some(error)) => Err(format!("OIDC error: {}", error)),
        _ => Err("No authorization code received".to_string()),
    }
}

#[tauri::command]
pub async fn fetch_profile_with_jwt(
    server_url: String,
    jwt_token: String,
) -> Result<UserProfile, OidcError> {
    let client = get_client();

    let url = format!("{}/api/profile", server_url);
    let resp = client
        .get(&url)
        .header("Authorization", format!("Bearer {}", jwt_token))
        .send()
        .await
        .map_err(|e| OidcError::RequestFailed(format!("Profile request failed: {}", e)))?;

    if !resp.status().is_success() {
        let status = resp.status();
        let body = resp.text().await.unwrap_or_default();
        return Err(OidcError::RequestFailed(format!(
            "Profile endpoint returned HTTP {}: {}",
            status, body
        )));
    }

    let profile: UserProfile = resp
        .json()
        .await
        .map_err(|e| OidcError::RequestFailed(format!("Invalid profile response: {}", e)))?;

    Ok(profile)
}

#[tauri::command]
pub async fn create_api_key_with_jwt(
    server_url: String,
    jwt_token: String,
    device_name: String,
    platform: String,
) -> Result<ApiKeyResponse, OidcError> {
    let client = get_client();

    let url = format!("{}/api/smtp-credentials/mobile", server_url);
    let body = serde_json::json!({
        "deviceName": device_name,
        "platform": platform,
    });

    let resp = client
        .post(&url)
        .header("Authorization", format!("Bearer {}", jwt_token))
        .header("Content-Type", "application/json")
        .body(body.to_string())
        .send()
        .await
        .map_err(|e| OidcError::RequestFailed(format!("API key creation failed: {}", e)))?;

    if !resp.status().is_success() {
        let status = resp.status();
        let body = resp.text().await.unwrap_or_default();
        return Err(OidcError::RequestFailed(format!(
            "API key endpoint returned HTTP {}: {}",
            status, body
        )));
    }

    let api_key_resp: ApiKeyResponse = resp
        .json()
        .await
        .map_err(|e| OidcError::RequestFailed(format!("Invalid API key response: {}", e)))?;

    Ok(api_key_resp)
}
