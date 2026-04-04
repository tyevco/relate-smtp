# appsettings.json Reference

The .NET backend uses `appsettings.json` as the base configuration file. Each project in `api/src/` has its own copy, though the API project's file contains the most complete set of defaults.

## Configuration File Locations

```
api/src/
  Relate.Smtp.Api/
    appsettings.json              # Full configuration with all sections
    appsettings.Development.json  # Development overrides
  Relate.Smtp.SmtpHost/
    appsettings.json              # SMTP-specific defaults
  Relate.Smtp.Pop3Host/
    appsettings.json              # POP3-specific defaults
  Relate.Smtp.ImapHost/
    appsettings.json              # IMAP-specific defaults
```

## Complete API appsettings.json

The API project's `appsettings.json` contains all configuration sections with their default values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "DevelopmentKey": ""
  },
  "Oidc": {
    "Authority": "",
    "Audience": ""
  },
  "Security": {
    "AuthenticationSalt": "",
    "RateLimit": {
      "MaxFailedAttempts": 5,
      "LockoutWindowMinutes": 15,
      "BaseBackoffDelaySeconds": 1,
      "MaxBackoffDelaySeconds": 30
    }
  },
  "Otel": {
    "Endpoint": null
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:5492"]
  },
  "Smtp": {
    "ServerName": "localhost",
    "Port": 587,
    "SecurePort": 465,
    "Enabled": true,
    "Mx": {
      "Enabled": false,
      "Port": 25,
      "HostedDomains": [],
      "ValidateRecipients": true
    }
  },
  "Pop3": {
    "ServerName": "localhost",
    "Port": 110,
    "SecurePort": 995,
    "Enabled": true
  },
  "Imap": {
    "ServerName": "localhost",
    "Port": 143,
    "SecurePort": 993,
    "Enabled": true
  },
  "OutboundMail": {
    "Enabled": false,
    "RelayHost": "",
    "RelayPort": 587,
    "RelayUsername": "",
    "RelayPassword": "",
    "RelayUseTls": true,
    "MaxConcurrency": 5,
    "MaxRetries": 10,
    "RetryBaseDelaySeconds": 60,
    "QueuePollingIntervalSeconds": 15,
    "SmtpTimeoutSeconds": 30,
    "SenderDomain": "localhost"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "localhost"
}
```

## Section-by-Section Breakdown

### ConnectionStrings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

The PostgreSQL connection string. Left empty in the base file because it must be provided at runtime. In Docker, this is constructed from the `POSTGRES_*` environment variables in the compose file.

**Override:** `ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...`

### Jwt

```json
{
  "Jwt": {
    "DevelopmentKey": ""
  }
}
```

Used only in development mode (when `Oidc.Authority` is empty). The `DevelopmentKey` is a symmetric signing key for generating and validating JWTs without an external identity provider. It must be at least 32 characters long for HS256 signing.

### Oidc

```json
{
  "Oidc": {
    "Authority": "",
    "Audience": ""
  }
}
```

OIDC (OpenID Connect) provider configuration. When `Authority` is empty, the system operates in development mode without OIDC authentication.

- **Authority** -- The OIDC provider's base URL (e.g., `https://accounts.google.com`, `https://your-tenant.auth0.com`)
- **Audience** -- The expected `aud` claim in tokens, typically the API's identifier

### Security

```json
{
  "Security": {
    "AuthenticationSalt": "",
    "RateLimit": {
      "MaxFailedAttempts": 5,
      "LockoutWindowMinutes": 15,
      "BaseBackoffDelaySeconds": 1,
      "MaxBackoffDelaySeconds": 30
    }
  }
}
```

- **AuthenticationSalt** -- Additional entropy mixed into API key hashing (BCrypt). Not strictly required since BCrypt generates its own salt, but adds defense in depth.
- **RateLimit** -- Brute-force protection for authentication endpoints. After `MaxFailedAttempts` failures within a window, the account is locked out for `LockoutWindowMinutes`. Failed attempts also trigger exponential backoff delays.

### Cors

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:5492"]
  }
}
```

The defaults allow requests from the Vite dev server (port 5173) and the default web access point (port 5492). For production, override with your actual frontend origins.

### Protocol Servers (Smtp, Pop3, Imap)

Each protocol section follows the same pattern:

```json
{
  "Smtp": {
    "ServerName": "localhost",
    "Port": 587,
    "SecurePort": 465,
    "Enabled": true
  }
}
```

- **ServerName** -- Used in protocol greetings (SMTP EHLO, POP3/IMAP banner)
- **Port** -- Plaintext/STARTTLS port
- **SecurePort** -- Implicit TLS port
- **Enabled** -- Toggle the entire protocol server on or off

The SMTP section has an additional `Mx` subsection for the inbound MX endpoint.

### OutboundMail

```json
{
  "OutboundMail": {
    "Enabled": false,
    "RelayHost": "",
    "RelayPort": 587,
    "RelayUsername": "",
    "RelayPassword": "",
    "RelayUseTls": true,
    "MaxConcurrency": 5,
    "MaxRetries": 10,
    "RetryBaseDelaySeconds": 60,
    "QueuePollingIntervalSeconds": 15,
    "SmtpTimeoutSeconds": 30,
    "SenderDomain": "localhost"
  }
}
```

Outbound delivery is disabled by default. When enabled, it processes the outbound email queue and delivers messages either directly (MX lookup) or through a configured relay.

### Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Standard .NET logging configuration. The `Microsoft.AspNetCore` category is set to `Warning` to suppress verbose framework logs (request/response logging, routing details, etc.).

Available log levels in order of verbosity: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`.

## Development Overrides

The `appsettings.Development.json` file provides overrides that apply when `ASPNETCORE_ENVIRONMENT=Development`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

The development override file is minimal because the base `appsettings.json` already has sensible defaults for local development. The key behavioral difference in development mode comes from the empty `Oidc.Authority`, which triggers development authentication.

## How Configuration Binding Works

.NET's configuration system merges settings from multiple sources. The loading order (later sources override earlier ones) is:

1. `appsettings.json` -- base defaults
2. `appsettings.{Environment}.json` -- environment-specific overrides (e.g., `appsettings.Development.json`)
3. Environment variables -- highest priority, used for runtime configuration
4. Command-line arguments -- can also override settings

### JSON to Environment Variable Mapping

The mapping between JSON keys and environment variables uses `__` (double underscore) as the hierarchy separator:

| JSON Path | Environment Variable |
|-----------|---------------------|
| `Smtp.Port` | `Smtp__Port` |
| `Smtp.Mx.Enabled` | `Smtp__Mx__Enabled` |
| `Smtp.Mx.HostedDomains[0]` | `Smtp__Mx__HostedDomains__0` |
| `ConnectionStrings.DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Logging.LogLevel.Default` | `Logging__LogLevel__Default` |

This means you can override any setting from `appsettings.json` by setting the corresponding environment variable, without modifying the file.

## Custom appsettings for Deployment

If you prefer file-based configuration over environment variables, you can mount a custom `appsettings.json` into the Docker container:

```bash
docker run -v /path/to/my/appsettings.json:/app/appsettings.json ghcr.io/four-robots/relate-mail-api:latest
```

Or in Docker Compose:

```yaml
services:
  api:
    volumes:
      - ./my-appsettings.json:/app/appsettings.json:ro
```

Environment variables still take precedence over the mounted file, so you can combine both approaches.
