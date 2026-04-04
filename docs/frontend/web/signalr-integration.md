# SignalR Integration

The web application uses SignalR to receive real-time push notifications from the backend. When new emails arrive, existing emails are updated, or delivery statuses change, the server pushes events to connected clients through a WebSocket connection. This eliminates the need for polling and provides instant UI updates.

::: info Screenshot
**[Screenshot placeholder: Real-time update]**

_TODO: Add screenshot showing a new email appearing in the inbox in real time_
:::

## Connection Architecture

### `SignalRConnection` Singleton (`src/api/signalr.ts`)

The SignalR client is implemented as a singleton class exported as `signalRConnection`. This ensures that only one WebSocket connection exists regardless of how many components subscribe to events.

```typescript
import { signalRConnection } from '@/api/signalr'
```

**Key design decisions:**

- **Singleton pattern** -- A single connection is shared across all routes and components. The connection is established once and reused.
- **Automatic reconnection** -- The connection is built with `.withAutomaticReconnect()`, which uses SignalR's default retry policy (0s, 2s, 10s, 30s delays) to handle temporary network interruptions.
- **Credentials included** -- `withCredentials: true` ensures cookies and auth headers are sent with the WebSocket handshake.
- **Connection deduplication** -- If `connect()` is called while a connection attempt is already in progress, it returns the existing promise rather than creating a duplicate connection.

### Connection Lifecycle

```
connect(apiUrl) ─── createConnection() ─── HubConnectionBuilder
                                              .withUrl('/hubs/email')
                                              .withAutomaticReconnect()
                                              .build()
                                              .start()
```

The connection transitions through these states:

1. **Disconnected** -- Initial state, or after `disconnect()` is called
2. **Connecting** -- `connect()` has been called, WebSocket handshake in progress
3. **Connected** -- Active and receiving events
4. **Reconnecting** -- Connection lost, automatic retry in progress
5. **Reconnected** -- Successfully restored after a temporary disconnection

When the connection closes (server shutdown, network failure beyond retry policy), the internal promise is cleared so the next `connect()` call starts fresh.

## Events

The server pushes five event types through the `/hubs/email` hub:

### `NewEmail`

Fired when a new email is received by the SMTP server and stored in the database.

```typescript
{
  id: string           // Email ID
  from: string         // Sender address
  fromDisplay?: string // Sender display name
  subject: string      // Email subject
  receivedAt: string   // ISO 8601 timestamp
  hasAttachments: boolean
}
```

### `EmailUpdated`

Fired when an email's metadata changes (e.g., marked as read/unread).

```typescript
{
  id: string       // Email ID
  isRead: boolean  // New read state
}
```

### `EmailDeleted`

Fired when an email is permanently deleted.

```typescript
emailId: string   // The ID of the deleted email
```

### `UnreadCountChanged`

Fired when the total unread email count changes. This can be triggered by new mail arriving, reading an email, or bulk operations.

```typescript
count: number     // New total unread count
```

### `DeliveryStatusChanged`

Fired when an outbound email's delivery status transitions (e.g., Queued to Sending, Sending to Sent).

```typescript
{
  id: string      // Outbound email ID
  status: string  // New status: 'Queued' | 'Sending' | 'Sent' | 'Failed' | 'PartialFailure'
}
```

## Event Subscription API

Each event has a corresponding subscription method on the `signalRConnection` singleton. Each method returns an unsubscribe function:

```typescript
const unsubscribe = signalRConnection.onNewEmail((email) => {
  console.log('New email from:', email.from)
})

// Later, to stop listening:
unsubscribe()
```

Available methods:

| Method | Event |
|--------|-------|
| `onNewEmail(handler)` | `NewEmail` |
| `onEmailUpdated(handler)` | `EmailUpdated` |
| `onEmailDeleted(handler)` | `EmailDeleted` |
| `onUnreadCountChanged(handler)` | `UnreadCountChanged` |
| `onDeliveryStatusChanged(handler)` | `DeliveryStatusChanged` |

If a handler is registered before the connection is established, a warning is logged and a no-op unsubscribe function is returned.

## Integration with the Inbox Route

The inbox route (`src/routes/index.tsx`) is the primary consumer of SignalR events. Here is how the integration works:

### On Mount

1. The route calls `signalRConnection.connect(apiUrl)` to establish (or reuse) the WebSocket connection
2. Event listeners are registered for all five event types
3. Each listener's unsubscribe function is stored for cleanup

### Event Handling

- **`NewEmail`** and **`EmailDeleted`** -- Invalidate the `['emails']` query key, causing TanStack Query to refetch the inbox list
- **`EmailUpdated`** -- Invalidate both the specific `['email', id]` and the `['emails']` list queries
- **`UnreadCountChanged`** -- Updates the unread count directly in local state (Jotai atom) for instant badge updates without waiting for a network round-trip
- **`DeliveryStatusChanged`** -- Invalidates the `['outbound']` query key to refresh the outbox view

### On Unmount

The route unsubscribes from all event handlers but does **not** disconnect the singleton. This means:

- Navigating away from the inbox stops processing events but keeps the WebSocket open
- Returning to the inbox re-registers handlers on the existing connection
- The connection is only torn down when the browser tab closes or the application unmounts entirely

This approach avoids the overhead of repeatedly establishing and tearing down WebSocket connections during normal navigation.
