# Frontend Overview

Relate Mail ships three client applications that communicate with the same .NET backend API. All three are built with React, TypeScript, and TanStack Query, and they share a common library of types, UI components, and utilities through the `@relate/shared` package.

## Client Applications

| App | Framework | State Management | Routing | Directory |
|-----|-----------|-----------------|---------|-----------|
| **Web** | React 19 + Vite 7.3 | TanStack Query + Jotai | TanStack Router (file-based) | `web/` |
| **Mobile** | React Native + Expo 54 | TanStack Query + Zustand | Expo Router (file-based) | `mobile/` |
| **Desktop** | Tauri 2 + React 19 | TanStack Query + Jotai | TanStack Router | `desktop/` |

### Web

The primary client application. A single-page app built with React 19, Vite 7.3, and Tailwind CSS 4.1. It uses TanStack Router for file-based routing, TanStack Query for server state, and Jotai for lightweight client-side state. The dev server runs on port 5492 and proxies `/api` requests to the backend at `localhost:5000`.

### Mobile

A React Native app built with Expo 54. It uses Expo Router for navigation, Zustand for local state, and TanStack Query for server state. API keys are persisted via Expo Secure Store, and OIDC authentication flows through Expo Auth Session.

### Desktop

A native desktop app powered by Tauri 2, which wraps the React frontend in a lightweight Rust shell. It shares almost all UI code with the web app through `@relate/shared` and adds desktop-specific capabilities through Tauri plugins for shell access, native notifications, and window state management.

## Shared Package: `@relate/shared`

The `@relate/shared` npm package is the linchpin that prevents code duplication across the three clients. It provides:

- **API type definitions** -- TypeScript interfaces for every API request and response, ensuring type safety across all clients
- **UI components** -- A Shadcn/ui-based component library built on Radix UI primitives (Button, Card, Dialog, Badge, Input, etc.)
- **Mail components** -- Email-specific presentation components (EmailList, EmailDetail, SearchBar, LabelBadge)
- **Utilities** -- Class name merging (`cn`), HTML sanitization for email bodies, and shared constants like pagination defaults
- **Theme CSS** -- CSS custom properties that define the design system's color palette for both light and dark modes

See [Shared Package](/frontend/shared/) for details.

## Workspace Organization

The monorepo uses **npm workspaces** to coordinate dependency management and builds across the frontend packages:

```
relate-mail/
  web/                    # npm workspace member
  desktop/                # npm workspace member
  packages/shared/        # npm workspace member (@relate/shared)
  mobile/                 # standalone (Expo requires its own node_modules)
```

The web and desktop apps import `@relate/shared` as a workspace dependency, which resolves to the local `packages/shared/` directory. The mobile app is intentionally excluded from workspaces because Expo's Metro bundler expects its own isolated `node_modules` tree -- it imports shared types directly rather than consuming the full package.

### Build Order

The shared package must be built (type-checked) before the consuming apps:

```bash
# From the repository root:
npm install                 # Install all workspace dependencies
npm run build:shared        # Type-check @relate/shared (prerequisite)
npm run dev:web             # Start web dev server
npm run dev:desktop         # Start desktop with Tauri
```

## Common Patterns

### Server State with TanStack Query

All three clients use TanStack Query for fetching, caching, and synchronizing server state. This means:

- Data is fetched declaratively using hooks (e.g., `useEmails()`, `useProfile()`)
- Responses are cached with configurable stale times (default: 30 seconds)
- Mutations automatically invalidate related queries on success
- Background refetching keeps the UI current without manual refreshes

### Authentication

The web and desktop clients authenticate via OIDC (OpenID Connect) with authorization code flow and PKCE. The mobile app supports both OIDC and API key authentication. When no OIDC provider is configured, all clients run in development mode without authentication.

### Real-Time Updates

The web client connects to the backend's SignalR hub at `/hubs/email` for real-time push notifications. When new emails arrive or delivery statuses change, the SignalR connection triggers TanStack Query cache invalidation so the UI updates instantly without polling.
