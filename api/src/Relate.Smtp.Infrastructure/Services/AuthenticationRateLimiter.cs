using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Relate.Smtp.Infrastructure.Services;

/// <summary>
/// Configuration options for authentication rate limiting.
/// </summary>
public sealed class AuthenticationRateLimitOptions
{
    /// <summary>
    /// Maximum failed authentication attempts before lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Time window for tracking failed attempts.
    /// </summary>
    public TimeSpan LockoutWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Base delay after first failure (doubles with each subsequent failure).
    /// </summary>
    public TimeSpan BaseBackoffDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum backoff delay.
    /// </summary>
    public TimeSpan MaxBackoffDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Server-instance salt for HMAC cache key generation.
    /// Should be a random string unique to each deployment.
    /// </summary>
    public string AuthenticationSalt { get; set; } = string.Empty;
}

/// <summary>
/// Rate limiter result indicating whether authentication should proceed.
/// </summary>
public readonly record struct RateLimitResult(
    bool IsBlocked,
    int FailedAttempts,
    TimeSpan? RetryAfter);

/// <summary>
/// Interface for authentication rate limiting.
/// </summary>
public interface IAuthenticationRateLimiter
{
    /// <summary>
    /// Check if an IP address should be rate limited before authentication.
    /// </summary>
    RateLimitResult CheckRateLimit(string ipAddress, string protocol);

    /// <summary>
    /// Record a failed authentication attempt for an IP address.
    /// </summary>
    void RecordFailure(string ipAddress, string protocol);

    /// <summary>
    /// Record a successful authentication, resetting the failure counter.
    /// </summary>
    void RecordSuccess(string ipAddress, string protocol);

    /// <summary>
    /// Generate a secure HMAC-based cache key for authentication caching.
    /// </summary>
    string GenerateCacheKey(string email, string password);
}

/// <summary>
/// Provides rate limiting for protocol authentication to prevent brute force attacks.
/// Uses HMAC-SHA256 with a server-instance salt for secure cache key generation.
/// </summary>
public sealed class AuthenticationRateLimiter : IAuthenticationRateLimiter, IDisposable
{
    private readonly MemoryCache _rateLimitCache;
    private readonly AuthenticationRateLimitOptions _options;
    private readonly byte[] _hmacKey;
    private readonly ILogger<AuthenticationRateLimiter> _logger;
    private bool _disposed;

    public AuthenticationRateLimiter(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AuthenticationRateLimiter> logger)
    {
        _logger = logger;
        _rateLimitCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 50000,
            ExpirationScanFrequency = TimeSpan.FromMinutes(1)
        });

        _options = new AuthenticationRateLimitOptions();
        configuration.GetSection("Security:RateLimit").Bind(_options);

        // Get or generate HMAC key from salt
        var salt = configuration["Security:AuthenticationSalt"];
        if (string.IsNullOrEmpty(salt))
        {
            if (environment.IsProduction())
                throw new InvalidOperationException(
                    "Security:AuthenticationSalt must be configured in production. " +
                    "Generate with: openssl rand -base64 32");

            // Generate a random key for this instance if no salt configured
            // This is less ideal but still better than no salt
            _hmacKey = RandomNumberGenerator.GetBytes(32);
            logger.LogWarning(
                "No Security:AuthenticationSalt configured. Using random instance key. " +
                "Configure a persistent salt in appsettings.json for consistent cache behavior across restarts.");
        }
        else
        {
            // Derive a key from the salt using SHA256
            _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes(salt));
        }
    }

    public string GenerateCacheKey(string email, string password)
    {
        var input = $"{email.ToLowerInvariant()}:{password}";
        var inputBytes = Encoding.UTF8.GetBytes(input);

        // Use HMAC-SHA256 with the server salt for secure cache keys
        using var hmac = new HMACSHA256(_hmacKey);
        var hash = hmac.ComputeHash(inputBytes);
        return Convert.ToBase64String(hash);
    }

    public RateLimitResult CheckRateLimit(string ipAddress, string protocol)
    {
        var key = GetRateLimitKey(ipAddress, protocol);

        if (!_rateLimitCache.TryGetValue(key, out RateLimitEntry? entry) || entry == null)
        {
            return new RateLimitResult(false, 0, null);
        }

        // Check if locked out
        if (entry.FailedAttempts >= _options.MaxFailedAttempts)
        {
            var lockoutEnd = entry.FirstFailure.Add(_options.LockoutWindow);
            var now = DateTimeOffset.UtcNow;

            if (now < lockoutEnd)
            {
                var retryAfter = lockoutEnd - now;
                _logger.LogWarning(
                    "{Protocol} rate limit blocking {IP}: {FailedAttempts} failed attempts, retry after {RetryAfter}s",
                    protocol.ToUpperInvariant(), ipAddress, entry.FailedAttempts, retryAfter.TotalSeconds);

                return new RateLimitResult(true, entry.FailedAttempts, retryAfter);
            }

            // Lockout expired, reset
            _rateLimitCache.Remove(key);
            return new RateLimitResult(false, 0, null);
        }

        // Calculate backoff delay based on failure count
        if (entry.FailedAttempts > 0)
        {
            var delay = CalculateBackoff(entry.FailedAttempts);
            var canRetryAt = entry.LastFailure.Add(delay);
            var now = DateTimeOffset.UtcNow;

            if (now < canRetryAt)
            {
                return new RateLimitResult(true, entry.FailedAttempts, canRetryAt - now);
            }
        }

        return new RateLimitResult(false, entry.FailedAttempts, null);
    }

    public void RecordFailure(string ipAddress, string protocol)
    {
        var key = GetRateLimitKey(ipAddress, protocol);
        var now = DateTimeOffset.UtcNow;

        var entry = _rateLimitCache.GetOrCreate(key, e =>
        {
            e.SetSize(1);
            e.SetAbsoluteExpiration(_options.LockoutWindow);
            return new RateLimitEntry
            {
                FailedAttempts = 0,
                FirstFailure = now,
                LastFailure = now
            };
        })!;

        entry.FailedAttempts++;
        entry.LastFailure = now;

        // Update cache with new expiration
        var options = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(_options.LockoutWindow);
        _rateLimitCache.Set(key, entry, options);

        _logger.LogDebug(
            "{Protocol} auth failure recorded for {IP}: {FailedAttempts} total failures",
            protocol.ToUpperInvariant(), ipAddress, entry.FailedAttempts);
    }

    public void RecordSuccess(string ipAddress, string protocol)
    {
        var key = GetRateLimitKey(ipAddress, protocol);
        _rateLimitCache.Remove(key);
    }

    private static string GetRateLimitKey(string ipAddress, string protocol)
    {
        return $"ratelimit:{protocol}:{ipAddress}";
    }

    private TimeSpan CalculateBackoff(int failedAttempts)
    {
        // Exponential backoff: base * 2^(failures-1)
        var multiplier = Math.Pow(2, failedAttempts - 1);
        var delay = TimeSpan.FromTicks((long)(_options.BaseBackoffDelay.Ticks * multiplier));

        // Cap at max delay
        if (delay > _options.MaxBackoffDelay)
        {
            delay = _options.MaxBackoffDelay;
        }

        return delay;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _rateLimitCache.Dispose();
        _disposed = true;
    }

    private sealed class RateLimitEntry
    {
        public int FailedAttempts { get; set; }
        public DateTimeOffset FirstFailure { get; set; }
        public DateTimeOffset LastFailure { get; set; }
    }
}
