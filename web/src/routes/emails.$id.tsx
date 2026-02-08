import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useEmail } from '../api/hooks'
import { EmailDetail } from '../components/mail/email-detail'
import { ArrowLeft } from 'lucide-react'

export const Route = createFileRoute('/emails/$id')({
  component: EmailDetailPage,
})

function EmailDetailPage() {
  const { id } = Route.useParams()
  const navigate = useNavigate()
  const { data: email, isLoading, error } = useEmail(id)

  if (isLoading) {
    return (
      <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
        <div className="text-center py-8 text-sm">Loading email...</div>
      </div>
    )
  }

  if (error || !email) {
    return (
      <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
        <button
          onClick={() => navigate({ to: '/' })}
          className="mb-4 flex items-center gap-2 text-blue-600 hover:text-blue-800 min-h-[44px] px-2"
        >
          <ArrowLeft className="h-4 w-4" />
          <span className="text-sm sm:text-base">Back to Inbox</span>
        </button>
        <div className="text-center py-8 text-red-600 text-sm">
          Email not found or error loading email.
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6">
      <button
        onClick={() => navigate({ to: '/' })}
        className="mb-4 flex items-center gap-2 text-blue-600 hover:text-blue-800 min-h-[44px] px-2"
      >
        <ArrowLeft className="h-4 w-4" />
        <span className="text-sm sm:text-base">Back</span>
      </button>
      <EmailDetail email={email} />
    </div>
  )
}
