# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Relate Mail is a full-stack email platform with SMTP, POP3, and IMAP servers, a REST API, and clients for web, mobile, and desktop. All services share a PostgreSQL database.

## Monorepo Structure

npm workspaces at root coordinate the frontend packages:

- **`api/`** - .NET 10.0 backend (6 projects in `src/`, tests in `tests/`)
- **`web/`** - React + TypeScript + Vite web frontend
- **`mobile/`** - React Native (Expo 54) mobile app
- **`desktop/`** - Tauri 2 desktop app (Rust + TypeScript)
- **`packages/shared/`** - Shared npm package (`@relate/shared`) with API types, UI components, utilities
- **`docker/`** - Docker Compose files for local and GHCR deployment

## Development Commands

### Root (monorepo)

```bash
npm install                    # Install all workspace dependencies
npm run build:shared           # Build @relate/shared (prerequisite for other builds)
npm run dev:web                # Run web dev server
npm run dev:desktop            # Run desktop with Tauri
npm run build:web              # Production web build
npm run build:desktop          # Production desktop build
```

### Backend (.NET)

```bash
cd api
dotnet build                   # Build all projects
dotnet test --filter "Category=Unit"         # Fast unit tests
dotnet test --filter "Category=Integration"  # DB tests (Testcontainers)
dotnet test --filter "Category=E2E"          # Full stack protocol tests

# Run individual servers (each in its own terminal)
dotnet run --project src/Relate.Smtp.Api         # API on http://localhost:5000
dotnet run --project src/Relate.Smtp.SmtpHost    # SMTP on ports 587, 465
dotnet run --project src/Relate.Smtp.Pop3Host    # POP3 on ports 110, 995
dotnet run --project src/Relate.Smtp.ImapHost    # IMAP on ports 143, 993

# Database migrations (from api/ directory)
dotnet ef migrations add MigrationName --project src/Relate.Smtp.Infrastructure --startup-project src/Relate.Smtp.Api
dotnet ef database update --project src/Relate.Smtp.Infrastructure --startup-project src/Relate.Smtp.Api
```

### Web Frontend

```bash
cd web
npm run dev              # Vite dev server (proxies /api to localhost:5000)
npm run build            # Production build (tsc + vite)
npm run lint             # ESLint
npm run test:run         # Vitest unit tests (single run)
npm run test             # Vitest watch mode
npm run test:coverage    # Coverage report (thresholds: 50%/45% branches)
npm run test:e2e         # Playwright E2E (requires running dev server)
```

### Mobile

```bash
cd mobile
npm start                # Expo dev server
npm test                 # Jest unit tests
npm run test:coverage    # Coverage (thresholds: 50%/40% branches)
npm run test:e2e:ios     # Detox E2E on iOS simulator
npm run test:e2e:android # Detox E2E on Android emulator
```

### Docker

```bash
cd docker
docker compose up --build                                        # Local build
docker compose -f docker-compose.yml -f docker-compose.dev.yml up  # Dev mode (exposes ports)
docker compose -f docker-compose.ghcr.yml up                     # Pre-built GHCR images
```

## Backend Architecture (.NET 10.0, Clean Architecture)

```
api/src/
  Relate.Smtp.Core/            # Domain entities + repository interfaces (no dependencies)
  Relate.Smtp.Infrastructure/  # EF Core DbContext, repository implementations, migrations
  Relate.Smtp.Api/             # REST API, controllers, JWT/OIDC + API key auth, SignalR hub
  Relate.Smtp.SmtpHost/        # SMTP server (SmtpServer library 11.1.0)
  Relate.Smtp.Pop3Host/        # Custom POP3 server (RFC 1939)
  Relate.Smtp.ImapHost/        # Custom IMAP4rev2 server (RFC 9051)
```

**Key patterns:**
- Repository interfaces in Core, implementations in Infrastructure
- `Infrastructure/DependencyInjection.cs` registers all data services
- All protocol hosts (SMTP/POP3/IMAP) share the same API key authentication with BCrypt hashing and 30s in-memory cache
- API key scopes: `smtp`, `pop3`, `imap`, `api:read`, `api:write`, `app`
- API supports dual auth: OIDC/JWT (first-party) + API key (third-party/mobile/desktop)
- `ApiKeyAuthenticationHandler` handles Bearer/ApiKey token validation
- SignalR hub at `/hubs/email` for real-time notifications; SMTP/POP3/IMAP hosts notify via HTTP
- Auto-migration on startup in development mode
- Test categories use `[Trait("Category", "Unit|Integration|E2E")]`; E2E uses `FullStackFixture` with Testcontainers

## Frontend Architecture (React + TypeScript)

**Web (`web/src/`):**
- TanStack Router with file-based routing in `src/routes/` — `routeTree.gen.ts` is auto-generated, do not edit
- TanStack Query for server state, Jotai for client state
- API client in `src/api/client.ts` — fetch wrapper that extracts OIDC tokens from sessionStorage
- Tailwind CSS 4.1 with CVA for component variants, Lucide React icons, Radix UI primitives
- Path alias: `@/` maps to `src/`
- Vite dev server proxies `/api` to `http://localhost:5000`
- MSW (Mock Service Worker) used for API mocking in tests

**Mobile (`mobile/app/`):**
- Expo Router with file-based routing, group routes: `(auth)`, `(main)/(tabs)`, `(main)/emails`
- Zustand for state, TanStack Query for server state
- Expo Secure Store for API key persistence, Expo Auth Session for OIDC

**Desktop (`desktop/`):**
- Tauri 2 with React frontend, Rust backend in `src-tauri/`
- Shell, notification, and window state plugins

**Shared (`packages/shared/`):**
- Exports: `@relate/shared` (index), `@relate/shared/api/types`, `@relate/shared/components/ui`, `@relate/shared/components/mail`, `@relate/shared/lib/utils`, `@relate/shared/styles/theme.css`

## Authentication

OIDC is optional. If `Oidc__Authority` (backend) or `VITE_OIDC_AUTHORITY` (frontend) is not set, the system runs in development mode without authentication.

## Configuration

Backend uses `appsettings.json` + environment variable overrides. Key settings:
- `ConnectionStrings__DefaultConnection` — PostgreSQL connection string
- `Oidc__Authority` / `Oidc__Audience` — OIDC provider (optional)
- `Smtp__Enabled`, `Pop3__Enabled`, `Imap__Enabled` — toggle protocols
- `{Protocol}__ServerName`, `{Protocol}__Port`, `{Protocol}__SecurePort` — server binding

Frontend uses `VITE_API_URL` (defaults to `/api`), `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID`.

## CI/CD

GitHub Actions workflows in `.github/workflows/`:
- **ci.yml** — Backend build/test (unit, integration, E2E), web build/lint/test, mobile lint/test, desktop lint, Docker build validation
- **docker-publish.yml** — Multi-platform (amd64/arm64) image publishing to GHCR on push to main/tags
- **mobile-build.yml** — Mobile lint/test + EAS builds
- **desktop-build.yml** — Desktop builds for Windows (NSIS/MSI), macOS (DMG), Linux (AppImage/Deb)
