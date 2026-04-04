# Domain Entities

The Core layer defines 18 entities that model the email domain. This page documents each entity, its fields, relationships, and enumerations.

::: info Screenshot
![Screenshot: Entity Relationship Diagram](./screenshots/entity-relationship-diagram.png)

_TODO: Add screenshot of entity relationship diagram showing all entities and their connections_
:::

## User

The central identity entity representing a user account.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `OidcSubject` | `string` | OIDC subject claim (unique with issuer) |
| `OidcIssuer` | `string` | OIDC issuer URL |
| `Email` | `string` | Primary email address |
| `DisplayName` | `string?` | Human-readable display name |
| `CreatedAt` | `DateTimeOffset` | Account creation timestamp |
| `LastLoginAt` | `DateTimeOffset?` | Most recent login timestamp |

**Relationships:**
- `AdditionalAddresses` -- `ICollection<UserEmailAddress>` -- Additional email addresses
- `ReceivedEmails` -- `ICollection<EmailRecipient>` -- Emails received by this user
- `SmtpApiKeys` -- `ICollection<SmtpApiKey>` -- API keys for protocol authentication
- `Labels` -- `ICollection<Label>` -- Custom labels for organizing email
- `EmailLabels` -- `ICollection<EmailLabel>` -- Label assignments on emails
- `EmailFilters` -- `ICollection<EmailFilter>` -- Automated filter rules
- `Preference` -- `UserPreference?` -- User settings (one-to-one)

## Email

Represents an inbound email message stored in the system.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `MessageId` | `string` | RFC 5322 Message-ID header (globally unique identifier) |
| `FromAddress` | `string` | Sender's email address |
| `FromDisplayName` | `string?` | Sender's display name |
| `Subject` | `string` | Email subject line |
| `TextBody` | `string?` | Plain text body |
| `HtmlBody` | `string?` | HTML body |
| `ReceivedAt` | `DateTimeOffset` | When the server received the message |
| `SizeBytes` | `long` | Total message size in bytes |
| `InReplyTo` | `string?` | Message-ID of the message being replied to |
| `References` | `string?` | Space-separated list of ancestor Message-IDs for threading |
| `ThreadId` | `Guid?` | Thread identifier for conversation grouping |
| `SentByUserId` | `Guid?` | User who authenticated to send this email (if sent via SMTP) |

**Relationships:**
- `Recipients` -- `ICollection<EmailRecipient>` -- Delivery records per recipient
- `Attachments` -- `ICollection<EmailAttachment>` -- File attachments
- `EmailLabels` -- `ICollection<EmailLabel>` -- Labels applied to this email

**Computed property:**
- `HasAttachments` -- `bool` -- Returns `true` if the email has any attachments

## EmailRecipient

Tracks per-recipient delivery and read status. Each email has one `EmailRecipient` record for each person it was addressed to.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `EmailId` | `Guid` | Foreign key to Email |
| `Address` | `string` | Recipient's email address |
| `DisplayName` | `string?` | Recipient's display name |
| `Type` | `RecipientType` | Whether recipient was To, Cc, or Bcc |
| `UserId` | `Guid?` | Foreign key to User (null if recipient is not a local user) |
| `IsRead` | `bool` | Per-user read status (allows different users to track read state independently) |

**Relationships:**
- `Email` -- Navigation to the parent Email
- `User` -- Navigation to the User (if the recipient is a local account)

## EmailAttachment

Stores file attachments as binary data alongside the email.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `EmailId` | `Guid` | Foreign key to Email |
| `FileName` | `string` | Original filename |
| `ContentType` | `string` | MIME content type (e.g., `application/pdf`) |
| `SizeBytes` | `long` | Attachment size in bytes |
| `Content` | `byte[]` | Binary attachment data |

## EmailLabel

Junction table that links emails to labels, scoped per user. A single email can have different labels for different users.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `EmailId` | `Guid` | Foreign key to Email |
| `UserId` | `Guid` | Foreign key to User |
| `LabelId` | `Guid` | Foreign key to Label |
| `AssignedAt` | `DateTimeOffset` | When the label was applied |

## Label

User-defined labels for organizing emails, similar to Gmail labels or folder tags.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | Foreign key to the owning User |
| `Name` | `string` | Label display name |
| `Color` | `string` | Hex color code (default: `#3b82f6`, blue) |
| `SortOrder` | `int` | Display order for UI rendering |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp |

**Relationships:**
- `User` -- Navigation to the owning User
- `EmailLabels` -- Emails with this label applied

## EmailFilter

Automated rules for organizing incoming email. Filters are evaluated in priority order when new mail arrives.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | Foreign key to User |
| `Name` | `string` | Human-readable filter name |
| `IsEnabled` | `bool` | Whether the filter is active (default: `true`) |
| `Priority` | `int` | Evaluation order (lower number = higher priority) |

**Conditions** (all nullable -- null means "don't filter on this field"):

| Field | Type | Description |
|-------|------|-------------|
| `FromAddressContains` | `string?` | Match if sender address contains this substring |
| `SubjectContains` | `string?` | Match if subject contains this substring |
| `BodyContains` | `string?` | Match if body contains this substring |
| `HasAttachments` | `bool?` | Match if email has (or lacks) attachments |

**Actions** (applied when conditions match):

| Field | Type | Description |
|-------|------|-------------|
| `MarkAsRead` | `bool` | Automatically mark the email as read |
| `AssignLabelId` | `Guid?` | Apply this label to the email |
| `Delete` | `bool` | Delete the email automatically |

**Metadata:**

| Field | Type | Description |
|-------|------|-------------|
| `CreatedAt` | `DateTimeOffset` | When the filter was created |
| `LastAppliedAt` | `DateTimeOffset?` | Last time this filter matched an email |
| `TimesApplied` | `int` | Total number of times this filter has been applied |

## SmtpApiKey

API keys used for protocol authentication (SMTP, POP3, IMAP) and API access. Keys are stored as BCrypt hashes with a prefix for efficient lookup.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | Foreign key to the owning User |
| `Name` | `string` | Human-readable key name (e.g., "Thunderbird", "iPhone") |
| `KeyHash` | `string` | BCrypt hash of the raw API key |
| `KeyPrefix` | `string?` | First 12 characters of the raw key for O(1) database lookup |
| `Scopes` | `string` | JSON array of permission scopes (e.g., `["smtp","pop3"]`) |
| `CreatedAt` | `DateTimeOffset` | Key creation timestamp |
| `LastUsedAt` | `DateTimeOffset?` | Most recent usage timestamp |
| `RevokedAt` | `DateTimeOffset?` | Revocation timestamp (null if active) |

The `KeyPrefix` field enables a two-phase lookup: first find candidate keys by prefix (database index scan), then verify the full key against the BCrypt hash. This avoids iterating through all keys for a user.

## UserEmailAddress

Additional email addresses associated with a user account, beyond their primary address. These addresses must be verified before they become active.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | Foreign key to User |
| `Address` | `string` | The email address |
| `IsVerified` | `bool` | Whether the address has been verified |
| `VerificationToken` | `string?` | Token sent for email verification |
| `VerificationTokenExpiresAt` | `DateTimeOffset?` | Token expiration time |
| `AddedAt` | `DateTimeOffset` | When the address was added |

## OutboundEmail

Represents an email being composed and sent by a user.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | Foreign key to the sending User |
| `FromAddress` | `string` | Sender's email address |
| `FromDisplayName` | `string?` | Sender's display name |
| `Subject` | `string` | Email subject |
| `TextBody` | `string?` | Plain text body |
| `HtmlBody` | `string?` | HTML body |
| `Status` | `OutboundEmailStatus` | Current delivery status |
| `InReplyTo` | `string?` | Message-ID being replied to |
| `References` | `string?` | Threading references |
| `OriginalEmailId` | `Guid?` | Email being replied to or forwarded |
| `MessageId` | `string?` | Generated RFC 5322 Message-ID (set on send) |
| `CreatedAt` | `DateTimeOffset` | Draft creation timestamp |
| `QueuedAt` | `DateTimeOffset?` | When the email entered the delivery queue |
| `SentAt` | `DateTimeOffset?` | Successful delivery timestamp |
| `RetryCount` | `int` | Number of delivery attempts |
| `NextRetryAt` | `DateTimeOffset?` | Scheduled time for next retry |
| `LastError` | `string?` | Most recent error message |

**Relationships:**
- `User` -- Navigation to the sending User
- `Recipients` -- `ICollection<OutboundRecipient>` -- Per-recipient delivery status
- `Attachments` -- `ICollection<OutboundAttachment>` -- File attachments
- `DeliveryLogs` -- `ICollection<DeliveryLog>` -- Delivery attempt records

## OutboundRecipient

Tracks per-recipient delivery status for outbound emails.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `OutboundEmailId` | `Guid` | Foreign key to OutboundEmail |
| `Address` | `string` | Recipient's email address |
| `DisplayName` | `string?` | Recipient's display name |
| `Type` | `RecipientType` | To, Cc, or Bcc |
| `Status` | `OutboundRecipientStatus` | Delivery status for this recipient |
| `StatusMessage` | `string?` | SMTP response or error detail |
| `DeliveredAt` | `DateTimeOffset?` | Successful delivery timestamp |

## OutboundAttachment

File attachments for outbound emails.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `OutboundEmailId` | `Guid` | Foreign key to OutboundEmail |
| `FileName` | `string` | Original filename |
| `ContentType` | `string` | MIME content type |
| `SizeBytes` | `long` | Attachment size in bytes |
| `Content` | `byte[]` | Binary attachment data |

## UserPreference

Per-user settings for display and notification preferences.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Id` | `Guid` | | Primary key |
| `UserId` | `Guid` | | Foreign key to User |
| `Theme` | `string` | `"system"` | UI theme (`light`, `dark`, `system`) |
| `DisplayDensity` | `string` | `"comfortable"` | Layout density (`compact`, `comfortable`, `spacious`) |
| `EmailsPerPage` | `int` | `20` | Pagination size |
| `DefaultSort` | `string` | `"receivedAt-desc"` | Default sort order |
| `ShowPreview` | `bool` | `true` | Show email preview in list |
| `GroupByDate` | `bool` | `false` | Group emails by date |
| `DesktopNotifications` | `bool` | `false` | Enable desktop push notifications |
| `EmailDigest` | `bool` | `false` | Enable periodic email digest |
| `DigestFrequency` | `string` | `"daily"` | Digest frequency (`daily`, `weekly`) |
| `DigestTime` | `TimeOnly` | `09:00` | Time of day to send digest |
| `UpdatedAt` | `DateTimeOffset` | | Last modification timestamp |

## PushSubscription

Web push notification subscriptions using the VAPID protocol.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | Foreign key to User |
| `Endpoint` | `string` | Push service endpoint URL |
| `P256dhKey` | `string` | Client public key (P-256 Diffie-Hellman) |
| `AuthKey` | `string` | Authentication secret |
| `UserAgent` | `string` | Browser/client user agent string |
| `CreatedAt` | `DateTimeOffset` | Subscription creation timestamp |
| `LastUsedAt` | `DateTimeOffset?` | Last notification sent timestamp |

## DeliveryLog

Records each delivery attempt for outbound emails, providing an audit trail for troubleshooting.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `OutboundEmailId` | `Guid` | Foreign key to OutboundEmail |
| `RecipientId` | `Guid?` | Foreign key to OutboundRecipient |
| `RecipientAddress` | `string` | Recipient email address |
| `MxHost` | `string?` | MX host that was contacted |
| `SmtpStatusCode` | `int?` | SMTP response code (e.g., 250, 550) |
| `SmtpResponse` | `string?` | Full SMTP response text |
| `Success` | `bool` | Whether the attempt succeeded |
| `ErrorMessage` | `string?` | Error description on failure |
| `AttemptNumber` | `int` | Which retry attempt this was |
| `AttemptedAt` | `DateTimeOffset` | When the attempt was made |
| `Duration` | `TimeSpan?` | How long the attempt took |

## Enumerations

### RecipientType

```csharp
public enum RecipientType { To, Cc, Bcc }
```

### OutboundEmailStatus

```csharp
public enum OutboundEmailStatus
{
    Draft,           // Being composed
    Queued,          // Ready for delivery
    Sending,         // Delivery in progress
    Sent,            // All recipients delivered
    PartialFailure,  // Some recipients failed
    Failed           // All recipients failed, max retries exceeded
}
```

### OutboundRecipientStatus

```csharp
public enum OutboundRecipientStatus
{
    Pending,   // Not yet attempted
    Sending,   // Delivery in progress
    Sent,      // Successfully delivered
    Failed,    // Permanently failed
    Deferred   // Temporarily failed, will retry
}
```
