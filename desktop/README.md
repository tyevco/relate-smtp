# Relate Mail Desktop

A desktop email client for Relate Mail built with Tauri, React, and TypeScript.

## Prerequisites

- Node.js 20+
- Rust (latest stable)
- Windows 10/11 (primary platform)
- Tauri CLI prerequisites: https://tauri.app/v2/guides/getting-started/prerequisites

## Development

### Install dependencies

From the repository root:

```bash
npm install
```

Or from the desktop directory:

```bash
cd desktop
npm install
```

### Run in development mode

```bash
npm run tauri:dev
```

This will:
1. Start the Vite dev server for the frontend
2. Compile the Rust backend
3. Launch the desktop app with hot reload

### Build for production

```bash
npm run tauri:build
```

This creates:
- Windows: MSI installer in `src-tauri/target/release/bundle/msi/`
- Portable EXE in `src-tauri/target/release/`

## Features

- **Email inbox** - View and manage received emails
- **Keyboard shortcuts** - Ctrl+R (refresh), Delete (delete), Escape (back), / (search)
- **Dark mode** - Follows system preference
- **Secure credential storage** - API keys stored in Windows Credential Manager
- **Native window** - Native title bar and window controls

## Architecture

```
desktop/
├── src/                    # React frontend
│   ├── api/               # API client using Tauri invoke
│   ├── components/        # Desktop-specific components
│   ├── hooks/             # Custom React hooks
│   ├── stores/            # Jotai state stores
│   └── views/             # Page components
├── src-tauri/             # Rust backend
│   └── src/
│       └── commands/      # Tauri commands for API, auth, settings
└── package.json
```

## Shared Components

This app uses components from `@relate/shared`:
- UI components (Button, Card, Input, etc.)
- Mail components (EmailList, EmailDetail, SearchBar)
- Type definitions for API responses

## Authentication

The app uses the same authentication flow as the mobile app:
1. Enter server URL
2. Enter email address
3. Enter API key (generated from web interface)
4. Credentials are stored securely in the system keychain

## License

MIT
