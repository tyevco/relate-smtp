using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class EmailLabelRepository : IEmailLabelRepository
{
    private readonly AppDbContext _context;

    public EmailLabelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmailLabel>> GetByEmailIdAsync(Guid emailId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailLabels
            .Include(el => el.Label)
            .Where(el => el.EmailId == emailId)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Email>> GetEmailsByLabelIdAsync(Guid userId, Guid labelId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.EmailLabels
            .Where(el => el.UserId == userId && el.LabelId == labelId)
            .Include(el => el.Email)
                .ThenInclude(e => e!.Recipients)
            .Select(el => el.Email!)
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetEmailCountByLabelIdAsync(Guid userId, Guid labelId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailLabels
            .Where(el => el.UserId == userId && el.LabelId == labelId)
            .CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailLabel> AddAsync(EmailLabel emailLabel, CancellationToken cancellationToken = default)
    {
        _context.EmailLabels.Add(emailLabel);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return emailLabel;
    }

    public async Task DeleteAsync(Guid emailId, Guid labelId, CancellationToken cancellationToken = default)
    {
        var emailLabel = await _context.EmailLabels
            .FirstOrDefaultAsync(el => el.EmailId == emailId && el.LabelId == labelId, cancellationToken).ConfigureAwait(false);

        if (emailLabel != null)
        {
            _context.EmailLabels.Remove(emailLabel);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
