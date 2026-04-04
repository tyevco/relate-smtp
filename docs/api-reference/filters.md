# Filters

Filters automatically process incoming emails based on conditions you define. When a new email arrives, it is evaluated against all enabled filters in priority order. Matching filters can mark emails as read, assign labels, or delete them.

**Base path:** `/api/filters`

All endpoints require authentication.

## List Filters

Retrieve all filters for the authenticated user, ordered by priority.

```
GET /api/filters
```

**Response** `200 OK`:

```json
[
  {
    "id": "filter-uuid-1",
    "name": "Auto-read newsletters",
    "isEnabled": true,
    "priority": 1,
    "fromAddressContains": "newsletter@",
    "subjectContains": null,
    "bodyContains": null,
    "hasAttachments": null,
    "markAsRead": true,
    "assignLabelId": "label-uuid",
    "delete": false,
    "createdAt": "2026-03-15T10:00:00Z",
    "updatedAt": "2026-03-15T10:00:00Z"
  }
]
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/filters" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Create Filter

Create a new email filter rule.

```
POST /api/filters
```

**Request body:**

```json
{
  "name": "Auto-read newsletters",
  "isEnabled": true,
  "priority": 1,
  "fromAddressContains": "newsletter@",
  "subjectContains": null,
  "bodyContains": null,
  "hasAttachments": null,
  "markAsRead": true,
  "assignLabelId": "label-uuid",
  "delete": false
}
```

**Fields:**

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | string | Yes | Display name for the filter |
| `isEnabled` | boolean | Yes | Whether the filter is active |
| `priority` | integer | Yes | Evaluation order (lower = higher priority) |
| `fromAddressContains` | string | No | Match if sender address contains this string |
| `subjectContains` | string | No | Match if subject contains this string |
| `bodyContains` | string | No | Match if body contains this string |
| `hasAttachments` | boolean | No | Match if email has/doesn't have attachments |
| `markAsRead` | boolean | No | Automatically mark matching emails as read |
| `assignLabelId` | string | No | Label ID to assign to matching emails |
| `delete` | boolean | No | Automatically delete matching emails |

**Conditions** are combined with AND logic: all specified conditions must match for the filter to trigger. At least one condition must be set.

**Actions** are all applied when a filter matches. You can combine actions (e.g., mark as read and assign a label).

**Response** `201 Created`: The created filter object.

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/filters" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Delete spam-like emails",
    "isEnabled": true,
    "priority": 10,
    "subjectContains": "You have won",
    "delete": true
  }' | jq
```

## Update Filter

Update an existing filter.

```
PUT /api/filters/{id}
```

**Request body:** Same shape as [Create Filter](#create-filter).

**Response** `204 No Content`

## Delete Filter

Permanently delete a filter.

```
DELETE /api/filters/{id}
```

**Response** `204 No Content`

## Test Filter

Test a filter against recent emails to see how many would match, without actually applying any actions.

```
POST /api/filters/{id}/test?limit=10
```

**Query Parameters:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `limit` | integer | `10` | Number of recent emails to test against |

**Response** `200 OK`:

```json
{
  "matchCount": 3,
  "matchedEmailIds": [
    "email-uuid-1",
    "email-uuid-2",
    "email-uuid-3"
  ]
}
```

**curl example:**

```bash
# Test a filter against the last 50 emails
curl -s -X POST "http://localhost:8080/api/filters/FILTER_ID/test?limit=50" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```
