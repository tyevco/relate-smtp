using System.Security.Claims;

namespace Relate.Smtp.Tests.Common.Helpers;

/// <summary>
/// Factory for creating ClaimsPrincipal instances for testing.
/// </summary>
public static class ClaimsPrincipalFactory
{
    /// <summary>
    /// Creates a ClaimsPrincipal with standard OIDC claims for testing.
    /// </summary>
    public static ClaimsPrincipal Create(
        string subject,
        string email,
        string? name = null,
        string issuer = "https://test-issuer.local",
        Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("sub", subject),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim("iss", issuer)
        };

        if (!string.IsNullOrEmpty(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
            claims.Add(new Claim("name", name));
        }

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal from a User entity.
    /// </summary>
    public static ClaimsPrincipal FromUser(Core.Entities.User user)
    {
        return Create(
            user.OidcSubject,
            user.Email,
            user.DisplayName,
            user.OidcIssuer);
    }
}
