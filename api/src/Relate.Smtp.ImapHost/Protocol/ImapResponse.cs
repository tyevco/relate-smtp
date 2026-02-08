namespace Relate.Smtp.ImapHost.Protocol;

/// <summary>
/// IMAP4rev2 response builder (RFC 9051)
/// </summary>
public static class ImapResponse
{
    // Response types
    public const string OkCode = "OK";
    public const string NoCode = "NO";
    public const string BadCode = "BAD";
    public const string ByeCode = "BYE";
    public const string PreauthCode = "PREAUTH";

    /// <summary>
    /// Server greeting on connection (untagged)
    /// </summary>
    public static string Greeting(string serverName) =>
        $"* OK {serverName} IMAP4rev2 server ready";

    /// <summary>
    /// Tagged OK response (command completed successfully)
    /// </summary>
    public static string TaggedOk(string tag, string message = "Completed") =>
        $"{tag} OK {message}";

    /// <summary>
    /// Tagged NO response (command failed, operational error)
    /// </summary>
    public static string TaggedNo(string tag, string message) =>
        $"{tag} NO {message}";

    /// <summary>
    /// Tagged BAD response (command error, protocol violation)
    /// </summary>
    public static string TaggedBad(string tag, string message) =>
        $"{tag} BAD {message}";

    /// <summary>
    /// Untagged OK response
    /// </summary>
    public static string UntaggedOk(string message) =>
        $"* OK {message}";

    /// <summary>
    /// Untagged BAD response (protocol error when no tag available)
    /// </summary>
    public static string UntaggedBad(string message) =>
        $"* BAD {message}";

    /// <summary>
    /// Untagged data response
    /// </summary>
    public static string Untagged(string data) =>
        $"* {data}";

    /// <summary>
    /// BYE response before closing
    /// </summary>
    public static string Bye(string message = "Logging out") =>
        $"* BYE {message}";

    /// <summary>
    /// CAPABILITY response
    /// </summary>
    public static string Capability() =>
        "* CAPABILITY IMAP4rev2 AUTH=PLAIN LITERAL+ ENABLE UNSELECT UIDPLUS CHILDREN";

    /// <summary>
    /// LIST response for a mailbox
    /// </summary>
    public static string List(string attributes, string delimiter, string mailboxName) =>
        $"* LIST ({attributes}) \"{delimiter}\" \"{mailboxName}\"";

    /// <summary>
    /// STATUS response for a mailbox
    /// </summary>
    public static string Status(string mailboxName, string statusData) =>
        $"* STATUS \"{mailboxName}\" ({statusData})";

    /// <summary>
    /// EXISTS response (message count)
    /// </summary>
    public static string Exists(int count) =>
        $"* {count} EXISTS";

    /// <summary>
    /// FLAGS response
    /// </summary>
    public static string Flags(string flags) =>
        $"* FLAGS ({flags})";

    /// <summary>
    /// Permanent flags response
    /// </summary>
    public static string PermanentFlags(string flags) =>
        $"* OK [PERMANENTFLAGS ({flags})] Permanent flags";

    /// <summary>
    /// UIDVALIDITY response
    /// </summary>
    public static string UidValidity(uint validity) =>
        $"* OK [UIDVALIDITY {validity}] UIDs valid";

    /// <summary>
    /// UIDNEXT response
    /// </summary>
    public static string UidNext(uint next) =>
        $"* OK [UIDNEXT {next}] Predicted next UID";

    /// <summary>
    /// FETCH response header
    /// </summary>
    public static string Fetch(int sequenceNumber, string data) =>
        $"* {sequenceNumber} FETCH ({data})";

    /// <summary>
    /// SEARCH response
    /// </summary>
    public static string Search(IEnumerable<uint> uids) =>
        $"* SEARCH {string.Join(" ", uids)}";

    /// <summary>
    /// EXPUNGE response
    /// </summary>
    public static string Expunge(int sequenceNumber) =>
        $"* {sequenceNumber} EXPUNGE";

    /// <summary>
    /// ENABLED response
    /// </summary>
    public static string Enabled(params string[] capabilities) =>
        $"* ENABLED {string.Join(" ", capabilities)}";
}
