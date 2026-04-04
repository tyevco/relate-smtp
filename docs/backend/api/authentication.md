# Authentication

Relate Mail uses a dual authentication system that supports both OIDC/JWT tokens (for first-party web and mobile clients) and API keys (for third-party integrations, protocol hosts, and mobile app persistence).

## Authentication Schemes

Two authentication schemes are registered and both are accepted by the default authorization policy:

```csharp
options.DefaultPolicy = new AuthorizationPolicyBuilder(
    JwtBearerDefaults.AuthenticationScheme,  // "Bearer"
    ApiKeyAuthenticationExtensions.ApiKeyScheme)  // "ApiKey"
    .RequireAuthenticatedUser()
    .Build();
```

This means any endpoint with `[Authorize]` accepts either a valid JWT or a valid API key. Controllers that need to restrict to API key only use `[Authorize(AuthenticationSchemes = ApiKeyAuthenticationExtensions.ApiKeyScheme)]`.

## OIDC/JWT Authentication

When `Oidc:Authority` is configured, the API validates JWT tokens against the configured OIDC provider:

- **Issuer validation** -- tokens must come from the configured authority
- **Audience validation** -- enforced when `Oidc:Audience` is set
- **Lifetime validation** -- expired tokens are rejected
- **Signing key validation** -- keys are fetched from the OIDC provider's JWKS endpoint

### Development Mode

When `Oidc:Authority` is not set (empty or missing), the API falls back to development mode using a symmetric signing key:

1. If `Jwt:DevelopmentKey` is configured, that key is used for token validation
2. Otherwise, a random 32-byte key is generated at startup (tokens are invalidated on restart)

Development tokens use:
- Issuer: `relate-mail-dev`
- Audience: `relate-mail`

```
WARNING: Using random dev JWT key -- tokens invalidated on restart.
Set Jwt:DevelopmentKey for persistent tokens.
```

## API Key Authentication

The `ApiKeyAuthenticationHandler` processes API key credentials from the `Authorization` header. It accepts two formats:

```
Authorization: Bearer <api-key>
Authorization: ApiKey <api-key>
```

The Bearer format is accepted because many HTTP clients and libraries default to Bearer authentication, making API keys easier to use in third-party integrations.

### Authentication Flow

1. **Extract** -- the handler reads the `Authorization` header and extracts the key value
2. **Lookup** -- calls `ISmtpApiKeyRepository.GetByKeyAsync()` which uses the 12-character prefix for an efficient database index lookup, then verifies the full key against the BCrypt hash
3. **Scope check** -- verifies the key has at least one API-relevant scope (`api:read`, `api:write`, or `app`). Keys with only protocol scopes (`smtp`, `pop3`, `imap`) are rejected at the API level since they are meant for protocol host authentication only.
4. **Claims creation** -- builds a `ClaimsPrincipal` with:
   - `NameIdentifier` / `sub` -- the user's GUID
   - `Name` / `email` -- the user's email address
   - `scope` -- one claim per scope on the key
5. **Background update** -- queues a `LastUsedAt` timestamp update to avoid write contention on every request

### Audit Logging

Every successful API key authentication is logged with the user ID and client IP address:

```
API key auth: User={UserId}, IP={IP}
```

### LastUsedAt Background Updates

To avoid a database write on every authenticated request, `LastUsedAt` updates are queued to a background task processor via `IBackgroundTaskQueue`. This batches timestamp updates and prevents write contention when many requests arrive simultaneously.

## Scope-Based Authorization

The `RequireScopeAttribute` provides attribute-based scope enforcement on controller classes or individual endpoints:

```csharp
[RequireScope("api:read")]          // Requires api:read scope
[RequireScope("api:write")]         // Requires api:write scope
[RequireScope("internal")]          // Requires internal scope
[RequireScope("api:read", "app")]   // Requires either api:read OR app scope
```

The attribute implements `IAuthorizationFilter` and checks the authenticated user's `scope` claims. If the user does not have at least one of the required scopes, it returns a `403 Forbidden` response.

### API Key Scopes

| Scope | Purpose | Used By |
|-------|---------|---------|
| `smtp` | SMTP server authentication | SmtpHost |
| `pop3` | POP3 server authentication | Pop3Host |
| `imap` | IMAP server authentication | ImapHost |
| `api:read` | Read access to email data via API | Third-party integrations |
| `api:write` | Write access (mark read, delete) via API | Third-party integrations |
| `app` | First-party client access (mobile/desktop) | Mobile and desktop apps |
| `internal` | Service-to-service communication | Protocol hosts -> API notifications |

When creating an API key without specifying scopes, all scopes are granted by default. The mobile key creation endpoint always assigns only the `app` scope.

## User Provisioning

The `UserProvisioningService` handles just-in-time user creation and email linking. It is called on every authenticated API request to ensure the user exists in the database.

### Provisioning Flow

1. **Extract subject** -- reads `NameIdentifier` or `sub` claim from the authenticated principal
2. **API key path** -- if the subject is a valid GUID, looks up the user by ID directly (API key auth sets the subject to the user's database ID)
3. **OIDC path** -- looks up by OIDC issuer + subject combination
4. **Existing user found** -- updates `LastLoginAt` and links any unlinked emails (both primary and additional addresses) to the user
5. **New user** -- creates a `User` entity from OIDC claims (`email`, `name`), stores `OidcSubject` and `OidcIssuer`, then links existing emails

The email linking step is important: when the SMTP server receives mail for an address before the user has logged in, those emails exist in the database without a user link. On first login, `LinkEmailsToUserAsync` connects those emails to the newly provisioned user.

### Issuer Validation

When `Oidc:Authority` is configured, the service rejects tokens from unexpected issuers:

```csharp
if (!string.Equals(issuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
{
    _logger.LogWarning("Rejected token with unexpected issuer: {Issuer}", issuer);
    return null!;
}
```

## Configuration Reference

| Setting | Description | Required |
|---------|-------------|----------|
| `Oidc:Authority` | OIDC provider URL | No (dev mode if empty) |
| `Oidc:Audience` | Expected JWT audience | No |
| `Oidc:ClientId` | OIDC client ID (for frontend config) | No |
| `Oidc:RedirectUri` | OIDC redirect URI (for frontend config) | No |
| `Oidc:Scope` | OIDC scopes (default: `openid profile email`) | No |
| `Jwt:DevelopmentKey` | Symmetric key for dev mode JWT signing | No (random if empty) |
