import { createRootRoute, Link, Outlet, useLocation } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { Mail, User, LogOut, Send } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/hooks/use-theme'

export const Route = createRootRoute({
  component: RootComponent,
})

function RootComponent() {
  const auth = useAuth()
  const location = useLocation()
  useTheme() // Apply theme from user preferences

  // Don't show navigation on login/callback pages
  const isAuthPage = location.pathname === '/login' || location.pathname === '/callback'
  const showNavigation = auth.isAuthenticated && !isAuthPage

  const handleLogout = () => {
    auth.signoutRedirect()
  }

  return (
    <div className="min-h-screen flex flex-col">
      {showNavigation && (
        <header className="border-b bg-background">
          <div className="container mx-auto px-4">
            <div className="flex h-14 items-center justify-between">
              <div className="flex items-center gap-6">
                <Link to="/" className="flex items-center gap-2 font-semibold">
                  <Mail className="h-5 w-5" />
                  <span>Relate SMTP</span>
                </Link>
                <nav className="flex items-center gap-4">
                  <Link
                    to="/"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Inbox
                  </Link>
                  <Link
                    to="/sent"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Sent Mail
                  </Link>
                  <Link
                    to="/profile"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Profile
                  </Link>
                  <Link
                    to="/smtp-settings"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    SMTP Settings
                  </Link>
                  <Link
                    to="/preferences"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Preferences
                  </Link>
                </nav>
              </div>
              <div className="flex items-center gap-2">
                {auth.user?.profile?.name && (
                  <span className="text-sm text-muted-foreground mr-2">
                    {auth.user.profile.name}
                  </span>
                )}
                <Button variant="ghost" size="icon" title="Profile">
                  <User className="h-4 w-4" />
                </Button>
                <Button variant="ghost" size="icon" onClick={handleLogout} title="Logout">
                  <LogOut className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        </header>
      )}
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
