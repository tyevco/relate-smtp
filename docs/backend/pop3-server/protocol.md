# POP3 Protocol Details

This page describes the POP3 protocol as implemented in Relate Mail, covering session state management, the command set, response format, and security mechanisms.

## Session State

The `Pop3State` enum defines the three phases of a POP3 session:

```csharp
public enum Pop3State
{
    Authorization,  // USER/PASS only
    Transaction,    // STAT/LIST/RETR/DELE/NOOP/RSET
    Update          // QUIT - apply deletions
}
```

### Authorization State

The session begins in the Authorization state immediately after the server sends its greeting. The only commands accepted are `USER`, `PASS`, and `QUIT`. The client must provide a username and password (an API key with `pop3` scope) to proceed.

### Transaction State

After successful authentication, the server loads the user's messages into memory as a list of `Pop3Message` objects. Each message has a 1-based message number, a reference to the database email ID, the message size in bytes, and a unique identifier (the RFC 5322 Message-ID). The client can then list, retrieve, delete, and manage messages.

### Update State

Entered when the client sends `QUIT` during the Transaction state. All messages marked for deletion are permanently removed from the database. The connection then closes.

## Pop3Session

`Pop3Session` extends the shared `ProtocolSession` base class and maintains:

| Property | Type | Description |
|----------|------|-------------|
| `State` | `Pop3State` | Current session state |
| `Messages` | `List<Pop3Message>` | Snapshot of user's mailbox at login |
| `DeletedMessages` | `HashSet<int>` | Message numbers marked for deletion |

The `Pop3Message` structure contains:

| Property | Type | Description |
|----------|------|-------------|
| `MessageNumber` | `int` | 1-based position in the mailbox listing |
| `EmailId` | `Guid` | Database identifier for the email |
| `SizeBytes` | `long` | Size of the message in bytes |
| `UniqueId` | `string` | Stable unique identifier (RFC 5322 Message-ID) |

### Deletion Limit

To prevent unbounded memory growth in long-running sessions, the session enforces a maximum of **10,000 messages** that can be marked for deletion (`MaxDeletedMessages`). Attempts to mark additional messages for deletion after this limit return an error.

## Supported Commands

### Authorization State Commands

#### USER &lt;name&gt;

Sets the username (email address) for the session. Must be followed by `PASS` before any mailbox access.

```
C: USER alice@example.com
S: +OK User accepted
```

#### PASS &lt;password&gt;

Provides the API key for authentication. The password is verified against the user's API keys with `pop3` scope using BCrypt hashing.

```
C: PASS rl_abc123def456...
S: +OK Logged in, 42 messages
```

On failure:
```
C: PASS wrong-key
S: -ERR Authentication failed
```

### Transaction State Commands

#### STAT

Returns the number of non-deleted messages and their total size.

```
C: STAT
S: +OK 42 1234567
```

The response format is `+OK <count> <total_size_bytes>`.

#### LIST [msg]

Without arguments, lists all non-deleted messages with their numbers and sizes, terminated by a period on a line by itself:

```
C: LIST
S: +OK 3 messages
S: 1 1024
S: 2 2048
S: 3 512
S: .
```

With a message number argument, returns the size for that specific message:

```
C: LIST 2
S: +OK 2 2048
```

#### RETR &lt;msg&gt;

Retrieves the complete RFC 822 message content. The server builds the message on the fly from the stored email data using MimeKit, including headers, body (text and/or HTML), and attachments. The message is automatically marked as read for the user.

```
C: RETR 1
S: +OK 1024 octets
S: From: sender@example.com
S: To: alice@example.com
S: Subject: Hello
S:
S: Message body here.
S: .
```

#### DELE &lt;msg&gt;

Marks a message for deletion. The message is not actually removed until `QUIT` is sent.

```
C: DELE 1
S: +OK Message deleted
```

Attempting to delete an already-deleted message returns an error:
```
C: DELE 1
S: -ERR Message already deleted
```

#### NOOP

Does nothing except reset the session inactivity timer. Returns a success response.

```
C: NOOP
S: +OK
```

#### RSET

Unmarks all messages that were marked for deletion during this session.

```
C: RSET
S: +OK 42 messages
```

#### TOP &lt;msg&gt; &lt;lines&gt;

Retrieves the message headers plus the first N lines of the message body. Useful for previewing messages without downloading the full content.

```
C: TOP 1 5
S: +OK
S: From: sender@example.com
S: To: alice@example.com
S: Subject: Hello
S:
S: First line of body
S: Second line of body
S: .
```

#### UIDL [msg]

Returns unique identifiers for messages. Unlike message numbers, UIDs are stable across sessions -- they do not change when other messages are deleted.

Without arguments:
```
C: UIDL
S: +OK
S: 1 <abc123@example.com>
S: 2 <def456@example.com>
S: .
```

With a message number:
```
C: UIDL 1
S: +OK 1 <abc123@example.com>
```

### Any State Commands

#### QUIT

In the Authorization state, simply closes the connection. In the Transaction state, transitions to the Update state where marked deletions are committed to the database, then closes.

```
C: QUIT
S: +OK Goodbye
```

## Response Format

All POP3 responses begin with a status indicator:

- **`+OK`** -- The command succeeded. May be followed by additional information.
- **`-ERR`** -- The command failed. Followed by a human-readable error description.

Multi-line responses (LIST, UIDL, RETR, TOP) are terminated by a period (`.`) on a line by itself.

## Security

### Line Length Limit

To prevent denial-of-service attacks from clients sending extremely long lines, the server uses `BoundedStreamReader` to enforce a maximum line length of **8,192 bytes**. If a client exceeds this limit, the connection is terminated with an error response.

### Session Timeout

Sessions that are inactive for longer than the configured timeout (default: 10 minutes) are terminated. The `LastActivityAt` timestamp is updated each time a command is received.

### Rate Limiting

Authentication attempts are rate-limited per IP address using exponential backoff. After multiple failed attempts, the IP is temporarily blocked from further authentication. See [Infrastructure Services](../infrastructure/services.md) for details on the rate limiter.

### Connection Limits

Each user is limited to a configurable maximum number of concurrent POP3 sessions (default: 5) enforced by the `ConnectionRegistry`. When a session ends, the connection count is decremented.
