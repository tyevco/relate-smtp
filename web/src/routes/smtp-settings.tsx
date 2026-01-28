import { useState, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
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
  const { data: credentials, isLoading } = useSmtpCredentials()
  const createKey = useCreateSmtpApiKey()
  const revokeKey = useRevokeSmtpApiKey()

  const [isCreatingKey, setIsCreatingKey] = useState(false)

  // Redirect to login if not authenticated
  useEffect(() => {
    const authority = import.meta.env.VITE_OIDC_AUTHORITY
    if (authority && !auth.isLoading && !auth.isAuthenticated) {
      window.location.href = '/login'
    }
  }, [auth.isAuthenticated, auth.isLoading])
  const [keyName, setKeyName] = useState('')
  const [createdKey, setCreatedKey] = useState<{ apiKey: string; name: string } | null>(null)
  const [copiedKey, setCopiedKey] = useState(false)

  const handleCreateKey = () => {
    if (keyName.trim()) {
      createKey.mutate({ name: keyName.trim() }, {
        onSuccess: (data) => {
          setCreatedKey({ apiKey: data.apiKey, name: data.name })
          setKeyName('')
          setIsCreatingKey(false)
        },
      })
    }
  }

  const handleCopyKey = async () => {
    if (createdKey) {
      await navigator.clipboard.writeText(createdKey.apiKey)
      setCopiedKey(true)
      setTimeout(() => setCopiedKey(false), 2000)
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
    <div className="container mx-auto px-4 py-6 max-w-4xl">
      <h1 className="text-2xl font-bold mb-6">SMTP Settings</h1>

      <div className="space-y-6">
        {/* Connection Information */}
        <Card>
          <CardHeader>
            <CardTitle>Connection Information</CardTitle>
            <CardDescription>
              Use these settings to configure your email client
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium text-muted-foreground">Server</label>
                <p className="mt-1 font-mono text-sm">{connectionInfo.server}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Port (STARTTLS)</label>
                <p className="mt-1 font-mono text-sm">{connectionInfo.port}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                <p className="mt-1 font-mono text-sm">{connectionInfo.securePort}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Username</label>
                <p className="mt-1 font-mono text-sm">{connectionInfo.username}</p>
              </div>
            </div>

            <div className="border-t pt-4 mt-4">
              <h4 className="text-sm font-medium mb-2">Setup Instructions</h4>
              <ol className="text-sm text-muted-foreground space-y-1 list-decimal list-inside">
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
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>API Keys</CardTitle>
                <CardDescription>
                  Manage API keys for SMTP authentication ({keys.length} active)
                </CardDescription>
              </div>
              {!isCreatingKey && (
                <Button onClick={() => setIsCreatingKey(true)}>
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
                <div className="flex gap-2 p-4 rounded border bg-muted/50">
                  <Input
                    value={keyName}
                    onChange={(e) => setKeyName(e.target.value)}
                    placeholder="Key name (e.g., Work Laptop, iPhone)"
                    onKeyDown={(e) => e.key === 'Enter' && handleCreateKey()}
                  />
                  <Button onClick={handleCreateKey} disabled={createKey.isPending || !keyName.trim()}>
                    Create
                  </Button>
                  <Button variant="outline" onClick={() => setIsCreatingKey(false)}>
                    Cancel
                  </Button>
                </div>
              )}

              {/* Key list */}
              {keys.length > 0 ? (
                keys.map((key) => (
                  <div key={key.id} className="flex items-center justify-between p-4 rounded border">
                    <div className="flex items-center gap-3">
                      <KeyRound className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <p className="font-medium">{key.name}</p>
                        <p className="text-sm text-muted-foreground">
                          Created {formatDistanceToNow(new Date(key.createdAt), { addSuffix: true })}
                          {key.lastUsedAt && (
                            <> • Last used {formatDistanceToNow(new Date(key.lastUsedAt), { addSuffix: true })}</>
                          )}
                        </p>
                      </div>
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => {
                        if (confirm(`Revoke API key "${key.name}"? Email clients using this key will stop working.`)) {
                          revokeKey.mutate(key.id)
                        }
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))
              ) : (
                <p className="text-sm text-muted-foreground text-center py-8">
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
              <ul className="text-sm space-y-1 list-disc list-inside">
                <li>Server: {connectionInfo.server}</li>
                <li>Port: {connectionInfo.port} (STARTTLS)</li>
                <li>Username: {connectionInfo.username}</li>
                <li>Password: (the API key above)</li>
              </ul>
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
