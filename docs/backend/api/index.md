# API Project Overview

The `Relate.Smtp.Api` project is the central REST API for Relate Mail. It serves as the primary interface for web, mobile, and desktop clients, and also hosts the bundled web frontend as a single-page application.

## Responsibilities

- **REST endpoints** for email management, composition, labels, filters, preferences, and credentials
- **SPA hosting** -- serves the bundled React web frontend with fallback to `index.html` for client-side routing
- **SignalR hub** at `/hubs/email` for real-time email notifications
- **User provisioning** -- JIT (just-in-time) user creation from OIDC claims on first login
- **Dual authentication** -- OIDC/JWT for first-party clients and API key auth for third-party integrations and protocol hosts

## Startup Pipeline

The `Program.cs` startup configures the ASP.NET Core middleware pipeline. The order of middleware registration matters because each component processes requests in sequence.

### Service Registration

1. **Infrastructure** -- `AddInfrastructure(connectionString)` registers the EF Core DbContext, all repository implementations, shared services (background task queue, authentication rate limiter, delivery queue processor), and database health checks
2. **Health checks** -- API-specific checks for SignalR and the delivery queue
3. **Application services** -- `UserProvisioningService`, `SmtpCredentialService`, `EmailFilterService`, `SignalREmailNotificationService`, `PushNotificationService`
4. **Authentication** -- OIDC/JWT Bearer + API key authentication (dual scheme)
5. **Authorization** -- default policy accepts either auth scheme
6. **CORS** -- configured with allowed origins, specific headers, and credentials support
7. **SignalR** -- for the real-time notification hub
8. **Rate limiting** -- three policies (see below)
9. **OpenTelemetry** -- tracing and metrics for ASP.NET Core and HttpClient

### Middleware Pipeline

After service registration, middleware is added in this order:

1. **Auto-migration** (development only) -- runs pending EF Core migrations on startup
2. **OpenAPI** (development only) -- serves the OpenAPI specification
3. **Exception handler** -- maps `UnauthorizedAccessException` to 401, all others to 500 with generic message
4. **HTTPS redirection**
5. **Security headers** -- custom middleware that adds protective headers to every response
6. **Static files + default files** -- serves the bundled web frontend
7. **CORS**
8. **Authentication**
9. **Authorization**
10. **Rate limiter**
11. **Controllers** -- maps API routes
12. **SignalR hub** -- maps `/hubs/email`
13. **Health checks** -- maps `/healthz` (unauthenticated)
14. **SPA fallback** -- `MapFallbackToFile("index.html")` for client-side routing

## Rate Limiting

Three rate limiting policies protect the API from abuse:

| Policy | Type | Limit | Window | Queue | Applied To |
|--------|------|-------|--------|-------|------------|
| `api` | Fixed window | 100 requests | 1 minute | 10 | General API endpoints |
| `auth` | Fixed window | 10 requests | 1 minute | 2 | Authentication endpoints (SmtpCredentials) |
| `write` | Sliding window | 30 requests | 1 minute (6 segments) | 5 | Mutating operations (create, update, delete) |

When a rate limit is exceeded, the API returns HTTP `429 Too Many Requests`.

Controllers opt into rate limiting with attributes:

```csharp
[EnableRateLimiting("api")]    // Applied at controller level
[EnableRateLimiting("write")]  // Applied on specific write endpoints
[EnableRateLimiting("auth")]   // Applied on credential management
```

## Security Headers

A custom middleware adds the following headers to every response:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing |
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `X-XSS-Protection` | `1; mode=block` | Legacy XSS protection |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Controls referrer information |

For non-API routes (i.e., the SPA), an additional `Content-Security-Policy` header is set:

```
default-src 'self';
script-src 'self' 'unsafe-inline';
style-src 'self' 'unsafe-inline';
img-src 'self' data: blob:;
font-src 'self';
connect-src 'self' wss: ws:;
frame-ancestors 'none';
```

This CSP allows the web frontend to function (including WebSocket connections for SignalR) while restricting external resource loading.

## Auto-Migration

When running in development mode (`ASPNETCORE_ENVIRONMENT=Development`), the API automatically applies pending EF Core migrations on startup:

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

This eliminates the need to manually run `dotnet ef database update` during development. In production, migrations should be applied deliberately as part of the deployment process.

## Network Configuration

The API listens on port **5000** (HTTP) by default. In the Docker deployment, it sits behind a reverse proxy that handles TLS termination.

The Vite development server proxies `/api` requests to `http://localhost:5000`, so during development the web frontend and API appear to share the same origin.

::: info Screenshot
**[Screenshot placeholder: API health check response]**

_TODO: Add screenshot of the /healthz endpoint JSON response_
:::
