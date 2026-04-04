# Profile

The Profile API manages the authenticated user's profile information and additional email addresses.

**Base path:** `/api/profile`

All endpoints require authentication.

## Get Profile

Retrieve the current user's profile.

```
GET /api/profile
```

**Response** `200 OK`:

```json
{
  "id": "user-uuid",
  "email": "you@example.com",
  "displayName": "Your Name",
  "createdAt": "2026-01-15T08:00:00Z",
  "lastLoginAt": "2026-04-03T14:00:00Z",
  "additionalAddresses": [
    {
      "id": "address-uuid",
      "address": "alias@example.com",
      "isVerified": true,
      "addedAt": "2026-02-10T12:00:00Z"
    }
  ]
}
```

**curl example:**

```bash
curl -s "http://localhost:8080/api/profile" \
  -H "Authorization: Bearer YOUR_TOKEN" | jq
```

## Update Profile

Update the user's display name.

```
PUT /api/profile
```

**Request body:**

```json
{
  "displayName": "New Display Name"
}
```

**Response** `204 No Content`

**curl example:**

```bash
curl -s -X PUT "http://localhost:8080/api/profile" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"displayName": "Jane Doe"}'
```

## Add Additional Address

Register an additional email address for the user's account. A verification token is generated upon creation.

```
POST /api/profile/addresses
```

**Request body:**

```json
{
  "address": "newalias@example.com"
}
```

**Response** `201 Created`:

```json
{
  "id": "new-address-uuid",
  "address": "newalias@example.com",
  "isVerified": false,
  "addedAt": "2026-04-03T15:00:00Z"
}
```

**curl example:**

```bash
curl -s -X POST "http://localhost:8080/api/profile/addresses" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"address": "newalias@example.com"}' | jq
```

## Remove Additional Address

Remove an additional email address from the account.

```
DELETE /api/profile/addresses/{addressId}
```

**Response** `204 No Content`

## Send Verification Email

Send a verification email to an additional address.

```
POST /api/profile/addresses/{addressId}/send-verification
```

::: warning Not Implemented
This endpoint currently returns `501 Not Implemented`. Verification is planned for a future release.
:::

## Verify Address

Verify an additional email address with a verification token.

```
POST /api/profile/addresses/{addressId}/verify
```

::: warning Not Implemented
This endpoint currently returns `501 Not Implemented`. Verification is planned for a future release.
:::
