import { useState } from 'react'
import { createRootRoute, Link, Outlet, useLocation } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { Mail, User, LogOut, Menu, X, PenSquare } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/hooks/use-theme'

export const Route = createRootRoute({
  component: RootComponent,
})

function RootComponent() {
  const auth = useAuth()
  const location = useLocation()
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  useTheme() // Apply theme from user preferences

  // Don't show navigation on login/callback pages
  const isAuthPage = location.pathname === '/login' || location.pathname === '/callback'
  const showNavigation = auth.isAuthenticated && !isAuthPage

  const handleLogout = () => {
    auth.signoutRedirect()
  }

  const closeMobileMenu = () => {
    setMobileMenuOpen(false)
  }

  return (
    <div className="min-h-screen flex flex-col">
      {showNavigation && (
        <header className="border-b bg-background sticky top-0 z-50">
          <div className="container mx-auto px-4">
            <div className="flex h-14 items-center justify-between">
              <div className="flex items-center gap-4 lg:gap-6">
                <Link to="/" className="flex items-center gap-2 font-semibold">
                  <Mail className="h-5 w-5" />
                  <span className="hidden sm:inline">Relate Mail</span>
                </Link>

                {/* Desktop Navigation */}
                <nav className="hidden lg:flex items-center gap-4">
                  <Link
                    to="/compose"
                    className="flex items-center gap-1 text-sm font-medium bg-primary text-primary-foreground px-3 py-1.5 rounded-md hover:bg-primary/90 transition-colors"
                  >
                    <PenSquare className="h-3.5 w-3.5" />
                    Compose
                  </Link>
                  <Link
                    to="/"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Inbox
                  </Link>
                  <Link
                    to="/drafts"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Drafts
                  </Link>
                  <Link
                    to="/sent"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Sent Mail
                  </Link>
                  <Link
                    to="/outbox"
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors [&.active]:text-foreground"
                  >
                    Outbox
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
                  <span className="text-sm text-muted-foreground mr-2 hidden md:inline">
                    {auth.user.profile.name}
                  </span>
                )}
                <Button variant="ghost" size="icon" className="hidden md:flex" title="Profile">
                  <User className="h-4 w-4" />
                </Button>
                <Button variant="ghost" size="icon" className="hidden lg:flex" onClick={handleLogout} title="Logout">
                  <LogOut className="h-4 w-4" />
                </Button>

                {/* Mobile Menu Button */}
                <Button
                  variant="ghost"
                  size="icon"
                  className="lg:hidden"
                  onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
                  aria-label="Toggle menu"
                >
                  {mobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
                </Button>
              </div>
            </div>

            {/* Mobile Navigation Drawer */}
            {mobileMenuOpen && (
              <div className="lg:hidden border-t py-4 space-y-2">
                <Link
                  to="/compose"
                  onClick={closeMobileMenu}
                  className="flex items-center gap-2 mx-4 mb-2 px-4 py-2 text-sm font-medium bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors justify-center"
                >
                  <PenSquare className="h-3.5 w-3.5" />
                  Compose
                </Link>
                <Link
                  to="/"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  Inbox
                </Link>
                <Link
                  to="/drafts"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  Drafts
                </Link>
                <Link
                  to="/sent"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  Sent Mail
                </Link>
                <Link
                  to="/outbox"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  Outbox
                </Link>
                <Link
                  to="/profile"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  Profile
                </Link>
                <Link
                  to="/smtp-settings"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  SMTP Settings
                </Link>
                <Link
                  to="/preferences"
                  onClick={closeMobileMenu}
                  className="block px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors [&.active]:text-foreground [&.active]:bg-accent"
                >
                  Preferences
                </Link>
                <div className="border-t pt-2 mt-2">
                  <button
                    onClick={() => {
                      handleLogout()
                      closeMobileMenu()
                    }}
                    className="w-full flex items-center gap-2 px-4 py-2 text-sm text-muted-foreground hover:text-foreground hover:bg-accent rounded-md transition-colors"
                  >
                    <LogOut className="h-4 w-4" />
                    Logout
                  </button>
                </div>
              </div>
            )}
          </div>
        </header>
      )}
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
