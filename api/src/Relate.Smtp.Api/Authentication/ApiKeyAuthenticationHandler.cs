using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string BearerScheme = "Bearer ";
    private const string ApiKeyScheme = "ApiKey ";

    private readonly ISmtpApiKeyRepository _apiKeyRepository;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISmtpApiKeyRepository apiKeyRepository,
        IBackgroundTaskQueue backgroundTaskQueue)
        : base(options, logger, encoder)
    {
        _apiKeyRepository = apiKeyRepository;
        _backgroundTaskQueue = backgroundTaskQueue;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Extract API key from Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var authValue = authHeader.ToString();
        if (string.IsNullOrWhiteSpace(authValue))
        {
            return AuthenticateResult.NoResult();
        }

        string? apiKey = null;
        if (authValue.StartsWith(BearerScheme, StringComparison.OrdinalIgnoreCase))
        {
            apiKey = authValue[BearerScheme.Length..].Trim();
        }
        else if (authValue.StartsWith(ApiKeyScheme, StringComparison.OrdinalIgnoreCase))
        {
            apiKey = authValue[ApiKeyScheme.Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Try api:read scope first (most common for external API)
        var keyEntity = await _apiKeyRepository.GetByKeyWithScopeAsync(apiKey, "api:read", Context.RequestAborted);

        // If not found with api:read, try api:write
        keyEntity ??= await _apiKeyRepository.GetByKeyWithScopeAsync(apiKey, "api:write", Context.RequestAborted);

        // If not found with api:write, try app (first-party mobile/desktop clients)
        keyEntity ??= await _apiKeyRepository.GetByKeyWithScopeAsync(apiKey, "app", Context.RequestAborted);

        if (keyEntity == null)
        {
            return AuthenticateResult.Fail("Invalid API key or missing required scope");
        }

        // Queue LastUsedAt update for background processing
        _backgroundTaskQueue.QueueLastUsedAtUpdate(keyEntity.Id, DateTimeOffset.UtcNow);

        // Create claims
        var scopes = _apiKeyRepository.ParseScopes(keyEntity.Scopes);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, keyEntity.UserId.ToString()),
            new Claim(ClaimTypes.Name, keyEntity.User.Email),
            new Claim("sub", keyEntity.UserId.ToString()), // OIDC-compatible
            new Claim("email", keyEntity.User.Email),
        };

        // Add scope claims
        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
