# Navigation

The mobile app uses [Expo Router](https://docs.expo.dev/router/introduction/) for file-based routing. Every file in the `app/` directory maps to a route, and navigation structure is determined by the folder hierarchy and special naming conventions.

## Route Tree

```
app/
  _layout.tsx              # Root layout (providers, splash screen, biometric gate)
  index.tsx                # Entry redirect based on account state
  (auth)/                  # Unauthenticated group
    _layout.tsx            # Stack with slide animation
    index.tsx              # Welcome screen
    add-account.tsx        # Server connection + OIDC flow
    oidc-callback.tsx      # Deep link callback handler
  (main)/                  # Authenticated group
    _layout.tsx            # Stack with modal presentation for email detail
    (tabs)/                # Bottom tab navigation
      _layout.tsx          # Tab bar configuration
      inbox.tsx            # Email inbox
      sent.tsx             # Sent emails
      settings.tsx         # Settings menu
    emails/
      [id].tsx             # Email detail (modal presentation)
    accounts.tsx           # Multi-account management
    api-keys.tsx           # API key rotation and revocation
    preferences.tsx        # User preferences
    security.tsx           # Biometric and security settings
```

## Root Layout

The root `_layout.tsx` sets up the application's provider hierarchy and global wrappers:

1. **QueryClientProvider** -- TanStack Query with 30-second stale time and 2 retries, tuned for an email app where data freshness matters.
2. **SafeAreaProvider** -- Ensures content renders within safe area insets on notched devices.
3. **GestureHandlerRootView** -- Required for swipe gestures on email list items.
4. **ErrorBoundary** -- Catches unhandled errors and displays a fallback UI instead of crashing.
5. **BiometricGate** -- Prompts for biometric authentication before showing app content (if enabled by the user).
6. **Stack** -- Root navigator with two screens: `(auth)` and `(main)`.

The splash screen is held visible during initial render via `SplashScreen.preventAutoHideAsync()` and dismissed after the layout mounts.

## Root Index (Entry Point)

The root `index.tsx` acts as a router guard. It checks `useHasAccounts()` from the Zustand account store:

- If accounts exist, redirect to `/(main)/(tabs)/inbox`
- If no accounts exist, redirect to `/(auth)`

This ensures users always land in the appropriate flow without seeing a blank screen.

## Auth Group

The `(auth)` group handles the onboarding and account connection flow. It uses a Stack navigator with slide transitions.

### Welcome Screen (`index.tsx`)

The landing page for new users. Displays a feature overview of Relate Mail and a prominent "Add Account" call-to-action that navigates to the connection screen.

### Add Account (`add-account.tsx`)

A multi-step flow for connecting to a Relate Mail server:

1. **Server URL input** -- The user enters their server's URL (e.g., `https://mail.example.com`).
2. **Server discovery** -- The app calls `/api/discovery` to fetch server capabilities and configuration, including the OIDC provider details.
3. **OIDC authentication** -- Opens the system browser via Expo AuthSession for the OIDC login flow with PKCE.
4. **API key creation** -- After successful OIDC auth, creates an API key with all scopes using the temporary JWT token.
5. **Account saved** -- The account metadata is persisted to the Zustand store and the API key is stored in secure storage.

### OIDC Callback (`oidc-callback.tsx`)

Handles the deep link callback from the OIDC provider. The app's URL scheme (`relate-mail://auth/callback`) is registered as a redirect URI. This screen extracts the authorization code from the callback URL and completes the token exchange.

## Main Group

The `(main)` group contains all authenticated screens. Its layout checks for active accounts on mount and redirects to `(auth)` if none are found.

### Tab Navigation

The `(tabs)` group renders a bottom tab bar with three tabs:

#### Inbox (`inbox.tsx`)

The primary screen of the app. Features include:

- **Infinite scroll** -- Paginated email loading with automatic fetch on scroll.
- **Pull-to-refresh** -- Swipe down to refresh the email list.
- **Search** -- Filter emails by subject, sender, or content.
- **Swipe actions** -- Right swipe to delete (red background), left swipe to toggle read/unread (blue background).
- **Unread count** -- Badge on the tab icon showing unread email count.
- **Account switcher** -- Header component to switch between connected accounts.

#### Sent (`sent.tsx`)

Displays sent emails with pagination. Same list presentation as the inbox but without swipe actions for read/unread toggling.

#### Settings (`settings.tsx`)

The settings hub showing:

- Current account information (email, server)
- API key age warning (alerts when key is older than 90 days)
- Menu items linking to: Accounts, API Keys, Preferences, Security
- Sign Out button

### Email Detail (`emails/[id].tsx`)

Presented as a modal over the tab navigation. Displays the full email with:

- **Header** -- From, To, Cc fields with formatted addresses
- **Subject** -- Prominently displayed
- **HTML body** -- Rendered in a WebView with DOMPurify sanitization to prevent XSS
- **Attachments** -- Listed with file name and size
- **Actions** -- Mark as read/unread, delete with confirmation

### Account Management Screens

These screens are pushed onto the main stack (not tabs):

- **`accounts.tsx`** -- View all connected accounts, switch the active account, remove accounts with a confirmation dialog.
- **`api-keys.tsx`** -- View API key details, rotate keys (creates new key + revokes old), revoke individual keys. The active account's key cannot be revoked without first rotating.
- **`preferences.tsx`** -- Theme selection (light/dark/system), notification preferences, display density settings.
- **`security.tsx`** -- Toggle biometric authentication, view certificate pinning status.

::: info Screenshot
**[Screenshot placeholder: Mobile tab navigation]**

_TODO: Add screenshot of the bottom tab navigation showing inbox, sent, and settings tabs_
:::
