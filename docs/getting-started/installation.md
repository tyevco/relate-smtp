# Installation

This guide walks through cloning the repository, installing dependencies, and verifying that everything builds correctly. Once complete, you can proceed to either [Local Development](./local-development) or [Docker Quickstart](./docker-quickstart).

## Prerequisites

Before you begin, make sure the following tools are installed:

| Tool | Minimum Version | Check Command |
|---|---|---|
| **Node.js** | 20.0.0 | `node --version` |
| **npm** | 10+ (ships with Node 20) | `npm --version` |
| **.NET SDK** | 10.0 | `dotnet --version` |
| **Git** | 2.x | `git --version` |

**Optional tools** (needed for specific platforms):

| Tool | Needed For | Check Command |
|---|---|---|
| **Docker** | Docker deployment, running PostgreSQL locally | `docker --version` |
| **Rust / Cargo** | Desktop app (Tauri 2) | `rustc --version` |
| **Expo CLI** | Mobile app development | `npx expo --version` |

## Clone the Repository

```bash
git clone https://github.com/four-robots/relate-mail.git
cd relate-mail
```

## Install Frontend Dependencies

Relate Mail uses npm workspaces to coordinate the frontend packages (web, desktop, shared library, and docs). A single `npm install` at the root pulls in dependencies for all workspaces:

```bash
npm install
```

Next, build the shared package. The web, desktop, and docs packages all depend on `@relate/shared`, so this must be built before anything else:

```bash
npm run build:shared
```

This compiles the shared TypeScript types, UI components, and utility functions that the other frontend packages import.

## Build the Backend

The .NET backend lives in the `api/` directory and contains six projects. Build them all at once:

```bash
cd api
dotnet build
```

This compiles:

- **Relate.Smtp.Core** — Domain entities and repository interfaces
- **Relate.Smtp.Infrastructure** — Entity Framework Core data access, migrations, and services
- **Relate.Smtp.Api** — REST API with controllers, authentication, and SignalR hub
- **Relate.Smtp.SmtpHost** — SMTP server (submission + MX inbound)
- **Relate.Smtp.Pop3Host** — POP3 server
- **Relate.Smtp.ImapHost** — IMAP server

## Verify the Build

Run the unit tests to make sure everything is working:

```bash
# From the api/ directory
dotnet test --filter "Category=Unit"
```

Unit tests do not require a database or any running services. They should pass immediately after a clean build.

For the frontend, verify the web app builds cleanly:

```bash
# From the repository root
npm run build:web
```

## What's Next

You now have all code compiled and ready. Choose your next step:

- **[Local Development](./local-development)** — Start PostgreSQL, the API, protocol servers, and a frontend dev server for day-to-day development work.
- **[Docker Quickstart](./docker-quickstart)** — Skip local toolchain setup and run everything in containers.
