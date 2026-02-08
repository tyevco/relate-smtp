using Relate.Smtp.Infrastructure.Repositories;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;

namespace Relate.Smtp.Tests.Integration.Infrastructure;

[Collection("PostgresDatabase")]
[Trait("Category", "Integration")]
public class SmtpApiKeyRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly UserFactory _userFactory;

    public SmtpApiKeyRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
        _userFactory = new UserFactory();
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateAsync_SavesApiKey()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Test Key");

        // Act
        var created = await repository.CreateAsync(apiKey);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var saved = await verifyContext.SmtpApiKeys.FindAsync(created.Id);
        saved.ShouldNotBeNull();
        saved.Name.ShouldBe("Test Key");
        saved.UserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetActiveKeysForUserAsync_ReturnsAllActiveUserKeys()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (key1, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Key 1");
        var (key2, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Key 2");
        var (key3, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Key 3");
        await key1.AddToDbAsync(context);
        await key2.AddToDbAsync(context);
        await key3.AddToDbAsync(context);

        // Act
        var result = await repository.GetActiveKeysForUserAsync(user.Id);

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetActiveKeysForUserAsync_ExcludesOtherUsersKeys()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        var user1 = _userFactory.CreateSequential();
        var user2 = _userFactory.CreateSequential();
        await user1.AddToDbAsync(context);
        await user2.AddToDbAsync(context);

        var (key1, _) = SmtpApiKeyFactory.CreateForUser(user1.Id);
        var (key2, _) = SmtpApiKeyFactory.CreateForUser(user2.Id);
        await key1.AddToDbAsync(context);
        await key2.AddToDbAsync(context);

        // Act
        var result = await repository.GetActiveKeysForUserAsync(user1.Id);

        // Assert
        result.Count.ShouldBe(1);
        result.First().UserId.ShouldBe(user1.Id);
    }

    [Fact]
    public async Task GetActiveKeysForUserAsync_ExcludesRevokedKeys()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (activeKey, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Active Key");
        var (revokedKey, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Revoked Key");
        revokedKey.RevokedAt = DateTimeOffset.UtcNow;
        await activeKey.AddToDbAsync(context);
        await revokedKey.AddToDbAsync(context);

        // Act
        var result = await repository.GetActiveKeysForUserAsync(user.Id);

        // Assert
        result.Count.ShouldBe(1);
        result.First().Name.ShouldBe("Active Key");
    }

    [Fact]
    public async Task UpdateLastUsedAsync_UpdatesTimestamp()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(user.Id);
        apiKey.LastUsedAt = null;
        await apiKey.AddToDbAsync(context);

        var newTimestamp = DateTimeOffset.UtcNow;

        // Act
        await repository.UpdateLastUsedAsync(apiKey.Id, newTimestamp);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SmtpApiKeys.FindAsync(apiKey.Id);
        updated.ShouldNotBeNull();
        updated.LastUsedAt.ShouldNotBeNull();
        updated.LastUsedAt.Value.ShouldBeInRange(
            newTimestamp.AddSeconds(-1),
            newTimestamp.AddSeconds(1));
    }

    [Fact]
    public async Task RevokeAsync_SetsRevokedAt()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(user.Id);
        await apiKey.AddToDbAsync(context);

        // Act
        await repository.RevokeAsync(apiKey.Id);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var revoked = await verifyContext.SmtpApiKeys.FindAsync(apiKey.Id);
        revoked.ShouldNotBeNull();
        revoked.RevokedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetByKeyWithScopeAsync_ReturnsKeyWithMatchingScope()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (apiKey, plainTextKey) = SmtpApiKeyFactory.CreateForUser(user.Id, "Test", SmtpApiKeyFactory.SmtpOnlyScopes);
        await apiKey.AddToDbAsync(context);

        // Act
        var result = await repository.GetByKeyWithScopeAsync(plainTextKey, "smtp");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(apiKey.Id);
    }

    [Fact]
    public async Task GetByKeyWithScopeAsync_ReturnsNullForMissingScope()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var (apiKey, plainTextKey) = SmtpApiKeyFactory.CreateForUser(user.Id, "Test", SmtpApiKeyFactory.SmtpOnlyScopes);
        await apiKey.AddToDbAsync(context);

        // Act - Request imap scope when key only has smtp
        var result = await repository.GetByKeyWithScopeAsync(plainTextKey, "imap");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("smtp", true)]
    [InlineData("pop3", true)]
    [InlineData("imap", true)]
    [InlineData("api:read", true)]
    [InlineData("api:write", true)]
    [InlineData("nonexistent", false)]
    public void HasScope_ChecksScopesCorrectly(string scope, bool expectedResult)
    {
        // Arrange
        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(Guid.NewGuid(), "Test", SmtpApiKeyFactory.AllScopes);

        using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        // Act
        var result = repository.HasScope(apiKey, scope);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void HasScope_LimitedScopes_OnlyReturnsGrantedScopes()
    {
        // Arrange
        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(Guid.NewGuid(), "Test", SmtpApiKeyFactory.SmtpOnlyScopes);

        using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        // Act & Assert
        repository.HasScope(apiKey, "smtp").ShouldBeTrue();
        repository.HasScope(apiKey, "pop3").ShouldBeFalse();
        repository.HasScope(apiKey, "imap").ShouldBeFalse();
    }

    [Fact]
    public void HasScope_InvalidJsonScopes_ReturnsFalse()
    {
        // Arrange
        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(Guid.NewGuid());
        apiKey.Scopes = "invalid json";

        using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        // Act
        var result = repository.HasScope(apiKey, "smtp");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasScope_EmptyScopes_ReturnsFalse()
    {
        // Arrange
        var (apiKey, _) = SmtpApiKeyFactory.CreateForUser(Guid.NewGuid(), "Test", Array.Empty<string>());

        using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        // Act
        var result = repository.HasScope(apiKey, "smtp");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ParseScopes_ParsesValidJson()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        // Act
        var result = repository.ParseScopes("[\"smtp\", \"pop3\", \"imap\"]");

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain("smtp");
        result.ShouldContain("pop3");
        result.ShouldContain("imap");
    }

    [Fact]
    public void ParseScopes_ReturnsEmptyForInvalidJson()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var repository = new SmtpApiKeyRepository(context);

        // Act
        var result = repository.ParseScopes("not valid json");

        // Assert
        result.Count.ShouldBe(0);
    }
}
