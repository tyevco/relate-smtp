import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useState, useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useSentEmails, useSentFromAddresses } from '../api/hooks'
import { EmailList } from '../components/mail/email-list'
import { Mail, Filter } from 'lucide-react'

export const Route = createFileRoute('/sent')({
  component: SentMailPage,
})

function SentMailPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [selectedFromAddress, setSelectedFromAddress] = useState<string | undefined>()
  const [page, setPage] = useState(1)

  const { data: addresses } = useSentFromAddresses()
  const { data: emails, isLoading } = useSentEmails(selectedFromAddress, page, 20)

  // Redirect to login if not authenticated
  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      window.location.href = '/login'
    }
  }, [auth.isAuthenticated, auth.isLoading])

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
      <div className="flex items-center justify-between mb-4 sm:mb-6">
        <h1 className="text-xl sm:text-2xl font-bold flex items-center gap-2">
          <Mail className="h-5 w-5 sm:h-6 sm:w-6" />
          <span className="hidden sm:inline">Sent Mail</span>
          <span className="sm:hidden">Sent</span>
        </h1>
      </div>

      {/* From Address Filter */}
      {addresses && addresses.length > 0 && (
        <div className="mb-4 flex flex-col sm:flex-row items-start sm:items-center gap-2">
          <Filter className="h-4 w-4 text-gray-500" />
          <select
            value={selectedFromAddress || 'all'}
            onChange={(e) => {
              setSelectedFromAddress(e.target.value === 'all' ? undefined : e.target.value)
              setPage(1)
            }}
            className="w-full sm:w-auto border rounded-md px-3 py-2 text-sm"
          >
            <option value="all">All sender addresses ({emails?.totalCount || 0})</option>
            {addresses.map((address) => (
              <option key={address} value={address}>
                {address}
              </option>
            ))}
          </select>
        </div>
      )}

      {/* Email List */}
      {isLoading ? (
        <div className="text-center py-8 text-sm">Loading...</div>
      ) : emails && emails.items.length > 0 ? (
        <>
          <div className="border rounded-lg bg-card">
            <EmailList
              emails={emails.items}
              onSelect={(id) => navigate({ to: `/emails/${id}` })}
            />
          </div>

          {/* Pagination */}
          {emails.totalCount > 20 && (
            <div className="mt-4 flex flex-wrap justify-center items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 sm:px-4 py-2 border rounded disabled:opacity-50 text-xs sm:text-sm min-h-[44px]"
              >
                Previous
              </button>
              <span className="px-2 sm:px-4 py-2 text-xs sm:text-sm">
                Page {page} of {Math.ceil(emails.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={page >= Math.ceil(emails.totalCount / 20)}
                className="px-3 sm:px-4 py-2 border rounded disabled:opacity-50 text-xs sm:text-sm min-h-[44px]"
              >
                Next
              </button>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-8 text-gray-500 text-sm">
          {selectedFromAddress
            ? `No emails sent from ${selectedFromAddress}`
            : 'No sent emails'}
        </div>
      )}
    </div>
  )
}
