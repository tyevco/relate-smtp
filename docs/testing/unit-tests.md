# Unit Tests

Unit tests are the fastest and most numerous tests in the Relate Mail test suite. They test individual components in isolation with all external dependencies mocked or stubbed.

## Backend Unit Tests

**Project:** `api/tests/Relate.Smtp.Tests.Unit/`  
**Framework:** xUnit  
**Category:** `[Trait("Category", "Unit")]`

### Running

```bash
# Run all backend unit tests
cd api
dotnet test --filter "Category=Unit"

# Run with verbose output
dotnet test --filter "Category=Unit" --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~EmailsControllerTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~EmailsControllerTests.GetEmails_ReturnsOk"

# Run tests in a specific directory/namespace
dotnet test --filter "FullyQualifiedName~Tests.Unit.Api"
```

### Test Organization

The unit test project mirrors the structure of the source projects it tests:

```
Relate.Smtp.Tests.Unit/
  Api/
    Controllers/
      EmailsControllerTests.cs       # Inbox CRUD endpoints
      LabelsControllerTests.cs        # Label management endpoints
      ProfileControllerTests.cs       # User profile endpoints
    Services/
      SmtpCredentialServiceTests.cs   # API key generation and validation
      UserProvisioningServiceTests.cs # User creation and setup
  ImapHost/
    ImapCommandTests.cs               # IMAP command parsing
    ImapSessionTests.cs               # IMAP session state machine
  Pop3Host/
    Pop3CommandTests.cs               # POP3 command parsing
    Pop3SessionTests.cs               # POP3 session state machine
  SmtpHost/
    CustomUserAuthenticatorTests.cs   # SMTP authentication logic
    MxMailboxFilterTests.cs           # MX domain/recipient validation
```

### Controller Tests

Controller tests verify that API endpoints return correct HTTP responses for given inputs. Dependencies (repositories, services) are mocked using a mocking framework.

**What they test:**
- Request validation and parameter binding
- Correct HTTP status codes (200, 201, 400, 404, etc.)
- Response body structure and content
- Authorization behavior (correct user scoping)
- Error handling and error response format

**Example patterns:**

```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task GetEmails_ReturnsOk_WithEmailList()
{
    // Arrange - mock repository to return test data
    // Act - call controller method
    // Assert - verify 200 OK with expected data
}

[Fact]
[Trait("Category", "Unit")]
public async Task GetEmails_ReturnsNotFound_WhenUserDoesNotExist()
{
    // Arrange - mock repository to return null
    // Act - call controller method
    // Assert - verify 404 response
}
```

### Service Tests

Service tests verify business logic in service classes that sit between controllers and repositories.

- **SmtpCredentialServiceTests** -- Tests API key generation, hashing, validation, scope checking, and revocation logic
- **UserProvisioningServiceTests** -- Tests user creation, default settings provisioning, and idempotent provisioning behavior

### Protocol Tests

Protocol tests verify the command parsing and session state management for each email protocol server.

**IMAP tests:**
- `ImapCommandTests` -- Parsing of IMAP commands (LOGIN, SELECT, FETCH, SEARCH, STORE, etc.) and argument extraction
- `ImapSessionTests` -- State transitions (Not Authenticated -> Authenticated -> Selected -> Logout), command availability per state, concurrent mailbox operations

**POP3 tests:**
- `Pop3CommandTests` -- Parsing of POP3 commands (USER, PASS, STAT, LIST, RETR, DELE, QUIT) and argument validation
- `Pop3SessionTests` -- State transitions (Authorization -> Transaction -> Update), lock handling, message deletion tracking

**SMTP tests:**
- `CustomUserAuthenticatorTests` -- API key authentication for SMTP submission, scope validation, BCrypt hash verification, in-memory cache behavior
- `MxMailboxFilterTests` -- Domain validation for the MX endpoint (accept mail for hosted domains, reject for others), recipient validation against the database

## Web Unit Tests

**Location:** `web/src/**/*.test.{ts,tsx}`  
**Framework:** Vitest + happy-dom + React Testing Library  
**Mock layer:** MSW (Mock Service Worker)

### Running

```bash
# Run all web unit tests (single pass)
cd web
npm run test:run

# Watch mode (re-runs on file changes)
npm run test

# Run with coverage report
npm run test:coverage

# Run a specific test file
npx vitest run src/components/EmailList.test.tsx

# Run tests matching a pattern
npx vitest run --grep "should render email subject"

# Run tests in a specific directory
npx vitest run src/components/
```

### Configuration

Vitest is configured in `web/vitest.config.ts`:

| Setting | Value |
|---------|-------|
| Environment | `happy-dom` (fast DOM implementation) |
| Globals | `true` (no need to import `describe`, `it`, etc.) |
| Setup file | `src/test/setup.ts` |
| Include | `src/**/*.{test,spec}.{ts,tsx}` |
| Exclude | `node_modules`, `dist`, `e2e` |
| Timeout | 10s per test, 10s per hook |

### Coverage Thresholds

Coverage is enforced via thresholds in the Vitest config. Tests fail if coverage drops below:

| Metric | Threshold |
|--------|-----------|
| Statements | 60% |
| Branches | 55% |
| Functions | 60% |
| Lines | 60% |

Coverage excludes generated files (`routeTree.gen.ts`), configuration files, type declarations, and test setup files.

The coverage provider is V8 and reports are generated in text, JSON, HTML, and LCOV formats.

### Testing Patterns

**Component tests** render React components and verify their output:

```typescript
import { render, screen } from '@testing-library/react'

it('should render the email subject', () => {
  render(<EmailListItem email={mockEmail} />)
  expect(screen.getByText('Test Subject')).toBeInTheDocument()
})
```

**API mocking** uses MSW to intercept fetch calls:

```typescript
import { http, HttpResponse } from 'msw'
import { server } from '@/test/server'

beforeEach(() => {
  server.use(
    http.get('/api/emails', () => {
      return HttpResponse.json({ emails: [mockEmail] })
    })
  )
})
```

## Mobile Unit Tests

**Location:** `mobile/**/*.test.{ts,tsx}`  
**Framework:** Jest + @testing-library/react-native

### Running

```bash
# Run all mobile tests
cd mobile
npm test

# Run in CI mode (no watch, exits with code)
npm test -- --ci

# Run with coverage
npm run test:coverage

# Run a specific test file
npx jest app/components/EmailList.test.tsx

# Run tests matching a pattern
npx jest --testPathPattern="EmailList"
```

### Testing Patterns

Mobile tests follow similar patterns to web tests but use React Native Testing Library:

```typescript
import { render, screen } from '@testing-library/react-native'

it('should display the email subject', () => {
  render(<EmailListItem email={mockEmail} />)
  expect(screen.getByText('Test Subject')).toBeTruthy()
})
```

## Writing New Unit Tests

### Backend

1. Add test files to the appropriate directory in `Relate.Smtp.Tests.Unit/`
2. Always include the category trait: `[Trait("Category", "Unit")]`
3. Follow the Arrange-Act-Assert pattern
4. Mock all external dependencies (repositories, HTTP clients, etc.)
5. Test both happy paths and error cases

### Web

1. Place test files next to the source file: `Component.tsx` -> `Component.test.tsx`
2. Use React Testing Library's `render` and `screen` for component tests
3. Use MSW for API mocking -- avoid mocking fetch directly
4. Test user-visible behavior, not implementation details

### Mobile

1. Place test files next to the source file or in a `__tests__` directory
2. Use `@testing-library/react-native` for component rendering
3. Test both rendering output and user interactions
