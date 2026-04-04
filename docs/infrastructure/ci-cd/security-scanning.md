# Security Scanning

Relate Mail integrates multiple security scanning tools into its CI/CD pipelines to catch vulnerabilities, secrets, and supply chain risks early.

## CodeQL

**File:** `.github/workflows/codeql.yml`

CodeQL is GitHub's semantic code analysis engine. It builds a database of the codebase and runs queries to find security vulnerabilities, bugs, and code quality issues.

### Languages Analyzed

CodeQL runs as a matrix across two languages:

| Language | Build Mode | What It Covers |
|----------|-----------|----------------|
| `csharp` | Manual (requires build) | All .NET projects in `api/` |
| `javascript-typescript` | None (no build needed) | Web (`web/`), mobile (`mobile/`), desktop (`desktop/`), shared (`packages/shared/`) |

The C# analysis requires a manual build step because CodeQL needs to intercept the compilation process to understand the code. JavaScript/TypeScript analysis works without a build since CodeQL can parse the source directly.

### Schedule

| Trigger | When |
|---------|------|
| Push to `main` | Every push |
| Pull request | Every PR targeting `main` |
| Scheduled | Weekly on Monday at 06:00 UTC |
| Manual dispatch | On demand |

The weekly schedule ensures that newly disclosed vulnerabilities are detected even when no code changes are being made.

### Analysis Steps

For the C# target:

1. Checkout repository
2. Initialize CodeQL with `build-mode: manual`
3. Setup .NET 10 SDK
4. `dotnet restore api/` -- restore NuGet packages
5. `dotnet build api/ --no-restore` -- compile the solution
6. Perform CodeQL analysis and upload results

For JavaScript/TypeScript:

1. Checkout repository
2. Initialize CodeQL with `build-mode: none`
3. Perform CodeQL analysis and upload results

### Viewing Results

CodeQL findings appear in the repository's **Security > Code scanning alerts** tab. Each alert includes:

- A description of the vulnerability or issue
- The affected file and line number
- A severity rating (critical, high, medium, low, note)
- A link to the CodeQL query documentation explaining the issue
- Suggested remediation

Results are categorized by language (`/language:csharp` and `/language:javascript-typescript`).

### Permissions

The CodeQL job requires:

| Permission | Reason |
|------------|--------|
| `actions: read` | Read workflow data |
| `contents: read` | Read repository source |
| `security-events: write` | Upload SARIF results |

## OpenSSF Scorecard

**File:** `.github/workflows/scorecard.yml`

The [OpenSSF Scorecard](https://securityscorecards.dev/) evaluates the repository's security practices against a set of automated checks. It measures how well the project follows supply chain security best practices.

### What It Checks

Scorecard evaluates many dimensions, including:

| Check | What It Measures |
|-------|-----------------|
| Branch Protection | Are branch protection rules enforced on the default branch? |
| Code Review | Are changes reviewed before merging? |
| Dangerous Workflow | Does the CI use dangerous patterns (e.g., `pull_request_target`)? |
| Dependency Update Tool | Is Dependabot or Renovate configured? |
| License | Does the project have a recognized license? |
| Maintained | Is the project actively maintained (recent commits)? |
| Pinned Dependencies | Are CI action versions pinned by SHA? |
| Security Policy | Is there a SECURITY.md file? |
| Signed Releases | Are releases signed? |
| Token Permissions | Do workflows use least-privilege permissions? |
| Vulnerabilities | Are there known vulnerabilities in dependencies? |

### Schedule

| Trigger | When |
|---------|------|
| Push to `main` | Every push |
| Scheduled | Weekly on Monday at 06:00 UTC |
| Manual dispatch | On demand |

### Steps

1. Checkout code (without persisted credentials for security)
2. Run the Scorecard analysis with `ossf/scorecard-action`
3. Publish results (enables the Scorecard badge)
4. Upload SARIF results to the GitHub Security tab

### Viewing Results

- **Badge:** The `publish_results: true` setting enables a public Scorecard badge that can be embedded in documentation
- **Security tab:** Results appear in **Security > Code scanning alerts** alongside CodeQL findings
- **API:** Results are also available at `https://api.securityscorecards.dev/projects/github.com/four-robots/relate-mail`

### Permissions

| Permission | Reason |
|------------|--------|
| `security-events: write` | Upload SARIF results |
| `id-token: write` | Required by the Scorecard action for result publishing |

## TruffleHog -- Secret Scanning

**Location:** Integrated into `.github/workflows/ci.yml` (the `secret-scan` job)

TruffleHog scans the repository's Git history for accidentally committed secrets such as API keys, passwords, tokens, and private keys.

### Configuration

```yaml
- name: Run TruffleHog
  uses: trufflesecurity/trufflehog@6961f2bace57ab32b23b3ba40f8f420f6bc7e004
  with:
    extra_args: --only-verified
```

The `--only-verified` flag is critical: it means TruffleHog only reports secrets that are **confirmed active** by actually testing them against the relevant service (e.g., verifying an AWS key can authenticate). This dramatically reduces false positives compared to pattern-only scanning.

### When It Runs

The secret scan runs on **every CI trigger** (pushes to main, pull requests, manual dispatch) and is **independent of path filtering**. Even if only documentation changes, the secret scan still runs. This is intentional -- a secret could be committed in any file.

### Requirements

The checkout step uses `fetch-depth: 0` to fetch the complete Git history. This allows TruffleHog to scan all commits, not just the latest one. A secret that was committed and then removed in a later commit is still a secret -- it exists in the Git history and can be recovered.

### What to Do When a Secret Is Found

If TruffleHog detects a verified secret:

1. **Rotate the secret immediately** -- generate a new key/token/password and update it wherever it's used
2. **Remove the secret from Git history** -- use `git filter-branch` or [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/) to rewrite history
3. **Audit access logs** -- check whether the secret was used by unauthorized parties
4. **Add the file to `.gitignore`** -- prevent the same file from being committed again

## Trivy -- Container Image Scanning

**Location:** Integrated into `.github/workflows/docker-publish.yml`

Trivy scans each published Docker image for known vulnerabilities in OS packages and application dependencies.

### Configuration

- **Severity filter:** `CRITICAL,HIGH` -- only reports critical and high severity CVEs
- **Output format:** SARIF, uploaded to GitHub Security tab
- **Behavior:** `continue-on-error: true` -- findings do not block the image push

### Viewing Results

Trivy findings appear alongside CodeQL and Scorecard results in **Security > Code scanning alerts**, filtered by the "trivy" tool name. Each finding includes:

- The CVE identifier
- Affected package and version
- Fixed version (if available)
- Severity rating

## Security Scanning Summary

| Tool | Scope | Frequency | Output Location |
|------|-------|-----------|-----------------|
| CodeQL | C#, JS/TS source code | Every push/PR + weekly | Security > Code scanning |
| Scorecard | Repository practices | Every push to main + weekly | Security > Code scanning |
| TruffleHog | Git history (secrets) | Every CI run | CI job failure |
| Trivy | Docker images (CVEs) | Every Docker publish | Security > Code scanning |
