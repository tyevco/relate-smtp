import { format } from 'date-fns'
import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, Paperclip, Trash2, Download, Reply, ReplyAll, Forward } from 'lucide-react'
import { sanitizeHtml } from '@relate/shared/lib/sanitize'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { AttachmentPreview } from './attachment-preview'
import { ExportDialog } from './export-dialog'
import type { EmailDetail as EmailDetailType } from '@/api/types'

interface EmailDetailViewProps {
  email: EmailDetailType
  onBack: () => void
  onDelete: () => void
}

interface EmailDetailProps {
  email: EmailDetailType
}

export function EmailDetailView({ email, onBack, onDelete }: EmailDetailViewProps) {
  const navigate = useNavigate()

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-2 p-3 sm:p-4 border-b">
        <Button variant="ghost" size="icon" onClick={onBack} className="lg:hidden">
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div className="flex-1" />
        <Button
          variant="ghost"
          size="icon"
          title="Reply"
          onClick={() => navigate({ to: '/compose', search: { replyTo: email.id } })}
        >
          <Reply className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          title="Reply All"
          onClick={() => navigate({ to: '/compose', search: { replyTo: email.id, replyAll: 'true' } })}
        >
          <ReplyAll className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          title="Forward"
          onClick={() => navigate({ to: '/compose', search: { forwardFrom: email.id } })}
        >
          <Forward className="h-4 w-4" />
        </Button>
        <ExportDialog
          emailId={email.id}
          trigger={
            <Button variant="ghost" size="icon">
              <Download className="h-4 w-4" />
            </Button>
          }
        />
        <Button variant="ghost" size="icon" onClick={onDelete}>
          <Trash2 className="h-4 w-4" />
        </Button>
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
                <AttachmentPreview
                  key={attachment.id}
                  emailId={email.id}
                  attachment={attachment}
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

        {/* Reply/Forward actions */}
        <div className="flex items-center gap-2 mt-6 pt-4 border-t">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate({ to: '/compose', search: { replyTo: email.id } })}
          >
            <Reply className="h-3.5 w-3.5 mr-1" />
            Reply
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate({ to: '/compose', search: { replyTo: email.id, replyAll: 'true' } })}
          >
            <ReplyAll className="h-3.5 w-3.5 mr-1" />
            Reply All
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate({ to: '/compose', search: { forwardFrom: email.id } })}
          >
            <Forward className="h-3.5 w-3.5 mr-1" />
            Forward
          </Button>
        </div>
      </div>
    </div>
  )
}

// Simplified component for standalone email detail page
export function EmailDetail({ email }: EmailDetailProps) {
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
                <AttachmentPreview
                  key={attachment.id}
                  emailId={email.id}
                  attachment={attachment}
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
