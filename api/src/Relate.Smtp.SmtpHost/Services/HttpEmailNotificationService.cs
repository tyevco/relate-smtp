using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.SmtpHost.Services;

/// <summary>
/// HTTP-based email notification service that calls the API to trigger SignalR notifications.
/// </summary>
public class HttpEmailNotificationService : IEmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpEmailNotificationService> _logger;

    public HttpEmailNotificationService(HttpClient httpClient, ILogger<HttpEmailNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifyNewEmailAsync(Guid userId, Email email, CancellationToken ct = default)
    {
        await NotifyMultipleUsersNewEmailAsync([userId], email, ct);
    }

    public async Task NotifyMultipleUsersNewEmailAsync(IEnumerable<Guid> userIds, Email email, CancellationToken ct = default)
    {
        try
        {
            // Create a simplified email object for the notification
            var emailData = new
            {
                id = email.Id,
                from = email.FromAddress,
                fromDisplay = email.FromDisplayName,
                subject = email.Subject,
                receivedAt = email.ReceivedAt,
                hasAttachments = email.Attachments.Any()
            };

            var payload = new
            {
                userIds = userIds.ToList(),
                email = emailData
            };

            var response = await _httpClient.PostAsJsonAsync("/api/internal/notifications/new-email", payload, ct);
            response.EnsureSuccessStatusCode();
        }
#pragma warning disable CA1031 // Do not catch general exception types - Notifications are non-critical, failures should not propagate
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Failed to send new email notification");
        }
    }

    public async Task NotifyEmailUpdatedAsync(Guid userId, Guid emailId, bool isRead, CancellationToken ct = default)
    {
        // Not called from SMTP host
        await Task.CompletedTask;
    }

    public async Task NotifyEmailDeletedAsync(Guid userId, Guid emailId, CancellationToken ct = default)
    {
        // Not called from SMTP host
        await Task.CompletedTask;
    }

    public async Task NotifyUnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken ct = default)
    {
        // Not called from SMTP host
        await Task.CompletedTask;
    }
}
