# Preferences

The Preferences API manages user-specific settings for the application, including theme, display density, notification preferences, and email digest configuration.

**Base path:** `/api/preferences`

All endpoints require authentication.

## Get Preferences

Retrieve the current user's preferences.

```
GET /api/preferences
```

**Response** `200 OK`:

```json
{
  "theme": "system",
  "displayDensity": "comfortable",
  "emailsPerPage": 20,
  "defaultSort": "date",
  "showPreview": true,
  "groupByDate": true,
  "desktopNotifications": true,
  "emailDigest": false,
  "digestFrequency": "daily",
  "digestTime": "08:00"
}
```

**Field descriptions:**

| Field | Type | Values | Description |
|---|---|---|---|
| `theme` | string | `"light"`, `"dark"`, `"system"` | UI color theme |
| `displayDensity` | string | `"compact"`, `"comfortable"`, `"spacious"` | Email list spacing |
| `emailsPerPage` | integer | 10-100 | Number of emails per page |
| `defaultSort` | string | `"date"`, `"sender"`, `"subject"` | Default sort order for email list |
| `showPreview` | boolean | | Show preview text in email list |
| `groupByDate` | boolean | | Group emails by date in the list view |
| `desktopNotifications` | boolean | | Enable browser/desktop push notifications |
| `emailDigest` | boolean | | Enable periodic email digest summaries |
| `digestFrequency` | string | `"daily"`, `"weekly"` | How often to send digest |
| `digestTime` | string | HH:mm format | Time of day to send digest |

**curl example:**

```bash
curl -s "http://localhost:8080/api/preferences" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Update Preferences

Update any or all preference fields. Only the fields included in the request body are updated; omitted fields remain unchanged.

```
PUT /api/preferences
```

**Request body** (all fields optional):

```json
{
  "theme": "dark",
  "displayDensity": "compact",
  "emailsPerPage": 50,
  "desktopNotifications": false
}
```

**Response** `204 No Content`

**curl example:**

```bash
# Switch to dark theme and compact density
curl -s -X PUT "http://localhost:8080/api/preferences" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"theme": "dark", "displayDensity": "compact"}'
```

```bash
# Enable daily digest at 9 AM
curl -s -X PUT "http://localhost:8080/api/preferences" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"emailDigest": true, "digestFrequency": "daily", "digestTime": "09:00"}'
```
