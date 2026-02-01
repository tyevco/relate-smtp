using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Models;
using Relate.Smtp.Infrastructure.Repositories;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;
using Shouldly;

namespace Relate.Smtp.Tests.Integration.Infrastructure;

[Collection("PostgresDatabase")]
[Trait("Category", "Integration")]
public class EmailRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly UserFactory _userFactory;
    private readonly EmailFactory _emailFactory;

    public EmailRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
        _userFactory = new UserFactory();
        _emailFactory = new EmailFactory();
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
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);
        var email = _emailFactory.ForUser(user);

        // Act
        await repository.AddAsync(email);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var saved = await verifyContext.Emails.FindAsync(email.Id);
        saved.ShouldNotBeNull();
        saved.Subject.ShouldBe(email.Subject);
        saved.FromAddress.ShouldBe(email.FromAddress);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEmail_ReturnsEmail()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);
        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        // Act
        var result = await repository.GetByIdAsync(email.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(email.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEmail_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncludesRecipientsAndAttachments()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        _emailFactory.WithRecipient(email, "cc@test.com", RecipientType.Cc);
        _emailFactory.WithAttachment(email, "test.pdf", "application/pdf");
        await email.AddToDbAsync(context);

        // Act
        var result = await repository.GetByIdWithDetailsAsync(email.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Recipients.Count.ShouldBe(2);
        result.Attachments.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsUserEmails()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);

        var user1 = _userFactory.CreateSequential();
        var user2 = _userFactory.CreateSequential();
        await user1.AddToDbAsync(context);
        await user2.AddToDbAsync(context);

        // Create emails for both users
        var emailsForUser1 = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.ForUser(user1))
            .ToList();
        var emailsForUser2 = Enumerable.Range(0, 2)
            .Select(_ => _emailFactory.ForUser(user2))
            .ToList();

        await emailsForUser1.AddToDbAsync(context);
        await emailsForUser2.AddToDbAsync(context);

        // Act
        var result = await repository.GetByUserIdAsync(user1.Id, 0, 10);

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldAllBe(e => e.Recipients.Any(r => r.UserId == user1.Id));
    }

    [Fact]
    public async Task GetByUserIdAsync_SupportsPagination()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var emails = Enumerable.Range(0, 25)
            .Select(_ => _emailFactory.ForUser(user))
            .ToList();
        await emails.AddToDbAsync(context);

        // Act
        var page1 = await repository.GetByUserIdAsync(user.Id, 0, 10);
        var page2 = await repository.GetByUserIdAsync(user.Id, 10, 10);
        var page3 = await repository.GetByUserIdAsync(user.Id, 20, 10);

        // Assert
        page1.Count.ShouldBe(10);
        page2.Count.ShouldBe(10);
        page3.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetCountByUserIdAsync_ReturnsCorrectCount()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var emails = Enumerable.Range(0, 15)
            .Select(_ => _emailFactory.ForUser(user))
            .ToList();
        await emails.AddToDbAsync(context);

        // Act
        var count = await repository.GetCountByUserIdAsync(user.Id);

        // Assert
        count.ShouldBe(15);
    }

    [Fact]
    public async Task GetUnreadCountByUserIdAsync_ReturnsCorrectUnreadCount()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        // Create mix of read and unread emails
        var unreadEmails = Enumerable.Range(0, 5)
            .Select(_ => _emailFactory.ForUser(user, isRead: false))
            .ToList();
        var readEmails = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.ForUser(user, isRead: true))
            .ToList();

        await unreadEmails.AddToDbAsync(context);
        await readEmails.AddToDbAsync(context);

        // Act
        var unreadCount = await repository.GetUnreadCountByUserIdAsync(user.Id);

        // Assert
        unreadCount.ShouldBe(5);
    }

    [Fact]
    public async Task SearchByUserIdAsync_FindsByQuery()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var matchingEmail = _emailFactory.ForUser(user);
        matchingEmail.Subject = "Important meeting tomorrow";
        var nonMatchingEmail = _emailFactory.ForUser(user);
        nonMatchingEmail.Subject = "Newsletter";

        await matchingEmail.AddToDbAsync(context);
        await nonMatchingEmail.AddToDbAsync(context);

        var filters = new EmailSearchFilters { Query = "meeting" };

        // Act
        var results = await repository.SearchByUserIdAsync(user.Id, filters, 0, 10);

        // Assert
        results.Count.ShouldBe(1);
        results.First().Subject.ShouldContain("meeting");
    }

    [Fact]
    public async Task SearchByUserIdAsync_FiltersByDateRange()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var oldEmail = _emailFactory.ForUser(user);
        oldEmail.ReceivedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var recentEmail = _emailFactory.ForUser(user);
        recentEmail.ReceivedAt = DateTimeOffset.UtcNow.AddDays(-1);

        await oldEmail.AddToDbAsync(context);
        await recentEmail.AddToDbAsync(context);

        var filters = new EmailSearchFilters
        {
            FromDate = DateTimeOffset.UtcNow.AddDays(-7)
        };

        // Act
        var results = await repository.SearchByUserIdAsync(user.Id, filters, 0, 10);

        // Assert
        results.Count.ShouldBe(1);
        results.First().Id.ShouldBe(recentEmail.Id);
    }

    [Fact]
    public async Task SearchByUserIdAsync_FiltersByHasAttachments()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var emailWithAttachment = _emailFactory.ForUser(user);
        _emailFactory.WithAttachment(emailWithAttachment);
        var emailWithoutAttachment = _emailFactory.ForUser(user);

        await emailWithAttachment.AddToDbAsync(context);
        await emailWithoutAttachment.AddToDbAsync(context);

        var filters = new EmailSearchFilters { HasAttachments = true };

        // Act
        var results = await repository.SearchByUserIdAsync(user.Id, filters, 0, 10);

        // Assert
        results.Count.ShouldBe(1);
        results.First().Id.ShouldBe(emailWithAttachment.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEmail()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);
        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        // Act
        await repository.DeleteAsync(email.Id);

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var deleted = await verifyContext.Emails.FindAsync(email.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task GetByMessageIdAsync_FindsByMessageId()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);
        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        // Act
        var result = await repository.GetByMessageIdAsync(email.MessageId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(email.Id);
    }

    [Fact]
    public async Task GetByThreadIdAsync_ReturnsAllThreadEmails()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var threadEmails = _emailFactory.CreateThread(3, user);
        await threadEmails.AddToDbAsync(context);
        var threadId = threadEmails.First().ThreadId!.Value;

        // Act
        var result = await repository.GetByThreadIdAsync(threadId, user.Id);

        // Assert
        result.Count.ShouldBe(4); // 1 parent + 3 replies
    }

    [Fact]
    public async Task LinkEmailsToUserAsync_LinksUnlinkedEmails()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.WithEmail("user@test.com");
        await user.AddToDbAsync(context);

        // Create email with unlinked recipient
        var email = _emailFactory.Create();
        _emailFactory.WithRecipient(email, "user@test.com", RecipientType.To, userId: null);
        await email.AddToDbAsync(context);

        // Act
        await repository.LinkEmailsToUserAsync(user.Id, new[] { "user@test.com" });

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var linkedEmail = await new EmailRepository(verifyContext).GetByIdWithDetailsAsync(email.Id);
        linkedEmail.ShouldNotBeNull();
        linkedEmail.Recipients.First().UserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetSentByUserIdAsync_ReturnsSentEmails()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EmailRepository(context);
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        var sentEmail = _emailFactory.SentByUser(user);
        _emailFactory.WithRecipient(sentEmail, "recipient@test.com");
        var receivedEmail = _emailFactory.ForUser(user);

        await sentEmail.AddToDbAsync(context);
        await receivedEmail.AddToDbAsync(context);

        // Act
        var result = await repository.GetSentByUserIdAsync(user.Id, 0, 10);

        // Assert
        result.Count.ShouldBe(1);
        result.First().SentByUserId.ShouldBe(user.Id);
    }
}
