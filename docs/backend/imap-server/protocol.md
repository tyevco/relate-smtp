# IMAP Protocol Details

This page describes the IMAP4rev2 protocol as implemented in Relate Mail, covering session state, the message model, the command/response format, and key data structures.

## Session State

The `ImapState` enum defines four session phases:

```csharp
public enum ImapState
{
    NotAuthenticated,  // Only LOGIN command allowed
    Authenticated,     // Can SELECT/EXAMINE mailboxes
    Selected,          // A mailbox is selected, full message access
    Logout             // Client issued LOGOUT, connection closing
}
```

## ImapSession

`ImapSession` extends `ProtocolSession` and maintains the full state of an IMAP connection:

| Property | Type | Description |
|----------|------|-------------|
| `State` | `ImapState` | Current session state |
| `SelectedMailbox` | `string?` | Name of the currently selected mailbox (e.g., `INBOX`) |
| `SelectedReadOnly` | `bool` | Whether the mailbox was opened with EXAMINE (read-only) |
| `Messages` | `List<ImapMessage>` | Messages in the selected mailbox |
| `DeletedUids` | `HashSet<uint>` | UIDs of messages flagged `\Deleted` |
| `EnabledCapabilities` | `HashSet<string>` | Capabilities activated via ENABLE |
| `UidValidity` | `uint` | UIDVALIDITY for the current mailbox |

### Deletion Limit

Like POP3, the IMAP session enforces a maximum of **10,000 deleted UIDs** to prevent unbounded memory growth.

## ImapMessage

Each message in the selected mailbox is represented as an `ImapMessage`:

| Property | Type | Description |
|----------|------|-------------|
| `SequenceNumber` | `int` | 1-based position in the mailbox. Changes when messages are expunged. |
| `Uid` | `uint` | Permanent unique identifier. Generated deterministically from the email's database GUID. Never reused within a mailbox. |
| `EmailId` | `Guid` | Database identifier for the email |
| `SizeBytes` | `long` | Message size in bytes |
| `MessageId` | `string` | RFC 5322 Message-ID header value |
| `Flags` | `ImapFlags` | Current message flags |
| `InternalDate` | `DateTimeOffset` | When the message was received by the server |
| `Subject` | `string?` | Message subject (used in ENVELOPE responses) |
| `FromAddress` | `string?` | Sender's email address |
| `FromDisplayName` | `string?` | Sender's display name |

### Sequence Numbers vs UIDs

This is a fundamental IMAP concept:

- **Sequence numbers** are 1-based and contiguous. If message 3 is expunged, what was message 4 becomes message 3. Clients use these for human-readable references.
- **UIDs** are permanent. Once assigned, a UID is never reused for a different message within the same UIDVALIDITY period. Clients use UIDs to track specific messages across sessions.

The `UID` prefix on commands (e.g., `UID FETCH`, `UID STORE`) instructs the server to interpret message references as UIDs instead of sequence numbers.

## ImapFlags

Flags are stored as a bit field enum:

```csharp
[Flags]
public enum ImapFlags
{
    None     = 0,
    Seen     = 1,   // \Seen - Message has been read
    Answered = 2,   // \Answered - Message has been replied to
    Flagged  = 4,   // \Flagged - Message is "flagged" (starred/important)
    Deleted  = 8,   // \Deleted - Message is marked for deletion
    Draft    = 16   // \Draft - Message is a draft
}
```

The `ToImapString()` extension method converts these to the wire format (e.g., `\Seen \Flagged`). The `ParseFlags()` method handles the reverse conversion.

In the database, flags map to the `EmailRecipient.IsRead` field for the `\Seen` flag. Other flags are maintained in the session's in-memory message state.

## Tag-Based Command/Response Correlation

Unlike POP3 where commands and responses are sequential, IMAP uses **tags** to correlate commands with responses. The client prefixes each command with a unique tag string:

```
C: a001 LOGIN user@example.com myapikey
S: * CAPABILITY IMAP4rev2 AUTH=PLAIN LITERAL+ ENABLE UNSELECT UIDPLUS CHILDREN
S: a001 OK LOGIN completed
C: a002 SELECT INBOX
S: * FLAGS (\Seen \Answered \Flagged \Deleted \Draft)
S: * OK [PERMANENTFLAGS (\Seen \Answered \Flagged \Deleted \Draft \*)] Permanent flags
S: * 42 EXISTS
S: * OK [UIDVALIDITY 123456789] UIDs valid
S: * OK [UIDNEXT 100] Predicted next UID
S: a002 OK [READ-WRITE] SELECT completed
```

Response types:

- **Tagged responses** (`a001 OK ...`) -- Final response to a tagged command. Status is OK, NO, or BAD.
- **Untagged responses** (`* ...`) -- Data or status that may precede the tagged response.

## Key FETCH Data Items

The FETCH command is the primary way to retrieve message data. Clients specify which data items they want:

### FLAGS

Returns the message's current flags:
```
* 1 FETCH (FLAGS (\Seen \Flagged))
```

### INTERNALDATE

The server's received timestamp:
```
* 1 FETCH (INTERNALDATE "15-Mar-2026 10:30:00 +0000")
```

### RFC822.SIZE

The message size in bytes:
```
* 1 FETCH (RFC822.SIZE 4096)
```

### ENVELOPE

A parsed representation of the message's header fields, structured as:
```
(date subject from sender reply-to to cc bcc in-reply-to message-id)
```

Each address field is a list of address structures: `(personal-name NIL mailbox-name host-name)`.

### BODY[] / RFC822

The complete raw message content, including all headers and body parts. When fetched with `BODY[]` (without `.PEEK`), the `\Seen` flag is automatically set:

```
* 1 FETCH (BODY[] {1024}
<1024 bytes of message data>
)
```

Using `BODY.PEEK[]` retrieves the content without setting the `\Seen` flag.

### BODY[HEADER]

Only the message headers, without the body:
```
* 1 FETCH (BODY[HEADER] {256}
<256 bytes of header data>
)
```

## SEARCH Criteria

The SEARCH command filters messages based on criteria. Supported criteria:

| Criterion | Description |
|-----------|-------------|
| `ALL` | Matches all messages |
| `SEEN` | Messages with `\Seen` flag |
| `UNSEEN` | Messages without `\Seen` flag |
| `DELETED` | Messages with `\Deleted` flag |
| `FLAGGED` | Messages with `\Flagged` flag |
| `UNFLAGGED` | Messages without `\Flagged` flag |

Search results are returned as a list of sequence numbers or UIDs (when using `UID SEARCH`):

```
C: a005 SEARCH UNSEEN
S: * SEARCH 3 7 12
S: a005 OK SEARCH completed
```

## UIDVALIDITY

UIDVALIDITY is a value that identifies a particular "incarnation" of a mailbox. If UIDVALIDITY changes between sessions, it means UIDs may have been reassigned and the client must re-sync. In Relate Mail, UIDVALIDITY is computed deterministically from the user's GUID, so it remains stable as long as the user account exists.

## Command Line Limits

To prevent denial-of-service attacks:

- Maximum command line length: **8,192 bytes** (enforced by `BoundedStreamReader`)
- Maximum arguments per command: **100**
- Maximum sequence set parts: **500** (e.g., `1,2,3,...,500`)

Lines exceeding these limits result in the connection being terminated.

## Session Timeout

Sessions that are inactive for longer than the configured timeout (default: 30 minutes) are terminated with a `BYE` response. The timeout is reset each time a command is received.
