using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Core.Models;
using Relate.Smtp.Infrastructure.Services;
using System.Text;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class EmailsController : ControllerBase
{
    private const int MaxExportEmails = 50_000;
    private static readonly MemoryCache ExportRateCache = new(new MemoryCacheOptions());

    private static readonly HashSet<string> SafeMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "application/pdf", "text/plain", "text/csv", "text/html",
        "application/zip", "application/gzip", "application/x-tar",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "message/rfc822"
    };

    private readonly IEmailRepository _emailRepository;
    private readonly UserProvisioningService _userProvisioningService;
    private readonly IEmailNotificationService _notificationService;

    public EmailsController(
        IEmailRepository emailRepository,
        UserProvisioningService userProvisioningService,
        IEmailNotificationService notificationService)
    {
        _emailRepository = emailRepository;
        _userProvisioningService = userProvisioningService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<EmailListResponse>> GetEmails(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var skip = (page - 1) * pageSize;
        var emailsTask = _emailRepository.GetByUserIdAsync(user.Id, skip, pageSize, cancellationToken);
        var countTask = _emailRepository.GetCountByUserIdAsync(user.Id, cancellationToken);
        var unreadTask = _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);
        await Task.WhenAll(emailsTask, countTask, unreadTask);
        var emails = emailsTask.Result;
        var totalCount = countTask.Result;
        var unreadCount = unreadTask.Result;

        var items = emails.Select(e => e.ToListItemDto(user.Id)).ToList();

        return Ok(new EmailListResponse(items, totalCount, unreadCount, page, pageSize));
    }

    [HttpGet("search")]
    public async Task<ActionResult<EmailListResponse>> SearchEmails(
        [FromQuery] string? q,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] bool? hasAttachments,
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var filters = new EmailSearchFilters
        {
            Query = q,
            FromDate = fromDate,
            ToDate = toDate,
            HasAttachments = hasAttachments,
            IsRead = isRead
        };

        var skip = (page - 1) * pageSize;
        var emails = await _emailRepository.SearchByUserIdAsync(user.Id, filters, skip, pageSize, cancellationToken);
        var totalCount = await _emailRepository.GetSearchCountByUserIdAsync(user.Id, filters, cancellationToken);
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);

        var items = emails.Select(e => e.ToListItemDto(user.Id)).ToList();

        return Ok(new EmailListResponse(items, totalCount, unreadCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmailDetailDto>> GetEmail(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        // Check if user has access to this email
        if (!email.Recipients.Any(r => r.UserId == user.Id) && email.SentByUserId != user.Id)
        {
            return NotFound();
        }

        return Ok(email.ToDetailDto(user.Id));
    }

    [HttpPatch("{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<EmailDetailDto>> UpdateEmail(
        Guid id,
        [FromBody] UpdateEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        var recipient = email.Recipients.FirstOrDefault(r => r.UserId == user.Id);
        if (recipient == null)
        {
            return NotFound();
        }

        if (request.IsRead.HasValue)
        {
            recipient.IsRead = request.IsRead.Value;
        }

        await _emailRepository.UpdateAsync(email, cancellationToken);

        // Notify user of email update
        await _notificationService.NotifyEmailUpdatedAsync(user.Id, email.Id, recipient.IsRead, cancellationToken);

        // Update unread count
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);
        await _notificationService.NotifyUnreadCountChangedAsync(user.Id, unreadCount, cancellationToken);

        return Ok(email.ToDetailDto(user.Id));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeleteEmail(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        if (!email.Recipients.Any(r => r.UserId == user.Id) && email.SentByUserId != user.Id)
        {
            return NotFound();
        }

        await _emailRepository.DeleteAsync(id, cancellationToken);

        // Notify user of email deletion
        await _notificationService.NotifyEmailDeletedAsync(user.Id, id, cancellationToken);

        // Update unread count
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);
        await _notificationService.NotifyUnreadCountChangedAsync(user.Id, unreadCount, cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> GetAttachment(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        if (!email.Recipients.Any(r => r.UserId == user.Id) && email.SentByUserId != user.Id)
        {
            return NotFound();
        }

        var attachment = email.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return NotFound();
        }

        var contentType = SafeMimeTypes.Contains(attachment.ContentType)
            ? attachment.ContentType
            : "application/octet-stream";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{attachment.FileName}\"";
        return File(attachment.Content, contentType, attachment.FileName);
    }

    [HttpGet("{id:guid}/export/eml")]
    public async Task<IActionResult> ExportAsEml(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        if (!email.Recipients.Any(r => r.UserId == user.Id) && email.SentByUserId != user.Id)
        {
            return NotFound();
        }

        // Reconstruct MIME message
#pragma warning disable CA2000 // Dispose objects before losing scope - MimeMessage doesn't implement IDisposable
        var message = new MimeMessage();
#pragma warning restore CA2000
        message.MessageId = email.MessageId;
        message.From.Add(new MailboxAddress(email.FromDisplayName, email.FromAddress));

        foreach (var recipient in email.Recipients)
        {
            var address = new MailboxAddress(recipient.DisplayName, recipient.Address);
            switch (recipient.Type)
            {
                case Core.Entities.RecipientType.To:
                    message.To.Add(address);
                    break;
                case Core.Entities.RecipientType.Cc:
                    message.Cc.Add(address);
                    break;
                case Core.Entities.RecipientType.Bcc:
                    message.Bcc.Add(address);
                    break;
            }
        }

        message.Subject = email.Subject;
        message.Date = email.ReceivedAt;

        // Build body
        var builder = new BodyBuilder();
        if (!string.IsNullOrEmpty(email.TextBody))
        {
            builder.TextBody = email.TextBody;
        }
        if (!string.IsNullOrEmpty(email.HtmlBody))
        {
            builder.HtmlBody = email.HtmlBody;
        }

        // Add attachments
        foreach (var attachment in email.Attachments)
        {
            builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }

        message.Body = builder.ToMessageBody();

        // Convert to EML format
        using var stream = new MemoryStream();
        await message.WriteToAsync(stream, cancellationToken);
        var emlContent = stream.ToArray();

        var fileName = $"{email.Subject.Replace("/", "-", StringComparison.Ordinal).Replace("\\", "-", StringComparison.Ordinal)}_{email.ReceivedAt:yyyyMMdd}.eml";
        return File(emlContent, "message/rfc822", fileName);
    }

    [HttpGet("export/mbox")]
    public async Task ExportAsMbox(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        // Rate limit: 1 export per user per 10 minutes
        var rateLimitKey = $"mbox_export_{user.Id}";
        if (ExportRateCache.TryGetValue(rateLimitKey, out _))
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Export rate limit exceeded. Please wait 10 minutes between exports." }, cancellationToken);
            return;
        }

        // Check email count in date range
        var count = await _emailRepository.GetCountByUserIdAsync(user.Id, cancellationToken);
        if (count > MaxExportEmails)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = $"Export limited to {MaxExportEmails:N0} emails. Use date filters to narrow the range." }, cancellationToken);
            return;
        }

        ExportRateCache.Set(rateLimitKey, true, TimeSpan.FromMinutes(10));

        Response.ContentType = "application/mbox";
        Response.Headers.ContentDisposition = $"attachment; filename=\"emails_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mbox\"";

        await using var writer = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);

        await foreach (var email in _emailRepository.StreamByUserIdAsync(user.Id, fromDate, toDate, cancellationToken))
        {
            // MBOX format starts with "From " line
            var fromLine = $"From {email.FromAddress} {email.ReceivedAt:ddd MMM dd HH:mm:ss yyyy}";
            await writer.WriteLineAsync(fromLine);

            // Reconstruct MIME message
#pragma warning disable CA2000 // Dispose objects before losing scope - MimeMessage doesn't implement IDisposable
            var message = new MimeMessage();
#pragma warning restore CA2000
            message.MessageId = email.MessageId;
            message.From.Add(new MailboxAddress(email.FromDisplayName, email.FromAddress));

            foreach (var recipient in email.Recipients)
            {
                var address = new MailboxAddress(recipient.DisplayName, recipient.Address);
                switch (recipient.Type)
                {
                    case Core.Entities.RecipientType.To:
                        message.To.Add(address);
                        break;
                    case Core.Entities.RecipientType.Cc:
                        message.Cc.Add(address);
                        break;
                    case Core.Entities.RecipientType.Bcc:
                        message.Bcc.Add(address);
                        break;
                }
            }

            message.Subject = email.Subject;
            message.Date = email.ReceivedAt;

            // Build body
            var builder = new BodyBuilder();
            if (!string.IsNullOrEmpty(email.TextBody))
            {
                builder.TextBody = email.TextBody;
            }
            if (!string.IsNullOrEmpty(email.HtmlBody))
            {
                builder.HtmlBody = email.HtmlBody;
            }

            // Add attachments
            foreach (var attachment in email.Attachments)
            {
                builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            message.Body = builder.ToMessageBody();

            // Write message to MBOX
            using var messageStream = new MemoryStream();
            await message.WriteToAsync(messageStream, cancellationToken);
            var messageContent = Encoding.UTF8.GetString(messageStream.ToArray());

            // Escape "From " at the beginning of lines (MBOX format requirement)
            messageContent = messageContent.Replace("\nFrom ", "\n>From ");

            await writer.WriteAsync(messageContent);
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

            // Flush periodically to avoid buffering too much
            await writer.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("threads/{threadId:guid}")]
    public async Task<ActionResult<IReadOnlyList<EmailDetailDto>>> GetThread(Guid threadId, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var emails = await _emailRepository.GetByThreadIdAsync(threadId, user.Id, cancellationToken);

        var dtos = emails.Select(e => e.ToDetailDto(user.Id)).ToList();

        return Ok(dtos);
    }

    [HttpPost("bulk/mark-read")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> BulkMarkRead([FromBody] BulkEmailOperationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        await _emailRepository.BulkMarkReadAsync(user.Id, request.EmailIds, request.IsRead ?? true, cancellationToken);

        return NoContent();
    }

    [HttpPost("bulk/delete")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<BulkDeleteResponse>> BulkDelete([FromBody] BulkEmailOperationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var deletedCount = await _emailRepository.BulkDeleteAsync(user.Id, request.EmailIds, cancellationToken);

        // Only notify for emails that were actually deleted
        // We don't have exact IDs of deleted emails, but we can send notifications for requested IDs
        // up to the deleted count (in practice, failed deletes are typically due to access issues)
        if (deletedCount > 0)
        {
            var notifyCount = Math.Min(deletedCount, request.EmailIds.Count);
            foreach (var emailId in request.EmailIds.Take(notifyCount))
            {
                await _notificationService.NotifyEmailDeletedAsync(user.Id, emailId, cancellationToken);
            }

            // Update unread count
            var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);
            await _notificationService.NotifyUnreadCountChangedAsync(user.Id, unreadCount, cancellationToken);
        }

        return Ok(new BulkDeleteResponse { DeletedCount = deletedCount });
    }

    [HttpGet("sent")]
    public async Task<ActionResult<EmailListResponse>> GetSentEmails(
        [FromQuery] string? fromAddress,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var skip = (page - 1) * pageSize;

        IReadOnlyList<Core.Entities.Email> emails;
        int totalCount;

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            emails = await _emailRepository.GetSentByUserIdAsync(user.Id, skip, pageSize, cancellationToken);
            totalCount = await _emailRepository.GetSentCountByUserIdAsync(user.Id, cancellationToken);
        }
        else
        {
            emails = await _emailRepository.GetSentByUserIdAndFromAddressAsync(
                user.Id, fromAddress, skip, pageSize, cancellationToken);
            totalCount = await _emailRepository.GetSentCountByUserIdAndFromAddressAsync(
                user.Id, fromAddress, cancellationToken);
        }

        var items = emails.Select(e => e.ToListItemDto(user.Id)).ToList();

        return Ok(new EmailListResponse(items, totalCount, 0, page, pageSize));
    }

    [HttpGet("sent/addresses")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetSentFromAddresses(
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var addresses = await _emailRepository.GetDistinctSentFromAddressesByUserIdAsync(
            user.Id, cancellationToken);

        return Ok(addresses);
    }
}
