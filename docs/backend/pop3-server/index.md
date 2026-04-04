# POP3 Server

POP3 (Post Office Protocol version 3) is one of the oldest and simplest email retrieval protocols. It follows a straightforward download-and-delete model: a client connects, authenticates, downloads messages, and optionally marks them for deletion. When the session ends, deletions are committed. This makes POP3 ideal for single-device setups where the user wants a local copy of their mail without maintaining server-side state.

Relate Mail includes a custom POP3 server implementation that is fully compliant with [RFC 1939](https://datatracker.ietf.org/doc/html/rfc1939).

## Architecture

The POP3 server runs as its own .NET hosted service in the `Relate.Smtp.Pop3Host` project. It shares the same PostgreSQL database, repository layer, and authentication infrastructure as the REST API and other protocol servers, but operates as an independent process.

```
Relate.Smtp.Pop3Host/
  Pop3ServerHostedService.cs       # TCP listener and connection management
  Pop3ServerOptions.cs             # Configuration model
  Pop3HealthCheck.cs               # Self-test health check
  Program.cs                       # Host entry point
  Protocol/
    Pop3State.cs                   # Session state enum
    Pop3Session.cs                 # Per-connection session state
    Pop3Command.cs                 # Command parser
    Pop3Response.cs                # Response builder
  Handlers/
    Pop3CommandHandler.cs          # Command dispatcher and execution
    Pop3MessageManager.cs          # Message loading, retrieval, deletion
    Pop3UserAuthenticator.cs       # Authentication via API keys
```

## Hosted Service

`Pop3ServerHostedService` extends `BackgroundService` and manages two TCP listeners:

- **Port 110** (plain text) -- Accepts connections and supports STARTTLS upgrade for encryption.
- **Port 995** (implicit TLS/POP3S) -- Requires TLS from the start of the connection. Only enabled when a certificate is configured.

Each incoming connection is handed off to a background task that creates a scoped DI container and delegates to `Pop3CommandHandler.HandleSessionAsync`. The server tracks all active tasks in a `ConcurrentBag<Task>` for graceful shutdown.

### TLS Configuration

When a certificate path is provided, the server loads an X.509 certificate (PEM or PKCS#12 format) and negotiates TLS 1.2 or TLS 1.3 with connecting clients. If no certificate is configured but a secure port is specified, the server logs a warning and skips TLS.

## Session Lifecycle

POP3 sessions move through three states:

1. **Authorization** -- The client identifies itself with `USER` and `PASS` commands. No mailbox access is available yet.
2. **Transaction** -- After successful authentication, the client can list, retrieve, and mark messages for deletion. All operations work against an in-memory snapshot of the mailbox taken at login time.
3. **Update** -- Triggered when the client sends `QUIT`. Messages marked for deletion during the Transaction state are permanently removed from the database.

If a client disconnects without sending `QUIT`, no deletions are committed -- this is a safety feature of the POP3 protocol.

## Configuration

Options are bound from the `Pop3` configuration section:

| Setting | Default | Description |
|---------|---------|-------------|
| `ServerName` | `localhost` | Name used in the server greeting |
| `Port` | `110` | Plain POP3 listener port |
| `SecurePort` | `995` | POP3S (implicit TLS) listener port |
| `RequireAuthentication` | `true` | Whether authentication is mandatory |
| `CertificatePath` | `null` | Path to TLS certificate file |
| `CertificatePassword` | `null` | Password for PKCS#12 certificates |
| `CheckCertificateRevocation` | `true` | Whether to check certificate revocation lists |
| `MaxConnectionsPerUser` | `5` | Maximum concurrent POP3 sessions per user |
| `SessionTimeout` | `10 minutes` | Inactivity timeout before session termination |
| `MaxMessagesPerSession` | `1000` | Maximum messages loaded into a session |

These can be set via environment variables (e.g., `Pop3__Port=110`) or `appsettings.json`.

## Health Check

`Pop3HealthCheck` performs a self-test by opening a TCP connection to the configured port on `localhost`, reading the greeting line, verifying it starts with `+OK`, and sending `QUIT`. If any step fails or times out (5 seconds), the health check reports unhealthy.

The health check is exposed as part of the `/healthz` endpoint when the POP3 host is running.

## Graceful Shutdown

When `StopAsync` is called, the server:

1. Stops both TCP listeners (no new connections accepted).
2. Waits up to 30 seconds for all active client tasks to complete.
3. Calls the base `StopAsync` to finalize the hosted service lifecycle.

Active sessions in the Transaction state that have not sent `QUIT` will not have their deletions committed, which is the correct POP3 behavior.

## Metrics

The server emits OpenTelemetry metrics for monitoring:

- `pop3.sessions.active` -- Current number of active POP3 sessions (up/down counter)
- `pop3.commands` -- Total POP3 commands processed (counter, tagged by command name)
- `pop3.auth.attempts` / `pop3.auth.failures` -- Authentication metrics
- `pop3.messages.retrieved` / `pop3.bytes.sent` -- Data transfer metrics

See the [Telemetry](../infrastructure/telemetry.md) page for details on consuming these metrics.
