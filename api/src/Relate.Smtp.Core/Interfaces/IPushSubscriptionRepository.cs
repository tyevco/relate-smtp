using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Core.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, Guid userId, CancellationToken cancellationToken = default);
    Task<PushSubscription> AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAtAsync(Guid id, DateTimeOffset lastUsedAt, CancellationToken cancellationToken = default);
}
