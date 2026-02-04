import { useState } from 'react'
import { useAtom } from 'jotai'
import { authAtom } from './stores/auth'
import { Sidebar } from './components/desktop/Sidebar'
import { Inbox } from './views/Inbox'
import { Settings } from './views/Settings'
import { SmtpSettings } from './views/SmtpSettings'
import { Login } from './views/Login'
import { useTheme } from './hooks/useTheme'
import { usePolling } from './hooks/usePolling'
import { useSignalR } from './hooks/useSignalR'
import { useWindowState } from './hooks/useWindowState'

type View = 'inbox' | 'sent' | 'smtp-settings' | 'settings'

function App() {
  const [auth] = useAtom(authAtom)
  const [currentView, setCurrentView] = useState<View>('inbox')

  // Initialize theme (follows system by default)
  useTheme()

  // Real-time notifications via SignalR (primary)
  useSignalR(auth.isAuthenticated ? auth.serverUrl : null, auth.apiKey)

  // Background polling as fallback for reconnection gaps
  usePolling(auth.isAuthenticated)

  // Persist window size and position
  useWindowState()

  // Show login if not authenticated
  if (!auth.isAuthenticated) {
    return <Login />
  }

  return (
    <div className="flex h-screen bg-background text-foreground">
      <Sidebar currentView={currentView} onNavigate={setCurrentView} />
      <main className="flex-1 overflow-hidden">
        {currentView === 'inbox' && <Inbox />}
        {currentView === 'sent' && (
          <div className="flex items-center justify-center h-full text-muted-foreground">
            Sent emails coming soon
          </div>
        )}
        {currentView === 'smtp-settings' && <SmtpSettings />}
        {currentView === 'settings' && <Settings />}
      </main>
    </div>
  )
}

export default App
