# Local Development

This guide explains how to start all Relate Mail services on your machine for development. You will need multiple terminal windows, one for each service. Make sure you have completed [Installation](./installation) first.

## 1. Start PostgreSQL

The easiest way to get PostgreSQL running locally is with Docker:

```bash
cd docker
docker compose up postgres -d
```

This starts a PostgreSQL 16 container with the default credentials defined in `docker-compose.yml`. The database data is persisted in a Docker volume, so it survives container restarts.

If you prefer to use an existing PostgreSQL installation, set the connection string via environment variable before starting the backend services:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=relate_mail;Username=postgres;Password=yourpassword"
```

## 2. Start the REST API

The API is the central service. It handles authentication, serves the REST endpoints, runs database migrations, and hosts the SignalR WebSocket hub.

```bash
cd api
dotnet run --project src/Relate.Smtp.Api
```

The API starts on **http://localhost:5000**. In development mode (when `Oidc__Authority` is not set), it runs without authentication, so you can immediately start making requests.

On first startup, the API automatically applies any pending Entity Framework Core migrations to the database. You do not need to run migrations manually in development.

## 3. Start Protocol Servers

Each protocol server runs as a separate process. Open a new terminal for each one:

### SMTP Server

```bash
cd api
dotnet run --project src/Relate.Smtp.SmtpHost
```

Listens on:
- **Port 587** — Authenticated submission (STARTTLS)
- **Port 465** — Authenticated submission (implicit TLS)
- **Port 25** — MX inbound (unauthenticated, for receiving internet mail; only accepts mail for configured hosted domains)

### POP3 Server

```bash
cd api
dotnet run --project src/Relate.Smtp.Pop3Host
```

Listens on:
- **Port 110** — Cleartext (STLS upgrade available)
- **Port 995** — Implicit TLS

### IMAP Server

```bash
cd api
dotnet run --project src/Relate.Smtp.ImapHost
```

Listens on:
- **Port 143** — Cleartext (STARTTLS upgrade available)
- **Port 993** — Implicit TLS

All protocol servers share the same PostgreSQL database and use API key authentication with BCrypt hashing.

## 4. Start a Frontend

### Web App

From the repository root:

```bash
npm run dev:web
```

The Vite dev server starts on **http://localhost:5492** and automatically proxies `/api` requests to `http://localhost:5000` (the REST API). Open a browser and navigate to `http://localhost:5492` to see the web interface.

::: info Screenshot
**[Screenshot placeholder: Web app inbox view]**

_TODO: Add screenshot of the web app running in development mode_
:::

### Mobile App

The mobile app uses Expo and runs outside the npm workspaces:

```bash
cd mobile
npm start
```

This launches the Expo dev server. Scan the QR code with the Expo Go app on your phone, or press `i` for the iOS simulator / `a` for the Android emulator.

The mobile app uses Expo Secure Store for API key persistence and Expo Auth Session for OIDC authentication.

### Desktop App

From the repository root:

```bash
npm run dev:desktop
```

This starts the Tauri 2 development build, which opens a native window running the React frontend with the Rust backend.

::: tip
The desktop build requires Rust and platform-specific dependencies. See the [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/) for your operating system.
:::

## Development Mode Authentication

When the `Oidc__Authority` environment variable (backend) or `VITE_OIDC_AUTHORITY` (frontend) is **not set**, the entire system runs in development mode without authentication. This means:

- The API does not validate JWT tokens
- The web app skips the login flow
- You can interact with all endpoints freely

This is the default behavior when running locally and makes it easy to develop without configuring an identity provider.

## Summary of Services

| Service | Command | Ports |
|---|---|---|
| PostgreSQL | `docker compose up postgres -d` | 5432 |
| REST API | `dotnet run --project src/Relate.Smtp.Api` | 5000 |
| SMTP Server | `dotnet run --project src/Relate.Smtp.SmtpHost` | 25, 465, 587 |
| POP3 Server | `dotnet run --project src/Relate.Smtp.Pop3Host` | 110, 995 |
| IMAP Server | `dotnet run --project src/Relate.Smtp.ImapHost` | 143, 993 |
| Web Frontend | `npm run dev:web` | 5492 |
| Mobile | `cd mobile && npm start` | 8081 (Expo) |
| Desktop | `npm run dev:desktop` | Native window |

## Running Tests

With the services running, you can execute the full test suite:

```bash
# Unit tests (no services required)
cd api && dotnet test --filter "Category=Unit"

# Integration tests (requires PostgreSQL via Testcontainers)
cd api && dotnet test --filter "Category=Integration"

# E2E tests (requires full stack via Testcontainers)
cd api && dotnet test --filter "Category=E2E"

# Frontend unit tests
npm run test -w web

# Frontend E2E tests (requires running dev server)
cd web && npm run test:e2e
```
