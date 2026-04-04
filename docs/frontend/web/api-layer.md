# API Layer

The web application's API layer lives in `src/api/` and consists of three main modules: a generic HTTP client, TypeScript type definitions, and a comprehensive set of TanStack Query hooks. Together, they provide type-safe data fetching with automatic caching and cache invalidation.

## API Client (`src/api/client.ts`)

The client module exports a generic `apiRequest<T>()` function and a convenience `api` object with methods for each HTTP verb.

### `apiRequest<T>(endpoint, options?)`

A generic fetch wrapper that:

1. Prepends the base URL (`VITE_API_URL` or `/api` by default) to the endpoint
2. Extracts the OIDC access token from `sessionStorage` (falling back to `localStorage`)
3. Sets `Content-Type: application/json` and `Authorization: Bearer <token>` headers
4. Handles error responses by throwing an `ApiError` with the HTTP status code
5. Returns `undefined` for `204 No Content` responses
6. Parses and returns JSON for all other successful responses

### Safe Storage Access

The client includes a `safeStorageAccess()` helper that wraps storage reads in a try/catch. This handles cases where `sessionStorage` or `localStorage` is unavailable, such as when the browser is in private/incognito mode with restricted storage access.

### `ApiError` Class

```typescript
class ApiError extends Error {
  status: number    // HTTP status code (e.g., 401, 404, 500)
  message: string   // Response body text or status text
}
```

TanStack Query hooks receive `ApiError` instances in their `onError` callbacks, making it straightforward to handle specific HTTP errors (e.g., showing a login prompt on 401).

### `api` Convenience Object

The `api` object provides shorthand methods:

```typescript
api.get<T>(endpoint)              // GET request
api.post<T>(endpoint, data)       // POST with JSON body
api.put<T>(endpoint, data)        // PUT with JSON body
api.patch<T>(endpoint, data)      // PATCH with JSON body
api.delete(endpoint)              // DELETE request
api.getHeaders()                  // Returns headers including auth token
api.baseUrl                       // The resolved API base URL
```

All methods are generic and return `Promise<T>`.

## Type Definitions (`src/api/types.ts`)

This file re-exports all TypeScript interfaces from `@relate/shared/api/types`. By centralizing the re-export, components and hooks throughout the web app can import types from `@/api/types` without needing to know about the shared package path.

```typescript
// All types are re-exported from the shared package
import type { EmailDetail, Profile, Label } from '@/api/types'
```

See [Shared Types](/frontend/shared/types) for the complete type reference.

## TanStack Query Hooks (`src/api/hooks.ts`)

The hooks module contains 40+ hooks organized by feature domain. Every hook follows TanStack Query conventions: queries return `UseQueryResult` objects (with `data`, `isLoading`, `error`), and mutations return `UseMutationResult` objects (with `mutate`, `mutateAsync`, `isPending`).

### Query Key Conventions

Query keys are structured arrays that enable granular cache invalidation:

| Pattern | Example | Invalidated by |
|---------|---------|---------------|
| `['emails', page, pageSize]` | `['emails', 1, 20]` | Any email mutation |
| `['email', id]` | `['email', 'abc-123']` | Mark read, delete |
| `['emails', 'search', ...]` | `['emails', 'search', 'invoice', ...]` | Email mutations |
| `['emails', 'sent', ...]` | `['emails', 'sent', null, 1, 20]` | Outbound mutations |
| `['profile']` | `['profile']` | Profile update, address changes |
| `['smtp-credentials']` | `['smtp-credentials']` | Key create/revoke |
| `['labels']` | `['labels']` | Label CRUD |
| `['filters']` | `['filters']` | Filter CRUD |
| `['preferences']` | `['preferences']` | Preference update |
| `['outbound', 'drafts', ...]` | `['outbound', 'drafts', 1, 20]` | Draft CRUD, send |
| `['outbound', 'outbox', ...]` | `['outbound', 'outbox', 1, 20]` | Send operations |
| `['thread', threadId]` | `['thread', 'msg-456']` | Email mutations |

### Cache Invalidation Strategy

Mutations invalidate related queries on success using `queryClient.invalidateQueries()`. Some mutations also use optimistic updates via `queryClient.setQueryData()` for instant UI feedback:

- **`useMarkEmailRead`** -- Sets the individual email cache entry directly, then invalidates the email list
- **`useUpdateProfile`** -- Sets the profile cache directly (no list to invalidate)
- **`useUpdatePreferences`** -- Sets the preferences cache directly
- **`useUpdateDraft`** -- Sets the individual draft cache, then invalidates the drafts list

### Default Configuration

- **`staleTime`**: 30 seconds for search queries (prevents refetching during rapid filter changes). Other queries use TanStack Query's default.
- **`gcTime`**: 5 minutes for search queries (keeps cached results available for back navigation)
- **`retry`**: 1 (TanStack Query default for queries)
- **`enabled`**: Many hooks use conditional enabling (e.g., `useEmail(id)` only fetches when `id` is truthy)

### Hooks by Feature

#### Email Inbox

| Hook | Type | Description |
|------|------|-------------|
| `useEmails(page, pageSize)` | Query | Fetch paginated inbox |
| `useSearchEmails(filters, page, pageSize)` | Query | Search with text query, date range, attachment/read filters |
| `useEmail(id)` | Query | Fetch single email detail |
| `useInfiniteEmails(pageSize)` | Infinite Query | Infinite-scroll email loading |
| `useThread(threadId)` | Query | Fetch all emails in a thread |
| `useMarkEmailRead()` | Mutation | Toggle read/unread status |
| `useDeleteEmail()` | Mutation | Delete a single email |
| `useBulkMarkRead()` | Mutation | Mark multiple emails read/unread |
| `useBulkDelete()` | Mutation | Delete multiple emails |

#### User Profile

| Hook | Type | Description |
|------|------|-------------|
| `useProfile()` | Query | Fetch user profile |
| `useUpdateProfile()` | Mutation | Update display name |
| `useAddEmailAddress()` | Mutation | Add an additional email address |
| `useRemoveEmailAddress()` | Mutation | Remove an additional address |
| `useSendVerification()` | Mutation | Trigger verification email |
| `useVerifyEmailAddress()` | Mutation | Submit verification code |

#### SMTP Credentials

| Hook | Type | Description |
|------|------|-------------|
| `useSmtpCredentials()` | Query | Fetch connection info and API keys |
| `useCreateSmtpApiKey()` | Mutation | Generate a new API key |
| `useRevokeSmtpApiKey()` | Mutation | Revoke an existing API key |

#### Labels

| Hook | Type | Description |
|------|------|-------------|
| `useLabels()` | Query | Fetch all labels |
| `useCreateLabel()` | Mutation | Create a new label |
| `useUpdateLabel()` | Mutation | Update label name/color/sort |
| `useDeleteLabel()` | Mutation | Delete a label |
| `useAddLabelToEmail()` | Mutation | Assign a label to an email |
| `useRemoveLabelFromEmail()` | Mutation | Remove a label from an email |
| `useEmailsByLabel(labelId, page, pageSize)` | Query | Fetch emails with a specific label |

#### Filters

| Hook | Type | Description |
|------|------|-------------|
| `useFilters()` | Query | Fetch all filters |
| `useCreateFilter()` | Mutation | Create a new filter |
| `useUpdateFilter()` | Mutation | Update a filter |
| `useDeleteFilter()` | Mutation | Delete a filter |
| `useTestFilter(id, limit)` | Query | Dry-run a filter (manual trigger only, `enabled: false`) |

#### Preferences

| Hook | Type | Description |
|------|------|-------------|
| `usePreferences()` | Query | Fetch user preferences |
| `useUpdatePreferences()` | Mutation | Save preference changes |

#### Outbound Email

| Hook | Type | Description |
|------|------|-------------|
| `useSentEmails(fromAddress, page, pageSize)` | Query | Fetch sent emails, optionally filtered by from-address |
| `useSentFromAddresses()` | Query | Fetch list of addresses the user has sent from |
| `useDrafts(page, pageSize)` | Query | Fetch draft list |
| `useDraft(id)` | Query | Fetch single draft detail |
| `useCreateDraft()` | Mutation | Create a new draft |
| `useUpdateDraft()` | Mutation | Update an existing draft |
| `useDeleteDraft()` | Mutation | Delete a draft |
| `useSendEmail()` | Mutation | Send a new email directly |
| `useSendDraft()` | Mutation | Queue a draft for sending |
| `useReplyToEmail()` | Mutation | Reply to an email |
| `useForwardEmail()` | Mutation | Forward an email |
| `useOutbox(page, pageSize)` | Query | Fetch outbound queue |
| `useOutboundSent(page, pageSize)` | Query | Fetch sent outbound emails |
| `useOutboundEmail(id)` | Query | Fetch outbound email detail |
