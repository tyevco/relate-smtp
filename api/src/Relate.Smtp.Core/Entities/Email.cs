namespace Relate.Smtp.Core.Entities;

public class Email
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string? FromDisplayName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public long SizeBytes { get; set; }

    // Threading fields
    public string? InReplyTo { get; set; }
    public string? References { get; set; }
    public Guid? ThreadId { get; set; }

    public ICollection<EmailRecipient> Recipients { get; set; } = new List<EmailRecipient>();
    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    public ICollection<EmailLabel> EmailLabels { get; set; } = new List<EmailLabel>();

    /// <summary>
    /// Computed property indicating if email has attachments.
    /// </summary>
    public bool HasAttachments => Attachments.Any();
}
