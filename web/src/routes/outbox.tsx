import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useState, useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useOutbox } from '@/api/hooks'
import { Badge } from '@/components/ui/badge'
import { Clock } from 'lucide-react'
import type { OutboundEmailListItem } from '@/api/types'

export const Route = createFileRoute('/outbox')({
  component: OutboxPage,
})

function OutboxPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const { data: outbox, isLoading } = useOutbox(page, 20)

  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: '/login' })
    }
  }, [auth.isAuthenticated, auth.isLoading, navigate])

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return ''
    return new Date(dateStr).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Queued':
        return <Badge variant="secondary">Queued</Badge>
      case 'Sending':
        return <Badge variant="default">Sending</Badge>
      default:
        return <Badge variant="outline">{status}</Badge>
    }
  }

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
      <div className="flex items-center justify-between mb-4 sm:mb-6">
        <h1 className="text-xl sm:text-2xl font-bold flex items-center gap-2">
          <Clock className="h-5 w-5 sm:h-6 sm:w-6" />
          Outbox
        </h1>
      </div>

      {isLoading ? (
        <div className="text-center py-8 text-sm">Loading...</div>
      ) : outbox && outbox.items.length > 0 ? (
        <>
          <div className="border rounded-lg bg-card divide-y">
            {outbox.items.map((email: OutboundEmailListItem) => (
              <div
                key={email.id}
                className="flex items-center justify-between px-4 py-3 hover:bg-accent/50 transition-colors"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium truncate">
                      {email.subject || '(No subject)'}
                    </span>
                    {getStatusBadge(email.status)}
                  </div>
                  <div className="text-xs text-muted-foreground mt-0.5">
                    {email.recipientCount} recipient{email.recipientCount !== 1 ? 's' : ''}
                    {email.queuedAt && ` · Queued ${formatDate(email.queuedAt)}`}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {outbox.totalCount > 20 && (
            <div className="mt-4 flex flex-wrap justify-center items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 sm:px-4 py-2 border rounded disabled:opacity-50 text-xs sm:text-sm min-h-[44px]"
              >
                Previous
              </button>
              <span className="px-2 sm:px-4 py-2 text-xs sm:text-sm">
                Page {page} of {Math.ceil(outbox.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={page >= Math.ceil(outbox.totalCount / 20)}
                className="px-3 sm:px-4 py-2 border rounded disabled:opacity-50 text-xs sm:text-sm min-h-[44px]"
              >
                Next
              </button>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-8 text-gray-500 text-sm">
          No emails in outbox
        </div>
      )}
    </div>
  )
}
