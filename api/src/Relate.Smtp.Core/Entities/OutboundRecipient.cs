namespace Relate.Smtp.Core.Entities;

public class OutboundRecipient
{
    public Guid Id { get; set; }
    public Guid OutboundEmailId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public RecipientType Type { get; set; }
    public OutboundRecipientStatus Status { get; set; } = OutboundRecipientStatus.Pending;
    public string? StatusMessage { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public OutboundEmail OutboundEmail { get; set; } = null!;
}
