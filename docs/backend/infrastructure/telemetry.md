# Telemetry

Relate Mail uses [OpenTelemetry](https://opentelemetry.io/) for distributed tracing and metrics collection across all backend services. The telemetry system provides visibility into protocol command processing, authentication patterns, message delivery, and database operations.

## TelemetryConfiguration

The `TelemetryConfiguration` class provides a centralized setup method and shared activity sources for all protocol servers.

### Activity Sources

Activity sources create distributed tracing spans for protocol operations:

| Source | Name | Used By |
|--------|------|---------|
| `SmtpActivitySource` | `Relate.Smtp` | SMTP server |
| `Pop3ActivitySource` | `Relate.Pop3` | POP3 server |
| `ImapActivitySource` | `Relate.Imap` | IMAP server |
| `ApiActivitySource` | `Relate.Api` | REST API |

Each protocol handler creates per-command activities. For example, the POP3 handler creates a `pop3.command.retr` activity for every RETR command, with tags for the session ID and command name.

### Registration

`AddRelateTelemetry` is an extension method on `IServiceCollection` that configures OpenTelemetry tracing and metrics:

```csharp
services.AddRelateTelemetry(
    configuration,
    serviceName: "relate-pop3",
    configureTracing: tracing => tracing.AddAspNetCoreInstrumentation(),
    configureMetrics: metrics => metrics.AddAspNetCoreInstrumentation()
);
```

The method:

1. Configures the OpenTelemetry **resource** with the service name and version.
2. Registers all four activity sources for **tracing**.
3. Adds Entity Framework Core instrumentation (including SQL statement capture).
4. Allows the caller to add additional instrumentation (e.g., ASP.NET Core, HTTP client).
5. Registers all three protocol meters for **metrics**.
6. Configures the **exporter** -- OTLP if an endpoint is configured, console otherwise.

### OTLP Endpoint

The exporter endpoint is configured via:

```
Otel__Endpoint=http://otel-collector:4317
```

When set, both traces and metrics are exported to the OTLP endpoint. When not set, traces are exported to the console (useful for development).

## ProtocolMetrics

`ProtocolMetrics` defines all protocol-level metrics using the .NET `System.Diagnostics.Metrics` API.

### SMTP Metrics

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `smtp.messages.received` | Counter | messages | Total messages received via SMTP |
| `smtp.bytes.received` | Counter | bytes | Total bytes received via SMTP |
| `smtp.auth.attempts` | Counter | attempts | SMTP authentication attempts |
| `smtp.auth.failures` | Counter | failures | SMTP authentication failures |
| `smtp.message.processing.duration` | Histogram | ms | Time to process an SMTP message |
| `smtp.connections.active` | UpDownCounter | connections | Currently active SMTP connections |

### POP3 Metrics

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `pop3.messages.retrieved` | Counter | messages | Total messages retrieved via POP3 |
| `pop3.bytes.sent` | Counter | bytes | Total bytes sent via POP3 |
| `pop3.auth.attempts` | Counter | attempts | POP3 authentication attempts |
| `pop3.auth.failures` | Counter | failures | POP3 authentication failures |
| `pop3.commands` | Counter | commands | POP3 commands processed (tagged by `command`) |
| `pop3.sessions.active` | UpDownCounter | sessions | Currently active POP3 sessions |

### IMAP Metrics

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `imap.messages.retrieved` | Counter | messages | Total messages retrieved via IMAP |
| `imap.bytes.sent` | Counter | bytes | Total bytes sent via IMAP |
| `imap.auth.attempts` | Counter | attempts | IMAP authentication attempts |
| `imap.auth.failures` | Counter | failures | IMAP authentication failures |
| `imap.commands` | Counter | commands | IMAP commands processed (tagged by `command`) |
| `imap.sessions.active` | UpDownCounter | sessions | Currently active IMAP sessions |

### Meter Names

Each protocol has its own meter:

| Meter | Name |
|-------|------|
| SMTP | `Relate.Smtp` |
| POP3 | `Relate.Pop3` |
| IMAP | `Relate.Imap` |

## Per-Command Tracking

Every protocol command is wrapped in an OpenTelemetry activity that captures:

| Tag | Description |
|-----|-------------|
| `{protocol}.session_id` | Session connection ID |
| `{protocol}.command` | Command name (e.g., `RETR`, `FETCH`) |
| `{protocol}.state` | Current session state (IMAP only) |

On error, additional tags are added:

| Tag | Description |
|-----|-------------|
| `exception.type` | Exception type name |
| `exception.message` | Exception message |

The activity status is set to `Error` on failure.

## Authentication Tracing

The `ProtocolAuthenticator` creates an activity for each authentication attempt:

| Tag | Description |
|-----|-------------|
| `{protocol}.auth.user` | Username (email address) |
| `{protocol}.auth.cache_hit` | Whether the result was served from cache |
| `{protocol}.auth.success` | Whether authentication succeeded |
| `{protocol}.auth.rate_limited` | Whether the request was rate-limited |
| `{protocol}.auth.failure_reason` | Reason for failure (`user_not_found`, `invalid_key`, `missing_scope`) |
| `{protocol}.auth.key_name` | Name of the matched API key (on success) |

## Consuming Telemetry

### Development

Without an OTLP endpoint configured, traces are written to the console. This is useful for debugging protocol interactions.

### Production with OTLP

Configure an OpenTelemetry Collector endpoint:

```bash
Otel__Endpoint=http://otel-collector:4317
```

The collector can then forward data to backends like:

- **Jaeger** or **Tempo** for distributed tracing
- **Prometheus** for metrics
- **Grafana** for dashboards

### Example: Grafana Dashboard

Useful queries for a Grafana dashboard:

- **Authentication failure rate**: `rate(smtp_auth_failures_total[5m]) / rate(smtp_auth_attempts_total[5m])`
- **Active sessions**: `pop3_sessions_active` / `imap_sessions_active`
- **Command throughput**: `rate(pop3_commands_total[5m])` grouped by `command` tag
- **Message retrieval rate**: `rate(imap_messages_retrieved_total[5m])`

### Example: Docker Compose with Jaeger

```yaml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # Jaeger UI
      - "4317:4317"    # OTLP gRPC

  api:
    environment:
      - Otel__Endpoint=http://jaeger:4317
```
