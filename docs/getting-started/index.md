# Getting Started

Relate Mail is a self-hosted, full-stack email platform. It provides production-grade SMTP, POP3, and IMAP servers alongside a REST API, and ships with clients for web, mobile, and desktop. All services share a single PostgreSQL database, giving you a complete email system you can run on your own infrastructure.

## What You Get

- **SMTP server** for sending and receiving email (authenticated submission on ports 587/465, MX inbound on port 25)
- **POP3 server** for downloading messages (RFC 1939, ports 110/995)
- **IMAP server** for full mailbox access (IMAP4rev2 / RFC 9051, ports 143/993)
- **REST API** for programmatic access to inboxes, composing email, filters, labels, preferences, and more
- **Web app** built with React, TypeScript, and Vite
- **Mobile app** built with React Native and Expo
- **Desktop app** built with Tauri 2 (Rust + TypeScript)
- **Real-time notifications** via SignalR WebSocket hub and web push

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Node.js | 20+ | For building frontend packages |
| .NET SDK | 10.0 | For building the backend services |
| PostgreSQL | 16+ | The shared database for all services |
| Docker | 24+ | Optional, but recommended for quick deployment |

If you just want to try Relate Mail without installing the full development toolchain, the [Docker Quickstart](./docker-quickstart) is the fastest path.

## Choose Your Path

### I want to run Relate Mail quickly

Follow the **[Docker Quickstart](./docker-quickstart)** guide. You will use pre-built container images from GitHub Container Registry and have the full platform running in minutes with a single `docker compose up` command.

### I want to develop locally

1. Start with **[Installation](./installation)** to clone the repository, install dependencies, and verify the build.
2. Then follow **[Local Development](./local-development)** to start each service and begin working on the codebase.

## Next Steps

- [Installation](./installation) — Clone, install, and build
- [Local Development](./local-development) — Run services locally for development
- [Docker Quickstart](./docker-quickstart) — Deploy with Docker Compose
- [Architecture Overview](/architecture/) — Understand how the system fits together
