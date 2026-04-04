# Message Handling

The SMTP server uses three custom handlers that plug into the SmtpServer library's processing pipeline: a message store for persistence, an authenticator for credential validation, and a mailbox filter for relay prevention.

## CustomMessageStore

**File:** `Handlers/CustomMessageStore.cs`

The message store is invoked after the SMTP `DATA` command completes. It receives the raw message bytes, parses them into structured data, and persists the result to PostgreSQL.

### Processing Pipeline

The `SaveAsync` method follows this sequence:

1. **Size validation** -- checks the raw buffer against `MaxMessageSizeBytes` (default 50 MB). Oversized messages are rejected with `SmtpReplyCode.SizeLimitExceeded`

2. **MIME parsing** -- uses MimeKit's `MimeMessage.LoadAsync()` to parse the raw bytes into a structured MIME message. This handles all MIME encoding, multipart boundaries, and nested message parts

3. **Entity construction** -- creates an `Email` entity with:
   - `MessageId` from the MIME `Message-Id` header (or a generated GUID if missing)
   - `FromAddress` and `FromDisplayName` from the `From` header
   - `Subject` (defaults to "(No Subject)" if missing)
   - `TextBody` and `HtmlBody` extracted from MIME parts
   - `ReceivedAt` set to UTC now
   - `SizeBytes` from the raw buffer length

4. **Threading** -- parses `In-Reply-To` and `References` headers:
   - If `In-Reply-To` contains a Message-Id, looks up the parent email in the database
   - If a parent is found, uses its `ThreadId` (or its own `Id` if it has no thread)
   - If no parent is found, `ThreadId` remains null (a new thread may be created later)

5. **Authenticated sender** -- if the session was authenticated (submission ports), extracts the user ID from the session context (`AuthenticatedUserId` property set by the authenticator)

6. **Recipients** -- iterates through `To`, `Cc`, and `Bcc` address lists:
   - For each address, looks up the user in the database by email
   - Creates an `EmailRecipient` record with `UserId` linked if the user exists, or `null` if not (the email is stored regardless, and will be linked when the user first logs in)

7. **Attachments** -- iterates through MIME attachments:
   - Each attachment is decoded from its MIME transfer encoding
   - Validated against `MaxAttachmentSizeBytes` (default 25 MB) -- oversized attachments cause the entire message to be rejected
   - Stored as binary blobs (`byte[]`) in the database
   - Filename is taken from the MIME part, or generated from the content type if missing (e.g., `attachment.pdf` for `application/pdf`)

8. **Persistence** -- saves the complete `Email` entity with all recipients and attachments in a single database operation

9. **Notification** -- sends real-time notifications to all recipient users via the notification service (HTTP to API, then SignalR broadcast + web push)

10. **Metrics** -- records `SmtpMessagesReceived`, `SmtpBytesReceived`, and `SmtpMessageProcessingDuration`

### Error Handling

The entire processing pipeline is wrapped in a try/catch. If any step fails (parse error, database error, etc.), the SMTP server returns `SmtpResponse.TransactionFailed` to the sending MTA, which will typically retry delivery later. The error is logged and recorded as an OpenTelemetry activity with error status.

### OpenTelemetry Activities

| Activity | Description |
|----------|-------------|
| `smtp.message.save` | Root activity for the entire save operation |
| `smtp.message.parse` | MIME parsing sub-activity (includes size tag) |
| `smtp.notify.users` | Notification dispatch (includes recipient count) |

Tags recorded on activities: `smtp.buffer_size`, `smtp.recipients_count`, `smtp.message_id`, `smtp.thread_id`, `smtp.has_attachments`.

### Filename Generation

When an attachment has no filename in the MIME headers, the store generates one based on the content type:

| MIME Type | Generated Filename |
|-----------|-------------------|
| `image/jpeg` | `attachment.jpg` |
| `image/png` | `attachment.png` |
| `application/pdf` | `attachment.pdf` |
| `application/zip` | `attachment.zip` |
| `text/plain` | `attachment.txt` |
| (unknown) | `attachment` |

## CustomUserAuthenticator

**File:** `Handlers/CustomUserAuthenticator.cs`

Handles SMTP `AUTH` commands by validating credentials against API keys stored in the database. Extends the shared `ProtocolAuthenticator` base class from Infrastructure.

### Authentication Flow

The `AuthenticateAsync` method:

1. Receives `user` (email address) and `password` (API key) from the SMTP AUTH command
2. Extracts the client IP from session properties for rate limiting
3. Delegates to `AuthenticateCoreAsync` (from `ProtocolAuthenticator` base class) which:
   - Checks the authentication rate limiter (prevents brute force)
   - Looks up the API key by its 12-character prefix
   - Verifies the full key against the BCrypt hash
   - Checks that the key has the `smtp` scope
   - Updates metrics counters (`SmtpAuthAttempts`, `SmtpAuthFailures`)
   - Queues a `LastUsedAt` update via the background task queue
4. On success, stores the authenticated user context in session properties:
   - `AuthenticatedUserId` -- the user's GUID (used later by `CustomMessageStore`)
   - `AuthenticatedEmail` -- the user's email address (normalized to lowercase)

### Rate Limiting

The `ProtocolAuthenticator` base class implements rate limiting via `IAuthenticationRateLimiter`. Failed authentication attempts from the same IP are tracked, and after a configurable number of failures, subsequent attempts from that IP are temporarily blocked. This prevents brute-force attacks against the SMTP AUTH mechanism.

### Shared Base Class

All protocol hosts (SMTP, POP3, IMAP) share the same `ProtocolAuthenticator` base class from Infrastructure. Each host overrides:

| Property | SMTP Value |
|----------|------------|
| `ProtocolName` | `"smtp"` |
| `RequiredScope` | `"smtp"` |
| `ActivitySource` | `TelemetryConfiguration.SmtpActivitySource` |
| `AuthAttemptsCounter` | `ProtocolMetrics.SmtpAuthAttempts` |
| `AuthFailuresCounter` | `ProtocolMetrics.SmtpAuthFailures` |

## MxMailboxFilter

**File:** `Handlers/MxMailboxFilter.cs`

The mailbox filter is invoked during the SMTP envelope negotiation phase -- specifically on the `MAIL FROM` and `RCPT TO` commands. Its primary purpose is preventing open relay on the MX endpoint (port 25).

### MAIL FROM Validation (`CanAcceptFromAsync`)

Controls which sender addresses are accepted:

| Scenario | Port | Authenticated | Result |
|----------|------|---------------|--------|
| MX disabled | Any | Yes | Accept |
| MX enabled | 587/465 | Yes | Accept |
| MX enabled | 25 | No | Accept (external MTAs send from any address) |
| MX enabled | Other | No | **Reject** (unauthenticated on non-MX port) |

On the MX port, all senders are accepted because external mail servers legitimately send on behalf of their users. The critical security check is on the recipient side.

### RCPT TO Validation (`CanDeliverToAsync`)

Controls which recipient addresses are accepted -- this is the open relay prevention mechanism:

| Scenario | Port | Authenticated | Result |
|----------|------|---------------|--------|
| MX disabled | Any | Yes | Accept |
| MX enabled | 587/465 | Yes | Accept (authenticated users can send to anyone) |
| MX enabled | 25 | No, hosted domain | Accept |
| MX enabled | 25 | No, foreign domain | **Reject** (relay attempt) |
| MX enabled | 25 | No, hosted domain, unknown user | **Reject** (if `ValidateRecipients` is true) |
| MX enabled | Other | No | **Reject** |

The key logic on port 25:

1. Extract the recipient's domain from the RCPT TO address
2. Check if the domain is in the configured `HostedDomains` set (case-insensitive)
3. If not a hosted domain: **reject** -- this is a relay attempt. Log a warning.
4. If `ValidateRecipients` is enabled, look up the recipient in the database
5. If the user doesn't exist: **reject** -- this prevents backscatter from accepting mail for nonexistent addresses

### Open Relay Prevention

An open relay is an SMTP server that accepts mail from anyone and delivers it to anyone. This is a serious security issue because spammers exploit open relays to send spam. The `MxMailboxFilter` prevents this by ensuring:

- **Port 25** (unauthenticated) only accepts mail **to** addresses at configured hosted domains
- **Ports 587/465** (authenticated) can send to any address, but require valid credentials
- Unauthenticated connections on submission ports are always rejected

The hosted domains check uses a `HashSet<string>` with `StringComparer.OrdinalIgnoreCase` for O(1) case-insensitive lookups.

### Factory Pattern

The `MxMailboxFilter` implements both `IMailboxFilter` and `IMailboxFilterFactory`. The factory pattern is required by the SmtpServer library to create filter instances per session. In this implementation, the factory returns `this` (the same instance) since the filter is stateless -- all configuration comes from the constructor parameters.

## HttpEmailNotificationService

**File:** `Services/HttpEmailNotificationService.cs`

After the `CustomMessageStore` saves an email, it notifies the API server so that connected clients receive real-time updates. This service makes an HTTP POST to the API's internal notification endpoint.

### Notification Payload

```json
POST /api/internal/notifications/new-email
Authorization: ApiKey <internal-api-key>

{
  "userIds": ["guid1", "guid2"],
  "email": {
    "id": "guid",
    "from": "sender@example.com",
    "fromDisplay": "Sender Name",
    "subject": "Email subject",
    "receivedAt": "2026-01-01T00:00:00Z",
    "hasAttachments": true
  }
}
```

The notification includes only the minimal email metadata needed for client notifications -- the full email content is not transmitted.

### Error Handling

Notification failures are **non-fatal**. If the API is unreachable or returns an error, the failure is logged as a warning but does not affect email storage. The email is already persisted in the database by the time the notification is sent, so users will see it on their next inbox refresh even if the real-time notification fails.

Only the `NotifyNewEmailAsync` and `NotifyMultipleUsersNewEmailAsync` methods are implemented -- the other `IEmailNotificationService` methods (`NotifyEmailUpdatedAsync`, `NotifyEmailDeletedAsync`, `NotifyUnreadCountChangedAsync`) are no-ops in the SMTP host since those events only occur through API operations.

## OpenTelemetry Summary

The SMTP server records the following traces and metrics:

### Activities (Traces)

| Name | Description |
|------|-------------|
| `smtp.message.save` | Complete message processing |
| `smtp.message.parse` | MIME message parsing |
| `smtp.filter.mail_from` | MAIL FROM validation |
| `smtp.filter.rcpt_to` | RCPT TO validation |
| `smtp.notify.users` | Notification dispatch |

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `smtp.messages.received` | Counter | Total messages received |
| `smtp.bytes.received` | Counter | Total bytes received |
| `smtp.message.processing_duration` | Histogram | Processing time in ms |
| `smtp.active_connections` | UpDownCounter | Current active connections |
| `smtp.auth.attempts` | Counter | Authentication attempts |
| `smtp.auth.failures` | Counter | Authentication failures |
