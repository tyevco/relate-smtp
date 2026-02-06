using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Models;

namespace Relate.Smtp.Core.Interfaces;

public interface IEmailRepository
{
    Task<Email?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Email?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets an email by ID only if the specified user has access (either as sender or recipient).
    /// Returns null if email not found or user doesn't have access.
    /// </summary>
    Task<Email?> GetByIdWithUserAccessAsync(Guid emailId, Guid userId, CancellationToken cancellationToken = default);
    Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Email>> GetByThreadIdAsync(Guid threadId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Email>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Email>> SearchByUserIdAsync(Guid userId, EmailSearchFilters filters, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetSearchCountByUserIdAsync(Guid userId, EmailSearchFilters filters, CancellationToken cancellationToken = default);
    Task<Email> AddAsync(Email email, CancellationToken cancellationToken = default);
    Task UpdateAsync(Email email, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task LinkEmailsToUserAsync(Guid userId, IEnumerable<string> emailAddresses, CancellationToken cancellationToken = default);

    // Sent mail methods
    Task<IReadOnlyList<Email>> GetSentByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetSentCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Email>> GetSentByUserIdAndFromAddressAsync(Guid userId, string fromAddress, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetSentCountByUserIdAndFromAddressAsync(Guid userId, string fromAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctSentFromAddressesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
