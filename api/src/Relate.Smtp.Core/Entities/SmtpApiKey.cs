namespace Relate.Smtp.Core.Entities;

public class SmtpApiKey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// First 12 characters of the raw API key, used for efficient lookup.
    /// This allows O(1) database lookup by prefix, then BCrypt verification only on matching keys.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// JSON array of permission scopes, e.g., ["smtp", "pop3", "api:read", "api:write"]
    /// </summary>
    public string Scopes { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
