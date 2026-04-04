# Repository Implementations

The `Repositories/` directory contains 9 repository classes that implement the interfaces defined in the Core layer. All repositories are registered as **scoped** services (one instance per request/scope) and use `AppDbContext` for database access.

## EmailRepository

The largest and most complex repository, `EmailRepository` handles all inbound email operations.

### Pagination

`GetByUserIdAsync` returns emails for a user's inbox, ordered by `ReceivedAt` descending, with `skip`/`take` pagination. The query joins through `EmailRecipient` to find emails where the user is a recipient, and eagerly loads `Recipients` and `Attachments`.

### Access Control

`GetByIdWithUserAccessAsync` verifies that the requesting user is either the sender (`SentByUserId`) or a recipient before returning the email. This is used by the POP3 and IMAP handlers to ensure users can only access their own mail.

### Text Search

`SearchByUserIdAsync` applies `EmailSearchFilters` to build a dynamic query:

- `Query` is matched using case-insensitive `Contains()` (translates to SQL `ILIKE`) across `FromAddress`, `Subject`, `TextBody`, and `HtmlBody`.
- `FromDate` / `ToDate` filter on `ReceivedAt`.
- `HasAttachments` checks the count of related `EmailAttachment` records.
- `IsRead` joins to `EmailRecipient` for the user's read status.

### Thread Lookup

`GetByThreadIdAsync` returns all emails in a thread, ordered by `ReceivedAt`, for a specific user. This supports the conversation view in the UI.

### Bulk Operations

- `BulkMarkReadAsync` uses EF Core's `ExecuteUpdateAsync` to set `IsRead` on `EmailRecipient` records in a single database round-trip.
- `BulkDeleteAsync` uses `ExecuteDeleteAsync` to remove emails efficiently.

Both methods return the number of affected records.

### Streaming

`StreamByUserIdAsync` returns an `IAsyncEnumerable<Email>` that yields emails one at a time without loading them all into memory. This is used for MBOX export and large data operations. Optional date range filters can be applied.

### Sent Mail

`GetSentByUserIdAsync` queries emails where `SentByUserId` matches the user, providing a view of messages the user sent through the SMTP server. Additional methods support filtering by `FromAddress` and listing distinct sender addresses.

## UserRepository

`UserRepository` manages user accounts with both OIDC and email-based lookups.

### Key Queries

- `GetByOidcSubjectAsync` -- Unique lookup by OIDC issuer + subject. The database has a unique index on these columns.
- `GetByEmailWithApiKeysAsync` -- Looks up a user by email address and eagerly loads their active (non-revoked) API keys. This is the primary query used by protocol authentication.
- `GetAllEmailAddressesAsync` -- Returns the primary email plus all additional verified addresses for a user.

### Email Address Management

The repository provides CRUD operations for `UserEmailAddress` records, supporting the flow of adding, verifying, and removing additional email addresses from a user's profile.

## SmtpApiKeyRepository

`SmtpApiKeyRepository` implements the two-phase API key lookup:

### Prefix-Based Lookup

1. Extract the first 12 characters of the raw API key as the prefix.
2. Query the database for active (non-revoked) keys matching this prefix -- this is an indexed O(1) lookup.
3. For each matching key, verify the full raw key against the BCrypt hash stored in `KeyHash`.
4. Optionally check that the key has the required scope.

This approach avoids BCrypt verification against every key for every user, which would be prohibitively expensive.

### Scope Handling

- `ParseScopes` deserializes the JSON array stored in `SmtpApiKey.Scopes` (e.g., `["smtp","pop3"]`).
- `HasScope` checks if a key's scope list includes a specific permission.

## LabelRepository

Standard CRUD operations for labels, with `GetByUserIdAsync` returning labels ordered by `SortOrder` for consistent UI display.

## EmailLabelRepository

Manages the many-to-many relationship between emails and labels:

- `GetEmailsByLabelIdAsync` -- Returns paginated emails with a specific label, joining through the `EmailLabel` junction table. Eagerly loads recipients and attachments.
- `GetEmailCountByLabelIdAsync` -- Count query for pagination UI.
- `AddAsync` / `DeleteAsync` -- Add or remove a label from an email.

## EmailFilterRepository

- `GetByUserIdAsync` -- All filters for a user, ordered by priority.
- `GetEnabledByUserIdAsync` -- Only active filters, used during incoming email processing.

## UserPreferenceRepository

- `GetByUserIdAsync` -- Fetches the user's preferences.
- `UpsertAsync` -- Uses EF Core's change tracking to either insert a new preference record or update an existing one. Sets `UpdatedAt` on save.

## PushSubscriptionRepository

- `GetByEndpointAsync` -- Looks up a subscription by its push endpoint URL and user ID. Used to prevent duplicate subscriptions from the same browser.
- `UpdateLastUsedAtAsync` -- Updates the `LastUsedAt` timestamp when a notification is sent.

## OutboundEmailRepository

### Queue Query

`GetQueuedForDeliveryAsync` is the critical query used by the `DeliveryQueueProcessor`:

```sql
SELECT TOP(@batchSize) *
FROM OutboundEmails
WHERE Status = 'Queued'
  AND (NextRetryAt IS NULL OR NextRetryAt <= @now)
ORDER BY QueuedAt
```

This fetches the oldest queued emails that are due for delivery (either never attempted or past their retry delay). Recipients and attachments are eagerly loaded to minimize database round-trips during delivery.

### Status-Based Views

- `GetDraftsByUserIdAsync` -- Emails with status `Draft`
- `GetOutboxByUserIdAsync` -- Emails with status `Queued` or `Sending`
- `GetSentByUserIdAsync` -- Emails with status `Sent`

Each includes a corresponding count method for pagination.

### Delivery Logging

`AddDeliveryLogAsync` appends a `DeliveryLog` record for each delivery attempt, creating an audit trail of MX hosts contacted, SMTP response codes, and timing information.
