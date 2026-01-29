using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class EmailsController : ControllerBase
{
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
        var emails = await _emailRepository.GetByUserIdAsync(user.Id, skip, pageSize, cancellationToken);
        var totalCount = await _emailRepository.GetCountByUserIdAsync(user.Id, cancellationToken);
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);

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
        if (!email.Recipients.Any(r => r.UserId == user.Id))
        {
            return NotFound();
        }

        return Ok(email.ToDetailDto(user.Id));
    }

    [HttpPatch("{id:guid}")]
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
    public async Task<IActionResult> DeleteEmail(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        if (!email.Recipients.Any(r => r.UserId == user.Id))
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

        if (!email.Recipients.Any(r => r.UserId == user.Id))
        {
            return NotFound();
        }

        var attachment = email.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return NotFound();
        }

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
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

        if (!email.Recipients.Any(r => r.UserId == user.Id))
        {
            return NotFound();
        }

        // Reconstruct MIME message
        var message = new MimeMessage();
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
        message.WriteTo(stream);
        var emlContent = stream.ToArray();

        var fileName = $"{email.Subject.Replace("/", "-").Replace("\\", "-")}_{email.ReceivedAt:yyyyMMdd}.eml";
        return File(emlContent, "message/rfc822", fileName);
    }

    [HttpGet("export/mbox")]
    public async Task<IActionResult> ExportAsMbox(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        // Get all emails for user (fetch large batch to get all emails)
        var allEmails = await _emailRepository.GetByUserIdAsync(user.Id, 1, 10000, cancellationToken);
        var emails = allEmails.AsEnumerable();

        if (fromDate.HasValue)
        {
            emails = emails.Where(e => e.ReceivedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            emails = emails.Where(e => e.ReceivedAt <= toDate.Value);
        }

        var emailList = emails.ToList();

        // Build MBOX format
        var mboxBuilder = new StringBuilder();

        foreach (var email in emailList)
        {
            // MBOX format starts with "From " line
            var fromLine = $"From {email.FromAddress} {email.ReceivedAt:ddd MMM dd HH:mm:ss yyyy}";
            mboxBuilder.AppendLine(fromLine);

            // Reconstruct MIME message
            var message = new MimeMessage();
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
            message.WriteTo(messageStream);
            var messageContent = Encoding.UTF8.GetString(messageStream.ToArray());

            // Escape "From " at the beginning of lines (MBOX format requirement)
            messageContent = messageContent.Replace("\nFrom ", "\n>From ");

            mboxBuilder.Append(messageContent);
            mboxBuilder.AppendLine();
            mboxBuilder.AppendLine();
        }

        var mboxContent = Encoding.UTF8.GetBytes(mboxBuilder.ToString());
        var fileName = $"emails_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mbox";

        return File(mboxContent, "application/mbox", fileName);
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
    public async Task<IActionResult> BulkMarkRead([FromBody] BulkEmailOperationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        foreach (var emailId in request.EmailIds)
        {
            var email = await _emailRepository.GetByIdWithDetailsAsync(emailId, cancellationToken);
            if (email != null && email.Recipients.Any(r => r.UserId == user.Id))
            {
                var recipient = email.Recipients.First(r => r.UserId == user.Id);
                recipient.IsRead = request.IsRead ?? true;
                await _emailRepository.UpdateAsync(email, cancellationToken);
            }
        }

        return NoContent();
    }

    [HttpPost("bulk/delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkEmailOperationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        foreach (var emailId in request.EmailIds)
        {
            var email = await _emailRepository.GetByIdAsync(emailId, cancellationToken);
            if (email != null)
            {
                await _emailRepository.DeleteAsync(emailId, cancellationToken);
                await _notificationService.NotifyEmailDeletedAsync(user.Id, emailId, cancellationToken);
            }
        }

        // Update unread count
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(user.Id, cancellationToken);
        await _notificationService.NotifyUnreadCountChangedAsync(user.Id, unreadCount, cancellationToken);

        return NoContent();
    }
}
