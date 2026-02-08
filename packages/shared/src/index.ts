// API Types - export with explicit names to avoid conflicts
export type {
  EmailListItem,
  EmailRecipient,
  EmailAttachment,
  EmailDetail,
  EmailListResponse,
  EmailAddress,
  Profile,
  SmtpApiKey,
  SmtpConnectionInfo,
  CreateApiKeyRequest,
  CreatedApiKey,
  SmtpCredentials,
  Label as LabelType,
  CreateLabelRequest,
  UpdateLabelRequest,
  EmailFilter,
  CreateEmailFilterRequest,
  UpdateEmailFilterRequest,
  UserPreference,
  UpdateUserPreferenceRequest,
} from './api/types'

// UI Components - export with explicit names to avoid conflicts
export {
  Badge,
  badgeVariants,
  Button,
  buttonVariants,
  Card,
  CardHeader,
  CardFooter,
  CardTitle,
  CardDescription,
  CardContent,
  Dialog,
  DialogPortal,
  DialogOverlay,
  DialogClose,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
  Input,
  Label,
  Popover,
  PopoverTrigger,
  PopoverContent,
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
  Switch,
} from './components/ui'
export type { BadgeProps, ButtonProps, InputProps, LabelProps, SwitchProps } from './components/ui'

// Mail Components - export with renamed EmailDetail to avoid conflict
export { EmailList, SearchBar, LabelBadge } from './components/mail'
export { EmailDetail as EmailDetailComponent, EmailDetailView } from './components/mail'

// Utilities
export { cn } from './lib/utils'
export { sanitizeHtml } from './lib/sanitize'
export { DEFAULT_PAGE_SIZE, MAX_PAGE_SIZE } from './lib/constants'
