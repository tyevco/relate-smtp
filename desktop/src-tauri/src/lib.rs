mod commands;

use tauri::Manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_notification::init())
        .setup(|app| {
            // Initialize app state
            app.manage(commands::AppState::default());
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::api::api_get,
            commands::api::api_post,
            commands::api::api_put,
            commands::api::api_patch,
            commands::api::api_delete,
            commands::auth::save_credentials,
            commands::auth::load_credentials,
            commands::auth::clear_credentials,
            commands::settings::get_settings,
            commands::settings::save_settings,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
