using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Moq;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Helpers;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.Api.Services;

[Trait("Category", "Unit")]
public class UserProvisioningServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailRepository> _emailRepositoryMock;
    private readonly Mock<ILogger<UserProvisioningService>> _loggerMock;
    private readonly UserProvisioningService _service;
    private readonly UserFactory _userFactory;

    public UserProvisioningServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailRepositoryMock = new Mock<IEmailRepository>();
        _loggerMock = new Mock<ILogger<UserProvisioningService>>();
        _userFactory = new UserFactory();

        _service = new UserProvisioningService(
            _userRepositoryMock.Object,
            _emailRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ExistingUser_ReturnsUserAndUpdatesLastLogin()
    {
        // Arrange
        var existingUser = _userFactory.Create();
        var principal = ClaimsPrincipalFactory.FromUser(existingUser);

        _userRepositoryMock
            .Setup(r => r.GetByOidcSubjectAsync(
                existingUser.OidcIssuer,
                existingUser.OidcSubject,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.GetOrCreateUserAsync(principal);

        // Assert
        result.ShouldBe(existingUser);
        _userRepositoryMock.Verify(r => r.UpdateAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
        _emailRepositoryMock.Verify(r => r.LinkEmailsToUserAsync(
            existingUser.Id,
            It.Is<IEnumerable<string>>(e => e.Contains(existingUser.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ExistingUserWithAdditionalAddresses_LinksAllAddresses()
    {
        // Arrange
        var existingUser = _userFactory.WithAdditionalAddresses("alias1@test.com", "alias2@test.com");
        var principal = ClaimsPrincipalFactory.FromUser(existingUser);

        _userRepositoryMock
            .Setup(r => r.GetByOidcSubjectAsync(
                existingUser.OidcIssuer,
                existingUser.OidcSubject,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        await _service.GetOrCreateUserAsync(principal);

        // Assert
        _emailRepositoryMock.Verify(r => r.LinkEmailsToUserAsync(
            existingUser.Id,
            It.Is<IEnumerable<string>>(e =>
                e.Contains(existingUser.Email) &&
                e.Contains("alias1@test.com") &&
                e.Contains("alias2@test.com")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_NewUser_CreatesUserAndLinksEmails()
    {
        // Arrange
        var subject = Guid.NewGuid().ToString();
        var issuer = "https://test-issuer.local";
        var email = "newuser@test.com";
        var name = "New User";

        var principal = ClaimsPrincipalFactory.Create(subject, email, name, issuer);

        _userRepositoryMock
            .Setup(r => r.GetByOidcSubjectAsync(issuer, subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        // Act
        var result = await _service.GetOrCreateUserAsync(principal);

        // Assert
        result.ShouldNotBeNull();
        capturedUser.ShouldNotBeNull();
        capturedUser.OidcSubject.ShouldBe(subject);
        capturedUser.OidcIssuer.ShouldBe(issuer);
        capturedUser.Email.ShouldBe(email);
        capturedUser.DisplayName.ShouldBe(name);

        _emailRepositoryMock.Verify(r => r.LinkEmailsToUserAsync(
            It.IsAny<Guid>(),
            It.Is<IEnumerable<string>>(e => e.Single() == email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_MissingSubjectClaim_ThrowsInvalidOperationException()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim("iss", "https://issuer.local")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.GetOrCreateUserAsync(principal));
    }

    [Fact]
    public async Task GetOrCreateUserAsync_MissingIssuerClaim_ThrowsInvalidOperationException()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-subject"),
            new Claim(ClaimTypes.Email, "test@test.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.GetOrCreateUserAsync(principal));
    }

    [Fact]
    public async Task GetOrCreateUserAsync_MissingEmailClaim_ForNewUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-subject"),
            new Claim("iss", "https://issuer.local")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _userRepositoryMock
            .Setup(r => r.GetByOidcSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.GetOrCreateUserAsync(principal));
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UsesSubClaimIfNameIdentifierMissing()
    {
        // Arrange
        var subject = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim("sub", subject),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim("iss", "https://issuer.local")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _userRepositoryMock
            .Setup(r => r.GetByOidcSubjectAsync("https://issuer.local", subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        // Act
        var result = await _service.GetOrCreateUserAsync(principal);

        // Assert
        result.OidcSubject.ShouldBe(subject);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UsesEmailClaimIfNoNameClaim()
    {
        // Arrange
        var email = "test@test.com";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-subject"),
            new Claim(ClaimTypes.Email, email),
            new Claim("iss", "https://issuer.local")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _userRepositoryMock
            .Setup(r => r.GetByOidcSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        // Act
        var result = await _service.GetOrCreateUserAsync(principal);

        // Assert
        result.DisplayName.ShouldBe(email);
    }
}
