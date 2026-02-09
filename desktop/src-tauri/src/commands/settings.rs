use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;
use tauri::{AppHandle, Manager};

#[derive(Debug, thiserror::Error)]
pub enum SettingsError {
    #[error("IO error: {0}")]
    IoError(String),
    #[error("Serialization error: {0}")]
    SerializationError(String),
}

impl serde::Serialize for SettingsError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

#[derive(Serialize, Deserialize, Default)]
pub struct AppSettings {
    pub theme: String,
    pub minimize_to_tray: bool,
    pub show_notifications: bool,
    pub window_width: Option<u32>,
    pub window_height: Option<u32>,
    pub window_x: Option<i32>,
    pub window_y: Option<i32>,
}

fn get_settings_path(app: &AppHandle) -> Result<PathBuf, SettingsError> {
    let app_dir = app
        .path()
        .app_config_dir()
        .map_err(|e| SettingsError::IoError(e.to_string()))?;

    // Create directory if it doesn't exist
    fs::create_dir_all(&app_dir).map_err(|e| SettingsError::IoError(e.to_string()))?;

    Ok(app_dir.join("settings.json"))
}

/// Synchronous version for use in non-async contexts (e.g., window close handler)
pub fn get_settings_sync(app: &AppHandle) -> Result<AppSettings, SettingsError> {
    let path = get_settings_path(app)?;

    if !path.exists() {
        return Ok(AppSettings::default());
    }

    let contents = fs::read_to_string(&path).map_err(|e| SettingsError::IoError(e.to_string()))?;

    serde_json::from_str(&contents).map_err(|e| SettingsError::SerializationError(e.to_string()))
}

#[tauri::command]
pub async fn get_settings(app: AppHandle) -> Result<AppSettings, SettingsError> {
    let path = get_settings_path(&app)?;

    if !path.exists() {
        return Ok(AppSettings::default());
    }

    let contents = fs::read_to_string(&path).map_err(|e| SettingsError::IoError(e.to_string()))?;

    serde_json::from_str(&contents).map_err(|e| SettingsError::SerializationError(e.to_string()))
}

#[tauri::command]
pub async fn save_settings(settings: AppSettings, app: AppHandle) -> Result<(), SettingsError> {
    let path = get_settings_path(&app)?;

    let json = serde_json::to_string_pretty(&settings)
        .map_err(|e| SettingsError::SerializationError(e.to_string()))?;

    fs::write(&path, json).map_err(|e| SettingsError::IoError(e.to_string()))?;

    Ok(())
}
