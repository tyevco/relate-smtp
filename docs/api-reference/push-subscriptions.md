# Push Subscriptions

The Push Subscriptions API manages Web Push notification subscriptions using the VAPID (Voluntary Application Server Identification) protocol. This enables the server to send push notifications to web browsers even when the app is not open.

**Base path:** `/api/push-subscriptions`

## Get VAPID Public Key

Retrieve the server's VAPID public key, needed to create a push subscription in the browser.

```
GET /api/push-subscriptions/vapid-public-key
```

::: info No authentication required
This endpoint is publicly accessible.
:::

**Response** `200 OK`:

```json
{
  "publicKey": "BNbx..."
}
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/push-subscriptions/vapid-public-key" | jq
```

**Browser usage:**

```javascript
// Fetch the VAPID public key
const res = await fetch('/api/push-subscriptions/vapid-public-key');
const { publicKey } = await res.json();

// Subscribe the browser
const registration = await navigator.serviceWorker.ready;
const subscription = await registration.pushManager.subscribe({
  userVisibleOnly: true,
  applicationServerKey: publicKey
});

// Send subscription to the server
const { endpoint, keys } = subscription.toJSON();
await fetch('/api/push-subscriptions', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    endpoint,
    p256dh: keys.p256dh,
    auth: keys.auth
  })
});
```

## Subscribe

Register a push subscription for the authenticated user's browser.

```
POST /api/push-subscriptions
```

**Request body:**

```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "p256dh": "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8p8REfXPk=",
  "auth": "tBHItJI5svbpC7rN2k6fTg=="
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `endpoint` | string | Yes | Push service URL provided by the browser |
| `p256dh` | string | Yes | Client public key for encryption |
| `auth` | string | Yes | Authentication secret for encryption |

**Response** `201 Created`

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/push-subscriptions" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "endpoint": "https://fcm.googleapis.com/fcm/send/...",
    "p256dh": "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry...",
    "auth": "tBHItJI5svbpC7rN2k6fTg=="
  }'
```

## Unsubscribe

Remove the push subscription for the authenticated user.

```
DELETE /api/push-subscriptions
```

**Response** `204 No Content`

**curl example:**

```bash
curl -s -X DELETE "http://localhost:8080/api/push-subscriptions" \
  -H "Authorization: Bearer YOUR_TOKEN"
```
