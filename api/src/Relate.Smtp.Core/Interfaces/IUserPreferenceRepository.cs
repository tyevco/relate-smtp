using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Core.Interfaces;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPreference> UpsertAsync(UserPreference preference, CancellationToken cancellationToken = default);
}
