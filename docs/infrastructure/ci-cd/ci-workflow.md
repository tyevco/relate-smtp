# CI Workflow

**File:** `.github/workflows/ci.yml`

The CI workflow is the primary quality gate for the Relate Mail monorepo. It validates every component that has changed, running builds, lints, type checks, and tests across the backend, web, mobile, and desktop projects.

## Triggers

| Event | Condition |
|-------|-----------|
| Pull request | Targeting `main` branch |
| Push | To `main` branch |
| Manual dispatch | `workflow_dispatch` (runs all jobs regardless of path changes) |

## Jobs

The workflow contains 14 jobs organized into detection, validation, and summary phases.

### Phase 1: Change Detection

#### `changes` -- Detect Changes

Runs on every trigger. Uses `dorny/paths-filter` to determine which areas of the monorepo have changed, producing boolean outputs consumed by downstream jobs:

| Output | Paths Monitored |
|--------|-----------------|
| `backend` | `api/**` |
| `web` | `web/**`, `packages/shared/**` |
| `mobile` | `mobile/**` |
| `desktop` | `desktop/**`, `packages/shared/**` |
| `docker` | `api/**`, `web/**`, `docker/**` |
| `shared` | `packages/shared/**` |

When triggered via `workflow_dispatch`, all jobs run regardless of path changes.

### Phase 2: Security

#### `secret-scan` -- Secret Scanning

Runs on every trigger, independent of path changes. Uses TruffleHog to scan the repository for accidentally committed secrets. The `--only-verified` flag means it only reports secrets that are confirmed active (e.g., a valid AWS key that actually authenticates), reducing false positives.

Requires a full Git history checkout (`fetch-depth: 0`) to scan all commits.

### Phase 3: Backend Pipeline

These jobs run sequentially when backend paths change.

#### `backend-build` -- Backend Build (.NET)

**Depends on:** `changes`  
**Condition:** `backend == 'true'` or manual dispatch

Steps:
1. Setup .NET 10 SDK
2. `dotnet restore` -- restore NuGet packages
3. `dotnet build --no-restore --configuration Release` -- compile all projects
4. `dotnet format --verify-no-changes` -- verify code formatting matches `.editorconfig`

#### `unit-tests` -- Unit Tests

**Depends on:** `changes`, `backend-build`  
**Condition:** `backend == 'true'` or manual dispatch

Runs `dotnet test --filter "Category=Unit"` against the `Relate.Smtp.Tests.Unit` project. These tests have no external dependencies (no database, no network) and execute quickly.

Test results are uploaded as a `.trx` artifact (`unit-test-results`) for inspection if tests fail.

#### `integration-tests` -- Integration Tests

**Depends on:** `changes`, `unit-tests`  
**Condition:** `backend == 'true'` or manual dispatch

Runs `dotnet test --filter "Category=Integration"` against `Relate.Smtp.Tests.Integration`. These tests use Testcontainers to spin up a real PostgreSQL instance, apply migrations, and run repository tests against a live database.

Environment variables:
- `TESTCONTAINERS_RYUK_DISABLED=false` -- enables container cleanup
- `TESTCONTAINERS_REUSE_ENABLE=true` -- reuses containers across test classes for speed
- `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock` -- Docker socket path for GitHub Actions runners

#### `e2e-tests` -- E2E & Protocol Tests

**Depends on:** `changes`, `integration-tests`  
**Condition:** `backend == 'true'` or manual dispatch

Runs `dotnet test --filter "Category=E2E"` against `Relate.Smtp.Tests.E2E`. These tests use the `FullStackFixture` to start all four servers (API, SMTP, POP3, IMAP) in-process alongside a Testcontainers PostgreSQL instance, then execute real protocol commands to verify end-to-end behavior.

### Phase 4: Web Frontend Pipeline

#### `web` -- Web Frontend

**Depends on:** `changes`  
**Condition:** `web == 'true'` or manual dispatch

Steps:
1. `npm ci` -- install all workspace dependencies
2. Lint shared package (`npm run lint -w @relate/shared`)
3. Type check shared package (`npm run typecheck -w @relate/shared`)
4. `npm run lint -w web` -- ESLint on web sources
5. `npx tsc --noEmit -p web/tsconfig.json` -- TypeScript type checking
6. `npm run build:web` -- Vite production build

#### `web-tests` -- Web Frontend Tests

**Depends on:** `changes`  
**Condition:** `web == 'true'` or manual dispatch

Runs Vitest unit tests (`npm run test:run -w web`) followed by coverage reporting (`npm run test:coverage -w web`). Coverage results are uploaded as an artifact.

Coverage thresholds are enforced: 60% statements/functions/lines, 55% branches.

#### `web-e2e` -- Web E2E Tests

**Depends on:** `changes`, `web-tests`  
**Condition:** `web == 'true'` or manual dispatch

Steps:
1. Install Playwright browsers (Chromium only)
2. Build the web app
3. Run Playwright tests against the Chromium project
4. Upload the Playwright HTML report as an artifact

### Phase 5: Mobile Pipeline

#### `mobile` -- Mobile App

**Depends on:** `changes`  
**Condition:** `mobile == 'true'` or manual dispatch

Working directory: `mobile/`

Steps:
1. `npm ci` -- install mobile dependencies
2. `npx tsc --noEmit` -- TypeScript type checking
3. `npm run lint` -- ESLint

#### `mobile-tests` -- Mobile App Tests

**Depends on:** `changes`  
**Condition:** `mobile == 'true'` or manual dispatch

Runs Jest unit tests (`npm test -- --ci`) and coverage (`npm run test:coverage -- --ci`). Coverage results are uploaded as an artifact.

### Phase 6: Desktop Pipeline

#### `desktop` -- Desktop App

**Depends on:** `changes`  
**Condition:** `desktop == 'true'` or manual dispatch

Steps:
1. `npm ci` -- install workspace dependencies
2. `npm run lint -w desktop` -- ESLint on TypeScript sources
3. `npx tsc --noEmit -p desktop/tsconfig.json` -- TypeScript type checking
4. Setup Rust toolchain with `clippy` component
5. Cache Cargo registry and build artifacts
6. Install system dependencies (`libwebkit2gtk-4.1-dev`, `librsvg2-dev`, `patchelf`, `libssl-dev`, `libgtk-3-dev`, `libayatana-appindicator3-dev`)
7. `cargo clippy --all-targets -- -D warnings` -- Rust linting (all warnings are errors)

### Phase 7: Docker Validation

#### `docker-validate` -- Docker Build Validation

**Depends on:** `changes`  
**Condition:** Pull requests only, when `docker == 'true'`

Uses a build matrix to validate all four Docker targets (api, smtp, pop3, imap) build successfully. Images are built but **not pushed**. GitHub Actions cache (`type=gha`) is used for layer caching.

### Phase 8: Summary

#### `summary` -- CI Summary

**Depends on:** All other jobs  
**Condition:** Always runs (even if some jobs fail or are skipped)

Generates a GitHub Actions step summary table showing:
- Which components had changes detected
- Build status for each component
- Test results for backend and frontend test suites

Skipped jobs (due to no changes detected) show as "skipped" in the summary.

## Job Dependency Graph

```
changes ─────────────────────────────────────────────────┐
  ├── secret-scan                                        │
  ├── backend-build ── unit-tests ── integration-tests ──┤
  │                                   └── e2e-tests ─────┤
  ├── web ───────────────────────────────────────────────┤
  ├── web-tests ── web-e2e ──────────────────────────────┤
  ├── mobile ────────────────────────────────────────────┤
  ├── mobile-tests ──────────────────────────────────────┤
  ├── desktop ───────────────────────────────────────────┤
  ├── docker-validate ───────────────────────────────────┤
  └──────────────────────────────────────────────────────┴── summary
```

## Runner Configuration

All jobs run on `ubuntu-latest` GitHub-hosted runners. The desktop job additionally requires system packages for the Rust/Tauri build, and the backend integration/E2E jobs require Docker (available on all GitHub-hosted runners).
