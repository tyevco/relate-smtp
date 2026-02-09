import { useState, useEffect } from 'react'
import { createFileRoute, useNavigate, useSearch } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useProfile, useSendEmail, useReplyToEmail, useForwardEmail, useEmail } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { Send, ArrowLeft, Plus, X } from 'lucide-react'
import type { RecipientRequest } from '@/api/types'

interface ComposeSearch {
  replyTo?: string
  replyAll?: string
  forwardFrom?: string
}

export const Route = createFileRoute('/compose')({
  component: ComposePage,
  validateSearch: (search: Record<string, unknown>): ComposeSearch => ({
    replyTo: search.replyTo as string | undefined,
    replyAll: search.replyAll as string | undefined,
    forwardFrom: search.forwardFrom as string | undefined,
  }),
})

function ComposePage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const search = useSearch({ from: '/compose' })
  const { data: profile } = useProfile()

  const replyToId = search.replyTo
  const isReplyAll = search.replyAll === 'true'
  const forwardFromId = search.forwardFrom
  const isReply = !!replyToId
  const isForward = !!forwardFromId

  const { data: originalEmail } = useEmail(replyToId || forwardFromId || '')

  const sendEmail = useSendEmail()
  const replyToEmail = useReplyToEmail()
  const forwardEmail = useForwardEmail()

  const [fromAddress, setFromAddress] = useState('')
  const [toRecipients, setToRecipients] = useState<RecipientRequest[]>([{ address: '', type: 'To' }])
  const [ccRecipients, setCcRecipients] = useState<RecipientRequest[]>([])
  const [bccRecipients, setBccRecipients] = useState<RecipientRequest[]>([])
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [showCc, setShowCc] = useState(false)
  const [showBcc, setShowBcc] = useState(false)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Set from address when profile loads
  useEffect(() => {
    if (profile?.email && !fromAddress) {
      setFromAddress(profile.email)
    }
  }, [profile, fromAddress])

  // Populate fields for reply/forward
  useEffect(() => {
    if (!originalEmail) return

    if (isReply) {
      const prefix = originalEmail.subject.startsWith('Re:') ? '' : 'Re: '
      setSubject(`${prefix}${originalEmail.subject}`)
      setToRecipients([{
        address: originalEmail.fromAddress,
        displayName: originalEmail.fromDisplayName || undefined,
        type: 'To',
      }])

      if (isReplyAll) {
        const ccList = originalEmail.recipients
          .filter(r =>
            r.address !== profile?.email &&
            r.address !== originalEmail.fromAddress
          )
          .map(r => ({
            address: r.address,
            displayName: r.displayName || undefined,
            type: 'Cc' as const,
          }))
        if (ccList.length > 0) {
          setCcRecipients(ccList)
          setShowCc(true)
        }
      }

      const quotedBody = `\n\n--- Original Message ---\nFrom: ${originalEmail.fromAddress}\nDate: ${new Date(originalEmail.receivedAt).toLocaleString()}\n\n${originalEmail.textBody || ''}`
      setBody(quotedBody)
    }

    if (isForward) {
      const prefix = originalEmail.subject.startsWith('Fwd:') ? '' : 'Fwd: '
      setSubject(`${prefix}${originalEmail.subject}`)
      setToRecipients([{ address: '', type: 'To' }])

      const forwardedBody = `\n\n--- Forwarded Message ---\nFrom: ${originalEmail.fromAddress}\nTo: ${originalEmail.recipients.filter(r => r.type === 'To').map(r => r.address).join(', ')}\nDate: ${new Date(originalEmail.receivedAt).toLocaleString()}\nSubject: ${originalEmail.subject}\n\n${originalEmail.textBody || ''}`
      setBody(forwardedBody)
    }
  }, [originalEmail, isReply, isReplyAll, isForward, profile?.email])

  // Redirect to login if not authenticated
  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: '/login' })
    }
  }, [auth.isAuthenticated, auth.isLoading, navigate])

  const handleSend = async () => {
    setError(null)

    const allRecipients = [
      ...toRecipients.filter(r => r.address.trim()),
      ...ccRecipients.filter(r => r.address.trim()),
      ...bccRecipients.filter(r => r.address.trim()),
    ]

    if (allRecipients.length === 0) {
      setError('At least one recipient is required')
      return
    }

    if (!subject.trim()) {
      setError('Subject is required')
      return
    }

    setSending(true)

    try {
      if (isReply && replyToId) {
        await replyToEmail.mutateAsync({
          emailId: replyToId,
          data: {
            textBody: body,
            replyAll: isReplyAll,
          },
        })
      } else if (isForward && forwardFromId) {
        await forwardEmail.mutateAsync({
          emailId: forwardFromId,
          data: {
            textBody: body,
            recipients: allRecipients,
          },
        })
      } else {
        await sendEmail.mutateAsync({
          fromAddress,
          fromDisplayName: profile?.displayName || undefined,
          subject,
          textBody: body,
          recipients: allRecipients,
        })
      }

      navigate({ to: '/' })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send email')
    } finally {
      setSending(false)
    }
  }

  const addRecipient = (
    list: RecipientRequest[],
    setList: React.Dispatch<React.SetStateAction<RecipientRequest[]>>,
    type: 'To' | 'Cc' | 'Bcc'
  ) => {
    setList([...list, { address: '', type }])
  }

  const removeRecipient = (
    list: RecipientRequest[],
    setList: React.Dispatch<React.SetStateAction<RecipientRequest[]>>,
    index: number
  ) => {
    setList(list.filter((_, i) => i !== index))
  }

  const updateRecipientAddress = (
    list: RecipientRequest[],
    setList: React.Dispatch<React.SetStateAction<RecipientRequest[]>>,
    index: number,
    address: string
  ) => {
    const updated = [...list]
    updated[index] = { ...updated[index], address }
    setList(updated)
  }

  const renderRecipientInputs = (
    label: string,
    list: RecipientRequest[],
    setList: React.Dispatch<React.SetStateAction<RecipientRequest[]>>,
    type: 'To' | 'Cc' | 'Bcc'
  ) => (
    <div className="flex items-start gap-2">
      <label className="w-12 pt-2 text-sm text-muted-foreground font-medium shrink-0">{label}:</label>
      <div className="flex-1 space-y-2">
        {list.map((recipient, index) => (
          <div key={index} className="flex items-center gap-1">
            <input
              type="email"
              value={recipient.address}
              onChange={(e) => updateRecipientAddress(list, setList, index, e.target.value)}
              placeholder="email@example.com"
              className="flex-1 border rounded-md px-3 py-2 text-sm bg-background focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {(list.length > 1 || type !== 'To') && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 shrink-0"
                onClick={() => removeRecipient(list, setList, index)}
              >
                <X className="h-3 w-3" />
              </Button>
            )}
          </div>
        ))}
        <Button
          variant="ghost"
          size="sm"
          className="text-xs h-7"
          onClick={() => addRecipient(list, setList, type)}
        >
          <Plus className="h-3 w-3 mr-1" />
          Add
        </Button>
      </div>
    </div>
  )

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6 max-w-4xl">
      <div className="flex items-center gap-2 mb-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate({ to: '/' })}
        >
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-xl font-bold">
          {isReply ? (isReplyAll ? 'Reply All' : 'Reply') : isForward ? 'Forward' : 'Compose'}
        </h1>
      </div>

      <div className="border rounded-lg bg-card p-4 sm:p-6 space-y-4">
        {/* From */}
        <div className="flex items-center gap-2">
          <label className="w-12 text-sm text-muted-foreground font-medium shrink-0">From:</label>
          <input
            type="email"
            value={fromAddress}
            onChange={(e) => setFromAddress(e.target.value)}
            className="flex-1 border rounded-md px-3 py-2 text-sm bg-background focus:outline-none focus:ring-2 focus:ring-ring"
            readOnly={isReply || isForward}
          />
        </div>

        {/* To */}
        {renderRecipientInputs('To', toRecipients, setToRecipients, 'To')}

        {/* Cc / Bcc toggles */}
        {!showCc && !showBcc && (
          <div className="flex gap-2 pl-14">
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-7"
              onClick={() => {
                setShowCc(true)
                if (ccRecipients.length === 0) setCcRecipients([{ address: '', type: 'Cc' }])
              }}
            >
              Cc
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-7"
              onClick={() => {
                setShowBcc(true)
                if (bccRecipients.length === 0) setBccRecipients([{ address: '', type: 'Bcc' }])
              }}
            >
              Bcc
            </Button>
          </div>
        )}

        {showCc && renderRecipientInputs('Cc', ccRecipients, setCcRecipients, 'Cc')}
        {showCc && !showBcc && (
          <div className="pl-14">
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-7"
              onClick={() => {
                setShowBcc(true)
                if (bccRecipients.length === 0) setBccRecipients([{ address: '', type: 'Bcc' }])
              }}
            >
              Bcc
            </Button>
          </div>
        )}
        {showBcc && renderRecipientInputs('Bcc', bccRecipients, setBccRecipients, 'Bcc')}

        {/* Subject */}
        <div className="flex items-center gap-2">
          <label className="w-12 text-sm text-muted-foreground font-medium shrink-0">Subj:</label>
          <input
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            placeholder="Subject"
            className="flex-1 border rounded-md px-3 py-2 text-sm bg-background focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>

        {/* Body */}
        <div>
          <textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            placeholder="Write your message..."
            rows={16}
            className="w-full border rounded-md px-3 py-2 text-sm bg-background focus:outline-none focus:ring-2 focus:ring-ring resize-y min-h-[200px]"
          />
        </div>

        {/* Error */}
        {error && (
          <div className="text-sm text-red-500 bg-red-50 dark:bg-red-900/20 rounded-md px-3 py-2">
            {error}
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-between pt-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate({ to: '/' })}
          >
            Cancel
          </Button>
          <Button
            size="sm"
            onClick={handleSend}
            disabled={sending}
          >
            <Send className="h-4 w-4 mr-1" />
            {sending ? 'Sending...' : 'Send'}
          </Button>
        </div>
      </div>
    </div>
  )
}
