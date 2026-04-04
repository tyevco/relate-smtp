# Security

The mobile app implements multiple layers of security to protect user credentials and email data, including encrypted credential storage, biometric authentication, and certificate pinning.

## Secure Storage

API keys are stored using platform-native secure storage, implemented in `lib/auth/secure-storage.ts`.

### Native Platforms (iOS and Android)

On native platforms, the app uses **Expo SecureStore**, which delegates to:

- **iOS**: Keychain Services with `WHEN_UNLOCKED` accessibility level, meaning credentials are only accessible when the device is unlocked by the user.
- **Android**: Android Keystore system, which stores cryptographic keys in a container that is more difficult to extract from the device.

### Web Platform Fallback

Since Expo SecureStore is not available on web, the app provides a fallback using **NaCl encryption** (via the `tweetnacl` library) with sessionStorage:

1. A random encryption key is generated per session
2. API keys are encrypted with NaCl's `secretbox` before storage
3. Encrypted values are stored in `sessionStorage` (cleared when the browser tab closes)
4. The encryption key is held in memory only and never persisted

This provides reasonable protection for the development-only web fallback, though it is not as secure as native Keychain/Keystore storage.

### Storage API

The secure storage module exposes the following functions:

| Function | Description |
|---|---|
| `storeApiKey(accountId, apiKey)` | Encrypt and store an API key for the given account |
| `getApiKey(accountId)` | Retrieve and decrypt the API key for the given account |
| `deleteApiKey(accountId)` | Remove the stored API key for the given account |
| `isSecureStorageAvailable()` | Check whether native secure storage is available |

Keys are stored with the prefix `relate_api_key_{accountId}`, ensuring each account's credentials are isolated.

## Biometric Authentication

The app supports biometric authentication via `lib/auth/biometric.ts`, using **expo-local-authentication** to integrate with platform biometric APIs.

### Supported Biometric Types

| Platform | Methods |
|---|---|
| iOS | Face ID, Touch ID |
| Android | Fingerprint, Face Unlock, Iris |
| Web | Not supported (biometrics disabled) |

### Biometric Functions

- **`isBiometricAvailable()`** -- Checks whether the device has biometric hardware and whether the user has enrolled at least one biometric credential.
- **`getBiometricType()`** -- Returns the specific type of biometric available (e.g., "Face ID", "Touch ID", "Iris"), used to display appropriate UI labels and icons.
- **`authenticateWithBiometrics()`** -- Triggers the platform biometric prompt. Returns a success/failure result. The prompt message reads: "Relate Mail uses Face ID to protect access to your email accounts" (as configured in `app.json` for iOS).

### Biometric Preferences Store

A separate Zustand store (persisted to AsyncStorage) tracks the user's biometric preferences:

- Whether biometric lock is enabled
- The last time biometric authentication succeeded

This store is separate from the account store because biometric settings are device-wide, not per-account.

## BiometricGate Component

The `components/ui/biometric-gate.tsx` component wraps the entire app content and enforces biometric authentication when enabled.

### Behavior

1. **On app launch** -- If biometric lock is enabled, the gate displays a lock screen and triggers the biometric prompt before showing any app content.
2. **On background resume** -- When the app returns from the background, the gate re-prompts for biometric authentication. This prevents unauthorized access if someone picks up an unlocked device.
3. **Lock screen** -- While locked, the gate renders a branded lock screen with a button to retry biometric authentication. No email data is visible behind it.
4. **Fallback** -- If biometric authentication fails (e.g., too many attempts), the user can retry. The app does not fall back to a PIN or password.

The BiometricGate is positioned in the root layout, above the navigation stack but below the QueryClientProvider, so it blocks all app interaction until authentication succeeds.

## Certificate Pinning

The app implements TLS certificate pinning via `lib/security/certificate-pinning.ts` and a custom Expo config plugin at `./plugins/withCertificatePinning`.

### Purpose

Certificate pinning prevents man-in-the-middle (MITM) attacks by validating that the server's TLS certificate matches a known, trusted certificate. Even if an attacker compromises a certificate authority, pinned connections will reject the forged certificate.

### Implementation

The Expo config plugin modifies the native project configuration during `expo prebuild` to inject certificate pinning rules into the platform's network security configuration:

- **iOS** -- Configures App Transport Security (ATS) with pinned certificates
- **Android** -- Adds a `network_security_config.xml` with pin entries

Certificate pins are configured per server domain and must be updated when server certificates are rotated.

::: info Screenshot
![Screenshot: Biometric authentication](./screenshots/mobile-biometric.png)

_TODO: Add screenshot of the biometric authentication lock screen with Face ID/Touch ID prompt_
:::
