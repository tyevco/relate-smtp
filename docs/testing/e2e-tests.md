# End-to-End Tests

End-to-end (E2E) tests verify that the complete system works as expected by exercising real protocols and user interfaces. They provide the highest confidence that the system behaves correctly in production-like conditions.

## Backend E2E Tests

**Project:** `api/tests/Relate.Smtp.Tests.E2E/`  
**Framework:** xUnit + Testcontainers  
**Category:** `[Trait("Category", "E2E")]`  
**Requirement:** Docker must be running

### Overview

Backend E2E tests start the entire Relate Mail server stack in-process -- the API server, SMTP server, POP3 server, and IMAP server -- alongside a real PostgreSQL database via Testcontainers. Tests then connect to these servers using actual protocol clients and execute real command sequences.

```
┌─────────────────────────────────────────────────┐
│              FullStackFixture                     │
│                                                   │
│  ┌──────────┐  ┌──────┐  ┌──────┐  ┌──────┐    │
│  │ API      │  │ SMTP │  │ POP3 │  │ IMAP │    │
│  │ (in-proc)│  │      │  │      │  │      │    │
│  └─────┬────┘  └──┬───┘  └──┬───┘  └──┬───┘    │
│        │          │         │         │          │
│        └──────────┴────┬────┴─────────┘          │
│                        │                          │
│              ┌─────────┴─────────┐               │
│              │  PostgreSQL 16    │               │
│              │  (Testcontainers) │               │
│              └───────────────────┘               │
└─────────────────────────────────────────────────┘
          ▲           ▲          ▲
          │           │          │
     SMTP client  POP3 client  IMAP client
     (test code)  (test code)  (test code)
```

### Running

```bash
# Run all E2E tests
cd api
dotnet test --filter "Category=E2E"

# Run with verbose output
dotnet test --filter "Category=E2E" --verbosity normal

# Run only SMTP protocol tests
dotnet test --filter "FullyQualifiedName~SmtpProtocolTests"

# Run only POP3 protocol tests
dotnet test --filter "FullyQualifiedName~Pop3ProtocolTests"

# Run only IMAP protocol tests
dotnet test --filter "FullyQualifiedName~ImapProtocolTests"
```

### Test Organization

```
Relate.Smtp.Tests.E2E/
  Fixtures/
    FullStackFixture.cs      # Starts all servers + PostgreSQL
  Protocol/
    SmtpProtocolTests.cs     # SMTP command sequences
    Pop3ProtocolTests.cs     # POP3 RFC 1939 compliance
    ImapProtocolTests.cs     # IMAP RFC 9051 compliance
```

### Protocol Test Coverage

**SmtpProtocolTests:**
- SMTP handshake (EHLO/HELO) and capability negotiation
- Authentication (AUTH PLAIN, AUTH LOGIN) with API keys
- Mail submission (MAIL FROM, RCPT TO, DATA) with valid credentials
- Rejection of unauthenticated submission on ports 587/465
- Message size limit enforcement
- Multiple recipients (To, Cc, Bcc)
- Error handling for invalid commands and sequences

**Pop3ProtocolTests:**
- POP3 greeting and capability announcement
- Authentication sequence (USER + PASS with API keys)
- Session commands: STAT (mailbox status), LIST (message list), RETR (retrieve message)
- Message deletion (DELE) and undelete (RSET)
- Session termination (QUIT) with proper Update state
- RFC 1939 compliance for command ordering and state transitions

**ImapProtocolTests:**
- IMAP greeting and capability announcement
- LOGIN authentication with API keys
- Mailbox operations: SELECT, EXAMINE, LIST
- Message operations: FETCH (headers, body, flags), STORE (flag changes), SEARCH
- Session state transitions (Not Authenticated -> Authenticated -> Selected)
- LOGOUT sequence and connection cleanup
- RFC 9051 (IMAP4rev2) compliance

### FullStackFixture

The `FullStackFixture` class (in `Fixtures/FullStackFixture.cs`) orchestrates the complete test environment:

1. Starts a PostgreSQL container via `PostgresContainerFixture`
2. Applies database migrations
3. Starts the API server via `ApiServerFixture` (using `WebApplicationFactory`)
4. Starts the SMTP server via `SmtpServerFixture`
5. Starts the POP3 server via `Pop3ServerFixture`
6. Starts the IMAP server via `ImapServerFixture`
7. Creates test users and API keys for authentication

All servers bind to random available ports to avoid conflicts with other processes or parallel test runs. The fixture provides properties for accessing the assigned ports.

## Web E2E Tests

**Location:** `web/e2e/`  
**Framework:** Playwright  
**Browser:** Chromium

### Running

```bash
cd web

# Install Playwright browsers (first time only)
npm run test:e2e:install
# or: npx playwright install --with-deps chromium

# Run E2E tests (headless)
npm run test:e2e

# Run on a specific browser project
npm run test:e2e -- --project=chromium

# Run in headed mode (visible browser)
npm run test:e2e:ui

# Run a specific test file
npx playwright test e2e/inbox.spec.ts

# View the last test report
npx playwright show-report
```

### Prerequisites

- Node.js 22+
- Playwright browsers installed (`npx playwright install --with-deps chromium`)
- A running development server or built application

In CI, the web app is built first (`npm run build:web`) and then tested. For local development, you can run tests against the Vite dev server.

### Test Reports

Playwright generates an HTML report after each run. In CI, this report is uploaded as the `web-e2e-results` artifact. Locally, view it with:

```bash
npx playwright show-report
```

The report includes:
- Test results with pass/fail status
- Screenshots on failure
- Trace files for debugging (step-by-step recording of browser actions)

## Mobile E2E Tests

**Framework:** Detox  
**Platforms:** iOS Simulator, Android Emulator

### Running

```bash
cd mobile

# iOS (requires macOS with Xcode and iOS Simulator)
npm run test:e2e:ios

# Android (requires Android SDK and running emulator)
npm run test:e2e:android
```

### Prerequisites

**iOS:**
- macOS with Xcode installed
- iOS Simulator configured
- Built app (Detox builds or pre-built via EAS)

**Android:**
- Android SDK with emulator configured
- Running Android emulator
- Built app (Detox builds or pre-built via EAS)

### How Detox Works

Detox builds the mobile app, installs it on the simulator/emulator, and then drives the UI through a gray-box testing approach:

1. Detox launches the app on the target device
2. Test code sends commands to interact with UI elements (tap, type, scroll)
3. Detox synchronizes with the app's JavaScript bridge and native UI
4. Assertions verify the expected state of the UI

## Writing New E2E Tests

### Backend Protocol Tests

1. Add test methods to the appropriate class in `Tests.E2E/Protocol/`
2. Use `[Trait("Category", "E2E")]` on the class or method
3. Obtain server ports from the `FullStackFixture`
4. Use standard .NET TCP/SMTP/IMAP clients to connect
5. Execute protocol commands and assert responses
6. Test both success and error paths

Example pattern:

```csharp
[Fact]
[Trait("Category", "E2E")]
public async Task Smtp_SubmitEmail_WithValidCredentials_Succeeds()
{
    // Connect to SMTP on the fixture's assigned port
    // Authenticate with test API key
    // Send a test email
    // Verify the email was accepted
    // Optionally verify via POP3/IMAP retrieval
}
```

### Web E2E Tests

1. Add `.spec.ts` files in `web/e2e/`
2. Use Playwright's `page` API for browser interaction
3. Follow the Page Object pattern for reusable selectors
4. Use `expect` assertions from `@playwright/test`

### Mobile E2E Tests

1. Add test files in `mobile/e2e/`
2. Use Detox's `element`, `expect`, and `by` APIs
3. Account for platform-specific behavior (iOS vs. Android)
