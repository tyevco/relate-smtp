# Docker Deployment

Relate Mail publishes four Docker images to the GitHub Container Registry (GHCR). These images are the recommended way to deploy and run the platform.

## Container Images

All images are published under the `ghcr.io/four-robots/relate-mail-*` namespace:

| Image | Description |
|-------|-------------|
| `ghcr.io/four-robots/relate-mail-api` | REST API server with embedded web frontend |
| `ghcr.io/four-robots/relate-mail-smtp` | SMTP server for sending and receiving email |
| `ghcr.io/four-robots/relate-mail-pop3` | POP3 server for email retrieval |
| `ghcr.io/four-robots/relate-mail-imap` | IMAP server for email access |

### Multi-Platform Support

Every image is built for two architectures:

- `linux/amd64` -- x86-64 servers, Intel/AMD desktops
- `linux/arm64` -- ARM servers (AWS Graviton, Ampere Altra), Apple Silicon Macs, Raspberry Pi 4+

Docker automatically selects the correct architecture when pulling. No manual platform selection is needed.

### Image Tags

Tags follow a consistent scheme across all four images:

| Tag Pattern | Example | Description |
|-------------|---------|-------------|
| `latest` | `latest` | Latest build from the `main` branch. Updated on every push to main. |
| `main` | `main` | Alias for the latest main branch build. |
| `<version>` | `1.2.3` | Exact semantic version, created from a `v1.2.3` Git tag. |
| `<major>.<minor>` | `1.2` | Tracks the latest patch within a minor version. |
| `<major>` | `1` | Tracks the latest minor and patch within a major version. |
| `main-<sha>` | `main-a1b2c3d` | Specific commit on main, useful for pinning to an exact build. |

**Recommended for production:** Pin to a specific version tag (e.g., `1.2.3`) or a minor version tag (e.g., `1.2`) to avoid unexpected changes. Use `latest` only for development or testing.

## Port Reference

| Service | Port | Protocol | Description |
|---------|------|----------|-------------|
| API | 8080 | HTTP | REST API and web frontend |
| SMTP | 587 | SMTP (STARTTLS) | Authenticated email submission |
| SMTP | 465 | SMTPS (implicit TLS) | Authenticated email submission |
| SMTP | 25 | SMTP (MX) | Inbound internet mail (when MX is enabled) |
| POP3 | 110 | POP3 | Email retrieval (plaintext or STARTTLS) |
| POP3 | 995 | POP3S (implicit TLS) | Encrypted email retrieval |
| IMAP | 143 | IMAP | Email access (plaintext or STARTTLS) |
| IMAP | 993 | IMAPS (implicit TLS) | Encrypted email access |

Additionally, each protocol server exposes an internal health check HTTP endpoint:

| Service | Health Check Port | Endpoint |
|---------|-------------------|----------|
| SMTP | 8081 | `/healthz` |
| POP3 | 8082 | `/healthz` |
| IMAP | 8083 | `/healthz` |

These health check ports are used internally by Docker health checks and should not be exposed to the public internet.

## Quick Start

The fastest way to get running is with the pre-built GHCR images:

```bash
# Clone the repository (for the compose files)
git clone https://github.com/four-robots/relate-mail.git
cd relate-mail/docker

# Create a .env file with required settings
cat > .env << 'EOF'
POSTGRES_PASSWORD=your-secure-password-here
INTERNAL_API_KEY=your-internal-key-here
EOF

# Start all services
docker compose -f docker-compose.ghcr.yml up -d
```

The web interface will be available at `http://localhost:8080`.

To use a specific version instead of `latest`:

```bash
IMAGE_TAG=1.2.3 docker compose -f docker-compose.ghcr.yml up -d
```

## Building from Source

To build the images locally from the repository source:

```bash
cd relate-mail/docker
docker compose up --build -d
```

This uses the multi-stage Dockerfile at `api/Dockerfile` to build all four images. See [Dockerfile Reference](./dockerfile) for details on the build stages.

::: info Screenshot
**[Screenshot placeholder: Docker services running]**

_TODO: Add screenshot of Docker services running in a terminal or Docker Desktop_
:::

## Next Steps

- [Dockerfile Reference](./dockerfile) -- Understand the multi-stage build
- [Compose Files](./compose-files) -- Detailed breakdown of each compose file
- [Health Checks](./health-checks) -- Monitoring and resource configuration
- [Environment Variables](../configuration/environment-variables) -- Complete configuration reference
