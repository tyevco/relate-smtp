using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Infrastructure.Authentication;

/// <summary>
/// Base class for protocol authenticators (SMTP, POP3, IMAP).
/// Extracts the shared authentication logic: rate limiting, caching, BCrypt
/// verification, scope checking, and background task queueing.
/// </summary>
public abstract class ProtocolAuthenticator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IAuthenticationRateLimiter _rateLimiter;
    private static readonly MemoryCache AuthCache = new(new MemoryCacheOptions
    {
        SizeLimit = 10000,
        ExpirationScanFrequency = TimeSpan.FromMinutes(1)
    });
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    protected abstract string ProtocolName { get; }
    protected abstract string RequiredScope { get; }
    protected abstract ActivitySource ActivitySource { get; }
    protected abstract Counter<long> AuthAttemptsCounter { get; }
    protected abstract Counter<long> AuthFailuresCounter { get; }

    protected ProtocolAuthenticator(
        IServiceProvider serviceProvider,
        ILogger logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IAuthenticationRateLimiter rateLimiter)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _backgroundTaskQueue = backgroundTaskQueue;
        _rateLimiter = rateLimiter;
    }

    public async Task<(bool IsAuthenticated, Guid? UserId)> AuthenticateCoreAsync(
        string username,
        string password,
        string clientIp,
        CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity($"{ProtocolName}.auth.validate");
        activity?.SetTag($"{ProtocolName}.auth.user", username);

        AuthAttemptsCounter.Add(1);

        // Check rate limit before authentication
        var rateLimitResult = _rateLimiter.CheckRateLimit(clientIp, ProtocolName);
        if (rateLimitResult.IsBlocked)
        {
            _logger.LogWarning("{Protocol} authentication rate limited for {User} from {IP}", ProtocolName.ToUpperInvariant(), username, clientIp);
            activity?.SetTag($"{ProtocolName}.auth.rate_limited", true);
            activity?.SetTag($"{ProtocolName}.auth.success", false);
            AuthFailuresCounter.Add(1);
            return (false, null);
        }

        var normalizedEmail = username.ToLowerInvariant();
        var cacheKey = _rateLimiter.GenerateCacheKey(normalizedEmail, password);

        // Check cache (30-second TTL)
        if (AuthCache.TryGetValue(cacheKey, out CacheEntry? cached) && cached != null)
        {
            _logger.LogDebug("{Protocol} authentication cache hit for: {Email}", ProtocolName.ToUpperInvariant(), username);
            activity?.SetTag($"{ProtocolName}.auth.cache_hit", true);
            activity?.SetTag($"{ProtocolName}.auth.success", cached.IsAuthenticated);

            if (!cached.IsAuthenticated)
            {
                AuthFailuresCounter.Add(1);
            }

            _backgroundTaskQueue.QueueLastUsedAtUpdate(cached.KeyId, DateTimeOffset.UtcNow);
            return (cached.IsAuthenticated, cached.UserId);
        }

        activity?.SetTag($"{ProtocolName}.auth.cache_hit", false);

        // Create scoped container for DB access
        using var scope = _serviceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();

        var user = await userRepo.GetByEmailWithApiKeysAsync(normalizedEmail, ct);
        if (user == null)
        {
            _logger.LogWarning("{Protocol} authentication failed: User not found: {Email}", ProtocolName.ToUpperInvariant(), username);
            activity?.SetTag($"{ProtocolName}.auth.success", false);
            activity?.SetTag($"{ProtocolName}.auth.failure_reason", "user_not_found");
            AuthFailuresCounter.Add(1);
            _rateLimiter.RecordFailure(clientIp, ProtocolName);
            SetCache(cacheKey, false, null, Guid.Empty);
            return (false, null);
        }

        // Verify password against each active API key using BCrypt
        foreach (var apiKey in user.SmtpApiKeys)
        {
            if (BCrypt.Net.BCrypt.Verify(password, apiKey.KeyHash))
            {
                // Check if key has required scope
                if (!apiKeyRepo.HasScope(apiKey, RequiredScope))
                {
                    _logger.LogWarning("{Protocol} authentication failed for {Email} - API key {KeyName} lacks '{Scope}' scope",
                        ProtocolName.ToUpperInvariant(), username, apiKey.Name, RequiredScope);
                    activity?.SetTag($"{ProtocolName}.auth.success", false);
                    activity?.SetTag($"{ProtocolName}.auth.failure_reason", "missing_scope");
                    AuthFailuresCounter.Add(1);
                    _rateLimiter.RecordFailure(clientIp, ProtocolName);
                    SetCache(cacheKey, false, null, Guid.Empty);
                    return (false, null);
                }

                _logger.LogInformation("{Protocol} user authenticated: {Email} using key: {KeyName}",
                    ProtocolName.ToUpperInvariant(), username, apiKey.Name);
                activity?.SetTag($"{ProtocolName}.auth.success", true);
                activity?.SetTag($"{ProtocolName}.auth.key_name", apiKey.Name);
                _rateLimiter.RecordSuccess(clientIp, ProtocolName);
                SetCache(cacheKey, true, user.Id, apiKey.Id);
                _backgroundTaskQueue.QueueLastUsedAtUpdate(apiKey.Id, DateTimeOffset.UtcNow);
                return (true, user.Id);
            }
        }

        _logger.LogWarning("{Protocol} authentication failed: Invalid API key for user: {Email}", ProtocolName.ToUpperInvariant(), username);
        activity?.SetTag($"{ProtocolName}.auth.success", false);
        activity?.SetTag($"{ProtocolName}.auth.failure_reason", "invalid_key");
        AuthFailuresCounter.Add(1);
        _rateLimiter.RecordFailure(clientIp, ProtocolName);
        SetCache(cacheKey, false, null, Guid.Empty);
        return (false, null);
    }

    private static void SetCache(string cacheKey, bool authenticated, Guid? userId, Guid keyId)
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

        AuthCache.Set(cacheKey, entry, options);
    }

    private sealed record CacheEntry
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
        public Guid KeyId { get; init; }
    }
}
