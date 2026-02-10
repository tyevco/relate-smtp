using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByOidcSubjectAsync(string issuer, string subject, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailWithApiKeysAsync(string email, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserEmailAddress> AddEmailAddressAsync(UserEmailAddress address, CancellationToken cancellationToken = default);
    Task<UserEmailAddress?> GetEmailAddressByIdAsync(Guid addressId, CancellationToken cancellationToken = default);
    Task UpdateEmailAddressAsync(UserEmailAddress address, CancellationToken cancellationToken = default);
    Task RemoveEmailAddressAsync(Guid addressId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllEmailAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
}
