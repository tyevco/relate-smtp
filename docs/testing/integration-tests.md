# Integration Tests

Integration tests verify that Relate Mail's data access layer works correctly against a real PostgreSQL database. They test repository implementations, database queries, migrations, and data integrity constraints.

## Overview

**Project:** `api/tests/Relate.Smtp.Tests.Integration/`  
**Framework:** xUnit + Testcontainers  
**Category:** `[Trait("Category", "Integration")]`  
**Requirement:** Docker must be running

Unlike unit tests that mock the database, integration tests use [Testcontainers](https://testcontainers.com/) to spin up a real PostgreSQL 16 instance in a Docker container. This ensures that Entity Framework Core queries, migrations, and database constraints are tested against the actual database engine.

## Running

```bash
# Run all integration tests
cd api
dotnet test --filter "Category=Integration"

# Run with verbose output
dotnet test --filter "Category=Integration" --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~EmailRepositoryTests"

# Run with test result output
dotnet test --filter "Category=Integration" --logger "trx;LogFileName=integration-tests.trx" --results-directory ./TestResults
```

### Prerequisites

- .NET 10 SDK
- Docker running locally (Testcontainers manages the PostgreSQL container automatically)

## Test Organization

```
Relate.Smtp.Tests.Integration/
  Infrastructure/
    EmailRepositoryTests.cs        # Email CRUD, search, read/unread, filtering
    SmtpApiKeyRepositoryTests.cs   # API key storage, lookup, scope queries
    UserRepositoryTests.cs         # User creation, lookup, profile updates
  PostgresDatabaseCollection.cs    # Shared collection fixture for database lifecycle
  GlobalUsings.cs                  # Common using directives
```

## How Testcontainers Works

Testcontainers automatically manages the PostgreSQL lifecycle:

1. **Before tests run:** A PostgreSQL 16 container is started with a random available port
2. **Database initialization:** EF Core migrations are applied to create the schema
3. **During tests:** Each test class gets a connection to the running database
4. **After tests complete:** The container is stopped and removed

The `PostgresDatabaseCollection` class serves as an xUnit collection fixture, ensuring that all integration tests share a single PostgreSQL container instance. This avoids the overhead of starting a new container for each test class.

```
┌──────────────────────────────────────┐
│        Testcontainers Runtime        │
│                                      │
│   ┌──────────────────────────────┐   │
│   │     PostgreSQL 16 Container  │   │
│   │     (random port mapping)    │   │
│   │                              │   │
│   │   relate_mail_test database  │   │
│   │   (migrations applied)       │   │
│   └──────────────────────────────┘   │
│                                      │
│   Shared across all test classes     │
│   in the collection                  │
└──────────────────────────────────────┘
```

## Repository Tests

### EmailRepositoryTests

Tests the `EmailRepository` implementation that handles all email data operations:

- **CRUD operations:** Creating emails, reading by ID, updating read/unread status, deleting
- **List queries:** Paginated inbox queries, sorting by date, filtering by read status
- **Search:** Full-text search across email subjects and bodies
- **Labels:** Assigning and removing labels from emails, querying by label
- **Bulk operations:** Marking multiple emails as read, bulk deletion

### SmtpApiKeyRepositoryTests

Tests the `SmtpApiKeyRepository` that manages API key storage and lookup:

- **Key storage:** Storing BCrypt-hashed API keys with associated scopes
- **Key lookup:** Finding keys by prefix (first 8 characters) for fast authentication
- **Scope queries:** Verifying scope assignments and filtering
- **Revocation:** Soft-deleting keys and ensuring revoked keys are not returned in lookups

### UserRepositoryTests

Tests the `UserRepository` for user account management:

- **User creation:** Creating new users with email addresses
- **Lookup:** Finding users by ID, email address, or external identity
- **Profile updates:** Updating display names, additional email addresses
- **Provisioning:** Idempotent user creation (no duplicate accounts)

## Test Data Management

Integration tests use factories from the `Tests.Common` project to generate test data:

```csharp
var email = EmailFactory.Create(userId: testUser.Id, subject: "Test Subject");
var user = UserFactory.Create(email: "test@example.com");
var apiKey = SmtpApiKeyFactory.Create(userId: testUser.Id, scopes: ["smtp", "imap"]);
```

Each test is responsible for inserting its own test data, and tests should not depend on data created by other tests. While tests share the same database container, the order of test execution is not guaranteed.

## Database Cleanup

Tests should clean up after themselves to avoid interference between tests. Common strategies:

- **Transaction rollback:** Wrap each test in a transaction and roll back at the end
- **Explicit cleanup:** Delete created records in the test teardown
- **Unique data:** Use unique identifiers (GUIDs) for test data to avoid collisions

## CI Configuration

In the CI workflow, integration tests run with specific Testcontainers environment variables:

```yaml
env:
  TESTCONTAINERS_RYUK_DISABLED: false
  TESTCONTAINERS_REUSE_ENABLE: true
  TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE: /var/run/docker.sock
```

- **RYUK:** The Testcontainers resource reaper that cleans up dangling containers. Enabled to prevent container leaks on CI runners.
- **REUSE:** Allows container reuse across test classes within the same test run, significantly speeding up the test suite.
- **DOCKER_SOCKET:** Ensures Testcontainers can communicate with Docker on GitHub Actions runners.

## Faster Local Runs

To speed up repeated local test runs, enable container reuse:

```bash
# Set in your shell profile or before running tests
export TESTCONTAINERS_REUSE_ENABLE=true

# First run starts the container (slower)
dotnet test --filter "Category=Integration"

# Subsequent runs reuse the existing container (faster)
dotnet test --filter "Category=Integration"
```

With reuse enabled, the PostgreSQL container persists between test runs. Testcontainers will reuse it as long as the container configuration (image, environment variables) matches.

## Troubleshooting

### "Docker is not running"

Testcontainers requires a Docker daemon. Start Docker Desktop or the Docker service:

```bash
# Linux
sudo systemctl start docker

# macOS / Windows
# Start Docker Desktop application
```

### "Container startup timed out"

The PostgreSQL container may take longer to start on first run (image pull). Increase the startup timeout or pre-pull the image:

```bash
docker pull postgres:16-alpine
```

### "Connection refused" errors in tests

Ensure no other process is using the port assigned by Testcontainers. Since Testcontainers uses random ports, this is rare. Check Docker logs if it persists:

```bash
docker logs $(docker ps -q --filter ancestor=postgres:16-alpine)
```

## Writing New Integration Tests

1. Create a test class in `Tests.Integration/Infrastructure/`
2. Implement `IClassFixture<PostgresContainerFixture>` or join the `PostgresDatabaseCollection`
3. Add `[Trait("Category", "Integration")]` to the class or each test method
4. Use factories from `Tests.Common` for test data
5. Test actual database operations through the repository interface
6. Verify both the happy path and constraint violations (unique keys, foreign keys, etc.)
