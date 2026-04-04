# Architecture Overview

Relate Mail is composed of four backend services and three frontend clients, all sharing a single PostgreSQL database. This page describes the high-level design, the reasoning behind key architectural decisions, and how the components communicate.

::: info Screenshot
**[Screenshot placeholder: Architecture diagram]**

_TODO: Add screenshot of high-level architecture diagram showing backend services, database, and clients_
:::

## System Components

### Backend Services

The backend is a .NET 10.0 solution containing four independently runnable services:

| Service | Role | Ports |
|---|---|---|
| **REST API** | HTTP API, authentication, SignalR WebSocket hub | 5000 (dev) / 8080 (Docker) |
| **SMTP Host** | Email submission (587/465) and MX inbound (25) | 25, 465, 587 |
| **POP3 Host** | Mailbox download access (RFC 1939) | 110, 995 |
| **IMAP Host** | Full mailbox access (IMAP4rev2 / RFC 9051) | 143, 993 |

All four services connect directly to the same PostgreSQL database. They share the Core (domain) and Infrastructure (data access) libraries but run as separate processes.

### Frontend Clients

| Client | Technology | Distribution |
|---|---|---|
| **Web** | React + TypeScript + Vite | Browser, served by any web server |
| **Mobile** | React Native + Expo 54 | iOS and Android via Expo/EAS |
| **Desktop** | Tauri 2 (Rust + TypeScript) | Windows, macOS, Linux native apps |

All three clients depend on `@relate/shared`, a shared npm package containing API type definitions, UI components, and utility functions.

### Database

PostgreSQL 16 is the single source of truth. All email content, user data, credentials, filters, labels, preferences, and outbound email queues live in one database. Entity Framework Core manages the schema through code-first migrations.

## Design Principles

### Clean Architecture (.NET Backend)

The backend follows Clean Architecture with three layers:

- **Core** contains domain entities (Email, OutboundEmail, Filter, Label, etc.) and repository interfaces. It has zero external dependencies.
- **Infrastructure** implements the repository interfaces using Entity Framework Core, provides database migrations, and contains service implementations for delivery, health checks, and telemetry.
- **Presentation** layer consists of the four host projects (API, SMTP, POP3, IMAP) that wire up dependency injection and expose their respective interfaces.

This separation means the domain logic is completely independent of the database technology, and each host project can be deployed and scaled independently.

### Separate Processes for Protocol Servers

Each protocol (SMTP, POP3, IMAP) runs in its own process rather than being bundled into the API. This design provides several benefits:

- **Independent scaling.** An IMAP server handling hundreds of persistent connections has very different resource needs than a REST API handling short-lived HTTP requests. Separate processes let you scale each service based on its actual load.
- **Protocol isolation.** A bug or crash in the SMTP server does not take down the API or IMAP access. Each protocol server can be restarted independently.
- **Security boundaries.** The SMTP MX endpoint on port 25 accepts unauthenticated inbound mail from the internet. Running it in a separate process with its own security configuration limits the blast radius of any vulnerability.
- **Deployment flexibility.** You can run all four services on one machine during development, then distribute them across multiple hosts or containers in production.

### File-Based Routing in Frontends

Both the web app (TanStack Router) and mobile app (Expo Router) use file-based routing, where the filesystem structure under the routes directory determines the application's URL structure. This convention eliminates manual route registration and keeps navigation discoverable by reading the directory tree.

### Runtime Configuration

The backend uses the standard .NET configuration system (`appsettings.json` + environment variable overrides), which means the same compiled binary can run in development, staging, and production with different configuration. The frontend uses Vite environment variables (`VITE_*`) at build time and a runtime config endpoint (`/api/config`) for values that need to change after deployment.

## Communication Patterns

### Service-to-Service: HTTP Internal Endpoint

When the SMTP server receives a new email, it needs to notify the API so that connected clients can be updated in real time. This happens through an HTTP POST to the API's internal notifications endpoint (`/api/internal-notifications`), authenticated with an API key that has the `internal` scope.

```
SMTP Host  ──HTTP POST──>  API (/api/internal-notifications)
                              │
                              ├──> SignalR hub ──WebSocket──> Web/Desktop clients
                              └──> Push notification ──> Mobile clients
```

This HTTP-based approach was chosen over message queues or shared-memory IPC because it works identically whether the services run on the same machine or across a network, and it does not introduce additional infrastructure dependencies.

### Client-to-API: REST + SignalR

Clients interact with the API through two channels:

1. **REST API** for all CRUD operations (reading emails, composing, managing filters and labels, etc.)
2. **SignalR WebSocket hub** at `/hubs/email` for real-time push notifications when new emails arrive or email state changes

The web and desktop clients connect to SignalR directly. The mobile client receives push notifications via web push (VAPID) for background updates and uses the REST API for foreground operations.

### Protocol Client Access

Standard email clients (Thunderbird, Apple Mail, Outlook) connect to the POP3 and IMAP servers using API key authentication. The protocol servers authenticate against the same credential store as the REST API, using BCrypt-hashed API keys with a 30-second in-memory cache for performance.

## Authentication Model

Relate Mail supports two authentication mechanisms that can operate simultaneously:

- **OIDC/JWT** for first-party web users. The API validates tokens against a configured OIDC authority. This is optional and can be disabled for development.
- **Scoped API keys** for third-party integrations, mobile clients, desktop clients, and protocol access. API keys are created through the REST API and can be restricted to specific scopes: `smtp`, `pop3`, `imap`, `api:read`, `api:write`, `app`, `internal`.

Both mechanisms are handled by the `ApiKeyAuthenticationHandler`, which inspects Bearer and ApiKey tokens in the Authorization header.

## Further Reading

- [Monorepo Structure](./monorepo-structure) — Detailed walkthrough of the repository layout
- [Data Flow](./data-flow) — Step-by-step traces through inbound delivery, outbound sending, and protocol access
