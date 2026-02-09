import { useState, useEffect } from 'react'
import { useAtomValue, useSetAtom } from 'jotai'
import { Sidebar } from './components/desktop/Sidebar'
import { Inbox } from './views/Inbox'
import { Sent } from './views/Sent'
import { Settings } from './views/Settings'
import { SmtpSettings } from './views/SmtpSettings'
import { Login } from './views/Login'
import { useTheme } from './hooks/useTheme'
import { usePolling } from './hooks/usePolling'
import { useSignalR } from './hooks/useSignalR'
import { useWindowState } from './hooks/useWindowState'
import {
  loadAccountsAtom,
  accountsLoadedAtom,
  hasAccountsAtom,
  activeAccountAtom,
  getAccountApiKey,
} from './stores/accounts'

type View = 'inbox' | 'sent' | 'smtp-settings' | 'settings'

function App() {
  const loadAccounts = useSetAtom(loadAccountsAtom)
  const accountsLoaded = useAtomValue(accountsLoadedAtom)
  const hasAccounts = useAtomValue(hasAccountsAtom)
  const activeAccount = useAtomValue(activeAccountAtom)
  const [currentView, setCurrentView] = useState<View>('inbox')
  const [showAddAccount, setShowAddAccount] = useState(false)
  const [apiKey, setApiKey] = useState<string | null>(null)

  // Initialize theme (follows system by default)
  useTheme()

  // Load accounts on mount
  useEffect(() => {
    if (!accountsLoaded) {
      loadAccounts()
    }
  }, [loadAccounts, accountsLoaded])

  // Load API key when active account changes
  useEffect(() => {
    async function loadApiKey() {
      if (activeAccount) {
        const key = await getAccountApiKey(activeAccount.id)
        setApiKey(key)
      } else {
        setApiKey(null)
      }
    }
    loadApiKey()
  }, [activeAccount])

  // Real-time notifications via SignalR (primary)
  useSignalR(hasAccounts && activeAccount ? activeAccount.server_url : null, apiKey)

  // Background polling as fallback for reconnection gaps
  usePolling(hasAccounts)

  // Persist window size and position
  useWindowState()

  // Show loading state while initializing
  if (!accountsLoaded) {
    return (
      <div className="flex items-center justify-center h-screen bg-background text-foreground">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  // Show login if not authenticated or adding account
  if (!hasAccounts || showAddAccount) {
    return (
      <Login
        onLoginComplete={() => {
          setShowAddAccount(false)
        }}
      />
    )
  }

  const handleAddAccount = () => {
    setShowAddAccount(true)
  }

  return (
    <div className="flex h-screen bg-background text-foreground">
      <Sidebar
        currentView={currentView}
        onNavigate={setCurrentView}
        onAddAccount={handleAddAccount}
      />
      <main className="flex-1 overflow-hidden">
        {currentView === 'inbox' && <Inbox />}
        {currentView === 'sent' && <Sent />}
        {currentView === 'smtp-settings' && <SmtpSettings />}
        {currentView === 'settings' && <Settings />}
      </main>
    </div>
  )
}

export default App
