# Infrastructure Layer

The `Relate.Smtp.Infrastructure` project is the outermost layer of the backend, implementing the data access, background services, health checks, and telemetry that the Core layer defines as abstractions. This is where Entity Framework Core, PostgreSQL, BCrypt, DNS resolution, SMTP delivery, and OpenTelemetry live.

## Responsibilities

- **Data access** -- EF Core `AppDbContext` with PostgreSQL, 9 repository implementations, Fluent API entity configurations, and database migrations
- **Background services** -- Outbound email delivery queue processor, background task queue for non-critical updates
- **Authentication** -- Shared protocol authenticator base class with rate limiting, caching, and BCrypt verification
- **Email delivery** -- SMTP delivery service with MX resolution and relay support
- **Health checks** -- Certificate expiry, connection pool, delivery queue, disk space, and memory monitoring
- **Telemetry** -- OpenTelemetry configuration with protocol-specific activity sources and metrics

## Project Structure

```
Relate.Smtp.Infrastructure/
  DependencyInjection.cs              # Service registration
  Authentication/
    ProtocolAuthenticator.cs          # Shared auth base for POP3/IMAP/SMTP
  Data/
    AppDbContext.cs                    # EF Core DbContext
    Configurations/                   # 15 Fluent API entity configurations
    Migrations/                       # 4 database migrations
  Health/
    CertificateExpiryHealthCheck.cs
    ConnectionPoolHealthCheck.cs
    DeliveryQueueHealthCheck.cs
    DiskSpaceHealthCheck.cs
    MemoryHealthCheck.cs
  Repositories/                       # 9 repository implementations
    EmailRepository.cs
    UserRepository.cs
    SmtpApiKeyRepository.cs
    LabelRepository.cs
    EmailLabelRepository.cs
    EmailFilterRepository.cs
    UserPreferenceRepository.cs
    PushSubscriptionRepository.cs
    OutboundEmailRepository.cs
  Services/
    AuthenticationRateLimiter.cs
    BackgroundTaskQueue.cs
    DeliveryQueueProcessor.cs
    MxResolverService.cs
    SmtpDeliveryService.cs
    OutboundMailOptions.cs
    EmailNotificationService.cs
    IDeliveryNotificationService.cs
  Telemetry/
    TelemetryConfiguration.cs
    ProtocolMetrics.cs
```

## Dependency Injection

`DependencyInjection.cs` provides a single extension method `AddInfrastructure(connectionString)` that registers all infrastructure services:

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
```

### Registered Services

| Service | Lifetime | Description |
|---------|----------|-------------|
| `AppDbContext` | Scoped | EF Core database context |
| `IEmailRepository` | Scoped | Email data access |
| `IUserRepository` | Scoped | User account data access |
| `ISmtpApiKeyRepository` | Scoped | API key management |
| `ILabelRepository` | Scoped | Label CRUD |
| `IEmailLabelRepository` | Scoped | Email-label relationships |
| `IEmailFilterRepository` | Scoped | Filter rule management |
| `IUserPreferenceRepository` | Scoped | User preferences |
| `IPushSubscriptionRepository` | Scoped | Push notification subscriptions |
| `IOutboundEmailRepository` | Scoped | Outbound email management |
| `BackgroundTaskQueue` | Singleton | Non-critical async update queue |
| `IAuthenticationRateLimiter` | Singleton | Brute-force protection |
| `ILookupClient` | Singleton | DNS client (DnsClient.NET) |
| `MxResolverService` | Singleton | MX record resolution |
| `SmtpDeliveryService` | Scoped | Outbound email delivery |
| `DeliveryQueueProcessor` | Hosted | Background queue processor |
| `BackgroundTaskQueueHostedService` | Hosted | Background task consumer |

### Health Checks

The following health checks are registered:

| Name | Tags | Description |
|------|------|-------------|
| `database` | (default) | EF Core database connectivity |
| `disk-space` | `system` | Available disk space monitoring |
| `memory` | `system` | Process memory usage monitoring |
| `connection-pool` | `database` | PostgreSQL connection pool utilization |

Additional health checks (`CertificateExpiryHealthCheck`, `DeliveryQueueHealthCheck`) are registered by the individual protocol hosts and the API.

## Connection String

The database connection is configured via:

```
ConnectionStrings__DefaultConnection=Host=localhost;Database=relatemail;Username=postgres;Password=...
```

Or in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=relatemail;..."
  }
}
```

## Further Reading

- [Data Access](./data-access.md) -- DbContext, entity configurations, migrations
- [Repositories](./repositories.md) -- Repository implementation details
- [Services](./services.md) -- Background services, delivery, rate limiting
- [Health Checks](./health-checks.md) -- Monitoring and alerting
- [Telemetry](./telemetry.md) -- OpenTelemetry integration
