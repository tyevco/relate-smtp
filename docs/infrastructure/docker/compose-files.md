# Docker Compose Files

The `docker/` directory contains three Compose files that cover different deployment scenarios. All files use Compose specification version 3.8.

## File Overview

| File | Purpose | Use Case |
|------|---------|----------|
| `docker-compose.yml` | Local build from source | Development, testing, CI validation |
| `docker-compose.ghcr.yml` | Pre-built GHCR images | Production, staging, quick start |
| `docker-compose.dev.yml` | Development overrides | Layered on top of `docker-compose.yml` for debugging |

## `docker-compose.yml` -- Local Build

This is the primary compose file for building from source. It defines five services that together form a complete Relate Mail deployment.

### Services

**postgres**
- Image: `postgres:16-alpine`
- Stores all email, user, and configuration data
- Persists data via a named volume (`postgres-data`)
- Health check: `pg_isready -U postgres` (5s interval, 5 retries)
- Resource limits: 1 CPU, 1 GB RAM (reserves 0.25 CPU, 256 MB)

**api**
- Built from `api/Dockerfile` with `target: api`
- Depends on `postgres` (waits for healthy status)
- Exposes port 8080 for the REST API and web frontend
- Receives OIDC configuration via environment variables
- Resource limits: 2 CPU, 2 GB RAM

**smtp**
- Built from `api/Dockerfile` with `target: smtp`
- Depends on `postgres` (waits for healthy status)
- Exposes ports 587 (STARTTLS), 465 (implicit TLS), and 25 (MX inbound)
- Connects to the API service at `http://api:8080` for internal notifications
- MX endpoint is disabled by default; enable with `SMTP_MX_ENABLED=true`

**pop3**
- Built from `api/Dockerfile` with `target: pop3`
- Depends on `postgres` (waits for healthy status)
- Exposes ports 110 (POP3) and 995 (POP3S)

**imap**
- Built from `api/Dockerfile` with `target: imap`
- Depends on `postgres` (waits for healthy status)
- Exposes ports 143 (IMAP) and 993 (IMAPS)

### Networking

All services join a shared bridge network called `relate-network`. This allows services to reach each other by container name (e.g., `postgres`, `api`). The SMTP service uses this to send internal notifications to `http://api:8080`.

### Usage

```bash
cd docker

# Create required .env file
cat > .env << 'EOF'
POSTGRES_PASSWORD=your-secure-password
INTERNAL_API_KEY=your-internal-key
EOF

# Build and start all services
docker compose up --build -d

# View logs
docker compose logs -f

# Stop all services
docker compose down

# Stop and remove data volumes
docker compose down -v
```

## `docker-compose.ghcr.yml` -- Pre-Built Images

This compose file pulls pre-built images from the GitHub Container Registry instead of building from source. It defines the same five services with identical configuration, but uses `image:` directives instead of `build:` directives.

### Image References

Each service image follows the pattern:

```
ghcr.io/${GITHUB_REPOSITORY:-four-robots/relate-mail}-<service>:${IMAGE_TAG:-latest}
```

The `IMAGE_TAG` environment variable controls which version to deploy. If not set, it defaults to `latest`.

### Usage

```bash
cd docker

# Deploy latest images
docker compose -f docker-compose.ghcr.yml up -d

# Deploy a specific version
IMAGE_TAG=1.2.3 docker compose -f docker-compose.ghcr.yml up -d

# Update to newest images
docker compose -f docker-compose.ghcr.yml pull
docker compose -f docker-compose.ghcr.yml up -d
```

### Differences from Local Build

The GHCR compose file is functionally identical to the local build file, with two key differences:

1. Uses `image:` instead of `build:` -- pulls pre-built images rather than building
2. No `deploy.resources` blocks -- resource limits are omitted since production environments may have different sizing needs. Add them back as needed for your infrastructure.
3. Health checks on protocol servers use the HTTP health endpoint (`wget --spider http://localhost:808x/healthz`) rather than protocol-level checks, since `nc` may not be available in the Alpine images.

## `docker-compose.dev.yml` -- Development Overrides

This is a partial compose file meant to be layered on top of `docker-compose.yml` using Docker Compose's multiple-file feature. It adds development-friendly settings that should not be used in production.

### Overrides

**postgres**
- Exposes port 5432 to the host, allowing direct database access with `psql` or a GUI tool

**api**
- Adds port 5000 mapping (5000 -> 8080), providing an additional access point
- Sets `ASPNETCORE_ENVIRONMENT=Development` for detailed error pages and auto-migration
- Adds CORS origins for local frontend dev servers (`http://localhost:5173`, `http://localhost:3000`)

**smtp**
- Sets `Logging__LogLevel__Default=Debug` for verbose protocol logging

### Usage

Layer the dev overrides on top of the local build file:

```bash
cd docker

# Start with development overrides
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Or set it as a default in .env
echo "COMPOSE_FILE=docker-compose.yml:docker-compose.dev.yml" >> .env
docker compose up --build
```

With dev overrides active, you can:
- Access the API at both `http://localhost:8080` and `http://localhost:5000`
- Connect to PostgreSQL directly at `localhost:5432`
- See detailed SMTP protocol logs

## Environment Variable Passthrough

All compose files read configuration from a `.env` file in the `docker/` directory. The following variables are referenced:

| Variable | Required | Default | Used By |
|----------|----------|---------|---------|
| `POSTGRES_PASSWORD` | Yes | -- | postgres, all services (connection string) |
| `POSTGRES_USER` | No | `postgres` | postgres, all services |
| `POSTGRES_DB` | No | `relate_mail` | postgres, all services |
| `OIDC_AUTHORITY` | No | (empty) | api |
| `OIDC_AUDIENCE` | No | (empty) | api |
| `SMTP_SERVER_NAME` | No | `localhost` | smtp |
| `SMTP_MX_ENABLED` | No | `false` | smtp |
| `SMTP_MX_HOSTED_DOMAIN` | No | (empty) | smtp |
| `SMTP_MX_VALIDATE_RECIPIENTS` | No | `true` | smtp |
| `POP3_SERVER_NAME` | No | `localhost` | pop3 |
| `IMAP_SERVER_NAME` | No | `localhost` | imap |
| `INTERNAL_API_KEY` | No | (empty) | smtp |
| `IMAGE_TAG` | No | `latest` | GHCR compose only |

Example `.env` file:

```env
POSTGRES_PASSWORD=my-secure-password
POSTGRES_USER=postgres
POSTGRES_DB=relate_mail
INTERNAL_API_KEY=shared-secret-for-service-communication
SMTP_SERVER_NAME=mail.example.com
SMTP_MX_ENABLED=true
SMTP_MX_HOSTED_DOMAIN=example.com
```

## Service Dependency Graph

Services start in dependency order. All application services wait for PostgreSQL to be healthy before starting:

```
postgres (healthy)
    ├── api
    ├── smtp
    ├── pop3
    └── imap
```

The `depends_on` directive with `condition: service_healthy` ensures that no application container starts until PostgreSQL is accepting connections. This prevents startup errors from database connection failures.
