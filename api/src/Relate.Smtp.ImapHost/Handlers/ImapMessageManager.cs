using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.ImapHost.Protocol;
using System.Security.Cryptography;
using System.Text;

namespace Relate.Smtp.ImapHost.Handlers;

public class ImapMessageManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImapMessageManager> _logger;
    private readonly ImapServerOptions _options;

    public ImapMessageManager(
        IServiceProvider serviceProvider,
        ILogger<ImapMessageManager> logger,
        IOptions<ImapServerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<List<ImapMessage>> LoadMessagesAsync(Guid userId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Load emails for user with configurable limit
        var emails = await emailRepo.GetByUserIdAsync(userId, 0, _options.MaxMessagesPerSession, ct);

        // Get the recipient records for this user to check read status
        var emailIds = emails.Select(e => e.Id).ToList();
        var recipients = await context.EmailRecipients
            .Where(r => emailIds.Contains(r.EmailId) && r.UserId == userId)
            .ToDictionaryAsync(r => r.EmailId, r => r, ct);

        var messages = new List<ImapMessage>();
        int sequenceNumber = 1;

        foreach (var email in emails.OrderBy(e => e.ReceivedAt))
        {
            var flags = ImapFlags.None;

            // Check if read
            if (recipients.TryGetValue(email.Id, out var recipient) && recipient.IsRead)
            {
                flags |= ImapFlags.Seen;
            }

            // Generate a stable UID from the email ID
            var uid = GenerateUidFromGuid(email.Id);

            messages.Add(new ImapMessage
            {
                SequenceNumber = sequenceNumber++,
                Uid = uid,
                EmailId = email.Id,
                SizeBytes = email.SizeBytes,
                MessageId = email.MessageId,
                Flags = flags,
                InternalDate = email.ReceivedAt
            });
        }

        _logger.LogInformation("Loaded {Count} messages for user {UserId}", messages.Count, userId);

        return messages;
    }

    /// <summary>
    /// Generate a stable UID from a GUID (takes first 4 bytes as uint)
    /// </summary>
    private static uint GenerateUidFromGuid(Guid id)
    {
        var bytes = id.ToByteArray();
        // Use the first 4 bytes, ensure it's positive by masking
        return BitConverter.ToUInt32(bytes, 0) & 0x7FFFFFFF;
    }

    public async Task<string> RetrieveMessageAsync(Guid emailId, Guid userId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = await emailRepo.GetByIdWithUserAccessAsync(emailId, userId, ct);
        if (email == null)
        {
            _logger.LogWarning("Email not found or access denied: {EmailId} for user {UserId}", emailId, userId);
            throw new UnauthorizedAccessException("Email not found or access denied");
        }

        // Build RFC 822 message
        var message = BuildRfc822Message(email);

        // Record metrics
        ProtocolMetrics.ImapMessagesRetrieved.Add(1);
        ProtocolMetrics.ImapBytesSent.Add(message.Length);

        return message;
    }

    public async Task MarkAsSeenAsync(Guid emailId, Guid userId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.EmailRecipients
            .Where(r => r.EmailId == emailId && r.UserId == userId && !r.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRead, true), ct);
    }

    public async Task<string> RetrieveHeadersAsync(Guid emailId, Guid userId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        var email = await emailRepo.GetByIdWithUserAccessAsync(emailId, userId, ct);
        if (email == null)
        {
            _logger.LogWarning("Email not found or access denied: {EmailId} for user {UserId}", emailId, userId);
            throw new UnauthorizedAccessException("Email not found or access denied");
        }

        // Build full message and extract headers
        var fullMessage = BuildRfc822Message(email);
        var headerEnd = fullMessage.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd == -1)
        {
            return fullMessage;
        }

        return fullMessage[..headerEnd];
    }

    public async Task<string> RetrieveBodyPartAsync(Guid emailId, Guid userId, int lines, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        var email = await emailRepo.GetByIdWithUserAccessAsync(emailId, userId, ct);
        if (email == null)
        {
            _logger.LogWarning("Email not found or access denied: {EmailId} for user {UserId}", emailId, userId);
            throw new UnauthorizedAccessException("Email not found or access denied");
        }

        var fullMessage = BuildRfc822Message(email);
        var headerEnd = fullMessage.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd == -1)
        {
            return fullMessage;
        }

        var headers = fullMessage[..headerEnd];
        var body = fullMessage[(headerEnd + 4)..];

        if (lines <= 0)
        {
            return headers + "\r\n\r\n";
        }

        var bodyLines = body.Split("\r\n").Take(lines);
        return headers + "\r\n\r\n" + string.Join("\r\n", bodyLines);
    }

    private string BuildRfc822Message(Email email)
    {
        #pragma warning disable CA2000 // Dispose objects before losing scope - MimeMessage doesn't implement IDisposable
        var message = new MimeMessage();
        #pragma warning restore CA2000
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

    public async Task ApplyDeletionsAsync(IEnumerable<Guid> emailIds, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        foreach (var emailId in emailIds)
        {
            await emailRepo.DeleteAsync(emailId, ct);
            _logger.LogInformation("Deleted email: {EmailId}", emailId);
        }
    }

    public async Task UpdateFlagsAsync(Guid emailId, Guid userId, ImapFlags flags, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var isRead = flags.HasFlag(ImapFlags.Seen);
        await context.EmailRecipients
            .Where(r => r.EmailId == emailId && r.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRead, isRead), ct);
    }
}
