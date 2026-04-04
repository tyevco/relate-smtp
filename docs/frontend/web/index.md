# Web Application

The Relate Mail web application is a single-page app built with React 19, TypeScript, and Vite 7.3. It provides a full-featured email client in the browser with real-time updates, composing and sending mail, label and filter management, and user preferences.

::: info Screenshot
**[Screenshot placeholder: Inbox view]**

_TODO: Add screenshot of the inbox view showing the email list, search bar, and detail panel_
:::

## Technology Stack

| Concern | Library | Version |
|---------|---------|---------|
| UI framework | React | 19.2 |
| Language | TypeScript | 5.9 |
| Build tool | Vite | 7.3 |
| Routing | TanStack Router | 1.x (file-based) |
| Server state | TanStack Query | 5.x |
| Client state | Jotai | 2.x |
| CSS framework | Tailwind CSS | 4.1 |
| Component variants | CVA (class-variance-authority) | 0.7 |
| UI primitives | Radix UI | latest |
| Icons | Lucide React | 0.563 |
| Real-time | SignalR (`@microsoft/signalr`) | 8.x |
| Auth | react-oidc-context + oidc-client-ts | 3.x |
| Testing | Vitest + MSW + Playwright | latest |

## Project Structure

```
web/
  src/
    api/              # API client, TanStack Query hooks, SignalR connection, types
    auth/             # OIDC AuthProvider wrapper
    components/
      mail/           # Email-specific components (list, detail, search, labels, etc.)
      filters/        # Filter builder components
      ui/             # Shadcn/ui components (badge, button, card, dialog, etc.)
    lib/              # Utilities, logger
    routes/           # TanStack Router file-based routes
    test/             # Test setup, mocks, factories
    config.ts         # Runtime configuration loader
    main.tsx          # Application entry point
  e2e/                # Playwright E2E tests
  public/             # Static assets
```

## Development

Start the dev server:

```bash
cd web
npm run dev
```

The Vite dev server runs on **port 5492** and proxies all `/api` requests to the backend at `http://localhost:5000` (configurable via `VITE_API_PROXY_URL` environment variable). This avoids CORS issues during local development.

### Path Alias

The `@/` path alias maps to `src/`, so imports look like:

```typescript
import { api } from '@/api/client'
import { Button } from '@/components/ui/button'
```

This is configured in both `vite.config.ts` (for Vite) and `tsconfig.json` (for TypeScript).

## Build

Create a production build:

```bash
cd web
npm run build
```

This runs TypeScript type checking (`tsc -b`) followed by Vite's production bundler. Output goes to `dist/`.

## Key Architectural Decisions

### File-Based Routing

Routes are defined as files in `src/routes/`. The TanStack Router plugin for Vite (`@tanstack/router-plugin/vite`) automatically generates `routeTree.gen.ts` from these files. Never edit the generated file manually -- it is regenerated on every dev server restart and build.

See [Routing](/frontend/web/routing) for the complete route reference.

### Server State vs. Client State

The app separates concerns between two state layers:

- **TanStack Query** manages all server-derived state: emails, profile, labels, filters, preferences, SMTP credentials. Data is fetched via hooks, cached, and automatically invalidated when mutations succeed.
- **Jotai** manages ephemeral client-only state: UI toggles, selected email ID, search input, pagination cursors. Jotai atoms are lightweight and avoid the boilerplate of context providers.

### Real-Time Updates via SignalR

The inbox route establishes a SignalR connection to `/hubs/email` on mount. When the server pushes events (new email, status change, deletion), the SignalR handler invalidates the relevant TanStack Query cache entries, causing the UI to refetch and update seamlessly.

See [SignalR Integration](/frontend/web/signalr-integration) for details.

### Shared Component Library

UI and mail components are imported from `@relate/shared` wherever possible. The web app's `src/components/` directory contains either re-exports of shared components or web-specific components that are not needed by the desktop or mobile clients.

See [Shared Package](/frontend/shared/) for the full component and utility reference.

## Available Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Vite dev server on port 5492 |
| `npm run build` | Production build (type check + bundle) |
| `npm run lint` | Run ESLint |
| `npm run test` | Vitest in watch mode |
| `npm run test:run` | Vitest single run |
| `npm run test:coverage` | Vitest with coverage report |
| `npm run test:e2e` | Playwright E2E tests |
| `npm run test:e2e:ui` | Playwright in headed/UI mode |
| `npm run test:e2e:install` | Install Playwright browsers |
| `npm run preview` | Preview production build locally |
