import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { useEffect, useState } from 'react'

export const Route = createFileRoute('/callback')({
  component: Callback,
})

function Callback() {
  const auth = useAuth()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const handleCallback = async () => {
      try {
        console.log('🔄 Callback: Auth state', {
          isAuthenticated: auth.isAuthenticated,
          isLoading: auth.isLoading,
          activeNavigator: auth.activeNavigator,
          error: auth.error,
        })

        // Wait for auth to finish loading
        if (auth.isLoading) {
          console.log('⏳ Callback: Still loading...')
          return
        }

        // Check for errors
        if (auth.error) {
          console.error('❌ Callback: Auth error', auth.error)
          setError(auth.error.message)
          return
        }

        // If authenticated, redirect to dashboard
        if (auth.isAuthenticated) {
          console.log('✅ Callback: Authenticated, redirecting to dashboard')
          window.location.href = '/'
        } else {
          console.log('⚠️ Callback: Not authenticated yet, waiting...')
        }
      } catch (err) {
        console.error('❌ Callback: Exception', err)
        setError(err instanceof Error ? err.message : 'Unknown error')
      }
    }

    handleCallback()
  }, [auth.isAuthenticated, auth.isLoading, auth.error, auth.activeNavigator])

  if (error) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="text-center space-y-4">
          <div className="text-lg text-red-600 font-semibold">Authentication Error</div>
          <div className="text-sm text-muted-foreground">{error}</div>
          <button
            onClick={() => window.location.href = '/login'}
            className="px-4 py-2 bg-primary text-primary-foreground rounded"
          >
            Back to Login
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex h-screen items-center justify-center bg-background">
      <div className="text-center space-y-4">
        <div className="text-lg">Processing authentication...</div>
        <div className="text-sm text-muted-foreground">
          {auth.isLoading ? 'Loading...' : auth.isAuthenticated ? 'Redirecting...' : 'Finalizing...'}
        </div>
      </div>
    </div>
  )
}
