using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Core.Interfaces;

public interface IOutboundEmailRepository
{
    Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OutboundEmail?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboundEmail>> GetDraftsByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetDraftsCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboundEmail>> GetOutboxByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetOutboxCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboundEmail>> GetSentByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetSentCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboundEmail>> GetQueuedForDeliveryAsync(int batchSize, CancellationToken cancellationToken = default);
    Task AddAsync(OutboundEmail outboundEmail, CancellationToken cancellationToken = default);
    Task UpdateAsync(OutboundEmail outboundEmail, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddDeliveryLogAsync(DeliveryLog log, CancellationToken cancellationToken = default);
}
