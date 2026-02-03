import { useState, useEffect } from 'react'
import { useSetAtom } from 'jotai'
import { invoke } from '@tauri-apps/api/core'
import { loginAtom } from '@/stores/auth'
import { Button, Input, Label, Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@relate/shared/components/ui'

interface Credentials {
  server_url: string
  api_key: string
  user_email: string
}

export function Login() {
  const login = useSetAtom(loginAtom)
  const [serverUrl, setServerUrl] = useState('')
  const [email, setEmail] = useState('')
  const [apiKey, setApiKey] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [isCheckingStored, setIsCheckingStored] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Try to load stored credentials on mount
  useEffect(() => {
    async function loadStoredCredentials() {
      try {
        const credentials = await invoke<Credentials | null>('load_credentials')
        if (credentials) {
          login({
            serverUrl: credentials.server_url,
            apiKey: credentials.api_key,
            userEmail: credentials.user_email,
          })
        }
      } catch (err) {
        console.error('Failed to load credentials:', err)
      } finally {
        setIsCheckingStored(false)
      }
    }
    loadStoredCredentials()
  }, [login])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    setError(null)

    try {
      // Normalize the server URL
      let normalizedUrl = serverUrl.trim()
      if (!normalizedUrl.startsWith('http://') && !normalizedUrl.startsWith('https://')) {
        normalizedUrl = `https://${normalizedUrl}`
      }
      // Remove trailing slash
      normalizedUrl = normalizedUrl.replace(/\/$/, '')

      // Save credentials to secure storage
      await invoke('save_credentials', {
        serverUrl: normalizedUrl,
        apiKey,
        userEmail: email,
      })

      login({
        serverUrl: normalizedUrl,
        apiKey,
        userEmail: email,
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to connect')
    } finally {
      setIsLoading(false)
    }
  }

  if (isCheckingStored) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  return (
    <div className="flex items-center justify-center min-h-screen bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Relate Mail</CardTitle>
          <CardDescription>
            Connect to your Relate Mail server
          </CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit}>
          <CardContent className="space-y-4">
            {error && (
              <div className="p-3 text-sm text-destructive bg-destructive/10 rounded-md">
                {error}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="serverUrl">Server URL</Label>
              <Input
                id="serverUrl"
                type="text"
                placeholder="mail.example.com"
                value={serverUrl}
                onChange={(e) => setServerUrl(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email Address</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="apiKey">API Key</Label>
              <Input
                id="apiKey"
                type="password"
                placeholder="Your API key"
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                required
              />
              <p className="text-xs text-muted-foreground">
                Generate an API key from the web interface under SMTP Settings
              </p>
            </div>
          </CardContent>
          <CardFooter>
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? 'Connecting...' : 'Connect'}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
