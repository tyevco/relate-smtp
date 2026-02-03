use crate::commands::AppState;
use tauri::State;

#[derive(Debug, thiserror::Error)]
pub enum ApiError {
    #[error("Not configured: {0}")]
    NotConfigured(String),
    #[error("Request failed: {0}")]
    RequestFailed(String),
}

impl serde::Serialize for ApiError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

fn get_client() -> reqwest::Client {
    reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(30))
        .build()
        .unwrap_or_default()
}

async fn make_request(
    state: &State<'_, AppState>,
    method: reqwest::Method,
    endpoint: &str,
    body: Option<String>,
) -> Result<String, ApiError> {
    let server_url = state
        .server_url
        .read()
        .unwrap()
        .clone()
        .ok_or_else(|| ApiError::NotConfigured("Server URL not set".to_string()))?;

    let api_key = state
        .api_key
        .read()
        .unwrap()
        .clone()
        .ok_or_else(|| ApiError::NotConfigured("API key not set".to_string()))?;

    let url = format!("{}/api{}", server_url, endpoint);
    let client = get_client();

    let mut request = client
        .request(method, &url)
        .header("X-Api-Key", &api_key)
        .header("Content-Type", "application/json");

    if let Some(body) = body {
        request = request.body(body);
    }

    let response = request
        .send()
        .await
        .map_err(|e| ApiError::RequestFailed(e.to_string()))?;

    let status = response.status();
    let text = response
        .text()
        .await
        .map_err(|e| ApiError::RequestFailed(e.to_string()))?;

    if !status.is_success() {
        return Err(ApiError::RequestFailed(format!(
            "HTTP {}: {}",
            status, text
        )));
    }

    Ok(text)
}

#[tauri::command]
pub async fn api_get(endpoint: String, state: State<'_, AppState>) -> Result<String, ApiError> {
    make_request(&state, reqwest::Method::GET, &endpoint, None).await
}

#[tauri::command]
pub async fn api_post(
    endpoint: String,
    body: Option<String>,
    state: State<'_, AppState>,
) -> Result<String, ApiError> {
    make_request(&state, reqwest::Method::POST, &endpoint, body).await
}

#[tauri::command]
pub async fn api_put(
    endpoint: String,
    body: Option<String>,
    state: State<'_, AppState>,
) -> Result<String, ApiError> {
    make_request(&state, reqwest::Method::PUT, &endpoint, body).await
}

#[tauri::command]
pub async fn api_patch(
    endpoint: String,
    body: Option<String>,
    state: State<'_, AppState>,
) -> Result<String, ApiError> {
    make_request(&state, reqwest::Method::PATCH, &endpoint, body).await
}

#[tauri::command]
pub async fn api_delete(endpoint: String, state: State<'_, AppState>) -> Result<String, ApiError> {
    make_request(&state, reqwest::Method::DELETE, &endpoint, None).await
}
