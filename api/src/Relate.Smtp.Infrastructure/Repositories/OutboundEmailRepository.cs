using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class OutboundEmailRepository : IOutboundEmailRepository
{
    private readonly AppDbContext _context;

    public OutboundEmailRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OutboundEmail?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboundEmail>> GetDraftsByUserIdAsync(
        Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .Where(e => e.UserId == userId && e.Status == OutboundEmailStatus.Draft)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetDraftsCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .CountAsync(e => e.UserId == userId && e.Status == OutboundEmailStatus.Draft, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboundEmail>> GetOutboxByUserIdAsync(
        Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .Include(e => e.Recipients)
            .Where(e => e.UserId == userId &&
                (e.Status == OutboundEmailStatus.Queued || e.Status == OutboundEmailStatus.Sending))
            .OrderByDescending(e => e.QueuedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetOutboxCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .CountAsync(e => e.UserId == userId &&
                (e.Status == OutboundEmailStatus.Queued || e.Status == OutboundEmailStatus.Sending), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboundEmail>> GetSentByUserIdAsync(
        Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .Include(e => e.Recipients)
            .Where(e => e.UserId == userId &&
                (e.Status == OutboundEmailStatus.Sent || e.Status == OutboundEmailStatus.PartialFailure))
            .OrderByDescending(e => e.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetSentCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .CountAsync(e => e.UserId == userId &&
                (e.Status == OutboundEmailStatus.Sent || e.Status == OutboundEmailStatus.PartialFailure), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboundEmail>> GetQueuedForDeliveryAsync(
        int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundEmails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .Where(e => e.Status == OutboundEmailStatus.Queued &&
                (e.NextRetryAt == null || e.NextRetryAt <= DateTimeOffset.UtcNow))
            .OrderBy(e => e.QueuedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(OutboundEmail outboundEmail, CancellationToken cancellationToken = default)
    {
        _context.OutboundEmails.Add(outboundEmail);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(OutboundEmail outboundEmail, CancellationToken cancellationToken = default)
    {
        _context.OutboundEmails.Update(outboundEmail);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var email = await _context.OutboundEmails.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (email != null)
        {
            _context.OutboundEmails.Remove(email);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddDeliveryLogAsync(DeliveryLog log, CancellationToken cancellationToken = default)
    {
        _context.DeliveryLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
