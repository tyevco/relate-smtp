# Dockerfile Reference

All four Relate Mail Docker images are built from a single multi-stage Dockerfile located at `api/Dockerfile`. This approach ensures consistency across services and keeps the build efficient by sharing intermediate layers.

## Build Stages

The Dockerfile defines six stages: two build stages and four runtime stages.

```
┌──────────────┐     ┌──────────────┐
│  node-build  │────>│    build     │
│  (Node 22)   │     │  (.NET 10)   │
└──────────────┘     └──────┬───────┘
                            │
              ┌─────────┬───┴───┬─────────┐
              │         │       │         │
         ┌────┴──┐ ┌───┴──┐ ┌──┴───┐ ┌───┴──┐
         │  api  │ │ smtp │ │ pop3 │ │ imap │
         └───────┘ └──────┘ └──────┘ └──────┘
         (runtime)  (runtime) (runtime) (runtime)
```

### Stage 1: `node-build` (Web Frontend)

**Base image:** `node:22-bookworm-slim`

This stage builds the React web frontend that gets embedded into the API image.

1. Copies `web/`, `packages/`, `package.json`, and `package-lock.json` into the build context
2. Runs `npm ci` to install dependencies from the lockfile
3. Runs `npm run build:web` to produce the optimized Vite output in `web/dist/`

The resulting `web/dist/` directory contains the static assets (HTML, JS, CSS) that the API serves.

### Stage 2: `build` (.NET Backend)

**Base image:** `mcr.microsoft.com/dotnet/sdk:10.0`

This stage compiles all four .NET applications:

1. **NuGet restore** -- Copies only `.csproj` files first (for Docker layer caching), then restores NuGet packages for all six projects:
   - `Relate.Smtp.Core`
   - `Relate.Smtp.Infrastructure`
   - `Relate.Smtp.Api`
   - `Relate.Smtp.SmtpHost`
   - `Relate.Smtp.Pop3Host`
   - `Relate.Smtp.ImapHost`

2. **Copy sources** -- Copies the full `api/` directory and the pre-built web frontend from `node-build`

3. **Publish** -- Builds and publishes each application to a separate output directory:
   ```
   /app/api   -- API server
   /app/smtp  -- SMTP server
   /app/pop3  -- POP3 server
   /app/imap  -- IMAP server
   ```

The `.csproj` build logic detects that `web/dist/index.html` already exists (from the `node-build` stage) and skips the npm build step, avoiding a redundant frontend compilation.

### Stage 3: `api` (Runtime)

**Base image:** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

The API runtime image serves both the REST API and the embedded web frontend.

| Property | Value |
|----------|-------|
| Exposed port | 8080 |
| Environment | `ASPNETCORE_URLS=http://+:8080` |
| Entrypoint | `dotnet Relate.Smtp.Api.dll` |
| User | `app` (UID 1654, non-root) |

**OCI Labels:**
- `org.opencontainers.image.title`: Relate Mail API
- `org.opencontainers.image.description`: REST API for Relate Mail email management
- `org.opencontainers.image.vendor`: Relate Mail
- `org.opencontainers.image.source`: https://github.com/four-robots/relate-mail

### Stage 4: `smtp` (Runtime)

**Base image:** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

| Property | Value |
|----------|-------|
| Exposed ports | 25, 587, 465, 8081 |
| Health check URL | `http://+:8081` (via `HealthCheck__Url` env var) |
| Entrypoint | `dotnet Relate.Smtp.SmtpHost.dll` |
| User | `app` (UID 1654, non-root) |

Port 25 is the MX endpoint for accepting inbound internet mail. It is only active when `Smtp__Mx__Enabled=true`. Ports 587 (STARTTLS) and 465 (implicit TLS) are for authenticated email submission.

### Stage 5: `pop3` (Runtime)

**Base image:** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

| Property | Value |
|----------|-------|
| Exposed ports | 110, 995, 8082 |
| Health check URL | `http://+:8082` |
| Entrypoint | `dotnet Relate.Smtp.Pop3Host.dll` |
| User | `app` (UID 1654, non-root) |

### Stage 6: `imap` (Runtime)

**Base image:** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

| Property | Value |
|----------|-------|
| Exposed ports | 143, 993, 8083 |
| Health check URL | `http://+:8083` |
| Entrypoint | `dotnet Relate.Smtp.ImapHost.dll` |
| User | `app` (UID 1654, non-root) |

## Building Specific Targets

Docker's `--target` flag lets you build a single service image without building the others:

```bash
# Build only the SMTP server image
docker build --target smtp -t relate-mail-smtp -f api/Dockerfile .

# Build only the API image
docker build --target api -t relate-mail-api -f api/Dockerfile .

# Build only the POP3 server image
docker build --target pop3 -t relate-mail-pop3 -f api/Dockerfile .

# Build only the IMAP server image
docker build --target imap -t relate-mail-imap -f api/Dockerfile .
```

Note that the build context must be the repository root (`.`), not the `api/` directory, because the Dockerfile needs access to `web/`, `packages/`, and `package.json` for the frontend build.

## Security Considerations

All runtime images follow security best practices:

- **Non-root execution** -- Each container runs as the built-in `app` user (UID 1654), which is provided by the .NET 8+ base images. No processes run as root.
- **Alpine-based** -- Runtime images use the Alpine variant of the ASP.NET base image, resulting in a smaller attack surface and image size.
- **Pinned digests** -- Base images are pinned by SHA256 digest rather than mutable tags, ensuring reproducible builds.
- **Minimal layers** -- Each runtime stage copies only the published application output, not the SDK or build tools.

## Layer Caching

The Dockerfile is structured to maximize Docker layer caching:

1. `.csproj` files are copied and restored before the full source, so NuGet restore is cached unless project references change
2. The `node-build` stage is independent from the .NET build, so frontend and backend builds can run in parallel with BuildKit
3. `BUILDKIT_INLINE_CACHE=1` is set in the compose files to enable cache metadata for subsequent builds
