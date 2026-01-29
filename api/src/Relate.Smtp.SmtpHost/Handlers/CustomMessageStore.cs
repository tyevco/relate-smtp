using System.Buffers;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Relate.Smtp.SmtpHost.Handlers;

public class CustomMessageStore : MessageStore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomMessageStore> _logger;

    public CustomMessageStore(IServiceProvider serviceProvider, ILogger<CustomMessageStore> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream();
            foreach (var segment in buffer)
            {
                stream.Write(segment.Span);
            }
            stream.Position = 0;

            var message = await MimeMessage.LoadAsync(stream, cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Extract authenticated user ID from session context (set by CustomUserAuthenticator)
            Guid? sentByUserId = null;
            if (context.Properties.TryGetValue("AuthenticatedUserId", out var userId))
            {
                sentByUserId = (Guid)userId;
            }

            // Parse threading headers
            var inReplyTo = message.InReplyTo;
            var references = message.References?.ToString();

            // Determine thread ID: use existing thread or create new one
            Guid? threadId = null;
            if (!string.IsNullOrEmpty(inReplyTo))
            {
                // Try to find parent email by MessageId
                var parentEmail = await emailRepository.GetByMessageIdAsync(inReplyTo, cancellationToken);
                if (parentEmail != null)
                {
                    // Use parent's thread ID or parent's ID as thread
                    threadId = parentEmail.ThreadId ?? parentEmail.Id;
                }
            }

            var email = new Email
            {
                Id = Guid.NewGuid(),
                MessageId = message.MessageId ?? Guid.NewGuid().ToString(),
                FromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                FromDisplayName = message.From.Mailboxes.FirstOrDefault()?.Name,
                Subject = message.Subject ?? "(No Subject)",
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,
                ReceivedAt = DateTimeOffset.UtcNow,
                SizeBytes = buffer.Length,
                InReplyTo = inReplyTo,
                References = references,
                ThreadId = threadId,
                SentByUserId = sentByUserId
            };

            // Add recipients (and link to users if they exist)
            await AddRecipientsAsync(email, message.To, RecipientType.To, userRepository, cancellationToken);
            await AddRecipientsAsync(email, message.Cc, RecipientType.Cc, userRepository, cancellationToken);
            await AddRecipientsAsync(email, message.Bcc, RecipientType.Bcc, userRepository, cancellationToken);

            // Add attachments
            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart mimePart)
                {
                    using var attachmentStream = new MemoryStream();
                    await mimePart.Content.DecodeToAsync(attachmentStream, cancellationToken);

                    email.Attachments.Add(new EmailAttachment
                    {
                        Id = Guid.NewGuid(),
                        EmailId = email.Id,
                        FileName = mimePart.FileName ?? "attachment",
                        ContentType = mimePart.ContentType.MimeType,
                        SizeBytes = attachmentStream.Length,
                        Content = attachmentStream.ToArray()
                    });
                }
            }

            await emailRepository.AddAsync(email, cancellationToken);

            _logger.LogInformation("Email saved: {MessageId} from {From} to {Recipients}",
                email.MessageId,
                email.FromAddress,
                string.Join(", ", email.Recipients.Select(r => r.Address)));

            // Notify all recipient users via SignalR (if configured)
            var notificationService = scope.ServiceProvider.GetService<IEmailNotificationService>();
            if (notificationService != null)
            {
                var recipientUserIds = email.Recipients
                    .Where(r => r.UserId.HasValue)
                    .Select(r => r.UserId!.Value)
                    .Distinct();

                await notificationService.NotifyMultipleUsersNewEmailAsync(recipientUserIds, email, cancellationToken);
            }

            return SmtpResponse.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email");
            return SmtpResponse.TransactionFailed;
        }
    }

    private static async Task AddRecipientsAsync(
        Email email,
        InternetAddressList addresses,
        RecipientType type,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        foreach (var address in addresses.Mailboxes)
        {
            // Try to find a user with this email address
            var user = await userRepository.GetByEmailAsync(address.Address, cancellationToken);

            email.Recipients.Add(new EmailRecipient
            {
                Id = Guid.NewGuid(),
                EmailId = email.Id,
                Address = address.Address,
                DisplayName = address.Name,
                Type = type,
                UserId = user?.Id, // Link to user if found, otherwise null
                IsRead = false
            });
        }
    }
}
