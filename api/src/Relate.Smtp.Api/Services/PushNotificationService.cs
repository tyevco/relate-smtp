using Microsoft.Extensions.Options;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using WebPush;

namespace Relate.Smtp.Api.Services;

public class PushNotificationService
{
    private readonly IPushSubscriptionRepository _subscriptionRepository;
    private readonly IOptions<PushOptions> _pushOptions;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IPushSubscriptionRepository subscriptionRepository,
        IOptions<PushOptions> pushOptions,
        ILogger<PushNotificationService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _pushOptions = pushOptions;
        _logger = logger;
    }

    public async Task SendNewEmailNotificationAsync(Guid userId, Email email, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        if (!subscriptions.Any())
        {
            return;
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = "New Email",
            body = $"From: {email.FromDisplayName ?? email.FromAddress}\nSubject: {email.Subject}",
            icon = "/icon.png",
            badge = "/badge.png",
            data = new
            {
                emailId = email.Id,
                url = "/"
            }
        });

        await SendNotificationsAsync(subscriptions, payload, cancellationToken);
    }

    private async Task SendNotificationsAsync(
        IEnumerable<Core.Entities.PushSubscription> subscriptions,
        string payload,
        CancellationToken cancellationToken)
    {
        var vapidSubject = _pushOptions.Value.VapidSubject;
        var vapidPublicKey = _pushOptions.Value.VapidPublicKey;
        var vapidPrivateKey = _pushOptions.Value.VapidPrivateKey;

        if (string.IsNullOrEmpty(vapidPublicKey) || string.IsNullOrEmpty(vapidPrivateKey))
        {
            _logger.LogWarning("VAPID keys not configured. Push notifications will not be sent.");
            return;
        }

        var webPushClient = new WebPushClient();

        foreach (var subscription in subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dhKey,
                    subscription.AuthKey);

                var vapidDetails = new VapidDetails(
                    vapidSubject,
                    vapidPublicKey,
                    vapidPrivateKey);

                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails, cancellationToken);

                // Update last used timestamp
                await _subscriptionRepository.UpdateLastUsedAtAsync(subscription.Id, DateTimeOffset.UtcNow, cancellationToken);
            }
            catch (WebPushException ex)
            {
                _logger.LogError(ex, "Failed to send push notification to subscription {SubscriptionId}", subscription.Id);

                // If subscription is gone (410), delete it
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    _logger.LogInformation("Removing expired subscription {SubscriptionId}", subscription.Id);
                    await _subscriptionRepository.DeleteAsync(subscription.Id, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending push notification to subscription {SubscriptionId}", subscription.Id);
            }
        }
    }
}

public class PushOptions
{
    public string VapidSubject { get; set; } = "mailto:admin@localhost";
    public string VapidPublicKey { get; set; } = string.Empty;
    public string VapidPrivateKey { get; set; } = string.Empty;
}
