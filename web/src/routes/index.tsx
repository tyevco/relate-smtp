import { useState, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { useEmails, useEmail, useMarkEmailRead, useDeleteEmail } from '@/api/hooks'
import { EmailList } from '@/components/mail/email-list'
import { EmailDetailView } from '@/components/mail/email-detail'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react'

export const Route = createFileRoute('/')({
  component: InboxPage,
})

function InboxPage() {
  const auth = useAuth()
  const [page, setPage] = useState(1)
  const [selectedEmailId, setSelectedEmailId] = useState<string | null>(null)

  // Call all hooks unconditionally at the top
  const { data: emailsData, isLoading, refetch } = useEmails(page)
  const { data: selectedEmail } = useEmail(selectedEmailId || '')
  const markRead = useMarkEmailRead()
  const deleteEmail = useDeleteEmail()

  // Redirect to login if not authenticated (after all hooks are called)
  useEffect(() => {
    const authority = import.meta.env.VITE_OIDC_AUTHORITY
    if (authority && !auth.isLoading && !auth.isAuthenticated) {
      console.log('🔐 Index: Not authenticated, redirecting to login')
      window.location.href = '/login'
    }
  }, [auth.isAuthenticated, auth.isLoading])

  // Show loading while checking auth
  if (auth.isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="text-lg">Loading...</div>
      </div>
    )
  }

  const handleSelectEmail = (id: string) => {
    setSelectedEmailId(id)
    markRead.mutate({ id, isRead: true })
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
    <div className="container mx-auto px-4 py-6">
      <div className="flex gap-6">
        {/* Email List */}
        <div className={`w-full md:w-96 ${selectedEmailId ? 'hidden md:block' : ''}`}>
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold">Inbox</h2>
              {emailsData && emailsData.unreadCount > 0 && (
                <Badge variant="secondary">{emailsData.unreadCount} unread</Badge>
              )}
            </div>
            <Button variant="ghost" size="icon" onClick={() => refetch()}>
              <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
            </Button>
          </div>

          <div className="border rounded-lg bg-card">
            {isLoading ? (
              <div className="p-8 text-center text-muted-foreground">
                Loading...
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
            <div className="flex items-center justify-between mt-4">
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
        <div className={`flex-1 ${selectedEmailId ? '' : 'hidden md:block'}`}>
          {selectedEmail ? (
            <div className="border rounded-lg bg-card h-[calc(100vh-8rem)] overflow-hidden">
              <EmailDetailView
                email={selectedEmail}
                onBack={handleBack}
                onDelete={handleDelete}
              />
            </div>
          ) : (
            <div className="border rounded-lg bg-card h-[calc(100vh-8rem)] flex items-center justify-center text-muted-foreground">
              Select an email to read
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
