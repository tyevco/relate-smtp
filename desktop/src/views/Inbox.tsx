import { useState } from 'react'
import { useEmails, useEmail, useMarkEmailRead, useDeleteEmail, useSearchEmails, type EmailSearchFilters } from '@/api/hooks'
import { EmailList, EmailDetailView, SearchBar } from '@relate/shared/components/mail'
import { Button, Badge } from '@relate/shared/components/ui'
import { ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react'
import { useShortcuts } from '@/hooks/useShortcuts'

export function Inbox() {
  const [page, setPage] = useState(1)
  const [selectedEmailId, setSelectedEmailId] = useState<string | null>(null)
  const [searchFilters, setSearchFilters] = useState<EmailSearchFilters>({})
  const isSearching = !!searchFilters.query

  const { data: emailsData, isLoading, refetch } = useEmails(page)
  const { data: searchData, isLoading: isSearchLoading, refetch: refetchSearch } = useSearchEmails(searchFilters, page)
  const { data: selectedEmail } = useEmail(selectedEmailId || '')
  const markRead = useMarkEmailRead()
  const deleteEmail = useDeleteEmail()

  // Keyboard shortcuts
  useShortcuts({
    onRefresh: () => (isSearching ? refetchSearch : refetch)(),
    onDelete: () => {
      if (selectedEmailId) {
        deleteEmail.mutate(selectedEmailId, {
          onSuccess: () => setSelectedEmailId(null),
        })
      }
    },
    onEscape: () => setSelectedEmailId(null),
    onSearch: () => {
      // Focus the search input
      const searchInput = document.querySelector('input[placeholder="Search emails..."]') as HTMLInputElement
      searchInput?.focus()
    },
    onMarkRead: () => {
      if (selectedEmailId) {
        markRead.mutate({ id: selectedEmailId, isRead: true })
      }
    },
    onMarkUnread: () => {
      if (selectedEmailId) {
        markRead.mutate({ id: selectedEmailId, isRead: false })
      }
    },
  })

  const currentData = isSearching ? searchData : emailsData
  const currentLoading = isSearching ? isSearchLoading : isLoading
  const currentRefetch = isSearching ? refetchSearch : refetch

  const handleSearch = (query: string) => {
    setSearchFilters({ query })
    setPage(1)
    setSelectedEmailId(null)
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

  const totalPages = currentData ? Math.ceil(currentData.totalCount / currentData.pageSize) : 1

  return (
    <div className="flex h-full">
      {/* Email List */}
      <div className={`w-96 border-r flex flex-col ${selectedEmailId ? 'hidden lg:flex' : 'flex'}`}>
        <div className="p-4 border-b">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold">
                {isSearching ? 'Search Results' : 'Inbox'}
              </h2>
              {currentData?.unreadCount !== undefined && currentData.unreadCount > 0 && (
                <Badge variant="secondary">{currentData.unreadCount} unread</Badge>
              )}
            </div>
            <Button variant="ghost" size="icon" onClick={() => currentRefetch()}>
              <RefreshCw className={`h-4 w-4 ${currentLoading ? 'animate-spin' : ''}`} />
            </Button>
          </div>

          <SearchBar onSearch={handleSearch} initialValue={searchFilters.query} />
        </div>

        <div className="flex-1 overflow-auto">
          {currentLoading ? (
            <div className="p-8 text-center text-muted-foreground">
              Loading...
            </div>
          ) : (
            <EmailList
              emails={currentData?.items || []}
              selectedId={selectedEmailId || undefined}
              onSelect={handleSelectEmail}
            />
          )}
        </div>

        {currentData && totalPages > 1 && (
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
            Select an email to read
          </div>
        )}
      </div>
    </div>
  )
}
