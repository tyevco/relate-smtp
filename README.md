[![CI](https://github.com/four-robots/relate-mail/actions/workflows/ci.yml/badge.svg)](https://github.com/four-robots/relate-mail/actions/workflows/ci.yml)
[![CodeQL](https://github.com/four-robots/relate-mail/actions/workflows/codeql.yml/badge.svg)](https://github.com/four-robots/relate-mail/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/four-robots/relate-mail/badge)](https://scorecard.dev/viewer/?uri=github.com/four-robots/relate-mail)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GHCR](https://img.shields.io/badge/GHCR-images-blue?logo=github)](https://github.com/orgs/four-robots/packages?repo_name=relate-mail)

# Relate Mail

A full-stack email server and management system with SMTP, POP3, IMAP, and REST API access.

## Features

### Email Protocols
- **SMTP Server** (ports 587, 465) - Authenticated email submission with STARTTLS/SSL
- **SMTP MX Endpoint** (port 25) - Accept inbound internet mail for hosted domains (open relay prevention)
- **POP3 Server** (ports 110, 995) - Retrieve emails via standard POP3 protocol (RFC 1939)
- **IMAP Server** (ports 143, 993) - Full IMAP4rev2 support (RFC 9051) with ENVELOPE, BODYSTRUCTURE, and AUTHENTICATE PLAIN

### Outbound Mail (MTA)
- **Compose & Send** - Create and send emails with To/Cc/Bcc recipients (up to 100 per email)
- **Reply & Forward** - Threaded replies (Reply, Reply All) and forwarding with attachment carry-over
- **Drafts** - Save, edit, and resume email drafts
- **Outbox** - Track queued, sending, and failed deliveries with retry logic
- **Sent Mail** - View sent email history with per-address filtering
- **RFC-compliant Threading** - Maintains Message-Id, In-Reply-To, and References headers

### Email Management
- **Labels** - Custom color-coded labels with sort ordering; assign to emails manually or via filters
- **Filters** - Rule-based automation: match on sender, subject, body, or attachments; actions include mark-as-read, assign label, and delete
- **Filter Testing** - Test filters against recent emails before enabling
- **Search** - Full-text search across inbox and sent mail

### Web UI
- **Inbox** - Email list with read/unread status, pagination, and preview
- **Compose** - Rich compose page with dynamic To/Cc/Bcc, from-address selection, and auto-quoting in replies
- **Drafts & Outbox** - Manage unsaved and in-flight emails
- **Filters** - Visual filter builder with condition/action display and enable/disable toggles
- **Preferences** - Theme (light/dark/system), display density, notification settings, email digest configuration
- **Profile** - Display name editing, additional email address management with verification
- **SMTP Settings** - API key generation with scope selection, connection info display, setup wizard

### Real-time & Notifications
- **SignalR Hub** (`/hubs/email`) - WebSocket-based real-time updates for new emails, read status changes, deletions, and unread counts
- **Web Push Notifications** - VAPID-based browser push for out-of-tab delivery
- **Email Digest** - Configurable daily or weekly email summaries

### Platform & Infrastructure
- **REST API** - Full programmatic email access with scoped permissions
- **Scoped API Keys** - Granular permissions: `smtp`, `pop3`, `imap`, `api:read`, `api:write`, `app`
- **Server Discovery** - `/api/discovery` endpoint advertises enabled features and server capabilities
- **Protocol Toggles** - Enable/disable SMTP, POP3, IMAP independently
- **PostgreSQL Database** - Production-ready database with full concurrency support
- **Docker Ready** - Multi-platform images (amd64/arm64) published to GitHub Container Registry
- **Runtime Configuration** - Deploy once, configure anywhere (no rebuild needed)
- **Mobile App** - React Native (Expo 54) with multi-account support, swipe actions, biometric auth
- **Desktop App** - Tauri 2 native app for Windows, macOS, and Linux with keyboard shortcuts

## Quick Start

### Using Pre-built Docker Images

```bash
# Copy environment template
cp docker/.env.example docker/.env

# Edit docker/.env with your settings (optional - works without OIDC)

# Start all services
cd docker
docker compose -f docker-compose.ghcr.yml up -d
```

Access the web UI at http://localhost:3000

### Docker Images

All images are published to GitHub Container Registry:

| Image | Purpose | Ports |
|-------|---------|-------|
| `ghcr.io/four-robots/relate-mail-api` | REST API + Web UI | 8080 |
| `ghcr.io/four-robots/relate-mail-smtp` | SMTP Server | 587, 465 |
| `ghcr.io/four-robots/relate-mail-pop3` | POP3 Server | 110, 995 |
| `ghcr.io/four-robots/relate-mail-imap` | IMAP Server | 143, 993 |

## Architecture

### Backend (.NET 10.0)

- **Relate.Smtp.Api** - REST API with JWT/OIDC authentication (serves bundled web frontend)
- **Relate.Smtp.SmtpHost** - SMTP server with API key authentication
- **Relate.Smtp.Pop3Host** - POP3 server with API key authentication
- **Relate.Smtp.ImapHost** - IMAP4rev2 server with API key authentication
- **Relate.Smtp.Core** - Domain entities and interfaces
- **Relate.Smtp.Infrastructure** - EF Core data access with PostgreSQL

### Frontend (React + TypeScript)

- **Vite** - Build tool and dev server
- **TanStack Router** - File-based routing
- **TanStack Query** - Server state management
- **Tailwind CSS** - Styling
- **react-oidc-context** - Optional OIDC authentication

## Development

### Prerequisites

- .NET 10.0 SDK
- Node.js 22+
- Docker (optional)

### Local Development

**Backend API:**
```bash
cd api/src/Relate.Smtp.Api
dotnet run
# Runs on http://localhost:5000
```

**SMTP Server:**
```bash
cd api/src/Relate.Smtp.SmtpHost
dotnet run
# Listens on ports 587, 465
```

**POP3 Server:**
```bash
cd api/src/Relate.Smtp.Pop3Host
dotnet run
# Listens on ports 110, 995
```

**IMAP Server:**
```bash
cd api/src/Relate.Smtp.ImapHost
dotnet run
# Listens on ports 143, 993
```

**Web Frontend:**
```bash
cd web
npm install
npm run dev
# Runs on http://localhost:5492
```

### Database Migrations

```bash
cd api
dotnet ef migrations add MigrationName \
  --project src/Relate.Smtp.Infrastructure \
  --startup-project src/Relate.Smtp.Api

dotnet ef database update \
  --project src/Relate.Smtp.Infrastructure \
  --startup-project src/Relate.Smtp.Api
```

## Docker Deployment

### Quick Deploy with GHCR Images

```bash
# Set environment variables
export GITHUB_REPOSITORY="four-robots/relate-mail"
export IMAGE_TAG="latest"  # or specific version like "v1.0.0"

# Start services
docker compose -f docker/docker-compose.ghcr.yml up -d

# Check status
docker compose -f docker/docker-compose.ghcr.yml ps

# View logs
docker compose -f docker/docker-compose.ghcr.yml logs -f
```

### Building Locally

```bash
cd docker
docker compose up -d --build
```

## Configuration

### Backend Environment Variables

**Database (PostgreSQL):**
```bash
ConnectionStrings__DefaultConnection=host=postgres;port=5432;database=relate-mail;user id=postgres;password=postgres
```

**OIDC Authentication (optional):**
```bash
Oidc__Authority=https://your-oidc-provider.com
Oidc__Audience=your-audience
```

**SMTP Server:**
```bash
Smtp__ServerName=smtp.example.com
Smtp__Port=587
Smtp__SecurePort=465
```

**POP3 Server:**
```bash
Pop3__ServerName=pop3.example.com
Pop3__Port=110
Pop3__SecurePort=995
Pop3__Enabled=true
```

**IMAP Server:**
```bash
Imap__ServerName=imap.example.com
Imap__Port=143
Imap__SecurePort=993
Imap__Enabled=true
```

### Runtime Configuration

The application supports **runtime configuration** - deploy once, configure anywhere:

```bash
# Set OIDC via environment variables (no rebuild needed!)
docker compose -f docker/docker-compose.ghcr.yml up -d \
  -e Oidc__Authority=https://sso.example.com \
  -e Oidc__Audience=your-audience
```

The same Docker images work across dev, staging, and production with different settings.

**Development mode:** Leave `Oidc__Authority` empty to run without authentication.

## API Reference

### Authentication

All authenticated endpoints accept either:
- **OIDC/JWT token:** `Authorization: Bearer {jwt_token}` (first-party web/mobile clients)
- **API key:** `Authorization: Bearer {api_key}` or `Authorization: ApiKey {api_key}` (third-party integrations)

### Generate API Key

1. Log into web UI at http://localhost:8080
2. Navigate to "SMTP Settings"
3. Click "Generate New Key"
4. Select permissions:
   - `smtp` - Send emails via SMTP
   - `pop3` - Retrieve emails via POP3
   - `imap` - Retrieve emails via IMAP
   - `api:read` - Read emails via REST API
   - `api:write` - Modify/delete emails via REST API
5. Copy the API key (shown only once)

### External Email Endpoints

**Base URL:** `/api/external/emails` | **Auth:** API key with `api:read` / `api:write` scope

| Endpoint | Method | Scope | Description |
|----------|--------|-------|-------------|
| `/` | GET | `api:read` | List inbox emails (paginated) |
| `/search` | GET | `api:read` | Search emails by query string |
| `/sent` | GET | `api:read` | List sent emails |
| `/{id}` | GET | `api:read` | Get email details |
| `/{id}` | PATCH | `api:write` | Mark read/unread |
| `/{id}` | DELETE | `api:write` | Delete email |

### Outbound Email Endpoints

**Base URL:** `/api/outbound` | **Auth:** OIDC/JWT or API key

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/send` | POST | Send email immediately (To/Cc/Bcc, up to 100 recipients) |
| `/drafts` | GET | List drafts (paginated) |
| `/drafts` | POST | Create a new draft |
| `/drafts/{id}` | GET | Get draft with recipients and attachments |
| `/drafts/{id}` | PUT | Update draft |
| `/drafts/{id}` | DELETE | Delete draft |
| `/drafts/{id}/send` | POST | Send a draft |
| `/reply/{emailId}` | POST | Reply to an email (supports `replyAll` flag) |
| `/forward/{emailId}` | POST | Forward an email (copies attachments) |
| `/outbox` | GET | View queued/sending emails |
| `/sent` | GET | View sent email history |
| `/{id}` | GET | Get outbound email detail |

### Labels Endpoints

**Base URL:** `/api/labels` | **Auth:** OIDC/JWT or API key

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | List all labels |
| `/` | POST | Create label (name, color, sort order) |
| `/{id}` | PUT | Update label |
| `/{id}` | DELETE | Delete label |
| `/emails/{emailId}` | POST | Add label to an email |
| `/emails/{emailId}/{labelId}` | DELETE | Remove label from an email |
| `/{labelId}/emails` | GET | List emails with a specific label |

### Filters Endpoints

**Base URL:** `/api/filters` | **Auth:** OIDC/JWT or API key

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | List all filters |
| `/` | POST | Create filter (conditions + actions) |
| `/{id}` | PUT | Update filter |
| `/{id}` | DELETE | Delete filter |
| `/{id}/test?limit=10` | POST | Test filter against recent emails |

**Filter conditions:** `FromAddressContains`, `SubjectContains`, `BodyContains`, `HasAttachments`
**Filter actions:** `MarkAsRead`, `AssignLabelId`, `Delete`

### Other Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/discovery` | GET | None | Server capabilities and enabled features |
| `/api/preferences` | GET/PUT | Yes | User preferences (theme, density, notifications) |
| `/api/profile` | GET/PUT | Yes | User profile and additional email addresses |
| `/api/pushSubscriptions` | POST/DELETE | Yes | Web push notification subscriptions |
| `/api/pushSubscriptions/vapid-public-key` | GET | None | VAPID public key for push setup |
| `/hubs/email` | WebSocket | JWT | Real-time email notifications (SignalR) |

### Example: List Emails

```bash
curl http://localhost:8080/api/external/emails \
  -H "Authorization: Bearer your_api_key"
```

### Example: Search Emails

```bash
curl "http://localhost:8080/api/external/emails/search?q=test&page=1" \
  -H "Authorization: Bearer your_api_key"
```

### Example: Send an Email

```bash
curl -X POST http://localhost:8080/api/outbound/send \
  -H "Authorization: Bearer your_token" \
  -H "Content-Type: application/json" \
  -d '{
    "to": ["recipient@example.com"],
    "subject": "Hello from Relate Mail",
    "body": "This is a test email.",
    "fromAddress": "you@example.com"
  }'
```

### Example: Create a Filter

```bash
curl -X POST http://localhost:8080/api/filters \
  -H "Authorization: Bearer your_token" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Auto-read newsletters",
    "fromAddressContains": "newsletter@",
    "markAsRead": true,
    "isEnabled": true
  }'
```

## Email Client Setup

Configure email clients to send and receive:

1. Generate an API key in the web UI (with appropriate scopes)
2. Configure your email client:

**Outgoing (SMTP):**
- Server: `localhost` (or your domain)
- Port: 587 (STARTTLS) or 465 (SSL/TLS)
- Username: Your email address
- Password: API key from web UI

**Incoming (IMAP)** - Recommended:
- Server: `localhost` (or your domain)
- Port: 143 (plain) or 993 (SSL/TLS)
- Username: Your email address
- Password: Same API key

**Incoming (POP3):**
- Server: `localhost` (or your domain)
- Port: 110 (plain) or 995 (SSL/TLS)
- Username: Your email address
- Password: Same API key

## GitHub Actions CI/CD

Images are automatically built and published to GHCR on every push. Security analysis runs via CodeQL and OpenSSF Scorecard.

### Workflow Triggers

- **Push to main/develop** - Builds and publishes images
- **Version tags** (`v1.0.0`) - Creates semantic version tags
- **Pull requests** - Builds but doesn't publish
- **Scheduled** - CodeQL and Scorecard run on cron schedules
- **Manual** - Run from Actions tab

### Publishing a New Version

```bash
# Create and push version tag
git tag v1.0.0
git push origin v1.0.0

# Wait for GitHub Actions to build and publish
# View progress at: https://github.com/four-robots/relate-mail/actions
```

### Image Tags Created

For tag `v1.0.0`:
- `v1.0.0` - Exact version
- `1.0.0` - Without v prefix
- `1.0` - Minor version
- `1` - Major version
- `latest` - Latest stable (main branch only)

### Platform Support

All images support:
- `linux/amd64` (x86_64)
- `linux/arm64` (ARM64, Apple Silicon, Raspberry Pi)

Docker automatically pulls the correct architecture for your platform.

## Common Operations

### Update to New Version

```bash
# Pull new images
docker compose -f docker/docker-compose.ghcr.yml pull

# Restart services
docker compose -f docker/docker-compose.ghcr.yml up -d
```

### View Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f api
docker compose logs -f smtp
docker compose logs -f pop3
docker compose logs -f imap
```

### Database Backup

```bash
# Backup PostgreSQL database
docker exec postgres pg_dump -U postgres relate-mail > backup.sql

# Restore
docker exec -i postgres psql -U postgres relate-mail < backup.sql
```

### Health Checks

```bash
# API + Web UI
curl http://localhost:8080/health

# SMTP (telnet)
telnet localhost 587

# POP3 (telnet)
telnet localhost 110

# IMAP (telnet)
telnet localhost 143
```

## Multiple Environments

Deploy the same Docker images to different environments by setting environment variables:

**Development:**
```bash
docker compose -f docker/docker-compose.ghcr.yml up -d
# Default: No OIDC, uses dev mode
```

**Production:**
```bash
export OIDC_AUTHORITY=https://sso.example.com
export OIDC_AUDIENCE=your-audience
docker compose -f docker/docker-compose.ghcr.yml up -d
```

Same images, different configurations!

## Troubleshooting

### Port Conflicts

If ports are already in use, edit `docker-compose.yml`:

```yaml
ports:
  - "2587:587"  # Use different host port
```

### Database Connection Issues

Verify PostgreSQL connection:

```bash
# Test connection
docker exec postgres psql -U postgres -d relate-mail -c "SELECT 1;"

# Check logs
docker logs postgres
```

### Permission Issues

Fix volume permissions:

```bash
docker compose exec api chown -R app:app /data
```

### Container Won't Start

Check logs:

```bash
docker compose logs api
docker compose logs smtp
docker compose logs pop3
docker compose logs imap
docker compose logs postgres
```

### OIDC Authentication Not Working

1. Verify `OIDC_AUTHORITY` is accessible from browser
2. Ensure `OIDC_AUDIENCE` matches your OAuth2 configuration
3. Check browser console for errors
4. Verify the API is receiving the correct environment variables

## Security Best Practices

1. **Use version tags** in production (not `latest`)
2. **Enable OIDC authentication** instead of dev mode
3. **Configure PostgreSQL** with proper credentials and SSL
4. **Set up SSL/TLS** for SMTP/POP3/HTTPS
5. **Restrict port access** with firewall rules
6. **Regular backups** of database
7. **Keep images updated** for security patches
8. **Use secrets management** for sensitive config (don't commit credentials)

## Project Structure

```
relate-mail/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml               # Build, lint, test (backend + frontend)
│   │   ├── codeql.yml           # CodeQL security analysis
│   │   ├── scorecard.yml        # OpenSSF Scorecard analysis
│   │   ├── docker-publish.yml   # Multi-platform GHCR image publishing
│   │   ├── desktop-build.yml    # Desktop builds (Windows/macOS/Linux)
│   │   └── mobile-build.yml     # Mobile lint/test + EAS builds
│   ├── CODEOWNERS               # Code ownership rules
│   └── FUNDING.yml              # Sponsorship links
├── api/                          # Backend (.NET 10.0)
│   ├── src/
│   │   ├── Relate.Smtp.Api/     # REST API + SignalR hub (serves bundled web frontend)
│   │   ├── Relate.Smtp.SmtpHost/# SMTP server (submission + MX endpoint)
│   │   ├── Relate.Smtp.Pop3Host/# POP3 server
│   │   ├── Relate.Smtp.ImapHost/# IMAP4rev2 server
│   │   ├── Relate.Smtp.Core/    # Domain entities and interfaces
│   │   └── Relate.Smtp.Infrastructure/ # EF Core data access + migrations
│   ├── tests/                    # Unit, integration, and E2E tests
│   └── Dockerfile                # Multi-stage build (4 targets: api, smtp, pop3, imap)
├── web/                          # Web frontend (React + TypeScript)
│   └── src/
│       ├── routes/              # TanStack Router pages (inbox, compose, drafts, outbox,
│       │                        #   sent, filters, preferences, profile, smtp-settings)
│       ├── components/          # React components
│       ├── auth/                # OIDC authentication
│       ├── api/                 # API client
│       └── config.ts            # Runtime configuration
├── mobile/                       # React Native (Expo 54) mobile app
├── desktop/                      # Tauri 2 desktop app (Rust + TypeScript)
├── packages/
│   └── shared/                   # @relate/shared npm package (types, components, utils)
├── docker/                       # Docker Compose files
│   ├── docker-compose.yml       # Build locally (includes PostgreSQL)
│   ├── docker-compose.ghcr.yml  # Use GHCR images
│   └── .env.example             # Environment template
├── CHANGELOG.md                  # Version history and release notes
├── CLAUDE.md                     # Project conventions and architecture guide
├── CODE_OF_CONDUCT.md            # Community code of conduct
├── CONTRIBUTING.md               # Contribution guidelines
├── LICENSE                       # MIT License
├── SECURITY.md                   # Security policy
├── SUPPORT.md                    # Support resources
└── README.md                     # This file
```

## Technology Stack

**Backend:**
- .NET 10.0 (ASP.NET Core, Worker Services)
- Entity Framework Core with PostgreSQL
- SignalR (real-time WebSocket communication)
- BCrypt.Net-Next (password hashing)
- SmtpServer library 11.1.0 (SMTP protocol)
- Custom POP3 implementation (RFC 1939)
- Custom IMAP4rev2 implementation (RFC 9051)
- MimeKit (email composition and parsing)
- Web Push (VAPID-based browser notifications)

**Web Frontend:**
- React 19 + TypeScript
- Vite (build tool)
- TanStack Router (file-based routing)
- TanStack Query (server state) + Jotai (client state)
- Tailwind CSS 4.1 + CVA (component variants)
- Radix UI (accessible primitives) + Lucide React (icons)
- react-oidc-context (authentication)
- MSW (API mocking in tests)

**Mobile:**
- React Native (Expo SDK 54)
- Expo Router v4 (file-based routing)
- TanStack Query + Zustand (state management)
- NativeWind (Tailwind CSS for React Native)
- Detox (E2E testing)

**Desktop:**
- Tauri 2 (Rust + TypeScript)
- Native window management and credential storage

**Infrastructure:**
- Docker (multi-stage builds, multi-platform amd64/arm64)
- GitHub Actions (CI/CD)
- GitHub Container Registry (image hosting)
- nginx (web server)
- CodeQL + OpenSSF Scorecard (security analysis)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file.

## Support

See [SUPPORT.md](SUPPORT.md) for ways to get help and report issues.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup, branch naming, testing requirements, and pull request guidelines.

## Acknowledgments

Built with ❤️ using modern .NET and React technologies.
