# Routing

The web application uses **TanStack Router** with file-based route generation. Routes are defined as `.tsx` files in `src/routes/`, and the Vite plugin `@tanstack/router-plugin/vite` automatically generates `routeTree.gen.ts` from them.

::: warning
Never edit `routeTree.gen.ts` manually. It is regenerated every time the dev server starts or a production build runs. Any manual changes will be overwritten.
:::

## Route File Conventions

TanStack Router uses file naming conventions to determine route structure:

- `__root.tsx` -- The root layout that wraps all routes
- `index.tsx` -- The `/` route (inbox)
- `compose.tsx` -- The `/compose` route
- `emails.$id.tsx` -- A dynamic route at `/emails/:id` (the `$` prefix denotes a path parameter)
- `login.tsx`, `callback.tsx` -- Authentication-related routes

## Route Reference

### `__root.tsx` -- Root Layout

The root layout renders on every page and provides the application shell:

- **Navigation header** with links to: Compose, Inbox, Drafts, Sent, Outbox, Profile, SMTP Settings, Preferences
- **Mobile-responsive drawer** that collapses the navigation into a hamburger menu on small screens
- **User profile display** showing the current user's name and email
- **Logout button** (when OIDC authentication is active)
- **Protected route wrapper** that redirects unauthenticated users to `/login`

All child routes render inside the root layout's `<Outlet />`.

### `index.tsx` -- Inbox

The main inbox view. This is the most feature-rich route in the application.

**Features:**

- **Email list** with sender name, subject line, time-ago timestamps, attachment count indicator, and unread/read visual state
- **Search** with a 300ms debounce to avoid excessive API calls while typing
- **Detail panel** that opens alongside the list when an email is selected (split-pane layout on larger screens)
- **SignalR real-time connection** -- connects on mount, sets up event listeners that invalidate TanStack Query caches, and updates unread count instantly via local state
- **Pagination** with page controls and configurable page size
- **Unread count badge** displayed in the navigation
- **Export dialog** for downloading emails as `.EML` (single) or `.MBOX` (batch) with optional date range filters

### `emails.$id.tsx` -- Email Detail (Full Page)

A full-page email detail view accessed at `/emails/:id`. Useful for deep-linking to a specific email or viewing on smaller screens where the split-pane inbox layout is not practical.

- Back button to return to the inbox
- Full email rendering with sanitized HTML body or plaintext fallback
- Recipient list (To, Cc, Bcc)
- Attachment list with download links
- Action buttons: Reply, Reply All, Forward, Delete

::: info Screenshot
**[Screenshot placeholder: Email detail]**

_TODO: Add screenshot of the full-page email detail view_
:::

### `compose.tsx` -- Compose Email

The email composition form for writing new messages, replies, and forwards.

**Query parameters:**

| Parameter | Purpose |
|-----------|---------|
| `replyTo` | Pre-populates the form as a reply to the specified email ID |
| `replyAll` | When combined with `replyTo`, includes all original recipients |
| `forwardFrom` | Pre-populates the form as a forward of the specified email ID |

**Features:**

- Dynamic **To**, **Cc**, and **Bcc** recipient fields -- users can add and remove recipients with email validation
- Subject line (auto-filled with `Re:` or `Fwd:` prefix for replies/forwards)
- Text body editor
- From address selector (when the user has multiple verified addresses)
- Send and Save as Draft actions

::: info Screenshot
**[Screenshot placeholder: Compose form]**

_TODO: Add screenshot of the compose form with recipient fields expanded_
:::

### `drafts.tsx` -- Drafts

Displays the list of saved draft emails.

- Each draft shows subject, recipients, and last-modified time
- **Send** button to immediately queue a draft for delivery
- **Delete** button to discard a draft
- Pagination controls

### `sent.tsx` -- Sent Mail

Displays emails that have been successfully sent.

- **From-address filter dropdown** to narrow results when the user has sent from multiple addresses
- Standard email list with subject, recipients, sent timestamp
- Pagination controls

### `outbox.tsx` -- Outbound Queue

Shows emails currently being processed for delivery.

- **Status badges** with color-coded indicators:
  - `Queued` -- waiting to be sent
  - `Sending` -- delivery in progress
- **Recipient count** per message
- Pagination controls
- Messages move out of the outbox view once they reach `Sent` or `Failed` status

### `filters.tsx` -- Email Filters

Manage rules that automatically process incoming email.

- **Filter list** showing each filter's name, priority, conditions, actions, and application count
- **Enable/disable toggle** per filter
- **Filter builder dialog** for creating and editing filters:
  - **Conditions**: from address contains, subject contains, body contains, has attachments
  - **Actions**: mark as read, assign a label, delete
  - Name and priority fields

### `preferences.tsx` -- User Preferences

User-configurable settings organized into sections:

- **Appearance**: theme (light/dark/system), display density (compact/comfortable/spacious)
- **Email list**: emails per page, default sort order, show preview text, group by date
- **Notifications**: desktop notification toggle, web push subscription, email digest (daily/weekly with configurable time)
- **Save button** that persists all changes via the preferences API

### `profile.tsx` -- User Profile

Manage account information:

- **Display name** editor
- **Primary email address** (read-only, set by OIDC provider)
- **Additional email addresses**: add new addresses, trigger verification emails, enter verification codes, remove addresses
- Additional addresses can be used as "from" addresses when composing mail

### `smtp-settings.tsx` -- SMTP Settings

Connection information and API key management for external client access:

- **Connection info cards** showing server hostnames, ports (plain and TLS), and enabled/disabled status for SMTP, POP3, and IMAP
- **API key generation**: name input, scope checkboxes (smtp, pop3, imap, api:read, api:write, app)
- **Key list** with creation date, last used date, active status, scopes, and a revoke button
- Newly created keys display the secret once -- it cannot be retrieved again

### `login.tsx` -- Login

A branded login card with an "Login with OIDC" button. If the user is already authenticated, they are redirected to the inbox.

### `callback.tsx` -- OIDC Callback

Handles the OIDC authorization code callback after the identity provider redirects back. Processes the authorization response, exchanges the code for tokens, and redirects to the inbox. Displays an error message if the authentication flow fails.

## Adding a New Route

To add a new route:

1. Create a new `.tsx` file in `src/routes/` following the naming conventions above
2. Export a `Route` object created with `createFileRoute`:

```tsx
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/my-route')({
  component: MyRouteComponent,
})

function MyRouteComponent() {
  return <div>My new page</div>
}
```

3. The Vite dev server will automatically detect the new file and regenerate `routeTree.gen.ts`
4. The route is now accessible at `/my-route`
