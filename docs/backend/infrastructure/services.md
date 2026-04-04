# Infrastructure Services

The Infrastructure layer provides several background and utility services that handle outbound email delivery, DNS resolution, authentication rate limiting, and cross-service notifications.

## DeliveryQueueProcessor

`DeliveryQueueProcessor` is a `BackgroundService` that polls the database for queued outbound emails and delivers them.

### Operation

1. **Poll** -- Every `QueuePollingIntervalSeconds` (default: 15), query `IOutboundEmailRepository.GetQueuedForDeliveryAsync` for up to `MaxConcurrency` (default: 5) emails.
2. **Deliver** -- For each email, mark status as `Sending`, call `SmtpDeliveryService.DeliverAsync`, and process the results.
3. **Log** -- Create a `DeliveryLog` record for each recipient delivery attempt with MX host, SMTP status code, response text, success/failure, and duration.
4. **Update recipients** -- Set each `OutboundRecipient.Status` to `Sent` or `Failed` based on the delivery result.
5. **Update email status** -- Based on aggregate results:
   - All succeeded: status becomes `Sent`, `SentAt` is set
   - All failed: retry logic applies
   - Mixed: status becomes `PartialFailure`
6. **Notify** -- Send a real-time status update via `IDeliveryNotificationService` (SignalR).

### Retry Logic

When delivery fails, the processor applies exponential backoff:

- `RetryCount` is incremented
- `NextRetryAt` is set to `now + baseDelay * 2^(retryCount - 1)`
- The delay is capped at 1 hour
- After `MaxRetries` (default: 10) attempts, the email is permanently marked as `Failed`

### Configuration

Options are bound from the `OutboundMail` configuration section:

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `false` | Whether outbound delivery is active |
| `RelayHost` | `null` | Smarthost for relay delivery (if set, all mail routes through it) |
| `RelayPort` | `587` | Relay SMTP port |
| `RelayUsername` | `null` | Relay authentication username |
| `RelayPassword` | `null` | Relay authentication password |
| `RelayUseTls` | `true` | Whether to use STARTTLS with the relay |
| `MaxConcurrency` | `5` | Maximum concurrent delivery tasks |
| `MaxRetries` | `10` | Maximum retry attempts per email |
| `RetryBaseDelaySeconds` | `60` | Base delay for exponential backoff |
| `QueuePollingIntervalSeconds` | `15` | How often to check for queued emails |
| `SmtpTimeoutSeconds` | `30` | SMTP connection timeout |
| `SenderDomain` | `localhost` | Domain used for HELO/EHLO and Message-ID generation |

## SmtpDeliveryService

`SmtpDeliveryService` handles the actual SMTP delivery using MailKit. It supports two delivery modes:

### Relay Mode

When `RelayHost` is configured, all outbound email is sent through a single smarthost:

1. Connect to the relay host on the configured port with optional STARTTLS.
2. Authenticate if credentials are provided.
3. Send the message (all recipients in one SMTP transaction).
4. Return a `RecipientDeliveryResult` for each recipient.

This mode is typical for production deployments where you use a service like Amazon SES, Mailgun, or Postfix as a relay.

### Direct MX Delivery

When no relay is configured, the service delivers directly to each recipient's mail server:

1. Group recipients by domain (e.g., all `@gmail.com` recipients together).
2. For each domain, resolve MX records via `MxResolverService`.
3. Try each MX host in priority order until delivery succeeds.
4. Connect on port 25 with opportunistic STARTTLS.
5. Build a domain-specific copy of the message with only the recipients for that domain.
6. Return per-recipient results.

If all MX hosts for a domain fail, the recipients for that domain are marked as failed.

### Message Building

`BuildMimeMessage` constructs a `MimeMessage` from an `OutboundEmail` entity:

- Sets From, To, Cc, Bcc addresses
- Generates a Message-ID using the configured sender domain
- Populates In-Reply-To and References headers for threading
- Builds the body with text and HTML parts
- Attaches files

## MxResolverService

`MxResolverService` resolves MX (Mail Exchange) DNS records for a domain using [DnsClient.NET](https://dnsclient.michaco.net/).

### Resolution

`ResolveMxHostsAsync(string domain)`:

1. Queries for MX records using `ILookupClient`.
2. Orders results by preference (lowest preference value = highest priority).
3. Strips trailing dots from hostnames.
4. If no MX records are found, falls back to the domain itself per [RFC 5321 Section 5.1](https://datatracker.ietf.org/doc/html/rfc5321#section-5.1).
5. If DNS fails, returns the domain as a fallback.

### Utility

`GetDomainFromAddress(string emailAddress)` -- Extracts the domain from an email address by splitting at the `@` sign.

## AuthenticationRateLimiter

`AuthenticationRateLimiter` provides brute-force protection for protocol authentication (SMTP, POP3, IMAP). It is registered as a **singleton** so all sessions share the same rate limit state.

### Rate Limit Algorithm

The rate limiter uses in-memory caching (`MemoryCache`) with per-IP tracking:

1. **Check** (`CheckRateLimit`) -- Before authentication, check if the IP is blocked:
   - If `FailedAttempts >= MaxFailedAttempts` (default: 5) and within the lockout window (default: 15 minutes), block the request.
   - If the lockout has expired, reset the counter.
   - Otherwise, apply exponential backoff: `baseDelay * 2^(failures - 1)`, capped at `MaxBackoffDelay` (default: 30 seconds).

2. **Record failure** (`RecordFailure`) -- Increment the failure counter and update the last failure timestamp.

3. **Record success** (`RecordSuccess`) -- Remove the rate limit entry, allowing immediate future authentication.

### Cache Key Generation

`GenerateCacheKey(email, password)` produces an HMAC-SHA256 hash of the credentials using a server-instance salt. This allows caching authentication results (in the `ProtocolAuthenticator`) without storing plaintext credentials:

- In production, `Security:AuthenticationSalt` must be configured (base64-encoded random string).
- In development, a random key is generated per instance (with a warning log).

### Memory Limits

The rate limit cache has a size limit of 50,000 entries with a 1-minute expiration scan frequency. Entries expire after the lockout window.

## BackgroundTaskQueue

The `BackgroundTaskQueue` and `BackgroundTaskQueueHostedService` pair provides a mechanism for queueing non-critical work that should not block the request path.

### Current Use

The primary use case is updating `SmtpApiKey.LastUsedAt` timestamps. When an API key is used for authentication, the timestamp update is queued rather than executed inline, because:

- The BCrypt verification is already expensive
- The timestamp update is not critical to the authentication response
- Inline database writes would add latency to every protocol command

### Implementation

The queue uses a bounded `Channel<LastUsedAtUpdate>` with a capacity of 10,000 items:

- `QueueLastUsedAtUpdate` writes to the channel (non-blocking, drops oldest if full)
- `BackgroundTaskQueueHostedService` reads from the channel and processes updates
- On shutdown, the service drains remaining items to prevent data loss

## HttpEmailNotificationService

Used by the SMTP host to notify the API when new email arrives:

1. The SMTP server processes an incoming email and stores it.
2. It sends an HTTP POST to `/api/internal/notifications/new-email` with the user IDs and email summary.
3. The API's `InternalNotificationsController` receives the notification and broadcasts it via SignalR.

The HTTP client is configured with an `Internal:ApiKey` header for authentication, and the endpoint requires the `internal` scope.

This service handles failures gracefully -- notification failures are logged but never prevent email delivery.
