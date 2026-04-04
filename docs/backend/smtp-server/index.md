# SMTP Server Overview

The `Relate.Smtp.SmtpHost` project implements an SMTP server using the [SmtpServer library (v11.1.0)](https://github.com/cosullivan/SmtpServer). It handles both authenticated client submission (ports 587/465) and unauthenticated inbound internet mail delivery (port 25).

## Architecture

The SMTP server runs as a .NET `BackgroundService` (`SmtpServerHostedService`) inside a minimal web application host. The web host exists solely to serve the `/healthz` health check endpoint on a separate port (default 8081).

```
SmtpHost Process
  |
  +-- SmtpServerHostedService (BackgroundService)
  |     |
  |     +-- Port 587: STARTTLS, authenticated submission
  |     +-- Port 465: Implicit TLS, authenticated submission
  |     +-- Port 25:  MX endpoint, unauthenticated inbound
  |
  +-- WebApplication (health checks only)
        |
        +-- Port 8081: /healthz endpoint
```

## Listeners

The `SmtpServerHostedService` configures up to three SMTP endpoints:

### Port 587 -- STARTTLS Submission

The standard mail submission port. Clients connect in plaintext, then upgrade to TLS using the STARTTLS command. Authentication is **required** -- only users with a valid API key (with `smtp` scope) can send mail through this port.

The `AllowUnsecureAuthentication()` option is enabled, which allows clients to authenticate before upgrading to TLS. While not ideal for security, this is necessary for compatibility with some mail clients that authenticate before issuing STARTTLS.

### Port 465 -- Implicit TLS Submission

The implicit TLS submission port. TLS is negotiated immediately on connection -- there is no plaintext phase. Authentication is **required**. This port is only activated when `SecurePort` is configured to a value greater than 0.

A TLS certificate must be configured for this port to function. See [TLS Configuration](./tls-configuration.md) for details.

### Port 25 -- MX Endpoint

The Mail Exchange endpoint for receiving inbound internet mail. This is how other mail servers (Gmail, Outlook, etc.) deliver mail to your domain. Authentication is **not required** on this port -- other MTAs (Mail Transfer Agents) connect anonymously.

The MX endpoint is **disabled by default** and must be explicitly enabled:

```json
{
  "Smtp": {
    "Mx": {
      "Enabled": true,
      "HostedDomains": ["example.com", "mail.example.com"],
      "ValidateRecipients": true
    }
  }
}
```

**Critical safety requirement:** When the MX endpoint is enabled, `HostedDomains` must be configured. If the array is empty, the server throws an `InvalidOperationException` at startup to prevent accidental open relay configuration.

Opportunistic STARTTLS is supported on port 25 when a TLS certificate is configured, allowing connecting servers to upgrade to encrypted communication.

## Handler Components

The SmtpServer library uses a handler-based architecture. Three custom handlers are registered:

| Handler | Purpose |
|---------|---------|
| `CustomMessageStore` | Parses MIME messages and stores them in the database |
| `CustomUserAuthenticator` | Validates credentials against API keys |
| `MxMailboxFilter` | Controls which addresses can send/receive on each port |

These handlers are detailed in the [Message Handling](./message-handling.md) page.

## Connection Metrics

The server tracks active connections using OpenTelemetry counters:

```csharp
_smtpServer.SessionCreated += (_, _) =>
    ProtocolMetrics.SmtpActiveConnections.Add(1);

_smtpServer.SessionCompleted += (_, _) =>
    ProtocolMetrics.SmtpActiveConnections.Add(-1);
```

Metrics updates are wrapped in try/catch to ensure metric failures never crash the server.

## Configuration

All configuration is in the `Smtp` section of `appsettings.json`:

```json
{
  "Smtp": {
    "ServerName": "mail.example.com",
    "Port": 587,
    "SecurePort": 465,
    "RequireAuthentication": true,
    "CertificatePath": "/path/to/cert.pfx",
    "CertificatePassword": "password",
    "MaxAttachmentSizeBytes": 26214400,
    "MaxMessageSizeBytes": 52428800,
    "Mx": {
      "Enabled": false,
      "Port": 25,
      "HostedDomains": [],
      "ValidateRecipients": true
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ServerName` | `localhost` | SMTP server hostname (used in EHLO greeting) |
| `Port` | `587` | STARTTLS submission port |
| `SecurePort` | `465` | Implicit TLS submission port (0 to disable) |
| `RequireAuthentication` | `true` | Whether submission ports require auth |
| `CertificatePath` | null | Path to TLS certificate (PFX or DER) |
| `CertificatePassword` | null | Certificate password (for PFX files) |
| `MaxAttachmentSizeBytes` | 25 MB | Maximum single attachment size |
| `MaxMessageSizeBytes` | 50 MB | Maximum total message size |
| `Mx:Enabled` | `false` | Enable the MX inbound endpoint |
| `Mx:Port` | `25` | MX endpoint port |
| `Mx:HostedDomains` | `[]` | Domains this server accepts mail for |
| `Mx:ValidateRecipients` | `true` | Reject mail to unknown users at hosted domains |

## Infrastructure Dependencies

The SMTP host shares the same Infrastructure layer as the API:

```csharp
builder.Services.AddInfrastructure(connectionString);
```

This provides:
- EF Core `AppDbContext` with PostgreSQL connection pooling
- Repository implementations (`IEmailRepository`, `IUserRepository`, `ISmtpApiKeyRepository`)
- Background task queue for async `LastUsedAt` updates
- Authentication rate limiter with configurable lockout
- OpenTelemetry configuration

## Notification Service

After storing an email, the SMTP host notifies the API to trigger real-time notifications. This is done via HTTP using `HttpEmailNotificationService`:

```csharp
builder.Services.AddHttpClient<IEmailNotificationService, HttpEmailNotificationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("ApiKey", internalApiKey);
});
```

Configuration:

| Setting | Default | Description |
|---------|---------|-------------|
| `Api:BaseUrl` | `http://localhost:5000` | API base URL for notifications |
| `Internal:ApiKey` | null | API key with `internal` scope |

## Running

```bash
cd api
dotnet run --project src/Relate.Smtp.SmtpHost
```

The server logs its port configuration on startup:

```
Starting SMTP server on port 587 (plain) and 465 (TLS)
MX endpoint enabled on port 25 for domains: example.com, mail.example.com
```
