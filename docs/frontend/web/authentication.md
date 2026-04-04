# Authentication

The web application supports OIDC (OpenID Connect) authentication using the `react-oidc-context` library backed by `oidc-client-ts`. Authentication is optional -- when no OIDC provider is configured, the application runs in development mode without any authentication layer.

## AuthProvider (`src/auth/AuthProvider.tsx`)

The `AuthProvider` component wraps the entire application and conditionally enables OIDC authentication.

### Behavior

1. Reads the runtime configuration via `getConfig()` to check for an `oidcAuthority` value
2. If **no authority is configured**: renders children directly without any auth wrapper. A console warning is logged indicating development mode.
3. If **an authority is configured**: wraps children in `react-oidc-context`'s `OidcAuthProvider` with the OIDC configuration described below.

### OIDC Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| `authority` | From `config.oidcAuthority` | The OIDC provider's base URL |
| `client_id` | From `config.oidcClientId` | The registered client identifier |
| `redirect_uri` | From config, or `window.location.origin` | Where the provider redirects after login |
| `response_type` | `code` | Authorization code flow |
| `scope` | `openid profile email` (configurable) | Standard OIDC scopes |
| `client_authentication` | `undefined` | Public client -- no client secret sent |
| `userStore` | `sessionStorage` | Tokens stored in session storage |
| `automaticSilentRenew` | `true` | Tokens are refreshed automatically before expiry |
| `loadUserInfo` | `true` | Fetches the UserInfo endpoint after login |

This configuration uses the **authorization code flow with PKCE** (Proof Key for Code Exchange), which is the recommended flow for single-page applications. No client secret is required because the app is a public client -- PKCE provides the security that a secret would normally offer.

### Callback Handlers

**`onSigninCallback`** -- After the OIDC provider redirects back with an authorization code, this handler removes the `code` and `state` query parameters from the URL using `window.history.replaceState()`. This prevents the parameters from appearing in the address bar and avoids issues if the user refreshes the page.

**`onSignoutCallback`** -- Redirects to the application root (`/`) after the user signs out.

**`onSigninError`** -- Logs the error and produces a user-friendly message based on the error type:
- Network errors: "Network error during authentication. Please check your connection."
- Token expiry: "Your session has expired. Please sign in again."
- Other: "Authentication failed. Please try again."

## Login Route (`src/routes/login.tsx`)

The login page displays a branded card with a "Login with OIDC" button. Clicking the button initiates the OIDC authorization code flow, which redirects the user to the configured identity provider.

If the user is already authenticated (has a valid session), they are automatically redirected to the inbox.

## Callback Route (`src/routes/callback.tsx`)

This route handles the redirect from the OIDC identity provider after the user authenticates. It:

1. Processes the authorization response (extracts the code from query parameters)
2. Exchanges the authorization code for access and ID tokens via the token endpoint
3. Stores the tokens in `sessionStorage`
4. Redirects the user to the inbox

If any step fails, an error message is displayed on the callback page.

## Token Usage in API Requests

The API client (`src/api/client.ts`) automatically extracts the access token from `sessionStorage` for every request:

1. Looks for a key matching `oidc.user:*` in `sessionStorage`
2. Parses the stored JSON to extract `access_token`
3. Includes it as a `Bearer` token in the `Authorization` header
4. Falls back to `localStorage` if no session storage key is found

Storage access is wrapped in a `safeStorageAccess()` helper to handle browsers where storage throws exceptions (e.g., Safari private browsing with restricted storage).

## Protected Routes

The root layout (`src/routes/__root.tsx`) enforces authentication for all routes except `/login` and `/callback`. When the OIDC provider is configured and the user is not authenticated, navigating to any protected route redirects to `/login`.

## Development Mode (No Auth)

When neither `VITE_OIDC_AUTHORITY` (build-time) nor the runtime config's `oidcAuthority` is set:

- The `AuthProvider` renders children without wrapping in the OIDC provider
- The API client sends requests without an `Authorization` header
- The backend also runs without authentication, accepting all requests
- All routes are accessible without login

This mode is intended for local development and testing.

## Configuration Reference

### Backend (environment variables or `appsettings.json`)

| Variable | Description |
|----------|-------------|
| `Oidc__Authority` | OIDC provider URL (e.g., `https://auth.example.com`) |
| `Oidc__Audience` | Expected audience for token validation |

### Frontend (build-time environment variables)

| Variable | Description |
|----------|-------------|
| `VITE_OIDC_AUTHORITY` | OIDC provider URL |
| `VITE_OIDC_CLIENT_ID` | Client ID registered with the provider |

### Frontend (runtime configuration via `/config/config.json`)

The `getConfig()` function loads runtime configuration from the backend's `ConfigController`, which allows OIDC settings to be changed without rebuilding the frontend. Runtime config takes precedence over build-time environment variables.
