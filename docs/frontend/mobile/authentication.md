# Authentication

The mobile app implements a multi-account authentication system where users can connect to one or more Relate Mail servers. Authentication uses OIDC for the initial login, after which a long-lived API key is created and stored securely on the device.

## Multi-Account Store

Account state is managed by a Zustand store defined in `lib/auth/account-store.ts`. The store is persisted to AsyncStorage so account metadata survives app restarts.

### Account Interface

Each connected account stores the following data:

| Field | Type | Description |
|---|---|---|
| `id` | string | Unique account identifier (UUID) |
| `displayName` | string | User's display name from their profile |
| `serverUrl` | string | Base URL of the Relate Mail server |
| `userEmail` | string | User's email address |
| `apiKeyId` | string | ID of the API key (not the key itself) |
| `scopes` | string[] | API key scopes granted |
| `createdAt` | string | When the account was added |
| `lastUsedAt` | string | Last time this account was active |
| `isActive` | boolean | Whether this is the currently active account |

Note that the actual API key value is never stored in the Zustand store. It is kept in platform secure storage (see [Security](./security.md)) and retrieved only when needed for API requests.

### Store Actions

The account store exposes the following actions:

- **`addAccount(account)`** -- Adds a new account and sets it as active. Deactivates any previously active account.
- **`removeAccount(id)`** -- Removes an account by ID. If the removed account was active, the first remaining account becomes active (if any).
- **`setActiveAccount(id)`** -- Switches the active account. Updates `lastUsedAt` on the newly active account.
- **`updateAccount(id, updates)`** -- Partial update of account metadata (e.g., refresh display name).
- **`updateLastUsed(id)`** -- Updates the `lastUsedAt` timestamp for the given account.
- **`getActiveAccount()`** -- Returns the currently active account, or `undefined` if none.

### Hooks

The store provides several convenience hooks for use in components:

- **`useActiveAccount()`** -- Returns the active account (reactive, re-renders on change).
- **`useAccounts()`** -- Returns the full list of accounts.
- **`useHasAccounts()`** -- Returns a boolean indicating whether any accounts exist. Used by the root index to determine initial navigation.

## OIDC Flow

The OIDC authentication logic lives in `lib/auth/oidc.ts`. The flow uses Expo AuthSession with PKCE (Proof Key for Code Exchange) for secure authorization.

### Server Discovery

Before authenticating, the app discovers the server's capabilities:

```
discoverServer(url) ->
  1. GET {url}/api/discovery    -- Server capabilities and OIDC info
  2. GET {url}/config/config.json  -- Frontend config (OIDC client ID, etc.)
  3. Validate HTTPS (HTTP allowed only for localhost during development)
  4. Return merged server configuration
```

Each HTTP request has a 10-second timeout and validates that the response is valid JSON. Discovery failures present user-friendly error messages explaining what went wrong.

### Authorization Flow

Once the server configuration is known, the OIDC flow proceeds:

```
performOidcAuth(config) ->
  1. Generate PKCE code_verifier and code_challenge
  2. Build authorization URL with:
     - response_type: code
     - client_id: from server config
     - redirect_uri: relate-mail://auth/callback
     - scope: openid profile email
     - code_challenge + code_challenge_method: S256
  3. Open system browser via Expo AuthSession
  4. User authenticates with OIDC provider
  5. Receive authorization code via deep link callback
  6. Exchange code for tokens (access_token, id_token)
  7. Return JWT for API key creation
```

The redirect URI `relate-mail://auth/callback` is registered as the app's URL scheme in `app.json`, allowing the OIDC provider to redirect back to the app after authentication.

## API Key Creation

After obtaining a JWT from the OIDC flow, the app creates a long-lived API key:

1. Create a temporary API client authenticated with the JWT (`Authorization: Bearer {jwt}`)
2. Call `POST /api/smtp-credentials` to create an API key with all scopes (`smtp`, `pop3`, `imap`, `api:read`, `api:write`, `app`)
3. Store the API key value in secure storage keyed by account ID
4. Store the API key ID (not the value) in the Zustand account store
5. Discard the JWT -- all subsequent API calls use the API key

This design means the short-lived OIDC token is only used once, and the app thereafter relies on the API key for authentication. The API key does not expire automatically but can be rotated or revoked from the API Keys screen.

## Account Switching

When the user switches active accounts:

1. The Zustand store updates `isActive` on the old and new accounts
2. `lastUsedAt` is updated on the newly active account
3. All TanStack Query caches are invalidated -- this forces fresh data fetches scoped to the new account
4. The API client automatically picks up the new account's credentials on the next request

Query keys include the account ID (e.g., `["emails", accountId, ...]`), so cached data from one account is never accidentally displayed for another.
