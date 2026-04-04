# Test Common (Shared Test Infrastructure)

**Project:** `api/tests/Relate.Smtp.Tests.Common/`

The `Tests.Common` project provides shared fixtures, factories, and helpers used by the unit, integration, and E2E test projects. It is not a test project itself -- it contains no tests -- but is referenced as a dependency by the other test projects.

## Project Structure

```
Relate.Smtp.Tests.Common/
  Factories/
    EmailFactory.cs          # Generate test email entities
    LabelFactory.cs          # Generate test label entities
    SmtpApiKeyFactory.cs     # Generate test API key entities
    UserFactory.cs           # Generate test user entities
  Fixtures/
    ApiServerFixture.cs      # In-process API server (WebApplicationFactory)
    ImapServerFixture.cs     # In-process IMAP server
    Pop3ServerFixture.cs     # In-process POP3 server
    PostgresContainerFixture.cs  # Testcontainers PostgreSQL
    SmtpServerFixture.cs     # In-process SMTP server
  Helpers/
    ClaimsPrincipalFactory.cs    # Create authenticated identities for tests
    TestAuthenticationHandler.cs # Bypass real auth in integration tests
    TestHelpers.cs               # General test utilities
```

## Factories

Factories generate pre-populated entity instances for use in tests. They provide sensible defaults for all required fields while allowing overrides for specific test scenarios.

### EmailFactory

Creates `Email` entity instances with realistic default values:

```csharp
// Create an email with defaults
var email = EmailFactory.Create();

// Create with specific values
var email = EmailFactory.Create(
    userId: testUser.Id,
    subject: "Important meeting",
    from: "sender@example.com",
    isRead: false
);
```

Default values include a generated subject, body, sender address, recipient address, and received date. The factory ensures all required fields are populated so the entity can be inserted into the database without constraint violations.

### LabelFactory

Creates `Label` entities for organizing emails:

```csharp
// Create a label with defaults
var label = LabelFactory.Create();

// Create with specific values
var label = LabelFactory.Create(
    userId: testUser.Id,
    name: "Work",
    color: "#FF5733"
);
```

### SmtpApiKeyFactory

Creates `SmtpApiKey` entities representing API credentials:

```csharp
// Create an API key with defaults
var apiKey = SmtpApiKeyFactory.Create();

// Create with specific scopes
var apiKey = SmtpApiKeyFactory.Create(
    userId: testUser.Id,
    scopes: new[] { "smtp", "imap", "pop3" }
);
```

The factory generates a realistic API key value and its BCrypt hash, so tests can use both the plaintext key (for authentication) and the stored hash (for database assertions).

### UserFactory

Creates `User` entities:

```csharp
// Create a user with defaults
var user = UserFactory.Create();

// Create with specific values
var user = UserFactory.Create(
    email: "alice@example.com",
    displayName: "Alice Smith"
);
```

## Fixtures

Fixtures manage the lifecycle of test infrastructure -- databases, servers, and other resources that need to be started before tests and stopped after.

### PostgresContainerFixture

Manages a PostgreSQL 16 container via Testcontainers. This is the foundation for all tests that need a real database.

**Lifecycle:**
1. On creation: Starts a `postgres:16-alpine` container on a random port
2. Applies Entity Framework Core migrations to create the database schema
3. Provides a connection string property for creating DbContext instances
4. On disposal: Stops and removes the container (unless reuse is enabled)

**Used by:** Integration tests, E2E tests

When `TESTCONTAINERS_REUSE_ENABLE=true` is set, the container persists between test runs for faster iteration.

### ApiServerFixture

Creates an in-process ASP.NET Core test server using `WebApplicationFactory<Program>`. This starts the full API pipeline (middleware, routing, controllers, dependency injection) without binding to a real network port.

**Key behaviors:**
- Replaces the real database connection with the Testcontainers PostgreSQL connection
- Configures `TestAuthenticationHandler` to bypass OIDC authentication
- Exposes an `HttpClient` for making API requests in tests
- Runs the same middleware pipeline as production (including authorization, validation, etc.)

**Used by:** E2E tests (as part of `FullStackFixture`)

### SmtpServerFixture

Starts the SMTP server in-process, bound to a random available port.

**Configuration:**
- Binds to `localhost` on an ephemeral port
- Uses the shared PostgreSQL connection from `PostgresContainerFixture`
- Authentication is enabled with test API keys
- MX mode can be toggled for testing inbound mail acceptance

**Provides:** The assigned port number for connecting test SMTP clients

### Pop3ServerFixture

Starts the POP3 server in-process, similar to the SMTP fixture.

**Provides:** The assigned port number for connecting test POP3 clients

### ImapServerFixture

Starts the IMAP server in-process, similar to the other protocol fixtures.

**Provides:** The assigned port number for connecting test IMAP clients

### FullStackFixture (E2E)

The `FullStackFixture` in `Tests.E2E/Fixtures/` composes all of the above fixtures into a complete test environment. It is not in `Tests.Common` but deserves mention here as the primary consumer of the common fixtures.

**Composition:**

```
FullStackFixture
  ├── PostgresContainerFixture    (real database)
  ├── ApiServerFixture            (REST API + web)
  ├── SmtpServerFixture           (SMTP protocol)
  ├── Pop3ServerFixture           (POP3 protocol)
  └── ImapServerFixture           (IMAP protocol)
```

**Lifecycle:**
1. Starts PostgreSQL container
2. Applies migrations
3. Creates test users and API keys
4. Starts all four application servers
5. Provides connection details (ports, credentials) to tests
6. Tears everything down after the test collection completes

## Helpers

### ClaimsPrincipalFactory

Creates `ClaimsPrincipal` instances for simulating authenticated users in unit tests. This is used when testing controllers or services that read user identity from the HTTP context.

```csharp
// Create a principal with a specific user ID and email
var principal = ClaimsPrincipalFactory.Create(
    userId: "user-123",
    email: "alice@example.com"
);
```

The factory sets up the standard claims (NameIdentifier, Email, Name) that the application expects from OIDC tokens.

### TestAuthenticationHandler

An ASP.NET Core authentication handler that bypasses real authentication in test environments. When registered with `WebApplicationFactory`, it automatically authenticates all requests with a configurable test identity.

This allows integration and E2E tests to make authenticated API requests without configuring a real OIDC provider or generating valid JWTs.

**How it works:**
1. Registered as the default authentication scheme in `ApiServerFixture`
2. Intercepts all authentication challenges
3. Returns a successful authentication result with a test `ClaimsPrincipal`
4. The principal's claims can be configured per-test if needed

### TestHelpers

General-purpose utility methods used across test projects:

- Random string generation for unique test data
- Port availability checking for server fixtures
- Retry helpers for eventual consistency assertions
- Common assertion helpers

## Using Test Common in New Test Projects

To reference `Tests.Common` from a new test project:

```xml
<ProjectReference Include="..\Relate.Smtp.Tests.Common\Relate.Smtp.Tests.Common.csproj" />
```

Then use factories and fixtures:

```csharp
using Relate.Smtp.Tests.Common.Factories;
using Relate.Smtp.Tests.Common.Fixtures;

public class MyNewTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _database;

    public MyNewTests(PostgresContainerFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MyTest()
    {
        var user = UserFactory.Create(email: "test@example.com");
        // ... test logic using _database.ConnectionString
    }
}
```
