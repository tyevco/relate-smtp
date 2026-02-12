# Contributing to Relate Mail

Thank you for your interest in contributing to Relate Mail! This guide will help you get started.

## Prerequisites

- [Node.js 22](https://nodejs.org/) (LTS)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for database and integration tests)
- [Git](https://git-scm.com/)

## Getting Started

1. **Fork and clone** the repository:

   ```bash
   git clone https://github.com/<your-username>/relate-mail.git
   cd relate-mail
   ```

2. **Install dependencies** (npm workspaces):

   ```bash
   npm install
   npm run build:shared
   ```

3. **Build the backend**:

   ```bash
   cd api
   dotnet build
   ```

4. **Start services** (each in its own terminal):

   ```bash
   # Docker (PostgreSQL)
   cd docker && docker compose up -d

   # API
   cd api && dotnet run --project src/Relate.Smtp.Api

   # Web frontend
   npm run dev:web
   ```

See the [README](README.md) for full setup details and configuration options.

## Development Workflow

### Branch Naming

Use descriptive branch names with a type prefix:

- `feat/short-description` — New features
- `fix/short-description` — Bug fixes
- `docs/short-description` — Documentation changes
- `refactor/short-description` — Code refactoring
- `test/short-description` — Test additions or changes
- `chore/short-description` — Build, CI, or dependency updates

### Making Changes

1. Create a branch from `main`.
2. Make your changes, following the [Code Guidelines](CLAUDE.md).
3. Write or update tests as appropriate.
4. Ensure all checks pass before opening a PR.

### Commit Messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): short description

Optional longer description.
```

**Types:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`, `perf`

**Scopes:** `api`, `web`, `mobile`, `desktop`, `shared`, `docker`, `ci`, `deps`

### Pull Requests

- Keep PRs focused — one logical change per PR.
- Fill out the PR description with what changed and why.
- Link related issues (e.g., `Closes #123`).
- All CI checks must pass before merge.

## Testing Requirements

- **Unit tests must pass** for all changes (`dotnet test --filter "Category=Unit"` for backend, `npm run test:run` for frontend).
- **Protocol changes** (SMTP/POP3/IMAP) should include E2E tests.
- **Frontend changes** should include component or integration tests where applicable.
- Run linting before submitting (`npm run lint` in `web/`).

## Code Style

Refer to [`CLAUDE.md`](CLAUDE.md) for coding standards, naming conventions, and architectural patterns used in this project.

## Questions?

See [SUPPORT.md](SUPPORT.md) for ways to get help.
