# SMTP Credentials

The SMTP Credentials API manages API keys and provides connection information for email protocols (SMTP, POP3, IMAP). API keys are used to authenticate with the protocol servers and the External Emails REST API.

**Base path:** `/api/smtp-credentials`

All endpoints require authentication. These endpoints have stricter rate limits to prevent abuse.

## Get Connection Info and Keys

Retrieve protocol connection details and a list of active API keys.

```
GET /api/smtp-credentials
```

**Response** `200 OK`:

```json
{
  "connectionInfo": {
    "smtp": {
      "serverName": "smtp.example.com",
      "port": 587,
      "securePort": 465,
      "enabled": true
    },
    "pop3": {
      "serverName": "pop3.example.com",
      "port": 110,
      "securePort": 995,
      "enabled": true
    },
    "imap": {
      "serverName": "imap.example.com",
      "port": 143,
      "securePort": 993,
      "enabled": true
    }
  },
  "keys": [
    {
      "id": "key-uuid",
      "name": "Thunderbird",
      "createdAt": "2026-03-01T10:00:00Z",
      "lastUsedAt": "2026-04-03T14:30:00Z",
      "isActive": true,
      "scopes": ["smtp", "pop3", "imap"]
    }
  ]
}
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/smtp-credentials" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Create API Key

Generate a new API key with specific scopes. The key value is returned only in this response and cannot be retrieved again.

```
POST /api/smtp-credentials
```

**Request body:**

```json
{
  "name": "My Email Client",
  "scopes": ["smtp", "pop3", "imap", "api:read", "api:write"]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | string | Yes | Human-readable name for the key |
| `scopes` | string[] | Yes | Permissions for this key |

**Available scopes:**

| Scope | Description |
|---|---|
| `smtp` | Send email via SMTP protocol |
| `pop3` | Retrieve email via POP3 protocol |
| `imap` | Access email via IMAP protocol |
| `api:read` | Read emails, labels, filters via REST API |
| `api:write` | Modify emails, labels, filters via REST API |
| `app` | Full application access (mobile/desktop clients) |
| `internal` | Service-to-service communication |

**Response** `201 Created`:

```json
{
  "id": "new-key-uuid",
  "name": "My Email Client",
  "createdAt": "2026-04-03T15:00:00Z",
  "lastUsedAt": null,
  "isActive": true,
  "scopes": ["smtp", "pop3", "imap", "api:read", "api:write"],
  "apiKey": "rm_a1b2c3d4e5f6g7h8i9j0..."
}
```

::: danger Save the API key
The `apiKey` field is only returned when the key is created. Store it securely -- you will not be able to view it again. If lost, revoke the key and create a new one.
:::

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/smtp-credentials" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "Thunderbird", "scopes": ["smtp", "pop3", "imap"]}' | jq
```

## Revoke API Key

Permanently revoke an API key. Any clients using this key will immediately lose access.

```
DELETE /api/smtp-credentials/{keyId}
```

**Response** `204 No Content`

**curl example:**

```bash
curl -s -X DELETE "http://localhost:8080/api/smtp-credentials/KEY_ID" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Rotate API Key

Create a new API key with the same name and scopes, and revoke the old one in a single atomic operation. Returns the new key.

```
POST /api/smtp-credentials/{keyId}/rotate
```

**Response** `200 OK`:

```json
{
  "id": "rotated-key-uuid",
  "name": "Thunderbird",
  "createdAt": "2026-04-03T16:00:00Z",
  "lastUsedAt": null,
  "isActive": true,
  "scopes": ["smtp", "pop3", "imap"],
  "apiKey": "rm_x9y8z7w6v5u4t3s2r1q0..."
}
```

::: tip
Rotation is useful for periodic key cycling without downtime. Update your client configuration with the new key immediately after rotation.
:::

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/smtp-credentials/KEY_ID/rotate" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Create Mobile Key

Create an API key pre-configured for mobile app use, including platform-specific metadata.

```
POST /api/smtp-credentials/mobile
```

**Request body:**

```json
{
  "name": "iPhone 15",
  "platform": "ios",
  "deviceId": "device-identifier"
}
```

**Response** `201 Created`: Same shape as [Create API Key](#create-api-key), with scopes appropriate for mobile access.
