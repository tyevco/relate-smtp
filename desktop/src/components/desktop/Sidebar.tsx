import { Button } from '@relate/shared/components/ui'
import { Inbox, Send, Settings, LogOut } from 'lucide-react'
import { useSetAtom } from 'jotai'
import { logoutAtom } from '@/stores/auth'

type View = 'inbox' | 'sent' | 'settings'

interface SidebarProps {
  currentView: View
  onNavigate: (view: View) => void
}

export function Sidebar({ currentView, onNavigate }: SidebarProps) {
  const logout = useSetAtom(logoutAtom)

  return (
    <aside className="w-64 border-r bg-card flex flex-col">
      <div className="p-4 border-b">
        <h1 className="text-lg font-semibold">Relate Mail</h1>
      </div>

      <nav className="flex-1 p-2">
        <div className="space-y-1">
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
        </div>
      </nav>

      <div className="p-2 border-t">
        <Button
          variant={currentView === 'settings' ? 'secondary' : 'ghost'}
          className="w-full justify-start"
          onClick={() => onNavigate('settings')}
        >
          <Settings className="h-4 w-4 mr-2" />
          Settings
        </Button>
        <Button
          variant="ghost"
          className="w-full justify-start text-destructive hover:text-destructive"
          onClick={() => logout()}
        >
          <LogOut className="h-4 w-4 mr-2" />
          Logout
        </Button>
      </div>
    </aside>
  )
}
