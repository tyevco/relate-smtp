# Health Checks

The Infrastructure layer provides five custom health checks that monitor system resources, database health, and delivery pipeline status. These are exposed via the `/healthz` endpoint and can be consumed by orchestrators (Docker, Kubernetes), load balancers, and monitoring systems.

## CertificateExpiryHealthCheck

Monitors TLS certificates used by the protocol servers (SMTP, POP3, IMAP) and warns when they are approaching expiry.

### Thresholds

| Status | Condition |
|--------|-----------|
| Healthy | Certificate valid for 30+ days |
| Degraded | Certificate expires within 30 days |
| Unhealthy | Certificate expires within 7 days or is already expired |
| Unhealthy | Certificate file not found or cannot be loaded |

### Response Data

When the certificate is found, the health check returns:

| Field | Description |
|-------|-------------|
| `subject` | Certificate subject (CN) |
| `notBefore` | Certificate validity start date |
| `notAfter` | Certificate expiration date |
| `daysUntilExpiry` | Days remaining until expiration |
| `thumbprint` | Certificate thumbprint for identification |

### Configuration

This health check is instantiated by the protocol hosts with their configured certificate path and password. If no certificate path is configured (TLS disabled), it reports healthy with the message "No TLS certificate configured."

## ConnectionPoolHealthCheck

Monitors PostgreSQL connection pool utilization by querying the database directly.

### How It Works

Executes two SQL queries:
1. `SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database()` -- Active connections
2. `SELECT setting FROM pg_settings WHERE name = 'max_connections'` -- Maximum allowed connections

Then computes the usage percentage.

### Thresholds

| Status | Condition |
|--------|-----------|
| Healthy | Under 80% utilization |
| Degraded | 80-95% utilization |
| Unhealthy | Over 95% utilization |

### Response Data

| Field | Description |
|-------|-------------|
| `activeConnections` | Current active connection count |
| `maxConnections` | PostgreSQL `max_connections` setting |
| `usedPercent` | Usage as a percentage |

### Registration

Registered in `DependencyInjection.cs` with the `database` tag:

```csharp
services.AddHealthChecks()
    .AddCheck<ConnectionPoolHealthCheck>("connection-pool", tags: ["database"]);
```

## DeliveryQueueHealthCheck

Monitors the outbound email delivery pipeline for stalled deliveries, high retry counts, and queue backlogs.

### Checks Performed

1. **Stalled deliveries** -- Emails stuck in `Sending` status for more than 10 minutes. This indicates the delivery processor may have crashed or the SMTP connection is hung.
2. **High retry count** -- Emails with 3 or more retry attempts. Indicates persistent delivery failures to certain domains.
3. **Queue backlog** -- More than 100 emails queued for delivery. May indicate the delivery processor cannot keep up or outbound delivery is disabled.

### Thresholds

| Status | Condition |
|--------|-----------|
| Healthy | No stalled deliveries, no high-retry emails, queue under 100 |
| Degraded | High-retry emails exist OR queue exceeds 100 |
| Unhealthy | Any emails stalled in Sending status |

### Response Data

| Field | Description |
|-------|-------------|
| `stalledCount` | Emails stuck in Sending for 10+ minutes |
| `highRetryCount` | Emails with 3+ retry attempts |
| `queuedCount` | Total emails in Queued status |

## DiskSpaceHealthCheck

Monitors available disk space on the root filesystem.

### Thresholds

| Status | Condition |
|--------|-----------|
| Healthy | More than 1 GB available |
| Degraded | Between 100 MB and 1 GB available |
| Unhealthy | Less than 100 MB available |

### Response Data

| Field | Description |
|-------|-------------|
| `availableGB` | Available free space in gigabytes |
| `totalGB` | Total disk size in gigabytes |
| `usedPercent` | Usage as a percentage |

### Implementation Note

The health check monitors the `/` mount point using `DriveInfo("/")`. In containerized deployments, this reflects the container's root filesystem. If you use volume mounts for data storage, consider adding checks for those mount points as well.

## MemoryHealthCheck

Monitors the process's memory usage using .NET's GC and process APIs.

### Thresholds

| Status | Condition |
|--------|-----------|
| Healthy | Working set under 1.5 GB |
| Degraded | Working set between 1.5 GB and 1.9 GB |
| Unhealthy | Working set over 1.9 GB |

### Response Data

| Field | Description |
|-------|-------------|
| `workingSetMB` | Process working set (physical memory) in MB |
| `gcHeapMB` | .NET GC managed heap size in MB |
| `totalAvailableMemoryMB` | Total available system memory in MB |
| `gen0Collections` | Generation 0 garbage collections |
| `gen1Collections` | Generation 1 garbage collections |
| `gen2Collections` | Generation 2 garbage collections |

GC collection counts are included to help diagnose memory pressure. A high gen2 collection count relative to gen0/gen1 may indicate the application is frequently allocating large objects or holding references too long.

## Consuming Health Checks

### Endpoint

All health checks are mapped to `/healthz` by the API host:

```
GET /healthz
```

Returns HTTP 200 (Healthy), 200 (Degraded), or 503 (Unhealthy) with a JSON body detailing each check.

### Filtering by Tag

You can query specific health check groups:

- `/healthz?tags=system` -- Disk space and memory checks only
- `/healthz?tags=database` -- Connection pool check only

### Docker and Kubernetes

In `docker-compose.yml`, configure health checks to poll the endpoint:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5000/healthz"]
  interval: 30s
  timeout: 10s
  retries: 3
```

For Kubernetes, use the endpoint as a liveness or readiness probe:

```yaml
livenessProbe:
  httpGet:
    path: /healthz
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 30
```
