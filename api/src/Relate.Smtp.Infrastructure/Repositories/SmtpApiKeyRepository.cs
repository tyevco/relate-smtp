using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class SmtpApiKeyRepository : ISmtpApiKeyRepository
{
    private readonly AppDbContext _context;

    public SmtpApiKeyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SmtpApiKey>> GetActiveKeysForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.SmtpApiKeys
            .Where(k => k.UserId == userId && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<SmtpApiKey> CreateAsync(SmtpApiKey key, CancellationToken ct = default)
    {
        _context.SmtpApiKeys.Add(key);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return key;
    }

    public async Task RevokeAsync(Guid keyId, CancellationToken ct = default)
    {
        var key = await _context.SmtpApiKeys.FindAsync([keyId], ct).ConfigureAwait(false);
        if (key != null)
        {
            key.RevokedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task UpdateLastUsedAsync(Guid keyId, DateTimeOffset lastUsed, CancellationToken ct = default)
    {
        var key = await _context.SmtpApiKeys.FindAsync([keyId], ct).ConfigureAwait(false);
        if (key != null)
        {
            key.LastUsedAt = lastUsed;
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<SmtpApiKey?> GetByKeyWithScopeAsync(string rawKey, string requiredScope, CancellationToken cancellationToken = default)
    {
        // Extract prefix for efficient lookup (first 12 characters)
        var prefix = rawKey.Length >= 12 ? rawKey[..12] : rawKey;

        // O(1) lookup by prefix instead of loading all keys
        var candidates = await _context.SmtpApiKeys
            .Include(k => k.User)
            .Where(k => k.RevokedAt == null && k.KeyPrefix == prefix)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // BCrypt verify only the matching candidates
        foreach (var key in candidates)
        {
            if (BCrypt.Net.BCrypt.Verify(rawKey, key.KeyHash))
            {
                // Verify scope
                if (HasScope(key, requiredScope))
                {
                    return key;
                }
                return null; // Key found but lacks scope
            }
        }

        // Fallback for legacy keys without prefix (backward compatibility)
        if (candidates.Count == 0)
        {
            var legacyKeys = await _context.SmtpApiKeys
                .Include(k => k.User)
                .Where(k => k.RevokedAt == null && k.KeyPrefix == null)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var key in legacyKeys)
            {
                if (BCrypt.Net.BCrypt.Verify(rawKey, key.KeyHash))
                {
                    if (HasScope(key, requiredScope))
                    {
                        return key;
                    }
                    return null;
                }
            }
        }

        return null; // Key not found
    }

    public IReadOnlyList<string> ParseScopes(string scopesJson)
    {
        if (string.IsNullOrWhiteSpace(scopesJson) || scopesJson == "[]")
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(scopesJson) ?? new List<string>();
        }
#pragma warning disable CA1031 // Do not catch general exception types - Intentionally catching all JSON parsing errors for graceful fallback
        catch (JsonException)
#pragma warning restore CA1031
        {
            return Array.Empty<string>();
        }
    }

    public bool HasScope(SmtpApiKey key, string scope)
    {
        var scopes = ParseScopes(key.Scopes);
        return scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}
