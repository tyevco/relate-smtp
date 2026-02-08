import { useState, useEffect, useRef } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useSmtpCredentials, useCreateSmtpApiKey, useRevokeSmtpApiKey } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Trash2, Plus, Copy, Check, KeyRound } from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'

export const Route = createFileRoute('/smtp-settings')({
  component: SmtpSettingsPage,
})

function SmtpSettingsPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const { data: credentials, isLoading } = useSmtpCredentials()
  const createKey = useCreateSmtpApiKey()
  const revokeKey = useRevokeSmtpApiKey()

  const [isCreatingKey, setIsCreatingKey] = useState(false)

  // Redirect to login if not authenticated
  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: '/login' })
    }
  }, [auth.isAuthenticated, auth.isLoading, navigate])
  const [keyName, setKeyName] = useState('')
  const [selectedScopes, setSelectedScopes] = useState<string[]>(['smtp', 'pop3', 'imap', 'api:read', 'api:write'])
  const [createdKey, setCreatedKey] = useState<{ apiKey: string; name: string; scopes: string[] } | null>(null)
  const [copiedKey, setCopiedKey] = useState(false)
  const copyTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (copyTimeoutRef.current) {
        clearTimeout(copyTimeoutRef.current)
      }
    }
  }, [])

  const scopeOptions = [
    { value: 'smtp', label: 'SMTP', description: 'Send emails via SMTP server' },
    { value: 'pop3', label: 'POP3', description: 'Retrieve emails via POP3 server' },
    { value: 'imap', label: 'IMAP', description: 'Retrieve emails via IMAP server' },
    { value: 'api:read', label: 'API Read', description: 'Read emails via REST API' },
    { value: 'api:write', label: 'API Write', description: 'Modify/delete emails via REST API' }
  ]

  const handleCreateKey = () => {
    if (keyName.trim() && selectedScopes.length > 0) {
      createKey.mutate({ name: keyName.trim(), scopes: selectedScopes }, {
        onSuccess: (data) => {
          setCreatedKey({ apiKey: data.apiKey, name: data.name, scopes: data.scopes })
          setKeyName('')
          setSelectedScopes(['smtp', 'pop3', 'imap', 'api:read', 'api:write'])
          setIsCreatingKey(false)
        },
      })
    }
  }

  const handleCopyKey = async () => {
    if (createdKey) {
      try {
        await navigator.clipboard.writeText(createdKey.apiKey)
        setCopiedKey(true)
        // Clear any existing timeout
        if (copyTimeoutRef.current) {
          clearTimeout(copyTimeoutRef.current)
        }
        copyTimeoutRef.current = setTimeout(() => setCopiedKey(false), 2000)
      } catch {
        // Fallback for browsers without clipboard API or when permission denied
        // eslint-disable-next-line no-alert -- clipboard fallback needs native dialog
        alert('Failed to copy to clipboard. Please copy the key manually.')
      }
    }
  }

  const handleCloseKeyModal = () => {
    setCreatedKey(null)
    setCopiedKey(false)
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-6">
        <div className="text-center text-muted-foreground">Loading...</div>
      </div>
    )
  }

  if (!credentials) {
    return (
      <div className="container mx-auto px-4 py-6">
        <div className="text-center text-muted-foreground">
          Please log in to view SMTP settings
        </div>
      </div>
    )
  }

  const { connectionInfo, keys } = credentials

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6 max-w-4xl">
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">SMTP Settings</h1>

      <div className="space-y-4 sm:space-y-6">
        {/* Connection Information */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg">Connection Information</CardTitle>
            <CardDescription className="text-xs sm:text-sm">
              Use these settings to configure your email client
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              {connectionInfo.smtpEnabled && (
                <>
                  <h4 className="font-medium mb-2 text-sm sm:text-base">Outgoing Mail (SMTP)</h4>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4 mb-4">
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Server</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm break-all">{connectionInfo.smtpServer}</p>
                    </div>
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Port (STARTTLS)</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm">{connectionInfo.smtpPort}</p>
                    </div>
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm">{connectionInfo.smtpSecurePort}</p>
                    </div>
                  </div>
                </>
              )}

              {connectionInfo.pop3Enabled && (
                <>
                  <h4 className="font-medium mb-2 text-sm sm:text-base">Incoming Mail (POP3)</h4>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4 mb-4">
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Server</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm break-all">{connectionInfo.pop3Server}</p>
                    </div>
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Port</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm">{connectionInfo.pop3Port}</p>
                    </div>
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm">{connectionInfo.pop3SecurePort}</p>
                    </div>
                  </div>
                </>
              )}

              {connectionInfo.imapEnabled && (
                <>
                  <h4 className="font-medium mb-2 text-sm sm:text-base">Incoming Mail (IMAP)</h4>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4 mb-4">
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Server</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm break-all">{connectionInfo.imapServer}</p>
                    </div>
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Port</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm">{connectionInfo.imapPort}</p>
                    </div>
                    <div>
                      <label className="text-xs sm:text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                      <p className="mt-1 font-mono text-xs sm:text-sm">{connectionInfo.imapSecurePort}</p>
                    </div>
                  </div>
                </>
              )}

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
                <div>
                  <label className="text-xs sm:text-sm font-medium text-muted-foreground">Username</label>
                  <p className="mt-1 font-mono text-xs sm:text-sm break-all">{connectionInfo.username}</p>
                </div>
                <div>
                  <label className="text-xs sm:text-sm font-medium text-muted-foreground">Password</label>
                  <p className="mt-1 text-xs sm:text-sm text-muted-foreground">Use generated API key</p>
                </div>
              </div>
            </div>

            <div className="border-t pt-4 mt-4">
              <h4 className="text-xs sm:text-sm font-medium mb-2">Setup Instructions</h4>
              <ol className="text-xs sm:text-sm text-muted-foreground space-y-1 list-decimal list-inside">
                <li>Generate an API key below</li>
                <li>Configure your email client with the server and port above</li>
                <li>Use your email address as the username</li>
                <li>Use the generated API key as the password</li>
              </ol>
            </div>
          </CardContent>
        </Card>

        {/* API Keys */}
        <Card>
          <CardHeader>
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 sm:gap-4">
              <div className="flex-1">
                <CardTitle className="text-base sm:text-lg">API Keys</CardTitle>
                <CardDescription className="text-xs sm:text-sm">
                  Manage API keys for SMTP authentication ({keys.length} active)
                </CardDescription>
              </div>
              {!isCreatingKey && (
                <Button onClick={() => setIsCreatingKey(true)} className="w-full sm:w-auto min-h-[44px]">
                  <Plus className="h-4 w-4 mr-1" />
                  Generate Key
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {/* Create key form */}
              {isCreatingKey && (
                <div className="p-3 sm:p-4 rounded border bg-muted/50 space-y-4">
                  <div>
                    <label className="text-xs sm:text-sm font-medium mb-2 block">Key Name</label>
                    <Input
                      value={keyName}
                      onChange={(e) => setKeyName(e.target.value)}
                      placeholder="Key name (e.g., Work Laptop, iPhone)"
                      onKeyDown={(e) => e.key === 'Enter' && selectedScopes.length > 0 && handleCreateKey()}
                      className="text-sm"
                    />
                  </div>

                  <div>
                    <label className="text-xs sm:text-sm font-medium mb-2 block">Permissions</label>
                    <div className="space-y-2">
                      {scopeOptions.map(scope => (
                        <label key={scope.value} className="flex items-start gap-2 cursor-pointer min-h-[44px] py-2">
                          <input
                            type="checkbox"
                            checked={selectedScopes.includes(scope.value)}
                            onChange={(e) => {
                              if (e.target.checked) {
                                setSelectedScopes([...selectedScopes, scope.value])
                              } else {
                                setSelectedScopes(selectedScopes.filter(s => s !== scope.value))
                              }
                            }}
                            className="mt-1 w-4 h-4"
                          />
                          <div className="flex-1">
                            <div className="font-medium text-xs sm:text-sm">{scope.label}</div>
                            <div className="text-xs text-muted-foreground">{scope.description}</div>
                          </div>
                        </label>
                      ))}
                    </div>
                  </div>

                  <div className="flex flex-col sm:flex-row gap-2">
                    <Button onClick={handleCreateKey} disabled={createKey.isPending || !keyName.trim() || selectedScopes.length === 0} className="min-h-[44px]">
                      Create
                    </Button>
                    <Button variant="outline" onClick={() => {
                      setIsCreatingKey(false)
                      setKeyName('')
                      setSelectedScopes(['smtp', 'pop3', 'imap', 'api:read', 'api:write'])
                    }} className="min-h-[44px]">
                      Cancel
                    </Button>
                  </div>
                </div>
              )}

              {/* Key list */}
              {keys.length > 0 ? (
                keys.map((key) => (
                  <div key={key.id} className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-3 sm:p-4 rounded border gap-3">
                    <div className="flex items-start gap-3 flex-1 min-w-0">
                      <KeyRound className="h-5 w-5 text-muted-foreground flex-shrink-0 mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="font-medium text-sm sm:text-base break-words">{key.name}</p>
                        <p className="text-xs sm:text-sm text-muted-foreground">
                          Created {formatDistanceToNow(new Date(key.createdAt), { addSuffix: true })}
                          {key.lastUsedAt && (
                            <> • Last used {formatDistanceToNow(new Date(key.lastUsedAt), { addSuffix: true })}</>
                          )}
                        </p>
                        <div className="flex flex-wrap gap-1 mt-1">
                          {key.scopes.map(scope => (
                            <span key={scope} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">
                              {scope}
                            </span>
                          ))}
                        </div>
                      </div>
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => {
                        // eslint-disable-next-line no-alert -- TODO: replace with confirmation dialog component
                        if (window.confirm(`Revoke API key "${key.name}"? Email clients using this key will stop working.`)) {
                          revokeKey.mutate(key.id)
                        }
                      }}
                      className="min-h-[44px] self-end sm:self-auto"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))
              ) : (
                <p className="text-xs sm:text-sm text-muted-foreground text-center py-8">
                  No API keys yet. Generate one to get started.
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Created Key Modal */}
      <Dialog open={!!createdKey} onOpenChange={handleCloseKeyModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>API Key Created</DialogTitle>
            <DialogDescription>
              Save this API key securely. It cannot be retrieved later.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">Key Name</label>
              <p className="mt-1 text-sm">{createdKey?.name}</p>
            </div>

            <div>
              <label className="text-sm font-medium">Permissions</label>
              <div className="flex gap-1 mt-1">
                {createdKey?.scopes.map(scope => (
                  <span key={scope} className="text-xs bg-primary/10 text-primary px-2 py-1 rounded">
                    {scope}
                  </span>
                ))}
              </div>
            </div>

            <div>
              <label className="text-sm font-medium">API Key</label>
              <div className="mt-1 flex gap-2">
                <Input
                  value={createdKey?.apiKey || ''}
                  readOnly
                  className="font-mono text-sm"
                />
                <Button
                  variant="outline"
                  size="icon"
                  onClick={handleCopyKey}
                >
                  {copiedKey ? (
                    <Check className="h-4 w-4" />
                  ) : (
                    <Copy className="h-4 w-4" />
                  )}
                </Button>
              </div>
            </div>

            <div className="bg-yellow-50 dark:bg-yellow-950/20 border border-yellow-200 dark:border-yellow-900 rounded p-3">
              <p className="text-sm text-yellow-800 dark:text-yellow-200">
                <strong>Important:</strong> This is the only time you'll see this API key.
                Copy it now and store it securely.
              </p>
            </div>

            <div className="bg-muted rounded p-3 space-y-2">
              <p className="text-sm font-medium">Email Client Configuration:</p>
              <div className="text-sm space-y-1">
                {connectionInfo.smtpEnabled && (
                  <>
                    <p className="font-medium">Outgoing (SMTP):</p>
                    <ul className="list-disc list-inside ml-2">
                      <li>Server: {connectionInfo.smtpServer}</li>
                      <li>Port: {connectionInfo.smtpPort} (STARTTLS)</li>
                    </ul>
                  </>
                )}
                {connectionInfo.pop3Enabled && (
                  <>
                    <p className="font-medium mt-2">Incoming (POP3):</p>
                    <ul className="list-disc list-inside ml-2">
                      <li>Server: {connectionInfo.pop3Server}</li>
                      <li>Port: {connectionInfo.pop3Port}</li>
                    </ul>
                  </>
                )}
                {connectionInfo.imapEnabled && (
                  <>
                    <p className="font-medium mt-2">Incoming (IMAP):</p>
                    <ul className="list-disc list-inside ml-2">
                      <li>Server: {connectionInfo.imapServer}</li>
                      <li>Port: {connectionInfo.imapPort}</li>
                    </ul>
                  </>
                )}
                <p className="font-medium mt-2">Authentication:</p>
                <ul className="list-disc list-inside ml-2">
                  <li>Username: {connectionInfo.username}</li>
                  <li>Password: (the API key above)</li>
                </ul>
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button onClick={handleCloseKeyModal}>Done</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
