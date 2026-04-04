# Building

The desktop app uses Tauri's build system to produce native installers for Windows, macOS, and Linux. The frontend is compiled with Vite, and the Rust backend is compiled to a native binary that embeds the frontend assets.

## Development

Start the development environment with hot reload for both the frontend and Rust backend:

```bash
# From the repository root
npm run dev:desktop

# Or from the desktop directory
cd desktop
npm run tauri:dev
```

This runs two processes simultaneously:

1. **Vite dev server** on `http://localhost:5173` -- serves the React frontend with HMR
2. **Tauri dev window** -- native window that loads from the Vite dev server, with Rust hot-reload on backend changes

The Vite dev server is started by Tauri automatically via the `beforeDevCommand` in `tauri.conf.json`.

## Production Build

Build a distributable installer for the current platform:

```bash
# From the repository root
npm run build:desktop

# Or from the desktop directory
cd desktop
npm run tauri:build
```

The build process:

1. Runs `npm run build` (TypeScript compilation + Vite production build)
2. Bundles the frontend output from `desktop/dist/` into the Rust binary
3. Compiles the Rust backend in release mode (LTO, size optimization, symbol stripping)
4. Packages the binary into platform-specific installers

## Platform Outputs

Tauri produces different installer formats depending on the build platform:

### Windows

| Format | File | Description |
|---|---|---|
| NSIS | `Relate Mail_0.1.0_x64-setup.exe` | Standard Windows installer with install/uninstall wizard |
| MSI | `Relate Mail_0.1.0_x64_en-US.msi` | Windows Installer package for enterprise/GPO deployment |

Both formats are built by default. The NSIS installer provides a familiar user experience, while MSI is preferred for managed enterprise environments.

### macOS

| Format | File | Description |
|---|---|---|
| DMG | `Relate Mail_0.1.0_aarch64.dmg` | Disk image with drag-to-Applications installation |

The DMG is built for the host architecture (Apple Silicon or Intel). Cross-compilation is possible with additional Xcode toolchain configuration.

### Linux

| Format | File | Description |
|---|---|---|
| AppImage | `relate-mail_0.1.0_amd64.AppImage` | Portable, self-contained executable (no installation needed) |
| Deb | `relate-mail_0.1.0_amd64.deb` | Debian/Ubuntu package for APT installation |

Build artifacts are written to `desktop/src-tauri/target/release/bundle/`.

## Icons

Tauri requires icons in multiple formats to support all platforms. The icon files are located in `desktop/src-tauri/icons/`:

| File | Usage |
|---|---|
| `32x32.png` | Windows taskbar, Linux panel |
| `128x128.png` | Standard app icon |
| `128x128@2x.png` | HiDPI/Retina displays |
| `icon.icns` | macOS application icon |
| `icon.ico` | Windows application icon |

To regenerate icons from a source image, use the Tauri CLI:

```bash
cd desktop
npx tauri icon path/to/source-1024x1024.png
```

## Tauri Configuration

The build is configured in `desktop/src-tauri/tauri.conf.json`:

```json
{
  "productName": "Relate Mail",
  "identifier": "com.relate.mail.desktop",
  "version": "0.1.0",
  "build": {
    "beforeDevCommand": "npm run dev",
    "devUrl": "http://localhost:5173",
    "beforeBuildCommand": "npm run build",
    "frontendDist": "../dist"
  },
  "bundle": {
    "active": true,
    "targets": "all"
  }
}
```

Key settings:

- **`frontendDist`** -- Path to the Vite build output, relative to `src-tauri/`
- **`targets: "all"`** -- Builds all available installer formats for the current platform
- **`bundle.active: true`** -- Enables the bundling step (set to `false` to produce only the binary without installers)

## Release Build Optimizations

The Rust release profile in `Cargo.toml` is configured for minimal binary size:

| Setting | Value | Effect |
|---|---|---|
| `codegen-units` | 1 | Single compilation unit enables maximum optimization |
| `lto` | true | Link-time optimization across all crates |
| `opt-level` | "s" | Optimize for binary size over speed |
| `panic` | "abort" | Remove panic unwinding code |
| `strip` | true | Strip debug symbols from the binary |

These settings increase build time but produce a significantly smaller distributable.

## Code Signing

Platform-specific code signing is required for distribution:

### Windows

Configure via `tauri.conf.json` under `bundle.windows`:

```json
{
  "certificateThumbprint": "YOUR_CERT_THUMBPRINT",
  "digestAlgorithm": "sha256",
  "timestampUrl": "http://timestamp.digicert.com"
}
```

Or via environment variables:

- `TAURI_SIGNING_PRIVATE_KEY` -- The private key for signing
- `TAURI_SIGNING_PRIVATE_KEY_PASSWORD` -- The key password

### macOS

Set the following environment variables:

- `APPLE_CERTIFICATE` -- Base64-encoded .p12 certificate
- `APPLE_CERTIFICATE_PASSWORD` -- Certificate password
- `APPLE_SIGNING_IDENTITY` -- Developer ID Application identity
- `APPLE_ID` -- Apple ID for notarization
- `APPLE_PASSWORD` -- App-specific password for notarization
- `APPLE_TEAM_ID` -- Apple Developer team ID

### Linux

Linux packages are typically not code-signed. For distribution via package repositories, GPG signing of the `.deb` package is recommended.

## CI/CD

The `desktop-build.yml` GitHub Actions workflow automates cross-platform builds:

1. **Trigger** -- Runs on push to main and on tags
2. **Matrix** -- Builds on `windows-latest`, `macos-latest`, and `ubuntu-latest` runners
3. **Steps** -- Install dependencies, build frontend, build Tauri, upload artifacts
4. **Artifacts** -- Installers for all platforms are uploaded as workflow artifacts and attached to GitHub Releases on tag pushes

The workflow installs platform-specific prerequisites (e.g., `libwebkit2gtk-4.1-dev` on Linux) before building.
