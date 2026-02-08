using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;
using System.Security.Cryptography;
using System.Text;

namespace Relate.Smtp.ImapHost.Handlers;

public class ImapUserAuthenticator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImapUserAuthenticator> _logger;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private static readonly MemoryCache _authCache = new(new MemoryCacheOptions
    {
        SizeLimit = 10000,
        ExpirationScanFrequency = TimeSpan.FromMinutes(1)
    });
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public ImapUserAuthenticator(
        IServiceProvider serviceProvider,
        ILogger<ImapUserAuthenticator> logger,
        IBackgroundTaskQueue backgroundTaskQueue)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _backgroundTaskQueue = backgroundTaskQueue;
    }

    private static string GenerateCacheKey(string email, string password)
    {
        var input = $"{email.ToLowerInvariant()}:{password}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    public async Task<(bool IsAuthenticated, Guid? UserId)> AuthenticateAsync(
        string username,
        string password,
        CancellationToken ct)
    {
        using var activity = TelemetryConfiguration.ImapActivitySource.StartActivity("imap.auth.validate");
        activity?.SetTag("imap.auth.user", username);

        ProtocolMetrics.ImapAuthAttempts.Add(1);

        var normalizedEmail = username.ToLowerInvariant();
        var cacheKey = GenerateCacheKey(normalizedEmail, password);

        // Check cache (30-second TTL)
        if (_authCache.TryGetValue(cacheKey, out CacheEntry? cached) && cached != null)
        {
            _logger.LogDebug("IMAP authentication cache hit for: {Email}", username);
            activity?.SetTag("imap.auth.cache_hit", true);
            activity?.SetTag("imap.auth.success", cached.IsAuthenticated);

            if (!cached.IsAuthenticated)
            {
                ProtocolMetrics.ImapAuthFailures.Add(1);
            }

            _backgroundTaskQueue.QueueLastUsedAtUpdate(cached.KeyId, DateTimeOffset.UtcNow);
            return (cached.IsAuthenticated, cached.UserId);
        }

        activity?.SetTag("imap.auth.cache_hit", false);

        // Create scoped container for DB access
        using var scope = _serviceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();

        var user = await userRepo.GetByEmailWithApiKeysAsync(normalizedEmail, ct);
        if (user == null)
        {
            _logger.LogWarning("IMAP authentication failed: User not found: {Email}", username);
            activity?.SetTag("imap.auth.success", false);
            activity?.SetTag("imap.auth.failure_reason", "user_not_found");
            ProtocolMetrics.ImapAuthFailures.Add(1);
            CacheResult(cacheKey, false, null, Guid.Empty);
            return (false, null);
        }

        // Verify password against each active API key using BCrypt
        foreach (var apiKey in user.SmtpApiKeys)
        {
            if (BCrypt.Net.BCrypt.Verify(password, apiKey.KeyHash))
            {
                // Check if key has imap scope
                if (!apiKeyRepo.HasScope(apiKey, "imap"))
                {
                    _logger.LogWarning("IMAP authentication failed for {Email} - API key {KeyName} lacks 'imap' scope", username, apiKey.Name);
                    activity?.SetTag("imap.auth.success", false);
                    activity?.SetTag("imap.auth.failure_reason", "missing_scope");
                    ProtocolMetrics.ImapAuthFailures.Add(1);
                    CacheResult(cacheKey, false, null, Guid.Empty);
                    return (false, null);
                }

                _logger.LogInformation("IMAP user authenticated: {Email} using key: {KeyName}", username, apiKey.Name);
                activity?.SetTag("imap.auth.success", true);
                activity?.SetTag("imap.auth.key_name", apiKey.Name);
                CacheResult(cacheKey, true, user.Id, apiKey.Id);
                _backgroundTaskQueue.QueueLastUsedAtUpdate(apiKey.Id, DateTimeOffset.UtcNow);
                return (true, user.Id);
            }
        }

        _logger.LogWarning("IMAP authentication failed: Invalid API key for user: {Email}", username);
        activity?.SetTag("imap.auth.success", false);
        activity?.SetTag("imap.auth.failure_reason", "invalid_key");
        ProtocolMetrics.ImapAuthFailures.Add(1);
        CacheResult(cacheKey, false, null, Guid.Empty);
        return (false, null);
    }

    private static void CacheResult(string cacheKey, bool authenticated, Guid? userId, Guid keyId)
    {
        var entry = new CacheEntry
        {
            IsAuthenticated = authenticated,
            UserId = userId,
            KeyId = keyId
        };

        var options = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(CacheDuration);

        _authCache.Set(cacheKey, entry, options);
    }

#pragma warning disable CA1852 // Type can be sealed - Records are implicitly sealed
    private record CacheEntry
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
        public Guid KeyId { get; init; }
    }
#pragma warning restore CA1852
}
