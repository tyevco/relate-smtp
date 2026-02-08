using MailKit.Net.Pop3;
using MailKit.Security;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;
using Shouldly;

namespace Relate.Smtp.Tests.E2E.Protocol;

[Trait("Category", "E2E")]
[Trait("Category", "Protocol")]
[Trait("Protocol", "POP3")]
public class Pop3ProtocolTests : IAsyncLifetime
{
    private Pop3ServerFixture _fixture = null!;
    private UserFactory _userFactory = null!;
    private EmailFactory _emailFactory = null!;

    public async ValueTask InitializeAsync()
    {
        _fixture = new Pop3ServerFixture();
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
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new Pop3Client();

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

        using var client = new Pop3Client();

        // Act & Assert
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await Should.ThrowAsync<AuthenticationException>(
            () => client.AuthenticateAsync(user.Email, "wrong-password"));

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task GetMessageCount_ReturnsCorrectCount()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        // Create emails for the user
        var emails = Enumerable.Range(0, 5)
            .Select(_ => _emailFactory.ForUser(user))
            .ToList();
        await emails.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var count = client.Count;

        // Assert
        count.ShouldBe(5);

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task GetMessage_RetrievesEmailContent()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        email.Subject = "POP3 Test Email";
        email.TextBody = "This is the email body for POP3 retrieval.";
        await email.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var message = await client.GetMessageAsync(0);

        // Assert
        message.ShouldNotBeNull();
        message.Subject.ShouldBe("POP3 Test Email");
        message.TextBody.ShouldContain("email body for POP3 retrieval");

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task GetMessageUids_ReturnsUniqueIds()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        var emails = Enumerable.Range(0, 3)
            .Select(_ => _emailFactory.ForUser(user))
            .ToList();
        await emails.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var uids = await client.GetMessageUidsAsync();

        // Assert
        uids.Count.ShouldBe(3);
        uids.Distinct().Count().ShouldBe(3); // All UIDs should be unique

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task DeleteMessage_MarksForDeletion()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        var initialCount = client.Count;
        initialCount.ShouldBe(1);

        // Act - Delete and disconnect (deletions applied on QUIT)
        await client.DeleteMessageAsync(0);
        await client.DisconnectAsync(true);

        // Assert - Reconnect and verify deletion
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var deletedEmail = await verifyContext.Emails.FindAsync(email.Id);
        deletedEmail.ShouldBeNull();
    }

    [Fact]
    public async Task GetMessageHeaders_ReturnsOnlyHeaders()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        email.Subject = "Headers Test";
        await email.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        var headers = await client.GetMessageHeadersAsync(0);

        // Assert
        headers.ShouldNotBeNull();
        headers[MimeKit.HeaderId.Subject].ShouldBe("Headers Test");

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Connect_WithoutPop3Scope_FailsAuthentication()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, key) = _userFactory.WithApiKey("SMTP Only", SmtpApiKeyFactory.SmtpOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new Pop3Client();

        // Act & Assert
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await Should.ThrowAsync<AuthenticationException>(
            () => client.AuthenticateAsync(user.Email, key));

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Noop_KeepsConnectionAlive()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Act
        await client.NoOpAsync();

        // Assert
        client.IsConnected.ShouldBeTrue();

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task Reset_UndoesDeleteMarks()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        var email = _emailFactory.ForUser(user);
        await email.AddToDbAsync(context);

        using var client = new Pop3Client();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(user.Email, plainTextKey);

        // Mark for deletion then reset
        await client.DeleteMessageAsync(0);
        await client.ResetAsync();

        // Disconnect without actually deleting
        await client.DisconnectAsync(true);

        // Assert - Email should still exist
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var stillExists = await verifyContext.Emails.FindAsync(email.Id);
        stillExists.ShouldNotBeNull();
    }
}
