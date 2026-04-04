# Docker Quickstart

This guide gets the full Relate Mail platform running with Docker Compose using pre-built images from GitHub Container Registry. No local build tools are required beyond Docker itself.

## Prerequisites

- **Docker** 24+ with Docker Compose v2 (`docker compose` command)

## 1. Clone and Configure

Clone the repository (you only need the `docker/` directory, but cloning the full repo is simplest):

```bash
git clone https://github.com/four-robots/relate-mail.git
cd relate-mail
```

Copy the example environment file and set your database password:

```bash
cp docker/.env.example docker/.env
```

Open `docker/.env` in your editor and set `POSTGRES_PASSWORD` to a strong password. This is the only required change to get started. All other settings have sensible defaults.

::: warning
The `POSTGRES_PASSWORD` variable is required. Docker Compose will refuse to start if it is empty.
:::

## 2. Start All Services

```bash
cd docker
docker compose -f docker-compose.ghcr.yml up -d
```

This pulls and starts five containers:

| Service | Image | Exposed Ports | Description |
|---|---|---|---|
| **postgres** | `postgres:16-alpine` | 5432 (internal) | PostgreSQL database |
| **api** | `ghcr.io/four-robots/relate-mail-api` | **8080** | REST API + SignalR hub |
| **smtp** | `ghcr.io/four-robots/relate-mail-smtp` | **25**, **587**, **465** | SMTP server (MX + submission) |
| **pop3** | `ghcr.io/four-robots/relate-mail-pop3` | **110**, **995** | POP3 server |
| **imap** | `ghcr.io/four-robots/relate-mail-imap` | **143**, **993** | IMAP server |

The API container automatically runs database migrations on startup.

::: info Screenshot
![Screenshot: Docker services running](./screenshots/docker-services.png)

_TODO: Add screenshot of `docker compose ps` showing all services healthy_
:::

## 3. Verify Services Are Healthy

Docker Compose includes health checks for every service. Check their status with:

```bash
docker compose -f docker-compose.ghcr.yml ps
```

All services should show a status of `healthy` after 30-60 seconds. You can also check individual health endpoints directly:

```bash
# API health check
curl http://localhost:8080/healthz

# SMTP health check
curl http://localhost:8081/healthz

# POP3 health check
curl http://localhost:8082/healthz

# IMAP health check
curl http://localhost:8083/healthz
```

Each health endpoint returns HTTP 200 when the service is ready to accept connections.

## 4. Access the API

The REST API is available at **http://localhost:8080**. Without OIDC configured, it runs in development mode with no authentication required:

```bash
# List emails in the inbox
curl http://localhost:8080/api/emails

# Check server capabilities
curl http://localhost:8080/api/discovery
```

## 5. Connect an Email Client

Once the services are running, you can connect a standard email client (Thunderbird, Apple Mail, Outlook, etc.) using the protocol servers:

| Protocol | Server | Port | Security |
|---|---|---|---|
| IMAP | `localhost` | 143 | STARTTLS |
| IMAP | `localhost` | 993 | SSL/TLS |
| POP3 | `localhost` | 110 | STLS |
| POP3 | `localhost` | 995 | SSL/TLS |
| SMTP (send) | `localhost` | 587 | STARTTLS |
| SMTP (send) | `localhost` | 465 | SSL/TLS |

Authentication uses API keys. You can create an API key through the REST API's SMTP credentials endpoint.

## Common Operations

### View logs

```bash
cd docker
docker compose -f docker-compose.ghcr.yml logs -f         # All services
docker compose -f docker-compose.ghcr.yml logs -f api      # API only
docker compose -f docker-compose.ghcr.yml logs -f smtp     # SMTP only
```

### Stop all services

```bash
cd docker
docker compose -f docker-compose.ghcr.yml down
```

### Stop and remove all data

```bash
cd docker
docker compose -f docker-compose.ghcr.yml down -v
```

The `-v` flag removes the PostgreSQL data volume, giving you a clean slate on the next start.

### Update to latest images

```bash
cd docker
docker compose -f docker-compose.ghcr.yml pull
docker compose -f docker-compose.ghcr.yml up -d
```

## Development Mode with Docker

If you want to build images from source instead of using pre-built GHCR images, use the default compose file:

```bash
cd docker
docker compose up --build
```

To expose additional debug ports (useful during development), layer the dev override:

```bash
cd docker
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

## Environment Variables

The `.env` file controls all service configuration. Key variables include:

| Variable | Required | Default | Description |
|---|---|---|---|
| `POSTGRES_PASSWORD` | Yes | — | Database password |
| `POSTGRES_USER` | No | `postgres` | Database username |
| `POSTGRES_DB` | No | `relate_mail` | Database name |
| `OIDC_AUTHORITY` | No | — | OIDC provider URL (leave empty for dev mode) |
| `OIDC_AUDIENCE` | No | — | OIDC audience identifier |
| `INTERNAL_API_KEY` | No | — | API key for service-to-service communication |
| `SMTP_SERVER_NAME` | No | `localhost` | Hostname the SMTP server announces |
| `SMTP_MX_ENABLED` | No | `false` | Enable MX inbound on port 25 |
| `SMTP_MX_HOSTED_DOMAIN` | No | — | Domain to accept inbound mail for |

See `docker/.env.example` for the full list of available variables with descriptions.
