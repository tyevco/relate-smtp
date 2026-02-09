import { useState } from 'react'
import { useSentEmails, useEmail, useDeleteEmail } from '@/api/hooks'
import { EmailList, EmailDetailView } from '@relate/shared/components/mail'
import { Button, Badge } from '@relate/shared/components/ui'
import { ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react'
import { useShortcuts } from '@/hooks/useShortcuts'

export function Sent() {
  const [page, setPage] = useState(1)
  const [selectedEmailId, setSelectedEmailId] = useState<string | null>(null)

  const { data: emailsData, isLoading, refetch } = useSentEmails(page)
  const { data: selectedEmail } = useEmail(selectedEmailId || '')
  const deleteEmail = useDeleteEmail()

  useShortcuts({
    onRefresh: () => refetch(),
    onDelete: () => {
      if (selectedEmailId) {
        deleteEmail.mutate(selectedEmailId, {
          onSuccess: () => setSelectedEmailId(null),
        })
      }
    },
    onEscape: () => setSelectedEmailId(null),
  })

  const handleSelectEmail = (id: string) => {
    setSelectedEmailId(id)
  }

  const handleBack = () => {
    setSelectedEmailId(null)
  }

  const handleDelete = () => {
    if (selectedEmailId) {
      deleteEmail.mutate(selectedEmailId, {
        onSuccess: () => setSelectedEmailId(null),
      })
    }
  }

  const totalPages = emailsData ? Math.ceil(emailsData.totalCount / emailsData.pageSize) : 1

  return (
    <div className="flex h-full">
      {/* Email List */}
      <div className={`w-96 border-r flex flex-col ${selectedEmailId ? 'hidden lg:flex' : 'flex'}`}>
        <div className="p-4 border-b">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold">Sent</h2>
              {emailsData && emailsData.totalCount > 0 && (
                <Badge variant="secondary">{emailsData.totalCount}</Badge>
              )}
            </div>
            <Button variant="ghost" size="icon" onClick={() => refetch()}>
              <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
            </Button>
          </div>
        </div>

        <div className="flex-1 overflow-auto">
          {isLoading ? (
            <div className="p-8 text-center text-muted-foreground">
              Loading...
            </div>
          ) : emailsData?.items.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              No sent emails
            </div>
          ) : (
            <EmailList
              emails={emailsData?.items || []}
              selectedId={selectedEmailId || undefined}
              onSelect={handleSelectEmail}
            />
          )}
        </div>

        {emailsData && totalPages > 1 && (
          <div className="flex items-center justify-between p-4 border-t">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Previous
            </Button>
            <span className="text-sm text-muted-foreground">
              Page {page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              Next
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        )}
      </div>

      {/* Email Detail */}
      <div className={`flex-1 ${selectedEmailId ? 'flex' : 'hidden lg:flex'}`}>
        {selectedEmail ? (
          <div className="flex-1 overflow-hidden">
            <EmailDetailView
              email={selectedEmail}
              onBack={handleBack}
              onDelete={handleDelete}
            />
          </div>
        ) : (
          <div className="flex-1 flex items-center justify-center text-muted-foreground">
            Select an email to view
          </div>
        )}
      </div>
    </div>
  )
}
