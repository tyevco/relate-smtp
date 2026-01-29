using SmtpServer;
using SmtpServer.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Relate.Smtp.Core.Interfaces;
using System.Collections.Concurrent;

namespace Relate.Smtp.SmtpHost.Handlers;

public class CustomUserAuthenticator : IUserAuthenticator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomUserAuthenticator> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _authCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public CustomUserAuthenticator(IServiceProvider serviceProvider, ILogger<CustomUserAuthenticator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<bool> AuthenticateAsync(
        ISessionContext context,
        string user,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = user.ToLowerInvariant();
        var cacheKey = $"{normalizedEmail}:{password}";

        // Check cache first
        if (_authCache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTimeOffset.UtcNow < cached.ExpiresAt)
            {
                _logger.LogDebug("SMTP authentication cache hit for: {User}", user);

                // Update LastUsedAt in background
                _ = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var apiKeyRepo = scope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();
                    await apiKeyRepo.UpdateLastUsedAsync(cached.KeyId, DateTimeOffset.UtcNow, CancellationToken.None);
                }, CancellationToken.None);

                return cached.IsAuthenticated;
            }

            _authCache.TryRemove(cacheKey, out _);
        }

        // Perform authentication
        using var authScope = _serviceProvider.CreateScope();
        var userRepository = authScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var apiKeyRepository = authScope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();

        var dbUser = await userRepository.GetByEmailWithApiKeysAsync(normalizedEmail, cancellationToken);

        if (dbUser == null)
        {
            _logger.LogWarning("SMTP authentication failed: User not found: {User}", user);
            CacheResult(cacheKey, Guid.Empty, false);
            return false;
        }

        // Try each active API key
        foreach (var apiKey in dbUser.SmtpApiKeys)
        {
            if (BCrypt.Net.BCrypt.Verify(password, apiKey.KeyHash))
            {
                _logger.LogInformation("SMTP user authenticated: {User} using key: {KeyName}", user, apiKey.Name);

                // Store authenticated user info in session context for later use
                context.Properties["AuthenticatedUserId"] = dbUser.Id;
                context.Properties["AuthenticatedEmail"] = normalizedEmail;

                // Update last used timestamp
                await apiKeyRepository.UpdateLastUsedAsync(apiKey.Id, DateTimeOffset.UtcNow, cancellationToken);

                CacheResult(cacheKey, apiKey.Id, true);
                return true;
            }
        }

        _logger.LogWarning("SMTP authentication failed: Invalid API key for user: {User}", user);
        CacheResult(cacheKey, Guid.Empty, false);
        return false;
    }

    private void CacheResult(string cacheKey, Guid keyId, bool isAuthenticated)
    {
        var entry = new CacheEntry
        {
            KeyId = keyId,
            IsAuthenticated = isAuthenticated,
            ExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration)
        };

        _authCache.TryAdd(cacheKey, entry);

        // Clean up expired entries periodically
        if (_authCache.Count > 1000)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _authCache
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _authCache.TryRemove(key, out _);
            }
        }
    }

    private class CacheEntry
    {
        public Guid KeyId { get; set; }
        public bool IsAuthenticated { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
