# Testing Overview

Relate Mail uses a testing pyramid approach with three tiers: fast unit tests at the base, integration tests in the middle, and end-to-end tests at the top. Each tier covers different aspects of the system, trading speed for confidence in real-world behavior.

## Testing Pyramid

```
         ┌───────────┐
         │   E2E     │  Fewest tests, slowest, highest confidence
         │  (full    │  Real protocols, real database
         │  stack)   │
         ├───────────┤
         │Integration│  Moderate count, moderate speed
         │ (database │  Real PostgreSQL via Testcontainers
         │  + repos) │
         ├───────────┤
         │   Unit    │  Most tests, fastest, isolated
         │(no deps)  │  No database, no network, mocked deps
         └───────────┘
```

## Test Suites by Platform

### Backend (.NET)

The backend tests live in `api/tests/` and are organized into four projects:

| Project | Category | What It Tests | Dependencies |
|---------|----------|---------------|-------------|
| `Tests.Unit` | Unit | Controllers, services, protocol parsing, session logic | None (mocked) |
| `Tests.Integration` | Integration | Repository implementations, database queries | Testcontainers PostgreSQL |
| `Tests.E2E` | E2E | Full protocol flows (SMTP, POP3, IMAP) | Testcontainers + all servers |
| `Tests.Common` | *(shared)* | Test fixtures, factories, helpers | *(used by other projects)* |

Backend tests use xUnit as the test framework and are categorized using the `[Trait("Category", "...")]` attribute, which allows selective execution via `dotnet test --filter "Category=Unit"`.

### Web Frontend

| Framework | Type | Location | Command |
|-----------|------|----------|---------|
| Vitest | Unit | `web/src/**/*.test.{ts,tsx}` | `npm run test:run -w web` |
| Playwright | E2E | `web/e2e/` | `npm run test:e2e -w web` |

Web unit tests use Vitest with happy-dom for DOM simulation and React Testing Library for component testing. MSW (Mock Service Worker) intercepts API calls in tests.

Coverage thresholds are enforced: 60% statements/functions/lines, 55% branches.

### Mobile

| Framework | Type | Location | Command |
|-----------|------|----------|---------|
| Jest | Unit | `mobile/**/*.test.{ts,tsx}` | `cd mobile && npm test` |
| Detox | E2E | `mobile/e2e/` | `cd mobile && npm run test:e2e:ios` |

Mobile unit tests use Jest with `@testing-library/react-native` for component testing.

### Desktop

The desktop app (Tauri 2) currently uses lint and type checking as its primary validation. The Rust backend is validated via `cargo clippy` with all warnings treated as errors.

## Running All Tests

### Quick Reference

```bash
# Backend unit tests (fastest, no dependencies)
cd api && dotnet test --filter "Category=Unit"

# Backend integration tests (requires Docker for Testcontainers)
cd api && dotnet test --filter "Category=Integration"

# Backend E2E tests (requires Docker, starts all servers)
cd api && dotnet test --filter "Category=E2E"

# All backend tests
cd api && dotnet test

# Web unit tests
cd web && npm run test:run

# Web unit tests with coverage
cd web && npm run test:coverage

# Web E2E tests (requires running dev server)
cd web && npm run test:e2e

# Mobile unit tests
cd mobile && npm test

# Mobile unit tests with coverage
cd mobile && npm run test:coverage
```

### Prerequisites

| Test Type | Requirements |
|-----------|-------------|
| Backend unit tests | .NET 10 SDK |
| Backend integration tests | .NET 10 SDK + Docker |
| Backend E2E tests | .NET 10 SDK + Docker |
| Web unit tests | Node.js 22+ |
| Web E2E tests | Node.js 22+ + Playwright browsers |
| Mobile unit tests | Node.js 22+ |
| Mobile E2E tests | iOS Simulator or Android Emulator + Detox |

## Test Project Structure

```
api/tests/
  Relate.Smtp.Tests.Unit/
    Api/
      Controllers/         # Controller unit tests
      Services/            # Service unit tests
    ImapHost/              # IMAP protocol parsing tests
    Pop3Host/              # POP3 protocol parsing tests
    SmtpHost/              # SMTP authenticator and filter tests
  Relate.Smtp.Tests.Integration/
    Infrastructure/        # Repository integration tests
  Relate.Smtp.Tests.E2E/
    Fixtures/              # FullStackFixture (starts all servers)
    Protocol/              # Protocol-level E2E tests
  Relate.Smtp.Tests.Common/
    Factories/             # Test data factories
    Fixtures/              # Reusable test fixtures
    Helpers/               # Auth helpers, utilities
```

## CI Integration

All test suites run in the CI workflow (`.github/workflows/ci.yml`). See [CI Workflow](../infrastructure/ci-cd/ci-workflow) for details on how tests are executed in the pipeline.

Test results are uploaded as artifacts in `.trx` format (backend) and coverage reports (web, mobile) for post-run analysis.

## Next Steps

- [Unit Tests](./unit-tests) -- Detailed guide for backend, web, and mobile unit tests
- [Integration Tests](./integration-tests) -- Database integration testing with Testcontainers
- [E2E Tests](./e2e-tests) -- Full stack protocol and browser testing
- [Test Common](./test-common) -- Shared fixtures, factories, and helpers
