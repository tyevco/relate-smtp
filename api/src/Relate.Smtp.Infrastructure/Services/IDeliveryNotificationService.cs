namespace Relate.Smtp.Infrastructure.Services;

public interface IDeliveryNotificationService
{
    Task NotifyDeliveryStatusChangedAsync(Guid userId, Guid outboundEmailId, string status, CancellationToken ct = default);
}
