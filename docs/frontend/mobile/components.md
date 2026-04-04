# Components and API Layer

The mobile app's component library and API layer are organized into focused modules under `components/` and `lib/`. All components are styled with NativeWind (Tailwind CSS for React Native).

## Mail Components

Located in `components/mail/`, these are email-specific UI components.

### EmailListItem (`email-list-item.tsx`)

The primary component for rendering emails in the inbox and sent lists. Built with `react-native-gesture-handler` for swipe interactions.

**Features:**

- Displays sender name, subject line, date, and an attachment icon (when applicable)
- Unread emails are visually distinguished with a bold subject and an unread indicator badge
- **Right swipe** -- Delete action with a red background and trash icon. Triggers a confirmation before deletion.
- **Left swipe** -- Toggle read/unread status with a blue background and mail icon. Applies immediately without confirmation.
- Date formatting via `date-fns` (relative for recent emails, absolute for older ones)

## UI Components

Located in `components/ui/`, these are reusable, general-purpose components used throughout the app.

### Avatar (`avatar.tsx`)

Displays a color-coded circle with initials, used for sender identification in email lists.

- **Sizes**: `sm` (32px), `md` (40px), `lg` (56px)
- **Colors**: Deterministically assigned based on the contact name, ensuring the same person always gets the same color
- Falls back gracefully when no name is available

### BiometricGate (`biometric-gate.tsx`)

Wraps app content with biometric authentication enforcement. See the [Security](./security.md) page for full details on behavior and configuration.

### Button (`button.tsx`)

A themed, pressable button component.

- **Variants**: primary, secondary, outline, destructive, ghost
- **Sizes**: sm, md, lg
- Supports disabled state with reduced opacity
- Loading state with spinner replacement

### Card (`card.tsx`)

A container component for grouping related content into sections.

- Rounded corners, subtle shadow, and background color matching the current theme
- Used in settings screens, account details, and email metadata displays

### EmptyState (`empty-state.tsx`)

Placeholder displayed when a list has no items (e.g., empty inbox, no sent emails).

- Accepts an icon, title, and description
- Optional action button (e.g., "Compose Email" when inbox is empty)
- Centered layout with muted styling

### ErrorBoundary (`error-boundary.tsx`)

A React error boundary that catches unhandled exceptions in the component tree.

- Displays a user-friendly error message instead of a crash
- Includes a "Try Again" button that resets the error state
- Positioned in the root layout to catch errors from any screen

### Input (`input.tsx`)

A styled text input with label and validation support.

- Label text rendered above the input
- Error state with red border and error message below
- Supports all standard `TextInput` props (placeholder, secureTextEntry, keyboard type, etc.)

### Loading (`loading.tsx`)

A centered spinner component with an optional message.

- Used as a full-screen loading state during data fetches
- Accepts a custom message (e.g., "Loading emails...", "Connecting to server...")
- Uses the platform's native activity indicator

## API Client

The API client module in `lib/api/client.ts` provides factory functions for creating authenticated HTTP clients.

### Client Types

| Function | Auth Method | Use Case |
|---|---|---|
| `getActiveApiClient()` | API key from secure store | Normal app usage with the active account |
| `getApiClientForAccount(id)` | API key for specific account | Operations on a non-active account |
| `createTempApiClient(url, jwt)` | Bearer JWT | During OIDC flow before API key exists |
| `createPublicApiClient(url)` | None | Server discovery (unauthenticated) |

### Client Configuration

All clients share common configuration:

- **Timeout**: 30 seconds per request
- **HTTPS validation**: HTTPS is required for all connections. HTTP is only allowed when the server URL points to `localhost` (development mode).
- **Headers**: `Content-Type: application/json` and `Accept: application/json` are set by default.
- **Auth header format**: `Authorization: ApiKey {key}` for API key auth, `Authorization: Bearer {jwt}` for JWT auth.

### How it Works

`getActiveApiClient()` performs the following steps on each call:

1. Read the active account from the Zustand store
2. Retrieve the account's API key from secure storage via `getApiKey(accountId)`
3. Construct a fetch wrapper that injects the `Authorization` header
4. Return the configured client

The client is not cached -- a fresh one is created per call to ensure the correct account credentials are always used, especially after account switches.

## React Query Hooks

The hooks in `lib/api/hooks.ts` provide a reactive data layer on top of the API client. They follow the same patterns as the web frontend but are adapted for mobile with account-scoped query keys.

### Available Hooks

**Email queries:**

| Hook | Description |
|---|---|
| `useEmails(options)` | Paginated email list for the active account |
| `useInfiniteEmails(options)` | Infinite scroll email list (used in inbox) |
| `useSearchEmails(query)` | Search emails by subject, sender, or body |
| `useEmail(id)` | Single email detail by ID |
| `useSentEmails(options)` | Paginated sent email list |

**Email mutations:**

| Hook | Description |
|---|---|
| `useMarkEmailRead(id, read)` | Toggle read/unread status |
| `useDeleteEmail(id)` | Delete an email |

**Account and settings:**

| Hook | Description |
|---|---|
| `useProfile()` | Current user profile |
| `useSmtpCredentials()` | List API keys for the account |
| `useRevokeSmtpApiKey()` | Revoke an API key |
| `useRotateSmtpApiKey()` | Create a new key and revoke the old one |

### Query Key Scoping

All query keys include the active account ID as a prefix:

```
["emails", accountId, page, filters...]
["email", accountId, emailId]
["profile", accountId]
["smtp-credentials", accountId]
```

This scoping ensures that:

- Switching accounts does not display stale data from a different account
- Cache invalidation on account switch clears only the relevant queries
- Multiple accounts can maintain independent cache entries if needed
