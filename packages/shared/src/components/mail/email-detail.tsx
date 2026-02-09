import { format } from 'date-fns'
import { ArrowLeft, Paperclip, Trash2 } from 'lucide-react'
import DOMPurify from 'dompurify'
import { Button } from '../ui/button'
import { Badge } from '../ui/badge'
import type { EmailDetail as EmailDetailType, EmailAttachment } from '../../api/types'

function sanitizeHtml(html: string | null | undefined): string {
  if (!html) return ''
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: [
      'p', 'br', 'b', 'i', 'u', 'strong', 'em', 'a', 'ul', 'ol', 'li', 'img',
      'table', 'tr', 'td', 'th', 'thead', 'tbody', 'tfoot', 'div', 'span',
      'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'pre', 'code',
      'hr', 'sub', 'sup', 'small', 'mark', 'del', 'ins', 'address',
    ],
    ALLOWED_ATTR: ['href', 'src', 'alt', 'class', 'style', 'target', 'rel', 'width', 'height'],
    ALLOW_DATA_ATTR: false,
    FORBID_TAGS: ['script', 'style', 'iframe', 'object', 'embed', 'form', 'input'],
  })
}

interface AttachmentItemProps {
  attachment: EmailAttachment
  onDownload?: (attachment: EmailAttachment) => void
  onPreview?: (attachment: EmailAttachment) => void
}

function AttachmentItem({ attachment, onDownload, onPreview }: AttachmentItemProps) {
  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  const isPreviewable = attachment.contentType.startsWith('image/') || attachment.contentType === 'application/pdf'

  return (
    <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-3 border rounded-md hover:bg-muted gap-2 sm:gap-0">
      <div className="flex items-center gap-3 flex-1 min-w-0 w-full sm:w-auto">
        <div className="flex-1 min-w-0">
          <div className="text-xs sm:text-sm font-medium truncate">{attachment.fileName}</div>
          <div className="text-xs text-muted-foreground">
            {formatFileSize(attachment.sizeBytes)} • {attachment.contentType}
          </div>
        </div>
      </div>
      <div className="flex gap-1 ml-2 w-full sm:w-auto">
        {isPreviewable && onPreview && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onPreview(attachment)}
            className="flex-1 sm:flex-none min-h-[44px] text-xs sm:text-sm"
          >
            Preview
          </Button>
        )}
        {onDownload && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onDownload(attachment)}
            className="flex-1 sm:flex-none min-h-[44px] text-xs sm:text-sm"
          >
            Download
          </Button>
        )}
      </div>
    </div>
  )
}

interface EmailDetailViewProps {
  email: EmailDetailType
  onBack?: () => void
  onDelete?: () => void
  onDownloadAttachment?: (attachment: EmailAttachment) => void
  onPreviewAttachment?: (attachment: EmailAttachment) => void
  actions?: React.ReactNode
}

export function EmailDetailView({
  email,
  onBack,
  onDelete,
  onDownloadAttachment,
  onPreviewAttachment,
  actions,
}: EmailDetailViewProps) {
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-2 p-3 sm:p-4 border-b">
        {onBack && (
          <Button variant="ghost" size="icon" onClick={onBack} className="lg:hidden">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        )}
        <div className="flex-1" />
        {actions}
        {onDelete && (
          <Button variant="ghost" size="icon" onClick={onDelete}>
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </div>

      <div className="flex-1 overflow-auto p-3 sm:p-4 lg:p-6">
        <h1 className="text-lg sm:text-xl font-semibold mb-4 break-words">{email.subject}</h1>

        <div className="flex items-start gap-3 sm:gap-4 mb-4">
          <div className="flex-shrink-0 w-8 h-8 sm:w-10 sm:h-10 rounded-full bg-primary/10 flex items-center justify-center">
            <span className="text-base sm:text-lg font-semibold text-primary">
              {(email.fromDisplayName || email.fromAddress || '?')[0].toUpperCase()}
            </span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-medium text-sm sm:text-base break-words">
              {email.fromDisplayName || email.fromAddress}
            </p>
            <p className="text-xs sm:text-sm text-muted-foreground break-all">
              {email.fromAddress}
            </p>
            <p className="text-xs sm:text-sm text-muted-foreground">
              {format(new Date(email.receivedAt), 'PPpp')}
            </p>
          </div>
        </div>

        <div className="mb-4">
          <div className="flex flex-wrap gap-1">
            {email.recipients.map((recipient) => (
              <Badge key={recipient.id} variant="secondary" className="text-xs">
                <span className="hidden sm:inline">{recipient.type}: </span>
                {recipient.displayName || recipient.address}
              </Badge>
            ))}
          </div>
        </div>

        {email.attachments.length > 0 && (
          <div className="mb-4 space-y-2">
            <div className="flex items-center gap-2">
              <Paperclip className="h-4 w-4" />
              <span className="text-xs sm:text-sm font-medium">
                {email.attachments.length} Attachment{email.attachments.length > 1 ? 's' : ''}
              </span>
            </div>
            <div className="space-y-2">
              {email.attachments.map((attachment) => (
                <AttachmentItem
                  key={attachment.id}
                  attachment={attachment}
                  onDownload={onDownloadAttachment}
                  onPreview={onPreviewAttachment}
                />
              ))}
            </div>
          </div>
        )}

        <div className="prose prose-sm sm:prose max-w-none">
          {email.htmlBody ? (
            <div
              dangerouslySetInnerHTML={{ __html: sanitizeHtml(email.htmlBody) }}
              className="border rounded-lg p-3 sm:p-4 bg-white dark:bg-gray-900 overflow-x-auto"
            />
          ) : (
            <pre className="whitespace-pre-wrap font-sans text-xs sm:text-sm break-words overflow-x-auto">
              {email.textBody || '(No content)'}
            </pre>
          )}
        </div>
      </div>
    </div>
  )
}

interface EmailDetailProps {
  email: EmailDetailType
  onDownloadAttachment?: (attachment: EmailAttachment) => void
  onPreviewAttachment?: (attachment: EmailAttachment) => void
}

export function EmailDetail({ email, onDownloadAttachment, onPreviewAttachment }: EmailDetailProps) {
  return (
    <div className="border rounded-lg bg-card overflow-hidden">
      <div className="flex-1 overflow-auto p-3 sm:p-4 lg:p-6">
        <h1 className="text-lg sm:text-xl font-semibold mb-4 break-words">{email.subject}</h1>

        <div className="flex items-start gap-3 sm:gap-4 mb-4">
          <div className="flex-shrink-0 w-8 h-8 sm:w-10 sm:h-10 rounded-full bg-primary/10 flex items-center justify-center">
            <span className="text-base sm:text-lg font-semibold text-primary">
              {(email.fromDisplayName || email.fromAddress || '?')[0].toUpperCase()}
            </span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-medium text-sm sm:text-base break-words">
              {email.fromDisplayName || email.fromAddress}
            </p>
            <p className="text-xs sm:text-sm text-muted-foreground break-all">
              {email.fromAddress}
            </p>
            <p className="text-xs sm:text-sm text-muted-foreground">
              {format(new Date(email.receivedAt), 'PPpp')}
            </p>
          </div>
        </div>

        <div className="mb-4">
          <div className="flex flex-wrap gap-1">
            {email.recipients.map((recipient) => (
              <Badge key={recipient.id} variant="secondary" className="text-xs">
                <span className="hidden sm:inline">{recipient.type}: </span>
                {recipient.displayName || recipient.address}
              </Badge>
            ))}
          </div>
        </div>

        {email.attachments.length > 0 && (
          <div className="mb-4 space-y-2">
            <div className="flex items-center gap-2">
              <Paperclip className="h-4 w-4" />
              <span className="text-xs sm:text-sm font-medium">
                {email.attachments.length} Attachment{email.attachments.length > 1 ? 's' : ''}
              </span>
            </div>
            <div className="space-y-2">
              {email.attachments.map((attachment) => (
                <AttachmentItem
                  key={attachment.id}
                  attachment={attachment}
                  onDownload={onDownloadAttachment}
                  onPreview={onPreviewAttachment}
                />
              ))}
            </div>
          </div>
        )}

        <div className="prose prose-sm sm:prose max-w-none">
          {email.htmlBody ? (
            <div
              dangerouslySetInnerHTML={{ __html: sanitizeHtml(email.htmlBody) }}
              className="border rounded-lg p-3 sm:p-4 bg-white dark:bg-gray-900 overflow-x-auto"
            />
          ) : (
            <pre className="whitespace-pre-wrap font-sans text-xs sm:text-sm break-words overflow-x-auto">
              {email.textBody || '(No content)'}
            </pre>
          )}
        </div>
      </div>
    </div>
  )
}
