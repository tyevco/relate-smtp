# Health Checks

The API project exposes a `/healthz` endpoint that reports the health of its subsystems. This endpoint is **unauthenticated** (`AllowAnonymous`) for use by load balancers, orchestrators, and monitoring systems.

## Endpoint

```
GET /healthz
```

Returns a JSON response with the overall status and details for each registered health check:

```json
{
  "status": "Healthy",
  "totalDuration": 12.34,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 5.21,
      "description": "PostgreSQL connection is healthy",
      "exception": null,
      "tags": ["infrastructure"],
      "data": null
    },
    {
      "name": "signalr",
      "status": "Healthy",
      "duration": 0.03,
      "description": "SignalR EmailHub is available",
      "exception": null,
      "tags": ["api"],
      "data": null
    },
    {
      "name": "delivery-queue",
      "status": "Healthy",
      "duration": 1.10,
      "description": null,
      "exception": null,
      "tags": ["api"],
      "data": null
    }
  ]
}
```

The `status` field will be one of: `Healthy`, `Degraded`, or `Unhealthy`.

## Registered Health Checks

### Database (from Infrastructure)

Registered by `AddInfrastructure()`, this check verifies that the PostgreSQL database is reachable and accepting connections. It is the most critical health check -- if the database is down, the API cannot function.

### SignalR Health Check

**Class:** `Health/SignalRHealthCheck.cs`
**Tag:** `api`

Verifies that the SignalR `EmailHub` is properly registered in the service container and can be resolved. This is a lightweight check that confirms the hub infrastructure is wired up correctly:

```csharp
var hubContext = _services.GetService<IHubContext<EmailHub>>();
if (hubContext == null)
{
    return HealthCheckResult.Unhealthy(
        "SignalR EmailHub is not registered in the service container");
}
return HealthCheckResult.Healthy("SignalR EmailHub is available");
```

If the hub cannot be resolved (e.g., due to a misconfigured dependency injection), the check reports unhealthy. Any unexpected exception during resolution is also caught and reported.

### Delivery Queue Health Check

**Tag:** `api`

Monitors the outbound email delivery queue processor. Reports unhealthy if the background processor has stopped or is failing to process queued emails.

### TLS Certificate Expiry (from Infrastructure)

Registered by `AddInfrastructure()` when a TLS certificate path is configured. Reports degraded status when the certificate is nearing expiration, giving operators advance warning to renew before service disruption.

## Docker Integration

The health check endpoint is used in Docker Compose configurations for container health monitoring:

```yaml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/healthz"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 15s
```

When a container's health check fails, Docker marks it as unhealthy. Docker Compose `depends_on` with `condition: service_healthy` can be used to ensure dependent services (like the SMTP host) wait for the API to be ready.

## Kubernetes Integration

For Kubernetes deployments, the health endpoint can serve as both a readiness and liveness probe:

```yaml
livenessProbe:
  httpGet:
    path: /healthz
    port: 5000
  initialDelaySeconds: 15
  periodSeconds: 30
readinessProbe:
  httpGet:
    path: /healthz
    port: 5000
  initialDelaySeconds: 5
  periodSeconds: 10
```

The readiness probe determines whether the pod should receive traffic. The liveness probe determines whether the pod should be restarted. Using the same endpoint for both works well because all critical subsystems (database, SignalR, delivery queue) are checked.

## OpenTelemetry

Health check results are automatically exported as part of the OpenTelemetry metrics pipeline when configured. The `AddRelateTelemetry` method in Infrastructure sets up tracing and metrics exporters that include ASP.NET Core health check instrumentation. This allows health check durations and statuses to be monitored in observability platforms like Prometheus, Grafana, or Jaeger.
