using System.Security.Claims;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Services;

public class UserProvisioningService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(
        IUserRepository userRepository,
        IEmailRepository emailRepository,
        ILogger<UserProvisioningService> logger)
    {
        _userRepository = userRepository;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User subject claim not found");

        var issuer = principal.FindFirstValue("iss")
            ?? throw new InvalidOperationException("User issuer claim not found");

        var existingUser = await _userRepository.GetByOidcSubjectAsync(issuer, subject, cancellationToken);

        if (existingUser != null)
        {
            existingUser.LastLoginAt = DateTimeOffset.UtcNow;
            await _userRepository.UpdateAsync(existingUser, cancellationToken);

            // Link any unlinked emails to this user (in case emails arrived after user was created)
            // Include both primary email and any additional addresses
            var userAddresses = new List<string> { existingUser.Email };
            userAddresses.AddRange(existingUser.AdditionalAddresses.Select(a => a.Address));
            await _emailRepository.LinkEmailsToUserAsync(existingUser.Id, userAddresses, cancellationToken);

            return existingUser;
        }

        // Create new user (JIT provisioning)
        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? throw new InvalidOperationException("User email claim not found");

        var displayName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name")
            ?? email;

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            OidcSubject = subject,
            OidcIssuer = issuer,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow
        };

        await _userRepository.AddAsync(newUser, cancellationToken);
        _logger.LogInformation("Created new user: {UserId} with email {Email}", newUser.Id, email);

        // Link existing emails to this user
        var addresses = new[] { email };
        await _emailRepository.LinkEmailsToUserAsync(newUser.Id, addresses, cancellationToken);

        return newUser;
    }
}
