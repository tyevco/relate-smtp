# Changelog

All notable changes to Relate Mail are documented here. This project uses [Conventional Commits](https://www.conventionalcommits.org/).

## [Unreleased]

### Added
- **OpenSSF Scorecard** workflow for supply chain security analysis
- README badges for CI, CodeQL, OpenSSF Scorecard, License, and GHCR
- GitHub community health files (CODE_OF_CONDUCT, CONTRIBUTING, SECURITY, SUPPORT, CODEOWNERS, FUNDING)

### Security
- Pin GitHub Actions to SHA digests for supply chain integrity
- Add explicit token permissions to all CI workflows
- Pin scorecard-action to v2.4.3 and codeql-action to v4

## [0.9.0] - 2026-02-09

### Added
- **Outbound mail delivery (MTA)** - Full email composition and sending with compose UI, drafts, outbox, sent mail, reply, reply-all, and forward
- **MX endpoint** - Accept inbound internet mail on port 25 for hosted domains (with open relay prevention)
- **Email verification workflow** - Verification codes for additional email addresses
- **SSL certificate pinning** for mobile platforms (iOS/Android)
- **Biometric authentication** for mobile app (fingerprint/Face ID)
- **API key rotation UI** and old key expiry notifications in mobile app

### Fixed
- EF Core migration for outbound email tables
- Build errors in SmtpDeliveryService and OutboundEmailsController
- MxMailboxFilter service registration

## [0.8.0] - 2026-02-08

### Security
- **Comprehensive security audit remediation** (GitHub issues #182-#217) covering 35 issues across 6 phases
- Resolve CodeQL sensitive-info alerts and container CVEs
- Rename parameters to break CodeQL data flow taint tracking

### Fixed
- Resolve remaining GitHub issues (#141, #145, #147, #149, #154, #155, #161)
- Security and code quality issues (#75-#80, #112, #131)
- PostgreSQL-compatible case-insensitive comparison
- Desktop Clippy warnings for Rust code
- Mobile ESLint errors and warnings
- Code analysis validations across backend and frontend

### Changed
- Upgrade CodeQL Action from v3 to v4
- Enforce build validations and strengthen linting across the monorepo
- Replace gitleaks with TruffleHog for secret scanning

## [0.7.0] - 2026-02-06

### Fixed
- 5 critical security vulnerabilities from code review
- 7 backend and frontend bugs from code review
- HIGH, MEDIUM, and LOW priority bugs across 6 batches
- E2E test API mocking fixtures for tests without backend
- Docker config.json generation at correct path
- Playwright E2E tests with proper auth token injection

## [0.6.0] - 2026-02-05

### Added
- **OpenTelemetry instrumentation** for distributed tracing and metrics
- CI path filtering to run jobs only when relevant files change
- Connection limits per user and per IP
- Bounded line reader for protocol parsing safety
- Protocol session base class shared by POP3 and IMAP

### Changed
- Extract shared `ProtocolAuthenticator` base class for SMTP/POP3/IMAP
- Extract `ClaimsPrincipalExtensions.GetUserId()` to eliminate duplication

## [0.5.0] - 2026-02-03

### Added
- **Tauri 2 desktop app** for Windows with native window management, keyboard shortcuts, tray icon, and notifications
- **@relate/shared npm package** - Shared types, UI components, mail components, and utilities across web/mobile/desktop
- Desktop CI/CD workflows for Windows (NSIS/MSI), macOS (DMG), Linux (AppImage/Deb)
- `app` scope for first-party mobile and desktop API keys

### Changed
- Rebrand from "Relate SMTP" to "Relate Mail"
- Migrate organization from tyevco to four-robots
- Refactor web app to use @relate/shared package
- Establish monorepo structure with npm workspaces

### Fixed
- Mobile OIDC auth and API key authentication flow
- Desktop window state hook with LogicalSize/LogicalPosition

## [0.4.0] - 2026-02-01

### Added
- **Comprehensive test suite** with xUnit v3 and Testcontainers (unit, integration, E2E)
- Frontend testing for web (Vitest + Playwright) and mobile (Jest + Detox)
- Mobile component and hook tests with React Native preset
- EAS build configuration for mobile CI

### Changed
- Upgrade to Expo SDK 54 with React Native 0.81

## [0.3.0] - 2026-01-31

### Added
- **React Native mobile app** with Expo - multi-account support, swipe actions, real-time notifications
- Responsive design for mobile, tablet, and desktop viewports
- Email detail view route

### Fixed
- Broken pipe errors in IMAP and POP3 servers

## [0.2.0] - 2026-01-30

### Added
- **IMAP4rev2 server** (RFC 9051) with ENVELOPE, BODYSTRUCTURE, FETCH, SEARCH, and AUTHENTICATE PLAIN
- IMAP Docker image and CI integration

## [0.1.0] - 2026-01-28

### Added
- **SMTP server** with per-user API key authentication (ports 587, 465)
- **POP3 server** implementation (RFC 1939) with TLS support
- **REST API** with OIDC/JWT authentication
- **Web frontend** - React + TypeScript + Vite with TanStack Router
- **Email management** - Labels, filters, preferences, push notifications, search, threading
- **Sent mail tracking** for emails sent via SMTP
- **External API** with scoped permissions (`smtp`, `pop3`, `imap`, `api:read`, `api:write`)
- **Docker deployment** with multi-stage builds and PostgreSQL
- **SignalR real-time updates** for new emails, read status, and unread counts
- User provisioning and email address management
- Runtime configuration (deploy once, configure anywhere)

## [0.0.1] - 2026-01-27

### Added
- Initial commit with project scaffolding
