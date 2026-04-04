# Mobile Build Workflow

**File:** `.github/workflows/mobile-build.yml`

This workflow builds the Relate Mail mobile app for Android and iOS using Expo Application Services (EAS). It runs lint, type checking, and tests before submitting builds to the EAS cloud build service.

## Triggers

| Event | Condition |
|-------|-----------|
| Push to `main` | When `mobile/**` or the workflow file changes |
| Pull request | When `mobile/**` changes |
| Manual dispatch | With platform and profile selection |

### Manual Dispatch Inputs

When triggered manually via `workflow_dispatch`, two inputs are available:

| Input | Options | Default | Description |
|-------|---------|---------|-------------|
| `platform` | `all`, `android`, `ios` | `all` | Which platform(s) to build |
| `profile` | `development`, `preview`, `production` | `preview` | EAS build profile |

**Build profiles:**
- `development` -- Debug build for simulators/emulators, enables developer tools
- `preview` -- Release build for internal testing, installable on physical devices
- `production` -- Store-ready build for App Store and Google Play submission

## Jobs

### `lint-typecheck-test` -- Lint, Type Check & Test

Runs on every trigger as the first quality gate.

**Steps:**
1. Checkout repository
2. Setup Node.js 20 with npm caching
3. `npm ci` -- install dependencies from lockfile
4. `npx tsc --noEmit` -- TypeScript type checking
5. `npm run lint` -- ESLint (failures are non-blocking with `|| true`)
6. `npm run test:coverage` -- Jest unit tests with coverage reporting

All steps run in the `mobile/` working directory.

### `build-android` -- Build Android

**Depends on:** `lint-typecheck-test`  
**Condition:** Push events, or manual dispatch with `platform` set to `all` or `android`

**Steps:**
1. Checkout repository
2. Setup Node.js 20
3. Setup Expo CLI (`expo/expo-github-action`) with EAS CLI, authenticated via `EXPO_TOKEN` secret
4. `npm ci` -- install dependencies
5. Check if EAS is configured (looks for `projectId` in `app.json`)
6. If configured: `eas build --platform android --profile <profile> --non-interactive`
7. If not configured: skip build with a notice

The build profile defaults to `preview` for push events and uses the manually selected profile for dispatch events.

### `build-ios` -- Build iOS

**Depends on:** `lint-typecheck-test`  
**Condition:** Push events, or manual dispatch with `platform` set to `all` or `ios`

Identical structure to the Android build, but runs `eas build --platform ios` instead.

### `build-summary` -- Build Summary

**Depends on:** `build-android`, `build-ios`  
**Condition:** Always runs

Generates a step summary showing the build status for each platform and links to the Expo dashboard for build artifacts.

## EAS Configuration

The workflow checks for EAS configuration before attempting a build. If `app.json` does not contain a `projectId` field, builds are skipped with a warning. To enable builds:

1. Install the EAS CLI locally: `npm install -g eas-cli`
2. Authenticate: `eas login`
3. Initialize the project: `cd mobile && eas init`
4. Commit the updated `app.json` with the `projectId`

## Required Secrets

| Secret | Description |
|--------|-------------|
| `EXPO_TOKEN` | Expo access token for EAS CLI authentication. Generate at [expo.dev/settings/access-tokens](https://expo.dev/settings/access-tokens). |

## Build Artifacts

EAS builds run in the cloud, so build artifacts (APK, AAB, IPA) are not uploaded as GitHub Actions artifacts. Instead, they are available on the [Expo dashboard](https://expo.dev) under the project's build history. The step summary includes a link to the dashboard.

## Job Dependency Graph

```
lint-typecheck-test
    ├── build-android
    └── build-ios
            └── build-summary (always)
```
