# Emails

The Emails API provides access to received emails in your inbox. There are two controllers that expose the same functionality:

- **EmailsController** (`/api/emails`) — for authenticated users (OIDC/JWT)
- **ExternalEmailsController** (`/api/external/emails`) — for API key holders with `api:read` or `api:write` scopes

Both controllers share the same request/response shapes. The examples below use `/api/emails`; replace with `/api/external/emails` when using API key authentication.

## List Emails

Retrieve a paginated list of inbox emails, sorted by date descending.

```
GET /api/emails?page=1&pageSize=20
```

**Query Parameters:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | integer | `1` | Page number |
| `pageSize` | integer | `20` | Items per page |

**Response** `200 OK`:

```json
{
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "fromAddress": "sender@example.com",
      "fromDisplayName": "Jane Doe",
      "subject": "Meeting tomorrow",
      "previewText": "Hi, just wanted to confirm our meeting...",
      "receivedAt": "2026-04-03T14:30:00Z",
      "isRead": false,
      "hasAttachments": true,
      "threadId": "thread-uuid",
      "labels": [
        { "id": "label-uuid", "name": "Work", "color": "#3b82f6" }
      ]
    }
  ],
  "totalCount": 142,
  "unreadCount": 7,
  "page": 1,
  "pageSize": 20
}
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/emails?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Search Emails

Search inbox emails with text and filter criteria.

```
GET /api/emails/search?q=text&fromDate=&toDate=&hasAttachments=&isRead=
```

**Query Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `q` | string | Full-text search across subject, body, sender |
| `fromDate` | ISO 8601 date | Only emails received on or after this date |
| `toDate` | ISO 8601 date | Only emails received on or before this date |
| `hasAttachments` | boolean | Filter by attachment presence |
| `isRead` | boolean | Filter by read/unread status |
| `page` | integer | Page number |
| `pageSize` | integer | Items per page |

**Response** `200 OK`: Same paginated shape as List Emails.

**curl example:**

```bash
# Search for unread emails with attachments from the last week
curl -s "http://localhost:8080/api/emails/search?q=invoice&hasAttachments=true&isRead=false&fromDate=2026-03-28" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Get Email

Retrieve a single email with full body, recipients, and attachments.

```
GET /api/emails/{id}
```

**Response** `200 OK`:

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "fromAddress": "sender@example.com",
  "fromDisplayName": "Jane Doe",
  "toAddresses": ["you@example.com"],
  "ccAddresses": ["team@example.com"],
  "subject": "Meeting tomorrow",
  "textBody": "Hi, just wanted to confirm our meeting...",
  "htmlBody": "<p>Hi, just wanted to confirm our meeting...</p>",
  "receivedAt": "2026-04-03T14:30:00Z",
  "isRead": true,
  "threadId": "thread-uuid",
  "messageId": "<unique-id@example.com>",
  "inReplyTo": "<parent-id@example.com>",
  "references": "<root-id@example.com> <parent-id@example.com>",
  "attachments": [
    {
      "id": "attach-uuid",
      "fileName": "report.pdf",
      "contentType": "application/pdf",
      "size": 245760
    }
  ],
  "labels": [
    { "id": "label-uuid", "name": "Work", "color": "#3b82f6" }
  ]
}
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/emails/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Mark Read / Unread

Update the read status of an email.

```
PATCH /api/emails/{id}
```

**Request body:**

```json
{
  "isRead": true
}
```

**Response** `204 No Content`

**curl example:**

```bash
# Mark as read
curl -s -X PATCH "http://localhost:8080/api/emails/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isRead": true}'
```

## Delete Email

Permanently delete an email.

```
DELETE /api/emails/{id}
```

**Response** `204 No Content`

**curl example:**

```bash
curl -s -X DELETE "http://localhost:8080/api/emails/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Download Attachment

Download a binary attachment from an email.

```
GET /api/emails/{id}/attachments/{attachmentId}
```

**Response** `200 OK` with the attachment's MIME type and binary body.

**curl example:**

```bash
curl -s "http://localhost:8080/api/emails/EMAIL_ID/attachments/ATTACH_ID" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -o report.pdf
```

## Export as EML

Download a single email in standard `.eml` (RFC 5322) format.

```
GET /api/emails/{id}/export/eml
```

**Response** `200 OK` with `Content-Type: message/rfc822`.

**curl example:**

```bash
curl -s "http://localhost:8080/api/emails/EMAIL_ID/export/eml" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -o email.eml
```

## Export as MBOX

Stream multiple emails as an MBOX archive. Limited to 50,000 emails per export with a 10-minute rate limit between exports.

```
GET /api/emails/export/mbox?fromDate=&toDate=
```

**Query Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `fromDate` | ISO 8601 date | Start of date range |
| `toDate` | ISO 8601 date | End of date range |

**Response** `200 OK` with `Content-Type: application/mbox`, streamed.

**curl example:**

```bash
curl -s "http://localhost:8080/api/emails/export/mbox?fromDate=2026-01-01&toDate=2026-04-01" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -o archive.mbox
```

## Get Thread

Retrieve all emails in a conversation thread.

```
GET /api/emails/threads/{threadId}
```

**Response** `200 OK`: Array of email objects (same shape as Get Email), ordered chronologically.

**curl example:**

```bash
curl -s "http://localhost:8080/api/emails/threads/THREAD_ID" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Bulk Mark Read / Unread

Mark multiple emails as read or unread in a single request.

```
POST /api/emails/bulk/mark-read
```

**Request body:**

```json
{
  "emailIds": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ],
  "isRead": true
}
```

**Response** `200 OK`:

```json
{
  "count": 2
}
```

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/emails/bulk/mark-read" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"emailIds": ["EMAIL_ID_1", "EMAIL_ID_2"], "isRead": true}' | jq
```

## Bulk Delete

Delete multiple emails in a single request.

```
POST /api/emails/bulk/delete
```

**Request body:**

```json
{
  "emailIds": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

**Response** `200 OK`:

```json
{
  "count": 2
}
```

## Sent Emails

Retrieve sent emails with optional from-address filter.

```
GET /api/emails/sent?page=1&pageSize=20&fromAddress=
```

**Query Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `page` | integer | Page number |
| `pageSize` | integer | Items per page |
| `fromAddress` | string | Filter by sender address |

**Response** `200 OK`: Paginated list of sent email items.

## Sent Addresses

List distinct from-addresses used in sent emails.

```
GET /api/emails/sent/addresses
```

**Response** `200 OK`:

```json
["you@example.com", "alias@example.com"]
```

---

## External Emails API

The External Emails API mirrors all of the above endpoints under `/api/external/emails`. It requires an API key with `api:read` scope for read operations and `api:write` scope for write operations (mark read, delete, bulk operations).

**curl example with API key:**

```bash
# List emails using API key
curl -s "http://localhost:8080/api/external/emails?page=1&pageSize=10" \
  -H "Authorization: ApiKey YOUR_API_KEY" | jq

# Mark email as read using API key
curl -s -X PATCH "http://localhost:8080/api/external/emails/EMAIL_ID" \
  -H "Authorization: ApiKey YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"isRead": true}'
```
