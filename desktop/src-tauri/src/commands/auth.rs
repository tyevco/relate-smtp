use crate::commands::AppState;
use keyring::Entry;
use serde::{Deserialize, Serialize};
use tauri::State;
use uuid::Uuid;

const SERVICE_NAME: &str = "com.relate.mail.desktop";
const ACCOUNTS_KEY: &str = "accounts";

#[derive(Debug, thiserror::Error)]
pub enum AuthError {
    #[error("Keyring error: {0}")]
    KeyringError(String),
    #[error("Serialization error: {0}")]
    SerializationError(String),
    #[error("Account not found: {0}")]
    AccountNotFound(String),
    #[error("Internal error: {0}")]
    Internal(String),
}

impl serde::Serialize for AuthError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct Account {
    pub id: String,
    pub display_name: String,
    pub server_url: String,
    pub user_email: String,
    pub api_key_id: String,
    pub scopes: Vec<String>,
    pub created_at: String,
    pub last_used_at: String,
}

#[derive(Serialize, Deserialize, Clone, Debug, Default)]
pub struct AccountsData {
    pub accounts: Vec<Account>,
    pub active_account_id: Option<String>,
}

// Legacy credential structure for migration
#[derive(Serialize, Deserialize)]
pub struct Credentials {
    pub server_url: String,
    pub api_key: String,
    pub user_email: String,
}

fn get_accounts_entry() -> Result<Entry, AuthError> {
    Entry::new(SERVICE_NAME, ACCOUNTS_KEY).map_err(|e| AuthError::KeyringError(e.to_string()))
}

fn get_api_key_entry(account_id: &str) -> Result<Entry, AuthError> {
    Entry::new(SERVICE_NAME, &format!("api_key_{account_id}"))
        .map_err(|e| AuthError::KeyringError(e.to_string()))
}

fn load_accounts_data() -> Result<AccountsData, AuthError> {
    let entry = get_accounts_entry()?;

    match entry.get_password() {
        Ok(json) => serde_json::from_str(&json)
            .map_err(|e| AuthError::SerializationError(e.to_string())),
        Err(keyring::Error::NoEntry) => Ok(AccountsData::default()),
        Err(e) => Err(AuthError::KeyringError(e.to_string())),
    }
}

fn save_accounts_data(data: &AccountsData) -> Result<(), AuthError> {
    let entry = get_accounts_entry()?;
    let json = serde_json::to_string(data)
        .map_err(|e| AuthError::SerializationError(e.to_string()))?;

    entry
        .set_password(&json)
        .map_err(|e| AuthError::KeyringError(e.to_string()))
}

fn get_api_key_for_account(account_id: &str) -> Result<Option<String>, AuthError> {
    let entry = get_api_key_entry(account_id)?;

    match entry.get_password() {
        Ok(key) => Ok(Some(key)),
        Err(keyring::Error::NoEntry) => Ok(None),
        Err(e) => Err(AuthError::KeyringError(e.to_string())),
    }
}

fn save_api_key_for_account(account_id: &str, api_key: &str) -> Result<(), AuthError> {
    let entry = get_api_key_entry(account_id)?;
    entry
        .set_password(api_key)
        .map_err(|e| AuthError::KeyringError(e.to_string()))
}

fn delete_api_key_for_account(account_id: &str) -> Result<(), AuthError> {
    let entry = get_api_key_entry(account_id)?;
    // Ignore error if entry doesn't exist
    let _ = entry.delete_credential();
    Ok(())
}

/// Load all accounts and return with active account info
#[tauri::command]
pub async fn load_accounts(
    state: State<'_, AppState>,
) -> Result<AccountsData, AuthError> {
    let mut data = load_accounts_data()?;

    // Auto-select first account if none is active but accounts exist
    if data.active_account_id.is_none() && !data.accounts.is_empty() {
        data.active_account_id = Some(data.accounts[0].id.clone());
        save_accounts_data(&data)?;
    }

    // If there's an active account, update AppState
    if let Some(active_id) = &data.active_account_id {
        if let Some(account) = data.accounts.iter().find(|a| &a.id == active_id) {
            if let Some(api_key) = get_api_key_for_account(&account.id)? {
                match state.server_url.write() {
                    Ok(mut guard) => *guard = Some(account.server_url.clone()),
                    Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
                }
                match state.api_key.write() {
                    Ok(mut guard) => *guard = Some(api_key),
                    Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
                }
            }
        }
    }

    Ok(data)
}

/// Get the API key for a specific account
#[tauri::command]
pub async fn get_account_api_key(account_id: String) -> Result<Option<String>, AuthError> {
    get_api_key_for_account(&account_id)
}

/// Save a new account with its API key
#[tauri::command]
pub async fn save_account(
    account: Account,
    api_key: String,
    state: State<'_, AppState>,
) -> Result<AccountsData, AuthError> {
    let mut data = load_accounts_data()?;

    // Check if account with same server_url and user_email already exists
    let existing_idx = data.accounts.iter().position(|a| {
        a.server_url == account.server_url && a.user_email == account.user_email
    });

    if let Some(idx) = existing_idx {
        // Update existing account
        let existing_id = data.accounts[idx].id.clone();
        data.accounts[idx] = Account {
            id: existing_id.clone(),
            ..account
        };
        // Update the API key
        save_api_key_for_account(&existing_id, &api_key)?;
        data.active_account_id = Some(existing_id);
    } else {
        // Save the API key for this account
        save_api_key_for_account(&account.id, &api_key)?;

        // Set as active account
        data.active_account_id = Some(account.id.clone());

        // Add to accounts list
        data.accounts.push(account.clone());
    }

    save_accounts_data(&data)?;

    // Update AppState with the new active account
    // Safe to use expect here: active_account_id is always set above in this function
    let active_id = data.active_account_id.as_ref()
        .ok_or_else(|| AuthError::Internal("active_account_id should be set".to_string()))?;
    let active_account = data.accounts.iter().find(|a| &a.id == active_id)
        .ok_or_else(|| AuthError::Internal("active account not found in list".to_string()))?;
    match state.server_url.write() {
        Ok(mut guard) => *guard = Some(active_account.server_url.clone()),
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }
    match state.api_key.write() {
        Ok(mut guard) => *guard = Some(api_key),
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }

    Ok(data)
}

/// Delete an account and its API key
#[tauri::command]
pub async fn delete_account(
    account_id: String,
    state: State<'_, AppState>,
) -> Result<AccountsData, AuthError> {
    let mut data = load_accounts_data()?;

    // Remove the account
    data.accounts.retain(|a| a.id != account_id);

    // Delete the API key
    delete_api_key_for_account(&account_id)?;

    // If we deleted the active account, switch to the first remaining one
    if data.active_account_id.as_ref() == Some(&account_id) {
        data.active_account_id = data.accounts.first().map(|a| a.id.clone());

        // Update AppState
        if let Some(new_active_id) = &data.active_account_id {
            if let Some(account) = data.accounts.iter().find(|a| &a.id == new_active_id) {
                if let Some(api_key) = get_api_key_for_account(&account.id)? {
                    match state.server_url.write() {
                        Ok(mut guard) => *guard = Some(account.server_url.clone()),
                        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
                    }
                    match state.api_key.write() {
                        Ok(mut guard) => *guard = Some(api_key),
                        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
                    }
                }
            }
        } else {
            // No accounts left, clear AppState
            match state.server_url.write() {
                Ok(mut guard) => *guard = None,
                Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
            }
            match state.api_key.write() {
                Ok(mut guard) => *guard = None,
                Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
            }
        }
    }

    save_accounts_data(&data)?;

    Ok(data)
}

/// Set the active account and update AppState
#[tauri::command]
pub async fn set_active_account(
    account_id: String,
    state: State<'_, AppState>,
) -> Result<Account, AuthError> {
    let mut data = load_accounts_data()?;

    // Find the account
    let account = data
        .accounts
        .iter()
        .find(|a| a.id == account_id)
        .ok_or_else(|| AuthError::AccountNotFound(account_id.clone()))?
        .clone();

    // Get the API key
    let api_key = get_api_key_for_account(&account_id)?
        .ok_or_else(|| AuthError::KeyringError("API key not found".to_string()))?;

    // Update active account
    data.active_account_id = Some(account_id.clone());

    // Update last_used_at
    if let Some(acc) = data.accounts.iter_mut().find(|a| a.id == account_id) {
        acc.last_used_at = chrono::Utc::now().to_rfc3339();
    }

    save_accounts_data(&data)?;

    // Update AppState
    match state.server_url.write() {
        Ok(mut guard) => *guard = Some(account.server_url.clone()),
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }
    match state.api_key.write() {
        Ok(mut guard) => *guard = Some(api_key),
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }

    Ok(account)
}

/// Generate a new unique account ID
#[tauri::command]
pub fn generate_account_id() -> String {
    Uuid::new_v4().to_string()
}

// ============================================================================
// Legacy commands for backwards compatibility during migration
// ============================================================================

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
    match state.server_url.write() {
        Ok(mut guard) => *guard = Some(server_url),
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }
    match state.api_key.write() {
        Ok(mut guard) => *guard = Some(api_key),
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }

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
            match state.server_url.write() {
                Ok(mut guard) => *guard = Some(credentials.server_url.clone()),
                Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
            }
            match state.api_key.write() {
                Ok(mut guard) => *guard = Some(credentials.api_key.clone()),
                Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
            }

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
    match state.server_url.write() {
        Ok(mut guard) => *guard = None,
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }
    match state.api_key.write() {
        Ok(mut guard) => *guard = None,
        Err(e) => return Err(AuthError::Internal(format!("State lock poisoned: {e}"))),
    }

    Ok(())
}
