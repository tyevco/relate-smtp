using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relate.Smtp.Core.Protocol;
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
    private readonly ConnectionRegistry _connectionRegistry;

    public ImapCommandHandler(
        ILogger<ImapCommandHandler> logger,
        ImapUserAuthenticator authenticator,
        ImapMessageManager messageManager,
        IOptions<ImapServerOptions> options,
        ConnectionRegistry connectionRegistry)
    {
        _logger = logger;
        _authenticator = authenticator;
        _messageManager = messageManager;
        _options = options.Value;
        _connectionRegistry = connectionRegistry;
    }

    public async Task HandleSessionAsync(Stream stream, string clientIp, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        // Use UTF8 without BOM - MailKit and other clients don't expect BOM in protocol greetings
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: false) { AutoFlush = true };

        var session = new ImapSession { ClientIp = clientIp };
        _logger.LogInformation("IMAP session started: {ConnectionId}", session.ConnectionId);

        try
        {
            // Send greeting
            await writer.WriteLineAsync(ImapResponse.Greeting(_options.ServerName));

            while (!ct.IsCancellationRequested && session.State != ImapState.Logout)
            {
                // Check for timeout
                if (session.IsTimedOut(_options.SessionTimeout))
                {
                    _logger.LogWarning("Session timeout: {ConnectionId}", session.ConnectionId);
                    await writer.WriteLineAsync(ImapResponse.Bye("Session timeout"));
                    break;
                }

                string? line;
                try
                {
                    line = await BoundedStreamReader.ReadLineBoundedAsync(reader, 8192, ct);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogWarning("Client sent line exceeding maximum length, disconnecting: {ConnectionId}", session.ConnectionId);
                    await writer.WriteLineAsync(ImapResponse.Bye("Line too long"));
                    break;
                }
                if (line == null) break;

                session.LastActivityAt = DateTime.UtcNow;
                _logger.LogDebug("Command received: {Command}", line);

                ImapCommand command;
                try
                {
                    command = ImapCommand.Parse(line);
                }
                catch (ImapParseException ex)
                {
                    _logger.LogWarning("Command parse error: {Error}", ex.Message);
                    await writer.WriteLineAsync(ImapResponse.UntaggedBad(ex.Message));
                    continue;
                }

                await ExecuteCommandAsync(command, session, reader, writer, ct);

                if (session.State == ImapState.Logout)
                    break;
            }
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe", StringComparison.Ordinal) || ex.InnerException?.Message.Contains("Broken pipe", StringComparison.Ordinal) == true)
        {
            _logger.LogDebug("Client disconnected unexpectedly (broken pipe): {ConnectionId}", session.ConnectionId);
        }
#pragma warning disable CA1031 // Do not catch general exception types - Protocol handler must not crash on any exception
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Session error: {ConnectionId}", session.ConnectionId);
        }
        finally
        {
            if (session.UserId.HasValue)
                _connectionRegistry.RemoveConnection(session.UserId.Value);

            _logger.LogInformation("IMAP session ended: {ConnectionId}", session.ConnectionId);
            try
            {
                await writer.FlushAsync(ct);
            }
#pragma warning disable CA1031 // Do not catch general exception types - Flush errors during cleanup are expected
            catch
#pragma warning restore CA1031
            {
                // Ignore flush errors during cleanup
            }
        }
    }

    private async Task ExecuteCommandAsync(
        ImapCommand command,
        ImapSession session,
        StreamReader reader,
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
                    await HandleNotAuthenticatedCommandAsync(command, session, reader, writer, ct);
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
#pragma warning disable CA1031 // Do not catch general exception types - Command handler must return error response
        catch (Exception ex)
#pragma warning restore CA1031
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
            var idsToDelete = session.Messages
                .Where(m => session.DeletedUids.Contains(m.Uid))
                .Select(m => m.EmailId)
                .ToList();

            if (idsToDelete.Count > 0)
            {
                await _messageManager.ApplyDeletionsAsync(idsToDelete, ct);
                _logger.LogInformation("Applied {Count} deletions on logout", idsToDelete.Count);
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
        StreamReader reader,
        StreamWriter writer,
        CancellationToken ct)
    {
        switch (command.Name)
        {
            case "LOGIN":
                await HandleLoginAsync(command, session, writer, ct);
                break;
            case "AUTHENTICATE":
                await HandleAuthenticateAsync(command, session, reader, writer, ct);
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

        var (authenticated, userId) = await _authenticator.AuthenticateAsync(username, password, session.ClientIp, ct);

        if (!authenticated || !userId.HasValue)
        {
            _logger.LogWarning("Authentication failed for: {Username}", username);
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Authentication failed"));
            return;
        }

        if (!_connectionRegistry.TryAddConnection(userId.Value, _options.MaxConnectionsPerUser))
        {
            _logger.LogWarning("Connection limit reached for user: {Username}", username);
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Too many connections"));
            return;
        }

        session.Username = username;
        session.UserId = userId.Value;
        session.State = ImapState.Authenticated;

        _logger.LogInformation("User authenticated: {Username}", username);
        await writer.WriteLineAsync(ImapResponse.Capability());
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "LOGIN completed"));
    }

    private async Task HandleAuthenticateAsync(
        ImapCommand command,
        ImapSession session,
        StreamReader reader,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (command.Arguments.Length < 1)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "AUTHENTICATE requires mechanism name"));
            return;
        }

        var mechanism = command.Arguments[0].ToUpperInvariant();
        if (mechanism != "PLAIN")
        {
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Unsupported authentication mechanism"));
            return;
        }

        string base64Credentials;

        // Check if credentials were provided inline (SASL-IR: initial response)
        if (command.Arguments.Length >= 2)
        {
            base64Credentials = command.Arguments[1];
        }
        else
        {
            // Send continuation request
            await writer.WriteLineAsync("+");

            // Read the Base64-encoded credentials from the client
            string? credentialLine;
            try
            {
                credentialLine = await BoundedStreamReader.ReadLineBoundedAsync(reader, 8192, ct);
            }
            catch (InvalidOperationException)
            {
                await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Credentials too long"));
                return;
            }

            if (string.IsNullOrEmpty(credentialLine) || credentialLine == "*")
            {
                await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Authentication cancelled"));
                return;
            }

            base64Credentials = credentialLine;
        }

        // Decode Base64: format is \0username\0password
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(base64Credentials);
        }
        catch (FormatException)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Invalid Base64 encoding"));
            return;
        }

        // Parse PLAIN mechanism: [authzid]\0authcid\0passwd
        var parts = SplitPlainCredentials(decoded);
        if (parts == null)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, "Invalid PLAIN credentials format"));
            return;
        }

        var (_, username, password) = parts.Value;

        var (authenticated, userId) = await _authenticator.AuthenticateAsync(username, password, session.ClientIp, ct);

        if (!authenticated || !userId.HasValue)
        {
            _logger.LogWarning("AUTHENTICATE PLAIN failed for: {Username}", username);
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Authentication failed"));
            return;
        }

        if (!_connectionRegistry.TryAddConnection(userId.Value, _options.MaxConnectionsPerUser))
        {
            _logger.LogWarning("Connection limit reached for user: {Username}", username);
            await writer.WriteLineAsync(ImapResponse.TaggedNo(command.Tag, "Too many connections"));
            return;
        }

        session.Username = username;
        session.UserId = userId.Value;
        session.State = ImapState.Authenticated;

        _logger.LogInformation("User authenticated via AUTHENTICATE PLAIN: {Username}", username);
        await writer.WriteLineAsync(ImapResponse.Capability());
        await writer.WriteLineAsync(ImapResponse.TaggedOk(command.Tag, "AUTHENTICATE completed"));
    }

    /// <summary>
    /// Split PLAIN SASL credentials: [authzid]\0authcid\0passwd
    /// </summary>
    private static (string Authzid, string Username, string Password)? SplitPlainCredentials(byte[] data)
    {
        var str = Encoding.UTF8.GetString(data);
        // Find the two NUL separators
        var firstNull = str.IndexOf('\0');
        if (firstNull < 0) return null;
        var secondNull = str.IndexOf('\0', firstNull + 1);
        if (secondNull < 0) return null;

        var authzid = str[..firstNull];
        var username = str[(firstNull + 1)..secondNull];
        var password = str[(secondNull + 1)..];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return null;

        return (authzid, username, password);
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

        // Compute UIDVALIDITY from user ID for deterministic, per-user value
        // RFC 9051 requires UIDVALIDITY to change only when UIDs are reassigned
        session.UidValidity = ComputeUidValidity(session.UserId!.Value);

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

        if (statusItems.Contains("MESSAGES", StringComparison.Ordinal))
            statusParts.Add($"MESSAGES {messages.Count}");
        if (statusItems.Contains("UNSEEN", StringComparison.Ordinal))
            statusParts.Add($"UNSEEN {messages.Count(m => !m.Flags.HasFlag(ImapFlags.Seen))}");
        if (statusItems.Contains("UIDNEXT", StringComparison.Ordinal))
            statusParts.Add($"UIDNEXT {(messages.Count > 0 ? messages.Max(m => m.Uid) + 1 : 1)}");
        if (statusItems.Contains("UIDVALIDITY", StringComparison.Ordinal))
            statusParts.Add($"UIDVALIDITY {ComputeUidValidity(session.UserId!.Value)}");

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

        List<ImapMessage> messages;
        try
        {
            messages = ParseSequenceSet(sequenceSet, session.Messages, useUid);
        }
        catch (ImapParseException ex)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, ex.Message));
            return;
        }

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

        if (includeUid || upperItems.Contains("UID", StringComparison.Ordinal))
        {
            parts.Add($"UID {msg.Uid}");
        }

        if (upperItems.Contains("FLAGS", StringComparison.Ordinal))
        {
            parts.Add($"FLAGS ({msg.Flags.ToImapString()})");
        }

        if (upperItems.Contains("INTERNALDATE", StringComparison.Ordinal))
        {
            parts.Add($"INTERNALDATE \"{msg.InternalDate:dd-MMM-yyyy HH:mm:ss zzzz}\"");
        }

        if (upperItems.Contains("RFC822.SIZE", StringComparison.Ordinal) || upperItems.Contains("SIZE", StringComparison.Ordinal))
        {
            parts.Add($"RFC822.SIZE {msg.SizeBytes}");
        }

        if (upperItems.Contains("ENVELOPE", StringComparison.Ordinal))
        {
            parts.Add(BuildEnvelope(msg));
        }

        if (upperItems.Contains("BODY[]", StringComparison.Ordinal) || upperItems.Contains("RFC822", StringComparison.Ordinal))
        {
            var content = await _messageManager.RetrieveMessageAsync(msg.EmailId, userId, ct);
            // Mark as seen if BODY[] (not BODY.PEEK[])
            if (!upperItems.Contains("PEEK", StringComparison.Ordinal))
            {
                msg.Flags |= ImapFlags.Seen;
                await _messageManager.MarkAsSeenAsync(msg.EmailId, userId, ct);
            }
            parts.Add($"BODY[] {{{content.Length}}}\r\n{content}");
        }
        else if (upperItems.Contains("BODY.PEEK[]", StringComparison.Ordinal))
        {
            var content = await _messageManager.RetrieveMessageAsync(msg.EmailId, userId, ct);
            parts.Add($"BODY[] {{{content.Length}}}\r\n{content}");
        }
        else if (upperItems.Contains("BODY[HEADER]", StringComparison.Ordinal) || upperItems.Contains("BODY.PEEK[HEADER]", StringComparison.Ordinal))
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

        List<ImapMessage> messages;
        try
        {
            messages = ParseSequenceSet(sequenceSet, session.Messages, useUid);
        }
        catch (ImapParseException ex)
        {
            await writer.WriteLineAsync(ImapResponse.TaggedBad(command.Tag, ex.Message));
            return;
        }

        var silent = dataItem.Contains(".SILENT", StringComparison.Ordinal);

        foreach (var msg in messages)
        {
            if (dataItem.StartsWith("+FLAGS", StringComparison.Ordinal))
            {
                msg.Flags |= flags;
            }
            else if (dataItem.StartsWith("-FLAGS", StringComparison.Ordinal))
            {
                msg.Flags &= ~flags;
            }
            else if (dataItem.StartsWith("FLAGS", StringComparison.Ordinal))
            {
                msg.Flags = flags;
            }

            // Track deletions
            if (msg.Flags.HasFlag(ImapFlags.Deleted))
            {
                if (!session.DeletedUids.Contains(msg.Uid))
                {
                    if (session.IsDeletedUidsLimitReached)
                    {
                        await writer.WriteLineAsync(
                            ImapResponse.TaggedNo(command.Tag,
                            $"Maximum deleted messages limit reached"));
                        return;
                    }
                    session.DeletedUids.Add(msg.Uid);
                }
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

    /// <summary>
    /// Build an RFC 9051 ENVELOPE structure from message metadata.
    /// Format: (date subject from sender reply-to to cc bcc in-reply-to message-id)
    /// </summary>
    private static string BuildEnvelope(ImapMessage msg)
    {
        var date = ImapQuote(msg.InternalDate.ToString("r"));
        var subject = ImapQuote(msg.Subject ?? "");
        var from = FormatAddressList(msg.FromAddress, msg.FromDisplayName);
        var messageId = ImapQuote(msg.MessageId);

        // RFC 9051: (date subject from sender reply-to to cc bcc in-reply-to message-id)
        // sender and reply-to default to from when not specified
        return $"ENVELOPE ({date} {subject} {from} {from} {from} NIL NIL NIL NIL {messageId})";
    }

    private static string ImapQuote(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "NIL";
        // Escape backslashes and quotes inside the string
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static string FormatAddressList(string? address, string? displayName)
    {
        if (string.IsNullOrEmpty(address))
            return "NIL";

        // RFC 9051 address: (personal-name at-domain-list mailbox-name host-name)
        var atIndex = address.IndexOf('@');
        var mailbox = atIndex >= 0 ? address[..atIndex] : address;
        var host = atIndex >= 0 ? address[(atIndex + 1)..] : "";

        var personal = ImapQuote(displayName);
        var mailboxQuoted = ImapQuote(mailbox);
        var hostQuoted = ImapQuote(host);

        return $"(({personal} NIL {mailboxQuoted} {hostQuoted}))";
    }

    private static ImapFlags ParseFlagsFromString(string flagsStr)
    {
        var flags = ImapFlags.None;
        var upper = flagsStr.ToUpperInvariant();

        if (upper.Contains(@"\SEEN", StringComparison.Ordinal)) flags |= ImapFlags.Seen;
        if (upper.Contains(@"\ANSWERED", StringComparison.Ordinal)) flags |= ImapFlags.Answered;
        if (upper.Contains(@"\FLAGGED", StringComparison.Ordinal)) flags |= ImapFlags.Flagged;
        if (upper.Contains(@"\DELETED", StringComparison.Ordinal)) flags |= ImapFlags.Deleted;
        if (upper.Contains(@"\DRAFT", StringComparison.Ordinal)) flags |= ImapFlags.Draft;

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
            if (session.DeletedUids.Contains(msg.Uid) && !criteria.Contains("DELETED", StringComparison.Ordinal))
                continue;

            var matches = true;

            if (criteria.Contains("ALL", StringComparison.Ordinal))
            {
                // Match all
            }
            else
            {
                if (criteria.Contains("SEEN", StringComparison.Ordinal) && !msg.Flags.HasFlag(ImapFlags.Seen))
                    matches = false;
                if (criteria.Contains("UNSEEN", StringComparison.Ordinal) && msg.Flags.HasFlag(ImapFlags.Seen))
                    matches = false;
                if (criteria.Contains("DELETED", StringComparison.Ordinal) && !msg.Flags.HasFlag(ImapFlags.Deleted))
                    matches = false;
                if (criteria.Contains("FLAGGED", StringComparison.Ordinal) && !msg.Flags.HasFlag(ImapFlags.Flagged))
                    matches = false;
                if (criteria.Contains("UNFLAGGED", StringComparison.Ordinal) && msg.Flags.HasFlag(ImapFlags.Flagged))
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

        var idsToDelete = deletedMessages.Select(m => m.EmailId).ToList();

        if (idsToDelete.Count > 0)
        {
            await _messageManager.ApplyDeletionsAsync(idsToDelete, ct);

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
            _logger.LogInformation("Expunged {Count} messages", idsToDelete.Count);
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
            var idsToDelete = session.Messages
                .Where(m => session.DeletedUids.Contains(m.Uid))
                .Select(m => m.EmailId)
                .ToList();

            if (idsToDelete.Count > 0)
            {
                await _messageManager.ApplyDeletionsAsync(idsToDelete, ct);
                _logger.LogInformation("Expunged {Count} messages on CLOSE", idsToDelete.Count);
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

    /// <summary>
    /// Compute a deterministic UIDVALIDITY value from the user ID.
    /// This ensures the same user always gets the same UIDVALIDITY for their INBOX.
    /// </summary>
    private static uint ComputeUidValidity(Guid userId)
    {
        // Use first 4 bytes of user GUID as basis for UIDVALIDITY
        // This is deterministic and unique per user
        var bytes = userId.ToByteArray();
        var value = BitConverter.ToUInt32(bytes, 0);
        // Ensure non-zero (UIDVALIDITY must be non-zero per RFC 9051)
        return value == 0 ? 1 : value;
    }

    /// <summary>
    /// Parse a sequence set and return matching messages.
    /// </summary>
    /// <exception cref="ImapParseException">Thrown when sequence set is malformed or exceeds limits.</exception>
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

        // Validate sequence set complexity to prevent DoS attacks
        if (parts.Length > ImapCommand.MaxSequenceSetParts)
        {
            throw new ImapParseException($"Sequence set has too many parts (max {ImapCommand.MaxSequenceSetParts})");
        }

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                throw new ImapParseException("Invalid sequence set: empty part");
            }

            if (part.Contains(':'))
            {
                var range = part.Split(':');
                if (range.Length != 2)
                {
                    throw new ImapParseException($"Invalid range in sequence set: {part}");
                }

                uint start, end;

                if (range[0] == "*")
                {
                    start = messages.Count > 0
                        ? (useUid ? messages.Max(m => m.Uid) : (uint)messages.Count)
                        : 1;
                }
                else if (!uint.TryParse(range[0], out start))
                {
                    throw new ImapParseException($"Invalid sequence number in range: {range[0]}");
                }

                if (range[1] == "*")
                {
                    end = messages.Count > 0
                        ? (useUid ? messages.Max(m => m.Uid) : (uint)messages.Count)
                        : 1;
                }
                else if (!uint.TryParse(range[1], out end))
                {
                    throw new ImapParseException($"Invalid sequence number in range: {range[1]}");
                }

                if (start > end)
                {
                    (start, end) = (end, start);
                }

                foreach (var msg in messages)
                {
                    var value = useUid ? msg.Uid : (uint)msg.SequenceNumber;
                    if (value >= start && value <= end && !results.Contains(msg))
                    {
                        results.Add(msg);
                    }
                }
            }
            else
            {
                if (!uint.TryParse(part, out var num))
                {
                    throw new ImapParseException($"Invalid sequence number: {part}");
                }
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
