import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { useEffect, useState, useCallback } from 'react'

export const Route = createFileRoute('/callback')({
  component: Callback,
})

function Callback() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const handleBackToLogin = useCallback(() => {
    navigate({ to: '/login' })
  }, [navigate])

  useEffect(() => {
    // Wait for auth to finish loading
    if (auth.isLoading) {
      return
    }

    // Check for errors
    if (auth.error) {
      setError(auth.error.message)
      return
    }

    // If authenticated, redirect to dashboard
    if (auth.isAuthenticated) {
      navigate({ to: '/' })
    }
  }, [auth.isAuthenticated, auth.isLoading, auth.error, navigate])

  if (error) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="text-center space-y-4">
          <div className="text-lg text-red-600 font-semibold">Authentication Error</div>
          <div className="text-sm text-muted-foreground">{error}</div>
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
