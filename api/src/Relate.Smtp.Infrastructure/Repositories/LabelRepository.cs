using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class LabelRepository : ILabelRepository
{
    private readonly AppDbContext _context;

    public LabelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Label>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Labels
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Label?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Label> AddAsync(Label label, CancellationToken cancellationToken = default)
    {
        _context.Labels.Add(label);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return label;
    }

    public async Task UpdateAsync(Label label, CancellationToken cancellationToken = default)
    {
        _context.Labels.Update(label);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var label = await _context.Labels.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (label != null)
        {
            _context.Labels.Remove(label);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
