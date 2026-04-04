# Development Setup

This guide walks through setting up a local development environment for Relate Mail from scratch.

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| **Node.js** | 22+ | Web, mobile, desktop, and shared package builds |
| **npm** | 10+ (bundled with Node.js) | Package management |
| **.NET SDK** | 10.0 | Backend API and protocol servers |
| **Docker** | 20+ | PostgreSQL (via Docker Compose or Testcontainers) |
| **Git** | 2.30+ | Version control |

### Optional

| Tool | Version | Purpose |
|------|---------|---------|
| **Rust** | stable | Desktop app (Tauri) development |
| **Xcode** | 15+ | iOS mobile development (macOS only) |
| **Android Studio** | Latest | Android mobile development |
| **EAS CLI** | Latest | Mobile cloud builds (`npm install -g eas-cli`) |

### Installing Prerequisites

**Node.js** (recommended: use [nvm](https://github.com/nvm-sh/nvm) or [fnm](https://github.com/Schniz/fnm)):

```bash
# Using fnm
fnm install 22
fnm use 22

# Or using nvm
nvm install 22
nvm use 22
```

**.NET SDK** (download from [dot.net](https://dot.net/download)):

```bash
# Verify installation
dotnet --version  # Should output 10.0.x
```

**Docker** (download from [docker.com](https://www.docker.com/products/docker-desktop)):

```bash
# Verify installation
docker --version
docker compose version
```

## IDE Recommendations

### Visual Studio Code

Recommended extensions:

| Extension | Purpose |
|-----------|---------|
| **C# Dev Kit** | .NET development, debugging, IntelliSense |
| **ESLint** | JavaScript/TypeScript linting |
| **Tailwind CSS IntelliSense** | CSS class autocompletion |
| **Prettier** | Code formatting |
| **REST Client** | Test API endpoints from `.http` files |

### JetBrains Rider

Excellent for .NET development with built-in support for C#, Entity Framework, and Docker. Also handles TypeScript/React well.

### Visual Studio 2024

Full-featured IDE for .NET development on Windows. Use the ASP.NET workload for backend development.

## Step-by-Step Setup

### 1. Fork and Clone

```bash
# Fork on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/relate-mail.git
cd relate-mail
```

### 2. Install Frontend Dependencies

```bash
# Install all workspace dependencies (web, mobile, desktop, shared)
npm install

# Build the shared package (prerequisite for web, desktop)
npm run build:shared
```

The shared package (`packages/shared/`) must be built first because the web and desktop projects import from `@relate/shared`.

### 3. Build the Backend

```bash
cd api
dotnet build
```

This restores NuGet packages and compiles all six projects (Core, Infrastructure, Api, SmtpHost, Pop3Host, ImapHost).

### 4. Start PostgreSQL

The simplest way is to use the Docker Compose file that ships with the project:

```bash
cd docker

# Create a minimal .env file
cat > .env << 'EOF'
POSTGRES_PASSWORD=devpassword
EOF

# Start only PostgreSQL
docker compose up postgres -d

# Verify it's running
docker compose ps
```

PostgreSQL will be available at `localhost:5432` with:
- Username: `postgres`
- Password: `devpassword`
- Database: `relate_mail`

Alternatively, if you have PostgreSQL installed locally, create the database:

```bash
createdb relate_mail
```

### 5. Run the API

```bash
cd api
dotnet run --project src/Relate.Smtp.Api
```

The API starts on `http://localhost:5000` (in development mode). On first run, it automatically applies database migrations to create the schema.

Since `Oidc__Authority` is not configured, the API runs in **development mode** without OIDC authentication.

### 6. Run the Web Frontend

In a separate terminal:

```bash
# From the repository root
npm run dev:web
```

The Vite dev server starts on `http://localhost:5492` and proxies `/api` requests to `http://localhost:5000`.

### 7. Verify

Open `http://localhost:5492` in your browser. You should see the Relate Mail web interface.

::: info Screenshot
![Screenshot: Development environment](./screenshots/dev-environment.png)

_TODO: Add screenshot of the Relate Mail web interface running in development mode_
:::

## Running Protocol Servers (Optional)

If you are working on SMTP, POP3, or IMAP, start those servers in additional terminals:

```bash
# SMTP server (ports 587, 465)
cd api
dotnet run --project src/Relate.Smtp.SmtpHost

# POP3 server (ports 110, 995)
cd api
dotnet run --project src/Relate.Smtp.Pop3Host

# IMAP server (ports 143, 993)
cd api
dotnet run --project src/Relate.Smtp.ImapHost
```

Each server connects to the same PostgreSQL database and reads its configuration from `appsettings.json` + `appsettings.Development.json`.

## Running Tests

### Backend Tests

```bash
cd api

# Unit tests (fast, no dependencies)
dotnet test --filter "Category=Unit"

# Integration tests (requires Docker for Testcontainers)
dotnet test --filter "Category=Integration"

# E2E tests (requires Docker, starts all servers in-process)
dotnet test --filter "Category=E2E"

# All tests
dotnet test
```

### Web Tests

```bash
cd web

# Unit tests (single run)
npm run test:run

# Unit tests (watch mode, re-runs on changes)
npm run test

# Coverage report
npm run test:coverage

# E2E tests (requires Playwright browsers)
npx playwright install --with-deps chromium
npm run test:e2e
```

### Mobile Tests

```bash
cd mobile

# Unit tests
npm test

# Coverage
npm run test:coverage
```

## Database Management

### Auto-Migration

In development mode (`ASPNETCORE_ENVIRONMENT=Development`), the API automatically applies pending migrations on startup. You do not need to run migration commands manually for existing migrations.

### Creating New Migrations

When you change entity classes or the DbContext, create a new migration:

```bash
cd api
dotnet ef migrations add YourMigrationName \
    --project src/Relate.Smtp.Infrastructure \
    --startup-project src/Relate.Smtp.Api
```

Apply it:

```bash
dotnet ef database update \
    --project src/Relate.Smtp.Infrastructure \
    --startup-project src/Relate.Smtp.Api
```

Or simply restart the API -- auto-migration will apply it.

### Reset Database

To drop and recreate the database:

```bash
# Using Docker
cd docker
docker compose down -v
docker compose up postgres -d

# Or using dotnet ef
cd api
dotnet ef database drop --project src/Relate.Smtp.Infrastructure --startup-project src/Relate.Smtp.Api
dotnet ef database update --project src/Relate.Smtp.Infrastructure --startup-project src/Relate.Smtp.Api
```

## Common Development Workflows

### Working on the Web Frontend

```bash
# Terminal 1: Start the API
cd api && dotnet run --project src/Relate.Smtp.Api

# Terminal 2: Start the web dev server (with hot reload)
npm run dev:web
```

Changes to React components and TypeScript files are reflected instantly via Vite's hot module replacement.

### Working on the Backend API

```bash
# Terminal 1: Start PostgreSQL
cd docker && docker compose up postgres -d

# Terminal 2: Run the API with file watching
cd api && dotnet watch run --project src/Relate.Smtp.Api
```

`dotnet watch` restarts the server when C# files change.

### Working on the Shared Package

```bash
# Terminal 1: Watch and rebuild the shared package
npm run dev -w @relate/shared

# Terminal 2: Start the web dev server (picks up shared changes)
npm run dev:web
```

### Working on the Desktop App

```bash
# Prerequisites: Rust toolchain installed

# Linux system dependencies
sudo apt-get install -y libwebkit2gtk-4.1-dev librsvg2-dev patchelf libssl-dev libgtk-3-dev libayatana-appindicator3-dev

# Run in development mode (Tauri + Vite)
npm run dev:desktop
```

## Environment Variables for Development

The defaults in `appsettings.json` and `appsettings.Development.json` are sufficient for most development work. If you need to override settings:

```bash
# Set before running the API
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=relate_mail;Username=postgres;Password=devpassword"

# Or create a .env file in the api/ directory (supported by some IDEs)
```

## Troubleshooting

### "Connection refused" when starting the API

PostgreSQL is not running. Start it with Docker Compose:

```bash
cd docker && docker compose up postgres -d
```

### "Port already in use"

Another process is using the port. Find and stop it:

```bash
# Find what's using port 5000
lsof -i :5000

# Or use a different port
cd api && dotnet run --project src/Relate.Smtp.Api --urls "http://localhost:5001"
```

### "Module not found: @relate/shared"

The shared package needs to be built first:

```bash
npm run build:shared
```

### "Entity Framework migrations failed"

Ensure the connection string is correct and PostgreSQL is running. For a fresh start:

```bash
cd docker && docker compose down -v && docker compose up postgres -d
```

### Web frontend shows a blank page

Check the browser console for errors. Common causes:
- The API is not running (start it on port 5000)
- The shared package is not built (`npm run build:shared`)
- Dependencies are outdated (`npm install` at the repo root)
