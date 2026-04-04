# Desktop App

Relate Mail's desktop client is built with [Tauri 2](https://v2.tauri.app/), combining a React frontend with a Rust backend to deliver a native desktop email experience. The app integrates deeply with operating system features including the system tray, native notifications, and the OS credential manager.

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | Tauri 2 (Rust backend + web frontend) |
| Frontend | React 19, Vite 7, TypeScript 5.9 |
| Styling | Tailwind CSS 4.1 |
| Server State | TanStack Query v5 |
| Client State | Jotai v2 (atoms backed by Tauri commands) |
| Real-time | SignalR client + fallback polling |
| Icons | Lucide React (via `@relate/shared`) |
| Notifications | tauri-plugin-notification |

## Platform Support

The desktop app builds for all three major platforms:

| Platform | Package Formats |
|---|---|
| Windows | NSIS installer (.exe), MSI installer |
| macOS | DMG disk image |
| Linux | AppImage, Debian package (.deb) |

## Window Configuration

| Setting | Value |
|---|---|
| Default size | 1200 x 800 px |
| Minimum size | 800 x 600 px |
| Centering | Enabled (opens centered on screen) |
| Decorations | Native window decorations |
| Transparency | Disabled |

## Content Security Policy

The app enforces a strict CSP to prevent code injection:

```
default-src 'self';
script-src 'self';
style-src 'self' 'unsafe-inline';
img-src 'self' data: blob:;
font-src 'self';
connect-src 'self' https: wss:;
frame-src 'none';
object-src 'none'
```

This allows the React frontend to function normally while blocking inline scripts, external frames, and plugin objects. WebSocket connections (`wss:`) are allowed for SignalR real-time communication.

## App Identity

- **Identifier**: `com.relate.mail.desktop`
- **Product name**: Relate Mail
- **Version**: 0.1.0

## Tauri Plugins

The app uses two Tauri plugins:

- **tauri-plugin-shell** -- Opens external URLs in the system browser (used for OIDC authentication and help links)
- **tauri-plugin-notification** -- Delivers native OS notifications for new email alerts

## Project Structure

```
desktop/
  src/                          # React frontend source
    hooks/                      # Custom hooks (window state, shortcuts, theme, SignalR, polling)
    lib/                        # Jotai atoms, Tauri API wrappers
  src-tauri/
    src/
      lib.rs                    # Tauri builder, plugin registration, setup
      main.rs                   # Entry point
      commands/
        mod.rs                  # Module declarations, AppState
        auth.rs                 # Account & credential management via OS keyring
        api.rs                  # HTTP proxy commands
        oidc.rs                 # OIDC authentication flow
        settings.rs             # App preferences persistence
        tray.rs                 # System tray creation and management
    tauri.conf.json             # Tauri configuration (window, CSP, icons, bundle)
    icons/                      # App icons for all platforms
    Cargo.toml                  # Rust dependencies
  package.json                  # Frontend dependencies and scripts
```

## Quick Start

```bash
# From the repository root
npm run dev:desktop

# Or from the desktop directory
cd desktop
npm run tauri:dev
```

This starts the Vite dev server for the frontend and the Tauri development window simultaneously. Changes to both the React frontend and Rust backend trigger hot reloads.

For production builds and platform-specific packaging, see [Building](./building.md).

::: info Screenshot
![Screenshot: Desktop window](./screenshots/desktop-window.png)

_TODO: Add screenshot of the desktop application window showing the main email interface_
:::
