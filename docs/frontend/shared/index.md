# Shared Package (`@relate/shared`)

The `@relate/shared` npm package contains code shared between the web, desktop, and mobile clients. Its purpose is to prevent duplication of TypeScript type definitions, UI components, utility functions, and design tokens across the three client applications.

## Why a Shared Package?

Without `@relate/shared`, every API type interface, every UI primitive, and every utility function would need to be copied into each client app. This creates a maintenance burden: a change to an API response shape would require updates in three places, and any inconsistency between clients could cause subtle bugs.

The shared package solves this by providing a single source of truth:

- **Types** -- All API request/response interfaces are defined once and consumed by all clients
- **UI components** -- Shadcn/ui primitives (Button, Card, Dialog, etc.) are built once and shared
- **Mail components** -- Email-specific presentation components (EmailList, EmailDetail, SearchBar, LabelBadge) are shared between web and desktop
- **Utilities** -- Class name merging, HTML sanitization, and pagination constants are defined once
- **Theme** -- CSS custom properties for the design system's color palette (light and dark modes)

## Package Location

```
packages/shared/
  src/
    api/
      types.ts          # All TypeScript interfaces for API communication
    components/
      mail/             # Email-specific presentation components
        email-detail.tsx
        email-list.tsx
        label-badge.tsx
        search-bar.tsx
        index.ts
      ui/               # General-purpose UI primitives
        badge.tsx
        button.tsx
        card.tsx
        confirmation-dialog.tsx
        dialog.tsx
        input.tsx
        label.tsx
        popover.tsx
        select.tsx
        switch.tsx
        index.ts
    lib/
      constants.ts      # Shared constants (page sizes)
      sanitize.ts       # DOMPurify HTML sanitization
      utils.ts          # Class name merging utility
    styles/
      theme.css         # CSS custom properties for theming
    index.ts            # Root barrel export
  package.json
  tsconfig.json
```

## Exports

The package uses subpath exports in `package.json` to provide clean import paths:

| Import Path | Resolves To | Contents |
|-------------|-------------|----------|
| `@relate/shared` | `src/index.ts` | Everything (barrel export) |
| `@relate/shared/api/types` | `src/api/types.ts` | All TypeScript interfaces |
| `@relate/shared/components/ui` | `src/components/ui/index.ts` | UI primitives |
| `@relate/shared/components/mail` | `src/components/mail/index.ts` | Mail components |
| `@relate/shared/lib/utils` | `src/lib/utils.ts` | `cn()` utility |
| `@relate/shared/lib/sanitize` | `src/lib/sanitize.ts` | `sanitizeHtml()` |
| `@relate/shared/lib/constants` | `src/lib/constants.ts` | Page size constants |
| `@relate/shared/styles/theme.css` | `src/styles/theme.css` | Theme CSS variables |

**Recommended practice:** Use subpath imports (e.g., `@relate/shared/api/types`) rather than the barrel import (`@relate/shared`) to avoid pulling in unnecessary code and to make dependency chains explicit.

## Building

The shared package uses TypeScript for type checking only -- it does not compile to JavaScript. Consumer bundlers (Vite for web/desktop, Metro for mobile) handle transpilation directly from the TypeScript source.

```bash
# From the repository root:
npm run build:shared

# Or from the package directory:
cd packages/shared
npm run typecheck
```

Both commands run `tsc --noEmit`, which validates types without producing output files. This must succeed before building any consuming application.

## Dependencies

The package has peer dependencies on React 19 and uses:

| Dependency | Purpose |
|------------|---------|
| `@radix-ui/react-dialog` | Accessible dialog primitive |
| `class-variance-authority` | Component variant management |
| `clsx` | Conditional class name construction |
| `date-fns` | Date formatting utilities |
| `dompurify` | HTML sanitization for email bodies |
| `lucide-react` | Icon components |
| `tailwind-merge` | Intelligent Tailwind class merging |

## How Clients Consume the Package

### Web and Desktop

Both are npm workspace members, so `@relate/shared` resolves to the local `packages/shared/` directory automatically. No publishing or linking is needed.

```typescript
// In web/src/api/types.ts
export type { EmailDetail, Profile } from '@relate/shared/api/types'

// In web/src/components/ui/button.tsx
export { Button, buttonVariants } from '@relate/shared/components/ui'
```

### Mobile

The mobile app (Expo) is not part of the npm workspace due to Metro bundler requirements. It imports shared types directly. UI components from `@relate/shared` are not used in mobile because React Native has its own component primitives.
