namespace Relate.Smtp.Core.Entities;

public class EmailRecipient
{
    public Guid Id { get; set; }
    public Guid EmailId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public RecipientType Type { get; set; }
    public Guid? UserId { get; set; }
    public bool IsRead { get; set; }

    /// <summary>
    /// Navigation property to the parent email. Always populated by EF Core when loaded from database.
    /// </summary>
    public Email Email { get; set; } = null!;
    public User? User { get; set; }
}
