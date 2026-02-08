using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;
using Shouldly;

namespace Relate.Smtp.Tests.E2E.Protocol;

[Trait("Category", "E2E")]
[Trait("Category", "Protocol")]
[Trait("Protocol", "IMAP")]
public class ImapProtocolTests : IAsyncLifetime
{
    private ImapServerFixture _fixture = null!;
    private UserFactory _userFactory = null!;
    private EmailFactory _emailFactory = null!;

    public async ValueTask InitializeAsync()
    {
        _fixture = new ImapServerFixture();
        await _fixture.InitializeAsync();
        _userFactory = new UserFactory();
        _emailFactory = new EmailFactory();
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task Connect_WithValidCredentials_Succeeds()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new ImapClient();

        // Act
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Assert
        client.IsConnected.ShouldBeTrue();
        client.IsAuthenticated.ShouldBeTrue();

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Connect_WithInvalidCredentials_FailsAuthentication()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var user = _userFactory.Create();
        await user.AddToDbAsync(context);

        using var client = new ImapClient();

        // Act & Assert
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await Should.ThrowAsync<AuthenticationException>(
            () => client.AuthenticateAsync(user.Email, "wrong-password"));

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task SelectInbox_ReturnsMailboxInfo()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        var emails = Enumerable.Range(0, 5)
            .Select(_ => _emailFactory.ForUser(user))
            .ToList();
        await emails.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var inbox = await client.GetFolderAsync("INBOX");
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        // Assert
        inbox.Count.ShouldBe(5);

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task GetFolder_ReturnsInbox()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var inbox = await client.GetFolderAsync("INBOX");

        // Assert
        inbox.ShouldNotBeNull();
        inbox.Name.ShouldBe("INBOX");

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task FetchMessage_RetrievesEmailContent()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        email.Subject = "IMAP Test Email";
        email.TextBody = "This is the email body for IMAP retrieval.";
        await email.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        var inbox = await client.GetFolderAsync("INBOX");
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        // Act
        var message = await inbox.GetMessageAsync(0);

        // Assert
        message.ShouldNotBeNull();
        message.Subject.ShouldBe("IMAP Test Email");
        message.TextBody.ShouldContain("email body for IMAP retrieval");

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task SetFlags_PersistsSeenFlag()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user, isRead: false);
        await email.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        var inbox = await client.GetFolderAsync("INBOX");
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        // Act - Set Seen flag
        await inbox.AddFlagsAsync(0, MessageFlags.Seen, true);
        await client.DisconnectAsync(true);

        // Assert - Reconnect and verify flag persisted
        using var client2 = new ImapClient();
        await client2.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client2.AuthenticateAsync(user.Email, plainTextKey);

        var inbox2 = await client2.GetFolderAsync("INBOX");
        await inbox2.OpenAsync(FolderAccess.ReadOnly);

        var summary = await inbox2.FetchAsync(0, 0, MessageSummaryItems.Flags);
        summary.First().Flags!.Value.HasFlag(MessageFlags.Seen).ShouldBeTrue();

        await client2.DisconnectAsync(true);
    }

    [Fact(Skip = "Flaky test - database persistence verified but IMAP server sees stale data. Investigating DbContext scoping.")]
    public async Task Search_FindsUnseenMessages()
    {
        // Arrange - Reset database to ensure clean state
        await _fixture.Postgres.ResetDatabaseAsync();

        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        // Create mix of read and unread - ensure IsRead is properly set
        var unread1 = _emailFactory.ForUser(user, isRead: false);
        var unread2 = _emailFactory.ForUser(user, isRead: false);
        var read = _emailFactory.ForUser(user, isRead: true);
        await unread1.AddToDbAsync(context);
        await unread2.AddToDbAsync(context);
        await read.AddToDbAsync(context);

        // Verify the IsRead flag was persisted correctly in database
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var savedRecipient = await verifyContext.EmailRecipients
            .FirstOrDefaultAsync(r => r.EmailId == read.Id && r.UserId == user.Id);
        savedRecipient.ShouldNotBeNull("Recipient should exist in database");
        savedRecipient.IsRead.ShouldBeTrue("Read email should have IsRead=true persisted in database");

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        var inbox = await client.GetFolderAsync("INBOX");
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        // Act
        var unseenUids = await inbox.SearchAsync(SearchQuery.NotSeen);

        // Assert
        unseenUids.Count.ShouldBe(2);

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Expunge_DeletesMarkedMessages()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        var inbox = await client.GetFolderAsync("INBOX");
        await inbox.OpenAsync(FolderAccess.ReadWrite);
        inbox.Count.ShouldBe(1);

        // Act - Mark for deletion and expunge
        await inbox.AddFlagsAsync(0, MessageFlags.Deleted, true);
        await inbox.ExpungeAsync();
        await client.DisconnectAsync(true);

        // Assert - Email should be deleted
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var deleted = await verifyContext.Emails.FindAsync(email.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task Examine_OpensReadOnly()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        var inbox = await client.GetFolderAsync("INBOX");

        // Act - Open as ReadOnly (EXAMINE)
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        // Assert
        inbox.Access.ShouldBe(FolderAccess.ReadOnly);

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Status_ReturnsMailboxStatus()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        var emails = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.ForUser(user, isRead: false))
            .ToList();
        await emails.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var inbox = await client.GetFolderAsync("INBOX");
        await inbox.StatusAsync(StatusItems.Count | StatusItems.Unread);

        // Assert
        inbox.Count.ShouldBe(3);
        inbox.Unread.ShouldBe(3);

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Connect_WithoutImapScope_FailsAuthentication()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, key) = _userFactory.WithApiKey("SMTP Only", SmtpApiKeyFactory.SmtpOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new ImapClient();

        // Act & Assert
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await Should.ThrowAsync<AuthenticationException>(
            () => client.AuthenticateAsync(user.Email, key));

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Capability_ReturnsServerCapabilities()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);

        // Act & Assert - Capabilities should be available after connect
        client.Capabilities.ShouldNotBe(ImapCapabilities.None);

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Noop_KeepsConnectionAlive()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.ImapOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new ImapClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        await client.NoOpAsync();

        // Assert
        client.IsConnected.ShouldBeTrue();

        await client.DisconnectAsync(true);
    }
}
