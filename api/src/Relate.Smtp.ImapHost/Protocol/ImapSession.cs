namespace Relate.Smtp.ImapHost.Protocol;

/// <summary>
/// Maintains state for an IMAP session
/// </summary>
public class ImapSession
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

    public ImapState State { get; set; } = ImapState.NotAuthenticated;
    public string? Username { get; set; }
    public Guid? UserId { get; set; }

    // Mailbox state (when Selected)
    public string? SelectedMailbox { get; set; }
    public bool SelectedReadOnly { get; set; }

    // Message list for the selected mailbox
    public List<ImapMessage> Messages { get; set; } = [];

    // Messages marked for deletion in this session
    public HashSet<uint> DeletedUids { get; set; } = [];

    // Enabled capabilities
    public HashSet<string> EnabledCapabilities { get; set; } = [];

    // UIDVALIDITY for the mailbox - set based on mailbox creation/modification time during SELECT
    public uint UidValidity { get; set; }

    public bool IsTimedOut(TimeSpan timeout) =>
        DateTime.UtcNow - LastActivityAt > timeout;

    /// <summary>
    /// Returns true if the deleted UIDs collection has reached its maximum size.
    /// </summary>
    public bool IsDeletedUidsLimitReached => DeletedUids.Count >= MaxDeletedMessages;
}

/// <summary>
/// Represents an email message in IMAP context
/// </summary>
public class ImapMessage
{
    /// <summary>
    /// Sequence number (1-based, can change)
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Unique identifier (persistent across sessions)
    /// </summary>
    public uint Uid { get; set; }

    /// <summary>
    /// Database email ID
    /// </summary>
    public Guid EmailId { get; set; }

    /// <summary>
    /// Message size in bytes
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Message-ID header
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Message flags
    /// </summary>
    public ImapFlags Flags { get; set; } = ImapFlags.None;

    /// <summary>
    /// Internal date (received date)
    /// </summary>
    public DateTimeOffset InternalDate { get; set; }
}

/// <summary>
/// IMAP message flags
/// </summary>
[Flags]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - This is a legitimate Flags enum for IMAP protocol
public enum ImapFlags
#pragma warning restore CA1711
{
    None = 0,
    Seen = 1,
    Answered = 2,
    Flagged = 4,
    Deleted = 8,
    Draft = 16
}

/// <summary>
/// Extension methods for ImapFlags
/// </summary>
public static class ImapFlagsExtensions
{
    public static string ToImapString(this ImapFlags flags)
    {
        var parts = new List<string>();

        if (flags.HasFlag(ImapFlags.Seen))
            parts.Add(@"\Seen");
        if (flags.HasFlag(ImapFlags.Answered))
            parts.Add(@"\Answered");
        if (flags.HasFlag(ImapFlags.Flagged))
            parts.Add(@"\Flagged");
        if (flags.HasFlag(ImapFlags.Deleted))
            parts.Add(@"\Deleted");
        if (flags.HasFlag(ImapFlags.Draft))
            parts.Add(@"\Draft");

        return string.Join(" ", parts);
    }

    public static ImapFlags ParseFlags(IEnumerable<string> flagStrings)
    {
        var flags = ImapFlags.None;

        foreach (var flag in flagStrings)
        {
            var normalized = flag.ToUpperInvariant();
            if (normalized == @"\SEEN")
                flags |= ImapFlags.Seen;
            else if (normalized == @"\ANSWERED")
                flags |= ImapFlags.Answered;
            else if (normalized == @"\FLAGGED")
                flags |= ImapFlags.Flagged;
            else if (normalized == @"\DELETED")
                flags |= ImapFlags.Deleted;
            else if (normalized == @"\DRAFT")
                flags |= ImapFlags.Draft;
        }

        return flags;
    }
}
