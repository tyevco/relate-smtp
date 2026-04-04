# Discovery

The Discovery endpoint advertises the server's capabilities and protocol connection details. It is used by mobile and desktop clients for automatic server configuration.

**Base path:** `/api/discovery`

## Get Server Capabilities

```
GET /api/discovery
```

::: info No authentication required
This endpoint is publicly accessible to allow client apps to discover server capabilities before the user authenticates.
:::

**Response** `200 OK`:

```json
{
  "smtp": {
    "enabled": true,
    "serverName": "smtp.example.com",
    "port": 587,
    "securePort": 465
  },
  "pop3": {
    "enabled": true,
    "serverName": "pop3.example.com",
    "port": 110,
    "securePort": 995
  },
  "imap": {
    "enabled": true,
    "serverName": "imap.example.com",
    "port": 143,
    "securePort": 993
  },
  "features": {
    "oidc": true,
    "pushNotifications": true
  }
}
```

**Field descriptions:**

| Field | Description |
|---|---|
| `smtp.enabled` | Whether SMTP submission is available |
| `smtp.serverName` | Hostname for SMTP connections |
| `smtp.port` | STARTTLS port (typically 587) |
| `smtp.securePort` | Implicit TLS port (typically 465) |
| `pop3.enabled` | Whether POP3 retrieval is available |
| `pop3.serverName` | Hostname for POP3 connections |
| `pop3.port` | STARTTLS port (typically 110) |
| `pop3.securePort` | Implicit TLS port (typically 995) |
| `imap.enabled` | Whether IMAP access is available |
| `imap.serverName` | Hostname for IMAP connections |
| `imap.port` | STARTTLS port (typically 143) |
| `imap.securePort` | Implicit TLS port (typically 993) |
| `features.oidc` | Whether OIDC authentication is configured |
| `features.pushNotifications` | Whether Web Push is available |

**curl example:**

```bash
curl -s "http://localhost:8080/api/discovery" | jq
```

## Usage in Client Apps

Mobile and desktop clients typically call this endpoint on first launch or when connecting to a new server:

```typescript
const response = await fetch(`${serverUrl}/api/discovery`);
const capabilities = await response.json();

if (capabilities.imap.enabled) {
  // Configure IMAP connection
  connectImap(capabilities.imap.serverName, capabilities.imap.securePort);
}

if (capabilities.features.oidc) {
  // Use OIDC login flow
  initiateOidcLogin();
} else {
  // Use API key authentication
  promptForApiKey();
}
```
