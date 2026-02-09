pub mod api;
pub mod auth;
pub mod oidc;
pub mod settings;
pub mod tray;

use std::sync::RwLock;

#[derive(Default)]
pub struct AppState {
    pub server_url: RwLock<Option<String>>,
    pub api_key: RwLock<Option<String>>,
}
