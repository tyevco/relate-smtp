# Features

The desktop app provides several platform-native features that enhance the email experience beyond what the web client offers.

## System Tray

The app creates a system tray icon with a context menu on launch.

**Menu items:**

- **Inbox** -- Brings the main window to focus and navigates to the inbox
- **Settings** -- Opens the settings view
- **Quit** -- Exits the application completely

**Unread badge:** The tray tooltip and icon update to reflect the current unread email count (e.g., "Relate Mail - 3 unread"). The badge count is updated via the `set_badge_count` Tauri command, triggered by real-time email notifications or periodic polling.

**Minimize to tray:** When enabled in settings, closing the window hides it to the system tray instead of quitting the app. The window can be restored by clicking the tray icon or selecting "Inbox" from the tray menu. This is controlled by the `minimize_to_tray` setting and handled in the Rust backend's window close event handler.

::: info Screenshot
![Screenshot: System tray](./screenshots/desktop-tray.png)

_TODO: Add screenshot of the system tray icon with its context menu and unread badge_
:::

## Window State Persistence

The `useWindowState` hook saves and restores the window's size and position across sessions. When the app launches, it reads the last saved state and applies it, so the window always opens where the user left it.

State is persisted to the Tauri app data directory alongside other settings.

## Keyboard Shortcuts

The `useShortcuts` hook registers global keyboard shortcuts for common actions:

- **Compose** -- Open the compose email view
- **Refresh** -- Reload the current email list
- **Navigation** -- Move between emails, switch views

Shortcuts are registered at the application level and work regardless of which component has focus, as long as the main window is active.

## Keyring Integration

All API keys are stored in the operating system's native credential manager, never in plain text files or browser storage:

| Platform | Credential Store |
|---|---|
| macOS | Keychain Services |
| Windows | Windows Credential Manager |
| Linux | Secret Service (GNOME Keyring, KWallet) |

The `keyring` Rust crate abstracts platform differences. Credentials are stored under the service name `com.relate.mail.desktop` with per-account keys. This provides:

- **Encryption at rest** -- Credentials are encrypted by the OS
- **User consent** -- Some platforms prompt the user when an app accesses credentials
- **Process isolation** -- Other applications cannot read Relate Mail's credentials without explicit permission

## Real-Time Updates

The desktop app receives real-time email notifications through two complementary mechanisms:

### SignalR Connection

The `useSignalR` hook establishes a persistent WebSocket connection to the server's `/hubs/email` endpoint. When new emails arrive, the server pushes a notification through the hub, and the frontend:

1. Invalidates the relevant TanStack Query caches (inbox list, unread count)
2. Updates the system tray badge count
3. Optionally displays a native OS notification

### Fallback Polling

The `usePolling` hook provides a fallback when WebSocket connections are unavailable (e.g., behind restrictive proxies or firewalls). It periodically polls the API for new emails at a configurable interval. The polling hook is only active when the SignalR connection is disconnected.

## Jotai State Management

Client-side state is managed with [Jotai](https://jotai.org/) atoms that are backed by Tauri commands for persistence:

| Atom | Description | Backend Command |
|---|---|---|
| `accountsStateAtom` | Full accounts state (list + active ID) | `load_accounts` / `save_account` |
| `accountsAtom` | Derived: list of all accounts | Read from `accountsStateAtom` |
| `activeAccountIdAtom` | Derived: ID of the active account | `set_active_account` |
| `activeAccountAtom` | Derived: the active account object | Computed from above |
| `hasAccountsAtom` | Derived: boolean, whether any accounts exist | Computed from `accountsAtom` |

When an atom's value changes, the corresponding Tauri command is invoked to persist the change to the OS keyring. On app launch, atoms are hydrated by calling `load_accounts` from the Rust backend. This keeps the React state layer in sync with the secure, persistent storage in Rust.

## Native Notifications

When a new email arrives (detected via SignalR or polling), the app can display a native OS notification using `tauri-plugin-notification`:

- **Title**: Sender name or email address
- **Body**: Email subject line
- **Click action**: Brings the app window to focus and navigates to the email

Notifications respect the user's `notifications` setting and are suppressed when the main window is focused and visible.

## Theme Support

The `useTheme` hook manages the application's visual theme:

| Mode | Behavior |
|---|---|
| Light | Always uses the light color scheme |
| Dark | Always uses the dark color scheme |
| System | Follows the operating system's appearance setting, updating automatically when the user changes their OS theme |

Theme preference is persisted via the `save_settings` Tauri command and loaded on startup. The CSS variables defined in `@relate/shared/styles/theme.css` power the actual styling, ensuring visual consistency with the web client.
