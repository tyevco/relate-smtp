import { formatDistanceToNow } from 'date-fns'
import { Mail, MailOpen, Paperclip } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { EmailListItem } from '@/api/types'

interface EmailListProps {
  emails: EmailListItem[]
  selectedId?: string
  onSelect: (id: string) => void
}

export function EmailList({ emails, selectedId, onSelect }: EmailListProps) {
  if (emails.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-48 text-muted-foreground">
        <Mail className="h-12 w-12 mb-2" />
        <p>No emails yet</p>
      </div>
    )
  }

  return (
    <div className="divide-y" role="listbox" aria-label="Email list">
      {emails.map((email) => (
        <button
          key={email.id}
          role="option"
          aria-selected={selectedId === email.id}
          aria-label={`Email from ${email.fromDisplayName || email.fromAddress}: ${email.subject || 'No subject'}`}
          onClick={() => onSelect(email.id)}
          className={cn(
            'w-full text-left p-3 sm:p-4 hover:bg-accent transition-colors min-h-[44px]',
            selectedId === email.id && 'bg-accent',
            !email.isRead && 'bg-primary/5'
          )}
        >
          <div className="flex items-start gap-2 sm:gap-3">
            <div className="mt-1 flex-shrink-0">
              {email.isRead ? (
                <MailOpen className="h-4 w-4 text-muted-foreground" />
              ) : (
                <Mail className="h-4 w-4 text-primary" />
              )}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-2">
                <span className={cn(
                  'truncate text-sm sm:text-base',
                  !email.isRead && 'font-semibold'
                )}>
                  {email.fromDisplayName || email.fromAddress}
                </span>
                <span className="text-xs sm:text-sm text-muted-foreground whitespace-nowrap flex-shrink-0">
                  {formatDistanceToNow(new Date(email.receivedAt), { addSuffix: true })}
                </span>
              </div>
              <p className={cn(
                'text-xs sm:text-sm truncate',
                !email.isRead && 'font-medium'
              )}>
                {email.subject}
              </p>
              {email.attachmentCount > 0 && (
                <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
                  <Paperclip className="h-3 w-3" />
                  <span className="hidden sm:inline">{email.attachmentCount} attachment{email.attachmentCount > 1 ? 's' : ''}</span>
                  <span className="sm:hidden">{email.attachmentCount}</span>
                </div>
              )}
            </div>
          </div>
        </button>
      ))}
    </div>
  )
}
