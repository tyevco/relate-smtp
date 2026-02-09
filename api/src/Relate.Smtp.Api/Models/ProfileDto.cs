using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Api.Models;

public record ProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    List<EmailAddressDto> AdditionalAddresses
);

public record EmailAddressDto(
    Guid Id,
    string Address,
    bool IsVerified,
    DateTimeOffset AddedAt
);

public record UpdateProfileRequest(
    string? DisplayName
);

public record AddEmailAddressRequest(
    string Address
);

public record VerifyEmailAddressRequest(
    string Code
);

public static class ProfileMappingExtensions
{
    public static ProfileDto ToDto(this User user)
    {
        return new ProfileDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.CreatedAt,
            user.LastLoginAt,
            user.AdditionalAddresses.Select(a => new EmailAddressDto(
                a.Id,
                a.Address,
                a.IsVerified,
                a.AddedAt
            )).ToList()
        );
    }
}
