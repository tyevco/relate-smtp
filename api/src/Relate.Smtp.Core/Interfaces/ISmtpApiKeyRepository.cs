using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Core.Interfaces;

public interface ISmtpApiKeyRepository
{
    Task<IReadOnlyList<SmtpApiKey>> GetActiveKeysForUserAsync(Guid userId, CancellationToken ct = default);
    Task<SmtpApiKey> CreateAsync(SmtpApiKey key, CancellationToken ct = default);
    Task RevokeAsync(Guid keyId, CancellationToken ct = default);
    Task UpdateLastUsedAsync(Guid keyId, DateTimeOffset lastUsed, CancellationToken ct = default);

    /// <summary>
    /// Find active API key by raw key value and verify it has required scope
    /// </summary>
    Task<SmtpApiKey?> GetByKeyWithScopeAsync(string rawKey, string requiredScope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find active API key by raw key value, returning all scopes without scope filtering.
    /// </summary>
    Task<SmtpApiKey?> GetByKeyAsync(string rawKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse scopes from JSON array string
    /// </summary>
    IReadOnlyList<string> ParseScopes(string scopesJson);

    /// <summary>
    /// Check if key has specific scope
    /// </summary>
    bool HasScope(SmtpApiKey key, string scope);
}
