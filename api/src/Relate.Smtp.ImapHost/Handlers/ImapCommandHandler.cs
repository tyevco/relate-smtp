using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.ImapHost.Protocol;
using System.Text;

namespace Relate.Smtp.ImapHost.Handlers;

public class ImapCommandHandler
{
    private readonly ILogger<ImapCommandHandler> _logger;
    private readonly ImapUserAuthenticator _authenticator;
    private readonly ImapMessageManager _messageManager;
    private readonly ImapServerOptions _options;

    public ImapCommandHandler(
        ILogger<ImapCommandHandler> logger,
        ImapUserAuthenticator authenticator,
        ImapMessageManager messageManager,
        IOptions<ImapServerOptions> options)
    {
        _logger = logger;
        _authenticator = authenticator;
        _messageManager = messageManager;
        _options = options.Value;
    }

    public async Task HandleSessionAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        // Use UTF8 without BOM - MailKit and other clients don't expect BOM in protocol greetings
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

        var session = new ImapSession();
        _logger.LogInformation("IMAP session started: {ConnectionId}", session.ConnectionId);

        // Send greeting
        await writer.WriteLineAsync(ImapResponse.Greeting(_options.ServerName));

        try
        {
            while (!ct.IsCancellationRequested && session.State != ImapState.Logout)
            {
                // Check for timeout
                if (session.IsTimedOut(_options.SessionTimeout))
                {
                    _logger.LogWarning("Session timeout: {ConnectionId}", session.ConnectionId);
                    await writer.WriteLineAsync(ImapResponse.Bye("Session timeout"));
                    break;
                }

                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                session.LastActivityAt = DateTime.UtcNow;
                _logger.LogDebug("Command received: {Command}", line);

                var command = ImapCommand.Parse(line);
                await ExecuteCommandAsync(command, session, writer, ct);

                if (session.State == ImapState.Logout)
                    break;
            }
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe") || ex.InnerException?.Message.Contains("Broken pipe") == true)
        {
            _logger.LogDebug("Client disconnected unexpectedly (broken pipe): {ConnectionId}", session.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session error: {ConnectionId}", session.ConnectionId);
        }
        finally
        {
            _logger.LogInformation("IMAP session ended: {ConnectionId}", session.ConnectionId);
        }
    }

    private async Task ExecuteCommandAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        using var activity = TelemetryConfiguration.ImapActivitySource.StartActivity($"imap.command.{command.Name.ToLowerInvariant()}");
        activity?.SetTag("imap.session_id", session.ConnectionId);
        activity?.SetTag("imap.command", command.Name);
        activity?.SetTag("imap.state", session.State.ToString());

        ProtocolMetrics.ImapCommands.Add(1, new KeyValuePair<string, object?>("command", command.Name));

        try
        {
            // Commands available in any state
            switch (command.Name)
            {
                case "CAPABILITY":
                    await HandleCapabilityAsync(command, writer);
                    return;
                case "NOOP":
                    await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "NOOP completed"));
                    return;
                case "LOGOUT":
                    await HandleLogoutAsync(command, session, writer, ct);
                    return;
                case "ENABLE":
                    await HandleEnableAsync(command, session, writer);
                    return;
            }

            // State-specific commands
            switch (session.State)
            {
                case ImapState.NotAuthenticated:
                    await HandleNotAuthenticatedCommandAsync(command, session, writer, ct);
                    break;
                case ImapState.Authenticated:
                    await HandleAuthenticatedCommandAsync(command, session, writer, ct);
                    break;
                case ImapState.Selected:
                    await HandleSelectedCommandAsync(command, session, writer, ct);
                    break;
                default:
                    await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Invalid state"));
                    break;
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            _logger.LogError(ex, "Command execution error: {Command}", command.Name);
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Internal server error"));
        }
    }

    private async Task HandleCapabilityAsync(ImapCommand command, StreamWriter writer)
    {
        await writer.WriteLineAsync(ImapResponse.Capability());
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "CAPABILITY completed"));
    }

    private async Task HandleLogoutAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        // Apply any pending deletions if in Selected state
        if (session.State == ImapState.Selected && session.DeletedUids.Count > 0)
        {
            var emailIds = session.Messages
                .Where(m => session.DeletedUids.Contains(m.Uid))
                .Select(m => m.EmailId)
                .ToList();

            if (emailIds.Count > 0)
            {
                await _messageManager.ApplyDeletionsAsync(emailIds, ct);
                _logger.LogInformation("Applied {Count} deletions on logout", emailIds.Count);
            }
        }

        await writer.WriteLineAsync(ImapResponse.Bye("Logging out"));
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "LOGOUT completed"));
        session.State = ImapState.Logout;
    }

    private async Task HandleEnableAsync(ImapCommand command, ImapSession session, StreamWriter writer)
    {
        if (command.Arguments.Length == 0)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "ENABLE requires capability names"));
            return;
        }

        var enabledCaps = new List<string>();
        foreach (var cap in command.Arguments)
        {
            var upperCap = cap.ToUpperInvariant();
            // Accept UTF8=ACCEPT for RFC 9051 compliance
            if (upperCap == "UTF8=ACCEPT")
            {
                session.EnabledCapabilities.Add(upperCap);
                enabledCaps.Add(upperCap);
            }
        }

        if (enabledCaps.Count > 0)
        {
            await writer.WriteLineAsync(ImapResponse.Enabled([.. enabledCaps]));
        }
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "ENABLE completed"));
    }

    private async Task HandleNotAuthenticatedCommandAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        switch (command.Name)
        {
            case "LOGIN":
                await HandleLoginAsync(command, session, writer, ct);
                break;
            default:
                await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Please authenticate first"));
                break;
        }
    }

    private async Task HandleLoginAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (command.Arguments.Length < 2)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "LOGIN requires username and password"));
            return;
        }

        var username = command.Arguments[0];
        var password = command.Arguments[1];

        var (authenticated, userId) = await _authenticator.AuthenticateAsync(username, password, ct);

        if (!authenticated || !userId.HasValue)
        {
            _logger.LogWarning("Authentication failed for: {Username}", username);
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Authentication failed"));
            return;
        }

        session.Username = username;
        session.UserId = userId.Value;
        session.State = ImapState.Authenticated;

        _logger.LogInformation("User authenticated: {Username}", username);
        await writer.WriteLineAsync(ImapResponse.Capability());
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "LOGIN completed"));
    }

    private async Task HandleAuthenticatedCommandAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        switch (command.Name)
        {
            case "SELECT":
                await HandleSelectAsync(command, session, writer, false, ct);
                break;
            case "EXAMINE":
                await HandleSelectAsync(command, session, writer, true, ct);
                break;
            case "LIST":
                await HandleListAsync(command, session, writer);
                break;
            case "STATUS":
                await HandleStatusAsync(command, session, writer, ct);
                break;
            default:
                await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Command not valid in Authenticated state"));
                break;
        }
    }

    private async Task HandleSelectAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        bool readOnly,
        CancellationToken ct)
    {
        if (command.Arguments.Length == 0)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, $"{command.Name} requires mailbox name"));
            return;
        }

        var mailboxName = command.Arguments[0].ToUpperInvariant();

        // Only support INBOX
        if (mailboxName != "INBOX")
        {
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Mailbox does not exist"));
            return;
        }

        // Load messages
        using var selectActivity = TelemetryConfiguration.ImapActivitySource.StartActivity("imap.mailbox.select");
        selectActivity?.SetTag("imap.mailbox", mailboxName);
        selectActivity?.SetTag("imap.read_only", readOnly);

        session.Messages = await _messageManager.LoadMessagesAsync(session.UserId!.Value, ct);
        session.SelectedMailbox = "INBOX";
        session.SelectedReadOnly = readOnly;
        session.DeletedUids.Clear();
        session.State = ImapState.Selected;

        selectActivity?.SetTag("imap.message_count", session.Messages.Count);

        // Send mailbox status
        await writer.WriteLineAsync(ImapResponse.Flags(@"\Seen \Answered \Flagged \Deleted \Draft"));
        await writer.WriteLineAsync(ImapResponse.PermanentFlags(@"\Seen \Answered \Flagged \Deleted \Draft \*"));
        await writer.WriteLineAsync(ImapResponse.Exists(session.Messages.Count));
        await writer.WriteLineAsync(ImapResponse.UidValidity(session.UidValidity));

        var nextUid = session.Messages.Count > 0
            ? session.Messages.Max(m => m.Uid) + 1
            : 1;
        await writer.WriteLineAsync(ImapResponse.UidNext(nextUid));

        var accessType = readOnly ? "READ-ONLY" : "READ-WRITE";
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, $"[{accessType}] {command.Name} completed"));
    }

    private async Task HandleListAsync(ImapCommand command, ImapSession session, StreamWriter writer)
    {
        // Return INBOX as the only mailbox
        await writer.WriteLineAsync(ImapResponse.List(@"\HasNoChildren", "/", "INBOX"));
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "LIST completed"));
    }

    private async Task HandleStatusAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (command.Arguments.Length < 2)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "STATUS requires mailbox and status items"));
            return;
        }

        var mailboxName = command.Arguments[0].ToUpperInvariant();
        if (mailboxName != "INBOX")
        {
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Mailbox does not exist"));
            return;
        }

        // Load messages to get counts
        var messages = await _messageManager.LoadMessagesAsync(session.UserId!.Value, ct);

        var statusItems = command.RawArguments.ToUpperInvariant();
        var statusParts = new List<string>();

        if (statusItems.Contains("MESSAGES"))
            statusParts.Add($"MESSAGES {messages.Count}");
        if (statusItems.Contains("UNSEEN"))
            statusParts.Add($"UNSEEN {messages.Count(m => !m.Flags.HasFlag(ImapFlags.Seen))}");
        if (statusItems.Contains("UIDNEXT"))
            statusParts.Add($"UIDNEXT {(messages.Count > 0 ? messages.Max(m => m.Uid) + 1 : 1)}");
        if (statusItems.Contains("UIDVALIDITY"))
            statusParts.Add($"UIDVALIDITY {session.UidValidity}");

        await writer.WriteLineAsync(ImapResponse.Status("INBOX", string.Join(" ", statusParts)));
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "STATUS completed"));
    }

    private async Task HandleSelectedCommandAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        switch (command.Name)
        {
            // Also allow authenticated commands in selected state
            case "SELECT":
                await HandleSelectAsync(command, session, writer, false, ct);
                break;
            case "EXAMINE":
                await HandleSelectAsync(command, session, writer, true, ct);
                break;
            case "LIST":
                await HandleListAsync(command, session, writer);
                break;
            case "STATUS":
                await HandleStatusAsync(command, session, writer, ct);
                break;
            // Selected-only commands
            case "FETCH":
                await HandleFetchAsync(command, session, writer, false, ct);
                break;
            case "STORE":
                await HandleStoreAsync(command, session, writer, false, ct);
                break;
            case "SEARCH":
                await HandleSearchAsync(command, session, writer, false);
                break;
            case "EXPUNGE":
                await HandleExpungeAsync(command, session, writer, ct);
                break;
            case "CLOSE":
                await HandleCloseAsync(command, session, writer, ct);
                break;
            case "UNSELECT":
                await HandleUnselectAsync(command, session, writer);
                break;
            case "UID":
                await HandleUidCommandAsync(command, session, writer, ct);
                break;
            default:
                await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Unknown command"));
                break;
        }
    }

    private async Task HandleUidCommandAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (command.Arguments.Length == 0)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "UID requires a subcommand"));
            return;
        }

        var subCommand = command.Arguments[0].ToUpperInvariant();
        var subArgs = command.Arguments.Skip(1).ToArray();

        switch (subCommand)
        {
            case "FETCH":
                await HandleFetchAsync(
                    new ImapCommand { Tag = command.Tag, Name = "FETCH", Arguments = subArgs, RawArguments = string.Join(" ", subArgs) },
                    session, writer, true, ct);
                break;
            case "STORE":
                await HandleStoreAsync(
                    new ImapCommand { Tag = command.Tag, Name = "STORE", Arguments = subArgs, RawArguments = string.Join(" ", subArgs) },
                    session, writer, true, ct);
                break;
            case "SEARCH":
                await HandleSearchAsync(
                    new ImapCommand { Tag = command.Tag, Name = "SEARCH", Arguments = subArgs, RawArguments = string.Join(" ", subArgs) },
                    session, writer, true);
                break;
            default:
                await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, $"Unknown UID subcommand: {subCommand}"));
                break;
        }
    }

    private async Task HandleFetchAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        bool useUid,
        CancellationToken ct)
    {
        if (command.Arguments.Length < 2)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "FETCH requires sequence set and items"));
            return;
        }

        var sequenceSet = command.Arguments[0];
        var fetchItems = command.RawArguments[(sequenceSet.Length + 1)..].Trim();

        var messages = ParseSequenceSet(sequenceSet, session.Messages, useUid);

        foreach (var msg in messages)
        {
            var fetchData = await BuildFetchDataAsync(msg, fetchItems, session.UserId!.Value, useUid, ct);
            await writer.WriteLineAsync(ImapResponse.Fetch(msg.SequenceNumber, fetchData));
        }

        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "FETCH completed"));
    }

    private async Task<string> BuildFetchDataAsync(
        ImapMessage msg,
        string fetchItems,
        Guid userId,
        bool includeUid,
        CancellationToken ct)
    {
        var parts = new List<string>();
        var upperItems = fetchItems.ToUpperInvariant();

        if (includeUid || upperItems.Contains("UID"))
        {
            parts.Add($"UID {msg.Uid}");
        }

        if (upperItems.Contains("FLAGS"))
        {
            parts.Add($"FLAGS ({msg.Flags.ToImapString()})");
        }

        if (upperItems.Contains("INTERNALDATE"))
        {
            parts.Add($"INTERNALDATE \"{msg.InternalDate:dd-MMM-yyyy HH:mm:ss zzzz}\"");
        }

        if (upperItems.Contains("RFC822.SIZE") || upperItems.Contains("SIZE"))
        {
            parts.Add($"RFC822.SIZE {msg.SizeBytes}");
        }

        if (upperItems.Contains("ENVELOPE"))
        {
            // Simplified envelope - would need full message parsing for complete implementation
            parts.Add("ENVELOPE NIL");
        }

        if (upperItems.Contains("BODY[]") || upperItems.Contains("RFC822"))
        {
            var content = await _messageManager.RetrieveMessageAsync(msg.EmailId, userId, ct);
            // Mark as seen if BODY[] (not BODY.PEEK[])
            if (!upperItems.Contains("PEEK"))
            {
                msg.Flags |= ImapFlags.Seen;
                await _messageManager.MarkAsSeenAsync(msg.EmailId, userId, ct);
            }
            parts.Add($"BODY[] {{{content.Length}}}\r\n{content}");
        }
        else if (upperItems.Contains("BODY.PEEK[]"))
        {
            var content = await _messageManager.RetrieveMessageAsync(msg.EmailId, userId, ct);
            parts.Add($"BODY[] {{{content.Length}}}\r\n{content}");
        }
        else if (upperItems.Contains("BODY[HEADER]") || upperItems.Contains("BODY.PEEK[HEADER]"))
        {
            var headers = await _messageManager.RetrieveHeadersAsync(msg.EmailId, userId, ct);
            parts.Add($"BODY[HEADER] {{{headers.Length}}}\r\n{headers}");
        }

        return string.Join(" ", parts);
    }

    private async Task HandleStoreAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        bool useUid,
        CancellationToken ct)
    {
        if (session.SelectedReadOnly)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Mailbox is read-only"));
            return;
        }

        if (command.Arguments.Length < 3)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "STORE requires sequence set, data item, and value"));
            return;
        }

        var sequenceSet = command.Arguments[0];
        var dataItem = command.Arguments[1].ToUpperInvariant();
        var flagsStr = command.RawArguments[(sequenceSet.Length + 1 + command.Arguments[1].Length)..].Trim();

        // Parse flags from the value (e.g., "(\Seen \Flagged)")
        var flags = ParseFlagsFromString(flagsStr);
        var messages = ParseSequenceSet(sequenceSet, session.Messages, useUid);

        var silent = dataItem.Contains(".SILENT");

        foreach (var msg in messages)
        {
            if (dataItem.StartsWith("+FLAGS"))
            {
                msg.Flags |= flags;
            }
            else if (dataItem.StartsWith("-FLAGS"))
            {
                msg.Flags &= ~flags;
            }
            else if (dataItem.StartsWith("FLAGS"))
            {
                msg.Flags = flags;
            }

            // Track deletions
            if (msg.Flags.HasFlag(ImapFlags.Deleted))
            {
                session.DeletedUids.Add(msg.Uid);
            }
            else
            {
                session.DeletedUids.Remove(msg.Uid);
            }

            // Update in database
            await _messageManager.UpdateFlagsAsync(msg.EmailId, session.UserId!.Value, msg.Flags, ct);

            if (!silent)
            {
                var uidPart = useUid ? $"UID {msg.Uid} " : "";
                await writer.WriteLineAsync(ImapResponse.Fetch(msg.SequenceNumber, $"{uidPart}FLAGS ({msg.Flags.ToImapString()})"));
            }
        }

        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "STORE completed"));
    }

    private static ImapFlags ParseFlagsFromString(string flagsStr)
    {
        var flags = ImapFlags.None;
        var upper = flagsStr.ToUpperInvariant();

        if (upper.Contains(@"\SEEN")) flags |= ImapFlags.Seen;
        if (upper.Contains(@"\ANSWERED")) flags |= ImapFlags.Answered;
        if (upper.Contains(@"\FLAGGED")) flags |= ImapFlags.Flagged;
        if (upper.Contains(@"\DELETED")) flags |= ImapFlags.Deleted;
        if (upper.Contains(@"\DRAFT")) flags |= ImapFlags.Draft;

        return flags;
    }

    private async Task HandleSearchAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        bool useUid)
    {
        var criteria = command.RawArguments.ToUpperInvariant();
        var results = new List<uint>();

        foreach (var msg in session.Messages)
        {
            if (session.DeletedUids.Contains(msg.Uid) && !criteria.Contains("DELETED"))
                continue;

            var matches = true;

            if (criteria.Contains("ALL"))
            {
                // Match all
            }
            else
            {
                if (criteria.Contains("SEEN") && !msg.Flags.HasFlag(ImapFlags.Seen))
                    matches = false;
                if (criteria.Contains("UNSEEN") && msg.Flags.HasFlag(ImapFlags.Seen))
                    matches = false;
                if (criteria.Contains("DELETED") && !msg.Flags.HasFlag(ImapFlags.Deleted))
                    matches = false;
                if (criteria.Contains("FLAGGED") && !msg.Flags.HasFlag(ImapFlags.Flagged))
                    matches = false;
                if (criteria.Contains("UNFLAGGED") && msg.Flags.HasFlag(ImapFlags.Flagged))
                    matches = false;
            }

            if (matches)
            {
                results.Add(useUid ? msg.Uid : (uint)msg.SequenceNumber);
            }
        }

        await writer.WriteLineAsync(ImapResponse.Search(results));
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "SEARCH completed"));
    }

    private async Task HandleExpungeAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (session.SelectedReadOnly)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Mailbox is read-only"));
            return;
        }

        var deletedMessages = session.Messages
            .Where(m => session.DeletedUids.Contains(m.Uid))
            .OrderByDescending(m => m.SequenceNumber)
            .ToList();

        var emailIds = deletedMessages.Select(m => m.EmailId).ToList();

        if (emailIds.Count > 0)
        {
            await _messageManager.ApplyDeletionsAsync(emailIds, ct);

            // Send EXPUNGE responses in descending order
            foreach (var msg in deletedMessages)
            {
                await writer.WriteLineAsync(ImapResponse.Expunge(msg.SequenceNumber));
                session.Messages.Remove(msg);
            }

            // Renumber remaining messages
            var seqNum = 1;
            foreach (var msg in session.Messages.OrderBy(m => m.InternalDate))
            {
                msg.SequenceNumber = seqNum++;
            }

            session.DeletedUids.Clear();
            _logger.LogInformation("Expunged {Count} messages", emailIds.Count);
        }

        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "EXPUNGE completed"));
    }

    private async Task HandleCloseAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer,
        CancellationToken ct)
    {
        // CLOSE expunges and returns to Authenticated state
        if (!session.SelectedReadOnly && session.DeletedUids.Count > 0)
        {
            var emailIds = session.Messages
                .Where(m => session.DeletedUids.Contains(m.Uid))
                .Select(m => m.EmailId)
                .ToList();

            if (emailIds.Count > 0)
            {
                await _messageManager.ApplyDeletionsAsync(emailIds, ct);
                _logger.LogInformation("Expunged {Count} messages on CLOSE", emailIds.Count);
            }
        }

        session.SelectedMailbox = null;
        session.Messages.Clear();
        session.DeletedUids.Clear();
        session.State = ImapState.Authenticated;

        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "CLOSE completed"));
    }

    private async Task HandleUnselectAsync(
        ImapCommand command,
        ImapSession session,
        StreamWriter writer)
    {
        // UNSELECT returns to Authenticated state without expunging
        session.SelectedMailbox = null;
        session.Messages.Clear();
        session.DeletedUids.Clear();
        session.State = ImapState.Authenticated;

        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "UNSELECT completed"));
    }

    private static List<ImapMessage> ParseSequenceSet(
        string sequenceSet,
        List<ImapMessage> messages,
        bool useUid)
    {
        var results = new List<ImapMessage>();

        // Handle * (all messages)
        if (sequenceSet == "*")
        {
            return new List<ImapMessage>(messages);
        }

        // Handle ranges like "1:*" or "1:10" or "1,3,5"
        var parts = sequenceSet.Split(',');
        foreach (var part in parts)
        {
            if (part.Contains(':'))
            {
                var range = part.Split(':');
                var start = range[0] == "*" ? (useUid ? messages.Max(m => (int)m.Uid) : messages.Count) : int.Parse(range[0]);
                var end = range[1] == "*" ? (useUid ? messages.Max(m => (int)m.Uid) : messages.Count) : int.Parse(range[1]);

                if (start > end)
                {
                    (start, end) = (end, start);
                }

                foreach (var msg in messages)
                {
                    var value = useUid ? (int)msg.Uid : msg.SequenceNumber;
                    if (value >= start && value <= end && !results.Contains(msg))
                    {
                        results.Add(msg);
                    }
                }
            }
            else
            {
                var num = int.Parse(part);
                var msg = useUid
                    ? messages.FirstOrDefault(m => m.Uid == num)
                    : messages.FirstOrDefault(m => m.SequenceNumber == num);
                if (msg != null && !results.Contains(msg))
                {
                    results.Add(msg);
                }
            }
        }

        return results;
    }
}
