# Monorepo Structure

The Relate Mail repository is organized as a monorepo containing both the .NET backend and all frontend clients. The frontend packages are coordinated through npm workspaces, while the backend uses a .NET solution file. This page maps out the directory structure, explains the dependency relationships, and documents the root-level scripts.

## Top-Level Layout

```
relate-mail/
├── api/                    # .NET 10.0 backend (solution + projects)
│   ├── src/
│   │   ├── Relate.Smtp.Core/           # Domain entities + interfaces
│   │   ├── Relate.Smtp.Infrastructure/  # EF Core, repositories, migrations
│   │   ├── Relate.Smtp.Api/            # REST API + SignalR hub
│   │   ├── Relate.Smtp.SmtpHost/       # SMTP server
│   │   ├── Relate.Smtp.Pop3Host/       # POP3 server
│   │   └── Relate.Smtp.ImapHost/       # IMAP server
│   └── tests/
│       ├── Relate.Smtp.UnitTests/
│       ├── Relate.Smtp.IntegrationTests/
│       ├── Relate.Smtp.E2ETests/
│       └── Relate.Smtp.TestCommon/
├── web/                    # React + Vite web app
├── mobile/                 # React Native + Expo mobile app
├── desktop/                # Tauri 2 desktop app
├── packages/
│   └── shared/             # @relate/shared npm package
├── docker/                 # Docker Compose files + .env
├── docs/                   # VitePress documentation (this site)
├── .github/workflows/      # CI/CD workflows
└── package.json            # Root npm workspace config
```

## npm Workspaces

The root `package.json` declares four workspaces:

```json
{
  "workspaces": [
    "packages/*",
    "web",
    "desktop",
    "docs"
  ]
}
```

Running `npm install` at the root installs dependencies for all workspaces and hoists shared packages to the root `node_modules/`. This means `web`, `desktop`, and `docs` all share a single copy of common dependencies like React and TypeScript.

### Why Mobile Is Not a Workspace

The `mobile/` directory is intentionally excluded from npm workspaces. Expo has specific requirements around dependency resolution and native module linking that conflict with npm workspace hoisting. The mobile app manages its own `node_modules/` independently with its own `npm install`.

## The Shared Package: `@relate/shared`

The `packages/shared/` directory contains the `@relate/shared` npm package, which is the glue between the frontend clients. It exports:

| Export Path | Contents |
|---|---|
| `@relate/shared` | Main index (re-exports everything) |
| `@relate/shared/api/types` | TypeScript types matching the REST API contracts |
| `@relate/shared/components/ui` | Reusable UI primitives (buttons, inputs, dialogs, etc.) |
| `@relate/shared/components/mail` | Email-specific components (message list, compose form, etc.) |
| `@relate/shared/lib/utils` | Utility functions |
| `@relate/shared/styles/theme.css` | Shared CSS theme variables |

This package **must be built before** the web, desktop, or docs packages can compile:

```bash
npm run build:shared
```

## .NET Backend Structure

The backend is organized as a standard .NET solution following Clean Architecture:

### Source Projects

```
api/src/
├── Relate.Smtp.Core/            # Zero dependencies
│   ├── Entities/                 # Email, OutboundEmail, Filter, Label, etc.
│   ├── Interfaces/               # IEmailRepository, IOutboundEmailRepository, etc.
│   └── Models/                   # DTOs and value objects
├── Relate.Smtp.Infrastructure/   # Depends on: Core
│   ├── Data/                     # EF Core DbContext
│   ├── Repositories/             # Repository implementations
│   ├── Migrations/               # EF Core migrations
│   ├── Services/                 # Delivery, health checks, telemetry
│   └── DependencyInjection.cs    # Service registration
├── Relate.Smtp.Api/              # Depends on: Core, Infrastructure
│   ├── Controllers/              # REST endpoints
│   ├── Hubs/                     # SignalR WebSocket hub
│   └── Auth/                     # ApiKeyAuthenticationHandler
├── Relate.Smtp.SmtpHost/         # Depends on: Core, Infrastructure
├── Relate.Smtp.Pop3Host/         # Depends on: Core, Infrastructure
└── Relate.Smtp.ImapHost/         # Depends on: Core, Infrastructure
```

### Test Projects

```
api/tests/
├── Relate.Smtp.UnitTests/          # Pure logic tests, no I/O
├── Relate.Smtp.IntegrationTests/   # Database tests via Testcontainers
├── Relate.Smtp.E2ETests/           # Full-stack protocol tests via Testcontainers
└── Relate.Smtp.TestCommon/         # Shared test utilities and fixtures
```

Tests use `[Trait("Category", "Unit")]`, `[Trait("Category", "Integration")]`, or `[Trait("Category", "E2E")]` attributes so they can be run selectively with `dotnet test --filter`.

## Dependency Graph

### Frontend Dependencies

```
@relate/shared
  ├──> web          (npm workspace)
  ├──> desktop      (npm workspace)
  └──> mobile       (standalone, imports from shared)
```

### Backend Dependencies

```
Relate.Smtp.Core  (no dependencies)
  └──> Relate.Smtp.Infrastructure  (EF Core, Npgsql)
        ├──> Relate.Smtp.Api           (ASP.NET Core, SignalR)
        ├──> Relate.Smtp.SmtpHost      (SmtpServer library)
        ├──> Relate.Smtp.Pop3Host      (custom TCP server)
        └──> Relate.Smtp.ImapHost      (custom TCP server)
```

The dependency arrows point from dependent to dependency. Core has no outward arrows, meaning it can be used anywhere without pulling in framework dependencies. The host projects at the top of the graph each produce a runnable executable.

## Root Scripts

The root `package.json` provides convenience scripts that delegate to the appropriate workspace:

| Script | Description |
|---|---|
| `npm run build:shared` | Build `@relate/shared` (must run first) |
| `npm run dev:web` | Start the Vite web dev server |
| `npm run dev:desktop` | Start the Tauri desktop dev build |
| `npm run build:web` | Production web build |
| `npm run build:desktop` | Production desktop build |
| `npm run lint` | Run ESLint on the web app |
| `npm run test` | Run Vitest on the web app |
| `npm run docs:dev` | Start the VitePress dev server |
| `npm run docs:build` | Build the documentation site |
| `npm run docs:preview` | Preview the built documentation |

Backend commands are run directly from the `api/` directory using `dotnet` CLI tools.

## Database Migrations

EF Core migrations are managed in the Infrastructure project:

```bash
cd api

# Create a new migration
dotnet ef migrations add MigrationName \
  --project src/Relate.Smtp.Infrastructure \
  --startup-project src/Relate.Smtp.Api

# Apply migrations
dotnet ef database update \
  --project src/Relate.Smtp.Infrastructure \
  --startup-project src/Relate.Smtp.Api
```

In development, the API auto-applies migrations on startup, so you rarely need to run `database update` manually.
