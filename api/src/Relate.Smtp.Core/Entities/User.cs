namespace Relate.Smtp.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string OidcSubject { get; set; } = string.Empty;
    public string OidcIssuer { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserEmailAddress> AdditionalAddresses { get; set; } = new List<UserEmailAddress>();
    public ICollection<EmailRecipient> ReceivedEmails { get; set; } = new List<EmailRecipient>();
    public ICollection<SmtpApiKey> SmtpApiKeys { get; set; } = new List<SmtpApiKey>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
    public ICollection<EmailLabel> EmailLabels { get; set; } = new List<EmailLabel>();
    public ICollection<EmailFilter> EmailFilters { get; set; } = new List<EmailFilter>();
    public UserPreference? Preference { get; set; }
}
