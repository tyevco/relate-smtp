using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Telemetry;
using System.Collections.Concurrent;

namespace Relate.Smtp.Pop3Host.Handlers;

public class Pop3UserAuthenticator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Pop3UserAuthenticator> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _authCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public Pop3UserAuthenticator(IServiceProvider serviceProvider, ILogger<Pop3UserAuthenticator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<(bool IsAuthenticated, Guid? UserId)> AuthenticateAsync(
        string username,
        string password,
        CancellationToken ct)
    {
        using var activity = TelemetryConfiguration.Pop3ActivitySource.StartActivity("pop3.auth.validate");
        activity?.SetTag("pop3.auth.user", username);

        ProtocolMetrics.Pop3AuthAttempts.Add(1);

        var normalizedEmail = username.ToLowerInvariant();
        var cacheKey = $"{normalizedEmail}:{password}";

        // Check cache (30-second TTL)
        if (_authCache.TryGetValue(cacheKey, out var cached) &&
            DateTimeOffset.UtcNow < cached.ExpiresAt)
        {
            _logger.LogDebug("POP3 authentication cache hit for: {Email}", username);
            activity?.SetTag("pop3.auth.cache_hit", true);
            activity?.SetTag("pop3.auth.success", cached.IsAuthenticated);

            if (!cached.IsAuthenticated)
            {
                ProtocolMetrics.Pop3AuthFailures.Add(1);
            }

            _ = UpdateLastUsedAsync(cached.KeyId); // Background update
            return (cached.IsAuthenticated, cached.UserId);
        }

        activity?.SetTag("pop3.auth.cache_hit", false);

        // Create scoped container for DB access
        using var scope = _serviceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();

        var user = await userRepo.GetByEmailWithApiKeysAsync(normalizedEmail, ct);
        if (user == null)
        {
            _logger.LogWarning("POP3 authentication failed: User not found: {Email}", username);
            activity?.SetTag("pop3.auth.success", false);
            activity?.SetTag("pop3.auth.failure_reason", "user_not_found");
            ProtocolMetrics.Pop3AuthFailures.Add(1);
            CacheResult(cacheKey, false, null, Guid.Empty);
            return (false, null);
        }

        // Verify password against each active API key using BCrypt
        foreach (var apiKey in user.SmtpApiKeys)
        {
            if (BCrypt.Net.BCrypt.Verify(password, apiKey.KeyHash))
            {
                // Check if key has pop3 scope
                if (!apiKeyRepo.HasScope(apiKey, "pop3"))
                {
                    _logger.LogWarning("POP3 authentication failed for {Email} - API key {KeyName} lacks 'pop3' scope", username, apiKey.Name);
                    activity?.SetTag("pop3.auth.success", false);
                    activity?.SetTag("pop3.auth.failure_reason", "missing_scope");
                    ProtocolMetrics.Pop3AuthFailures.Add(1);
                    CacheResult(cacheKey, false, null, Guid.Empty);
                    return (false, null);
                }

                _logger.LogInformation("POP3 user authenticated: {Email} using key: {KeyName}", username, apiKey.Name);
                activity?.SetTag("pop3.auth.success", true);
                activity?.SetTag("pop3.auth.key_name", apiKey.Name);
                CacheResult(cacheKey, true, user.Id, apiKey.Id);
                _ = UpdateLastUsedAsync(apiKey.Id); // Background update
                return (true, user.Id);
            }
        }

        _logger.LogWarning("POP3 authentication failed: Invalid API key for user: {Email}", username);
        activity?.SetTag("pop3.auth.success", false);
        activity?.SetTag("pop3.auth.failure_reason", "invalid_key");
        ProtocolMetrics.Pop3AuthFailures.Add(1);
        CacheResult(cacheKey, false, null, Guid.Empty);
        return (false, null);
    }

    private void CacheResult(string key, bool authenticated, Guid? userId, Guid keyId)
    {
        _authCache[key] = new CacheEntry
        {
            IsAuthenticated = authenticated,
            UserId = userId,
            KeyId = keyId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration)
        };

        // Cleanup if cache grows too large
        if (_authCache.Count > 1000)
        {
            var expired = _authCache
                .Where(x => DateTimeOffset.UtcNow >= x.Value.ExpiresAt)
                .Select(x => x.Key)
                .ToList();
            foreach (var k in expired)
                _authCache.TryRemove(k, out _);
        }
    }

    private Task UpdateLastUsedAsync(Guid keyId)
    {
        // Background task to update LastUsedAt
        return Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var apiKeyRepo = scope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();
                await apiKeyRepo.UpdateLastUsedAsync(keyId, DateTimeOffset.UtcNow, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update LastUsedAt for API key: {KeyId}", keyId);
            }
        });
    }

    private record CacheEntry
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
        public Guid KeyId { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }
}
