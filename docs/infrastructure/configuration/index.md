# Configuration Overview

Relate Mail follows a **deploy once, configure via environment variables** philosophy. The same Docker images and binaries work across development, staging, and production environments -- only the configuration changes at runtime.

## Configuration Hierarchy

The .NET backend uses a layered configuration system where later sources override earlier ones:

```
appsettings.json              (base defaults, checked into source)
  └── appsettings.{Env}.json  (environment-specific overrides)
       └── Environment variables  (highest priority, runtime overrides)
```

In practice, `appsettings.json` provides sensible defaults, and environment variables override specific values at deployment time. You rarely need to modify the JSON files.

## Environment Variable Mapping

.NET configuration uses a hierarchical key structure (e.g., `Smtp.Port`). Environment variables map to this hierarchy using double underscores (`__`) as separators:

| JSON Path | Environment Variable |
|-----------|---------------------|
| `ConnectionStrings.DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Smtp.Port` | `Smtp__Port` |
| `Smtp.Mx.Enabled` | `Smtp__Mx__Enabled` |
| `Oidc.Authority` | `Oidc__Authority` |
| `Cors.AllowedOrigins[0]` | `Cors__AllowedOrigins__0` |

### Array Values

Array configuration values use numeric indices as keys:

```bash
# JSON: { "Smtp": { "Mx": { "HostedDomains": ["example.com", "mail.example.com"] } } }
Smtp__Mx__HostedDomains__0=example.com
Smtp__Mx__HostedDomains__1=mail.example.com

# JSON: { "Cors": { "AllowedOrigins": ["https://app.example.com"] } }
Cors__AllowedOrigins__0=https://app.example.com
```

### Boolean Values

Boolean settings accept `true` or `false` (case-insensitive):

```bash
Smtp__Enabled=true
Smtp__Mx__Enabled=false
Pop3__Enabled=true
```

## Frontend Runtime Configuration

The web frontend does not bake configuration into the build. Instead, it fetches runtime configuration from the API at startup:

```
GET /api/config
```

This endpoint returns the OIDC settings and other frontend-relevant configuration. The API reads these from its own configuration (environment variables or appsettings) and serves them to the frontend. This means you can change OIDC providers or other frontend settings by updating the API's environment variables and restarting -- no frontend rebuild required.

Build-time variables (`VITE_*`) are still available for cases where you build the frontend separately, but the Docker images use the runtime config endpoint.

## Configuration Areas

| Area | Key Prefix | Description |
|------|-----------|-------------|
| [Database](./environment-variables#database) | `ConnectionStrings__` | PostgreSQL connection |
| [Authentication](./environment-variables#authentication) | `Oidc__`, `Jwt__` | OIDC provider, JWT settings |
| [SMTP](./environment-variables#smtp) | `Smtp__` | SMTP server, MX endpoint, TLS |
| [POP3](./environment-variables#pop3) | `Pop3__` | POP3 server, TLS |
| [IMAP](./environment-variables#imap) | `Imap__` | IMAP server, TLS |
| [Outbound Mail](./environment-variables#outbound-mail) | `OutboundMail__` | Email delivery, relay, retry |
| [Security](./environment-variables#security) | `Security__` | Rate limiting, auth salt |
| [CORS](./environment-variables#cors) | `Cors__` | Cross-origin request settings |
| [Internal](./environment-variables#internal) | `Internal__` | Service-to-service communication |
| [Frontend](./environment-variables#frontend) | `VITE_` | Build-time frontend settings |

## Development Mode

When `Oidc__Authority` is not set (empty or missing), the system runs in **development mode**:

- Authentication is relaxed -- the API accepts development JWT tokens
- The web frontend skips the OIDC login flow
- Auto-migration applies pending database migrations on startup

This makes it easy to get started without configuring an identity provider. For production, always set `Oidc__Authority` to your OIDC provider URL.

## Next Steps

- [Environment Variables](./environment-variables) -- Complete reference of all configuration options
- [appsettings.json](./appsettings) -- Default configuration file structure
- [Email Client Setup](./email-client-setup) -- Configure Thunderbird, Outlook, and Apple Mail
