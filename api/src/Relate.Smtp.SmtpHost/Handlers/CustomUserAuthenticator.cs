using SmtpServer;
using SmtpServer.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;

namespace Relate.Smtp.SmtpHost.Handlers;

public class CustomUserAuthenticator : IUserAuthenticator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomUserAuthenticator> _logger;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IAuthenticationRateLimiter _rateLimiter;
    private static readonly MemoryCache _authCache = new(new MemoryCacheOptions
    {
        SizeLimit = 10000,
        ExpirationScanFrequency = TimeSpan.FromMinutes(1)
    });
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public CustomUserAuthenticator(
        IServiceProvider serviceProvider,
        ILogger<CustomUserAuthenticator> logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IAuthenticationRateLimiter rateLimiter)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _backgroundTaskQueue = backgroundTaskQueue;
        _rateLimiter = rateLimiter;
    }

    public async Task<bool> AuthenticateAsync(
        ISessionContext context,
        string user,
        string password,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryConfiguration.SmtpActivitySource.StartActivity("smtp.auth.validate");
        activity?.SetTag("smtp.auth.user", user);

        ProtocolMetrics.SmtpAuthAttempts.Add(1);

        // Get client IP for rate limiting
        var clientIp = context.Properties.TryGetValue("ClientIP", out var ip) ? ip?.ToString() ?? "unknown" : "unknown";

        // Check rate limit before authentication
        var rateLimitResult = _rateLimiter.CheckRateLimit(clientIp, "smtp");
        if (rateLimitResult.IsBlocked)
        {
            _logger.LogInformation("SMTP authentication rate limited for {User} from {IP}", user, clientIp);
            activity?.SetTag("smtp.auth.rate_limited", true);
            activity?.SetTag("smtp.auth.success", false);
            ProtocolMetrics.SmtpAuthFailures.Add(1);
            return false;
        }

        var normalizedEmail = user.ToLowerInvariant();
        var cacheKey = _rateLimiter.GenerateCacheKey(normalizedEmail, password);

        // Check cache first
        if (_authCache.TryGetValue(cacheKey, out CacheEntry? cached) && cached != null)
        {
            _logger.LogDebug("SMTP authentication cache hit for: {User}", user);
            activity?.SetTag("smtp.auth.cache_hit", true);
            activity?.SetTag("smtp.auth.success", cached.IsAuthenticated);

            if (!cached.IsAuthenticated)
            {
                ProtocolMetrics.SmtpAuthFailures.Add(1);
            }

            // Queue LastUsedAt update for background processing
            _backgroundTaskQueue.QueueLastUsedAtUpdate(cached.KeyId, DateTimeOffset.UtcNow);

            return cached.IsAuthenticated;
        }

        activity?.SetTag("smtp.auth.cache_hit", false);

        // Perform authentication
        using var authScope = _serviceProvider.CreateScope();
        var userRepository = authScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var apiKeyRepository = authScope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();

        var dbUser = await userRepository.GetByEmailWithApiKeysAsync(normalizedEmail, cancellationToken);

        if (dbUser == null)
        {
            _logger.LogWarning("SMTP authentication failed for: {User}", user);
            activity?.SetTag("smtp.auth.success", false);
            activity?.SetTag("smtp.auth.failure_reason", "user_not_found");
            ProtocolMetrics.SmtpAuthFailures.Add(1);
            _rateLimiter.RecordFailure(clientIp, "smtp");
            CacheResult(cacheKey, Guid.Empty, false);
            return false;
        }

        // Try each active API key
        foreach (var apiKey in dbUser.SmtpApiKeys)
        {
            if (BCrypt.Net.BCrypt.Verify(password, apiKey.KeyHash))
            {
                // Check if key has smtp scope
                if (!apiKeyRepository.HasScope(apiKey, "smtp"))
                {
                    _logger.LogWarning("SMTP authentication failed for {User} - API key {KeyName} lacks 'smtp' scope", user, apiKey.Name);
                    activity?.SetTag("smtp.auth.success", false);
                    activity?.SetTag("smtp.auth.failure_reason", "missing_scope");
                    ProtocolMetrics.SmtpAuthFailures.Add(1);
                    _rateLimiter.RecordFailure(clientIp, "smtp");
                    CacheResult(cacheKey, Guid.Empty, false);
                    return false;
                }

                _logger.LogInformation("SMTP user authenticated: {User} using key: {KeyName}", user, apiKey.Name);
                activity?.SetTag("smtp.auth.success", true);
                activity?.SetTag("smtp.auth.key_name", apiKey.Name);

                // Store authenticated user info in session context for later use
                context.Properties["AuthenticatedUserId"] = dbUser.Id;
                context.Properties["AuthenticatedEmail"] = normalizedEmail;

                // Queue last used timestamp update for background processing
                _backgroundTaskQueue.QueueLastUsedAtUpdate(apiKey.Id, DateTimeOffset.UtcNow);

                // Clear rate limit on success
                _rateLimiter.RecordSuccess(clientIp, "smtp");

                CacheResult(cacheKey, apiKey.Id, true);
                return true;
            }
        }

        _logger.LogWarning("SMTP authentication failed for: {User}", user);
        activity?.SetTag("smtp.auth.success", false);
        activity?.SetTag("smtp.auth.failure_reason", "invalid_key");
        ProtocolMetrics.SmtpAuthFailures.Add(1);
        _rateLimiter.RecordFailure(clientIp, "smtp");
        CacheResult(cacheKey, Guid.Empty, false);
        return false;
    }

    private static void CacheResult(string cacheKey, Guid keyId, bool isAuthenticated)
    {
        var entry = new CacheEntry
        {
            KeyId = keyId,
            IsAuthenticated = isAuthenticated
        };

        var options = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(CacheDuration);

        _authCache.Set(cacheKey, entry, options);
    }

    private sealed class CacheEntry
    {
        public Guid KeyId { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}
