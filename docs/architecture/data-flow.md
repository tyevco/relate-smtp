# Data Flow

This page traces the path of email data through the Relate Mail system for each major scenario: receiving inbound mail from the internet, authenticated submission from a local client, sending outbound mail, accessing mail via IMAP/POP3, and real-time notifications.

## Inbound Email (MX Delivery)

When another mail server on the internet sends an email to a domain hosted by Relate Mail, the message flows through the SMTP MX endpoint on port 25.

```
Internet MTA
    │
    ▼
Port 25 (SMTP MX endpoint, unauthenticated)
    │
    ▼
MxMailboxFilter
    ├── Checks recipient domain against configured hosted domains
    ├── REJECT if domain is not hosted (prevents open relay)
    └── ACCEPT if domain matches
    │
    ▼
CustomMessageStore
    ├── Parses MIME message (headers, body, attachments)
    ├── Extracts From, To, Cc, Subject, Date, Message-Id
    ├── Stores parsed email in PostgreSQL
    └── Returns success to sending MTA
    │
    ▼
HTTP POST to API /api/internal-notifications
    ├── Authenticated with internal-scoped API key
    └── Triggers real-time notification pipeline (see below)
```

The `MxMailboxFilter` is the critical security component. It validates that every recipient address belongs to a domain the server is configured to host. This prevents the server from acting as an open relay, which would allow anyone on the internet to use it to send spam.

## Authenticated Submission

When a user sends email through the SMTP submission ports (587 or 465), the flow is similar but includes authentication:

```
Email client (Thunderbird, Apple Mail, etc.)
    │
    ▼
Port 587 (STARTTLS) or Port 465 (implicit TLS)
    │
    ▼
ProtocolAuthenticator
    ├── Validates API key (Bearer or plain AUTH)
    ├── BCrypt hash comparison with 30s in-memory cache
    ├── Checks key has 'smtp' scope
    └── REJECT if invalid
    │
    ▼
CustomMessageStore
    ├── Parses and stores email in PostgreSQL
    └── Associates email with authenticated user
    │
    ▼
HTTP POST to API /api/internal-notifications
    └── Triggers real-time notifications
```

## Outbound Email (Sending)

When a user composes and sends an email through the REST API, the message goes through a queued delivery pipeline with retry logic:

```
Client (web/mobile/desktop)
    │
    ▼
POST /api/outbound/send
    │
    ▼
OutboundEmailsController
    ├── Validates request (recipients, body)
    ├── Creates OutboundEmail entity with status: Queued
    ├── Creates OutboundRecipient for each To/Cc/Bcc address
    ├── Stores OutboundAttachment records if applicable
    ├── Sets RFC-compliant Message-Id header
    ├── Sets In-Reply-To and References headers for threading
    └── Persists to PostgreSQL
    │
    ▼
DeliveryQueueProcessor (background service)
    ├── Polls for OutboundEmail records with status: Queued
    ├── Groups recipients by domain
    └── For each domain:
         │
         ▼
    SmtpDeliveryService
         ├── MX record DNS lookup for recipient domain
         ├── Connects to remote MTA (highest-priority MX)
         ├── Delivers message via SMTP
         ├── On success: status → Sent
         └── On failure: see retry logic below
```

### Outbound Retry Logic

When delivery fails (remote server down, temporary rejection, network error), the system retries with exponential backoff:

```
Delivery attempt fails
    │
    ▼
Increment RetryCount on OutboundEmail
    │
    ▼
Calculate NextRetryAt with exponential backoff
    │
    ▼
Set status → Queued (eligible for re-polling)
    │
    ▼
Store LastError for diagnostics
    │
    ▼
DeliveryQueueProcessor picks it up again at NextRetryAt
    │
    ▼
After max retries exhausted: status → Failed
```

Per-recipient tracking means that if a message has recipients at multiple domains and one domain is unreachable, only the failed recipients are retried. Successfully delivered recipients are marked as `Sent` independently.

## Protocol Access (IMAP / POP3)

Standard email clients access the mailbox through IMAP or POP3. Both protocols follow the same authentication and data access pattern:

### IMAP Access

```
Email client
    │
    ▼
Port 143 (STARTTLS) or Port 993 (implicit TLS)
    │
    ▼
IMAP Command Parser
    ├── Parses IMAP4rev2 commands (LOGIN, SELECT, FETCH, SEARCH, etc.)
    └── Dispatches to appropriate handler
    │
    ▼
ProtocolAuthenticator (on LOGIN/AUTHENTICATE)
    ├── API key validation (BCrypt + cache)
    ├── Checks key has 'imap' scope
    └── Returns user identity
    │
    ▼
Repository Layer
    ├── IEmailRepository for mailbox operations
    ├── Translates IMAP operations to database queries
    └── Supports FETCH, STORE (flags), SEARCH, COPY, EXPUNGE
    │
    ▼
PostgreSQL
```

### POP3 Access

```
Email client
    │
    ▼
Port 110 (STLS upgrade) or Port 995 (implicit TLS)
    │
    ▼
POP3 Command Parser (RFC 1939)
    ├── Parses POP3 commands (USER, PASS, LIST, RETR, DELE, etc.)
    └── Dispatches to handler
    │
    ▼
ProtocolAuthenticator (on USER/PASS)
    ├── API key validation (BCrypt + cache)
    ├── Checks key has 'pop3' scope
    └── Returns user identity
    │
    ▼
Repository Layer
    ├── IEmailRepository for message retrieval
    ├── LIST returns message sizes
    ├── RETR returns full message content
    └── DELE marks messages for deletion (committed on QUIT)
    │
    ▼
PostgreSQL
```

## Real-Time Notifications

When a new email arrives (via SMTP MX or submission), the system pushes real-time updates to all connected clients through two channels:

```
SMTP Host receives new email
    │
    ▼
HTTP POST to /api/internal-notifications
    ├── Authenticated with 'internal' scope API key
    └── Payload: { mailboxId, emailId, from, subject }
    │
    ▼
API InternalNotificationsController
    │
    ├──────────────────────────────────┐
    ▼                                  ▼
SignalR Hub (/hubs/email)         Push Notification Service
    │                                  │
    ▼                                  ▼
WebSocket broadcast               VAPID web push
    │                                  │
    ▼                                  ▼
Web app + Desktop app             Mobile app (background)
(instant UI update)               (system notification)
```

### SignalR Connection Lifecycle

1. When the web or desktop app loads, it establishes a WebSocket connection to `/hubs/email`.
2. The hub authenticates the connection using the same JWT or API key mechanism as the REST API.
3. When a new email notification arrives, the hub broadcasts to all connections belonging to the affected mailbox.
4. The client receives the event and uses TanStack Query cache invalidation to refresh the inbox, making the new email appear without a full page reload.

### Push Notifications

For mobile clients that are not actively connected:

1. The mobile app registers a push subscription via `POST /api/push-subscriptions` with its VAPID endpoint.
2. When a notification event fires, the API sends a web push message to all registered subscriptions for the affected user.
3. The mobile OS delivers the notification, and tapping it opens the app to the relevant email.

## Draft and Outbox Flow

Composing an email involves several intermediate states:

```
User opens compose form
    │
    ▼
POST /api/outbound (creates draft)
    ├── OutboundEmail with status: Draft
    └── Returns draft ID
    │
    ▼
PUT /api/outbound/{id} (auto-save updates)
    ├── Updates recipients, subject, body
    └── Remains in Draft status
    │
    ▼
POST /api/outbound/{id}/send
    ├── Validates required fields
    ├── Status: Draft → Queued
    └── DeliveryQueueProcessor takes over (see outbound flow above)
```

Drafts are persisted server-side, so they are available across all clients. The outbox view shows all outbound emails with status `Queued` or `Sending`, and the sent mail view shows those with status `Sent`.
