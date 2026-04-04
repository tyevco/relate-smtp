# SMTP Health Checks

The SMTP host exposes a `/healthz` endpoint on a dedicated health check port (default 8081) that reports the status of the SMTP server and its dependencies. This is separate from the SMTP protocol ports to avoid confusion between health probes and mail traffic.

## Health Endpoint

```
GET http://localhost:8081/healthz
```

The health check port is configured via:

```json
{
  "HealthCheck": {
    "Url": "http://localhost:8081"
  }
}
```

Response format matches the API's health endpoint:

```json
{
  "status": "Healthy",
  "totalDuration": 45.12,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 8.5,
      "description": "PostgreSQL connection is healthy",
      "tags": ["infrastructure"]
    },
    {
      "name": "smtp",
      "status": "Healthy",
      "duration": 32.1,
      "description": "SMTP server accepting connections on port 587",
      "tags": ["protocol"]
    },
    {
      "name": "dns-mx",
      "status": "Healthy",
      "duration": 15.3,
      "description": "DNS MX resolution working (gmail.com -> gmail-smtp-in.l.google.com, ...)",
      "tags": ["smtp"],
      "data": {
        "testDomain": "gmail.com",
        "mxRecordCount": 5
      }
    },
    {
      "name": "certificate",
      "status": "Healthy",
      "duration": 0.5,
      "description": null,
      "tags": ["tls"]
    }
  ]
}
```

## Registered Health Checks

### SmtpHealthCheck

**Class:** `SmtpHealthCheck.cs`
**Tag:** `protocol`

Verifies the SMTP server is accepting connections by performing a TCP health probe against the configured SMTP port (default 587). This tests the actual SMTP protocol:

1. Opens a TCP connection to `localhost:{SmtpPort}`
2. Reads the server greeting
3. Verifies the greeting starts with `220` (the SMTP ready response code)
4. Sends `QUIT` to cleanly close the connection
5. Returns healthy if all steps succeed within a 5-second timeout

```csharp
using var client = new TcpClient();
await client.ConnectAsync("localhost", _options.Port, cts.Token);

var greeting = await reader.ReadLineAsync(cts.Token);
if (greeting == null || !greeting.StartsWith("220", StringComparison.Ordinal))
{
    return HealthCheckResult.Unhealthy(
        $"SMTP server on port {_options.Port} returned unexpected greeting: {greeting}");
}
```

**Failure scenarios:**
- **Connection refused** -- the SMTP server process has crashed or the port is not listening
- **Timeout** -- the server is overloaded or hanging
- **Unexpected greeting** -- the port is in use by another service, or the SMTP server is in an error state

This check probes the submission port (587), not the MX port (25), because the submission port is always active when the SMTP host is running.

### DnsResolutionHealthCheck

**Class:** `DnsResolutionHealthCheck.cs`
**Tag:** `smtp`

Verifies that DNS MX record resolution is functioning correctly. This is essential for outbound mail delivery -- if MX resolution fails, the server cannot determine where to deliver emails.

The check queries MX records for `gmail.com` as a well-known test domain:

```csharp
var result = await _lookupClient.QueryAsync(
    "gmail.com", QueryType.MX, cancellationToken: cancellationToken);

var mxRecords = result.Answers
    .OfType<MxRecord>()
    .ToList();
```

**Result states:**

| Condition | Status | Description |
|-----------|--------|-------------|
| MX records returned | Healthy | Shows top 3 MX hosts by preference |
| No MX records | Degraded | DNS works but returned no MX records (unusual) |
| Query failed | Unhealthy | DNS resolution is broken |

The healthy response includes metadata about the test:

```json
{
  "status": "Healthy",
  "description": "DNS MX resolution working (gmail.com -> gmail-smtp-in.l.google.com, alt1.gmail-smtp-in.l.google.com, alt2.gmail-smtp-in.l.google.com)",
  "data": {
    "testDomain": "gmail.com",
    "mxRecordCount": 5
  }
}
```

This check uses the `DnsClient` library (`ILookupClient`) which is registered by the Infrastructure layer.

### Database Health Check (from Infrastructure)

**Tag:** `infrastructure`

Registered by `AddInfrastructure()`, this check verifies PostgreSQL connectivity. It is the same check used by the API project. If the database is unreachable, the SMTP server cannot store incoming emails or authenticate users.

### Certificate Expiry Health Check (from Infrastructure)

**Tag:** `tls`

Registered when `Smtp:CertificatePath` is configured. Monitors the TLS certificate's expiration date and reports degraded status as the certificate approaches expiry. This gives operators advance warning to renew the certificate before it expires and breaks TLS connections.

## Docker Integration

The health check endpoint is designed for use with Docker's HEALTHCHECK directive:

```yaml
services:
  smtp:
    image: relate-mail-smtp
    ports:
      - "587:587"
      - "465:465"
      - "25:25"
    healthcheck:
      test: ["CMD", "curl", "-sf", "http://localhost:8081/healthz"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s
```

The health check port (8081) is typically not exposed outside the Docker network -- it is only used for internal health monitoring.

### Docker Compose Dependencies

Using health checks with Docker Compose ensures services start in the correct order:

```yaml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-sf", "http://localhost:5000/healthz"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 15s

  smtp:
    depends_on:
      api:
        condition: service_healthy
    environment:
      - Api__BaseUrl=http://api:5000
```

This ensures the SMTP host waits for the API to be healthy before starting, which is important because the SMTP host needs to send notifications to the API after storing emails.

## Kubernetes Integration

For Kubernetes, use the health endpoint as both a liveness and readiness probe:

```yaml
containers:
  - name: smtp
    ports:
      - containerPort: 587
      - containerPort: 465
      - containerPort: 25
      - containerPort: 8081  # health only
    livenessProbe:
      httpGet:
        path: /healthz
        port: 8081
      initialDelaySeconds: 10
      periodSeconds: 30
    readinessProbe:
      httpGet:
        path: /healthz
        port: 8081
      initialDelaySeconds: 5
      periodSeconds: 10
```

The readiness probe controls whether the pod receives SMTP traffic. If the database or DNS resolution is unhealthy, the pod is removed from the service, preventing mail from being accepted when it cannot be stored or delivered.

## Monitoring

Health check results are included in the OpenTelemetry metrics pipeline. You can set up alerts based on:

- **smtp check unhealthy** -- the SMTP server has stopped accepting connections
- **dns-mx check unhealthy** -- DNS resolution is broken, outbound delivery will fail
- **certificate check degraded** -- TLS certificate is expiring soon
- **database check unhealthy** -- PostgreSQL is unreachable

::: info Screenshot
**[Screenshot placeholder: Health check dashboard]**

_TODO: Add screenshot of a Grafana dashboard showing SMTP health check metrics over time_
:::
