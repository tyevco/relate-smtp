using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly AppDbContext _context;

    public UserPreferenceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserPreference> UpsertAsync(UserPreference preference, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdAsync(preference.UserId, cancellationToken);

        if (existing == null)
        {
            _context.UserPreferences.Add(preference);
        }
        else
        {
            existing.Theme = preference.Theme;
            existing.DisplayDensity = preference.DisplayDensity;
            existing.EmailsPerPage = preference.EmailsPerPage;
            existing.DefaultSort = preference.DefaultSort;
            existing.ShowPreview = preference.ShowPreview;
            existing.GroupByDate = preference.GroupByDate;
            existing.DesktopNotifications = preference.DesktopNotifications;
            existing.EmailDigest = preference.EmailDigest;
            existing.DigestFrequency = preference.DigestFrequency;
            existing.DigestTime = preference.DigestTime;
            existing.UpdatedAt = preference.UpdatedAt;

            _context.UserPreferences.Update(existing);
            preference = existing;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return preference;
    }
}
