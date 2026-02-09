namespace Relate.Smtp.Core.Entities;

public class DeliveryLog
{
    public Guid Id { get; set; }
    public Guid OutboundEmailId { get; set; }
    public Guid? RecipientId { get; set; }
    public string RecipientAddress { get; set; } = string.Empty;
    public string? MxHost { get; set; }
    public int? SmtpStatusCode { get; set; }
    public string? SmtpResponse { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptNumber { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public TimeSpan? Duration { get; set; }

    public OutboundEmail OutboundEmail { get; set; } = null!;
    public OutboundRecipient? Recipient { get; set; }
}
