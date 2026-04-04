# SignalR Hub

The `EmailHub` provides real-time email notifications to connected web and desktop clients. It is hosted at `/hubs/email` and requires authentication.

## Connection Management

When a client connects to the hub, the server extracts the user ID from the authenticated principal's claims and adds the connection to a user-specific group:

```
user_{userId}
```

This group-based approach means a single user can have multiple active connections (e.g., open in two browser tabs) and all connections receive the same notifications. When a connection is closed, it is removed from the group.

The hub reads the user ID from either the `sub` claim (OIDC standard) or the `NameIdentifier` claim (ASP.NET default), making it compatible with both JWT and API key authentication.

If a connection is established without a user ID in the claims, the hub logs a warning but does not reject the connection -- the client simply will not receive any user-specific notifications.

## Events

The following events are pushed from the server to connected clients:

### `NewEmail`

Sent when a new email is received for the user.

```json
{
  "id": "guid",
  "from": "sender@example.com",
  "fromDisplay": "Sender Name",
  "subject": "Email subject",
  "receivedAt": "2026-01-01T00:00:00Z",
  "hasAttachments": false
}
```

### `EmailUpdated`

Sent when an email's read status changes (e.g., user marks an email as read in another tab or via API).

```json
{
  "id": "guid",
  "isRead": true
}
```

### `EmailDeleted`

Sent when an email is deleted. The payload is the email's GUID.

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### `UnreadCountChanged`

Sent after any operation that changes the unread count (mark read/unread, delete). The payload is the new total unread count.

```json
42
```

### `DeliveryStatusChanged`

Sent when an outbound email's delivery status changes (Queued -> Sending -> Sent / Failed).

```json
{
  "id": "guid",
  "status": "Sent"
}
```

## Notification Services

Two services bridge domain events to the SignalR hub:

### SignalREmailNotificationService

Implements `IEmailNotificationService` and handles inbound email events. It wraps `IHubContext<EmailHub>` and sends typed events to user groups:

- `NotifyNewEmailAsync` -- sends `NewEmail` event + triggers web push notification
- `NotifyEmailUpdatedAsync` -- sends `EmailUpdated` event
- `NotifyEmailDeletedAsync` -- sends `EmailDeleted` event
- `NotifyUnreadCountChangedAsync` -- sends `UnreadCountChanged` event
- `NotifyMultipleUsersNewEmailAsync` -- sends `NewEmail` to multiple user groups in parallel, also triggers push notifications for each user

The new email notification is special because it triggers both a SignalR event and a web push notification (via `PushNotificationService`). This ensures users receive notifications even when the tab is in the background.

### SignalRDeliveryNotificationService

Implements `IDeliveryNotificationService` and handles outbound delivery status changes:

- `NotifyDeliveryStatusChangedAsync` -- sends `DeliveryStatusChanged` event to the sending user's group

This is called by the background delivery queue processor when an outbound email transitions between states.

## Protocol Host Integration

The SMTP, POP3, and IMAP hosts run as separate processes and do not have direct access to the SignalR hub. Instead, they use HTTP to trigger notifications:

```
Protocol Host                          API
    |                                   |
    |  POST /api/internal/notifications |
    |  Authorization: ApiKey <key>      |
    |  { userIds, email }               |
    |---------------------------------->|
    |                                   |-- SignalR broadcast
    |                                   |-- Web push notification
    |            200 OK                 |
    |<----------------------------------|
```

The `HttpEmailNotificationService` in each protocol host makes an HTTP POST to the API's internal notification endpoint. This requires an API key with the `internal` scope, configured via:

```
Internal__ApiKey=<api-key-with-internal-scope>
Api__BaseUrl=http://localhost:5000
```

If the notification fails (network error, API down), the failure is logged but does not affect email delivery -- notifications are best-effort.

## Client Integration

### Web / Desktop

Both the web and desktop clients use the `@microsoft/signalr` package to connect to the hub:

```typescript
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('/hubs/email', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .build();

connection.on('NewEmail', (email) => {
  // Update inbox, show notification
});

connection.on('UnreadCountChanged', (count) => {
  // Update badge/counter
});

connection.on('DeliveryStatusChanged', (status) => {
  // Update outbox item status
});

await connection.start();
```

The `withAutomaticReconnect()` configuration handles transient disconnections gracefully, automatically re-establishing the connection and re-joining the user's group.

### Authentication

The SignalR connection authenticates using the same token as the REST API:
- **OIDC/JWT** -- the access token is passed via the `accessTokenFactory` callback
- **API key** -- can also be used, though this is primarily for protocol host connections

::: info Screenshot
![Screenshot: Real-time notification flow](./screenshots/signalr-notification-flow.png)

_TODO: Add screenshot of the browser DevTools showing SignalR WebSocket messages_
:::
