# IMAP Handlers

The IMAP server's command processing is split across three handler classes. Due to the protocol's complexity, the command handler is significantly larger than its POP3 counterpart.

## ImapCommandHandler

`ImapCommandHandler` is the central command dispatcher for the IMAP server, implementing the full RFC 9051 command set.

### Session Loop

`HandleSessionAsync` follows the same pattern as the POP3 handler:

1. Creates an `ImapSession` with the client's IP.
2. Sends the greeting: `* OK <server-name> IMAP4rev2 server ready`.
3. Enters a read loop checking for timeout, reading bounded lines, and dispatching parsed commands.
4. On cleanup, removes the connection from `ConnectionRegistry` and flushes the stream.

The key difference from POP3 is that IMAP commands include a tag prefix that must be parsed and echoed in the response.

### Command Routing by State

Commands are dispatched in two phases. First, commands available in **any state** are checked:

| Command | Description |
|---------|-------------|
| `CAPABILITY` | Lists server capabilities |
| `NOOP` | Keep-alive, returns OK |
| `LOGOUT` | Commits deletions and disconnects |
| `ENABLE` | Activates optional capabilities (e.g., `UTF8=ACCEPT`) |

Then, state-specific commands are dispatched:

#### Not Authenticated State

| Command | Description |
|---------|-------------|
| `LOGIN <user> <pass>` | Plain text authentication |
| `AUTHENTICATE PLAIN` | SASL PLAIN authentication with Base64 encoding |

#### Authenticated State

| Command | Description |
|---------|-------------|
| `SELECT <mailbox>` | Opens a mailbox for read-write access |
| `EXAMINE <mailbox>` | Opens a mailbox for read-only access |
| `LIST` | Lists available mailboxes |
| `STATUS <mailbox> (items)` | Returns mailbox statistics without selecting |

#### Selected State

All Authenticated-state commands remain available, plus:

| Command | Description |
|---------|-------------|
| `FETCH <set> <items>` | Retrieves message data |
| `STORE <set> <flags>` | Modifies message flags |
| `SEARCH <criteria>` | Searches messages by criteria |
| `EXPUNGE` | Permanently removes `\Deleted` messages |
| `CLOSE` | Expunges and returns to Authenticated state |
| `UNSELECT` | Returns to Authenticated state without expunging |
| `UID <subcommand>` | UID-prefixed variants of FETCH, STORE, SEARCH |

### Authentication

The handler supports two authentication methods:

**LOGIN** takes the username and password as direct arguments:
```
C: a001 LOGIN user@example.com rl_apikey123
S: * CAPABILITY IMAP4rev2 AUTH=PLAIN ...
S: a001 OK LOGIN completed
```

**AUTHENTICATE PLAIN** uses the SASL PLAIN mechanism. Credentials are Base64-encoded in the format `\0username\0password`:

```
C: a001 AUTHENTICATE PLAIN
S: +
C: AHVzZXJAZXhhbXBsZS5jb20AcmxfYXBpa2V5MTIz
S: * CAPABILITY IMAP4rev2 AUTH=PLAIN ...
S: a001 OK AUTHENTICATE completed
```

The SASL Initial Response (SASL-IR) is also supported, where credentials are sent inline with the AUTHENTICATE command:

```
C: a001 AUTHENTICATE PLAIN AHVzZXJAZXhhbXBsZS5jb20AcmxfYXBpa2V5MTIz
S: a001 OK AUTHENTICATE completed
```

### SELECT and EXAMINE

Both commands open a mailbox. SELECT allows modifications; EXAMINE opens read-only. On success, the server responds with mailbox status:

```
S: * FLAGS (\Seen \Answered \Flagged \Deleted \Draft)
S: * OK [PERMANENTFLAGS (\Seen \Answered \Flagged \Deleted \Draft \*)] Permanent flags
S: * 42 EXISTS
S: * OK [UIDVALIDITY 2847563901] UIDs valid
S: * OK [UIDNEXT 100] Predicted next UID
S: a002 OK [READ-WRITE] SELECT completed
```

Currently, only `INBOX` is supported as a selectable mailbox. UIDVALIDITY is computed deterministically from the user's GUID to ensure stability across sessions.

### FETCH

FETCH is the most complex command, supporting multiple data items in a single request. The handler parses a sequence set (e.g., `1:*`, `1,3,5`, `10:20`) and builds the response for each matching message.

Supported data items: `UID`, `FLAGS`, `INTERNALDATE`, `RFC822.SIZE`, `ENVELOPE`, `BODY[]`, `BODY.PEEK[]`, `BODY[HEADER]`, `BODY.PEEK[HEADER]`, `RFC822`.

When `BODY[]` is fetched (without `.PEEK`), the message is automatically marked as `\Seen`.

### STORE

Modifies flags on messages. Supports three operations:

- `FLAGS <flags>` -- Replace all flags
- `+FLAGS <flags>` -- Add flags
- `-FLAGS <flags>` -- Remove flags

Adding `.SILENT` suppresses the FETCH response for each modified message (e.g., `+FLAGS.SILENT (\Seen)`).

When the `\Deleted` flag is set, the message's UID is tracked in the session's `DeletedUids` set for later expunging. Flag changes are persisted to the database immediately via `ImapMessageManager.UpdateFlagsAsync`.

### SEARCH

Evaluates criteria against the in-memory message list. Messages with `\Deleted` flags are excluded from results unless the search specifically includes the `DELETED` criterion. Supports `UID SEARCH` for returning UIDs instead of sequence numbers.

### EXPUNGE

Permanently deletes messages that have the `\Deleted` flag:

1. Identifies all messages with UIDs in the `DeletedUids` set.
2. Calls `ImapMessageManager.ApplyDeletionsAsync` to remove them from the database.
3. Sends `EXPUNGE` responses in descending sequence number order.
4. Removes the messages from the in-memory list and renumbers remaining messages.
5. Clears the `DeletedUids` set.

### CLOSE vs UNSELECT

Both return to the Authenticated state:

- **CLOSE** expunges `\Deleted` messages first (like an implicit EXPUNGE), then clears the mailbox state.
- **UNSELECT** clears the mailbox state without expunging. This allows clients to discard pending deletions.

### LOGOUT

Commits any pending deletions (if in Selected state), sends `BYE`, and transitions to the Logout state.

### OpenTelemetry Integration

Every command execution is wrapped in an activity (`imap.command.<name>`) with tags for session ID, command name, and current state. The `imap.commands` counter is incremented for each command.

## ImapMessageManager

`ImapMessageManager` handles the mapping between domain entities and IMAP protocol structures.

### Loading Messages

`LoadMessagesAsync` fetches the user's emails, queries per-user read status from `EmailRecipient`, and builds `ImapMessage` objects with:

- Sequential sequence numbers (1-based)
- Deterministic UIDs generated from the email's database GUID (first 4 bytes, masked to ensure non-negative)
- Initial flags (the `\Seen` flag is set if `EmailRecipient.IsRead` is true)
- Envelope metadata (subject, from address, date)

### Message Retrieval

Three retrieval methods serve different FETCH data items:

- `RetrieveMessageAsync` -- Full RFC 822 message (for `BODY[]` and `RFC822`)
- `RetrieveHeadersAsync` -- Headers only (for `BODY[HEADER]`)
- `RetrieveBodyPartAsync` -- Headers plus N lines of body

All use MimeKit to build RFC 822 formatted messages from the stored email data, following the same pattern as the POP3 message manager.

### Flag Management

- `MarkAsSeenAsync` -- Sets `IsRead = true` on the `EmailRecipient` record
- `UpdateFlagsAsync` -- Maps IMAP flags to database state (currently, `\Seen` maps to `IsRead`)

### Deletion

`ApplyDeletionsAsync` permanently removes emails from the database, identical to the POP3 implementation.

## ImapUserAuthenticator

`ImapUserAuthenticator` extends `ProtocolAuthenticator` with IMAP-specific configuration:

| Property | Value |
|----------|-------|
| `ProtocolName` | `imap` |
| `RequiredScope` | `imap` |
| `ActivitySource` | `TelemetryConfiguration.ImapActivitySource` |
| `AuthAttemptsCounter` | `ProtocolMetrics.ImapAuthAttempts` |
| `AuthFailuresCounter` | `ProtocolMetrics.ImapAuthFailures` |

The authentication flow is identical to the POP3 authenticator (rate limiting, caching, BCrypt verification, scope checking) but requires the `imap` scope on the API key.

The `AUTHENTICATE PLAIN` mechanism decoding is handled in `ImapCommandHandler`, which parses the Base64-encoded `\0username\0password` format before passing the extracted credentials to the authenticator.
