import { useState } from 'react'
import { useAtom } from 'jotai'
import { authAtom } from './stores/auth'
import { Sidebar } from './components/desktop/Sidebar'
import { Inbox } from './views/Inbox'
import { Settings } from './views/Settings'
import { Login } from './views/Login'
import { useTheme } from './hooks/useTheme'

type View = 'inbox' | 'sent' | 'settings'

function App() {
  const [auth] = useAtom(authAtom)
  const [currentView, setCurrentView] = useState<View>('inbox')

  // Initialize theme (follows system by default)
  useTheme()

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
        {currentView === 'settings' && <Settings />}
      </main>
    </div>
  )
}

export default App
