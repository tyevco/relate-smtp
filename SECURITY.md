# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest | :x:               |

Only the most recent release receives security updates.

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Instead, report vulnerabilities through one of these channels:

1. **GitHub Private Vulnerability Reporting** — Use the "Report a vulnerability" button on the [Security tab](../../security/advisories/new) of this repository.
2. **Email** — Send details to **security@relatemail.dev**.

### What to Include

- Description of the vulnerability
- Steps to reproduce
- Affected component(s) and version(s)
- Potential impact assessment
- Any suggested fix (optional)

### Response Timeline

- **Acknowledgment** — Within 48 hours of report
- **Initial assessment** — Within 5 business days
- **Fix or mitigation** — Dependent on severity; critical issues are prioritized

We will coordinate disclosure timing with you and credit reporters in the release notes (unless you prefer to remain anonymous).

## Scope

The following components are in scope for security reports:

- SMTP, POP3, and IMAP servers
- REST API and authentication system (JWT/OIDC, API keys)
- Docker images and container configuration
- Web, desktop, and mobile clients

### Out of Scope

- Social engineering attacks against maintainers or users
- Denial-of-service attacks against infrastructure not owned by the project
- Vulnerabilities in upstream dependencies (report these to the upstream project)
- Issues requiring physical access to a user's device
