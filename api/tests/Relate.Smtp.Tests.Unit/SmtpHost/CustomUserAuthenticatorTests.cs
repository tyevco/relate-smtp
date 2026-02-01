using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.SmtpHost.Handlers;
using Relate.Smtp.Tests.Common.Factories;
using Shouldly;
using SmtpServer;

namespace Relate.Smtp.Tests.Unit.SmtpHost;

[Trait("Category", "Unit")]
[Trait("Protocol", "SMTP")]
public class CustomUserAuthenticatorTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ISmtpApiKeyRepository> _apiKeyRepositoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<ILogger<CustomUserAuthenticator>> _loggerMock;
    private readonly CustomUserAuthenticator _authenticator;
    private readonly UserFactory _userFactory;

    public CustomUserAuthenticatorTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _apiKeyRepositoryMock = new Mock<ISmtpApiKeyRepository>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<CustomUserAuthenticator>>();
        _userFactory = new UserFactory();

        // Setup service provider chain
        var scopedServiceProvider = new Mock<IServiceProvider>();
        scopedServiceProvider.Setup(s => s.GetService(typeof(IUserRepository)))
            .Returns(_userRepositoryMock.Object);
        scopedServiceProvider.Setup(s => s.GetService(typeof(ISmtpApiKeyRepository)))
            .Returns(_apiKeyRepositoryMock.Object);

        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProvider.Object);
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);

        _authenticator = new CustomUserAuthenticator(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var (user, plainTextKey) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        var apiKey = user.SmtpApiKeys.First();
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apiKeyRepositoryMock
            .Setup(r => r.HasScope(apiKey, "smtp"))
            .Returns(true);

        // Act
        var result = await _authenticator.AuthenticateAsync(context.Object, user.Email, plainTextKey, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_StoresUserIdInContext()
    {
        // Arrange
        var (user, plainTextKey) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        var apiKey = user.SmtpApiKeys.First();
        var context = CreateMockSessionContext();
        var properties = new Dictionary<string, object>();
        context.Setup(c => c.Properties).Returns(properties);

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apiKeyRepositoryMock
            .Setup(r => r.HasScope(apiKey, "smtp"))
            .Returns(true);

        // Act
        await _authenticator.AuthenticateAsync(context.Object, user.Email, plainTextKey, CancellationToken.None);

        // Assert
        properties.ShouldContainKey("AuthenticatedUserId");
        properties["AuthenticatedUserId"].ShouldBe(user.Id);
        properties.ShouldContainKey("AuthenticatedEmail");
        properties["AuthenticatedEmail"].ShouldBe(user.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task AuthenticateAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticator.AuthenticateAsync(context.Object, "unknown@test.com", "password", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var (user, _) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticator.AuthenticateAsync(context.Object, user.Email, "wrong-password", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_MissingSmtpScope_ReturnsFalse()
    {
        // Arrange
        var (user, plainTextKey) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.Pop3OnlyScopes);
        var apiKey = user.SmtpApiKeys.First();
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apiKeyRepositoryMock
            .Setup(r => r.HasScope(apiKey, "smtp"))
            .Returns(false);

        // Act
        var result = await _authenticator.AuthenticateAsync(context.Object, user.Email, plainTextKey, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        var (user, plainTextKey) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        var apiKey = user.SmtpApiKeys.First();
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apiKeyRepositoryMock
            .Setup(r => r.HasScope(apiKey, "smtp"))
            .Returns(true);

        // Act
        await _authenticator.AuthenticateAsync(context.Object, user.Email.ToUpperInvariant(), plainTextKey, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(r => r.GetByEmailWithApiKeysAsync(
            user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AuthenticateAsync_CachesSuccessfulAuthentication()
    {
        // Arrange
        var (user, plainTextKey) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        var apiKey = user.SmtpApiKeys.First();
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apiKeyRepositoryMock
            .Setup(r => r.HasScope(apiKey, "smtp"))
            .Returns(true);

        // Act - Call twice
        await _authenticator.AuthenticateAsync(context.Object, user.Email, plainTextKey, CancellationToken.None);
        await _authenticator.AuthenticateAsync(context.Object, user.Email, plainTextKey, CancellationToken.None);

        // Assert - Repository should only be called once due to caching
        _userRepositoryMock.Verify(r => r.GetByEmailWithApiKeysAsync(
            user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_CachesFailedAuthentication()
    {
        // Arrange
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act - Call twice
        await _authenticator.AuthenticateAsync(context.Object, "unknown@test.com", "password", CancellationToken.None);
        await _authenticator.AuthenticateAsync(context.Object, "unknown@test.com", "password", CancellationToken.None);

        // Assert - Repository should only be called once due to caching
        _userRepositoryMock.Verify(r => r.GetByEmailWithApiKeysAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_UpdatesLastUsedTimestamp()
    {
        // Arrange
        var (user, plainTextKey) = _userFactory.WithApiKey("Test Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        var apiKey = user.SmtpApiKeys.First();
        var context = CreateMockSessionContext();

        _userRepositoryMock
            .Setup(r => r.GetByEmailWithApiKeysAsync(user.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apiKeyRepositoryMock
            .Setup(r => r.HasScope(apiKey, "smtp"))
            .Returns(true);

        // Act
        await _authenticator.AuthenticateAsync(context.Object, user.Email, plainTextKey, CancellationToken.None);

        // Assert
        _apiKeyRepositoryMock.Verify(r => r.UpdateLastUsedAsync(
            apiKey.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private Mock<ISessionContext> CreateMockSessionContext()
    {
        var contextMock = new Mock<ISessionContext>();
        var properties = new Dictionary<string, object>();
        contextMock.Setup(c => c.Properties).Returns(properties);
        return contextMock;
    }
}
