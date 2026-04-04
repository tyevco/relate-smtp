# Outbound Email

The Outbound API handles composing, drafting, sending, replying to, and forwarding emails. All endpoints require authentication.

**Base path:** `/api/outbound`

## Send Email

Compose and immediately send an email.

```
POST /api/outbound/send
```

**Request body:**

```json
{
  "fromAddress": "you@example.com",
  "subject": "Project update",
  "textBody": "Here is the plain text version.",
  "htmlBody": "<p>Here is the <strong>HTML</strong> version.</p>",
  "recipients": [
    { "address": "alice@example.com", "displayName": "Alice", "type": "To" },
    { "address": "bob@example.com", "displayName": "Bob", "type": "Cc" },
    { "address": "secret@example.com", "type": "Bcc" }
  ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `fromAddress` | string | Yes | Sender email address |
| `subject` | string | Yes | Email subject line |
| `textBody` | string | No | Plain text body |
| `htmlBody` | string | No | HTML body |
| `recipients` | array | Yes | Up to 100 recipients |
| `recipients[].address` | string | Yes | Recipient email address |
| `recipients[].displayName` | string | No | Recipient display name |
| `recipients[].type` | string | Yes | `"To"`, `"Cc"`, or `"Bcc"` |

At least one of `textBody` or `htmlBody` must be provided. Maximum of 100 recipients per email.

**Response** `200 OK`:

```json
{
  "id": "outbound-uuid",
  "status": "Queued",
  "createdAt": "2026-04-03T15:00:00Z"
}
```

The email enters the outbound queue and is processed by the background delivery service. Check delivery status via [Get Outbound Email](#get-outbound-email).

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/outbound/send" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fromAddress": "you@example.com",
    "subject": "Hello from the API",
    "textBody": "This email was sent via the Relate Mail API.",
    "recipients": [
      { "address": "friend@example.com", "displayName": "Friend", "type": "To" }
    ]
  }' | jq
```

## List Drafts

Retrieve a paginated list of draft emails.

```
GET /api/outbound/drafts?page=1&pageSize=20
```

**Query Parameters:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | integer | `1` | Page number |
| `pageSize` | integer | `20` | Items per page |

**Response** `200 OK`: Paginated list of draft summaries.

```json
{
  "items": [
    {
      "id": "draft-uuid",
      "fromAddress": "you@example.com",
      "subject": "Work in progress",
      "createdAt": "2026-04-02T10:00:00Z",
      "updatedAt": "2026-04-03T09:30:00Z"
    }
  ],
  "totalCount": 3,
  "page": 1,
  "pageSize": 20
}
```

## Create Draft

Save an email as a draft without sending.

```
POST /api/outbound/drafts
```

**Request body:** Same shape as [Send Email](#send-email). All fields are optional for drafts (you can save an empty draft and fill it in later).

**Response** `201 Created`: The created draft object.

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/outbound/drafts" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fromAddress": "you@example.com",
    "subject": "Draft: quarterly report",
    "textBody": "TODO: add numbers"
  }' | jq
```

## Get Draft

Retrieve a draft with its recipients and attachments.

```
GET /api/outbound/drafts/{id}
```

**Response** `200 OK`:

```json
{
  "id": "draft-uuid",
  "fromAddress": "you@example.com",
  "subject": "Draft: quarterly report",
  "textBody": "TODO: add numbers",
  "htmlBody": null,
  "recipients": [],
  "attachments": [],
  "createdAt": "2026-04-02T10:00:00Z",
  "updatedAt": "2026-04-03T09:30:00Z"
}
```

## Update Draft

Replace a draft's contents.

```
PUT /api/outbound/drafts/{id}
```

**Request body:** Same shape as [Send Email](#send-email).

**Response** `204 No Content`

## Delete Draft

Permanently delete a draft.

```
DELETE /api/outbound/drafts/{id}
```

**Response** `204 No Content`

## Send Draft

Send a previously saved draft. The draft must have at least one recipient and a subject.

```
POST /api/outbound/drafts/{id}/send
```

**Response** `200 OK`: The outbound email object with status `Queued`.

## Reply

Reply to a received email. The API automatically sets `In-Reply-To` and `References` headers for proper threading.

```
POST /api/outbound/reply/{emailId}
```

**Request body:**

```json
{
  "textBody": "Thanks for the update!",
  "htmlBody": "<p>Thanks for the update!</p>",
  "replyAll": false
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `textBody` | string | No | Plain text reply body |
| `htmlBody` | string | No | HTML reply body |
| `replyAll` | boolean | No | `true` to reply to all recipients (default: `false`) |

**Response** `200 OK`: The outbound email object with status `Queued`.

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/outbound/reply/EMAIL_ID" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"textBody": "Sounds good, see you then!", "replyAll": false}' | jq
```

## Forward

Forward a received email to new recipients. Attachments from the original email are automatically copied.

```
POST /api/outbound/forward/{emailId}
```

**Request body:**

```json
{
  "textBody": "FYI - see the email below.",
  "recipients": [
    { "address": "colleague@example.com", "displayName": "Colleague", "type": "To" }
  ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `textBody` | string | No | Additional text prepended to the forward |
| `recipients` | array | Yes | New recipients for the forwarded email |

**Response** `200 OK`: The outbound email object with status `Queued`.

## List Outbox

View emails currently queued or in the process of being sent.

```
GET /api/outbound/outbox?page=1&pageSize=20
```

**Response** `200 OK`: Paginated list of outbound emails with status `Queued` or `Sending`.

## List Sent

View successfully sent emails.

```
GET /api/outbound/sent?page=1&pageSize=20
```

**Response** `200 OK`: Paginated list of outbound emails with status `Sent`.

## Get Outbound Email

Retrieve an outbound email with full detail including per-recipient delivery status.

```
GET /api/outbound/{id}
```

**Response** `200 OK`:

```json
{
  "id": "outbound-uuid",
  "fromAddress": "you@example.com",
  "subject": "Hello from the API",
  "textBody": "This email was sent via the Relate Mail API.",
  "htmlBody": null,
  "status": "Sent",
  "createdAt": "2026-04-03T15:00:00Z",
  "sentAt": "2026-04-03T15:00:05Z",
  "messageId": "<unique-id@example.com>",
  "inReplyTo": null,
  "references": null,
  "retryCount": 0,
  "lastError": null,
  "recipients": [
    {
      "id": "recipient-uuid",
      "address": "friend@example.com",
      "displayName": "Friend",
      "type": "To",
      "status": "Delivered",
      "deliveredAt": "2026-04-03T15:00:05Z"
    }
  ],
  "attachments": []
}
```

**Outbound email statuses:**

| Status | Description |
|---|---|
| `Draft` | Saved but not queued for sending |
| `Queued` | In the send queue, awaiting processing |
| `Sending` | Currently being delivered via MX resolution |
| `Sent` | Successfully delivered to all recipients |
| `Failed` | Delivery failed after all retry attempts |

**Recipient statuses:**

| Status | Description |
|---|---|
| `Pending` | Not yet attempted |
| `Delivered` | Successfully delivered |
| `Failed` | Delivery failed for this recipient |
