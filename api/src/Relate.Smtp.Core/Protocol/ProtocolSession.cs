namespace Relate.Smtp.Core.Protocol;

public abstract class ProtocolSession
{
    public string ConnectionId { get; init; } = Guid.NewGuid().ToString();
    public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public string ClientIp { get; init; } = "unknown";
    public string? Username { get; set; }
    public Guid? UserId { get; set; }

    public bool IsTimedOut(TimeSpan timeout) =>
        DateTime.UtcNow - LastActivityAt > timeout;
}
