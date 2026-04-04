# Backend Overview

Relate Mail's backend is built on **.NET 10.0** using Clean Architecture principles. It consists of six projects that together provide a REST API, SMTP server, POP3 server, and IMAP server -- all sharing a single PostgreSQL database.

## Project Structure

```
api/src/
  Relate.Smtp.Core/            # Domain entities + repository interfaces
  Relate.Smtp.Infrastructure/  # EF Core DbContext, repositories, migrations
  Relate.Smtp.Api/             # REST API, controllers, auth, SignalR hub
  Relate.Smtp.SmtpHost/        # SMTP server (ports 25, 465, 587)
  Relate.Smtp.Pop3Host/        # POP3 server (ports 110, 995)
  Relate.Smtp.ImapHost/        # IMAP4rev2 server (ports 143, 993)
```

## Clean Architecture

The dependency graph is strictly layered:

```
   Core (no dependencies)
     ^
     |
 Infrastructure (depends on Core)
     ^
     |
 Api / SmtpHost / Pop3Host / ImapHost (depend on Core + Infrastructure)
```

### Core

The innermost layer. Contains domain entities (`Email`, `User`, `SmtpApiKey`, `OutboundEmail`, `Label`, `EmailFilter`, etc.) and repository interfaces (`IEmailRepository`, `IUserRepository`, `ISmtpApiKeyRepository`, etc.). Core has **zero** external dependencies -- it defines the contracts that other layers implement.

### Infrastructure

Implements Core's repository interfaces using Entity Framework Core and PostgreSQL. Provides:

- `AppDbContext` with entity configurations and migrations
- Repository implementations for all domain entities
- Shared services used across protocol hosts (authentication rate limiting, background task queuing, delivery queue processing)
- `DependencyInjection.cs` that registers all data services via `builder.Services.AddInfrastructure(connectionString)`
- Health checks for the database and TLS certificate expiry
- OpenTelemetry configuration shared by all hosts

### Protocol Hosts

The four host projects (Api, SmtpHost, Pop3Host, ImapHost) are independently deployable applications that share common infrastructure:

| Feature | Api | SmtpHost | Pop3Host | ImapHost |
|---------|-----|----------|----------|----------|
| PostgreSQL via Infrastructure | Yes | Yes | Yes | Yes |
| API key auth (BCrypt + 30s cache) | Yes | Yes | Yes | Yes |
| Health check endpoint (`/healthz`) | Yes | Yes | Yes | Yes |
| OpenTelemetry instrumentation | Yes | Yes | Yes | Yes |
| Background task queue | Yes | Yes | Yes | Yes |

Each protocol host runs as its own process, allowing you to scale them independently. For example, you might run multiple SMTP host instances behind a load balancer while keeping a single API instance.

## Building

From the `api/` directory:

```bash
# Build all six projects
dotnet build

# Build a specific project
dotnet build src/Relate.Smtp.Api
dotnet build src/Relate.Smtp.SmtpHost
```

The solution file at `api/Relate.Smtp.sln` includes all projects and their test counterparts.

## Running

Each host runs in its own process. During development, you typically run them in separate terminals:

```bash
# Terminal 1: REST API (http://localhost:5000)
dotnet run --project src/Relate.Smtp.Api

# Terminal 2: SMTP server (ports 587, 465, optionally 25)
dotnet run --project src/Relate.Smtp.SmtpHost

# Terminal 3: POP3 server (ports 110, 995)
dotnet run --project src/Relate.Smtp.Pop3Host

# Terminal 4: IMAP server (ports 143, 993)
dotnet run --project src/Relate.Smtp.ImapHost
```

In development mode, the API project automatically runs EF Core migrations on startup, so the database schema stays current without manual intervention.

## Testing

Tests are organized by category using the `[Trait("Category", "...")]` attribute:

```bash
# Unit tests -- fast, no external dependencies
dotnet test --filter "Category=Unit"

# Integration tests -- require PostgreSQL (via Testcontainers)
dotnet test --filter "Category=Integration"

# End-to-end tests -- full stack with all protocol hosts
dotnet test --filter "Category=E2E"

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestName"

# Run all tests
dotnet test
```

### Test Infrastructure

- **Unit tests** mock repository interfaces and test business logic in isolation.
- **Integration tests** use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a real PostgreSQL container, verifying that EF Core queries and migrations work correctly.
- **E2E tests** use `FullStackFixture` which starts the API, SMTP, POP3, and IMAP hosts with Testcontainers for PostgreSQL, then exercises complete workflows (e.g., send an email via SMTP, retrieve it via POP3, verify it appears in the API).

## Database Migrations

Migrations are managed with EF Core CLI tools:

```bash
cd api

# Create a new migration
dotnet ef migrations add MigrationName \
  --project src/Relate.Smtp.Infrastructure \
  --startup-project src/Relate.Smtp.Api

# Apply pending migrations
dotnet ef database update \
  --project src/Relate.Smtp.Infrastructure \
  --startup-project src/Relate.Smtp.Api
```

In development mode (`ASPNETCORE_ENVIRONMENT=Development`), the API project auto-migrates on startup.

## Configuration

All projects use the standard .NET configuration system (`appsettings.json` + environment variable overrides). Key settings:

| Setting | Description | Default |
|---------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | Dev fallback to localhost |
| `Oidc:Authority` | OIDC provider URL (optional) | Empty (dev mode) |
| `Oidc:Audience` | OIDC audience | Empty |
| `Smtp:Enabled` | Toggle SMTP protocol | `true` |
| `Pop3:Enabled` | Toggle POP3 protocol | `true` |
| `Imap:Enabled` | Toggle IMAP protocol | `true` |

Environment variables use double-underscore notation: `ConnectionStrings__DefaultConnection`, `Oidc__Authority`, etc.
