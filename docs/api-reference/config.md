# Config

The Config endpoint provides runtime frontend configuration, allowing the web app to load OIDC and other settings without requiring a rebuild.

**Base path:** `/api/config`

## Get Frontend Configuration

```
GET /api/config
```

::: info No authentication required
This endpoint is called during app initialization, before the user has authenticated.
:::

**Response** `200 OK`:

```json
{
  "oidc": {
    "authority": "https://auth.example.com",
    "clientId": "relate-mail-web",
    "enabled": true
  }
}
```

**Field descriptions:**

| Field | Type | Description |
|---|---|---|
| `oidc.authority` | string | OIDC provider URL |
| `oidc.clientId` | string | Client ID for the web application |
| `oidc.enabled` | boolean | Whether OIDC authentication is configured |

When `oidc.enabled` is `false`, the web app runs in development mode without authentication.

**curl example:**

```bash
curl -s "http://localhost:8080/api/config" | jq
```

## Usage in Web App

The web app calls this endpoint at startup to configure the OIDC client:

```typescript
// config.ts
export async function loadConfig() {
  const response = await fetch('/api/config');
  const config = await response.json();

  if (config.oidc.enabled) {
    initializeOidc({
      authority: config.oidc.authority,
      clientId: config.oidc.clientId,
      redirectUri: window.location.origin + '/callback'
    });
  }
}
```

This pattern allows the same built frontend artifact to be deployed against different OIDC providers without rebuilding. The backend reads the OIDC settings from its own configuration (`Oidc__Authority`, `Oidc__ClientId` environment variables or `appsettings.json`) and exposes them to the frontend through this endpoint.
