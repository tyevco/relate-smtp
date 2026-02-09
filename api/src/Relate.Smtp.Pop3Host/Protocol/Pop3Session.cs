namespace Relate.Smtp.Pop3Host.Protocol;

public class Pop3Session
{
    /// <summary>
    /// Maximum number of messages that can be marked for deletion in a single session.
    /// Prevents unbounded memory growth in long-running sessions.
    /// </summary>
    public const int MaxDeletedMessages = 10000;

    public string ConnectionId { get; init; } = Guid.NewGuid().ToString();
    public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public string ClientIp { get; init; } = "unknown";

    public Pop3State State { get; set; } = Pop3State.Authorization;
    public string? Username { get; set; }
    public Guid? UserId { get; set; }

    public List<Pop3Message> Messages { get; set; } = new();
    public HashSet<int> DeletedMessages { get; set; } = new();

    public bool IsTimedOut(TimeSpan timeout) =>
        DateTime.UtcNow - LastActivityAt > timeout;

    /// <summary>
    /// Returns true if the deleted messages collection has reached its maximum size.
    /// </summary>
    public bool IsDeletedMessagesLimitReached => DeletedMessages.Count >= MaxDeletedMessages;
}

public class Pop3Message
{
    public int MessageNumber { get; set; }  // 1-based
    public Guid EmailId { get; set; }
    public long SizeBytes { get; set; }
    public string UniqueId { get; set; } = string.Empty;
}
