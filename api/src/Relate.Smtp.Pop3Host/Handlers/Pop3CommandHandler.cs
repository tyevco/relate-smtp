using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relate.Smtp.Pop3Host.Protocol;
using System.Text;

namespace Relate.Smtp.Pop3Host.Handlers;

public class Pop3CommandHandler
{
    private readonly ILogger<Pop3CommandHandler> _logger;
    private readonly Pop3UserAuthenticator _authenticator;
    private readonly Pop3MessageManager _messageManager;
    private readonly Pop3ServerOptions _options;

    public Pop3CommandHandler(
        ILogger<Pop3CommandHandler> logger,
        Pop3UserAuthenticator authenticator,
        Pop3MessageManager messageManager,
        IOptions<Pop3ServerOptions> options)
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

        var session = new Pop3Session();
        _logger.LogInformation("POP3 session started: {ConnectionId}", session.ConnectionId);

        // Send greeting
        await writer.WriteLineAsync(Pop3Response.Greeting(_options.ServerName));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Check for timeout
                if (session.IsTimedOut(_options.SessionTimeout))
                {
                    _logger.LogWarning("Session timeout: {ConnectionId}", session.ConnectionId);
                    await writer.WriteLineAsync(Pop3Response.Error("Session timeout"));
                    break;
                }

                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                session.LastActivityAt = DateTime.UtcNow;
                _logger.LogDebug("Command received: {Command}", line);

                var command = Pop3Command.Parse(line);
                var response = await ExecuteCommandAsync(command, session, writer, ct);

                if (!string.IsNullOrEmpty(response))
                    await writer.WriteLineAsync(response);

                if (command.Name == "QUIT")
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
            _logger.LogInformation("POP3 session ended: {ConnectionId}", session.ConnectionId);
        }
    }

    private async Task<string> ExecuteCommandAsync(
        Pop3Command command,
        Pop3Session session,
        StreamWriter writer,
        CancellationToken ct)
    {
        try
        {
            return command.Name switch
            {
                "USER" => HandleUser(command, session),
                "PASS" => await HandlePassAsync(command, session, ct),
                "STAT" => HandleStat(session),
                "LIST" => await HandleListAsync(command, session, writer, ct),
                "RETR" => await HandleRetrAsync(command, session, writer, ct),
                "DELE" => HandleDele(command, session),
                "NOOP" => Pop3Response.Success(),
                "RSET" => HandleRset(session),
                "QUIT" => await HandleQuitAsync(session, ct),
                "UIDL" => await HandleUidlAsync(command, session, writer, ct),
                "TOP" => await HandleTopAsync(command, session, writer, ct),
                _ => Pop3Response.Error("Unknown command")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution error: {Command}", command.Name);
            return Pop3Response.Error("Internal server error");
        }
    }

    private string HandleUser(Pop3Command command, Pop3Session session)
    {
        if (session.State != Pop3State.Authorization)
            return Pop3Response.Error("Already authenticated");

        if (command.Arguments.Length == 0)
            return Pop3Response.Error("USER requires argument");

        session.Username = command.Arguments[0];
        _logger.LogDebug("USER command: {Username}", session.Username);
        return Pop3Response.Success("User accepted");
    }

    private async Task<string> HandlePassAsync(
        Pop3Command command,
        Pop3Session session,
        CancellationToken ct)
    {
        if (session.State != Pop3State.Authorization)
            return Pop3Response.Error("Not in authorization state");

        if (string.IsNullOrEmpty(session.Username))
            return Pop3Response.Error("USER required first");

        if (command.Arguments.Length == 0)
            return Pop3Response.Error("PASS requires argument");

        var password = command.Arguments[0];
        var (authenticated, userId) = await _authenticator.AuthenticateAsync(
            session.Username, password, ct);

        if (!authenticated || !userId.HasValue)
        {
            _logger.LogWarning("Authentication failed for: {Username}", session.Username);
            return Pop3Response.Error("Authentication failed");
        }

        session.UserId = userId.Value;
        session.Messages = await _messageManager.LoadMessagesAsync(userId.Value, ct);
        session.State = Pop3State.Transaction;

        _logger.LogInformation("User authenticated: {Username}, {Count} messages",
            session.Username, session.Messages.Count);
        return Pop3Response.Success($"Logged in, {session.Messages.Count} messages");
    }

    private string HandleStat(Pop3Session session)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        var activeMessages = session.Messages
            .Where(m => !session.DeletedMessages.Contains(m.MessageNumber))
            .ToList();

        var totalSize = activeMessages.Sum(m => m.SizeBytes);
        return Pop3Response.Success($"{activeMessages.Count} {totalSize}");
    }

    private async Task<string> HandleListAsync(
        Pop3Command command,
        Pop3Session session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        var activeMessages = session.Messages
            .Where(m => !session.DeletedMessages.Contains(m.MessageNumber))
            .ToList();

        // LIST with message number
        if (command.Arguments.Length > 0)
        {
            if (!int.TryParse(command.Arguments[0], out var msgNum))
                return Pop3Response.Error("Invalid message number");

            var message = activeMessages.FirstOrDefault(m => m.MessageNumber == msgNum);
            if (message == null)
                return Pop3Response.Error("No such message");

            return Pop3Response.Success($"{message.MessageNumber} {message.SizeBytes}");
        }

        // LIST all messages
        await writer.WriteLineAsync(Pop3Response.Success($"{activeMessages.Count} messages"));
        foreach (var message in activeMessages)
        {
            await writer.WriteLineAsync($"{message.MessageNumber} {message.SizeBytes}");
        }
        await writer.WriteLineAsync(".");
        return string.Empty;
    }

    private async Task<string> HandleRetrAsync(
        Pop3Command command,
        Pop3Session session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        if (command.Arguments.Length == 0)
            return Pop3Response.Error("RETR requires message number");

        if (!int.TryParse(command.Arguments[0], out var msgNum))
            return Pop3Response.Error("Invalid message number");

        if (session.DeletedMessages.Contains(msgNum))
            return Pop3Response.Error("Message deleted");

        var message = session.Messages.FirstOrDefault(m => m.MessageNumber == msgNum);
        if (message == null)
            return Pop3Response.Error("No such message");

        var content = await _messageManager.RetrieveMessageAsync(
            message.EmailId, session.UserId!.Value, ct);

        await writer.WriteLineAsync(Pop3Response.Success($"{content.Length} octets"));
        await writer.WriteAsync(content);
        if (!content.EndsWith("\r\n"))
            await writer.WriteLineAsync();
        await writer.WriteLineAsync(".");

        return string.Empty;
    }

    private string HandleDele(Pop3Command command, Pop3Session session)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        if (command.Arguments.Length == 0)
            return Pop3Response.Error("DELE requires message number");

        if (!int.TryParse(command.Arguments[0], out var msgNum))
            return Pop3Response.Error("Invalid message number");

        if (session.DeletedMessages.Contains(msgNum))
            return Pop3Response.Error("Message already deleted");

        var message = session.Messages.FirstOrDefault(m => m.MessageNumber == msgNum);
        if (message == null)
            return Pop3Response.Error("No such message");

        session.DeletedMessages.Add(msgNum);
        _logger.LogDebug("Message marked for deletion: {MessageNumber}", msgNum);
        return Pop3Response.Success("Message deleted");
    }

    private string HandleRset(Pop3Session session)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        var count = session.DeletedMessages.Count;
        session.DeletedMessages.Clear();
        _logger.LogDebug("Reset {Count} deletion marks", count);
        return Pop3Response.Success($"{session.Messages.Count} messages");
    }

    private async Task<string> HandleQuitAsync(Pop3Session session, CancellationToken ct)
    {
        if (session.State == Pop3State.Transaction && session.DeletedMessages.Any())
        {
            // Apply deletions
            var emailIds = session.Messages
                .Where(m => session.DeletedMessages.Contains(m.MessageNumber))
                .Select(m => m.EmailId)
                .ToList();

            await _messageManager.ApplyDeletionsAsync(emailIds, ct);
            _logger.LogInformation("Applied {Count} deletions", emailIds.Count);
        }

        return Pop3Response.Success("Goodbye");
    }

    private async Task<string> HandleUidlAsync(
        Pop3Command command,
        Pop3Session session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        var activeMessages = session.Messages
            .Where(m => !session.DeletedMessages.Contains(m.MessageNumber))
            .ToList();

        // UIDL with message number
        if (command.Arguments.Length > 0)
        {
            if (!int.TryParse(command.Arguments[0], out var msgNum))
                return Pop3Response.Error("Invalid message number");

            var message = activeMessages.FirstOrDefault(m => m.MessageNumber == msgNum);
            if (message == null)
                return Pop3Response.Error("No such message");

            return Pop3Response.Success($"{message.MessageNumber} {message.UniqueId}");
        }

        // UIDL all messages
        await writer.WriteLineAsync(Pop3Response.Success());
        foreach (var message in activeMessages)
        {
            await writer.WriteLineAsync($"{message.MessageNumber} {message.UniqueId}");
        }
        await writer.WriteLineAsync(".");
        return string.Empty;
    }

    private async Task<string> HandleTopAsync(
        Pop3Command command,
        Pop3Session session,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (session.State != Pop3State.Transaction)
            return Pop3Response.Error("Not authenticated");

        if (command.Arguments.Length < 2)
            return Pop3Response.Error("TOP requires message number and line count");

        if (!int.TryParse(command.Arguments[0], out var msgNum))
            return Pop3Response.Error("Invalid message number");

        if (!int.TryParse(command.Arguments[1], out var lines))
            return Pop3Response.Error("Invalid line count");

        if (session.DeletedMessages.Contains(msgNum))
            return Pop3Response.Error("Message deleted");

        var message = session.Messages.FirstOrDefault(m => m.MessageNumber == msgNum);
        if (message == null)
            return Pop3Response.Error("No such message");

        var content = await _messageManager.RetrieveTopAsync(message.EmailId, lines, ct);

        await writer.WriteLineAsync(Pop3Response.Success());
        await writer.WriteAsync(content);
        if (!content.EndsWith("\r\n"))
            await writer.WriteLineAsync();
        await writer.WriteLineAsync(".");

        return string.Empty;
    }
}
