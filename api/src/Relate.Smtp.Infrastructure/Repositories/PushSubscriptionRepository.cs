using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly AppDbContext _context;

    public PushSubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, cancellationToken);
    }

    public async Task<PushSubscription> AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default)
    {
        _context.PushSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.PushSubscriptions.FindAsync(new object[] { id }, cancellationToken);
        if (subscription != null)
        {
            _context.PushSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateLastUsedAtAsync(Guid id, DateTimeOffset lastUsedAt, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.PushSubscriptions.FindAsync(new object[] { id }, cancellationToken);
        if (subscription != null)
        {
            subscription.LastUsedAt = lastUsedAt;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
