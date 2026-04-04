# Labels

Labels let you organize emails with custom names, colors, and sort order. You can assign multiple labels to an email and retrieve emails by label.

**Base path:** `/api/labels`

All endpoints require authentication.

## List Labels

Retrieve all labels for the authenticated user, ordered by `sortOrder`.

```
GET /api/labels
```

**Response** `200 OK`:

```json
[
  {
    "id": "label-uuid-1",
    "name": "Work",
    "color": "#3b82f6",
    "sortOrder": 0,
    "emailCount": 42
  },
  {
    "id": "label-uuid-2",
    "name": "Personal",
    "color": "#10b981",
    "sortOrder": 1,
    "emailCount": 15
  }
]
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/labels" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Create Label

Create a new label.

```
POST /api/labels
```

**Request body:**

```json
{
  "name": "Urgent",
  "color": "#ef4444",
  "sortOrder": 2
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | string | Yes | Label display name |
| `color` | string | Yes | Hex color code (e.g., `#ef4444`) |
| `sortOrder` | integer | Yes | Position in the label list |

**Response** `201 Created`:

```json
{
  "id": "new-label-uuid",
  "name": "Urgent",
  "color": "#ef4444",
  "sortOrder": 2,
  "emailCount": 0
}
```

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/labels" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "Urgent", "color": "#ef4444", "sortOrder": 2}' | jq
```

## Update Label

Update an existing label's name, color, or sort order.

```
PUT /api/labels/{id}
```

**Request body:**

```json
{
  "name": "High Priority",
  "color": "#f59e0b",
  "sortOrder": 0
}
```

**Response** `204 No Content`

## Delete Label

Delete a label. This removes the label from all emails that have it assigned.

```
DELETE /api/labels/{id}
```

**Response** `204 No Content`

**curl example:**

```bash
curl -s -X DELETE "http://localhost:8080/api/labels/LABEL_ID" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Add Label to Email

Assign a label to an email.

```
POST /api/labels/emails/{emailId}
```

**Request body:**

```json
{
  "labelId": "label-uuid"
}
```

**Response** `204 No Content`

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/labels/emails/EMAIL_ID" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"labelId": "LABEL_ID"}'
```

## Remove Label from Email

Remove a label assignment from an email.

```
DELETE /api/labels/emails/{emailId}/{labelId}
```

**Response** `204 No Content`

## List Emails by Label

Retrieve a paginated list of emails that have a specific label assigned.

```
GET /api/labels/{labelId}/emails?page=1&pageSize=20
```

**Query Parameters:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | integer | `1` | Page number |
| `pageSize` | integer | `20` | Items per page |

**Response** `200 OK`: Paginated list of email items (same shape as the [Emails](./emails) list response).

**curl example:**

```bash
curl -s "http://localhost:8080/api/labels/LABEL_ID/emails?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```
