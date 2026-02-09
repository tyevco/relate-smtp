using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MimeKit;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/outbound")]
[Authorize]
[EnableRateLimiting("api")]
public class OutboundEmailsController : ControllerBase
{
    private readonly IOutboundEmailRepository _outboundEmailRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly UserProvisioningService _userProvisioningService;
    private readonly IDeliveryNotificationService _notificationService;
    private readonly IOptions<OutboundMailOptions> _outboundOptions;

    public OutboundEmailsController(
        IOutboundEmailRepository outboundEmailRepository,
        IEmailRepository emailRepository,
        UserProvisioningService userProvisioningService,
        IDeliveryNotificationService notificationService,
        IOptions<OutboundMailOptions> outboundOptions)
    {
        _outboundEmailRepository = outboundEmailRepository;
        _emailRepository = emailRepository;
        _userProvisioningService = userProvisioningService;
        _notificationService = notificationService;
        _outboundOptions = outboundOptions;
    }

    // --- Draft CRUD ---

    [HttpPost("drafts")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<OutboundEmailDetailDto>> CreateDraft(
        [FromBody] CreateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var draft = new OutboundEmail
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FromAddress = request.FromAddress,
            FromDisplayName = request.FromDisplayName,
            Subject = request.Subject,
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody,
            Status = OutboundEmailStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var r in request.Recipients)
        {
            draft.Recipients.Add(new OutboundRecipient
            {
                Id = Guid.NewGuid(),
                OutboundEmailId = draft.Id,
                Address = r.Address,
                DisplayName = r.DisplayName,
                Type = ParseRecipientType(r.Type),
                Status = OutboundRecipientStatus.Pending
            });
        }

        await _outboundEmailRepository.AddAsync(draft, cancellationToken);

        return CreatedAtAction(nameof(GetDraft), new { id = draft.Id }, draft.ToDetailDto());
    }

    [HttpGet("drafts")]
    public async Task<ActionResult<OutboundEmailListResponse>> GetDrafts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var skip = (page - 1) * pageSize;

        var drafts = await _outboundEmailRepository.GetDraftsByUserIdAsync(user.Id, skip, pageSize, cancellationToken);
        var totalCount = await _outboundEmailRepository.GetDraftsCountByUserIdAsync(user.Id, cancellationToken);

        var items = drafts.Select(d => d.ToListItemDto()).ToList();
        return Ok(new OutboundEmailListResponse(items, totalCount, page, pageSize));
    }

    [HttpGet("drafts/{id:guid}")]
    public async Task<ActionResult<OutboundEmailDetailDto>> GetDraft(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var draft = await _outboundEmailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (draft == null || draft.UserId != user.Id)
        {
            return NotFound();
        }

        return Ok(draft.ToDetailDto());
    }

    [HttpPut("drafts/{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<OutboundEmailDetailDto>> UpdateDraft(
        Guid id,
        [FromBody] UpdateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var draft = await _outboundEmailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (draft == null || draft.UserId != user.Id)
        {
            return NotFound();
        }

        if (draft.Status != OutboundEmailStatus.Draft)
        {
            return BadRequest(new { error = "Only drafts can be updated" });
        }

        if (request.FromAddress != null) draft.FromAddress = request.FromAddress;
        if (request.FromDisplayName != null) draft.FromDisplayName = request.FromDisplayName;
        if (request.Subject != null) draft.Subject = request.Subject;
        if (request.TextBody != null) draft.TextBody = request.TextBody;
        if (request.HtmlBody != null) draft.HtmlBody = request.HtmlBody;

        if (request.Recipients != null)
        {
            draft.Recipients.Clear();
            foreach (var r in request.Recipients)
            {
                draft.Recipients.Add(new OutboundRecipient
                {
                    Id = Guid.NewGuid(),
                    OutboundEmailId = draft.Id,
                    Address = r.Address,
                    DisplayName = r.DisplayName,
                    Type = ParseRecipientType(r.Type),
                    Status = OutboundRecipientStatus.Pending
                });
            }
        }

        await _outboundEmailRepository.UpdateAsync(draft, cancellationToken);

        return Ok(draft.ToDetailDto());
    }

    [HttpDelete("drafts/{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeleteDraft(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var draft = await _outboundEmailRepository.GetByIdAsync(id, cancellationToken);

        if (draft == null || draft.UserId != user.Id)
        {
            return NotFound();
        }

        if (draft.Status != OutboundEmailStatus.Draft)
        {
            return BadRequest(new { error = "Only drafts can be deleted" });
        }

        await _outboundEmailRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // --- Send ---

    [HttpPost("send")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<OutboundEmailDetailDto>> SendEmail(
        [FromBody] SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Recipients.Count == 0)
        {
            return BadRequest(new { error = "At least one recipient is required" });
        }

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var opts = _outboundOptions.Value;

        var email = new OutboundEmail
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FromAddress = request.FromAddress,
            FromDisplayName = request.FromDisplayName,
            Subject = request.Subject,
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody,
            Status = OutboundEmailStatus.Queued,
            MessageId = MimeUtils.GenerateMessageId(opts.SenderDomain),
            CreatedAt = DateTimeOffset.UtcNow,
            QueuedAt = DateTimeOffset.UtcNow
        };

        foreach (var r in request.Recipients)
        {
            email.Recipients.Add(new OutboundRecipient
            {
                Id = Guid.NewGuid(),
                OutboundEmailId = email.Id,
                Address = r.Address,
                DisplayName = r.DisplayName,
                Type = ParseRecipientType(r.Type),
                Status = OutboundRecipientStatus.Pending
            });
        }

        await _outboundEmailRepository.AddAsync(email, cancellationToken);

        return Ok(email.ToDetailDto());
    }

    [HttpPost("drafts/{id:guid}/send")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<OutboundEmailDetailDto>> SendDraft(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var draft = await _outboundEmailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (draft == null || draft.UserId != user.Id)
        {
            return NotFound();
        }

        if (draft.Status != OutboundEmailStatus.Draft)
        {
            return BadRequest(new { error = "Only drafts can be sent" });
        }

        if (draft.Recipients.Count == 0)
        {
            return BadRequest(new { error = "At least one recipient is required" });
        }

        var opts = _outboundOptions.Value;
        draft.Status = OutboundEmailStatus.Queued;
        draft.MessageId = MimeUtils.GenerateMessageId(opts.SenderDomain);
        draft.QueuedAt = DateTimeOffset.UtcNow;

        await _outboundEmailRepository.UpdateAsync(draft, cancellationToken);

        return Ok(draft.ToDetailDto());
    }

    // --- Reply & Forward ---

    [HttpPost("reply/{emailId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<OutboundEmailDetailDto>> Reply(
        Guid emailId,
        [FromBody] ReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var originalEmail = await _emailRepository.GetByIdWithDetailsAsync(emailId, cancellationToken);

        if (originalEmail == null)
        {
            return NotFound();
        }

        if (!originalEmail.Recipients.Any(r => r.UserId == user.Id))
        {
            return NotFound();
        }

        var opts = _outboundOptions.Value;

        var reply = new OutboundEmail
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FromAddress = user.Email,
            FromDisplayName = user.DisplayName,
            Subject = originalEmail.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
                ? originalEmail.Subject
                : $"Re: {originalEmail.Subject}",
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody,
            Status = OutboundEmailStatus.Queued,
            MessageId = MimeUtils.GenerateMessageId(opts.SenderDomain),
            InReplyTo = originalEmail.MessageId,
            References = BuildReferences(originalEmail),
            OriginalEmailId = originalEmail.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            QueuedAt = DateTimeOffset.UtcNow
        };

        // Add the original sender as recipient
        reply.Recipients.Add(new OutboundRecipient
        {
            Id = Guid.NewGuid(),
            OutboundEmailId = reply.Id,
            Address = originalEmail.FromAddress,
            DisplayName = originalEmail.FromDisplayName,
            Type = RecipientType.To,
            Status = OutboundRecipientStatus.Pending
        });

        if (request.ReplyAll)
        {
            // Add all original To/Cc recipients except the current user
            foreach (var recipient in originalEmail.Recipients)
            {
                if (string.Equals(recipient.Address, user.Email, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(recipient.Address, originalEmail.FromAddress, StringComparison.OrdinalIgnoreCase))
                    continue;

                reply.Recipients.Add(new OutboundRecipient
                {
                    Id = Guid.NewGuid(),
                    OutboundEmailId = reply.Id,
                    Address = recipient.Address,
                    DisplayName = recipient.DisplayName,
                    Type = recipient.Type == RecipientType.Bcc ? RecipientType.Cc : recipient.Type,
                    Status = OutboundRecipientStatus.Pending
                });
            }
        }

        await _outboundEmailRepository.AddAsync(reply, cancellationToken);

        return Ok(reply.ToDetailDto());
    }

    [HttpPost("forward/{emailId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<ActionResult<OutboundEmailDetailDto>> Forward(
        Guid emailId,
        [FromBody] ForwardRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var originalEmail = await _emailRepository.GetByIdWithDetailsAsync(emailId, cancellationToken);

        if (originalEmail == null)
        {
            return NotFound();
        }

        if (!originalEmail.Recipients.Any(r => r.UserId == user.Id))
        {
            return NotFound();
        }

        if (request.Recipients.Count == 0)
        {
            return BadRequest(new { error = "At least one recipient is required" });
        }

        var opts = _outboundOptions.Value;

        var forward = new OutboundEmail
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FromAddress = user.Email,
            FromDisplayName = user.DisplayName,
            Subject = originalEmail.Subject.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase)
                ? originalEmail.Subject
                : $"Fwd: {originalEmail.Subject}",
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody,
            Status = OutboundEmailStatus.Queued,
            MessageId = MimeUtils.GenerateMessageId(opts.SenderDomain),
            References = BuildReferences(originalEmail),
            OriginalEmailId = originalEmail.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            QueuedAt = DateTimeOffset.UtcNow
        };

        foreach (var r in request.Recipients)
        {
            forward.Recipients.Add(new OutboundRecipient
            {
                Id = Guid.NewGuid(),
                OutboundEmailId = forward.Id,
                Address = r.Address,
                DisplayName = r.DisplayName,
                Type = ParseRecipientType(r.Type),
                Status = OutboundRecipientStatus.Pending
            });
        }

        // Copy attachments from original email
        foreach (var attachment in originalEmail.Attachments)
        {
            forward.Attachments.Add(new OutboundAttachment
            {
                Id = Guid.NewGuid(),
                OutboundEmailId = forward.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes,
                Content = attachment.Content
            });
        }

        await _outboundEmailRepository.AddAsync(forward, cancellationToken);

        return Ok(forward.ToDetailDto());
    }

    // --- Outbox & Sent ---

    [HttpGet("outbox")]
    public async Task<ActionResult<OutboundEmailListResponse>> GetOutbox(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var skip = (page - 1) * pageSize;

        var emails = await _outboundEmailRepository.GetOutboxByUserIdAsync(user.Id, skip, pageSize, cancellationToken);
        var totalCount = await _outboundEmailRepository.GetOutboxCountByUserIdAsync(user.Id, cancellationToken);

        var items = emails.Select(e => e.ToListItemDto()).ToList();
        return Ok(new OutboundEmailListResponse(items, totalCount, page, pageSize));
    }

    [HttpGet("sent")]
    public async Task<ActionResult<OutboundEmailListResponse>> GetSent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var skip = (page - 1) * pageSize;

        var emails = await _outboundEmailRepository.GetSentByUserIdAsync(user.Id, skip, pageSize, cancellationToken);
        var totalCount = await _outboundEmailRepository.GetSentCountByUserIdAsync(user.Id, cancellationToken);

        var items = emails.Select(e => e.ToListItemDto()).ToList();
        return Ok(new OutboundEmailListResponse(items, totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OutboundEmailDetailDto>> GetOutboundEmail(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var email = await _outboundEmailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null || email.UserId != user.Id)
        {
            return NotFound();
        }

        return Ok(email.ToDetailDto());
    }

    // --- Helpers ---

    private static RecipientType ParseRecipientType(string type)
    {
        return type.ToUpperInvariant() switch
        {
            "TO" => RecipientType.To,
            "CC" => RecipientType.Cc,
            "BCC" => RecipientType.Bcc,
            _ => RecipientType.To
        };
    }

    private static string BuildReferences(Email originalEmail)
    {
        var references = new List<string>();

        if (!string.IsNullOrEmpty(originalEmail.References))
        {
            references.AddRange(originalEmail.References.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        if (!string.IsNullOrEmpty(originalEmail.MessageId))
        {
            references.Add(originalEmail.MessageId);
        }

        return string.Join(" ", references);
    }
}
