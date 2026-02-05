import { useState } from 'react'
import { useAtomValue } from 'jotai'
import { Button } from '@relate/shared/components/ui'
import { Inbox, Send, KeyRound, ChevronRight } from 'lucide-react'
import { Avatar } from './Avatar'
import { AccountSwitcher } from './AccountSwitcher'
import { activeAccountAtom } from '@/stores/accounts'
import { getHostname } from '@/lib/utils'

type View = 'inbox' | 'sent' | 'smtp-settings' | 'settings'

interface SidebarProps {
  currentView: View
  onNavigate: (view: View) => void
  onAddAccount: () => void
}

export function Sidebar({ currentView, onNavigate, onAddAccount }: SidebarProps) {
  const activeAccount = useAtomValue(activeAccountAtom)
  const [accountDialogOpen, setAccountDialogOpen] = useState(false)

  const handleOpenSettings = () => {
    onNavigate('settings')
  }

  return (
    <>
      <aside className="w-56 border-r bg-card flex flex-col">
        {/* Header */}
        <div className="p-4 border-b">
          <h1 className="text-lg font-semibold">Relate Mail</h1>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-2 space-y-1">
          <Button
            variant={currentView === 'inbox' ? 'secondary' : 'ghost'}
            className="w-full justify-start"
            onClick={() => onNavigate('inbox')}
          >
            <Inbox className="h-4 w-4 mr-2" />
            Inbox
          </Button>
          <Button
            variant={currentView === 'sent' ? 'secondary' : 'ghost'}
            className="w-full justify-start"
            onClick={() => onNavigate('sent')}
          >
            <Send className="h-4 w-4 mr-2" />
            Sent
          </Button>
          <Button
            variant={currentView === 'smtp-settings' ? 'secondary' : 'ghost'}
            className="w-full justify-start"
            onClick={() => onNavigate('smtp-settings')}
          >
            <KeyRound className="h-4 w-4 mr-2" />
            SMTP Settings
          </Button>
        </nav>

        {/* Account section at bottom */}
        {activeAccount && (
          <div className="p-2 border-t">
            <button
              onClick={() => setAccountDialogOpen(true)}
              className="w-full p-2 rounded-lg hover:bg-accent flex items-center gap-3 transition-colors"
            >
              <Avatar
                name={activeAccount.display_name}
                email={activeAccount.user_email}
                size="sm"
              />
              <div className="flex-1 text-left min-w-0">
                <div className="text-sm font-medium truncate">
                  {activeAccount.display_name || activeAccount.user_email}
                </div>
                <div className="text-xs text-muted-foreground truncate">
                  {getHostname(activeAccount.server_url)}
                </div>
              </div>
              <ChevronRight className="h-4 w-4 text-muted-foreground flex-shrink-0" />
            </button>
          </div>
        )}
      </aside>

      <AccountSwitcher
        open={accountDialogOpen}
        onOpenChange={setAccountDialogOpen}
        onAddAccount={onAddAccount}
        onOpenSettings={handleOpenSettings}
      />
    </>
  )
}
