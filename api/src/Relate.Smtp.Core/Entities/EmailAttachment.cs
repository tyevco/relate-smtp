namespace Relate.Smtp.Core.Entities;

public class EmailAttachment
{
    public Guid Id { get; set; }
    public Guid EmailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Navigation property to the parent email. Always populated by EF Core when loaded from database.
    /// </summary>
    public Email Email { get; set; } = null!;
}
