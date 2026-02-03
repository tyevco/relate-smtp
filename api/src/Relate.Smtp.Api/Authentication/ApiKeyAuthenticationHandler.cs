using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ISmtpApiKeyRepository _apiKeyRepository;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISmtpApiKeyRepository apiKeyRepository)
        : base(options, logger, encoder)
    {
        _apiKeyRepository = apiKeyRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Extract API key from Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        string? apiKey = null;
        var authValue = authHeader.ToString();

        if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            apiKey = authValue.Substring("Bearer ".Length).Trim();
        }
        else if (authValue.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            apiKey = authValue.Substring("ApiKey ".Length).Trim();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Try api:read scope first (most common for external API)
        var keyEntity = await _apiKeyRepository.GetByKeyWithScopeAsync(apiKey, "api:read", Context.RequestAborted);

        // If not found with api:read, try api:write
        if (keyEntity == null)
        {
            keyEntity = await _apiKeyRepository.GetByKeyWithScopeAsync(apiKey, "api:write", Context.RequestAborted);
        }

        // If not found with api:write, try app (first-party mobile/desktop clients)
        if (keyEntity == null)
        {
            keyEntity = await _apiKeyRepository.GetByKeyWithScopeAsync(apiKey, "app", Context.RequestAborted);
        }

        if (keyEntity == null)
        {
            return AuthenticateResult.Fail("Invalid API key or missing required scope");
        }

        // Update LastUsedAt (background task to avoid blocking)
        _ = Task.Run(async () =>
        {
            try
            {
                await _apiKeyRepository.UpdateLastUsedAsync(keyEntity.Id, DateTimeOffset.UtcNow, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update LastUsedAt for API key {KeyId}", keyEntity.Id);
            }
        });

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
