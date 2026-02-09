import { useState, useEffect } from 'react'
import { useSetAtom, useAtomValue } from 'jotai'
import { invoke } from '@tauri-apps/api/core'
import { Loader2 } from 'lucide-react'
import {
  Button,
  Input,
  Label,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from '@relate/shared/components/ui'
import {
  addAccountAtom,
  loadAccountsAtom,
  accountsLoadedAtom,
  hasAccountsAtom,
  generateAccountId,
  type Account,
} from '@/stores/accounts'

interface OidcConfig {
  authority: string
  client_id: string
  scopes: string | null
}

interface ServerDiscovery {
  discovery: unknown
  oidc_config: OidcConfig | null
}

interface TokenResponse {
  access_token: string
  id_token: string | null
  refresh_token: string | null
  expires_in: number | null
  token_type: string | null
}

interface UserProfile {
  id: string
  email: string
  display_name: string | null
}

interface ApiKeyResponse {
  id: string
  name: string
  apiKey: string
  scopes: string[] | null
  createdAt: string
}

type Step = 'url' | 'discovering' | 'authenticating' | 'creating-key'

const stepMessages: Record<Exclude<Step, 'url'>, string> = {
  discovering: 'Connecting to server...',
  authenticating: 'Waiting for authentication...',
  'creating-key': 'Setting up your account...',
}

interface LoginProps {
  onLoginComplete?: () => void
}

export function Login({ onLoginComplete }: LoginProps) {
  const addAccount = useSetAtom(addAccountAtom)
  const loadAccounts = useSetAtom(loadAccountsAtom)
  const accountsLoaded = useAtomValue(accountsLoadedAtom)
  const hasAccounts = useAtomValue(hasAccountsAtom)
  const [serverUrl, setServerUrl] = useState('')
  const [step, setStep] = useState<Step>('url')
  const [isInitializing, setIsInitializing] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function initialize() {
      try {
        await loadAccounts()
      } catch (err) {
        console.error('Failed to load accounts:', err)
      } finally {
        setIsInitializing(false)
      }
    }

    if (!accountsLoaded) {
      initialize()
    } else {
      setIsInitializing(false)
    }
  }, [loadAccounts, accountsLoaded])

  // If accounts are loaded and user has accounts, trigger callback
  useEffect(() => {
    if (accountsLoaded && hasAccounts && onLoginComplete) {
      onLoginComplete()
    }
  }, [accountsLoaded, hasAccounts, onLoginComplete])

  const handleConnect = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Normalize URL
    let normalizedUrl = serverUrl.trim()
    if (!normalizedUrl.startsWith('http://') && !normalizedUrl.startsWith('https://')) {
      normalizedUrl = `https://${normalizedUrl}`
    }
    normalizedUrl = normalizedUrl.replace(/\/$/, '')

    // Validate URL format
    let parsedUrl: URL
    try {
      parsedUrl = new URL(normalizedUrl)
      if (parsedUrl.protocol !== 'https:' && parsedUrl.protocol !== 'http:') {
        throw new Error('Invalid protocol')
      }
    } catch {
      setError('Please enter a valid server URL (e.g., mail.example.com)')
      return
    }

    try {
      // Step 1: Discover server
      setStep('discovering')
      const discovery = await invoke<ServerDiscovery>('discover_server', {
        serverUrl: normalizedUrl,
      })

      if (!discovery.oidc_config) {
        throw new Error('This server does not have OIDC authentication enabled.')
      }

      const { authority, client_id, scopes } = discovery.oidc_config

      // Validate authority URL
      try {
        const authorityUrl = new URL(authority)
        if (authorityUrl.protocol !== 'https:') {
          throw new Error('OIDC authority must use HTTPS')
        }
      } catch {
        throw new Error('Invalid OIDC configuration from server')
      }

      // Step 2: OIDC authentication
      setStep('authenticating')
      const tokens = await invoke<TokenResponse>('start_oidc_auth', {
        authority,
        clientId: client_id,
        scopes,
      })

      // Step 3: Fetch profile and create API key
      setStep('creating-key')
      const profile = await invoke<UserProfile>('fetch_profile_with_jwt', {
        serverUrl: normalizedUrl,
        jwtToken: tokens.access_token,
      })

      const apiKeyResp = await invoke<ApiKeyResponse>('create_api_key_with_jwt', {
        serverUrl: normalizedUrl,
        jwtToken: tokens.access_token,
        deviceName: 'Relate Mail Desktop',
        platform: navigator.userAgent.includes('Mac') ? 'macos' : navigator.userAgent.includes('Linux') ? 'linux' : 'windows',
      })

      // Step 4: Generate account ID and save account
      const accountId = await generateAccountId()
      const now = new Date().toISOString()

      const account: Account = {
        id: accountId,
        display_name: profile.display_name ?? profile.email,
        server_url: normalizedUrl,
        user_email: profile.email,
        api_key_id: apiKeyResp.id,
        scopes: apiKeyResp.scopes ?? ['smtp', 'pop3', 'imap', 'api:read', 'api:write'],
        created_at: now,
        last_used_at: now,
      }

      await addAccount({ account, apiKey: apiKeyResp.apiKey })

      // Notify parent that login is complete
      if (onLoginComplete) {
        onLoginComplete()
      }
    } catch (err) {
      setError(typeof err === 'string' ? err : err instanceof Error ? err.message : 'Connection failed')
      setStep('url')
    }
  }

  if (isInitializing) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  // If user has accounts, don't show login (App.tsx will handle this)
  if (hasAccounts) {
    return null
  }

  return (
    <div className="flex items-center justify-center min-h-screen bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Relate Mail</CardTitle>
          <CardDescription>Connect to your Relate Mail server</CardDescription>
        </CardHeader>
        {step === 'url' ? (
          <form onSubmit={handleConnect}>
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
                <p className="text-xs text-muted-foreground">
                  You'll be redirected to your organization's login page
                </p>
              </div>
            </CardContent>
            <CardFooter>
              <Button type="submit" className="w-full">
                Connect
              </Button>
            </CardFooter>
          </form>
        ) : (
          <CardContent className="flex flex-col items-center gap-3 py-8">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            <p className="text-sm text-muted-foreground">{stepMessages[step]}</p>
          </CardContent>
        )}
      </Card>
    </div>
  )
}
