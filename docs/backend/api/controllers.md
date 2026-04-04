# Controllers

The API has 12 controllers organized by domain. All controllers use attribute routing and return JSON responses. Unless noted otherwise, endpoints require authentication (either OIDC/JWT or API key).

## ConfigController

**Route:** `/config/config.json`
**Auth:** None (public)

Serves runtime configuration for the web frontend, allowing OIDC settings to be configured at deployment time rather than build time.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/config/config.json` | Returns OIDC configuration (authority, client ID, redirect URI, scope) |

The response is cached for 5 minutes (`ResponseCache(Duration = 300)`). The web frontend fetches this on startup to configure its OIDC client.

**Response example:**
```json
{
  "oidcAuthority": "https://auth.example.com",
  "oidcClientId": "relate-mail",
  "oidcRedirectUri": "https://mail.example.com/callback",
  "oidcScope": "openid profile email"
}
```

---

## DiscoveryController

**Route:** `/api/discovery`
**Auth:** None (`[AllowAnonymous]`)

Advertises server capabilities so mobile and desktop clients can auto-configure themselves during setup.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/discovery` | Returns server version, API version, enabled features, and OIDC status |

**Response example:**
```json
{
  "version": "1.0.0",
  "apiVersion": "v1",
  "oidcEnabled": true,
  "features": ["smtp", "pop3", "imap", "api-keys", "labels", "filters", "preferences", "oidc"]
}
```

Features reflect the server's runtime configuration. Protocols that are disabled via configuration (e.g., `Smtp:Enabled=false`) are omitted from the features list.

---

## EmailsController

**Route:** `/api/emails`
**Auth:** Required (JWT or API key)
**Rate limit:** `api` (100/min), `write` on mutating endpoints (30/min)

The primary inbox controller for authenticated users. Handles listing, searching, reading, updating, deleting, and exporting emails.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/emails` | List inbox emails (paginated, default 20/page, max 100) |
| GET | `/api/emails/search` | Full-text search with filters (query, date range, attachments, read status) |
| GET | `/api/emails/{id}` | Get email by ID with full details |
| PATCH | `/api/emails/{id}` | Update email (mark read/unread) |
| DELETE | `/api/emails/{id}` | Delete email |
| GET | `/api/emails/{id}/attachments/{attachmentId}` | Download attachment as file |
| GET | `/api/emails/{id}/export/eml` | Export single email as `.eml` (RFC 822) |
| GET | `/api/emails/export/mbox` | Stream export as MBOX format (50k email limit, 10min rate limit per user) |
| GET | `/api/emails/threads/{threadId}` | Get all emails in a thread |
| POST | `/api/emails/bulk/mark-read` | Bulk mark emails as read/unread |
| POST | `/api/emails/bulk/delete` | Bulk delete emails (returns deleted count) |
| GET | `/api/emails/sent` | List sent emails (optional `fromAddress` filter) |
| GET | `/api/emails/sent/addresses` | List distinct "from" addresses used in sent mail |

**Search query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `q` | string | Full-text search across subject, body, sender |
| `fromDate` | DateTimeOffset | Filter emails received after this date |
| `toDate` | DateTimeOffset | Filter emails received before this date |
| `hasAttachments` | bool | Filter by attachment presence |
| `isRead` | bool | Filter by read/unread status |
| `page` | int | Page number (default 1) |
| `pageSize` | int | Items per page (default 20, max 100) |

**MBOX export details:**

The MBOX export endpoint streams emails directly to the response body, avoiding large memory allocations. It enforces a hard limit of 50,000 emails and a per-user rate limit of one export every 10 minutes (tracked via an in-memory cache). Supports optional `fromDate` and `toDate` query parameters to narrow the export range.

**Attachment downloads** validate the MIME type against a safelist of known types. Unrecognized types are served as `application/octet-stream` with a `Content-Disposition: attachment` header to prevent browser execution.

---

## ExternalEmailsController

**Route:** `/api/external/emails`
**Auth:** API key only (`ApiKey` scheme), scope-gated

Provides the same inbox operations as `EmailsController` but scoped to API key authentication with explicit scope requirements. This is the endpoint third-party integrations use to access mailbox data.

| Method | Path | Scope | Description |
|--------|------|-------|-------------|
| GET | `/api/external/emails` | `api:read` | List inbox emails (paginated) |
| GET | `/api/external/emails/search` | `api:read` | Search with filters |
| GET | `/api/external/emails/sent` | `api:read` | List sent emails |
| GET | `/api/external/emails/{id}` | `api:read` | Get email by ID |
| PATCH | `/api/external/emails/{id}` | `api:write` | Mark read/unread |
| DELETE | `/api/external/emails/{id}` | `api:write` | Delete email |

The controller uses `[RequireScope]` attributes to enforce that the API key has the appropriate read or write scope.

---

## FiltersController

**Route:** `/api/filters`
**Auth:** Required

Manages email filter rules that automatically process incoming emails. Filters have conditions (from, subject, body, attachments) and actions (mark as read, assign label, delete).

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/filters` | List all filters for the authenticated user |
| POST | `/api/filters` | Create a new filter |
| PUT | `/api/filters/{id}` | Update an existing filter |
| DELETE | `/api/filters/{id}` | Delete a filter |
| POST | `/api/filters/{id}/test` | Test filter against recent emails (returns match count and IDs) |

**Create/Update request fields:**

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Filter name |
| `isEnabled` | bool | Whether the filter is active (default `true`) |
| `priority` | int | Execution order (lower = first, default `100`) |
| `fromAddressContains` | string? | Match sender address or display name |
| `subjectContains` | string? | Match subject line |
| `bodyContains` | string? | Match text or HTML body |
| `hasAttachments` | bool? | Match by attachment presence |
| `markAsRead` | bool | Action: auto-mark as read |
| `assignLabelId` | Guid? | Action: assign this label |
| `delete` | bool | Action: delete the email |

**Test endpoint:** The test endpoint (`POST /api/filters/{id}/test?limit=10`) runs the filter's conditions against the user's most recent emails (up to `limit`, max 100) and returns the count and IDs of matching emails without actually applying any actions.

---

## InternalNotificationsController

**Route:** `/api/internal/notifications`
**Auth:** API key with `internal` scope

Service-to-service endpoint used by the SMTP, POP3, and IMAP hosts to trigger real-time notifications in the API. This is not intended for external use.

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/internal/notifications/new-email` | Notify that new email(s) arrived for specific users |

**Request body:**
```json
{
  "userIds": ["guid1", "guid2"],
  "email": {
    "id": "guid",
    "from": "sender@example.com",
    "fromDisplay": "Sender Name",
    "subject": "Email subject",
    "receivedAt": "2026-01-01T00:00:00Z",
    "hasAttachments": false
  }
}
```

When called, the API broadcasts `NewEmail` events via SignalR and sends web push notifications to all specified users.

---

## LabelsController

**Route:** `/api/labels`
**Auth:** Required

Manages user-defined labels with colors and sort ordering, and handles assigning/removing labels on emails.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/labels` | List all labels for the authenticated user |
| POST | `/api/labels` | Create a new label (name, color hex, sort order) |
| PUT | `/api/labels/{id}` | Update label properties |
| DELETE | `/api/labels/{id}` | Delete a label |
| POST | `/api/labels/emails/{emailId}` | Add a label to an email (body: `{ "labelId": "guid" }`) |
| DELETE | `/api/labels/emails/{emailId}/{labelId}` | Remove a label from an email |
| GET | `/api/labels/{labelId}/emails` | List emails with a specific label (paginated) |

Label ownership is verified on every operation -- users can only manage their own labels and can only label emails they have access to.

---

## OutboundEmailsController

**Route:** `/api/outbound`
**Auth:** Required
**Rate limit:** `api` (100/min), `write` on mutating endpoints (30/min)

Handles email composition with a draft workflow, sending, replying, and forwarding. Outbound emails go through a status lifecycle: Draft -> Queued -> Sending -> Sent / Failed.

### Draft CRUD

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/outbound/drafts` | Create a new draft |
| GET | `/api/outbound/drafts` | List drafts (paginated) |
| GET | `/api/outbound/drafts/{id}` | Get draft by ID |
| PUT | `/api/outbound/drafts/{id}` | Update a draft (only if status is Draft) |
| DELETE | `/api/outbound/drafts/{id}` | Delete a draft (only if status is Draft) |

### Sending

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/outbound/send` | Compose and immediately queue an email for delivery |
| POST | `/api/outbound/drafts/{id}/send` | Send an existing draft (transitions Draft -> Queued) |

**Validation on send:**
- At least 1 recipient required, maximum 100
- Valid email address format for sender and all recipients
- Subject limited to 998 characters (RFC 2822)

The send endpoint generates an RFC-compliant `Message-Id` using MimeKit and queues the email for background delivery.

### Reply and Forward

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/outbound/reply/{emailId}` | Reply to an email (with `replyAll` option in body) |
| POST | `/api/outbound/forward/{emailId}` | Forward an email to new recipients (copies attachments) |

Reply automatically:
- Adds "Re:" prefix to subject (if not already present)
- Sets `In-Reply-To` and `References` headers for threading
- Addresses the reply to the original sender (and all recipients if `replyAll: true`, excluding the current user and Bcc -> Cc promotion)

Forward automatically:
- Adds "Fwd:" prefix to subject
- Sets `References` header
- Copies all attachments from the original email

### Outbox and Sent Mail

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/outbound/outbox` | List queued/sending emails (paginated) |
| GET | `/api/outbound/sent` | List sent emails (paginated) |
| GET | `/api/outbound/{id}` | Get any outbound email by ID |

---

## PreferencesController

**Route:** `/api/preferences`
**Auth:** Required

Manages per-user display and notification preferences with sensible defaults.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/preferences` | Get user preferences (returns defaults if none saved) |
| PUT | `/api/preferences` | Update preferences (upsert -- creates if none exist) |

**Preference fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `theme` | string | `"system"` | UI theme (system, light, dark) |
| `displayDensity` | string | `"comfortable"` | Row density (comfortable, compact) |
| `emailsPerPage` | int | `20` | Pagination size |
| `defaultSort` | string | `"receivedAt-desc"` | Default sort order |
| `showPreview` | bool | `true` | Show email preview text in list |
| `groupByDate` | bool | `false` | Group emails by date |
| `desktopNotifications` | bool | `false` | Enable desktop notifications |
| `emailDigest` | bool | `false` | Enable email digest |
| `digestFrequency` | string | `"daily"` | Digest frequency |
| `digestTime` | TimeOnly | `09:00` | Time to send digest |

---

## ProfileController

**Route:** `/api/profile`
**Auth:** Required

Manages user profile information and additional email addresses.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/profile` | Get user profile |
| PUT | `/api/profile` | Update display name |
| POST | `/api/profile/addresses` | Add an additional email address |
| DELETE | `/api/profile/addresses/{addressId}` | Remove an additional email address |
| POST | `/api/profile/addresses/{addressId}/send-verification` | Send verification email (501 -- not yet implemented) |
| POST | `/api/profile/addresses/{addressId}/verify` | Verify email address with code (501 -- not yet implemented) |

When an additional address is added, the system immediately links any existing unlinked emails for that address to the user. Verification tokens are 8-character alphanumeric codes (ambiguous characters removed) with a 24-hour expiry.

---

## PushSubscriptionsController

**Route:** `/api/push-subscriptions`
**Auth:** Required (except VAPID key endpoint)
**Rate limit:** `api` (100/min)

Manages Web Push notification subscriptions using the VAPID protocol.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/push-subscriptions/vapid-public-key` | None | Get VAPID public key for client subscription |
| POST | `/api/push-subscriptions` | Required | Subscribe to push notifications |
| DELETE | `/api/push-subscriptions/{id}` | Required | Unsubscribe from push notifications |

The VAPID public key endpoint is intentionally anonymous because clients need the key before they can authenticate. If push notifications are not configured on the server, it returns a 400 error.

**Subscribe request body:**
```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "p256dhKey": "base64-encoded-key",
  "authKey": "base64-encoded-auth"
}
```

Duplicate subscriptions (same endpoint + user) are detected and return the existing subscription.

---

## SmtpCredentialsController

**Route:** `/api/smtp-credentials`
**Auth:** Required
**Rate limit:** `auth` (10/min)

Manages API keys used for SMTP/POP3/IMAP authentication and third-party API access. Also returns server connection information.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/smtp-credentials` | Get connection info + list active API keys |
| POST | `/api/smtp-credentials` | Create a new API key (returns plaintext key once) |
| DELETE | `/api/smtp-credentials/{keyId}` | Revoke an API key |
| POST | `/api/smtp-credentials/{keyId}/rotate` | Rotate a key (creates new, revokes old, preserves scopes) |
| POST | `/api/smtp-credentials/mobile` | Create a mobile app API key (`app` scope, requires device name + platform) |

**GET response** includes full connection details for all protocols:
```json
{
  "connectionInfo": {
    "smtpServer": "mail.example.com",
    "smtpPort": 587,
    "smtpSecurePort": 465,
    "smtpEnabled": true,
    "pop3Server": "mail.example.com",
    "pop3Port": 110,
    "pop3SecurePort": 995,
    "pop3Enabled": true,
    "imapServer": "mail.example.com",
    "imapPort": 143,
    "imapSecurePort": 993,
    "imapEnabled": true,
    "username": "user@example.com",
    "activeKeyCount": 2
  },
  "apiKeys": [...]
}
```

**Key creation** generates a 32-byte random key (Base64-encoded), stores a BCrypt hash, and returns the plaintext key exactly once. The 12-character prefix is stored for efficient database lookup.

**Valid scopes:** `smtp`, `pop3`, `imap`, `api:read`, `api:write`, `app`, `internal`

**Mobile key creation** requires `deviceName` and `platform` (ios, android, windows, macos, web). The generated key automatically gets the `app` scope and a descriptive name like "Mobile App - iPhone 15 (ios)".

**Key rotation** atomically creates a new key with the same name and scopes, then revokes the old key. The new plaintext key is returned in the response.
