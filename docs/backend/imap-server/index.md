# IMAP Server

IMAP (Internet Message Access Protocol) is the standard protocol for server-side email management. Unlike POP3's download-and-delete model, IMAP keeps messages on the server and allows clients to manage them in place -- reading, flagging, searching, and organizing across multiple devices. When you check your email on your phone and mark a message as read, your desktop client sees the same change.

Relate Mail includes a custom IMAP server that implements [RFC 9051 (IMAP4rev2)](https://datatracker.ietf.org/doc/html/rfc9051), the latest revision of the IMAP protocol.

## Why IMAP is More Complex Than POP3

POP3 is stateless between sessions: download, optionally delete, disconnect. IMAP must maintain persistent server-side state:

- **Multiple mailboxes** -- IMAP supports selecting different mailboxes (INBOX, Sent, custom folders).
- **Per-message flags** -- Each message can be individually flagged as Seen, Answered, Flagged, Deleted, or Draft.
- **Unique identifiers** -- Every message has both a sequence number (position-based, changes with expunge) and a UID (permanent, never reused).
- **Server-side search** -- Clients can search messages by criteria without downloading them.
- **Concurrent access** -- Multiple clients can access the same mailbox simultaneously.

## Architecture

The IMAP server runs as its own .NET hosted service in the `Relate.Smtp.ImapHost` project:

```
Relate.Smtp.ImapHost/
  ImapServerHostedService.cs       # TCP listener and connection management
  ImapServerOptions.cs             # Configuration model
  ImapHealthCheck.cs               # Self-test health check
  Program.cs                       # Host entry point
  Protocol/
    ImapState.cs                   # Session state enum
    ImapSession.cs                 # Per-connection session state and message model
    ImapCommand.cs                 # Tagged command parser with quoted string support
    ImapResponse.cs                # RFC 9051 response builder
  Handlers/
    ImapCommandHandler.cs          # Full command dispatcher
    ImapMessageManager.cs          # Message loading, retrieval, flag management
    ImapUserAuthenticator.cs       # AUTHENTICATE PLAIN and LOGIN support
```

## Hosted Service

`ImapServerHostedService` extends `BackgroundService` and manages two TCP listeners:

- **Port 143** (plain text) -- Accepts connections and supports STARTTLS upgrade.
- **Port 993** (implicit TLS/IMAPS) -- Requires TLS from the start. Only enabled when a certificate is configured.

The connection handling pattern is identical to the POP3 server: each client connection spawns a background task with a scoped DI container, delegating to `ImapCommandHandler.HandleSessionAsync`.

## Session States

IMAP sessions move through four states:

1. **Not Authenticated** -- Initial state after connection. Only `LOGIN`, `AUTHENTICATE`, `CAPABILITY`, and `LOGOUT` are available.
2. **Authenticated** -- After successful login. The client can list mailboxes, check status, and select a mailbox.
3. **Selected** -- A mailbox is open. Full message operations (FETCH, STORE, SEARCH, EXPUNGE) are available. Authenticated-state commands also remain available.
4. **Logout** -- The client has issued `LOGOUT`. Pending deletions are committed and the connection closes.

## Configuration

Options are bound from the `Imap` configuration section:

| Setting | Default | Description |
|---------|---------|-------------|
| `ServerName` | `localhost` | Name used in the server greeting |
| `Port` | `143` | Plain IMAP listener port |
| `SecurePort` | `993` | IMAPS (implicit TLS) listener port |
| `RequireAuthentication` | `true` | Whether authentication is mandatory |
| `CertificatePath` | `null` | Path to TLS certificate file |
| `CertificatePassword` | `null` | Password for PKCS#12 certificates |
| `CheckCertificateRevocation` | `true` | Whether to check certificate revocation lists |
| `MaxConnectionsPerUser` | `5` | Maximum concurrent IMAP sessions per user |
| `SessionTimeout` | `30 minutes` | Inactivity timeout before session termination |
| `MaxMessagesPerSession` | `2000` | Maximum messages loaded into a session |

These can be set via environment variables (e.g., `Imap__Port=143`) or `appsettings.json`.

## Advertised Capabilities

The server advertises the following capabilities in response to `CAPABILITY`:

```
IMAP4rev2 AUTH=PLAIN LITERAL+ ENABLE UNSELECT UIDPLUS CHILDREN
```

- **IMAP4rev2** -- RFC 9051 compliance
- **AUTH=PLAIN** -- SASL PLAIN authentication mechanism
- **LITERAL+** -- Non-synchronizing literal support
- **ENABLE** -- Capability activation (used for UTF8=ACCEPT)
- **UNSELECT** -- Close mailbox without expunging
- **UIDPLUS** -- UID-based operations
- **CHILDREN** -- Mailbox children attribute in LIST

## Health Check

The IMAP health check operates identically to the POP3 health check: it opens a TCP connection to the configured port, verifies the greeting, and sends `LOGOUT`.

## Graceful Shutdown

The shutdown process mirrors the POP3 server:

1. Stop TCP listeners.
2. Wait up to 30 seconds for active sessions to complete.
3. Finalize the hosted service.

Sessions in the Selected state with pending deletions that have not sent `LOGOUT` will not have those deletions committed.

## Metrics

The server emits OpenTelemetry metrics:

- `imap.sessions.active` -- Current active IMAP sessions
- `imap.commands` -- Commands processed (tagged by command name)
- `imap.auth.attempts` / `imap.auth.failures` -- Authentication metrics
- `imap.messages.retrieved` / `imap.bytes.sent` -- Data transfer metrics

See the [Telemetry](../infrastructure/telemetry.md) page for details.
