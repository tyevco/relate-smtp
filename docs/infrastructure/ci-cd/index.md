# CI/CD Overview

Relate Mail uses GitHub Actions for continuous integration and continuous delivery. The CI system is designed around two principles: **build only what changed** (path-filtered pipelines) and **security first** (secret scanning, vulnerability analysis, and supply chain verification on every run).

## Workflows

Six workflow files live in `.github/workflows/`:

| Workflow | File | Purpose |
|----------|------|---------|
| [CI](./ci-workflow) | `ci.yml` | Primary build, lint, and test pipeline for all components |
| [Docker Publish](./docker-publish) | `docker-publish.yml` | Build and push multi-platform Docker images to GHCR |
| [Mobile Build](./mobile-build) | `mobile-build.yml` | Build mobile apps via Expo EAS |
| [Desktop Build](./desktop-build) | `desktop-build.yml` | Build desktop apps for Windows, macOS, and Linux |
| [CodeQL](./security-scanning#codeql) | `codeql.yml` | Static analysis for C# and JavaScript/TypeScript |
| [OpenSSF Scorecard](./security-scanning#openssf-scorecard) | `scorecard.yml` | Supply chain security assessment |

## Trigger Summary

| Event | CI | Docker Publish | Mobile Build | Desktop Build | CodeQL | Scorecard |
|-------|:--:|:--------------:|:------------:|:-------------:|:------:|:---------:|
| Push to `main` | Yes | Yes | Yes | Yes | Yes | Yes |
| Pull request | Yes | -- | Yes | Yes | Yes | -- |
| Version tag (`v*`) | -- | Yes | -- | -- | -- | -- |
| Manual dispatch | Yes | Yes | Yes | Yes | Yes | Yes |
| Weekly schedule | -- | -- | -- | -- | Yes | Yes |

## Path Filtering

The CI workflow uses [dorny/paths-filter](https://github.com/dorny/paths-filter) to detect which parts of the monorepo have changed. Jobs only run when their relevant paths are modified:

| Filter | Paths | Jobs Triggered |
|--------|-------|----------------|
| `backend` | `api/**` | Backend build, unit tests, integration tests, E2E tests |
| `web` | `web/**`, `packages/shared/**` | Web lint/build, web unit tests, web E2E tests |
| `mobile` | `mobile/**` | Mobile lint/typecheck, mobile unit tests |
| `desktop` | `desktop/**`, `packages/shared/**` | Desktop lint/typecheck, Rust clippy |
| `docker` | `api/**`, `web/**`, `docker/**` | Docker build validation |
| `shared` | `packages/shared/**` | Triggers web and desktop rebuilds |

This approach keeps CI fast -- a change to only the mobile app does not trigger backend tests or Docker builds.

## Security Scanning

Security is integrated at multiple levels:

- **TruffleHog** -- Scans every PR for accidentally committed secrets (verified secrets only to minimize false positives)
- **Trivy** -- Scans Docker images for known vulnerabilities (CRITICAL and HIGH severity) during the publish workflow
- **CodeQL** -- Weekly static analysis of C# and JavaScript/TypeScript code for security vulnerabilities and code quality issues
- **OpenSSF Scorecard** -- Weekly assessment of supply chain security practices (dependency pinning, branch protection, signed releases, etc.)

See [Security Scanning](./security-scanning) for details on each tool.

## Permissions

All workflows use `permissions: read-all` as their default, following the principle of least privilege. Individual jobs that need write access (e.g., publishing Docker images, uploading SARIF results) explicitly declare the specific permissions they require.

## Artifacts

Several jobs upload artifacts for debugging and reporting:

| Artifact | Workflow | Contents |
|----------|----------|----------|
| `unit-test-results` | CI | `.trx` test results file |
| `integration-test-results` | CI | `.trx` test results file |
| `e2e-test-results` | CI | `.trx` test results file |
| `web-coverage-report` | CI | HTML/JSON coverage report |
| `web-e2e-results` | CI | Playwright HTML report |
| `mobile-coverage-report` | CI | Jest coverage report |
| `desktop-windows-msi` | Desktop Build | Windows MSI installer |
| `desktop-windows-nsis` | Desktop Build | Windows NSIS installer |
| `desktop-macos-dmg` | Desktop Build | macOS DMG image |
| `desktop-linux-appimage` | Desktop Build | Linux AppImage |
| `desktop-linux-deb` | Desktop Build | Linux .deb package |
