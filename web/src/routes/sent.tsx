import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { useSentEmails, useSentFromAddresses } from '../api/hooks'
import { EmailList } from '../components/mail/email-list'
import { Mail, Filter } from 'lucide-react'

export const Route = createFileRoute('/sent')({
  component: SentMailPage,
})

function SentMailPage() {
  const [selectedFromAddress, setSelectedFromAddress] = useState<string | undefined>()
  const [page, setPage] = useState(1)

  const { data: addresses } = useSentFromAddresses()
  const { data: emails, isLoading } = useSentEmails(selectedFromAddress, page, 20)

  return (
    <div className="container mx-auto p-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold flex items-center gap-2">
          <Mail className="h-6 w-6" />
          Sent Mail
        </h1>
      </div>

      {/* From Address Filter */}
      {addresses && addresses.length > 0 && (
        <div className="mb-4 flex items-center gap-2">
          <Filter className="h-4 w-4 text-gray-500" />
          <select
            value={selectedFromAddress || 'all'}
            onChange={(e) => {
              setSelectedFromAddress(e.target.value === 'all' ? undefined : e.target.value)
              setPage(1)
            }}
            className="border rounded-md px-3 py-2"
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
        <div className="text-center py-8">Loading...</div>
      ) : emails && emails.items.length > 0 ? (
        <>
          <EmailList emails={emails.items} />

          {/* Pagination */}
          {emails.totalCount > 20 && (
            <div className="mt-4 flex justify-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-4 py-2 border rounded disabled:opacity-50"
              >
                Previous
              </button>
              <span className="px-4 py-2">
                Page {page} of {Math.ceil(emails.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={page >= Math.ceil(emails.totalCount / 20)}
                className="px-4 py-2 border rounded disabled:opacity-50"
              >
                Next
              </button>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-8 text-gray-500">
          {selectedFromAddress
            ? `No emails sent from ${selectedFromAddress}`
            : 'No sent emails'}
        </div>
      )}
    </div>
  )
}
