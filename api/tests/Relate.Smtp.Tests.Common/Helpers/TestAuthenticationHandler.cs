using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relate.Smtp.Tests.Common.Helpers;

/// <summary>
/// Authentication handler for testing that accepts user identity via headers.
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";
    public const string UserIdHeader = "X-Test-UserId";
    public const string UserEmailHeader = "X-Test-UserEmail";
    public const string UserNameHeader = "X-Test-UserName";
    public const string IssuerHeader = "X-Test-Issuer";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for test user headers
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues) ||
            !Request.Headers.TryGetValue(UserEmailHeader, out var emailValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test authentication headers"));
        }

        var userId = userIdValues.FirstOrDefault();
        var email = emailValues.FirstOrDefault();
        var name = Request.Headers.TryGetValue(UserNameHeader, out var nameValues)
            ? nameValues.FirstOrDefault() ?? email
            : email;
        var issuer = Request.Headers.TryGetValue(IssuerHeader, out var issuerValues)
            ? issuerValues.FirstOrDefault() ?? "https://test-issuer.local"
            : "https://test-issuer.local";

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test authentication headers"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim(ClaimTypes.Name, name!),
            new Claim("name", name!),
            new Claim("iss", issuer!)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Extension methods for configuring test authentication.
/// </summary>
public static class TestAuthenticationExtensions
{
    /// <summary>
    /// Adds test authentication headers to an HttpClient.
    /// </summary>
    public static HttpClient WithTestUser(
        this HttpClient client,
        Guid userId,
        string email,
        string? name = null,
        string? issuer = null)
    {
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserEmailHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserNameHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.IssuerHeader);

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserEmailHeader, email);

        if (!string.IsNullOrEmpty(name))
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserNameHeader, name);
        }

        if (!string.IsNullOrEmpty(issuer))
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IssuerHeader, issuer);
        }

        return client;
    }
}
