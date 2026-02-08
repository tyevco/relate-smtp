using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;
using Relate.Smtp.Tests.Common.Helpers;
using Shouldly;

namespace Relate.Smtp.Tests.E2E.Protocol;

[Trait("Category", "E2E")]
[Trait("Category", "Protocol")]
[Trait("Protocol", "SMTP")]
public class SmtpProtocolTests : IAsyncLifetime
{
    private SmtpServerFixture _fixture = null!;
    private UserFactory _userFactory = null!;

    public async ValueTask InitializeAsync()
    {
        _fixture = new SmtpServerFixture();
        await _fixture.InitializeAsync();
        _userFactory = new UserFactory();
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
        var (user, plainTextKey) = _userFactory.WithApiKey("Test", SmtpApiKeyFactory.SmtpOnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new SmtpClient();

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

        using var client = new SmtpClient();

        // Act & Assert
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await Should.ThrowAsync<AuthenticationException>(
            () => client.AuthenticateAsync(user.Email, "wrong-password"));

        await client.DisconnectAsync(true);
    }

    [Fact]
    public async Task SendEmail_SimplePlainText_StoresInDatabase()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (sender, senderKey) = _userFactory.WithApiKey("Sender Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        await sender.AddToDbAsync(context);

        var recipient = _userFactory.WithEmail("recipient@test.local");
        await recipient.AddToDbAsync(context);

        using var client = new SmtpClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(sender.Email, senderKey);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", sender.Email));
        message.To.Add(new MailboxAddress("Recipient", recipient.Email));
        message.Subject = "Test Email via SMTP";
        message.Body = new TextPart("plain") { Text = "This is a test email body." };

        // Act
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        // Wait for email to be processed
        await TestHelpers.WaitForConditionAsync(async () =>
        {
            await using var ctx = _fixture.Postgres.CreateDbContext();
            return await ctx.Emails.AnyAsync(e => e.Subject == "Test Email via SMTP");
        }, timeoutMessage: "Email 'Test Email via SMTP' was not found in database");

        // Assert
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var email = verifyContext.Emails.FirstOrDefault(e => e.Subject == "Test Email via SMTP");
        email.ShouldNotBeNull();
        email.FromAddress.ShouldBe(sender.Email);
        email.TextBody!.ShouldContain("test email body");
    }

    [Fact]
    public async Task SendEmail_WithAttachment_StoresAttachmentInDatabase()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (sender, senderKey) = _userFactory.WithApiKey("Sender Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        await sender.AddToDbAsync(context);

        using var client = new SmtpClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(sender.Email, senderKey);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", sender.Email));
        message.To.Add(new MailboxAddress("Recipient", "recipient@test.local"));
        message.Subject = "Email with Attachment";

        var builder = new BodyBuilder
        {
            TextBody = "See attachment."
        };
        builder.Attachments.Add("test.txt", System.Text.Encoding.UTF8.GetBytes("File content"));
        message.Body = builder.ToMessageBody();

        // Act
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        // Wait for email to be processed
        await TestHelpers.WaitForConditionAsync(async () =>
        {
            await using var ctx = _fixture.Postgres.CreateDbContext();
            return await ctx.Emails.AnyAsync(e => e.Subject == "Email with Attachment");
        }, timeoutMessage: "Email 'Email with Attachment' was not found in database");

        // Assert
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var email = verifyContext.Emails
            .Where(e => e.Subject == "Email with Attachment")
            .FirstOrDefault();
        email.ShouldNotBeNull();

        var attachments = verifyContext.EmailAttachments.Where(a => a.EmailId == email.Id).ToList();
        attachments.Count.ShouldBe(1);
        attachments[0].FileName.ShouldBe("test.txt");
    }

    [Fact]
    public async Task SendEmail_LinksRecipientToExistingUser()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (sender, senderKey) = _userFactory.WithApiKey("Sender Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        await sender.AddToDbAsync(context);

        var recipient = _userFactory.WithEmail("known-recipient@test.local");
        await recipient.AddToDbAsync(context);

        using var client = new SmtpClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(sender.Email, senderKey);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", sender.Email));
        message.To.Add(new MailboxAddress("Known Recipient", recipient.Email));
        message.Subject = "Email to Known Recipient";
        message.Body = new TextPart("plain") { Text = "Hello!" };

        // Act
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        // Wait for email to be processed
        await TestHelpers.WaitForConditionAsync(async () =>
        {
            await using var ctx = _fixture.Postgres.CreateDbContext();
            return await ctx.EmailRecipients.AnyAsync(r => r.Address == recipient.Email);
        }, timeoutMessage: $"Email recipient '{recipient.Email}' was not found in database");

        // Assert
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var emailRecipients = verifyContext.EmailRecipients
            .Where(r => r.Address == recipient.Email)
            .ToList();
        emailRecipients.Count.ShouldBeGreaterThan(0);
        emailRecipients.First().UserId.ShouldBe(recipient.Id);
    }

    [Fact]
    public async Task SendEmail_TracksSentByUser()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (sender, senderKey) = _userFactory.WithApiKey("Sender Key", SmtpApiKeyFactory.SmtpOnlyScopes);
        await sender.AddToDbAsync(context);

        using var client = new SmtpClient();
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await client.AuthenticateAsync(sender.Email, senderKey);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", sender.Email));
        message.To.Add(new MailboxAddress("Recipient", "recipient@external.com"));
        message.Subject = "Tracking Test";
        message.Body = new TextPart("plain") { Text = "Track this." };

        // Act
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        // Wait for email to be processed
        await TestHelpers.WaitForConditionAsync(async () =>
        {
            await using var ctx = _fixture.Postgres.CreateDbContext();
            return await ctx.Emails.AnyAsync(e => e.Subject == "Tracking Test");
        }, timeoutMessage: "Email 'Tracking Test' was not found in database");

        // Assert
        await using var verifyContext = _fixture.Postgres.CreateDbContext();
        var email = verifyContext.Emails.FirstOrDefault(e => e.Subject == "Tracking Test");
        email.ShouldNotBeNull();
        email.SentByUserId.ShouldBe(sender.Id);
    }

    [Fact]
    public async Task SendEmail_WithoutSmtpScope_FailsAuthentication()
    {
        // Arrange
        await using var context = _fixture.Postgres.CreateDbContext();
        var (user, key) = _userFactory.WithApiKey("Pop3 Only", SmtpApiKeyFactory.Pop3OnlyScopes);
        await user.AddToDbAsync(context);

        using var client = new SmtpClient();

        // Act & Assert
        await client.ConnectAsync("localhost", _fixture.Port, SecureSocketOptions.None);
        await Should.ThrowAsync<AuthenticationException>(
            () => client.AuthenticateAsync(user.Email, key));

        await client.DisconnectAsync(true);
    }
}
