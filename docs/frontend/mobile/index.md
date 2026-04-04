# Mobile App

Relate Mail's mobile client is a React Native application built with [Expo SDK 54](https://expo.dev/) and [Expo Router v4](https://docs.expo.dev/router/introduction/) for file-based navigation. It provides a native email experience on iOS and Android, with a web fallback available for development purposes.

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | React Native 0.81 with Expo SDK 54 (New Architecture enabled) |
| Navigation | Expo Router v4 (file-based routing in `app/`) |
| Styling | NativeWind 4.1 (Tailwind CSS for React Native) |
| Server State | TanStack Query v5 (30s stale time, 2 retries) |
| Client State | Zustand v5 (persisted to AsyncStorage) |
| Icons | Lucide React Native |
| Real-time | SignalR client (`@microsoft/signalr`) |

## Key Dependencies

The mobile app relies on several Expo modules for native platform integration:

- **expo-secure-store** -- Encrypted credential storage (iOS Keychain / Android Keystore)
- **expo-local-authentication** -- Biometric authentication (Face ID, Touch ID, fingerprint)
- **expo-auth-session** -- OIDC authentication with PKCE
- **expo-crypto** -- Cryptographic operations for secure token handling
- **expo-web-browser** -- In-app browser for OAuth flows
- **react-native-webview** -- HTML email rendering with DOMPurify sanitization
- **react-native-gesture-handler** -- Swipe gestures for email actions
- **react-native-reanimated** -- Smooth animations for transitions and gestures
- **tweetnacl** -- NaCl encryption for web platform fallback storage

## Multi-Account Architecture

The mobile app supports connecting to multiple Relate Mail servers simultaneously. Each account stores its own server URL, user profile, and API key. Users can switch between accounts from the inbox header, and all query caches are scoped by account ID to prevent data leakage between accounts.

Account metadata is persisted to AsyncStorage via Zustand, while sensitive credentials (API keys) are stored in the platform's secure storage (see [Security](./security.md)).

## Platform Support

| Platform | Status |
|---|---|
| iOS | Full support (iPhone and iPad) |
| Android | Full support |
| Web | Development fallback only |

The app uses `portrait` orientation by default and supports both light and dark modes via the `userInterfaceStyle: "automatic"` setting.

## App Identity

- **Bundle identifier**: `dev.fourrobots.relatemail` (iOS and Android)
- **URL scheme**: `relate-mail://` (used for OIDC callback deep links)
- **EAS project**: Cloud builds configured via Expo Application Services

## Project Structure

```
mobile/
  app/                      # Expo Router file-based routes
    (auth)/                 # Unauthenticated login flow
    (main)/                 # Authenticated app screens
      (tabs)/               # Bottom tab navigation
      emails/               # Email detail screens
  components/
    mail/                   # Email-specific components
    ui/                     # Reusable UI components
  lib/
    api/                    # API client and React Query hooks
    auth/                   # Account store, OIDC, secure storage
    security/               # Certificate pinning
  assets/                   # App icons and splash screen
  e2e/                      # Detox end-to-end tests
```

## Quick Start

```bash
cd mobile
npm install

# Start the Expo dev server
npm start

# Platform-specific
npm run ios       # Open in iOS simulator
npm run android   # Open in Android emulator
npm run web       # Open in web browser (dev fallback)
```

For testing, see [Testing](./testing.md). For build and distribution, the app uses EAS Build:

```bash
npm run build:ios       # EAS cloud build for iOS
npm run build:android   # EAS cloud build for Android
```

::: info Screenshot
**[Screenshot placeholder: Mobile inbox]**

_TODO: Add screenshot of the mobile inbox view showing the email list with swipe actions and account switcher_
:::
