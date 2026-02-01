using System.Security.Cryptography;
using System.Text.Json;
using Bogus;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Tests.Common.Factories;

/// <summary>
/// Factory for generating test SmtpApiKey entities.
/// Tracks plain text keys for authentication testing.
/// </summary>
public class SmtpApiKeyFactory
{
    private const int ApiKeyBytes = 32;
    private const int BCryptWorkFactor = 4; // Lower for faster tests

    private readonly Faker _faker = new();

    /// <summary>
    /// Standard scopes for full access.
    /// </summary>
    public static readonly string[] AllScopes = ["smtp", "pop3", "imap", "api:read", "api:write"];

    /// <summary>
    /// SMTP-only scope.
    /// </summary>
    public static readonly string[] SmtpOnlyScopes = ["smtp"];

    /// <summary>
    /// POP3-only scope.
    /// </summary>
    public static readonly string[] Pop3OnlyScopes = ["pop3"];

    /// <summary>
    /// IMAP-only scope.
    /// </summary>
    public static readonly string[] ImapOnlyScopes = ["imap"];

    /// <summary>
    /// Generates a random API key string.
    /// </summary>
    public static string GenerateKey()
    {
        var bytes = new byte[ApiKeyBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Hashes a key using BCrypt (with lower work factor for testing).
    /// </summary>
    public static string HashKey(string plainTextKey)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextKey, BCryptWorkFactor);
    }

    /// <summary>
    /// Creates an API key entity for a user, returning both the entity and plain text key.
    /// </summary>
    public static (SmtpApiKey ApiKey, string PlainTextKey) CreateForUser(
        Guid userId,
        string name = "Test Key",
        string[]? scopes = null)
    {
        var plainTextKey = GenerateKey();
        var apiKey = new SmtpApiKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            KeyHash = HashKey(plainTextKey),
            Scopes = JsonSerializer.Serialize(scopes ?? AllScopes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return (apiKey, plainTextKey);
    }

    /// <summary>
    /// Creates an API key with random name.
    /// </summary>
    public (SmtpApiKey ApiKey, string PlainTextKey) Create(Guid userId, string[]? scopes = null)
    {
        var name = _faker.Commerce.ProductName();
        return CreateForUser(userId, name, scopes);
    }

    /// <summary>
    /// Creates an API key that has been revoked.
    /// </summary>
    public static (SmtpApiKey ApiKey, string PlainTextKey) CreateRevoked(
        Guid userId,
        string name = "Revoked Key")
    {
        var (apiKey, plainTextKey) = CreateForUser(userId, name);
        apiKey.RevokedAt = DateTimeOffset.UtcNow.AddDays(-1);
        return (apiKey, plainTextKey);
    }

    /// <summary>
    /// Creates an API key with specific scopes.
    /// </summary>
    public static (SmtpApiKey ApiKey, string PlainTextKey) CreateWithScopes(
        Guid userId,
        params string[] scopes)
    {
        return CreateForUser(userId, "Scoped Key", scopes);
    }

    /// <summary>
    /// Creates multiple API keys for a user.
    /// </summary>
    public IReadOnlyList<(SmtpApiKey ApiKey, string PlainTextKey)> CreateMany(
        Guid userId,
        int count,
        string[]? scopes = null)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateForUser(userId, $"Key {i + 1}", scopes))
            .ToList();
    }
}

/// <summary>
/// Extension methods for SmtpApiKeyFactory.
/// </summary>
public static class SmtpApiKeyFactoryExtensions
{
    /// <summary>
    /// Adds an API key to the database context and saves.
    /// </summary>
    public static async Task<SmtpApiKey> AddToDbAsync(
        this SmtpApiKey apiKey,
        Infrastructure.Data.AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        context.SmtpApiKeys.Add(apiKey);
        await context.SaveChangesAsync(cancellationToken);
        return apiKey;
    }
}
