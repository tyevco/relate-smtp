namespace Relate.Smtp.Core.Entities;

public class UserEmailAddress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? VerificationToken { get; set; }
    public DateTimeOffset? VerificationTokenExpiresAt { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    /// <summary>
    /// Navigation property to the address owner. Always populated by EF Core when loaded from database.
    /// </summary>
    public User User { get; set; } = null!;
}
