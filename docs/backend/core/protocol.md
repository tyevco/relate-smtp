# Protocol Utilities

The `Protocol/` directory in the Core layer contains shared base classes used by both the POP3 and IMAP server implementations. These utilities handle session management, network I/O safety, and connection tracking.

## ProtocolSession

`ProtocolSession` is the abstract base class for all protocol sessions (POP3 and IMAP). It tracks connection metadata and provides timeout detection.

```csharp
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
```

| Property | Description |
|----------|-------------|
| `ConnectionId` | A unique GUID string assigned to each session, used for logging and tracing correlation. |
| `ConnectedAt` | Timestamp when the connection was accepted. |
| `LastActivityAt` | Updated each time the server processes a command from the client. Used for timeout detection. |
| `ClientIp` | The client's IP address, extracted from the TCP endpoint. Used for rate limiting and logging. |
| `Username` | Set after the client provides a username (via `USER` in POP3 or `LOGIN`/`AUTHENTICATE` in IMAP). |
| `UserId` | Set after successful authentication, linking the session to a database user. |

The `IsTimedOut` method compares the time since the last activity to the configured timeout. The protocol handlers check this on each loop iteration and terminate the session if it has expired.

### Subclasses

- **`Pop3Session`** extends `ProtocolSession` with POP3-specific state: `Pop3State`, `Messages`, `DeletedMessages`
- **`ImapSession`** extends `ProtocolSession` with IMAP-specific state: `ImapState`, `SelectedMailbox`, `Messages`, `DeletedUids`, `EnabledCapabilities`, `UidValidity`

## BoundedStreamReader

`BoundedStreamReader` provides a single static method for reading network input with a size limit, preventing denial-of-service attacks from clients that send extremely long lines.

```csharp
public static class BoundedStreamReader
{
    public static async Task<string?> ReadLineBoundedAsync(
        StreamReader reader, int maxLength = 8192, CancellationToken ct = default);
}
```

### Behavior

The method reads one character at a time from the stream:

- **`\n`** (newline) -- Returns the accumulated string (line complete).
- **`\r`** (carriage return) -- Skipped, since POP3 and IMAP use `\r\n` line endings.
- **End of stream** (0 bytes read) -- Returns `null` if nothing was read, or the partial string if some data was accumulated.
- **Length exceeded** -- Throws `InvalidOperationException` when the accumulated string exceeds `maxLength`.
- **Cancellation** -- Respects the `CancellationToken` for cooperative shutdown.

### Default Limit

The default maximum line length is **8,192 bytes**, which aligns with:

- RFC 1939 (POP3): No explicit limit, but 8K is a common implementation choice
- RFC 9051 (IMAP): Recommends 8,192 as maximum command line length

### Usage

Both `Pop3CommandHandler` and `ImapCommandHandler` use `BoundedStreamReader` in their session loops:

```csharp
try
{
    line = await BoundedStreamReader.ReadLineBoundedAsync(reader, 8192, ct);
}
catch (InvalidOperationException)
{
    // Client sent line exceeding maximum length
    await writer.WriteLineAsync(errorResponse);
    break;
}
```

## ConnectionRegistry

`ConnectionRegistry` tracks the number of active protocol connections per user, enforcing per-user connection limits to prevent resource exhaustion.

```csharp
public class ConnectionRegistry
{
    public bool TryAddConnection(Guid userId, int maxConnections);
    public void RemoveConnection(Guid userId);
}
```

### Thread Safety

The registry uses a `ConcurrentDictionary<Guid, int>` for lock-free, thread-safe operation. `TryAddConnection` uses an optimistic compare-and-swap loop:

1. Get the current count for the user (or 0 if not present).
2. If the count is at or above the maximum, return `false`.
3. Attempt to atomically update the count from `current` to `current + 1`.
4. If another thread modified the count concurrently, retry from step 1.

This pattern avoids locks entirely while remaining correct under concurrent access from multiple connection-handling threads.

### Usage

Both POP3 and IMAP handlers follow the same pattern:

```csharp
// During authentication
if (!_connectionRegistry.TryAddConnection(userId, maxConnections))
    return Error("Too many connections");

// During cleanup (finally block)
if (session.UserId.HasValue)
    _connectionRegistry.RemoveConnection(session.UserId.Value);
```

### Shared Instance

Each protocol host registers `ConnectionRegistry` as a singleton, so all sessions within a protocol host share the same registry. Note that POP3 and IMAP have separate registries since they run as separate processes, so the per-user limit is per-protocol.
