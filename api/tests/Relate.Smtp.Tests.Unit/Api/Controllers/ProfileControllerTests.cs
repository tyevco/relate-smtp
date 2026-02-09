using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Relate.Smtp.Api.Controllers;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Helpers;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.Api.Controllers;

[Trait("Category", "Unit")]
public class ProfileControllerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailRepository> _emailRepositoryMock;
    private readonly Mock<UserProvisioningService> _userProvisioningServiceMock;
    private readonly ProfileController _controller;
    private readonly UserFactory _userFactory;
    private readonly User _testUser;

    public ProfileControllerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailRepositoryMock = new Mock<IEmailRepository>();
        _userProvisioningServiceMock = new Mock<UserProvisioningService>(
            Mock.Of<IUserRepository>(),
            Mock.Of<IEmailRepository>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<UserProvisioningService>>());

        _userFactory = new UserFactory();
        _testUser = _userFactory.Create();

        _controller = new ProfileController(
            _userRepositoryMock.Object,
            _emailRepositoryMock.Object,
            _userProvisioningServiceMock.Object);

        // Setup default user provisioning
        _userProvisioningServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        // Set up controller context
        var httpContext = new DefaultHttpContext();
        httpContext.User = ClaimsPrincipalFactory.FromUser(_testUser);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetProfile_ReturnsUserProfile()
    {
        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<ProfileDto>();
        response.Id.ShouldBe(_testUser.Id);
        response.Email.ShouldBe(_testUser.Email);
    }

    [Fact]
    public async Task AddEmailAddress_ValidAddress_CreatesUnverifiedAddressWithToken()
    {
        // Arrange
        var request = new AddEmailAddressRequest("new@example.com");

        UserEmailAddress? capturedAddress = null;
        _userRepositoryMock
            .Setup(r => r.AddEmailAddressAsync(It.IsAny<UserEmailAddress>(), It.IsAny<CancellationToken>()))
            .Callback<UserEmailAddress, CancellationToken>((a, _) => capturedAddress = a)
            .ReturnsAsync((UserEmailAddress a, CancellationToken _) => a);

        // Act
        var result = await _controller.AddEmailAddress(request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailAddressDto>();
        response.Address.ShouldBe("new@example.com");
        response.IsVerified.ShouldBeFalse();

        capturedAddress.ShouldNotBeNull();
        capturedAddress.IsVerified.ShouldBeFalse();
        capturedAddress.VerificationToken.ShouldNotBeNullOrWhiteSpace();
        capturedAddress.VerificationToken!.Length.ShouldBe(6);
        capturedAddress.VerificationTokenExpiresAt.ShouldNotBeNull();
        capturedAddress.VerificationTokenExpiresAt!.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AddEmailAddress_EmptyAddress_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddEmailAddressRequest("");

        // Act
        var result = await _controller.AddEmailAddress(request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddEmailAddress_InvalidFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddEmailAddressRequest("not-an-email");

        // Act
        var result = await _controller.AddEmailAddress(request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddEmailAddress_DuplicateAddress_ReturnsBadRequest()
    {
        // Arrange
        _testUser.AdditionalAddresses.Add(new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "existing@example.com",
            IsVerified = true,
            AddedAt = DateTimeOffset.UtcNow
        });

        var request = new AddEmailAddressRequest("existing@example.com");

        // Act
        var result = await _controller.AddEmailAddress(request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveEmailAddress_ExistingAddress_ReturnsNoContent()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "extra@example.com",
            IsVerified = false,
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        // Act
        var result = await _controller.RemoveEmailAddress(address.Id);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _userRepositoryMock.Verify(r => r.RemoveEmailAddressAsync(address.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEmailAddress_NonExistentAddress_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RemoveEmailAddress(Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SendVerification_UnverifiedAddress_ReturnsOk()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "unverified@example.com",
            IsVerified = false,
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        // Act
        var result = await _controller.SendVerification(address.Id);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        _userRepositoryMock.Verify(r => r.UpdateEmailAddressAsync(
            It.Is<UserEmailAddress>(a =>
                a.Id == address.Id &&
                a.VerificationToken != null &&
                a.VerificationToken.Length == 6 &&
                a.VerificationTokenExpiresAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendVerification_AlreadyVerified_ReturnsBadRequest()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "verified@example.com",
            IsVerified = true,
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        // Act
        var result = await _controller.SendVerification(address.Id);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendVerification_NonExistentAddress_ReturnsNotFound()
    {
        // Act
        var result = await _controller.SendVerification(Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task VerifyEmailAddress_ValidCode_VerifiesAndReturnsAddress()
    {
        // Arrange
        var token = "123456";
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "pending@example.com",
            IsVerified = false,
            VerificationToken = token,
            VerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(23),
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        var request = new VerifyEmailAddressRequest(token);

        // Act
        var result = await _controller.VerifyEmailAddress(address.Id, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailAddressDto>();
        response.IsVerified.ShouldBeTrue();
        response.Address.ShouldBe("pending@example.com");

        _userRepositoryMock.Verify(r => r.UpdateEmailAddressAsync(
            It.Is<UserEmailAddress>(a =>
                a.Id == address.Id &&
                a.IsVerified == true &&
                a.VerificationToken == null &&
                a.VerificationTokenExpiresAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAddress_InvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "pending@example.com",
            IsVerified = false,
            VerificationToken = "123456",
            VerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(23),
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        var request = new VerifyEmailAddressRequest("999999");

        // Act
        var result = await _controller.VerifyEmailAddress(address.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        _userRepositoryMock.Verify(r => r.UpdateEmailAddressAsync(It.IsAny<UserEmailAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAddress_ExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "pending@example.com",
            IsVerified = false,
            VerificationToken = "123456",
            VerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        var request = new VerifyEmailAddressRequest("123456");

        // Act
        var result = await _controller.VerifyEmailAddress(address.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VerifyEmailAddress_AlreadyVerified_ReturnsBadRequest()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "verified@example.com",
            IsVerified = true,
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        var request = new VerifyEmailAddressRequest("123456");

        // Act
        var result = await _controller.VerifyEmailAddress(address.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VerifyEmailAddress_NonExistentAddress_ReturnsNotFound()
    {
        // Arrange
        var request = new VerifyEmailAddressRequest("123456");

        // Act
        var result = await _controller.VerifyEmailAddress(Guid.NewGuid(), request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task VerifyEmailAddress_EmptyCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailAddressRequest("");

        // Act
        var result = await _controller.VerifyEmailAddress(Guid.NewGuid(), request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VerifyEmailAddress_NullToken_ReturnsBadRequest()
    {
        // Arrange
        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Address = "pending@example.com",
            IsVerified = false,
            VerificationToken = null,
            VerificationTokenExpiresAt = null,
            AddedAt = DateTimeOffset.UtcNow
        };
        _testUser.AdditionalAddresses.Add(address);

        var request = new VerifyEmailAddressRequest("123456");

        // Act
        var result = await _controller.VerifyEmailAddress(address.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
