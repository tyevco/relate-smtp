using Bogus;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Tests.Common.Factories;

/// <summary>
/// Factory for generating test User entities with realistic fake data.
/// </summary>
public class UserFactory
{
    private readonly Faker<User> _faker;
    private int _counter;

    public UserFactory()
    {
        _faker = new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.OidcSubject, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.OidcIssuer, _ => "https://test-issuer.local")
            .RuleFor(u => u.Email, f => f.Internet.Email().ToLowerInvariant())
            .RuleFor(u => u.DisplayName, f => f.Name.FullName())
            .RuleFor(u => u.CreatedAt, f => f.Date.PastOffset(1).ToUniversalTime())
            .RuleFor(u => u.LastLoginAt, f => f.Date.RecentOffset(30).ToUniversalTime());
    }

    /// <summary>
    /// Creates a new User with random data.
    /// </summary>
    public User Create()
    {
        return _faker.Generate();
    }

    /// <summary>
    /// Creates a User with a specific email address.
    /// </summary>
    public User WithEmail(string email)
    {
        var user = Create();
        user.Email = email.ToLowerInvariant();
        return user;
    }

    /// <summary>
    /// Creates a User with a predictable email pattern for testing.
    /// </summary>
    public User CreateSequential()
    {
        _counter++;
        var user = Create();
        user.Email = $"testuser{_counter}@test.local";
        user.DisplayName = $"Test User {_counter}";
        user.OidcSubject = $"test-subject-{_counter}";
        return user;
    }

    /// <summary>
    /// Creates multiple users.
    /// </summary>
    public IReadOnlyList<User> CreateMany(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Create())
            .ToList();
    }

    /// <summary>
    /// Creates a user with additional email addresses.
    /// </summary>
    public User WithAdditionalAddresses(params string[] addresses)
    {
        var user = Create();
        foreach (var address in addresses)
        {
            user.AdditionalAddresses.Add(new UserEmailAddress
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Address = address.ToLowerInvariant(),
                IsVerified = true,
                AddedAt = DateTimeOffset.UtcNow
            });
        }
        return user;
    }

    /// <summary>
    /// Creates a user with an API key for testing SMTP/POP3/IMAP authentication.
    /// </summary>
    public (User User, string PlainTextKey) WithApiKey(
        string keyName = "Test Key",
        params string[] scopes)
    {
        var user = Create();
        var (apiKey, plainTextKey) = SmtpApiKeyFactory.CreateForUser(user.Id, keyName, scopes);
        user.SmtpApiKeys.Add(apiKey);
        return (user, plainTextKey);
    }
}

/// <summary>
/// Extension methods for UserFactory.
/// </summary>
public static class UserFactoryExtensions
{
    /// <summary>
    /// Adds a user to the database context and saves.
    /// </summary>
    public static async Task<User> AddToDbAsync(
        this User user,
        Infrastructure.Data.AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
