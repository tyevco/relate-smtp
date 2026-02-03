import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Mail } from 'lucide-react'

export const Route = createFileRoute('/login')({
  component: Login,
})

function Login() {
  const auth = useAuth()

  // Redirect to dashboard if already authenticated
  useEffect(() => {
    if (!auth.isLoading && auth.isAuthenticated) {
      console.log('🔐 Login: Already authenticated, redirecting to dashboard')
      window.location.href = '/'
    }
  }, [auth.isAuthenticated, auth.isLoading])

  const handleLogin = () => {
    auth.signinRedirect()
  }

  if (auth.isLoading) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="text-lg text-foreground">Loading...</div>
      </div>
    )
  }

  return (
    <div className="relative flex h-screen items-center justify-center bg-background">
      <div className="w-full max-w-md px-4">
        <Card className="shadow-2xl">
          <CardHeader className="text-center space-y-6 pb-8 pt-12">
            <div className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-primary/10">
              <Mail className="h-12 w-12 text-primary" />
            </div>
            <div>
              <CardTitle className="text-4xl font-bold">
                Relate Mail
              </CardTitle>
              <CardDescription className="mt-3 text-base">
                Please sign in to continue
              </CardDescription>
            </div>
          </CardHeader>

          <CardContent className="pb-12">
            <Button
              onClick={handleLogin}
              className="w-full h-12 text-base"
              size="lg"
            >
              Login with OIDC
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
