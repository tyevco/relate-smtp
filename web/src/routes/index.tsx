import { useState, useEffect, useCallback, useRef } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useQueryClient } from '@tanstack/react-query'
import { useEmails, useEmail, useMarkEmailRead, useDeleteEmail, useSearchEmails, type EmailSearchFilters } from '@/api/hooks'
import { EmailList } from '@/components/mail/email-list'
import { EmailDetailView } from '@/components/mail/email-detail'
import { SearchBar } from '@/components/mail/search-bar'
import { ExportDialog } from '@/components/mail/export-dialog'
import { ErrorBoundary } from '@/components/error-boundary'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react'
import { signalRConnection } from '@/api/signalr'

export const Route = createFileRoute('/')({
  component: InboxPage,
})

function InboxPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [selectedEmailId, setSelectedEmailId] = useState<string | null>(null)
  const [unreadCount, setUnreadCount] = useState<number | null>(null)
  const [searchFilters, setSearchFilters] = useState<EmailSearchFilters>({})
  const isSearching = !!searchFilters.query

  // Call all hooks unconditionally at the top
  const { data: emailsData, isLoading, refetch } = useEmails(page)
  const { data: searchData, isLoading: isSearchLoading, refetch: refetchSearch } = useSearchEmails(searchFilters, page)
  const { data: selectedEmail } = useEmail(selectedEmailId || '')
  const markRead = useMarkEmailRead()
  const deleteEmail = useDeleteEmail()

  // Use search results if searching, otherwise use regular emails
  const currentData = isSearching ? searchData : emailsData
  const currentLoading = isSearching ? isSearchLoading : isLoading
  const currentRefetch = isSearching ? refetchSearch : refetch

  // Sync unread count from server data to local state
  // Local state allows SignalR to update count without refetching all emails
  useEffect(() => {
    if (currentData) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- intentional: sync server data to local state for SignalR updates
      setUnreadCount(currentData.unreadCount)
    }
  }, [currentData])

  // Debounce timeout ref for search
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Handle search with debouncing to prevent race conditions
  const handleSearch = useCallback((query: string) => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current)
    }
    setPage(1) // Reset to first page when searching
    setSelectedEmailId(null) // Clear selection
    searchTimeoutRef.current = setTimeout(() => {
      setSearchFilters({ query })
    }, 300) // 300ms debounce delay
  }, [])

  const handleSelectEmail = useCallback((id: string) => {
    setSelectedEmailId(id)
    markRead.mutate({ id, isRead: true })
  }, [markRead])

  const handleBack = useCallback(() => {
    setSelectedEmailId(null)
  }, [])

  const handleDelete = useCallback(() => {
    if (selectedEmailId) {
      deleteEmail.mutate(selectedEmailId, {
        onSuccess: () => setSelectedEmailId(null),
      })
    }
  }, [selectedEmailId, deleteEmail])

  // Connect to SignalR for real-time updates
  useEffect(() => {
    // Get API base URL - if it starts with '/', it's relative, so use window.location.origin
    let apiUrl = import.meta.env.VITE_API_URL || '/api'
    if (apiUrl.startsWith('/')) {
      apiUrl = window.location.origin
    }

    // Track if effect is still mounted to prevent state updates after unmount
    let isMounted = true

    // Store unsubscribe functions to clean up on unmount
    let unsubNewEmail: (() => void) | undefined
    let unsubEmailUpdated: (() => void) | undefined
    let unsubEmailDeleted: (() => void) | undefined
    let unsubUnreadCount: (() => void) | undefined

    const setupConnection = async () => {
      try {
        await signalRConnection.connect(apiUrl)

        // Check if still mounted after async operation
        if (!isMounted) return

        // Handle new email notifications
        unsubNewEmail = signalRConnection.onNewEmail(() => {
          queryClient.invalidateQueries({ queryKey: ['emails'] })
        })

        // Handle email updates (read/unread)
        unsubEmailUpdated = signalRConnection.onEmailUpdated(() => {
          queryClient.invalidateQueries({ queryKey: ['emails'] })
        })

        // Handle email deletions
        unsubEmailDeleted = signalRConnection.onEmailDeleted(() => {
          queryClient.invalidateQueries({ queryKey: ['emails'] })
        })

        // Handle unread count changes
        unsubUnreadCount = signalRConnection.onUnreadCountChanged((count) => {
          if (isMounted) {
            setUnreadCount(count)
          }
        })
      } catch (error) {
        if (isMounted) {
          console.error('Failed to connect to SignalR:', error)
        }
      }
    }

    setupConnection()

    return () => {
      isMounted = false
      // Unsubscribe handlers but don't disconnect singleton connection
      // The connection persists for app lifetime and handles reconnection
      unsubNewEmail?.()
      unsubEmailUpdated?.()
      unsubEmailDeleted?.()
      unsubUnreadCount?.()
    }
  }, [queryClient, auth.isAuthenticated])

  // Redirect to login if not authenticated (after all hooks are called)
  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: '/login' })
    }
  }, [auth.isAuthenticated, auth.isLoading, navigate])

  // Show loading while checking auth
  if (auth.isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="text-lg">Loading...</div>
      </div>
    )
  }

  const totalPages = currentData ? Math.ceil(currentData.totalCount / currentData.pageSize) : 1

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
      <div className="flex flex-col lg:flex-row gap-4 lg:gap-6">
        {/* Email List */}
        <div className={`w-full lg:w-96 ${selectedEmailId ? 'hidden lg:block' : ''}`}>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-4 gap-2">
            <div className="flex items-center gap-2">
              <h2 className="text-base sm:text-lg font-semibold">
                {isSearching ? 'Search Results' : 'Inbox'}
              </h2>
              {(unreadCount !== null && unreadCount > 0) && (
                <Badge variant="secondary" className="text-xs">{unreadCount} unread</Badge>
              )}
            </div>
            <div className="flex items-center gap-1">
              <ExportDialog />
              <Button variant="ghost" size="icon" onClick={() => currentRefetch()}>
                <RefreshCw className={`h-4 w-4 ${currentLoading ? 'animate-spin' : ''}`} />
              </Button>
            </div>
          </div>

          <div className="mb-4">
            <SearchBar onSearch={handleSearch} initialValue={searchFilters.query} />
          </div>

          <div className="border rounded-lg bg-card">
            {currentLoading ? (
              <div className="p-8 text-center text-muted-foreground text-sm">
                Loading...
              </div>
            ) : (
              <ErrorBoundary>
                <EmailList
                  emails={currentData?.items || []}
                  selectedId={selectedEmailId || undefined}
                  onSelect={handleSelectEmail}
                />
              </ErrorBoundary>
            )}
          </div>

          {emailsData && totalPages > 1 && (
            <div className="flex items-center justify-between mt-4 gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="text-xs sm:text-sm"
              >
                <ChevronLeft className="h-4 w-4 sm:mr-1" />
                <span className="hidden sm:inline">Previous</span>
              </Button>
              <span className="text-xs sm:text-sm text-muted-foreground whitespace-nowrap">
                Page {page} of {totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="text-xs sm:text-sm"
              >
                <span className="hidden sm:inline">Next</span>
                <ChevronRight className="h-4 w-4 sm:ml-1" />
              </Button>
            </div>
          )}
        </div>

        {/* Email Detail */}
        <div className={`flex-1 ${selectedEmailId ? '' : 'hidden lg:block'}`}>
          {selectedEmail ? (
            <div className="border rounded-lg bg-card h-[calc(100vh-10rem)] sm:h-[calc(100vh-9rem)] lg:h-[calc(100vh-8rem)] overflow-hidden">
              <ErrorBoundary>
                <EmailDetailView
                  email={selectedEmail}
                  onBack={handleBack}
                  onDelete={handleDelete}
                />
              </ErrorBoundary>
            </div>
          ) : (
            <div className="border rounded-lg bg-card h-[calc(100vh-10rem)] sm:h-[calc(100vh-9rem)] lg:h-[calc(100vh-8rem)] flex items-center justify-center text-muted-foreground text-sm">
              Select an email to read
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
