using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.AdditionalAddresses)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByOidcSubjectAsync(string issuer, string subject, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.AdditionalAddresses)
            .FirstOrDefaultAsync(u => u.OidcIssuer == issuer && u.OidcSubject == subject, cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.Users
            .Include(u => u.AdditionalAddresses)
            // Use ToLower() for PostgreSQL-compatible case-insensitive comparison
            // EF Core translates ToLower() to SQL LOWER() function
#pragma warning disable CA1304, CA1309, CA1311, CA1862
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == normalizedEmail ||
                u.AdditionalAddresses.Any(a => a.Address.ToLower() == normalizedEmail),
                cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1304, CA1309, CA1311, CA1862
    }

    public async Task<User?> GetByEmailWithApiKeysAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.Users
            .Include(u => u.SmtpApiKeys.Where(k => k.RevokedAt == null))
            // Use ToLower() for PostgreSQL-compatible case-insensitive comparison
            // EF Core translates ToLower() to SQL LOWER() function
#pragma warning disable CA1304, CA1309, CA1311, CA1862
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1304, CA1309, CA1311, CA1862
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTimeOffset.UtcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserEmailAddress> AddEmailAddressAsync(UserEmailAddress address, CancellationToken cancellationToken = default)
    {
        _context.UserEmailAddresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return address;
    }

    public async Task<UserEmailAddress?> GetEmailAddressByIdAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        return await _context.UserEmailAddresses.FindAsync([addressId], cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateEmailAddressAsync(UserEmailAddress address, CancellationToken cancellationToken = default)
    {
        _context.UserEmailAddresses.Update(address);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveEmailAddressAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        var address = await _context.UserEmailAddresses.FindAsync([addressId], cancellationToken).ConfigureAwait(false);
        if (address != null)
        {
            _context.UserEmailAddresses.Remove(address);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<string>> GetAllEmailAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.AdditionalAddresses)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
            return Array.Empty<string>();

        var addresses = new List<string> { user.Email };
        addresses.AddRange(user.AdditionalAddresses.Select(a => a.Address));
        return addresses;
    }
}
