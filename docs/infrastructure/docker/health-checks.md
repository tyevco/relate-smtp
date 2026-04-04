# Health Checks and Resource Limits

Each service in the Docker Compose deployment includes a health check to verify it is running correctly, along with resource limits to prevent any single service from consuming all available system resources.

## Health Check Configuration

### PostgreSQL

```yaml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-postgres}"]
  interval: 5s
  timeout: 5s
  retries: 5
```

The `pg_isready` utility checks whether PostgreSQL is accepting connections. This health check runs every 5 seconds with a tight interval because all other services depend on it -- the faster PostgreSQL reports healthy, the sooner the application services can start.

### API Server

```yaml
healthcheck:
  test: ["CMD-SHELL", "wget -q --spider http://localhost:8080/healthz || exit 1"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 10s
```

The API exposes a `/healthz` endpoint that returns HTTP 200 when the server is ready to handle requests. The 10-second `start_period` gives the .NET runtime time to initialize and apply any pending database migrations before the first health check runs.

### SMTP Server

**Local build** (`docker-compose.yml`):
```yaml
healthcheck:
  test: ["CMD-SHELL", "echo 'QUIT' | nc -w 5 localhost 587 | grep -q '^220' || exit 1"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 10s
```

This sends a TCP connection to port 587 and verifies the SMTP server responds with a `220` greeting, which is the standard SMTP server ready response defined in RFC 5321. The `QUIT` command is sent to cleanly close the connection.

**GHCR images** (`docker-compose.ghcr.yml`):
```yaml
healthcheck:
  test: ["CMD-SHELL", "wget -q --spider http://localhost:8081/healthz || exit 1"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 5s
```

The GHCR variant uses the HTTP health check endpoint on port 8081 instead, since `nc` (netcat) may not be installed in the Alpine-based runtime image. The `HealthCheck__Url` environment variable configures this endpoint.

### POP3 Server

**Local build:**
```yaml
healthcheck:
  test: ["CMD-SHELL", "echo 'QUIT' | nc -w 5 localhost 110 | grep -q '^+OK' || exit 1"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 10s
```

Verifies the POP3 server responds with a `+OK` greeting, the standard POP3 server ready indicator defined in RFC 1939.

**GHCR images:** Uses `wget --spider http://localhost:8082/healthz`.

### IMAP Server

**Local build:**
```yaml
healthcheck:
  test: ["CMD-SHELL", "echo 'a001 LOGOUT' | nc -w 5 localhost 143 | grep -q '^\\* OK' || exit 1"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 10s
```

Verifies the IMAP server responds with a `* OK` greeting, the standard IMAP server ready response defined in RFC 9051. The `a001 LOGOUT` command cleanly terminates the session.

**GHCR images:** Uses `wget --spider http://localhost:8083/healthz`.

## Health Check Parameters

All application services share these timing parameters:

| Parameter | Value | Description |
|-----------|-------|-------------|
| `interval` | 30s | Time between health check executions |
| `timeout` | 10s | Maximum time a single check can take before being considered failed |
| `retries` | 3 | Number of consecutive failures before marking the container as unhealthy |
| `start_period` | 10s | Grace period after container start during which failures are not counted |

PostgreSQL uses a tighter interval (5s) because it is a critical dependency -- other services cannot start until it is healthy.

### Health State Transitions

A container's health state follows this lifecycle:

```
starting ──(start_period expires)──> healthy ──(retries exceeded)──> unhealthy
                                       ^                                │
                                       └──────(check passes)───────────┘
```

During the `starting` phase, health check failures are ignored. After the `start_period`, the container must pass a health check to become `healthy`. If three consecutive checks fail (30s apart), the container is marked `unhealthy`.

## Resource Limits

The local build compose file (`docker-compose.yml`) defines resource limits for each service using Docker's `deploy.resources` configuration:

### PostgreSQL

```yaml
deploy:
  resources:
    limits:
      cpus: '1.0'
      memory: 1G
    reservations:
      cpus: '0.25'
      memory: 256M
```

PostgreSQL typically has moderate resource needs for an email workload. The 1 GB memory limit is sufficient for default `shared_buffers` and connection pooling. Increase this if you have many concurrent users or large mailboxes.

### API Server

```yaml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 2G
    reservations:
      cpus: '0.25'
      memory: 256M
```

The API handles HTTP requests, WebSocket connections (SignalR), and serves the web frontend. It benefits from higher CPU allocation for concurrent request processing.

### Protocol Servers (SMTP, POP3, IMAP)

```yaml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 2G
    reservations:
      cpus: '0.25'
      memory: 256M
```

Each protocol server gets the same allocation. In practice, SMTP may need more resources if processing many concurrent inbound connections or large attachments. POP3 and IMAP resource usage scales with the number of concurrent client connections.

### Resource Summary

| Service | CPU Limit | Memory Limit | CPU Reserved | Memory Reserved |
|---------|-----------|--------------|--------------|-----------------|
| PostgreSQL | 1.0 | 1 GB | 0.25 | 256 MB |
| API | 2.0 | 2 GB | 0.25 | 256 MB |
| SMTP | 2.0 | 2 GB | 0.25 | 256 MB |
| POP3 | 2.0 | 2 GB | 0.25 | 256 MB |
| IMAP | 2.0 | 2 GB | 0.25 | 256 MB |
| **Total** | **9.0** | **9 GB** | **1.25** | **1.28 GB** |

The reservation values represent the minimum resources guaranteed to each container. The limits are the maximum a container can consume. A host machine should have at least 2 CPU cores and 4 GB RAM for a minimal deployment; 4+ cores and 8+ GB is recommended for production use.

### Adjusting Limits

To modify resource limits, either edit the compose file directly or override them in a separate compose file:

```yaml
# docker-compose.override.yml
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '4.0'
          memory: 4G
```

Then run:

```bash
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

## Restart Policies

All services use the `unless-stopped` restart policy:

```yaml
restart: unless-stopped
```

This means:
- Containers restart automatically if they crash or if Docker restarts
- Containers do **not** restart if they are explicitly stopped with `docker compose stop` or `docker stop`
- On system reboot, containers restart automatically (assuming Docker is configured to start on boot)

This policy is a good balance for production deployments -- it provides automatic recovery from crashes without interfering with intentional maintenance stops.

## Monitoring Health Status

Check the health status of all services:

```bash
# View health status for all containers
docker compose ps

# Watch health status in real time
watch docker compose ps

# Inspect a specific container's health details
docker inspect --format='{{json .State.Health}}' docker-api-1 | jq
```

A healthy deployment shows all services in `Up (healthy)` state:

```
NAME        STATUS              PORTS
postgres    Up 2 minutes (healthy)   5432/tcp
api         Up 2 minutes (healthy)   0.0.0.0:8080->8080/tcp
smtp        Up 2 minutes (healthy)   0.0.0.0:25->25/tcp, 0.0.0.0:465->465/tcp, 0.0.0.0:587->587/tcp
pop3        Up 2 minutes (healthy)   0.0.0.0:110->110/tcp, 0.0.0.0:995->995/tcp
imap        Up 2 minutes (healthy)   0.0.0.0:143->143/tcp, 0.0.0.0:993->993/tcp
```
