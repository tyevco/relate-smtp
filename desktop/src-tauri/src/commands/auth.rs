use crate::commands::AppState;
use keyring::Entry;
use serde::{Deserialize, Serialize};
use tauri::State;

const SERVICE_NAME: &str = "com.relate.mail.desktop";

#[derive(Debug, thiserror::Error)]
pub enum AuthError {
    #[error("Keyring error: {0}")]
    KeyringError(String),
    #[error("Serialization error: {0}")]
    SerializationError(String),
}

impl serde::Serialize for AuthError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

#[derive(Serialize, Deserialize)]
pub struct Credentials {
    pub server_url: String,
    pub api_key: String,
    pub user_email: String,
}

#[tauri::command]
pub async fn save_credentials(
    server_url: String,
    api_key: String,
    user_email: String,
    state: State<'_, AppState>,
) -> Result<(), AuthError> {
    let credentials = Credentials {
        server_url: server_url.clone(),
        api_key: api_key.clone(),
        user_email,
    };

    let json =
        serde_json::to_string(&credentials).map_err(|e| AuthError::SerializationError(e.to_string()))?;

    let entry = Entry::new(SERVICE_NAME, "credentials")
        .map_err(|e| AuthError::KeyringError(e.to_string()))?;

    entry
        .set_password(&json)
        .map_err(|e| AuthError::KeyringError(e.to_string()))?;

    // Update app state
    *state.server_url.write().unwrap() = Some(server_url);
    *state.api_key.write().unwrap() = Some(api_key);

    Ok(())
}

#[tauri::command]
pub async fn load_credentials(state: State<'_, AppState>) -> Result<Option<Credentials>, AuthError> {
    let entry = Entry::new(SERVICE_NAME, "credentials")
        .map_err(|e| AuthError::KeyringError(e.to_string()))?;

    match entry.get_password() {
        Ok(json) => {
            let credentials: Credentials = serde_json::from_str(&json)
                .map_err(|e| AuthError::SerializationError(e.to_string()))?;

            // Update app state
            *state.server_url.write().unwrap() = Some(credentials.server_url.clone());
            *state.api_key.write().unwrap() = Some(credentials.api_key.clone());

            Ok(Some(credentials))
        }
        Err(keyring::Error::NoEntry) => Ok(None),
        Err(e) => Err(AuthError::KeyringError(e.to_string())),
    }
}

#[tauri::command]
pub async fn clear_credentials(state: State<'_, AppState>) -> Result<(), AuthError> {
    let entry = Entry::new(SERVICE_NAME, "credentials")
        .map_err(|e| AuthError::KeyringError(e.to_string()))?;

    // Ignore error if entry doesn't exist
    let _ = entry.delete_credential();

    // Clear app state
    *state.server_url.write().unwrap() = None;
    *state.api_key.write().unwrap() = None;

    Ok(())
}
