# Testing

The web application has two testing layers: **unit/component tests** using Vitest and **end-to-end tests** using Playwright.

## Unit and Component Tests

Unit tests use **Vitest** with the **happy-dom** environment for fast, Node-based DOM simulation. Tests live alongside the source code with a `.test.tsx` or `.test.ts` suffix.

### Running Tests

```bash
cd web
npm run test:run         # Single run (CI-friendly)
npm run test             # Watch mode (re-runs on file changes)
npm run test:coverage    # Single run with coverage report
npm run test:ui          # Vitest UI (browser-based test viewer)
```

### Coverage Thresholds

The project enforces minimum coverage thresholds. The build fails if coverage drops below:

| Metric | Threshold |
|--------|-----------|
| Statements | 60% |
| Branches | 55% |
| Functions | 60% |
| Lines | 60% |

Coverage reports are generated in four formats: `text` (console), `json`, `html`, and `lcov`.

Files excluded from coverage measurement:

- `node_modules/`, `src/test/` (test infrastructure)
- `*.d.ts` (type declarations)
- `*.config.*` (configuration files)
- `routeTree.gen.ts` (auto-generated)
- `src/main.tsx` (entry point)
- `src/vite-env.d.ts` (Vite type shim)

### Test Setup (`src/test/setup.ts`)

The setup file runs before every test file and establishes the testing environment:

**Environment stubs:**

- `import.meta.env` is stubbed with test-appropriate values (`VITE_API_URL: '/api'`, `MODE: 'test'`, etc.)
- `window.matchMedia` is mocked to return `matches: false` (prevents media query errors)
- `ResizeObserver` is mocked (prevents errors from components that observe element sizes)
- `IntersectionObserver` is mocked (prevents errors from lazy-loading or virtualization)
- `Element.prototype.scrollTo` and `window.scrollTo` are mocked (prevents errors from scroll operations)

**MSW (Mock Service Worker):**

- `beforeAll`: Starts the MSW server with `onUnhandledRequest: 'warn'` (warns about API calls that don't have a matching handler rather than failing)
- `afterEach`: Calls `cleanup()` to unmount rendered components and `server.resetHandlers()` to remove any per-test handler overrides
- `afterAll`: Stops the MSW server

### Test Query Client

Tests that render components using TanStack Query need a `QueryClientProvider`. The test utilities provide `createTestQueryClient()` which creates a client configured for testing:

- **No retries** -- Queries fail immediately instead of retrying, making assertion errors obvious
- **`staleTime: 0`** -- Data is always considered stale, preventing caching from interfering with test isolation

### Custom Render

The test utilities provide an `AllProviders` wrapper that includes all necessary context providers (QueryClient, Router, etc.), so individual tests don't need to set up the provider tree manually.

```typescript
import { render } from '@/test/utils'

render(<EmailList />) // Automatically wrapped with all providers
```

### Mock Factories

The test utilities include factory functions for creating test data:

| Factory | Description |
|---------|-------------|
| `createMockEmailListItem()` | Returns an `EmailListItem` with realistic defaults |
| `createMockEmailDetail()` | Returns a full `EmailDetail` with recipients and attachments |
| `createMockProfile()` | Returns a `Profile` with additional addresses |

These factories accept partial overrides, so tests can specify only the fields they care about:

```typescript
const email = createMockEmailListItem({ isRead: false, attachmentCount: 3 })
```

### MSW Handlers

API mocking is done with MSW (Mock Service Worker), which intercepts `fetch` calls at the network level. Default handlers are defined in `src/test/mocks/` and return realistic responses for all API endpoints.

Individual tests can override handlers for specific scenarios:

```typescript
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

server.use(
  http.get('/api/emails', () => {
    return HttpResponse.json({ items: [], totalCount: 0, unreadCount: 0, page: 1, pageSize: 20 })
  })
)
```

### Example Test

```typescript
import { render, screen } from '@/test/utils'
import { EmailList } from '@/components/mail/email-list'

describe('EmailList', () => {
  it('displays unread indicator for unread emails', async () => {
    render(<EmailList emails={[createMockEmailListItem({ isRead: false })]} />)
    expect(screen.getByRole('listitem')).toHaveClass('bg-primary/5')
  })
})
```

### Timeouts

- **Test timeout**: 10 seconds
- **Hook timeout**: 10 seconds (for setup/teardown hooks)

These are configured in `vitest.config.ts` and are generous enough for async operations while catching truly hanging tests.

## End-to-End Tests

E2E tests use **Playwright** and live in the `e2e/` directory. They run against a real browser and a running dev server, testing complete user workflows.

### Running E2E Tests

```bash
cd web

# Install browsers (first time only)
npm run test:e2e:install

# Run tests headlessly
npm run test:e2e

# Run tests with Playwright's interactive UI
npm run test:e2e:ui
```

### Prerequisites

E2E tests require:

1. Playwright browsers installed (`npm run test:e2e:install`)
2. The web dev server running (`npm run dev`)
3. The backend API running (`dotnet run --project src/Relate.Smtp.Api` from the `api/` directory)

### Configuration

Playwright configuration is in `playwright.config.ts` at the web root. The configuration points at the local dev server URL and sets browser-specific options for Chromium, Firefox, and WebKit.
