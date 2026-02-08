import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { useEffect, useCallback, useMemo } from 'react'

export const Route = createFileRoute('/callback')({
  component: Callback,
})

function Callback() {
  const auth = useAuth()
  const navigate = useNavigate()

  // Derive error message directly from auth.error
  const errorMessage = useMemo(() => auth.error?.message ?? null, [auth.error])

  const handleBackToLogin = useCallback(() => {
    navigate({ to: '/login' })
  }, [navigate])

  // Handle redirects based on auth state
  useEffect(() => {
    // Wait for auth to finish loading
    if (auth.isLoading) {
      return
    }

    // Don't redirect if there's an error
    if (auth.error) {
      return
    }

    // If authenticated, redirect to dashboard
    if (auth.isAuthenticated) {
      navigate({ to: '/' })
    }
  }, [auth.isAuthenticated, auth.isLoading, auth.error, navigate])

  if (errorMessage) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="text-center space-y-4">
          <div className="text-lg text-red-600 font-semibold">Authentication Error</div>
          <div className="text-sm text-muted-foreground">{errorMessage}</div>
          <button
            onClick={handleBackToLogin}
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
