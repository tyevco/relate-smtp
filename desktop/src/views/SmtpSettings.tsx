import { useState } from 'react'
import { useSmtpCredentials, useCreateSmtpApiKey, useRevokeSmtpApiKey } from '../api/hooks'
import { Button } from '@relate/shared/components/ui'
import { Input } from '@relate/shared/components/ui'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@relate/shared/components/ui'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogFooter } from '@relate/shared/components/ui'
import { Trash2, Plus, Copy, Check, KeyRound } from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'

export function SmtpSettings() {
  const { data: credentials, isLoading } = useSmtpCredentials()
  const createKey = useCreateSmtpApiKey()
  const revokeKey = useRevokeSmtpApiKey()

  const [isCreatingKey, setIsCreatingKey] = useState(false)
  const [keyName, setKeyName] = useState('')
  const [selectedScopes, setSelectedScopes] = useState<string[]>(['smtp', 'pop3', 'imap', 'api:read', 'api:write'])
  const [createdKey, setCreatedKey] = useState<{ apiKey: string; name: string; scopes: string[] } | null>(null)
  const [copiedKey, setCopiedKey] = useState(false)

  const scopeOptions = [
    { value: 'smtp', label: 'SMTP', description: 'Send emails via SMTP server' },
    { value: 'pop3', label: 'POP3', description: 'Retrieve emails via POP3 server' },
    { value: 'imap', label: 'IMAP', description: 'Retrieve emails via IMAP server' },
    { value: 'api:read', label: 'API Read', description: 'Read emails via REST API' },
    { value: 'api:write', label: 'API Write', description: 'Modify/delete emails via REST API' },
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
      <div className="flex items-center justify-center h-full text-muted-foreground">
        Loading...
      </div>
    )
  }

  if (!credentials) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground">
        Unable to load SMTP settings
      </div>
    )
  }

  const { connectionInfo, keys } = credentials

  return (
    <div className="h-full overflow-auto p-6 max-w-4xl">
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
            {connectionInfo.smtpEnabled && (
              <>
                <h4 className="font-medium mb-2">Outgoing Mail (SMTP)</h4>
                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Server</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.smtpServer}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Port (STARTTLS)</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.smtpPort}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.smtpSecurePort}</p>
                  </div>
                </div>
              </>
            )}

            {connectionInfo.pop3Enabled && (
              <>
                <h4 className="font-medium mb-2">Incoming Mail (POP3)</h4>
                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Server</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.pop3Server}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Port</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.pop3Port}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.pop3SecurePort}</p>
                  </div>
                </div>
              </>
            )}

            {connectionInfo.imapEnabled && (
              <>
                <h4 className="font-medium mb-2">Incoming Mail (IMAP)</h4>
                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Server</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.imapServer}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Port</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.imapPort}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Secure Port (SSL/TLS)</label>
                    <p className="mt-1 font-mono text-sm">{connectionInfo.imapSecurePort}</p>
                  </div>
                </div>
              </>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium text-muted-foreground">Username</label>
                <p className="mt-1 font-mono text-sm">{connectionInfo.username}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Password</label>
                <p className="mt-1 text-sm text-muted-foreground">Use generated API key</p>
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
                  Manage API keys for authentication ({keys.length} active)
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
              {isCreatingKey && (
                <div className="p-4 rounded border bg-muted/50 space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">Key Name</label>
                    <Input
                      value={keyName}
                      onChange={(e) => setKeyName(e.target.value)}
                      placeholder="Key name (e.g., Work Laptop, iPhone)"
                      onKeyDown={(e) => e.key === 'Enter' && selectedScopes.length > 0 && handleCreateKey()}
                    />
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">Permissions</label>
                    <div className="space-y-2">
                      {scopeOptions.map(scope => (
                        <label key={scope.value} className="flex items-start gap-2 cursor-pointer py-1">
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
                          <div>
                            <div className="font-medium text-sm">{scope.label}</div>
                            <div className="text-xs text-muted-foreground">{scope.description}</div>
                          </div>
                        </label>
                      ))}
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button onClick={handleCreateKey} disabled={createKey.isPending || !keyName.trim() || selectedScopes.length === 0}>
                      Create
                    </Button>
                    <Button variant="outline" onClick={() => {
                      setIsCreatingKey(false)
                      setKeyName('')
                      setSelectedScopes(['smtp', 'pop3', 'imap', 'api:read', 'api:write'])
                    }}>
                      Cancel
                    </Button>
                  </div>
                </div>
              )}

              {keys.length > 0 ? (
                keys.map((key) => (
                  <div key={key.id} className="flex items-center justify-between p-4 rounded border">
                    <div className="flex items-start gap-3">
                      <KeyRound className="h-5 w-5 text-muted-foreground flex-shrink-0 mt-0.5" />
                      <div>
                        <p className="font-medium">{key.name}</p>
                        <p className="text-sm text-muted-foreground">
                          Created {formatDistanceToNow(new Date(key.createdAt), { addSuffix: true })}
                          {key.lastUsedAt && (
                            <> &middot; Last used {formatDistanceToNow(new Date(key.lastUsedAt), { addSuffix: true })}</>
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
                <Button variant="outline" size="icon" onClick={handleCopyKey}>
                  {copiedKey ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
            </div>

            <div className="bg-yellow-50 dark:bg-yellow-950/20 border border-yellow-200 dark:border-yellow-900 rounded p-3">
              <p className="text-sm text-yellow-800 dark:text-yellow-200">
                <strong>Important:</strong> This is the only time you'll see this API key.
                Copy it now and store it securely.
              </p>
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
