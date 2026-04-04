# Shared Utilities

The `@relate/shared` package provides three utility modules: pagination constants, HTML sanitization, and a class name merging helper. These are used across the web, desktop, and mobile clients.

## Constants (`@relate/shared/lib/constants`)

Shared pagination defaults used by all API hooks and list components.

```typescript
/** Default number of items to fetch per page */
export const DEFAULT_PAGE_SIZE = 20

/** Maximum number of items that can be requested per page */
export const MAX_PAGE_SIZE = 100
```

These constants ensure all clients use the same pagination behavior. The API backend also validates against `MAX_PAGE_SIZE`, so keeping the frontend aligned avoids unnecessary validation errors.

**Usage:**

```typescript
import { DEFAULT_PAGE_SIZE } from '@relate/shared/lib/constants'

export function useEmails(page = 1, pageSize = DEFAULT_PAGE_SIZE) {
  return useQuery({
    queryKey: ['emails', page, pageSize],
    queryFn: () => api.get(`/emails?page=${page}&pageSize=${pageSize}`),
  })
}
```

## HTML Sanitization (`@relate/shared/lib/sanitize`)

Email bodies frequently contain HTML that may include malicious content -- script tags, event handlers, iframes, or other vectors for XSS attacks. The `sanitizeHtml()` function uses **DOMPurify** to strip dangerous content while preserving safe formatting.

### `sanitizeHtml(html: string | null | undefined): string`

Takes an HTML string (or null/undefined) and returns a sanitized HTML string safe for rendering with `dangerouslySetInnerHTML`.

```typescript
import { sanitizeHtml } from '@relate/shared/lib/sanitize'

const clean = sanitizeHtml(email.htmlBody)
// <div dangerouslySetInnerHTML={{ __html: clean }} />
```

If the input is `null` or `undefined`, an empty string is returned.

### Allowed Tags

The following HTML tags are permitted and will be preserved in the output:

**Text formatting:** `p`, `br`, `b`, `i`, `u`, `strong`, `em`, `sub`, `sup`, `small`, `mark`, `del`, `ins`

**Headings:** `h1`, `h2`, `h3`, `h4`, `h5`, `h6`

**Structure:** `div`, `span`, `blockquote`, `pre`, `code`, `hr`, `address`

**Lists:** `ul`, `ol`, `li`

**Links and media:** `a`, `img`

**Tables:** `table`, `tr`, `td`, `th`, `thead`, `tbody`, `tfoot`, `caption`, `col`, `colgroup`

**Figures:** `figure`, `figcaption`

### Allowed Attributes

Only these attributes are kept on permitted tags:

| Attribute | Purpose |
|-----------|---------|
| `href` | Link URLs on `<a>` tags |
| `src` | Image sources on `<img>` tags |
| `alt` | Alternative text for images |
| `class` | CSS class names |
| `target` | Link target (e.g., `_blank`) |
| `rel` | Link relationship |
| `width`, `height` | Dimension attributes for images and tables |
| `title` | Tooltip text |
| `colspan`, `rowspan` | Table cell spanning |
| `scope` | Table header scope |

Data attributes (`data-*`) are explicitly disallowed (`ALLOW_DATA_ATTR: false`).

### Forbidden Tags

These tags are explicitly stripped, even if they somehow pass the allowed tag filter:

- `script` -- JavaScript execution
- `style` -- CSS injection (can exfiltrate data or alter page layout)
- `iframe` -- Embedded frames (can load external content)
- `object`, `embed` -- Plugin content
- `form`, `input` -- Form elements (can trick users into submitting data)

### Forbidden Attributes

These event handler attributes are stripped from all tags:

- `onerror` -- Fires when a resource fails to load (commonly used for XSS)
- `onload` -- Fires when a resource loads
- `onclick` -- Click handler
- `onmouseover` -- Mouse hover handler
- `onfocus` -- Focus handler
- `onblur` -- Blur handler

### Security Considerations

The sanitization is applied every time an email's HTML body is rendered. Even though HTML is stored as-is in the database (to preserve the original message), it is never trusted at render time. This defense-in-depth approach means that even if a future code path bypasses sanitization in one place, other rendering paths remain protected.

The `target` attribute is added to the allowed list (`ADD_ATTR: ['target']`) so that links in emails can open in new tabs (`target="_blank"`), which is the expected behavior for links within an email client.

## Class Name Utility (`@relate/shared/lib/utils`)

### `cn(...inputs: ClassValue[]): string`

A utility function that combines **clsx** (conditional class name construction) with **tailwind-merge** (intelligent Tailwind CSS class deduplication).

```typescript
import { cn } from '@relate/shared/lib/utils'

// Simple merging
cn('px-4 py-2', 'text-sm')
// => 'px-4 py-2 text-sm'

// Conditional classes
cn('base-class', isActive && 'bg-primary', isDisabled && 'opacity-50')
// => 'base-class bg-primary' (when isActive=true, isDisabled=false)

// Tailwind conflict resolution
cn('px-4', 'px-6')
// => 'px-6' (tailwind-merge removes the conflicting px-4)

// Object syntax
cn({ 'font-bold': isBold, 'text-red-500': hasError })
```

**Why both clsx and tailwind-merge?**

- **clsx** handles conditional logic: objects, arrays, falsy values are all supported
- **tailwind-merge** resolves Tailwind CSS specificity conflicts: when two classes target the same CSS property (e.g., `px-4` and `px-6`), the last one wins. Without tailwind-merge, both classes would be applied and the result would depend on CSS source order, which can produce surprising behavior.

This function is used extensively in all UI components to merge default styles with user-provided `className` props:

```typescript
function Button({ className, variant, ...props }) {
  return (
    <button
      className={cn(buttonVariants({ variant }), className)}
      {...props}
    />
  )
}
```
