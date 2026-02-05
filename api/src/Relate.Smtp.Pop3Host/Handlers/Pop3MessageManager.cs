using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.Pop3Host.Protocol;
using System.Text;

namespace Relate.Smtp.Pop3Host.Handlers;

public class Pop3MessageManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Pop3MessageManager> _logger;

    public Pop3MessageManager(IServiceProvider serviceProvider, ILogger<Pop3MessageManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<Pop3Message>> LoadMessagesAsync(Guid userId, CancellationToken ct)
    {
        using var activity = TelemetryConfiguration.Pop3ActivitySource.StartActivity("pop3.messages.load");
        activity?.SetTag("pop3.user_id", userId.ToString());

        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        // Load all emails for user (POP3 loads everything on auth)
        var emails = await emailRepo.GetByUserIdAsync(userId, 0, int.MaxValue, ct);

        var messages = new List<Pop3Message>();
        int messageNumber = 1;

        foreach (var email in emails.OrderBy(e => e.ReceivedAt))
        {
            messages.Add(new Pop3Message
            {
                MessageNumber = messageNumber++,
                EmailId = email.Id,
                SizeBytes = email.SizeBytes,
                UniqueId = email.MessageId
            });
        }

        activity?.SetTag("pop3.message_count", messages.Count);
        _logger.LogInformation("Loaded {Count} messages for user {UserId}", messages.Count, userId);
        return messages;
    }

    public async Task<string> RetrieveMessageAsync(Guid emailId, Guid userId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = await emailRepo.GetByIdWithDetailsAsync(emailId, ct);
        if (email == null)
        {
            _logger.LogWarning("Email not found: {EmailId}", emailId);
            throw new InvalidOperationException("Email not found");
        }

        // Mark as read for this user
        await context.EmailRecipients
            .Where(r => r.EmailId == emailId && r.UserId == userId && !r.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRead, true), ct);

        // Build RFC 822 message
        var message = BuildRfc822Message(email);

        // Record metrics
        ProtocolMetrics.Pop3MessagesRetrieved.Add(1);
        ProtocolMetrics.Pop3BytesSent.Add(message.Length);

        return message;
    }

    public async Task<string> RetrieveTopAsync(Guid emailId, int lines, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        var email = await emailRepo.GetByIdWithDetailsAsync(emailId, ct);
        if (email == null)
        {
            _logger.LogWarning("Email not found: {EmailId}", emailId);
            throw new InvalidOperationException("Email not found");
        }

        // Build full message
        var fullMessage = BuildRfc822Message(email);

        // Split into headers and body
        var parts = fullMessage.Split("\r\n\r\n", 2);
        if (parts.Length == 1)
            return fullMessage; // Only headers, no body

        var headers = parts[0];
        var body = parts[1];

        // Take first N lines of body
        var bodyLines = body.Split("\r\n").Take(lines);
        return headers + "\r\n\r\n" + string.Join("\r\n", bodyLines);
    }

    private string BuildRfc822Message(Email email)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(email.FromDisplayName, email.FromAddress));

        foreach (var recipient in email.Recipients)
        {
            var mailbox = new MailboxAddress(recipient.DisplayName, recipient.Address);
            switch (recipient.Type)
            {
                case RecipientType.To:
                    message.To.Add(mailbox);
                    break;
                case RecipientType.Cc:
                    message.Cc.Add(mailbox);
                    break;
                case RecipientType.Bcc:
                    message.Bcc.Add(mailbox);
                    break;
            }
        }

        message.Subject = email.Subject;
        message.MessageId = email.MessageId;
        message.Date = email.ReceivedAt;

        var builder = new BodyBuilder();
        if (!string.IsNullOrEmpty(email.TextBody))
            builder.TextBody = email.TextBody;
        if (!string.IsNullOrEmpty(email.HtmlBody))
            builder.HtmlBody = email.HtmlBody;

        foreach (var attachment in email.Attachments)
        {
            builder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType));
        }

        message.Body = builder.ToMessageBody();

        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public async Task ApplyDeletionsAsync(
        IEnumerable<Guid> emailIds,
        CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        foreach (var emailId in emailIds)
        {
            await emailRepo.DeleteAsync(emailId, ct);
            _logger.LogInformation("Deleted email: {EmailId}", emailId);
        }
    }
}
