# Shared Type Definitions

All TypeScript interfaces for API communication are defined in `packages/shared/src/api/types.ts` and exported via `@relate/shared/api/types`. These types are consumed by all three client applications (web, desktop, mobile) to ensure type-safe API interactions.

## Email Types

### `EmailListItem`

Represents a single email in a list view (inbox, search results, label view).

```typescript
interface EmailListItem {
  id: string                    // Unique email identifier (UUID)
  messageId: string             // RFC 2822 Message-ID header value
  fromAddress: string           // Sender email address
  fromDisplayName: string | null // Sender display name (may be absent)
  subject: string               // Email subject line
  receivedAt: string            // ISO 8601 timestamp of receipt
  sizeBytes: number             // Total email size in bytes
  isRead: boolean               // Whether the user has read this email
  attachmentCount: number       // Number of file attachments
}
```

### `EmailRecipient`

Represents a single recipient of an email.

```typescript
interface EmailRecipient {
  id: string                    // Recipient record ID
  address: string               // Recipient email address
  displayName: string | null    // Recipient display name
  type: 'To' | 'Cc' | 'Bcc'   // Recipient type
}
```

### `EmailAttachment`

Represents a file attached to an email.

```typescript
interface EmailAttachment {
  id: string           // Attachment record ID
  fileName: string     // Original file name
  contentType: string  // MIME type (e.g., 'application/pdf', 'image/png')
  sizeBytes: number    // File size in bytes
}
```

### `EmailDetail`

The full representation of an email, including body content, recipients, and attachments. Returned when fetching a single email by ID.

```typescript
interface EmailDetail {
  id: string
  messageId: string
  fromAddress: string
  fromDisplayName: string | null
  subject: string
  textBody: string | null        // Plain text body (may be absent)
  htmlBody: string | null        // HTML body (may be absent)
  receivedAt: string
  sizeBytes: number
  isRead: boolean
  recipients: EmailRecipient[]   // All recipients (To, Cc, Bcc)
  attachments: EmailAttachment[] // All file attachments
}
```

### `EmailListResponse`

Paginated response for email list endpoints.

```typescript
interface EmailListResponse {
  items: EmailListItem[]   // Emails for the current page
  totalCount: number       // Total emails matching the query
  unreadCount: number      // Unread emails in the result set
  page: number             // Current page number (1-based)
  pageSize: number         // Items per page
}
```

## User Types

### `Profile`

The authenticated user's profile information.

```typescript
interface Profile {
  id: string                              // User ID
  email: string                           // Primary email address
  displayName: string | null              // User's chosen display name
  createdAt: string                       // Account creation timestamp
  lastLoginAt: string | null              // Most recent login timestamp
  additionalAddresses: EmailAddress[]     // Extra email addresses the user has added
}
```

### `EmailAddress`

An additional email address associated with a user's account.

```typescript
interface EmailAddress {
  id: string           // Address record ID
  address: string      // The email address
  isVerified: boolean  // Whether verification has been completed
  addedAt: string      // When the address was added
}
```

## SMTP and Credential Types

### `SmtpConnectionInfo`

Server connection details for all three email protocols. Displayed on the SMTP settings page.

```typescript
interface SmtpConnectionInfo {
  smtpServer: string       // SMTP server hostname
  smtpPort: number         // SMTP submission port (typically 587)
  smtpSecurePort: number   // SMTP TLS port (typically 465)
  smtpEnabled: boolean     // Whether SMTP is active
  pop3Server: string       // POP3 server hostname
  pop3Port: number         // POP3 port (typically 110)
  pop3SecurePort: number   // POP3 TLS port (typically 995)
  pop3Enabled: boolean     // Whether POP3 is active
  imapServer: string       // IMAP server hostname
  imapPort: number         // IMAP port (typically 143)
  imapSecurePort: number   // IMAP TLS port (typically 993)
  imapEnabled: boolean     // Whether IMAP is active
  username: string         // The user's login username
  activeKeyCount: number   // Number of active API keys
}
```

### `SmtpApiKey`

An existing API key (the secret itself is not included -- it is only returned at creation time).

```typescript
interface SmtpApiKey {
  id: string                // Key record ID
  name: string              // User-assigned name for the key
  createdAt: string         // Creation timestamp
  lastUsedAt: string | null // Last time the key was used for authentication
  isActive: boolean         // Whether the key is active (not revoked)
  scopes: string[]          // Authorized scopes (e.g., ['smtp', 'pop3', 'api:read'])
}
```

### `CreateApiKeyRequest`

Request body for generating a new API key.

```typescript
interface CreateApiKeyRequest {
  name: string        // Human-readable name for the key
  scopes?: string[]   // Scopes to grant (defaults vary by server config)
}
```

### `CreatedApiKey`

Response when a new API key is successfully created. This is the only time the raw API key secret is returned.

```typescript
interface CreatedApiKey {
  id: string          // Key record ID
  name: string        // The name provided in the request
  apiKey: string      // The raw API key secret -- store it now, it cannot be retrieved later
  scopes: string[]    // Granted scopes
  createdAt: string   // Creation timestamp
}
```

### `SmtpCredentials`

Combined response containing connection info and all API keys.

```typescript
interface SmtpCredentials {
  connectionInfo: SmtpConnectionInfo  // Server connection details
  keys: SmtpApiKey[]                  // All API keys (active and revoked)
}
```

## Label Types

### `Label`

A user-defined label for organizing emails.

```typescript
interface Label {
  id: string         // Label record ID
  name: string       // Display name (e.g., "Important", "Work")
  color: string      // Hex color code (e.g., "#ef4444")
  sortOrder: number  // Display order (lower numbers appear first)
  createdAt: string  // Creation timestamp
}
```

### `CreateLabelRequest`

```typescript
interface CreateLabelRequest {
  name: string          // Label name
  color: string         // Hex color code
  sortOrder?: number    // Optional sort position
}
```

### `UpdateLabelRequest`

All fields are optional -- only provided fields are updated.

```typescript
interface UpdateLabelRequest {
  name?: string
  color?: string
  sortOrder?: number
}
```

## Filter Types

### `EmailFilter`

A rule that automatically processes incoming email based on conditions.

```typescript
interface EmailFilter {
  id: string
  name: string                      // Filter name
  isEnabled: boolean                // Whether the filter is active
  priority: number                  // Execution order (lower runs first)

  // Conditions (all optional -- omitted conditions are not checked)
  fromAddressContains?: string      // Match if sender address contains this string
  subjectContains?: string          // Match if subject contains this string
  bodyContains?: string             // Match if body contains this string
  hasAttachments?: boolean          // Match if email has/doesn't have attachments

  // Actions (applied when all specified conditions match)
  markAsRead: boolean               // Automatically mark matching emails as read
  assignLabelId?: string            // Apply this label to matching emails
  assignLabelName?: string          // Label name (included for display convenience)
  assignLabelColor?: string         // Label color (included for display convenience)
  delete: boolean                   // Automatically delete matching emails

  // Statistics
  createdAt: string
  lastAppliedAt?: string            // When the filter last matched an email
  timesApplied: number              // Total number of emails this filter has matched
}
```

### `CreateEmailFilterRequest`

```typescript
interface CreateEmailFilterRequest {
  name: string
  isEnabled?: boolean
  priority?: number
  fromAddressContains?: string
  subjectContains?: string
  bodyContains?: string
  hasAttachments?: boolean
  markAsRead?: boolean
  assignLabelId?: string
  delete?: boolean
}
```

### `UpdateEmailFilterRequest`

All fields are optional -- only provided fields are updated.

```typescript
interface UpdateEmailFilterRequest {
  name?: string
  isEnabled?: boolean
  priority?: number
  fromAddressContains?: string
  subjectContains?: string
  bodyContains?: string
  hasAttachments?: boolean
  markAsRead?: boolean
  assignLabelId?: string
  delete?: boolean
}
```

## Preference Types

### `UserPreference`

User-configurable application settings.

```typescript
interface UserPreference {
  id: string
  userId: string
  theme: 'light' | 'dark' | 'system'
  displayDensity: 'compact' | 'comfortable' | 'spacious'
  emailsPerPage: number                        // Items per page (default: 20)
  defaultSort: string                           // Sort field and direction
  showPreview: boolean                          // Show email preview text in list
  groupByDate: boolean                          // Group emails by date in the list
  desktopNotifications: boolean                 // Enable browser notifications
  emailDigest: boolean                          // Enable periodic email digest
  digestFrequency: 'daily' | 'weekly'           // Digest delivery frequency
  digestTime: string                            // Time of day for digest (e.g., "09:00")
  updatedAt: string                             // Last modification timestamp
}
```

### `UpdateUserPreferenceRequest`

All fields are optional -- only provided fields are updated.

```typescript
interface UpdateUserPreferenceRequest {
  theme?: 'light' | 'dark' | 'system'
  displayDensity?: 'compact' | 'comfortable' | 'spacious'
  emailsPerPage?: number
  defaultSort?: string
  showPreview?: boolean
  groupByDate?: boolean
  desktopNotifications?: boolean
  emailDigest?: boolean
  digestFrequency?: 'daily' | 'weekly'
  digestTime?: string
}
```

## Outbound Email Types

These types represent emails being composed and sent through the system.

### `OutboundEmailStatus`

```typescript
type OutboundEmailStatus = 'Draft' | 'Queued' | 'Sending' | 'Sent' | 'PartialFailure' | 'Failed'
```

Status progression: **Draft** (being composed) -> **Queued** (submitted for delivery) -> **Sending** (delivery in progress) -> **Sent** (all recipients succeeded) or **PartialFailure** (some recipients failed) or **Failed** (delivery completely failed).

### `OutboundRecipientStatus`

```typescript
type OutboundRecipientStatus = 'Pending' | 'Sending' | 'Sent' | 'Failed' | 'Deferred'
```

Per-recipient delivery status. **Deferred** means the remote server asked to try again later.

### `OutboundEmailListItem`

Summary of an outbound email for list views.

```typescript
interface OutboundEmailListItem {
  id: string
  fromAddress: string
  fromDisplayName: string | null
  subject: string
  status: OutboundEmailStatus
  createdAt: string
  queuedAt: string | null
  sentAt: string | null
  recipientCount: number
  attachmentCount: number
}
```

### `OutboundRecipient`

A recipient of an outbound email with per-recipient delivery tracking.

```typescript
interface OutboundRecipient {
  id: string
  address: string
  displayName: string | null
  type: 'To' | 'Cc' | 'Bcc'
  status: OutboundRecipientStatus
  statusMessage: string | null     // Error message or server response
  deliveredAt: string | null       // When delivery was confirmed
}
```

### `OutboundAttachment`

```typescript
interface OutboundAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
}
```

### `OutboundEmailDetail`

Full representation of an outbound email, including retry information.

```typescript
interface OutboundEmailDetail {
  id: string
  fromAddress: string
  fromDisplayName: string | null
  subject: string
  textBody: string | null
  htmlBody: string | null
  status: OutboundEmailStatus
  messageId: string | null         // RFC 2822 Message-ID (assigned when queued)
  inReplyTo: string | null         // Message-ID of the email being replied to
  originalEmailId: string | null   // ID of the original email (for replies/forwards)
  createdAt: string
  queuedAt: string | null
  sentAt: string | null
  retryCount: number               // Number of delivery attempts so far
  lastError: string | null         // Most recent delivery error message
  recipients: OutboundRecipient[]
  attachments: OutboundAttachment[]
}
```

### `OutboundEmailListResponse`

Paginated response for outbound email lists (drafts, outbox, sent).

```typescript
interface OutboundEmailListResponse {
  items: OutboundEmailListItem[]
  totalCount: number
  page: number
  pageSize: number
}
```

### `SendEmailRequest`

Request body for sending a new email directly (not from a draft).

```typescript
interface SendEmailRequest {
  fromAddress: string
  fromDisplayName?: string
  subject: string
  textBody?: string
  htmlBody?: string
  recipients: RecipientRequest[]
}
```

### `RecipientRequest`

A recipient specification used in send, draft, and forward requests.

```typescript
interface RecipientRequest {
  address: string
  displayName?: string
  type: 'To' | 'Cc' | 'Bcc'
}
```

### `CreateDraftRequest`

```typescript
interface CreateDraftRequest {
  fromAddress: string
  fromDisplayName?: string
  subject: string
  textBody?: string
  htmlBody?: string
  recipients: RecipientRequest[]
}
```

### `UpdateDraftRequest`

All fields are optional -- only provided fields are updated.

```typescript
interface UpdateDraftRequest {
  fromAddress?: string
  fromDisplayName?: string
  subject?: string
  textBody?: string
  htmlBody?: string
  recipients?: RecipientRequest[]
}
```

### `ReplyRequest`

```typescript
interface ReplyRequest {
  textBody?: string
  htmlBody?: string
  replyAll: boolean    // true = reply to all recipients, false = reply to sender only
}
```

### `ForwardRequest`

```typescript
interface ForwardRequest {
  textBody?: string
  htmlBody?: string
  recipients: RecipientRequest[]   // New recipients for the forwarded message
}
```
