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

            // Create system tray
            if let Err(e) = commands::tray::create_tray(app.handle()) {
                eprintln!("Failed to create tray: {}", e);
            }

            // Handle window close event - minimize to tray instead of quitting
            let app_handle = app.handle().clone();
            if let Some(window) = app.get_webview_window("main") {
                window.on_window_event(move |event| {
                    if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                        // Check settings for minimize_to_tray preference
                        let should_minimize = commands::settings::get_settings_sync(&app_handle)
                            .map(|s| s.minimize_to_tray)
                            .unwrap_or(false);

                        if should_minimize {
                            // Hide window instead of closing
                            api.prevent_close();
                            if let Some(win) = app_handle.get_webview_window("main") {
                                let _ = win.hide();
                            }
                        }
                        // If not minimize_to_tray, default behavior (close + quit)
                    }
                });
            }

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
            commands::tray::set_tray_tooltip,
            commands::tray::set_badge_count,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
