# Desktop Build Workflow

**File:** `.github/workflows/desktop-build.yml`

This workflow builds the Relate Mail desktop application for Windows, macOS, and Linux using Tauri 2. Each platform build runs on a native runner and produces installable artifacts.

## Triggers

| Event | Condition |
|-------|-----------|
| Push to `main` | When `desktop/**`, `packages/shared/**`, or the workflow file changes |
| Pull request | Same path filters, targeting `main` |
| Manual dispatch | With platform selection |

### Manual Dispatch Input

| Input | Options | Default | Description |
|-------|---------|---------|-------------|
| `platform` | `all`, `windows`, `macos`, `linux` | `all` | Which platform(s) to build |

## Jobs

### `lint-typecheck` -- Lint & Type Check

Runs on `ubuntu-latest` as the first quality gate.

**Steps:**
1. Checkout repository
2. Setup Node.js 20 with npm caching
3. `npm ci` -- install all workspace dependencies
4. `npm run lint -w desktop` -- ESLint on TypeScript sources
5. `npx tsc --noEmit -p desktop/tsconfig.json` -- TypeScript type checking

### `build-windows` -- Build Windows

**Depends on:** `lint-typecheck`  
**Runs on:** `windows-latest`  
**Condition:** All triggers unless manually dispatched with a different platform

**Steps:**
1. Checkout repository
2. Setup Node.js 20
3. Setup Rust stable toolchain
4. Rust cache (Cargo registry + `desktop/src-tauri/target`)
5. `npm ci` -- install dependencies
6. `npm run build:desktop` -- Tauri build

**Artifacts:**

| Artifact | Path | Format |
|----------|------|--------|
| `desktop-windows-msi` | `desktop/src-tauri/target/release/bundle/msi/*.msi` | Windows Installer (MSI) |
| `desktop-windows-nsis` | `desktop/src-tauri/target/release/bundle/nsis/*.exe` | NSIS installer (EXE) |

The MSI artifact uses `if-no-files-found: warn` (warns if missing), while NSIS uses `ignore` (silently skips if not produced).

### `build-macos` -- Build macOS

**Depends on:** `lint-typecheck`  
**Runs on:** `macos-latest`  
**Condition:** All triggers unless manually dispatched with a different platform

**Steps:**
1. Checkout repository
2. Setup Node.js 20
3. Setup Rust stable toolchain
4. Rust cache
5. `npm ci` -- install dependencies
6. `npm run build:desktop` -- Tauri build

**Artifacts:**

| Artifact | Path | Format |
|----------|------|--------|
| `desktop-macos-dmg` | `desktop/src-tauri/target/release/bundle/dmg/*.dmg` | macOS Disk Image |

### `build-linux` -- Build Linux

**Depends on:** `lint-typecheck`  
**Runs on:** `ubuntu-22.04` (pinned for library compatibility)  
**Condition:** All triggers unless manually dispatched with a different platform

**Steps:**
1. Checkout repository
2. Install system dependencies:
   - `libwebkit2gtk-4.1-dev` -- WebView rendering engine
   - `libappindicator3-dev` -- System tray support
   - `librsvg2-dev` -- SVG icon rendering
   - `patchelf` -- ELF binary patching for AppImage
   - `libssl-dev` -- TLS support
   - `libdbus-1-dev` -- D-Bus IPC
   - `libsecret-1-dev` -- Secure credential storage
3. Setup Node.js 20
4. Setup Rust stable toolchain
5. Rust cache
6. `npm ci` -- install dependencies
7. `npm run build:desktop` -- Tauri build

**Artifacts:**

| Artifact | Path | Format |
|----------|------|--------|
| `desktop-linux-appimage` | `desktop/src-tauri/target/release/bundle/appimage/*.AppImage` | Portable Linux app |
| `desktop-linux-deb` | `desktop/src-tauri/target/release/bundle/deb/*.deb` | Debian/Ubuntu package |

### `build-summary` -- Build Summary

**Depends on:** `build-windows`, `build-macos`, `build-linux`  
**Condition:** Always runs

Generates a step summary table showing the build status for each platform.

## Platform Runners

Each platform builds on a native GitHub Actions runner to ensure the correct toolchain and system libraries:

| Platform | Runner | Reason |
|----------|--------|--------|
| Windows | `windows-latest` | MSVC toolchain, Windows SDK |
| macOS | `macos-latest` | Xcode toolchain, Apple frameworks |
| Linux | `ubuntu-22.04` | Pinned for consistent `libwebkit2gtk-4.1` availability |

Linux is pinned to `ubuntu-22.04` rather than `ubuntu-latest` to ensure the required WebKitGTK version and other system libraries are available at known versions.

## Job Dependency Graph

```
lint-typecheck
    ├── build-windows
    ├── build-macos
    └── build-linux
           └── build-summary (always)
```

The three platform builds run in parallel after lint/typecheck passes.

## Downloading Artifacts

Build artifacts are available for download from the GitHub Actions run page. Each platform produces one or more installable files:

- **Windows:** MSI installer for managed deployments, NSIS EXE for standalone installation
- **macOS:** DMG disk image that users mount and drag to Applications
- **Linux:** AppImage for portable use (no installation needed), `.deb` for Debian/Ubuntu package management

Artifacts are retained according to the repository's artifact retention policy (default: 90 days).

## Caching

Rust compilation is cached using `swatinem/rust-cache` with the workspace set to `desktop/src-tauri -> target`. This caches:

- Cargo registry index and downloaded crates
- Compiled dependencies in the `target` directory

The cache key is based on `Cargo.lock`, so it invalidates when dependencies change. This typically saves 3-5 minutes per build by avoiding recompilation of unchanged dependencies.
