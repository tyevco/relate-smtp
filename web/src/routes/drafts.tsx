import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useState, useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useDrafts, useDeleteDraft, useSendDraft } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { FileText, Trash2, Send } from 'lucide-react'
import type { OutboundEmailListItem } from '@/api/types'

export const Route = createFileRoute('/drafts')({
  component: DraftsPage,
})

function DraftsPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const { data: drafts, isLoading } = useDrafts(page, 20)
  const deleteDraft = useDeleteDraft()
  const sendDraft = useSendDraft()

  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: '/login' })
    }
  }, [auth.isAuthenticated, auth.isLoading, navigate])

  const handleDelete = (id: string) => {
    deleteDraft.mutate(id)
  }

  const handleSend = (id: string) => {
    sendDraft.mutate(id)
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
      <div className="flex items-center justify-between mb-4 sm:mb-6">
        <h1 className="text-xl sm:text-2xl font-bold flex items-center gap-2">
          <FileText className="h-5 w-5 sm:h-6 sm:w-6" />
          Drafts
        </h1>
      </div>

      {isLoading ? (
        <div className="text-center py-8 text-sm">Loading...</div>
      ) : drafts && drafts.items.length > 0 ? (
        <>
          <div className="border rounded-lg bg-card divide-y">
            {drafts.items.map((draft: OutboundEmailListItem) => (
              <div
                key={draft.id}
                className="flex items-center justify-between px-4 py-3 hover:bg-accent/50 transition-colors"
              >
                <div className="flex-1 min-w-0 cursor-pointer" onClick={() => navigate({ to: '/compose' })}>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium truncate">
                      {draft.subject || '(No subject)'}
                    </span>
                    {draft.recipientCount > 0 && (
                      <span className="text-xs text-muted-foreground shrink-0">
                        ({draft.recipientCount} recipient{draft.recipientCount !== 1 ? 's' : ''})
                      </span>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground mt-0.5">
                    {formatDate(draft.createdAt)}
                  </div>
                </div>
                <div className="flex items-center gap-1 ml-2 shrink-0">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    title="Send"
                    onClick={() => handleSend(draft.id)}
                    disabled={draft.recipientCount === 0}
                  >
                    <Send className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-destructive"
                    title="Delete"
                    onClick={() => handleDelete(draft.id)}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {drafts.totalCount > 20 && (
            <div className="mt-4 flex flex-wrap justify-center items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 sm:px-4 py-2 border rounded disabled:opacity-50 text-xs sm:text-sm min-h-[44px]"
              >
                Previous
              </button>
              <span className="px-2 sm:px-4 py-2 text-xs sm:text-sm">
                Page {page} of {Math.ceil(drafts.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={page >= Math.ceil(drafts.totalCount / 20)}
                className="px-3 sm:px-4 py-2 border rounded disabled:opacity-50 text-xs sm:text-sm min-h-[44px]"
              >
                Next
              </button>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-8 text-gray-500 text-sm">
          No drafts
        </div>
      )}
    </div>
  )
}
