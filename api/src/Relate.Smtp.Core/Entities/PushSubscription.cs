namespace Relate.Smtp.Core.Entities;

/// <summary>
/// Represents a browser push notification subscription for a user.
/// </summary>
public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Endpoint { get; set; } = string.Empty;
    public string P256dhKey { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
