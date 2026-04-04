# Docker Publish Workflow

**File:** `.github/workflows/docker-publish.yml`

This workflow builds multi-platform Docker images and publishes them to the GitHub Container Registry (GHCR). It also scans each image for security vulnerabilities before publishing results to the GitHub Security tab.

## Triggers

| Event | Condition |
|-------|-----------|
| Push to `main` | When `api/**`, `web/**`, `docker/**`, or the workflow file itself changes |
| Version tag | Tags matching `v*` (e.g., `v1.0.0`, `v2.1.3`) |
| Manual dispatch | `workflow_dispatch` |

The path filter ensures Docker images are only rebuilt when backend, frontend, or Docker configuration files change. Documentation-only changes do not trigger a rebuild.

## Build Matrix

All four service images are built in parallel using a strategy matrix:

| Name | Dockerfile Target | Published Image |
|------|-------------------|-----------------|
| `api` | `api` | `ghcr.io/four-robots/relate-mail-api` |
| `smtp` | `smtp` | `ghcr.io/four-robots/relate-mail-smtp` |
| `pop3` | `pop3` | `ghcr.io/four-robots/relate-mail-pop3` |
| `imap` | `imap` | `ghcr.io/four-robots/relate-mail-imap` |

All four images are built from the same Dockerfile (`api/Dockerfile`) with the repository root as the build context.

## Platforms

Each image is built for two architectures using QEMU emulation and Docker Buildx:

- `linux/amd64` -- x86-64 servers and desktops
- `linux/arm64` -- ARM servers (AWS Graviton, Ampere), Apple Silicon, Raspberry Pi 4+

The `docker/setup-qemu-action` enables cross-platform builds on the x86-based GitHub Actions runners.

## Build Steps

For each matrix entry, the workflow executes:

1. **Checkout** -- Clone the repository
2. **Setup QEMU** -- Enable cross-architecture emulation for ARM builds
3. **Setup Buildx** -- Configure Docker Buildx for multi-platform builds
4. **Login to GHCR** -- Authenticate using `GITHUB_TOKEN` (automatic, no secrets needed)
5. **Extract metadata** -- Generate image tags and OCI labels using `docker/metadata-action`
6. **Build and push** -- Build the Docker image for both platforms and push to GHCR
7. **Scan for vulnerabilities** -- Run Trivy against the published image
8. **Upload SARIF** -- Publish vulnerability scan results to GitHub Security

## Tagging Strategy

The `docker/metadata-action` generates tags based on the Git ref that triggered the build:

### Push to `main`

| Tag | Example | Description |
|-----|---------|-------------|
| `main` | `ghcr.io/four-robots/relate-mail-api:main` | Branch name |
| `main-<sha>` | `ghcr.io/four-robots/relate-mail-api:main-a1b2c3d` | Branch + short SHA |
| `latest` | `ghcr.io/four-robots/relate-mail-api:latest` | Convenience tag (main only) |

### Version Tag (e.g., `v1.2.3`)

| Tag | Example | Description |
|-----|---------|-------------|
| `1.2.3` | `ghcr.io/four-robots/relate-mail-api:1.2.3` | Exact version |
| `1.2` | `ghcr.io/four-robots/relate-mail-api:1.2` | Minor version (tracks latest patch) |
| `1` | `ghcr.io/four-robots/relate-mail-api:1` | Major version (tracks latest minor) |

This follows standard Docker tagging conventions. Users can pin to an exact version (`1.2.3`) for reproducibility, or use a less specific tag (`1.2` or `1`) to automatically receive compatible updates.

## Security Scanning

After each image is built and pushed, Trivy scans it for known vulnerabilities:

```yaml
- name: Scan image for vulnerabilities
  uses: aquasecurity/trivy-action@915b19bbe73b92a6cf82a1bc12b087c9a19a5fe2
  with:
    image-ref: ghcr.io/four-robots/relate-mail-<service>:main
    format: 'sarif'
    output: 'trivy-results.sarif'
    severity: 'CRITICAL,HIGH'
```

Key details:
- Only **CRITICAL** and **HIGH** severity vulnerabilities are reported -- medium and low are filtered out to reduce noise
- Results are output in SARIF format and uploaded to the GitHub Security tab via `codeql-action/upload-sarif`
- The scan step uses `continue-on-error: true` so that a vulnerability finding does not block the image push -- findings appear as security alerts for review rather than build failures

Vulnerability findings can be viewed in the repository's **Security > Code scanning alerts** tab.

## Permissions

The job requires elevated permissions beyond the workflow default:

| Permission | Reason |
|------------|--------|
| `contents: read` | Read repository source |
| `packages: write` | Push images to GHCR |
| `id-token: write` | Required by GHCR authentication |
| `security-events: write` | Upload Trivy SARIF results |

## Build Caching

Docker layer caching uses GitHub Actions cache (`type=gha`):

```yaml
cache-from: type=gha
cache-to: type=gha,mode=max
```

This caches intermediate build layers between workflow runs. The `mode=max` setting caches all layers (not just the final image layers), which improves cache hit rates for the multi-stage build. Shared layers between the four targets (the `node-build` and `build` stages) are cached once and reused.

## Build Summary

A separate `build-summary` job runs after all matrix builds complete (regardless of success or failure) and generates a GitHub Actions step summary listing all published image names.
