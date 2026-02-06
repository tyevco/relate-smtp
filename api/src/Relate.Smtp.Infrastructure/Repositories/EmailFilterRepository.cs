using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class EmailFilterRepository : IEmailFilterRepository
{
    private readonly AppDbContext _context;

    public EmailFilterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmailFilter>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailFilters
            .Include(f => f.AssignLabel)
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Priority)
            .ThenBy(f => f.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EmailFilter>> GetEnabledByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailFilters
            .Include(f => f.AssignLabel)
            .Where(f => f.UserId == userId && f.IsEnabled)
            .OrderBy(f => f.Priority)
            .ThenBy(f => f.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailFilter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailFilters
            .Include(f => f.AssignLabel)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailFilter> AddAsync(EmailFilter filter, CancellationToken cancellationToken = default)
    {
        _context.EmailFilters.Add(filter);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return filter;
    }

    public async Task UpdateAsync(EmailFilter filter, CancellationToken cancellationToken = default)
    {
        _context.EmailFilters.Update(filter);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = await _context.EmailFilters.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (filter != null)
        {
            _context.EmailFilters.Remove(filter);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
