# POP3 Handlers

The POP3 server's command processing is split across three handler classes, each responsible for a specific concern: command dispatch, message management, and authentication.

## Pop3CommandHandler

`Pop3CommandHandler` is the main entry point for processing a POP3 session. It manages the read/write loop, dispatches parsed commands to the appropriate handler methods, and enforces session-level constraints.

### Session Loop

The `HandleSessionAsync` method:

1. Creates a new `Pop3Session` with the client's IP address.
2. Sends the server greeting (`+OK <server-name> POP3 server ready`).
3. Enters a read loop that continues until the client sends `QUIT`, the session times out, or the connection is lost.
4. On each iteration, reads a line (bounded to 8,192 bytes), parses it into a `Pop3Command`, and dispatches it via `ExecuteCommandAsync`.
5. On cleanup, removes the user's connection from the `ConnectionRegistry` and flushes the output stream.

### Command Dispatch

`ExecuteCommandAsync` uses a `switch` expression to route commands to handler methods:

| Command | Handler Method | State Required |
|---------|----------------|----------------|
| `USER` | `HandleUser` | Authorization |
| `PASS` | `HandlePassAsync` | Authorization |
| `STAT` | `HandleStat` | Transaction |
| `LIST` | `HandleListAsync` | Transaction |
| `RETR` | `HandleRetrAsync` | Transaction |
| `DELE` | `HandleDele` | Transaction |
| `NOOP` | (inline) | Transaction |
| `RSET` | `HandleRset` | Transaction |
| `QUIT` | `HandleQuitAsync` | Any |
| `UIDL` | `HandleUidlAsync` | Transaction |
| `TOP` | `HandleTopAsync` | Transaction |

Each handler validates that the session is in the correct state before processing. State violations return `-ERR` responses.

### OpenTelemetry Integration

Every command execution is wrapped in an OpenTelemetry activity (`pop3.command.<name>`) with tags for the session ID and command name. A counter metric (`pop3.commands`) is also incremented, tagged by command name. If an exception occurs during execution, it is captured on the activity span with the exception type and message, and a generic `-ERR Internal server error` is returned to the client.

### Error Handling

The handler uses a catch-all exception handler around command execution to ensure that no unhandled exception crashes the session loop. All errors are logged and returned as `-ERR` responses to the client.

## Pop3MessageManager

`Pop3MessageManager` is responsible for loading the user's emails from the database and converting them to protocol-level structures.

### Loading Messages

`LoadMessagesAsync(Guid userId, CancellationToken ct)` creates a scoped service provider, fetches the user's emails via `IEmailRepository.GetByUserIdAsync` (limited by `MaxMessagesPerSession`), orders them by `ReceivedAt`, and maps each to a `Pop3Message` with a sequential 1-based message number.

The message list is captured as a snapshot at login time and does not change during the session, which is the correct POP3 behavior -- the mailbox is "locked" for the duration of the session.

### Retrieving Messages

`RetrieveMessageAsync(Guid messageId, Guid userId, CancellationToken ct)` loads the full email with access control (verifying the user is a recipient), builds an RFC 822 message using MimeKit, and marks the email as read for the user. The method:

1. Fetches the email via `GetByIdWithUserAccessAsync` (returns null if the user lacks access).
2. Marks the `EmailRecipient.IsRead` flag to `true` using a bulk EF Core update.
3. Constructs a `MimeMessage` with From, To, Cc headers (BCC recipients are excluded), subject, date, text/HTML body, and attachments.
4. Serializes the message to a UTF-8 string.
5. Records metrics for messages retrieved and bytes sent.

### Retrieving Headers (TOP)

`RetrieveTopAsync` builds the full RFC 822 message and then splits it at the `\r\n\r\n` boundary between headers and body. It returns the headers followed by only the first N lines of the body as requested.

### Applying Deletions

`ApplyDeletionsAsync(IEnumerable<Guid> messageIds, CancellationToken ct)` iterates through the email IDs that were marked for deletion and calls `IEmailRepository.DeleteAsync` for each one. This is called during the Update state when the client sends `QUIT`.

## Pop3UserAuthenticator

`Pop3UserAuthenticator` extends the shared `ProtocolAuthenticator` base class, providing POP3-specific configuration:

| Property | Value |
|----------|-------|
| `ProtocolName` | `pop3` |
| `RequiredScope` | `pop3` |
| `ActivitySource` | `TelemetryConfiguration.Pop3ActivitySource` |
| `AuthAttemptsCounter` | `ProtocolMetrics.Pop3AuthAttempts` |
| `AuthFailuresCounter` | `ProtocolMetrics.Pop3AuthFailures` |

The authentication flow (inherited from `ProtocolAuthenticator`) works as follows:

1. **Rate limit check** -- Consults the `IAuthenticationRateLimiter` to see if the client IP is blocked.
2. **Cache check** -- Looks up an HMAC-based cache key derived from the email and password. Cache entries have a 30-second TTL.
3. **Database lookup** -- Fetches the user by email address, then iterates through their active API keys, verifying the password against each BCrypt hash.
4. **Scope verification** -- Confirms the matched API key has the `pop3` scope.
5. **Result caching** -- Caches the authentication result (success or failure) for 30 seconds.
6. **Background update** -- Queues an API key `LastUsedAt` timestamp update via the background task queue.

The method signature exposes a simple async interface:

```csharp
public Task<(bool IsAuthenticated, Guid? UserId)> AuthenticateAsync(
    string username, string password, string clientIp, CancellationToken ct)
```

The `ConnectionRegistry` is consulted after successful authentication to enforce the per-user connection limit before allowing the session to proceed.
