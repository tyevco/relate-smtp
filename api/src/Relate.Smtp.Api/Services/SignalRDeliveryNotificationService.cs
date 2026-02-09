using Microsoft.AspNetCore.SignalR;
using Relate.Smtp.Api.Hubs;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Api.Services;

public class SignalRDeliveryNotificationService : IDeliveryNotificationService
{
    private readonly IHubContext<EmailHub> _hubContext;

    public SignalRDeliveryNotificationService(IHubContext<EmailHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyDeliveryStatusChangedAsync(Guid userId, Guid outboundEmailId, string status, CancellationToken ct = default)
    {
        var data = new
        {
            id = outboundEmailId,
            status
        };

        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("DeliveryStatusChanged", data, ct);
    }
}
