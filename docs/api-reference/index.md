# API Reference

Relate Mail exposes a RESTful JSON API for managing emails, labels, filters, user profiles, preferences, and SMTP credentials.

## Base URL

| Environment | Base URL |
|---|---|
| Docker (default) | `http://localhost:8080/api` |
| Local development | `http://localhost:5000/api` |

All endpoints described in this reference are relative to the base URL. For example, `GET /api/emails` means `http://localhost:8080/api/emails` in Docker.

## Authentication

The API supports two authentication methods. Every request to a protected endpoint must include one of these.

### OIDC / JWT (first-party)

Used by the web and mobile apps when OIDC is configured. Pass the JWT token in the `Authorization` header:

```
Authorization: Bearer {jwt_token}
```

### API Key (third-party)

Used by external integrations, desktop apps, mobile apps, and protocol clients (SMTP, POP3, IMAP). Either header format is accepted:

```
Authorization: Bearer {api_key}
Authorization: ApiKey {api_key}
```

API keys are created via the [SMTP Credentials](./smtp-credentials) endpoint. Each key has one or more **scopes** that control what it can access:

| Scope | Description |
|---|---|
| `smtp` | Send email via SMTP |
| `pop3` | Retrieve email via POP3 |
| `imap` | Access email via IMAP |
| `api:read` | Read inbox, labels, filters via REST API |
| `api:write` | Modify emails, labels, filters via REST API |
| `app` | Full app access (mobile/desktop) |
| `internal` | Service-to-service communication |

## Common Response Patterns

### Paginated Lists

Endpoints that return collections use consistent pagination:

```json
{
  "items": [],
  "totalCount": 142,
  "unreadCount": 7,
  "page": 1,
  "pageSize": 20
}
```

Query parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | integer | `1` | Page number (1-based) |
| `pageSize` | integer | `20` | Items per page |

### Success Responses

| Status | Meaning |
|---|---|
| `200 OK` | Request succeeded, response body included |
| `201 Created` | Resource created, response body included |
| `204 No Content` | Action succeeded, no response body (deletes, updates) |

### Error Responses

| Status | Meaning |
|---|---|
| `400 Bad Request` | Validation error. Body contains error details. |
| `401 Unauthorized` | Missing or invalid authentication. |
| `403 Forbidden` | Authenticated but insufficient permissions/scopes. |
| `404 Not Found` | Resource does not exist. |
| `429 Too Many Requests` | Rate limit exceeded. |

Validation errors follow this shape:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "fieldName": ["Error message"]
  }
}
```

## Rate Limiting

| Category | Limit |
|---|---|
| Global | 100 requests/minute |
| Authentication endpoints | 10 requests/minute |
| Write operations | 30 requests/minute |

When rate-limited, the API returns `429 Too Many Requests`. Retry after the duration indicated in the `Retry-After` header.

## Content Type

All request and response bodies use JSON:

```
Content-Type: application/json
```

Binary endpoints (attachment downloads, EML/MBOX exports) return appropriate MIME types.

## Quick Example

```bash
# List inbox emails using an API key
curl -s http://localhost:8080/api/emails \
  -H "Authorization: Bearer YOUR_API_KEY" | jq
```

## Endpoints

| Section | Prefix | Description |
|---|---|---|
| [Emails](./emails) | `/api/emails`, `/api/external/emails` | Inbox, search, threads, bulk operations |
| [Outbound](./outbound) | `/api/outbound` | Compose, drafts, send, reply, forward |
| [Labels](./labels) | `/api/labels` | Custom labels with color and sorting |
| [Filters](./filters) | `/api/filters` | Automatic email filter rules |
| [Profile](./profile) | `/api/profile` | User profile and additional addresses |
| [Preferences](./preferences) | `/api/preferences` | Theme, density, notification settings |
| [SMTP Credentials](./smtp-credentials) | `/api/smtp-credentials` | API key management and connection info |
| [Push Subscriptions](./push-subscriptions) | `/api/push-subscriptions` | Web push notification subscriptions |
| [Discovery](./discovery) | `/api/discovery` | Server capabilities and protocol info |
| [Config](./config) | `/api/config` | Runtime frontend configuration |
| [Internal Notifications](./notifications) | `/api/internal-notifications` | Service-to-service new email events |
| [SignalR Hub](./signalr) | `/hubs/email` | Real-time WebSocket events |
