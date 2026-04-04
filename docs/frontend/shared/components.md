# Shared Components

The `@relate/shared` package provides two categories of components: **mail components** for email-specific presentation and **UI components** for general-purpose primitives. Both are built with React 19, Tailwind CSS, and Radix UI.

## Mail Components

Import from `@relate/shared/components/mail`.

These components handle the visual presentation of email data. They receive typed props and render pure UI -- they do not fetch data or manage state. Data fetching is handled by the consuming application's hooks layer.

### `EmailList`

Renders a list of email items from an `EmailListItem[]` array. Each row shows:

- Sender display name or address
- Subject line
- Relative timestamp (via date-fns)
- Attachment count indicator
- Read/unread visual state (Lucide `Mail`/`MailOpen` icons)
- Unread rows are highlighted with a subtle primary color background

Accepts callbacks for row click, selection, and context actions.

### `EmailDetailView` / `EmailDetail`

Two components that work together:

**`EmailDetailView`** is the pure presentation component. Given an `EmailDetail` object, it renders:

- Sender avatar with computed initials
- Sender name and address
- Recipient badges grouped by type (To, Cc, Bcc)
- Email body: HTML content is sanitized via `sanitizeHtml()` before rendering with `dangerouslySetInnerHTML`. If no HTML body is available, the plain text body is displayed.
- Attachments with file metadata
- Action buttons (Reply, Reply All, Forward, Delete)

**`EmailDetail`** is a wrapper that adds data fetching concerns. In the shared package it provides a consistent component interface that consuming apps can extend with their own data layer.

### `SearchBar`

A search input component with:

- Leading search icon
- Clearable text input
- Submit handler
- Responsive width behavior

### `LabelBadge`

Displays a colored label tag. The label's hex color is used to compute a background (at 20% opacity), border, and text color. Supports an optional remove button for detaching the label from an email.

## UI Components

Import from `@relate/shared/components/ui`.

These are Shadcn/ui-style components built on Radix UI primitives. They use **CVA (class-variance-authority)** for variant management and **Tailwind CSS** for styling. All components are fully accessible, implementing proper ARIA attributes, keyboard navigation, and focus management through the underlying Radix primitives.

### Badge

An inline status/tag indicator.

**Variants:**

| Variant | Appearance |
|---------|------------|
| `default` | Primary background, primary foreground text |
| `secondary` | Secondary background, secondary foreground text |
| `destructive` | Red/destructive background |
| `outline` | Transparent background, visible border |

```tsx
import { Badge } from '@relate/shared/components/ui'

<Badge variant="secondary">Draft</Badge>
<Badge variant="destructive">Failed</Badge>
```

### Button

A button with multiple variants and sizes.

**Variants:**

| Variant | Appearance |
|---------|------------|
| `default` | Primary fill |
| `destructive` | Red/destructive fill |
| `outline` | Border only, transparent background |
| `secondary` | Secondary fill |
| `ghost` | No border or background, hover highlight |
| `link` | Styled as a text link with underline |

**Sizes:** `default`, `sm`, `lg`, `icon` (square, for icon-only buttons)

```tsx
import { Button } from '@relate/shared/components/ui'

<Button variant="outline" size="sm" onClick={handleCancel}>Cancel</Button>
<Button variant="destructive" onClick={handleDelete}>Delete</Button>
```

### Card

A container with consistent border, padding, and shadow. Composed of sub-components:

| Component | Purpose |
|-----------|---------|
| `Card` | Outer container |
| `CardHeader` | Top section (holds title and description) |
| `CardTitle` | Heading text (`h3` by default) |
| `CardDescription` | Subtitle/secondary text |
| `CardContent` | Main body content |
| `CardFooter` | Bottom section for actions |

```tsx
import { Card, CardHeader, CardTitle, CardContent } from '@relate/shared/components/ui'

<Card>
  <CardHeader>
    <CardTitle>SMTP Settings</CardTitle>
  </CardHeader>
  <CardContent>
    {/* Server details */}
  </CardContent>
</Card>
```

### ConfirmationDialog

A purpose-built dialog for confirming destructive or important actions. This is a higher-level component that composes `Dialog`, `Button`, and standard dialog sub-components.

**Props:**

| Prop | Type | Description |
|------|------|-------------|
| `open` | `boolean` | Whether the dialog is visible |
| `onOpenChange` | `(open: boolean) => void` | Called when open state should change |
| `title` | `string` | Dialog heading |
| `description` | `string` | Explanatory text |
| `confirmLabel` | `string` | Confirm button text (default: "Confirm") |
| `cancelLabel` | `string` | Cancel button text (default: "Cancel") |
| `variant` | `'default' \| 'destructive'` | Confirm button style |
| `onConfirm` | `() => void \| Promise<void>` | Called when confirmed (supports async) |
| `isLoading` | `boolean` | External loading state |

The confirm handler supports both synchronous and asynchronous operations. When `onConfirm` returns a promise, the dialog automatically shows a loading state and disables both buttons until the promise resolves or rejects.

```tsx
import { ConfirmationDialog } from '@relate/shared/components/ui'

<ConfirmationDialog
  open={showDelete}
  onOpenChange={setShowDelete}
  title="Delete Email"
  description="This action cannot be undone."
  variant="destructive"
  confirmLabel="Delete"
  onConfirm={async () => { await deleteEmail(id) }}
/>
```

### Dialog

A modal dialog built on `@radix-ui/react-dialog`. Provides the foundational dialog behavior: overlay, focus trap, close on escape, portal rendering.

**Sub-components:**

| Component | Purpose |
|-----------|---------|
| `Dialog` | Root controller (manages open state) |
| `DialogTrigger` | Element that opens the dialog on click |
| `DialogPortal` | Renders content in a portal outside the DOM tree |
| `DialogOverlay` | Semi-transparent backdrop |
| `DialogContent` | The dialog panel itself |
| `DialogHeader` | Top section layout |
| `DialogFooter` | Bottom section layout (typically for action buttons) |
| `DialogTitle` | Heading (required for accessibility) |
| `DialogDescription` | Description text |
| `DialogClose` | Close button element |

### Input

A styled text input extending the native `<input>` element. Applies consistent border, focus ring, placeholder, and disabled styles via Tailwind CSS. Forwards all native input props via `React.forwardRef`.

### Label

A form label built on `@radix-ui/react-label`. Provides consistent typography and a disabled appearance when its associated input is disabled. Uses `React.forwardRef` for ref forwarding.

### Popover

A floating content panel built on `@radix-ui/react-popover`. Used for dropdowns, color pickers, and other floating UI that should appear near a trigger element.

**Sub-components:** `Popover`, `PopoverTrigger`, `PopoverContent`

### Select

A dropdown select built on `@radix-ui/react-select`. Provides keyboard navigation, ARIA attributes, and a styled dropdown menu.

**Sub-components:** `Select`, `SelectTrigger`, `SelectValue`, `SelectContent`, `SelectItem`

### Switch

A toggle switch built on `@radix-ui/react-switch`. Used for boolean settings such as enabling filters or toggling notifications. Renders as a pill-shaped track with a sliding thumb indicator.

## Theme CSS

Import from `@relate/shared/styles/theme.css`.

The theme stylesheet defines CSS custom properties for the design system's color palette. It provides two sets of values -- one for light mode (`:root`) and one for dark mode (`.dark` class).

**Variables defined:**

| Variable | Purpose |
|----------|---------|
| `--background` / `--foreground` | Page background and text colors |
| `--card` / `--card-foreground` | Card container colors |
| `--popover` / `--popover-foreground` | Popover/dropdown colors |
| `--primary` / `--primary-foreground` | Primary action colors |
| `--secondary` / `--secondary-foreground` | Secondary/subtle colors |
| `--muted` / `--muted-foreground` | Muted/disabled colors |
| `--accent` / `--accent-foreground` | Accent highlight colors |
| `--destructive` / `--destructive-foreground` | Destructive/danger colors |
| `--border` | Border color |
| `--input` | Input border color |
| `--ring` | Focus ring color |
| `--radius` | Base border radius (0.5rem) |

The `@theme` block maps these CSS variables to Tailwind CSS color tokens (e.g., `--color-primary: hsl(var(--primary))`), and the `@layer base` block applies default border and background colors to all elements.

All values use HSL color space notation, allowing components to adjust opacity without redefining colors (e.g., `bg-primary/10` for a 10% opacity primary background).
