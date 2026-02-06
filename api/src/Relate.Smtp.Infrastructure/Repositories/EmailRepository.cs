using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Core.Models;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly AppDbContext _context;

    public EmailRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Email?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Email?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Email?> GetByIdWithUserAccessAsync(Guid emailId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .Where(e => e.Id == emailId)
            .Where(e => e.SentByUserId == userId || e.Recipients.Any(r => r.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Email>> GetByThreadIdAsync(Guid threadId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .Where(e => (e.ThreadId == threadId || e.Id == threadId) &&
                        e.Recipients.Any(r => r.UserId == userId))
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Email>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Include(e => e.Recipients)
            .Where(e => e.Recipients.Any(r => r.UserId == userId))
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Where(e => e.Recipients.Any(r => r.UserId == userId))
            .CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailRecipients
            .Where(r => r.UserId == userId && !r.IsRead)
            .CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Email>> SearchByUserIdAsync(
        Guid userId,
        EmailSearchFilters filters,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(userId, filters);

        return await query
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetSearchCountByUserIdAsync(
        Guid userId,
        EmailSearchFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(userId, filters);
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<Email> BuildSearchQuery(Guid userId, EmailSearchFilters filters)
    {
        var query = _context.Emails
            .Include(e => e.Recipients)
            .Where(e => e.Recipients.Any(r => r.UserId == userId));

        // Full-text search across From, Subject, and Body
        if (!string.IsNullOrWhiteSpace(filters.Query))
        {
            var searchTerm = filters.Query.ToLower();
            query = query.Where(e =>
                e.FromAddress.ToLower().Contains(searchTerm) ||
                (e.FromDisplayName != null && e.FromDisplayName.ToLower().Contains(searchTerm)) ||
                (e.Subject != null && e.Subject.ToLower().Contains(searchTerm)) ||
                (e.TextBody != null && e.TextBody.ToLower().Contains(searchTerm)) ||
                (e.HtmlBody != null && e.HtmlBody.ToLower().Contains(searchTerm)));
        }

        // Date range filters
        if (filters.FromDate.HasValue)
        {
            query = query.Where(e => e.ReceivedAt >= filters.FromDate.Value);
        }

        if (filters.ToDate.HasValue)
        {
            query = query.Where(e => e.ReceivedAt <= filters.ToDate.Value);
        }

        // Attachment filter
        if (filters.HasAttachments.HasValue)
        {
            if (filters.HasAttachments.Value)
            {
                query = query.Where(e => e.Attachments.Any());
            }
            else
            {
                query = query.Where(e => !e.Attachments.Any());
            }
        }

        // Read/Unread filter
        if (filters.IsRead.HasValue)
        {
            var isRead = filters.IsRead.Value;
            query = query.Where(e => e.Recipients.Any(r => r.UserId == userId && r.IsRead == isRead));
        }

        return query;
    }

    public async Task<Email> AddAsync(Email email, CancellationToken cancellationToken = default)
    {
        _context.Emails.Add(email);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return email;
    }

    public async Task UpdateAsync(Email email, CancellationToken cancellationToken = default)
    {
        _context.Emails.Update(email);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (email != null)
        {
            _context.Emails.Remove(email);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task LinkEmailsToUserAsync(Guid userId, IEnumerable<string> emailAddresses, CancellationToken cancellationToken = default)
    {
        var addresses = emailAddresses.Select(a => a.ToLowerInvariant()).ToList();

        var recipients = await _context.EmailRecipients
            .Where(r => r.UserId == null && addresses.Contains(r.Address.ToLower()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var recipient in recipients)
        {
            recipient.UserId = userId;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Email>> GetSentByUserIdAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Include(e => e.Recipients)
            .Where(e => e.SentByUserId == userId)
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetSentCountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Where(e => e.SentByUserId == userId)
            .CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Email>> GetSentByUserIdAndFromAddressAsync(
        Guid userId,
        string fromAddress,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Include(e => e.Recipients)
            .Where(e => e.SentByUserId == userId && e.FromAddress.ToLower() == fromAddress.ToLower())
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetSentCountByUserIdAndFromAddressAsync(
        Guid userId,
        string fromAddress,
        CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Where(e => e.SentByUserId == userId && e.FromAddress.ToLower() == fromAddress.ToLower())
            .CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetDistinctSentFromAddressesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Emails
            .Where(e => e.SentByUserId == userId)
            .Select(e => e.FromAddress)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> BulkMarkReadAsync(
        Guid userId,
        IEnumerable<Guid> emailIds,
        bool isRead,
        CancellationToken cancellationToken = default)
    {
        var emailIdList = emailIds.ToList();
        return await _context.EmailRecipients
            .Where(r => emailIdList.Contains(r.EmailId) && r.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRead, isRead), cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> BulkDeleteAsync(
        Guid userId,
        IEnumerable<Guid> emailIds,
        CancellationToken cancellationToken = default)
    {
        var emailIdList = emailIds.ToList();
        return await _context.Emails
            .Where(e => emailIdList.Contains(e.Id))
            .Where(e => e.Recipients.Any(r => r.UserId == userId) || e.SentByUserId == userId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<Email> StreamByUserIdAsync(
        Guid userId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = _context.Emails
            .Include(e => e.Recipients)
            .Include(e => e.Attachments)
            .Where(e => e.Recipients.Any(r => r.UserId == userId))
            .OrderByDescending(e => e.ReceivedAt)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ReceivedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ReceivedAt <= toDate.Value);
        }

        await foreach (var email in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return email;
        }
    }
}
