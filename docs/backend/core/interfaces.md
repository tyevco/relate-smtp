# Repository Interfaces

The Core layer defines 9 repository interfaces that establish the contract for data access. The Infrastructure layer provides the concrete implementations using EF Core and PostgreSQL.

All interfaces follow the same conventions:
- Methods are asynchronous (returning `Task<T>`)
- All methods accept an optional `CancellationToken` for cooperative cancellation
- Collections are returned as `IReadOnlyList<T>` to signal that the caller should not modify them
- Paginated queries use `skip`/`take` parameters

## IEmailRepository

The largest repository, handling all inbound email operations including search, threading, bulk operations, and streaming.

```csharp
public interface IEmailRepository
{
    // Single email access
    Task<Email?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Email?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Email?> GetByIdWithUserAccessAsync(Guid emailId, Guid userId, CancellationToken ct = default);
    Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken ct = default);

    // Thread access
    Task<IReadOnlyList<Email>> GetByThreadIdAsync(Guid threadId, Guid userId, CancellationToken ct = default);

    // User inbox (paginated)
    Task<IReadOnlyList<Email>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken ct = default);

    // Search
    Task<IReadOnlyList<Email>> SearchByUserIdAsync(Guid userId, EmailSearchFilters filters,
        int skip, int take, CancellationToken ct = default);
    Task<int> GetSearchCountByUserIdAsync(Guid userId, EmailSearchFilters filters, CancellationToken ct = default);

    // CRUD
    Task<Email> AddAsync(Email email, CancellationToken ct = default);
    Task UpdateAsync(Email email, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // User linking (associates emails with a new user based on address)
    Task LinkEmailsToUserAsync(Guid userId, IEnumerable<string> emailAddresses, CancellationToken ct = default);

    // Sent mail
    Task<IReadOnlyList<Email>> GetSentByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetSentCountByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Email>> GetSentByUserIdAndFromAddressAsync(Guid userId, string fromAddress,
        int skip, int take, CancellationToken ct = default);
    Task<int> GetSentCountByUserIdAndFromAddressAsync(Guid userId, string fromAddress, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctSentFromAddressesByUserIdAsync(Guid userId, CancellationToken ct = default);

    // Bulk operations
    Task<int> BulkMarkReadAsync(Guid userId, IEnumerable<Guid> emailIds, bool isRead, CancellationToken ct = default);
    Task<int> BulkDeleteAsync(Guid userId, IEnumerable<Guid> emailIds, CancellationToken ct = default);

    // Streaming (for export)
    IAsyncEnumerable<Email> StreamByUserIdAsync(
        Guid userId, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null,
        CancellationToken ct = default);
}
```

**Key methods:**

- `GetByIdWithUserAccessAsync` -- Access-controlled fetch that verifies the user is either the sender or a recipient. Used by POP3 and IMAP to ensure users only see their own mail.
- `SearchByUserIdAsync` -- Full-text search across From, Subject, and Body fields with date range and attachment filters.
- `LinkEmailsToUserAsync` -- When a new user registers, this links existing emails addressed to their email addresses.
- `StreamByUserIdAsync` -- Returns an `IAsyncEnumerable` for memory-efficient processing of large mailboxes (used for MBOX export).
- `BulkMarkReadAsync` / `BulkDeleteAsync` -- Efficient batch operations that update multiple emails in a single database round-trip.

## IUserRepository

Manages user accounts and their additional email addresses.

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByOidcSubjectAsync(string issuer, string subject, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailWithApiKeysAsync(string email, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task UpdateLastLoginAsync(Guid userId, CancellationToken ct = default);

    // Additional email addresses
    Task<UserEmailAddress> AddEmailAddressAsync(UserEmailAddress address, CancellationToken ct = default);
    Task<UserEmailAddress?> GetEmailAddressByIdAsync(Guid addressId, CancellationToken ct = default);
    Task UpdateEmailAddressAsync(UserEmailAddress address, CancellationToken ct = default);
    Task RemoveEmailAddressAsync(Guid addressId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllEmailAddressesAsync(Guid userId, CancellationToken ct = default);
}
```

**Key methods:**

- `GetByOidcSubjectAsync` -- Looks up a user by their OIDC provider's subject claim and issuer. Used during OIDC authentication.
- `GetByEmailWithApiKeysAsync` -- Eagerly loads the user's active API keys. Used by protocol authenticators to verify passwords without additional queries.
- `GetAllEmailAddressesAsync` -- Returns all addresses (primary + additional) for a user. Used when linking incoming emails to the user.

## ISmtpApiKeyRepository

Manages API keys with BCrypt verification and scope handling.

```csharp
public interface ISmtpApiKeyRepository
{
    Task<IReadOnlyList<SmtpApiKey>> GetActiveKeysForUserAsync(Guid userId, CancellationToken ct = default);
    Task<SmtpApiKey> CreateAsync(SmtpApiKey key, CancellationToken ct = default);
    Task RevokeAsync(Guid keyId, CancellationToken ct = default);
    Task UpdateLastUsedAsync(Guid keyId, DateTimeOffset lastUsed, CancellationToken ct = default);

    // Key lookup and verification
    Task<SmtpApiKey?> GetByKeyWithScopeAsync(string rawKey, string requiredScope, CancellationToken ct = default);
    Task<SmtpApiKey?> GetByKeyAsync(string rawKey, CancellationToken ct = default);

    // Scope utilities
    IReadOnlyList<string> ParseScopes(string scopesJson);
    bool HasScope(SmtpApiKey key, string scope);
}
```

**Key methods:**

- `GetByKeyWithScopeAsync` -- Performs the two-phase lookup: finds candidates by key prefix (first 12 characters), then verifies the full key with BCrypt. Only returns keys that have the required scope.
- `GetByKeyAsync` -- Same lookup without scope filtering. Used by the API's `ApiKeyAuthenticationHandler`.
- `ParseScopes` -- Deserializes the JSON scope array stored on the key entity.
- `HasScope` -- Checks if a key includes a specific permission scope.

## ILabelRepository

Standard CRUD for user-defined labels.

```csharp
public interface ILabelRepository
{
    Task<IReadOnlyList<Label>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Label?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Label> AddAsync(Label label, CancellationToken ct = default);
    Task UpdateAsync(Label label, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

## IEmailLabelRepository

Manages the many-to-many relationship between emails and labels.

```csharp
public interface IEmailLabelRepository
{
    Task<IReadOnlyList<EmailLabel>> GetByEmailIdAsync(Guid emailId, CancellationToken ct = default);
    Task<IReadOnlyList<Email>> GetEmailsByLabelIdAsync(Guid userId, Guid labelId,
        int skip, int take, CancellationToken ct = default);
    Task<int> GetEmailCountByLabelIdAsync(Guid userId, Guid labelId, CancellationToken ct = default);
    Task<EmailLabel> AddAsync(EmailLabel emailLabel, CancellationToken ct = default);
    Task DeleteAsync(Guid emailId, Guid labelId, CancellationToken ct = default);
}
```

The `GetEmailsByLabelIdAsync` method is scoped by user ID, ensuring users only see their own labeled emails. It supports pagination for labels with many emails.

## IEmailFilterRepository

Manages automated filter rules.

```csharp
public interface IEmailFilterRepository
{
    Task<IReadOnlyList<EmailFilter>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<EmailFilter>> GetEnabledByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<EmailFilter?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmailFilter> AddAsync(EmailFilter filter, CancellationToken ct = default);
    Task UpdateAsync(EmailFilter filter, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

`GetEnabledByUserIdAsync` returns only active filters, ordered by priority. This is used when processing incoming email to apply filter rules.

## IUserPreferenceRepository

Simple CRUD for user preferences with upsert semantics.

```csharp
public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserPreference> UpsertAsync(UserPreference preference, CancellationToken ct = default);
}
```

The `UpsertAsync` method creates the preference record if it does not exist, or updates it if it does. This avoids the need for separate create/update endpoints.

## IPushSubscriptionRepository

Manages web push notification subscriptions.

```csharp
public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, Guid userId, CancellationToken ct = default);
    Task<PushSubscription> AddAsync(PushSubscription subscription, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateLastUsedAtAsync(Guid id, DateTimeOffset lastUsedAt, CancellationToken ct = default);
}
```

`GetByEndpointAsync` is scoped by user ID to prevent one user from managing another user's subscriptions. This is used to detect duplicate subscriptions.

## IOutboundEmailRepository

Manages outbound emails through their lifecycle from draft to sent.

```csharp
public interface IOutboundEmailRepository
{
    Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OutboundEmail?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    // User-facing queries
    Task<IReadOnlyList<OutboundEmail>> GetDraftsByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetDraftsCountByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<OutboundEmail>> GetOutboxByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetOutboxCountByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<OutboundEmail>> GetSentByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetSentCountByUserIdAsync(Guid userId, CancellationToken ct = default);

    // Delivery queue
    Task<IReadOnlyList<OutboundEmail>> GetQueuedForDeliveryAsync(int batchSize, CancellationToken ct = default);

    // CRUD
    Task AddAsync(OutboundEmail outboundEmail, CancellationToken ct = default);
    Task UpdateAsync(OutboundEmail outboundEmail, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Delivery logging
    Task AddDeliveryLogAsync(DeliveryLog log, CancellationToken ct = default);
}
```

**Key methods:**

- `GetQueuedForDeliveryAsync` -- Fetches outbound emails with status `Queued` and `NextRetryAt` in the past (or null). Used by the `DeliveryQueueProcessor` background service. Returns up to `batchSize` emails with their recipients and attachments eagerly loaded.
- `GetDraftsByUserIdAsync` / `GetOutboxByUserIdAsync` / `GetSentByUserIdAsync` -- Query emails by status for the compose, outbox, and sent mail views.
- `AddDeliveryLogAsync` -- Records a delivery attempt for audit and troubleshooting.
