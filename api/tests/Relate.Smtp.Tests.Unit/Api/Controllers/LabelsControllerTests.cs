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
public class LabelsControllerTests
{
    private readonly Mock<ILabelRepository> _labelRepositoryMock;
    private readonly Mock<IEmailLabelRepository> _emailLabelRepositoryMock;
    private readonly Mock<IEmailRepository> _emailRepositoryMock;
    private readonly Mock<UserProvisioningService> _userProvisioningServiceMock;
    private readonly LabelsController _controller;
    private readonly UserFactory _userFactory;
    private readonly EmailFactory _emailFactory;
    private readonly LabelFactory _labelFactory;
    private readonly User _testUser;

    public LabelsControllerTests()
    {
        _labelRepositoryMock = new Mock<ILabelRepository>();
        _emailLabelRepositoryMock = new Mock<IEmailLabelRepository>();
        _emailRepositoryMock = new Mock<IEmailRepository>();
        _userProvisioningServiceMock = new Mock<UserProvisioningService>(
            Mock.Of<IUserRepository>(),
            Mock.Of<IEmailRepository>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<UserProvisioningService>>());

        _userFactory = new UserFactory();
        _emailFactory = new EmailFactory();
        _labelFactory = new LabelFactory();
        _testUser = _userFactory.Create();

        _controller = new LabelsController(
            _labelRepositoryMock.Object,
            _emailLabelRepositoryMock.Object,
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
    public async Task GetLabels_ReturnsUsersLabels()
    {
        // Arrange
        var labels = _labelFactory.CreateCommonLabels(_testUser.Id);

        _labelRepositoryMock
            .Setup(r => r.GetByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        // Act
        var result = await _controller.GetLabels();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<List<LabelDto>>();
        response.Count.ShouldBe(5);
    }

    [Fact]
    public async Task CreateLabel_ValidRequest_CreatesAndReturnsLabel()
    {
        // Arrange
        var request = new CreateLabelRequest("Work", "#3b82f6", 0);

        Label? capturedLabel = null;
        _labelRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Callback<Label, CancellationToken>((l, _) => capturedLabel = l)
            .ReturnsAsync((Label l, CancellationToken _) => l);

        // Act
        var result = await _controller.CreateLabel(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var response = createdResult.Value.ShouldBeOfType<LabelDto>();
        response.Name.ShouldBe("Work");
        response.Color.ShouldBe("#3b82f6");

        capturedLabel.ShouldNotBeNull();
        capturedLabel.UserId.ShouldBe(_testUser.Id);
        capturedLabel.Name.ShouldBe("Work");
    }

    [Fact]
    public async Task UpdateLabel_ExistingLabel_UpdatesAndReturns()
    {
        // Arrange
        var label = _labelFactory.WithName(_testUser.Id, "Old Name", "#000000");
        var request = new UpdateLabelRequest("New Name", "#ffffff", 5);

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.UpdateLabel(label.Id, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<LabelDto>();
        response.Name.ShouldBe("New Name");
        response.Color.ShouldBe("#ffffff");

        _labelRepositoryMock.Verify(r => r.UpdateAsync(label, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLabel_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var label = _labelFactory.WithName(_testUser.Id, "Original", "#3b82f6");
        label.SortOrder = 10;
        var request = new UpdateLabelRequest(Name: "Updated", Color: null, SortOrder: null);

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.UpdateLabel(label.Id, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<LabelDto>();
        response.Name.ShouldBe("Updated");
        response.Color.ShouldBe("#3b82f6"); // Unchanged
        response.SortOrder.ShouldBe(10);    // Unchanged
    }

    [Fact]
    public async Task UpdateLabel_NonExistentLabel_ReturnsNotFound()
    {
        // Arrange
        var labelId = Guid.NewGuid();
        var request = new UpdateLabelRequest("Name", null, null);

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        var result = await _controller.UpdateLabel(labelId, request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateLabel_OtherUsersLabel_ReturnsNotFound()
    {
        // Arrange
        var otherUser = _userFactory.Create();
        var label = _labelFactory.Create(otherUser.Id);
        var request = new UpdateLabelRequest("Name", null, null);

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.UpdateLabel(label.Id, request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteLabel_ExistingLabel_DeletesAndReturnsNoContent()
    {
        // Arrange
        var label = _labelFactory.Create(_testUser.Id);

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.DeleteLabel(label.Id);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _labelRepositoryMock.Verify(r => r.DeleteAsync(label.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLabel_NonExistentLabel_ReturnsNotFound()
    {
        // Arrange
        var labelId = Guid.NewGuid();

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        var result = await _controller.DeleteLabel(labelId);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AddLabelToEmail_ValidRequest_AddsLabel()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);
        var label = _labelFactory.Create(_testUser.Id);
        var request = new AddLabelRequest(label.Id);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);
        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.AddLabelToEmail(email.Id, request);

        // Assert
        result.ShouldBeOfType<OkResult>();
        _emailLabelRepositoryMock.Verify(r => r.AddAsync(
            It.Is<EmailLabel>(el =>
                el.EmailId == email.Id &&
                el.LabelId == label.Id &&
                el.UserId == _testUser.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddLabelToEmail_NonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var label = _labelFactory.Create(_testUser.Id);
        var request = new AddLabelRequest(label.Id);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email?)null);

        // Act
        var result = await _controller.AddLabelToEmail(emailId, request);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Email not found");
    }

    [Fact]
    public async Task AddLabelToEmail_OtherUsersLabel_ReturnsNotFound()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);
        var otherUser = _userFactory.Create();
        var label = _labelFactory.Create(otherUser.Id);
        var request = new AddLabelRequest(label.Id);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);
        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.AddLabelToEmail(email.Id, request);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Label not found");
    }

    [Fact]
    public async Task RemoveLabelFromEmail_ValidRequest_RemovesLabel()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);
        var label = _labelFactory.Create(_testUser.Id);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _controller.RemoveLabelFromEmail(email.Id, label.Id);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _emailLabelRepositoryMock.Verify(r => r.DeleteAsync(
            email.Id, label.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEmailsByLabel_ReturnsEmailsWithLabel()
    {
        // Arrange
        var label = _labelFactory.Create(_testUser.Id);
        var emails = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.ForUser(_testUser))
            .ToList();

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);
        _emailLabelRepositoryMock
            .Setup(r => r.GetEmailsByLabelIdAsync(_testUser.Id, label.Id, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);
        _emailLabelRepositoryMock
            .Setup(r => r.GetEmailCountByLabelIdAsync(_testUser.Id, label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.GetEmailsByLabel(label.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.Items.Count.ShouldBe(3);
        response.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetEmailsByLabel_OtherUsersLabel_ReturnsNotFound()
    {
        // Arrange
        var otherUser = _userFactory.Create();
        var label = _labelFactory.Create(otherUser.Id);

        _labelRepositoryMock
            .Setup(r => r.GetByIdAsync(label.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        var result = await _controller.GetEmailsByLabel(label.Id);

        // Assert
        var notFoundResult = result.Result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Label not found");
    }
}
