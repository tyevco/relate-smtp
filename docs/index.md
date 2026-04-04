---
layout: home

hero:
  name: Relate Mail
  text: Full-Stack Email Platform
  tagline: Self-hosted email server with SMTP, POP3, IMAP, REST API, and modern clients for web, mobile, and desktop.
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started/
    - theme: alt
      text: API Reference
      link: /api-reference/
    - theme: alt
      text: GitHub
      link: https://github.com/four-robots/relate-mail

features:
  - icon: "\U0001F4E8"
    title: Complete Email Protocols
    details: Full implementations of SMTP (submission + MX inbound), POP3 (RFC 1939), and IMAP4rev2 (RFC 9051) — all sharing a single PostgreSQL database.
  - icon: "\U0001F310"
    title: REST API & Real-Time
    details: A comprehensive REST API for inbox management, composing, filters, labels, and preferences. SignalR WebSocket hub delivers real-time notifications to connected clients.
  - icon: "\U0001F4F1"
    title: Web, Mobile & Desktop Clients
    details: A React + Vite web app, a React Native (Expo) mobile app, and a Tauri 2 desktop app — all sharing a common UI component and type library.
  - icon: "\U0001F433"
    title: Docker-Ready Deployment
    details: Multi-service Docker Compose setup with pre-built images on GitHub Container Registry. Supports amd64 and arm64 architectures out of the box.
  - icon: "\U0001F512"
    title: Flexible Authentication
    details: Dual authentication model supporting OIDC/JWT for first-party users and scoped API keys for third-party integrations, mobile, and desktop clients.
  - icon: "\U0001F9E9"
    title: Clean Architecture
    details: .NET 10 backend following Clean Architecture principles with clear separation between domain, infrastructure, and presentation layers. Independently scalable protocol services.
---
