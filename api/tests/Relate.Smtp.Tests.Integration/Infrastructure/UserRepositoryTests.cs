using Relate.Smtp.Core.Entities;
using Relate.Smtp.Infrastructure.Repositories;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;
using Shouldly;

namespace Relate.Smtp.Tests.Integration.Infrastructure;

[Collection("PostgresDatabase")]
[Trait("Category", "Integration")]
public class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly UserFactory _userFactory;

    public UserRepositoryTests(PostgresContainerFixture fixture)
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
    public async Task AddAsync_SavesToDatabase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.Create();

        // Act
        await repository.AddAsync(user);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var saved = await verifyContext.Users.FindAsync(user.Id);
        saved.ShouldNotBeNull();
        saved.Email.ShouldBe(user.Email);
        saved.DisplayName.ShouldBe(user.DisplayName);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByIdAsync(user.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        result.Email.ShouldBe(user.Email);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByOidcSubjectAsync_FindsByIssuerAndSubject()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByOidcSubjectAsync(user.OidcIssuer, user.OidcSubject);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetByOidcSubjectAsync_IncludesAdditionalAddresses()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.WithAdditionalAddresses("alias1@test.com", "alias2@test.com");
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByOidcSubjectAsync(user.OidcIssuer, user.OidcSubject);

        // Assert
        result.ShouldNotBeNull();
        result.AdditionalAddresses.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetByOidcSubjectAsync_WrongIssuer_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByOidcSubjectAsync("https://wrong-issuer.local", user.OidcSubject);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_FindsByEmail()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.WithEmail("findme@test.com");
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByEmailAsync("findme@test.com");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_CaseInsensitive()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.WithEmail("test@example.com");
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByEmailAsync("TEST@EXAMPLE.COM");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_FindsByAdditionalAddress()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.WithAdditionalAddresses("alias@test.com");
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByEmailAsync("alias@test.com");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetByEmailWithApiKeysAsync_IncludesApiKeys()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var (user, _) = _userFactory.WithApiKey("Key 1", SmtpApiKeyFactory.AllScopes);
        var (apiKey2, _) = SmtpApiKeyFactory.CreateForUser(user.Id, "Key 2", SmtpApiKeyFactory.SmtpOnlyScopes);
        user.SmtpApiKeys.Add(apiKey2);
        await user.AddToDbAsync(context);

        // Act
        var result = await repository.GetByEmailWithApiKeysAsync(user.Email);

        // Assert
        result.ShouldNotBeNull();
        result.SmtpApiKeys.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUser()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        // Act
        user.DisplayName = "Updated Name";
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(user);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.Users.FindAsync(user.Id);
        updated.ShouldNotBeNull();
        updated.DisplayName.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task UserEmailAddress_CanBeAddedAndRetrieved()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var additionalAddress = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Address = "additional@test.com",
            IsVerified = true,
            AddedAt = DateTimeOffset.UtcNow
        };

        context.UserEmailAddresses.Add(additionalAddress);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByOidcSubjectAsync(user.OidcIssuer, user.OidcSubject);

        // Assert
        result.ShouldNotBeNull();
        result.AdditionalAddresses.Count.ShouldBe(1);
        result.AdditionalAddresses.First().Address.ShouldBe("additional@test.com");
    }
}
