# Services

The API project contains several domain services that encapsulate business logic beyond simple CRUD operations. These are registered as scoped services in the dependency injection container.

## EmailFilterService

**File:** `Services/EmailFilterService.cs`

Evaluates email filter conditions and applies actions when emails match. Filters are processed automatically when new emails arrive and can also be tested manually via the API.

### Condition Evaluation

The `EmailMatchesFilter` method evaluates all conditions as a logical AND -- every non-null condition must match for the filter to trigger. Conditions are evaluated in memory against the email entity:

| Condition | Match Logic |
|-----------|-------------|
| `FromAddressContains` | Case-insensitive substring match against `FromAddress` or `FromDisplayName` |
| `SubjectContains` | Case-insensitive substring match against `Subject` |
| `BodyContains` | Case-insensitive substring match against `TextBody` or `HtmlBody` |
| `HasAttachments` | Boolean match against whether the email has any attachments |

If a condition field is null or empty, it is skipped (treated as "any value matches").

### Action Execution

When an email matches a filter, the following actions are applied in order:

1. **Mark as Read** -- if `filter.MarkAsRead` is true, sets `recipient.IsRead = true` for the current user's recipient record
2. **Assign Label** -- if `filter.AssignLabelId` has a value, creates an `EmailLabel` association. If the label is already assigned (duplicate), the error is caught and logged at debug level
3. **Delete** -- if `filter.Delete` is true, deletes the email entirely

### Filter Processing

The `ApplyFiltersToEmailAsync` method processes filters for a specific user and email:

1. Fetches all enabled filters for the user, ordered by priority
2. Iterates through each filter, checking conditions
3. For each match, applies actions and updates filter statistics:
   - `LastAppliedAt` is set to the current time
   - `TimesApplied` is incremented
4. Multiple filters can match the same email -- they are applied in sequence

Note that if an early filter deletes the email, subsequent filters will still be evaluated but their actions may fail silently since the email no longer exists.

## SmtpCredentialService

**File:** `Services/SmtpCredentialService.cs`

Handles API key generation, hashing, and verification. This is a stateless utility service with no database dependencies.

### Key Generation

```csharp
public string GenerateApiKey()
```

Generates a cryptographically secure 32-byte (256-bit) random key and returns it as a Base64-encoded string. The resulting key is approximately 44 characters long.

### Key Hashing

```csharp
public string HashPassword(string password)
```

Hashes the API key using BCrypt with a work factor of **11**. This provides strong protection for stored keys -- even if the database is compromised, recovering the original keys requires significant computational effort.

### Prefix Extraction

```csharp
public string ExtractKeyPrefix(string apiKey)
```

Extracts the first **12 characters** of the API key as a prefix. This prefix is stored in plaintext alongside the hash and is used for efficient database lookups. When a client presents an API key:

1. The first 12 characters are extracted
2. A database query finds the key record by prefix (indexed column)
3. The full key is verified against the BCrypt hash

This avoids scanning all keys in the database for every authentication request.

### Password Verification

```csharp
public bool VerifyPassword(string password, string hash)
```

Delegates to `BCrypt.Net.BCrypt.Verify()` to check a plaintext key against its stored hash.

## UserProvisioningService

**File:** `Services/UserProvisioningService.cs`

JIT (just-in-time) user provisioning from authentication claims. Called on every authenticated request to ensure the user exists in the database.

### Provisioning Logic

The `GetOrCreateUserAsync` method follows this decision tree:

1. **Extract subject** from claims (`NameIdentifier` or `sub`)
2. **API key path** -- if subject is a valid GUID:
   - Look up user by ID
   - If found: update `LastLoginAt`, link emails (primary + additional addresses), return
3. **OIDC path** -- extract issuer claim:
   - Validate issuer matches `Oidc:Authority` (if configured)
   - Look up by OIDC subject + issuer
   - If found: update `LastLoginAt`, link emails, return
4. **Create new user** -- extract email and display name from claims:
   - Create `User` entity with OIDC subject/issuer
   - Link existing unlinked emails for this address

### Email Linking

A critical function of provisioning is linking pre-existing emails to the user. Consider this scenario:

1. Someone sends an email to `alice@example.com`
2. The SMTP server stores it, but `alice` has never logged in -- the email has no user link
3. Alice logs in for the first time via OIDC
4. `UserProvisioningService` creates her user record and calls `LinkEmailsToUserAsync`
5. The stored email is now linked to Alice's account and appears in her inbox

This linking also runs on subsequent logins to catch emails that arrived between sessions. Both the primary email and all additional addresses are included in the linking query.

## SignalREmailNotificationService

**File:** `Services/EmailNotificationService.cs`

Bridges domain email events to the SignalR hub for real-time client notifications. Also triggers web push notifications for new emails.

### Events Dispatched

| Method | SignalR Event | Also Triggers |
|--------|---------------|---------------|
| `NotifyNewEmailAsync` | `NewEmail` | Web push notification |
| `NotifyEmailUpdatedAsync` | `EmailUpdated` | -- |
| `NotifyEmailDeletedAsync` | `EmailDeleted` | -- |
| `NotifyUnreadCountChangedAsync` | `UnreadCountChanged` | -- |
| `NotifyMultipleUsersNewEmailAsync` | `NewEmail` (to each user) | Web push (to each user) |

The multi-user notification method dispatches SignalR and push notifications in parallel using `Task.WhenAll`, minimizing latency when an email has multiple recipients.

All events are sent to the user's SignalR group (`user_{userId}`), so only the relevant user's connected clients receive the notification.

## PushNotificationService

**File:** `Services/PushNotificationService.cs`

VAPID-based Web Push notification service for delivering notifications when the browser tab is not active or the app is in the background.

### Configuration

Push notifications require three VAPID settings:

| Setting | Description |
|---------|-------------|
| `Push:VapidSubject` | Contact URL (mailto: or https:) |
| `Push:VapidPublicKey` | VAPID public key (Base64 URL-encoded) |
| `Push:VapidPrivateKey` | VAPID private key (Base64 URL-encoded) |

If any of these are not configured, push notifications are silently skipped with a warning log.

### Notification Delivery

When `SendNewEmailNotificationAsync` is called:

1. Fetches all push subscriptions for the user from the database
2. For each subscription, sends a JSON payload via the Web Push protocol:
   ```json
   {
     "title": "New Email",
     "body": "From: Sender Name\nSubject: Email subject",
     "icon": "/icon.png",
     "badge": "/badge.png",
     "data": { "emailId": "guid", "url": "/" }
   }
   ```
3. Updates `LastUsedAt` on successful delivery
4. Handles subscription expiry: if the push service returns `410 Gone`, the subscription is automatically deleted from the database
5. Other errors are logged but do not prevent delivery to remaining subscriptions

### Error Handling

Each subscription is delivered independently. A failure to deliver to one subscription does not affect others. This is important because users may have stale subscriptions from old browsers or devices, and those failures should not block notifications to active devices.
