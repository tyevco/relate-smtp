namespace Relate.Smtp.Core.Entities;

public class OutboundAttachment
{
    public Guid Id { get; set; }
    public Guid OutboundEmailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public OutboundEmail OutboundEmail { get; set; } = null!;
}
