using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relate.Smtp.Api.Authentication;
using Relate.Smtp.Api.Authorization;
using Relate.Smtp.Api.Extensions;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Core.Models;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/external/emails")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationExtensions.ApiKeyScheme)]
public class ExternalEmailsController : ControllerBase
{
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger<ExternalEmailsController> _logger;

    public ExternalEmailsController(
        IEmailRepository emailRepository,
        ILogger<ExternalEmailsController> logger)
    {
        _emailRepository = emailRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get received emails (inbox)
    /// </summary>
    [HttpGet]
    [RequireScope("api:read")]
    public async Task<ActionResult<EmailListResponse>> GetEmails(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var userId = GetUserId();
        var skip = (page - 1) * pageSize;

        var emails = await _emailRepository.GetByUserIdAsync(userId, skip, pageSize, cancellationToken);
        var totalCount = await _emailRepository.GetCountByUserIdAsync(userId, cancellationToken);
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(userId, cancellationToken);

        var items = emails.Select(e => e.ToListItemDto(userId)).ToList();

        return Ok(new EmailListResponse(items, totalCount, unreadCount, page, pageSize));
    }

    /// <summary>
    /// Search emails with filters
    /// </summary>
    [HttpGet("search")]
    [RequireScope("api:read")]
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

        var userId = GetUserId();
        var skip = (page - 1) * pageSize;

        var filters = new EmailSearchFilters
        {
            Query = q,
            FromDate = fromDate,
            ToDate = toDate,
            HasAttachments = hasAttachments,
            IsRead = isRead
        };

        var emails = await _emailRepository.SearchByUserIdAsync(userId, filters, skip, pageSize, cancellationToken);
        var totalCount = await _emailRepository.GetSearchCountByUserIdAsync(userId, filters, cancellationToken);
        var unreadCount = await _emailRepository.GetUnreadCountByUserIdAsync(userId, cancellationToken);

        var items = emails.Select(e => e.ToListItemDto(userId)).ToList();

        return Ok(new EmailListResponse(items, totalCount, unreadCount, page, pageSize));
    }

    /// <summary>
    /// Get sent emails
    /// </summary>
    [HttpGet("sent")]
    [RequireScope("api:read")]
    public async Task<ActionResult<EmailListResponse>> GetSentEmails(
        [FromQuery] string? fromAddress,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var userId = GetUserId();
        var skip = (page - 1) * pageSize;

        IReadOnlyList<Core.Entities.Email> emails;
        int totalCount;

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            emails = await _emailRepository.GetSentByUserIdAsync(userId, skip, pageSize, cancellationToken);
            totalCount = await _emailRepository.GetSentCountByUserIdAsync(userId, cancellationToken);
        }
        else
        {
            emails = await _emailRepository.GetSentByUserIdAndFromAddressAsync(
                userId, fromAddress, skip, pageSize, cancellationToken);
            totalCount = await _emailRepository.GetSentCountByUserIdAndFromAddressAsync(
                userId, fromAddress, cancellationToken);
        }

        var items = emails.Select(e => e.ToListItemDto(userId)).ToList();

        return Ok(new EmailListResponse(items, totalCount, 0, page, pageSize));
    }

    /// <summary>
    /// Get email by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequireScope("api:read")]
    public async Task<ActionResult<EmailDetailDto>> GetEmailById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        // Verify user has access to this email (either received or sent)
        var hasAccess = email.Recipients.Any(r => r.UserId == userId) || email.SentByUserId == userId;
        if (!hasAccess)
        {
            return NotFound(); // Don't reveal existence
        }

        return Ok(email.ToDetailDto(userId));
    }

    /// <summary>
    /// Mark email as read/unread
    /// </summary>
    [HttpPatch("{id:guid}")]
    [RequireScope("api:write")]
    public async Task<ActionResult<EmailDetailDto>> UpdateEmail(
        Guid id,
        [FromBody] UpdateEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var email = await _emailRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        // Verify user received this email
        var recipient = email.Recipients.FirstOrDefault(r => r.UserId == userId);
        if (recipient == null)
        {
            return NotFound();
        }

        if (request.IsRead.HasValue)
        {
            recipient.IsRead = request.IsRead.Value;
            await _emailRepository.UpdateAsync(email, cancellationToken);
        }

        return Ok(email.ToDetailDto(userId));
    }

    /// <summary>
    /// Delete email
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequireScope("api:write")]
    public async Task<ActionResult> DeleteEmail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var email = await _emailRepository.GetByIdAsync(id, cancellationToken);

        if (email == null)
        {
            return NotFound();
        }

        // Verify user has access
        var hasAccess = email.Recipients.Any(r => r.UserId == userId) || email.SentByUserId == userId;
        if (!hasAccess)
        {
            return NotFound();
        }

        await _emailRepository.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    private Guid GetUserId() => User.GetUserId();
}
