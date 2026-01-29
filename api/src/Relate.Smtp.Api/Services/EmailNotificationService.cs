using Microsoft.AspNetCore.SignalR;
using Relate.Smtp.Api.Hubs;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Api.Services;

/// <summary>
/// SignalR implementation of email notification service with push notification support.
/// </summary>
public class SignalREmailNotificationService : IEmailNotificationService
{
    private readonly IHubContext<EmailHub> _hubContext;
    private readonly PushNotificationService _pushNotificationService;

    public SignalREmailNotificationService(
        IHubContext<EmailHub> hubContext,
        PushNotificationService pushNotificationService)
    {
        _hubContext = hubContext;
        _pushNotificationService = pushNotificationService;
    }

    public async Task NotifyNewEmailAsync(Guid userId, Email email, CancellationToken ct = default)
    {
        var emailData = new
        {
            id = email.Id,
            from = email.FromAddress,
            fromDisplay = email.FromDisplayName,
            subject = email.Subject,
            receivedAt = email.ReceivedAt,
            hasAttachments = email.HasAttachments
        };

        // Send SignalR notification
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("NewEmail", emailData, ct);

        // Send push notification
        await _pushNotificationService.SendNewEmailNotificationAsync(userId, email, ct);
    }

    public async Task NotifyEmailUpdatedAsync(Guid userId, Guid emailId, bool isRead, CancellationToken ct = default)
    {
        var updateData = new
        {
            id = emailId,
            isRead = isRead
        };

        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("EmailUpdated", updateData, ct);
    }

    public async Task NotifyEmailDeletedAsync(Guid userId, Guid emailId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("EmailDeleted", emailId, ct);
    }

    public async Task NotifyUnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("UnreadCountChanged", unreadCount, ct);
    }

    public async Task NotifyMultipleUsersNewEmailAsync(IEnumerable<Guid> userIds, Email email, CancellationToken ct = default)
    {
        var emailData = new
        {
            id = email.Id,
            from = email.FromAddress,
            fromDisplay = email.FromDisplayName,
            subject = email.Subject,
            receivedAt = email.ReceivedAt,
            hasAttachments = email.HasAttachments
        };

        var userIdList = userIds.ToList();

        // Send SignalR notifications
        var signalRTasks = userIdList.Select(userId =>
            _hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync("NewEmail", emailData, ct));

        // Send push notifications
        var pushTasks = userIdList.Select(userId =>
            _pushNotificationService.SendNewEmailNotificationAsync(userId, email, ct));

        await Task.WhenAll(signalRTasks.Concat(pushTasks));
    }
}
