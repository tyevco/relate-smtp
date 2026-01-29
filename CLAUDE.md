# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Relate SMTP is a full-stack application that provides SMTP and POP3 email servers with a web-based management interface. The system consists of four main components:

1. **API** (.NET 10.0 ASP.NET Core) - REST API for managing emails and users
2. **SMTP Server** (.NET 10.0 Worker Service) - Custom SMTP server for receiving emails
3. **POP3 Server** (.NET 10.0 Worker Service) - Custom POP3 server for retrieving emails
4. **Web Frontend** (React + TypeScript + Vite) - Email management UI

All services share a database for email and user data persistence. Uses PostgreSQL database for production-grade performance and concurrency.

## Architecture

### Backend (.NET 10.0)

The backend follows Clean Architecture principles with three layers:

- **Relate.Smtp.Core** - Domain entities and interfaces (Entities/, Interfaces/)
  - Core business objects: `User`, `Email`, `EmailAttachment`, `EmailRecipient`, `UserEmailAddress`, `SmtpApiKey`
  - Repository interfaces: `IEmailRepository`, `IUserRepository`, `ISmtpApiKeyRepository`

- **Relate.Smtp.Infrastructure** - Data access and persistence
  - Entity Framework Core with PostgreSQL
  - Repository implementations in Repositories/
  - Database context in Data/AppDbContext.cs
  - Entity configurations in Data/Configurations/
  - BCrypt.Net-Next for password hashing

- **Relate.Smtp.Api** - Web API (port 8080 in containers, 5000 in dev)
  - Controllers: `EmailsController`, `ProfileController`, `SmtpCredentialsController`
  - Services: `UserProvisioningService`, `SmtpCredentialService`
  - JWT/OIDC authentication (optional, falls back to dev mode)
  - CORS configured for frontend origins

- **Relate.Smtp.SmtpHost** - SMTP server (ports 587, 465)
  - Hosted service running SmtpServer library
  - Custom handlers: `CustomMessageStore`, `CustomUserAuthenticator`
  - Authenticates users and stores incoming emails in database

- **Relate.Smtp.Pop3Host** - POP3 server (ports 110, 995)
  - Custom POP3 protocol implementation (RFC 1939)
  - TCP server with BackgroundService pattern
  - Custom handlers: `Pop3UserAuthenticator`, `Pop3CommandHandler`, `Pop3MessageManager`
  - Authenticates users with API keys and retrieves emails from database
  - Supports all standard POP3 commands (USER, PASS, STAT, LIST, RETR, DELE, UIDL, TOP, etc.)

### Frontend (React + TypeScript)

- **Routing**: TanStack Router with file-based routing (routes/)
  - Route tree auto-generated in routeTree.gen.ts (do not edit manually)

- **State Management**:
  - TanStack Query for server state
  - Jotai for client state
  - react-oidc-context for authentication (optional)

- **API Client**: Custom fetch wrapper in src/api/client.ts
  - Base URL from VITE_API_URL env var (defaults to '/api')
  - React hooks in src/api/hooks.ts

- **UI Components**:
  - Tailwind CSS with CVA for component variants
  - Lucide React icons
  - Custom components in src/components/

## Development Commands

### Backend (.NET)

```bash
# Navigate to api directory
cd api

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run API server (development)
cd src/Relate.Smtp.Api
dotnet run
# Listens on http://localhost:5000 by default

# Run SMTP server (development)
cd src/Relate.Smtp.SmtpHost
dotnet run

# Run POP3 server (development)
cd src/Relate.Smtp.Pop3Host
dotnet run

# Database migrations (from api/ directory)
# Database migrations
dotnet ef migrations add MigrationName --project src/Relate.Smtp.Infrastructure --startup-project src/Relate.Smtp.Api
dotnet ef database update --project src/Relate.Smtp.Infrastructure --startup-project src/Relate.Smtp.Api

# Note: Ensure your appsettings.json has the correct connection string before running migrations
```

### Frontend (Web)

```bash
# Navigate to web directory
cd web

# Install dependencies
npm install

# Run development server (Vite)
npm run dev
# Runs on http://localhost:5173

# Build for production
npm run build

# Preview production build
npm run preview

# Lint code
npm run lint
```

### Docker Compose

```bash
# Production build (from root directory)
docker compose -f docker/docker-compose.yml up

# Development with override
docker compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml up

# Services:
# - api: http://localhost:5000
# - smtp: ports 587, 465
# - pop3: ports 110, 995
# - web: http://localhost:3000
```

## Configuration

### Backend Environment Variables

- `ConnectionStrings__DefaultConnection` - PostgreSQL database connection string
  - Format: `host=localhost;port=5432;database=relate-smtp;user id=myuser;password=mypass`
- `Oidc__Authority` - OIDC provider URL (optional, enables JWT auth)
- `Oidc__Audience` - OIDC audience claim (optional)
- `Cors__AllowedOrigins__0` - CORS origins (can add multiple with __1, __2, etc.)
- `Smtp__ServerName` - SMTP server hostname
- `Smtp__Port` - SMTP listening port (default: 587)
- `Smtp__SecurePort` - SMTP secure port (default: 465)
- `Smtp__RequireAuthentication` - Whether SMTP requires auth
- `Pop3__ServerName` - POP3 server hostname
- `Pop3__Port` - POP3 listening port (default: 110)
- `Pop3__SecurePort` - POP3S secure port (default: 995)
- `Pop3__RequireAuthentication` - Whether POP3 requires auth
- `Pop3__SessionTimeout` - POP3 session timeout (default: 10 minutes)
- `Pop3__CertificatePath` - Path to SSL/TLS certificate for POP3S (optional)
- `Pop3__CertificatePassword` - Certificate password (optional)

### Frontend Environment Variables (.env)

- `VITE_API_URL` - Backend API URL (defaults to '/api')
- `VITE_OIDC_AUTHORITY` - OIDC provider URL (optional)
- `VITE_OIDC_CLIENT_ID` - OIDC client ID (optional)

## Key Patterns

### Authentication

Both frontend and backend support optional OIDC/JWT authentication. If `Oidc__Authority` (backend) or `VITE_OIDC_AUTHORITY` (frontend) is not configured, the system operates in development mode without authentication.

### Database Access

- All database operations go through repository interfaces
- EF Core handles PostgreSQL persistence
- In development, API auto-migrates database on startup (Program.cs:77-82)

### SMTP Server

- Built on SmtpServer library (11.1.0)
- **Per-User API Keys**: Users generate API keys via the web UI (SMTP Settings page)
- **Authentication**: Custom authenticator validates email + API key against BCrypt-hashed keys in database
- **Security**: API keys are hashed with BCrypt (work factor 11), shown only once during generation
- Custom message store parses MIME messages and persists to database
- Runs as a hosted service within a .NET Worker Service
- In-memory authentication cache (30s TTL) reduces database load
- **Email Linking**: Recipients are automatically linked to user accounts via:
  - **On arrival**: When emails arrive, recipients are immediately linked if matching users exist
  - **On login**: When users log in, any unlinked emails are retroactively linked
  - Checks both primary email and additional email addresses registered by users

### POP3 Server

- Custom implementation of RFC 1939 (POP3 protocol)
- **Authentication**: Reuses SMTP API keys - same email + API key credentials work for both protocols
- **Security**: BCrypt-hashed keys with 30-second authentication cache (same as SMTP)
- **Protocol Support**: All standard POP3 commands implemented:
  - Authorization: USER, PASS, QUIT
  - Transaction: STAT, LIST, RETR, DELE, NOOP, RSET, UIDL, TOP
  - Update: Applies deletions on QUIT
- **Message Retrieval**: Builds RFC 822 messages from database using MimeKit
- **Session Management**: 10-minute timeout, per-connection state tracking
- **SSL/TLS**: Supports POP3S on port 995 with certificate configuration
- Runs as a BackgroundService with custom TCP server
- Same API keys work for both SMTP (sending) and POP3 (retrieving)

### Frontend Routing

- TanStack Router generates type-safe routes
- Route files in src/routes/ define route components
- Run dev server to regenerate routeTree.gen.ts after route changes

## Database

Uses PostgreSQL for production-grade performance and concurrency.

Database stores:
- Users and their email addresses
- Received emails with recipients, attachments, and content
- SMTP API keys (BCrypt-hashed) for per-user authentication

**Connection String Format:**
- `host=localhost;port=5432;database=relate-smtp;user id=myuser;password=mypass`

## Email Client Setup

Users configure email clients to send and receive emails through the server:

1. Log into the web application
2. Navigate to "SMTP Settings" page
3. Generate an API key with a descriptive name (e.g., "Work Laptop", "iPhone")
4. Copy the generated API key (shown only once)
5. Configure email client:
   - **Outgoing Mail (SMTP)**:
     - Server: Value from connection info (e.g., localhost, smtp.example.com)
     - Port: 587 (STARTTLS) or 465 (SSL/TLS)
     - Username: User's email address
     - Password: Generated API key
   - **Incoming Mail (POP3)**:
     - Server: Value from connection info (e.g., localhost, pop3.example.com)
     - Port: 110 (plain) or 995 (SSL/TLS)
     - Username: User's email address
     - Password: Same generated API key

Users can create multiple API keys for different devices and revoke them individually via the web UI. The same API key works for both SMTP (sending) and POP3 (retrieving) protocols.
