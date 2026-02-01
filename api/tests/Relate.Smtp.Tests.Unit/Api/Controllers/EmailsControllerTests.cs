using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Relate.Smtp.Api.Controllers;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Core.Models;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Helpers;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.Api.Controllers;

[Trait("Category", "Unit")]
public class EmailsControllerTests
{
    private readonly Mock<IEmailRepository> _emailRepositoryMock;
    private readonly Mock<UserProvisioningService> _userProvisioningServiceMock;
    private readonly Mock<IEmailNotificationService> _notificationServiceMock;
    private readonly EmailsController _controller;
    private readonly UserFactory _userFactory;
    private readonly EmailFactory _emailFactory;
    private readonly User _testUser;

    public EmailsControllerTests()
    {
        _emailRepositoryMock = new Mock<IEmailRepository>();
        _userProvisioningServiceMock = new Mock<UserProvisioningService>(
            Mock.Of<IUserRepository>(),
            Mock.Of<IEmailRepository>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<UserProvisioningService>>());
        _notificationServiceMock = new Mock<IEmailNotificationService>();

        _userFactory = new UserFactory();
        _emailFactory = new EmailFactory();
        _testUser = _userFactory.Create();

        _controller = new EmailsController(
            _emailRepositoryMock.Object,
            _userProvisioningServiceMock.Object,
            _notificationServiceMock.Object);

        // Setup default user provisioning
        _userProvisioningServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        // Set up controller context with a mock user
        var httpContext = new DefaultHttpContext();
        httpContext.User = ClaimsPrincipalFactory.FromUser(_testUser);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetEmails_ReturnsEmailListResponse()
    {
        // Arrange
        var emails = Enumerable.Range(0, 5)
            .Select(_ => _emailFactory.ForUser(_testUser))
            .ToList();

        _emailRepositoryMock
            .Setup(r => r.GetByUserIdAsync(_testUser.Id, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);
        _emailRepositoryMock
            .Setup(r => r.GetCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.GetEmails();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.Items.Count.ShouldBe(5);
        response.TotalCount.ShouldBe(5);
        response.UnreadCount.ShouldBe(3);
        response.Page.ShouldBe(1);
        response.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task GetEmails_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var emails = Enumerable.Range(0, 10)
            .Select(_ => _emailFactory.ForUser(_testUser))
            .ToList();

        _emailRepositoryMock
            .Setup(r => r.GetByUserIdAsync(_testUser.Id, 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);
        _emailRepositoryMock
            .Setup(r => r.GetCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _controller.GetEmails(page: 2, pageSize: 10);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.Page.ShouldBe(2);
        response.PageSize.ShouldBe(10);
        response.TotalCount.ShouldBe(25);
    }

    [Theory]
    [InlineData(0, 20)]  // Invalid page defaults to 1
    [InlineData(-1, 20)] // Negative page defaults to 1
    public async Task GetEmails_InvalidPage_DefaultsToPageOne(int invalidPage, int expectedSkip)
    {
        // Arrange
        _emailRepositoryMock
            .Setup(r => r.GetByUserIdAsync(_testUser.Id, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Email>());
        _emailRepositoryMock
            .Setup(r => r.GetCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetEmails(page: invalidPage);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.Page.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]   // Invalid pageSize defaults to 20
    [InlineData(-1)]  // Negative pageSize defaults to 20
    [InlineData(101)] // Over 100 defaults to 20
    public async Task GetEmails_InvalidPageSize_DefaultsToTwenty(int invalidPageSize)
    {
        // Arrange
        _emailRepositoryMock
            .Setup(r => r.GetByUserIdAsync(_testUser.Id, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Email>());
        _emailRepositoryMock
            .Setup(r => r.GetCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetEmails(pageSize: invalidPageSize);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task GetEmail_ExistingEmailWithAccess_ReturnsEmail()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _controller.GetEmail(email.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailDetailDto>();
        response.Id.ShouldBe(email.Id);
        response.Subject.ShouldBe(email.Subject);
    }

    [Fact]
    public async Task GetEmail_NonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email?)null);

        // Act
        var result = await _controller.GetEmail(emailId);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetEmail_EmailWithoutAccess_ReturnsNotFound()
    {
        // Arrange
        var otherUser = _userFactory.Create();
        var email = _emailFactory.ForUser(otherUser);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _controller.GetEmail(email.Id);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateEmail_MarkAsRead_UpdatesAndNotifies()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser, isRead: false);
        var request = new UpdateEmailRequest(IsRead: true);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        // Act
        var result = await _controller.UpdateEmail(email.Id, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailDetailDto>();
        response.IsRead.ShouldBeTrue();

        _emailRepositoryMock.Verify(r => r.UpdateAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyEmailUpdatedAsync(
            _testUser.Id, email.Id, true, It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyUnreadCountChangedAsync(
            _testUser.Id, 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEmail_NonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var request = new UpdateEmailRequest(IsRead: true);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email?)null);

        // Act
        var result = await _controller.UpdateEmail(emailId, request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteEmail_ExistingEmailWithAccess_DeletesAndNotifies()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.DeleteEmail(email.Id);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _emailRepositoryMock.Verify(r => r.DeleteAsync(email.Id, It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyEmailDeletedAsync(
            _testUser.Id, email.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEmail_NonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email?)null);

        // Act
        var result = await _controller.DeleteEmail(emailId);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAttachment_ExistingAttachment_ReturnsFile()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);
        _emailFactory.WithAttachment(email, "test.pdf", "application/pdf", new byte[] { 1, 2, 3 });
        var attachment = email.Attachments.First();

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _controller.GetAttachment(email.Id, attachment.Id);

        // Assert
        var fileResult = result.ShouldBeOfType<FileContentResult>();
        fileResult.FileDownloadName.ShouldBe("test.pdf");
        fileResult.ContentType.ShouldBe("application/pdf");
        fileResult.FileContents.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task GetAttachment_NonExistentAttachment_ReturnsNotFound()
    {
        // Arrange
        var email = _emailFactory.ForUser(_testUser);

        _emailRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _controller.GetAttachment(email.Id, Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SearchEmails_WithQuery_ReturnsMatchingEmails()
    {
        // Arrange
        var emails = new List<Email> { _emailFactory.ForUser(_testUser) };

        _emailRepositoryMock
            .Setup(r => r.SearchByUserIdAsync(
                _testUser.Id,
                It.Is<EmailSearchFilters>(f => f.Query == "test"),
                0, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);
        _emailRepositoryMock
            .Setup(r => r.GetSearchCountByUserIdAsync(
                _testUser.Id,
                It.IsAny<EmailSearchFilters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _emailRepositoryMock
            .Setup(r => r.GetUnreadCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.SearchEmails(
            q: "test",
            fromDate: null,
            toDate: null,
            hasAttachments: null,
            isRead: null);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.Items.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetThread_ReturnsAllEmailsInThread()
    {
        // Arrange
        var threadId = Guid.NewGuid();
        var emails = _emailFactory.CreateThread(3, _testUser);

        _emailRepositoryMock
            .Setup(r => r.GetByThreadIdAsync(threadId, _testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _controller.GetThread(threadId);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<List<EmailDetailDto>>();
        response.Count.ShouldBe(4); // 1 parent + 3 replies
    }

    [Fact]
    public async Task BulkMarkRead_UpdatesMultipleEmails()
    {
        // Arrange
        var emails = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.ForUser(_testUser, isRead: false))
            .ToList();
        var emailIds = emails.Select(e => e.Id).ToList();
        var request = new BulkEmailOperationRequest { EmailIds = emailIds, IsRead = true };

        foreach (var email in emails)
        {
            _emailRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(email.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(email);
        }

        // Act
        var result = await _controller.BulkMarkRead(request);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _emailRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetSentEmails_ReturnsUsersSentEmails()
    {
        // Arrange
        var emails = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.SentByUser(_testUser))
            .ToList();

        _emailRepositoryMock
            .Setup(r => r.GetSentByUserIdAsync(_testUser.Id, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);
        _emailRepositoryMock
            .Setup(r => r.GetSentCountByUserIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.GetSentEmails(fromAddress: null);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmailListResponse>();
        response.Items.Count.ShouldBe(3);
        response.TotalCount.ShouldBe(3);
        response.UnreadCount.ShouldBe(0); // Sent emails don't have unread count
    }
}
