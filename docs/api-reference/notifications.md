# Internal Notifications

The Internal Notifications endpoint is a service-to-service API used by the SMTP, POP3, and IMAP protocol hosts to notify the API server when new emails are received or mailbox state changes.

**Base path:** `/api/internal-notifications`

::: warning Not for external consumers
This endpoint is intended for internal use between Relate Mail services. It requires an API key with the `internal` scope, which is not typically granted to end-user keys.
:::

## Notify New Email

Notify the API that a new email has been received so it can trigger real-time updates and push notifications.

```
POST /api/internal-notifications
```

**Authentication:** API key with `internal` scope.

```
Authorization: ApiKey {internal_api_key}
```

**Request body:**

```json
{
  "type": "new_email",
  "userId": "user-uuid",
  "emailId": "email-uuid"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | string | Yes | Notification type (e.g., `new_email`) |
| `userId` | string | Yes | The user who received the email |
| `emailId` | string | Yes | The ID of the newly stored email |

**Response** `200 OK`

## Processing Pipeline

When the API receives an internal notification, it triggers the following actions:

1. **Filter evaluation** -- The new email is checked against the user's active [filters](./filters). Matching filters may mark the email as read, assign labels, or delete it.
2. **SignalR broadcast** -- A `NewEmail` event is sent to the user's connected [SignalR](./signalr) clients, along with an `UnreadCountChanged` event if the email is unread.
3. **Push notifications** -- If the user has [push subscriptions](./push-subscriptions) registered and desktop notifications enabled in [preferences](./preferences), a Web Push notification is sent.

## Architecture

In a typical deployment, the flow looks like this:

```
SMTP Server (port 25/587/465)
  │
  ├─ Receives email
  ├─ Stores in database
  └─ POST /api/internal-notifications ──► API Server
                                            │
                                            ├─ Evaluate filters
                                            ├─ SignalR → browser
                                            └─ Web Push → browser
```

The protocol hosts authenticate to the API using a shared internal API key configured at deployment time. This key should have only the `internal` scope.

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/internal-notifications" \
  -H "Authorization: ApiKey INTERNAL_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"type": "new_email", "userId": "USER_ID", "emailId": "EMAIL_ID"}'
```
