# Rust Backend

The Tauri Rust backend handles operating system integration, secure credential storage, HTTP proxying, and system tray management. All commands are invokable from the React frontend via Tauri's IPC bridge.

## Entry Point

The application entry is defined in `src-tauri/src/lib.rs`. The `run()` function configures the Tauri builder with:

1. **Plugins** -- Registers `tauri-plugin-shell` (external URL opening) and `tauri-plugin-notification` (native alerts).
2. **App state** -- Initializes shared `AppState` and makes it available to all commands via Tauri's state management.
3. **System tray** -- Creates the tray icon and menu during setup.
4. **Window close handler** -- Intercepts the window close event. If the user has enabled "minimize to tray" in settings, the window is hidden instead of closed. Otherwise, the app quits normally.
5. **Command registration** -- All IPC commands are registered with `tauri::generate_handler![]`.

## AppState

The `AppState` struct (defined in `commands/mod.rs`) holds shared, mutable state accessible to all Tauri commands. It is initialized with `Default::default()` during setup and injected into command functions via Tauri's `State<AppState>` extractor.

## Command Modules

### auth.rs -- Account and Credential Management

This module manages multi-account storage using the operating system's native credential manager via the `keyring` crate.

**Constants:**
- Service name: `com.relate.mail.desktop`
- Accounts storage key: `accounts`

**Data structures:**

```rust
Account {
    id: String,              // UUID
    display_name: String,    // User's name
    server_url: String,      // Relate Mail server URL
    user_email: String,      // User's email
    api_key_id: String,      // ID of the stored API key
    scopes: Vec<String>,     // Granted API key scopes
    created_at: String,      // ISO timestamp
    last_used_at: String,    // ISO timestamp
}

AccountsData {
    accounts: Vec<Account>,
    active_account_id: Option<String>,
}
```

**Commands:**

| Command | Description |
|---|---|
| `load_accounts()` | Loads all accounts from the OS keyring. Returns `AccountsData` with the account list and active account ID. |
| `get_account_api_key(account_id)` | Retrieves the API key for a specific account from the keyring. The key is stored separately from account metadata for security. |
| `save_account(account)` | Saves or updates an account in the keyring. Serializes the full `AccountsData` to JSON and stores it. |
| `delete_account(account_id)` | Removes an account and its API key from the keyring. If the deleted account was active, clears the active selection. |
| `set_active_account(account_id)` | Updates which account is the active one. Validates the account exists before setting. |
| `generate_account_id()` | Generates a new UUID v4 string for use as an account identifier. |

Legacy `save_credentials`, `load_credentials`, and `clear_credentials` commands are also registered for backward compatibility during migration from the single-account model.

**Error types:**

```rust
AuthError {
    KeyringError(String),       // OS keyring access failed
    SerializationError(String), // JSON serialization/deserialization failed
    AccountNotFound(String),    // Requested account ID does not exist
    Internal(String),           // Other unexpected errors
}
```

All error types implement `Serialize` so they can be transmitted across the Tauri IPC boundary to the frontend.

### api.rs -- HTTP Proxy

The API proxy commands route HTTP requests from the frontend to the Relate Mail backend API, injecting authentication headers from the keyring.

| Command | HTTP Method |
|---|---|
| `api_get()` | GET |
| `api_post()` | POST |
| `api_put()` | PUT |
| `api_patch()` | PATCH |
| `api_delete()` | DELETE |

Each command:

1. Reads the active account's API key from the keyring
2. Constructs the full URL from the account's `server_url` + the requested path
3. Sets `Authorization: ApiKey {key}` header
4. Forwards request body (for POST/PUT/PATCH) and query parameters
5. Returns the response body and status code to the frontend

This proxy pattern keeps API keys in the Rust backend and out of the JavaScript context, preventing credential exposure through browser devtools or XSS.

### oidc.rs -- OIDC Authentication

Handles the OIDC login flow for adding new accounts.

| Command | Description |
|---|---|
| `discover_server(url)` | Fetches `/api/discovery` from the given server URL to retrieve capabilities and OIDC configuration. |
| `start_oidc_auth(config)` | Opens the system browser for OIDC authentication using the `open` crate. Constructs the authorization URL with PKCE parameters and returns the authorization code after callback. |
| `fetch_profile_with_jwt(jwt)` | Retrieves the user profile from the API using a temporary JWT Bearer token. Used during account setup to get display name and email. |
| `create_api_key_with_jwt(jwt, platform)` | Creates an API key via `POST /api/smtp-credentials` using the temporary JWT. The key is then stored in the keyring for ongoing use. |

### settings.rs -- App Preferences

Persists application settings to the filesystem (Tauri's app data directory).

| Command | Description |
|---|---|
| `get_settings()` | Loads settings from disk. Returns defaults if no settings file exists. |
| `save_settings(settings)` | Writes settings to disk as JSON. |

**Settings fields:**

| Field | Type | Default | Description |
|---|---|---|---|
| `minimize_to_tray` | bool | false | Hide window on close instead of quitting |
| `theme` | string | "system" | UI theme: "light", "dark", or "system" |
| `notifications` | bool | true | Enable native new email notifications |

The `get_settings_sync` function is also available (non-async) for use in the window close handler, where async operations are not supported.

### tray.rs -- System Tray

Manages the system tray icon and menu.

| Command | Description |
|---|---|
| `create_tray(app_handle)` | Builds the system tray with an icon and context menu containing Inbox, Settings, and Quit items. Called during app setup. |
| `set_tray_tooltip(message)` | Updates the tray icon tooltip text (e.g., "Relate Mail - 3 unread"). |
| `set_badge_count(count)` | Updates the tray icon or menu to reflect the unread email count. |

Tray menu item clicks are handled via event listeners that navigate the main window to the appropriate route or quit the application.

## Rust Dependencies

Key dependencies from `Cargo.toml`:

| Crate | Purpose |
|---|---|
| `tauri` | Application framework (with tray-icon and devtools features) |
| `keyring` | OS-native credential storage (macOS Keychain, Windows Credential Manager, Linux Secret Service) |
| `reqwest` | HTTP client for API proxy and OIDC operations |
| `serde` / `serde_json` | Serialization for IPC and storage |
| `uuid` | Account ID generation |
| `chrono` | Timestamp handling |
| `open` | Opens URLs in the system browser |
| `thiserror` | Ergonomic error type derivation |
| `tokio` | Async runtime (full features) |

## Build Profile

The release build is optimized for size:

- **LTO**: Full link-time optimization enabled
- **Codegen units**: 1 (maximizes optimization)
- **Opt level**: `s` (optimize for size)
- **Panic**: abort (reduces binary size)
- **Strip**: true (removes debug symbols)

Clippy lints are configured strictly: `unsafe_code` is denied, and `unwrap_used`, `expect_used`, and `panic` generate warnings to encourage proper error handling.
