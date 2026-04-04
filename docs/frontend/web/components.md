# Components

The web application's components live in `web/src/components/` and are organized into three categories: mail components for email-specific UI, filter components for rule management, and UI components for general-purpose primitives.

Many of these components are thin wrappers or direct re-exports from the `@relate/shared` package. Web-specific components (like the filter builder or export dialog) that are not needed by the mobile or desktop clients live only in the web app.

## Mail Components

Located in `src/components/mail/`.

### `email-list.tsx`

Renders a scrollable list of email items. Each row displays:

- **Sender name** (or email address if no display name is set)
- **Subject line** truncated to fit
- **Time-ago timestamp** (e.g., "3 hours ago", "Yesterday")
- **Attachment count** indicator (paperclip icon with count)
- **Unread indicator** using `Mail` (unread) and `MailOpen` (read) icons from Lucide
- **Visual styling**: unread emails have a `bg-primary/5` background tint to stand out

Each row includes ARIA labels for accessibility, identifying the sender, subject, and read state for screen readers.

### `email-detail.tsx`

Two components that work together:

- **`EmailDetailView`** -- The presentation component that renders the full email content:
  - Sender avatar with generated initials (first letters of display name)
  - Sender name and address
  - Recipients displayed as **To**, **Cc**, and **Bcc** badges
  - Email body: sanitized HTML rendering (via DOMPurify) with a plaintext fallback when no HTML body is available
  - Attachment list with file names, sizes, and download links
  - Action buttons: Reply, Reply All, Forward, Delete

- **`EmailDetail`** -- A wrapper component that handles data fetching (via `useEmail` hook) and loading/error states, then delegates rendering to `EmailDetailView`.

### `search-bar.tsx`

A search input with:

- Search icon (magnifying glass) on the left
- Text input with placeholder
- Clear button (X icon) that appears when the input has content
- Submit handler for explicit search
- Responsive sizing that adapts to the container width

### `label-badge.tsx`

Renders a colored badge for an email label. The color is applied dynamically:

- Background at 20% opacity of the label color
- Border and text in the full label color
- Optional **remove button** (X icon) for detaching a label from an email

### `label-manager.tsx`

A dialog for creating, editing, and deleting labels:

- Name text input
- Color picker with 10 preset colors (displayed as clickable swatches)
- Create/update and delete actions

### `label-selector.tsx`

A dropdown menu for selecting which labels to apply to an email. Displays all available labels with their color badges and allows toggling labels on/off for the selected email.

### `attachment-preview.tsx`

Displays file attachment information and provides preview capabilities:

- File name, content type, and human-readable size
- **Image preview**: opens a modal with the full image for image content types
- **PDF preview**: embeds the PDF in a modal viewer
- **Download button** for all attachment types
- URL validation to prevent rendering of malicious attachment URLs

### `export-dialog.tsx`

A dialog for exporting emails in standard formats:

- **Single email export**: downloads as `.EML` file
- **Batch export**: downloads all matching emails as `.MBOX` file
- **Optional date filters**: from-date and to-date pickers to narrow the export range

## Filter Components

Located in `src/components/filters/`.

### `filter-builder.tsx`

A form dialog for creating and editing email filter rules. Includes:

- **Name** text input
- **Priority** number input (lower numbers run first)
- **Enabled toggle** switch
- **Condition inputs**:
  - From address contains (text)
  - Subject contains (text)
  - Body contains (text)
  - Has attachments (checkbox)
- **Action checkboxes**:
  - Mark as read
  - Delete
- **Label selector** for the "assign label" action

::: info Screenshot
**[Screenshot placeholder: Filter builder]**

_TODO: Add screenshot of the filter builder dialog with conditions and actions filled in_
:::

## UI Components

Located in `src/components/ui/`. These are Shadcn/ui-style components built on Radix UI primitives with Tailwind CSS styling and CVA (class-variance-authority) for variant management.

### `badge.tsx`

An inline badge/tag component.

**Variants**: `default`, `secondary`, `destructive`, `outline`

```tsx
<Badge variant="destructive">Failed</Badge>
```

### `button.tsx`

A button component with multiple variants and sizes.

**Variants**: `default`, `destructive`, `outline`, `secondary`, `ghost`, `link`
**Sizes**: `default`, `sm`, `lg`, `icon`

```tsx
<Button variant="outline" size="sm">Cancel</Button>
```

### `card.tsx`

A card container with composable sub-components:

- `Card` -- outer container with border and shadow
- `CardHeader` -- top section for title and description
- `CardTitle` -- heading text
- `CardDescription` -- subtitle/description text
- `CardContent` -- main body area
- `CardFooter` -- bottom section for actions

### `dialog.tsx`

A modal dialog built on `@radix-ui/react-dialog`. Composable sub-components:

- `Dialog`, `DialogTrigger`, `DialogContent`
- `DialogHeader`, `DialogFooter`
- `DialogTitle`, `DialogDescription`
- `DialogClose`, `DialogOverlay`, `DialogPortal`

Includes an overlay backdrop, close button, and focus trapping for accessibility.

### `input.tsx`

A styled text input that extends the native `<input>` element with consistent border, focus ring, and disabled states.

### `label.tsx`

A form label built on `@radix-ui/react-label` with consistent typography and disabled state styling.

### `popover.tsx`

A floating content panel built on `@radix-ui/react-popover`. Used for dropdown menus, color pickers, and other floating UI.

### `select.tsx`

A dropdown select built on `@radix-ui/react-select` with:

- `Select`, `SelectTrigger`, `SelectValue`
- `SelectContent`, `SelectItem`

Renders a native-like dropdown with keyboard navigation and ARIA attributes.

### `switch.tsx`

A toggle switch built on `@radix-ui/react-switch`. Used for boolean settings like enabling filters and toggling notification preferences.

## Other Components

### `error-boundary.tsx`

A React error boundary that catches rendering errors in child components. When an error is caught, it displays:

- An `AlertTriangle` icon from Lucide
- The error message
- A visual indication that something went wrong

This prevents a single component failure from crashing the entire application.
