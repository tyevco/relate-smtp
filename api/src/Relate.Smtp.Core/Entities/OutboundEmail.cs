namespace Relate.Smtp.Core.Entities;

public class OutboundEmail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string? FromDisplayName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public OutboundEmailStatus Status { get; set; } = OutboundEmailStatus.Draft;

    // Threading support for replies/forwards
    public string? InReplyTo { get; set; }
    public string? References { get; set; }
    public Guid? OriginalEmailId { get; set; }

    // Generated on send
    public string? MessageId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? QueuedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public string? LastError { get; set; }

    public User User { get; set; } = null!;
    public ICollection<OutboundRecipient> Recipients { get; set; } = new List<OutboundRecipient>();
    public ICollection<OutboundAttachment> Attachments { get; set; } = new List<OutboundAttachment>();
    public ICollection<DeliveryLog> DeliveryLogs { get; set; } = new List<DeliveryLog>();
}
