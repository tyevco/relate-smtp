# Contributing to Relate Mail

Thank you for your interest in contributing to Relate Mail. This guide covers the workflow, conventions, and expectations for contributions.

## How to Contribute

### Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/relate-mail.git
   cd relate-mail
   ```
3. **Create a branch** from `main`:
   ```bash
   git checkout -b feat/your-feature-name
   ```
4. **Make your changes** -- see [Development Setup](./development-setup) for environment configuration
5. **Test your changes** -- run the relevant test suites
6. **Commit** using conventional commit messages
7. **Push** your branch and open a **Pull Request**

### Branch Naming

Use a prefix that describes the type of change:

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feat/` | New feature | `feat/imap-idle-support` |
| `fix/` | Bug fix | `fix/pop3-utf8-encoding` |
| `docs/` | Documentation only | `docs/api-key-guide` |
| `refactor/` | Code restructuring (no behavior change) | `refactor/smtp-session-cleanup` |
| `test/` | Adding or updating tests | `test/label-repository-coverage` |
| `chore/` | Build, CI, dependency updates | `chore/update-dotnet-sdk` |

## Commit Messages

Relate Mail uses [Conventional Commits](https://www.conventionalcommits.org/) for clear, machine-readable commit history.

### Format

```
type(scope): description

[optional body]

[optional footer]
```

### Types

| Type | When to Use |
|------|-------------|
| `feat` | A new feature or capability |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or correcting tests |
| `chore` | Build process, CI, tooling, or dependency changes |
| `ci` | CI configuration changes |
| `perf` | Performance improvement |

### Scopes

The scope identifies which part of the monorepo is affected:

| Scope | Area |
|-------|------|
| `api` | Backend API, controllers, services |
| `smtp` | SMTP server |
| `pop3` | POP3 server |
| `imap` | IMAP server |
| `web` | Web frontend |
| `mobile` | Mobile app |
| `desktop` | Desktop app |
| `shared` | Shared package (`@relate/shared`) |
| `docker` | Docker configuration |
| `ci` | CI/CD workflows |
| `deps` | Dependency updates |
| `db` | Database migrations |

### Examples

```
feat(api): add email attachment download endpoint

fix(smtp): handle AUTH PLAIN with embedded null bytes

docs(web): add keyboard shortcut reference

refactor(imap): extract FETCH response builder into separate class

test(pop3): add protocol compliance tests for UIDL command

chore(deps): bump dompurify from 3.3.1 to 3.3.2

ci: add Trivy container scanning to docker-publish workflow

fix(build): null-safe MimeKit 4.15.1 nullability for TextBody and MimePart.Content
```

## Pull Request Guidelines

### Requirements

- **Focused:** Each PR should address one concern. Do not mix a feature with an unrelated refactor.
- **CI must pass:** All relevant checks must pass before merging. The CI system automatically runs only the tests affected by your changes.
- **Describe what and why:** The PR description should explain what changed and why. The code diff shows *what* -- the description should add context about *why*.
- **Tests:** Include tests for new features and bug fixes. Protocol changes should include E2E tests that verify the full command sequence.

### PR Description Template

When opening a PR, include:

1. **Summary** -- What does this PR do? (1-3 sentences)
2. **Motivation** -- Why is this change needed?
3. **Testing** -- How was this tested? What test commands were run?
4. **Breaking changes** -- Does this change break any existing behavior, API contracts, or configuration?

### Review Process

1. Open a PR targeting `main`
2. CI runs automatically based on the paths you changed
3. A maintainer reviews the code
4. Address any feedback with additional commits (do not force-push during review)
5. Once approved and CI is green, the PR is merged

## What to Contribute

### Good First Contributions

- Improving documentation
- Adding test coverage for untested code paths
- Fixing typos and error messages
- Small bug fixes with clear reproduction steps

### Feature Contributions

For larger features, it is recommended to open an issue first to discuss the approach. This avoids spending time on work that may not align with the project direction.

Areas where contributions are welcome:

- Email protocol compliance improvements (SMTP, POP3, IMAP RFCs)
- Web UI improvements and accessibility
- Mobile app features
- Performance improvements with benchmarks
- Docker and deployment documentation
- Security improvements

## Testing Requirements

All changes must pass the existing test suites. Additionally:

| Change Type | Required Tests |
|-------------|---------------|
| API controller/service changes | Unit tests |
| Repository/database changes | Integration tests |
| Protocol server changes | Unit tests + E2E protocol tests |
| Web component changes | Vitest component tests |
| Mobile component changes | Jest component tests |
| Bug fixes | A regression test that would have caught the bug |

Run the relevant tests locally before pushing:

```bash
# Backend
cd api && dotnet test --filter "Category=Unit"

# Web
cd web && npm run test:run

# Mobile
cd mobile && npm test
```

## Code Style

### Backend (.NET)

- Follow the `.editorconfig` in the repository root
- `dotnet format` is enforced in CI -- run it locally to verify: `cd api && dotnet format --verify-no-changes`
- Use the existing patterns: repository interfaces in Core, implementations in Infrastructure

### Frontend (TypeScript)

- ESLint is enforced in CI for web, mobile, and desktop
- Run linting locally: `npm run lint -w web` (or `mobile`, `desktop`)
- Follow existing component patterns and naming conventions
- Use TypeScript strictly -- no `any` without justification

### Rust (Desktop)

- `cargo clippy -- -D warnings` is enforced in CI
- All warnings are treated as errors

## License

Relate Mail is released under the MIT License. By contributing, you agree that your contributions will be licensed under the same license.

## Code of Conduct

Be respectful, constructive, and inclusive. We are building software together -- disagreement on technical approaches is expected and healthy, but personal attacks are not tolerated.
