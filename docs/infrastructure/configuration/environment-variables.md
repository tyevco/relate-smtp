# Environment Variables

Complete reference of all environment variables used to configure Relate Mail services.

## Database {#database}

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | *(required)* | PostgreSQL connection string. Format: `Host=hostname;Port=5432;Database=relate_mail;Username=user;Password=pass` |

All services (API, SMTP, POP3, IMAP) require this variable. In Docker Compose deployments, it is constructed automatically from the `POSTGRES_*` variables.

**Example:**

```bash
ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=relate_mail;Username=postgres;Password=my-secret"
```

## Authentication {#authentication}

| Variable | Default | Description |
|----------|---------|-------------|
| `Oidc__Authority` | *(empty)* | OIDC provider URL (e.g., `https://auth.example.com`). When empty, the system runs in development mode without OIDC authentication. |
| `Oidc__Audience` | *(empty)* | Expected audience claim in OIDC tokens. Typically the API's client ID or URL. |
| `Jwt__DevelopmentKey` | *(empty)* | Symmetric key for signing development JWT tokens. Only used when `Oidc__Authority` is not set. Must be at least 32 characters for HS256. |

### Development Mode

When `Oidc__Authority` is empty, authentication operates in development mode:
- The API generates and validates JWTs using the `Jwt__DevelopmentKey` symmetric key
- No external identity provider is required
- The web frontend skips the OIDC login redirect

For production, always configure a real OIDC provider.

## SMTP Server {#smtp}

| Variable | Default | Description |
|----------|---------|-------------|
| `Smtp__Enabled` | `true` | Enable or disable the SMTP server |
| `Smtp__ServerName` | `localhost` | SMTP server hostname (used in the EHLO greeting) |
| `Smtp__Port` | `587` | SMTP STARTTLS submission port |
| `Smtp__SecurePort` | `465` | SMTP implicit TLS submission port |
| `Smtp__RequireAuthentication` | `true` | Require authentication on submission ports (587, 465) |
| `Smtp__CertificatePath` | *(empty)* | Path to a PFX/PKCS12 TLS certificate file |
| `Smtp__CertificatePassword` | *(empty)* | Password for the TLS certificate file |
| `Smtp__MaxAttachmentSizeBytes` | `26214400` | Maximum attachment size (default: 25 MB) |
| `Smtp__MaxMessageSizeBytes` | `52428800` | Maximum total message size (default: 50 MB) |

### MX Endpoint (Inbound Internet Mail)

| Variable | Default | Description |
|----------|---------|-------------|
| `Smtp__Mx__Enabled` | `false` | Enable the MX endpoint on port 25 for receiving internet mail |
| `Smtp__Mx__Port` | `25` | MX endpoint port |
| `Smtp__Mx__HostedDomains__0` | *(empty)* | First hosted domain (e.g., `example.com`). Add more with `__1`, `__2`, etc. |
| `Smtp__Mx__ValidateRecipients` | `true` | Check that the recipient email address exists in the database before accepting mail |

The MX endpoint accepts unauthenticated inbound mail **only** for the configured hosted domains. It is not an open relay -- mail addressed to domains not listed in `HostedDomains` is rejected.

**Multiple hosted domains:**

```bash
Smtp__Mx__HostedDomains__0=example.com
Smtp__Mx__HostedDomains__1=mail.example.com
Smtp__Mx__HostedDomains__2=another-domain.org
```

## POP3 Server {#pop3}

| Variable | Default | Description |
|----------|---------|-------------|
| `Pop3__Enabled` | `true` | Enable or disable the POP3 server |
| `Pop3__ServerName` | `localhost` | POP3 server hostname |
| `Pop3__Port` | `110` | POP3 plaintext/STARTTLS port |
| `Pop3__SecurePort` | `995` | POP3 implicit TLS (POP3S) port |
| `Pop3__RequireAuthentication` | `true` | Require authentication for POP3 connections |

## IMAP Server {#imap}

| Variable | Default | Description |
|----------|---------|-------------|
| `Imap__Enabled` | `true` | Enable or disable the IMAP server |
| `Imap__ServerName` | `localhost` | IMAP server hostname |
| `Imap__Port` | `143` | IMAP plaintext/STARTTLS port |
| `Imap__SecurePort` | `993` | IMAP implicit TLS (IMAPS) port |
| `Imap__RequireAuthentication` | `true` | Require authentication for IMAP connections |

## Outbound Mail {#outbound-mail}

| Variable | Default | Description |
|----------|---------|-------------|
| `OutboundMail__Enabled` | `false` | Enable outbound email delivery |
| `OutboundMail__RelayHost` | *(empty)* | SMTP relay server hostname. When empty, delivers directly via MX lookup. |
| `OutboundMail__RelayPort` | `587` | Relay server port |
| `OutboundMail__RelayUsername` | *(empty)* | Relay server authentication username |
| `OutboundMail__RelayPassword` | *(empty)* | Relay server authentication password |
| `OutboundMail__RelayUseTls` | `true` | Use TLS when connecting to the relay server |
| `OutboundMail__MaxConcurrency` | `5` | Maximum concurrent outbound delivery connections |
| `OutboundMail__MaxRetries` | `10` | Maximum delivery retry attempts before marking as failed |
| `OutboundMail__RetryBaseDelaySeconds` | `60` | Base delay between retries (increases exponentially) |
| `OutboundMail__QueuePollingIntervalSeconds` | `15` | How often to check the outbound queue for pending messages |
| `OutboundMail__SmtpTimeoutSeconds` | `30` | Timeout for outbound SMTP connections |
| `OutboundMail__SenderDomain` | `localhost` | Domain used in the MAIL FROM envelope and Message-Id header |

### Direct Delivery vs. Relay

When `RelayHost` is empty, the system performs direct MX delivery by looking up the recipient domain's MX records and connecting directly. This requires:
- Port 25 outbound access (many cloud providers block this by default)
- Proper SPF, DKIM, and DMARC records for your sender domain

When `RelayHost` is set, all outbound mail is sent through the specified relay server (e.g., Amazon SES, SendGrid, Mailgun, or your ISP's SMTP server). This is the recommended approach for most deployments.

## Security {#security}

| Variable | Default | Description |
|----------|---------|-------------|
| `Security__AuthenticationSalt` | *(empty)* | Additional salt for API key hashing (combined with BCrypt) |
| `Security__RateLimit__MaxFailedAttempts` | `5` | Maximum failed authentication attempts before lockout |
| `Security__RateLimit__LockoutWindowMinutes` | `15` | Duration of authentication lockout |
| `Security__RateLimit__BaseBackoffDelaySeconds` | `1` | Base delay for exponential backoff on failed auth |
| `Security__RateLimit__MaxBackoffDelaySeconds` | `30` | Maximum backoff delay |

## CORS {#cors}

| Variable | Default | Description |
|----------|---------|-------------|
| `Cors__AllowedOrigins__0` | `http://localhost:5173` | First allowed CORS origin |
| `Cors__AllowedOrigins__1` | `http://localhost:5492` | Second allowed CORS origin |

Add more origins using sequential indices (`__2`, `__3`, etc.). In production, set these to your actual frontend URLs:

```bash
Cors__AllowedOrigins__0=https://mail.example.com
Cors__AllowedOrigins__1=https://app.example.com
```

## Internal Communication {#internal}

| Variable | Default | Description |
|----------|---------|-------------|
| `Internal__ApiKey` | *(empty)* | Pre-shared API key for service-to-service communication. The SMTP server uses this to notify the API of new incoming messages. |
| `Api__BaseUrl` | *(empty)* | Internal URL of the API server. Used by protocol hosts to send notifications. In Docker: `http://api:8080`. |

The internal API key should be a long, random string. It is used by the SMTP, POP3, and IMAP hosts to authenticate with the API's internal notification endpoint.

## Observability {#observability}

| Variable | Default | Description |
|----------|---------|-------------|
| `Otel__Endpoint` | *(null)* | OpenTelemetry collector endpoint URL. When set, the API exports traces and metrics. |

## Logging {#logging}

| Variable | Default | Description |
|----------|---------|-------------|
| `Logging__LogLevel__Default` | `Information` | Default log level |
| `Logging__LogLevel__Microsoft.AspNetCore` | `Warning` | ASP.NET Core framework log level |

Standard .NET log levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.

## Health Checks {#health-checks}

| Variable | Default | Description |
|----------|---------|-------------|
| `HealthCheck__Url` | *(varies)* | HTTP URL for the internal health check endpoint. Set automatically in Docker images: SMTP=`http://+:8081`, POP3=`http://+:8082`, IMAP=`http://+:8083`. |

## Frontend (Build-Time) {#frontend}

These variables are used when building the web frontend outside of Docker (e.g., for local development). In Docker deployments, the frontend reads configuration from the `/api/config` runtime endpoint instead.

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | `/api` | Base URL for API requests. Defaults to `/api` which is proxied by Vite in development. |
| `VITE_OIDC_AUTHORITY` | *(empty)* | OIDC provider URL for the frontend login flow |
| `VITE_OIDC_CLIENT_ID` | *(empty)* | OIDC client ID for the frontend application |

## ASP.NET Core {#aspnet}

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment. Set to `Development` for auto-migration and detailed errors. |
| `ASPNETCORE_URLS` | `http://+:8080` | URL bindings for the API server (set in Dockerfile) |
| `AllowedHosts` | `localhost` | Semicolon-separated list of allowed Host header values. Set to `*` for production behind a reverse proxy. |

## Complete Example

A production `.env` file for Docker Compose:

```env
# Database
POSTGRES_PASSWORD=strong-random-password-here
POSTGRES_USER=postgres
POSTGRES_DB=relate_mail

# Authentication
OIDC_AUTHORITY=https://auth.example.com
OIDC_AUDIENCE=relate-mail-api

# SMTP
SMTP_SERVER_NAME=mail.example.com
SMTP_MX_ENABLED=true
SMTP_MX_HOSTED_DOMAIN=example.com
SMTP_MX_VALIDATE_RECIPIENTS=true

# Internal communication
INTERNAL_API_KEY=long-random-string-for-service-communication

# Outbound mail (via relay)
OutboundMail__Enabled=true
OutboundMail__RelayHost=smtp.sendgrid.net
OutboundMail__RelayPort=587
OutboundMail__RelayUsername=apikey
OutboundMail__RelayPassword=SG.your-sendgrid-api-key
OutboundMail__SenderDomain=example.com
```
