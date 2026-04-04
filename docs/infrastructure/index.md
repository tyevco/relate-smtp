# Infrastructure Overview

Relate Mail is designed as a set of cooperating services that can be deployed together on a single machine or distributed across multiple hosts. All services share a single PostgreSQL database, which simplifies operations and keeps data consistent across protocols.

## Deployment Options

### Docker (Recommended)

Docker is the recommended deployment method for Relate Mail. Four container images are published to the GitHub Container Registry (GHCR), each running a single concern:

| Image | Purpose | Key Ports |
|-------|---------|-----------|
| `ghcr.io/four-robots/relate-mail-api` | REST API + web frontend | 8080 |
| `ghcr.io/four-robots/relate-mail-smtp` | SMTP server (submission + MX) | 587, 465, 25 |
| `ghcr.io/four-robots/relate-mail-pop3` | POP3 server | 110, 995 |
| `ghcr.io/four-robots/relate-mail-imap` | IMAP server | 143, 993 |

All four images are built from the same multi-stage Dockerfile and share the same codebase. They are published for both `linux/amd64` and `linux/arm64`, so they run natively on x86 servers, Apple Silicon Macs, and ARM-based cloud instances (AWS Graviton, Ampere Altra, etc.).

Docker Compose files are provided for three scenarios:

- **Local build** -- build images from source with `docker-compose.yml`
- **Pre-built images** -- pull from GHCR with `docker-compose.ghcr.yml`
- **Development** -- overlay `docker-compose.dev.yml` for debug ports and verbose logging

See [Docker deployment](./docker/) for full details.

### Local Development

For development, you can run each service directly on your machine without Docker. This requires:

- .NET 10 SDK for the backend services
- Node.js 22+ for the web frontend
- PostgreSQL 16 (run via Docker or install locally)

The API includes auto-migration in development mode, so the database schema is applied automatically on startup. See the [Development Setup](../contributing/development-setup) guide for step-by-step instructions.

### Cloud Deployment

Relate Mail can be deployed to any cloud provider that supports Docker containers. Common options include:

- **VPS / Dedicated Server** -- Run Docker Compose directly on a Linux server (DigitalOcean, Hetzner, Linode, etc.)
- **Kubernetes** -- Deploy each image as a separate Deployment with a shared PostgreSQL StatefulSet or managed database
- **AWS ECS / Google Cloud Run / Azure Container Apps** -- Use the GHCR images with a managed PostgreSQL instance
- **Railway / Fly.io / Render** -- Platform-as-a-service options that support Docker images

Regardless of hosting, the deployment model is the same: run the four containers, point them at a PostgreSQL database, and configure via environment variables.

## Runtime Configuration

Relate Mail follows a **deploy once, configure via environment variables** philosophy. The same container images work for development, staging, and production -- only the configuration changes. There is no need to rebuild images when changing settings.

Key configuration areas include:

- **Database** -- PostgreSQL connection string
- **Authentication** -- OIDC provider URL and audience (optional; omit for development mode)
- **TLS** -- Certificate paths for encrypted protocol connections
- **Protocol toggles** -- Enable or disable SMTP, POP3, and IMAP independently
- **MX endpoint** -- Accept inbound internet mail on port 25 for hosted domains
- **CORS** -- Allowed origins for the web API

See [Configuration](./configuration/) for the complete reference.

## Multi-Platform Support

All Docker images are built for two platforms:

- `linux/amd64` -- Standard x86-64 servers and desktops
- `linux/arm64` -- ARM-based servers (AWS Graviton, Ampere), Apple Silicon Macs, Raspberry Pi 4+

Docker automatically selects the correct platform when pulling images, so no manual selection is needed.

## Architecture Summary

```
                     ┌─────────────┐
                     │  PostgreSQL  │
                     │   (shared)   │
                     └──────┬──────┘
                            │
          ┌─────────┬───────┼───────┬─────────┐
          │         │       │       │         │
     ┌────┴────┐ ┌──┴──┐ ┌─┴──┐ ┌──┴──┐     │
     │   API   │ │SMTP │ │POP3│ │IMAP │     │
     │ :8080   │ │:587 │ │:110│ │:143 │     │
     │ (+ web) │ │:465 │ │:995│ │:993 │     │
     └─────────┘ │:25  │ └────┘ └─────┘     │
                 └─────┘                     │
                            ┌────────────────┘
                            │ Internal API
                            │ (new-email notifications)
```

The SMTP, POP3, and IMAP hosts communicate with the API service over an internal HTTP channel for new-email notifications and SignalR real-time updates. This connection uses a pre-shared internal API key configured via the `Internal__ApiKey` environment variable.
