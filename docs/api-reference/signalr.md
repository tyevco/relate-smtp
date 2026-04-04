# SignalR Hub

Relate Mail uses a SignalR WebSocket hub for real-time email notifications. Connected clients receive instant updates when emails arrive, are read/deleted, or when outbound delivery status changes.

**Endpoint:** `/hubs/email`

## Connection

### Authentication

The hub requires a JWT token passed as a query string parameter:

```
wss://your-server/hubs/email?access_token={jwt_token}
```

### Client Setup (JavaScript)

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/email', {
    accessTokenFactory: () => getToken()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
console.log('Connected to email hub');
```

### Reconnection

The client is configured with automatic reconnect using exponential backoff. The `withAutomaticReconnect()` default strategy retries at 0, 2, 10, and 30 seconds. You can customize the intervals:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/email', {
    accessTokenFactory: () => getToken()
  })
  .withAutomaticReconnect([0, 1000, 5000, 10000, 30000])
  .build();

connection.onreconnecting((error) => {
  console.log('Reconnecting...', error);
});

connection.onreconnected((connectionId) => {
  console.log('Reconnected:', connectionId);
});

connection.onclose((error) => {
  console.log('Connection closed:', error);
});
```

## Events

### NewEmail

Fired when a new email is received in the user's inbox.

**Payload:**

```json
{
  "id": "email-uuid",
  "fromAddress": "sender@example.com",
  "fromDisplayName": "Jane Doe",
  "subject": "Meeting tomorrow",
  "previewText": "Hi, just wanted to confirm...",
  "receivedAt": "2026-04-03T14:30:00Z",
  "hasAttachments": false
}
```

```javascript
connection.on('NewEmail', (email) => {
  showNotification(`New email from ${email.fromDisplayName}`, email.subject);
  refreshInbox();
});
```

### EmailUpdated

Fired when an email's read/unread status changes.

**Payload:**

```json
{
  "emailId": "email-uuid",
  "isRead": true
}
```

```javascript
connection.on('EmailUpdated', ({ emailId, isRead }) => {
  updateEmailInList(emailId, { isRead });
});
```

### EmailDeleted

Fired when an email is deleted.

**Payload:**

```json
{
  "emailId": "email-uuid"
}
```

```javascript
connection.on('EmailDeleted', ({ emailId }) => {
  removeEmailFromList(emailId);
});
```

### UnreadCountChanged

Fired when the total unread email count changes.

**Payload:**

```json
{
  "count": 7
}
```

```javascript
connection.on('UnreadCountChanged', ({ count }) => {
  updateBadge(count);
});
```

### DeliveryStatusChanged

Fired when an outbound email's delivery status is updated.

**Payload:**

```json
{
  "outboundEmailId": "outbound-uuid",
  "status": "Sent"
}
```

Possible `status` values: `Queued`, `Sending`, `Sent`, `Failed`.

```javascript
connection.on('DeliveryStatusChanged', ({ outboundEmailId, status }) => {
  if (status === 'Sent') {
    showToast('Email delivered successfully');
  } else if (status === 'Failed') {
    showToast('Email delivery failed', 'error');
  }
  refreshOutbox();
});
```

## Groups

Each authenticated user's connections are placed in a SignalR group named `user_{userId}`. Events are broadcast to the group, so all of a user's connected devices (web, desktop) receive updates simultaneously.

## Complete Example

```javascript
import * as signalR from '@microsoft/signalr';

function createEmailHub(getToken) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/email', {
      accessTokenFactory: () => getToken()
    })
    .withAutomaticReconnect()
    .build();

  // Handle all events
  connection.on('NewEmail', (email) => {
    console.log('New email:', email.subject);
  });

  connection.on('EmailUpdated', ({ emailId, isRead }) => {
    console.log(`Email ${emailId} marked as ${isRead ? 'read' : 'unread'}`);
  });

  connection.on('EmailDeleted', ({ emailId }) => {
    console.log(`Email ${emailId} deleted`);
  });

  connection.on('UnreadCountChanged', ({ count }) => {
    console.log(`Unread count: ${count}`);
  });

  connection.on('DeliveryStatusChanged', ({ outboundEmailId, status }) => {
    console.log(`Outbound ${outboundEmailId}: ${status}`);
  });

  // Connection lifecycle
  connection.onreconnecting(() => console.log('Reconnecting...'));
  connection.onreconnected(() => console.log('Reconnected'));
  connection.onclose(() => console.log('Disconnected'));

  return {
    start: () => connection.start(),
    stop: () => connection.stop(),
    connection
  };
}

// Usage
const hub = createEmailHub(() => sessionStorage.getItem('access_token'));
await hub.start();
```
