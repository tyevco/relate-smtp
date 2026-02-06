using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relate.Smtp.Api.Authentication;
using Relate.Smtp.Api.Authorization;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Api.Controllers;

/// <summary>
/// Internal API for triggering notifications from other services (e.g., SMTP Host).
/// Requires API key authentication with 'internal' scope.
/// </summary>
[ApiController]
[Route("api/internal/notifications")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationExtensions.ApiKeyScheme)]
[RequireScope("internal")]
public class InternalNotificationsController : ControllerBase
{
    private readonly IEmailNotificationService _notificationService;

    public InternalNotificationsController(IEmailNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("new-email")]
    public async Task<IActionResult> NotifyNewEmail(
        [FromBody] NewEmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create a minimal Email object for notification purposes
        var email = new Email
        {
            Id = request.Email.Id,
            FromAddress = request.Email.From,
            FromDisplayName = request.Email.FromDisplay,
            Subject = request.Email.Subject,
            ReceivedAt = request.Email.ReceivedAt
        };

        // If there are attachments, add a placeholder (the actual data isn't needed for notifications)
        if (request.Email.HasAttachments)
        {
            email.Attachments.Add(new EmailAttachment { Id = Guid.NewGuid(), EmailId = email.Id, FileName = "attachment" });
        }

        await _notificationService.NotifyMultipleUsersNewEmailAsync(
            request.UserIds,
            email,
            cancellationToken);

        return Ok();
    }
}

public record NewEmailNotificationRequest(
    List<Guid> UserIds,
    EmailNotificationData Email);

public record EmailNotificationData(
    Guid Id,
    string From,
    string? FromDisplay,
    string Subject,
    DateTimeOffset ReceivedAt,
    bool HasAttachments);
