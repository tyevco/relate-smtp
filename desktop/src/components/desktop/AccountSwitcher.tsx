import { useState } from 'react'
import { useAtomValue, useSetAtom } from 'jotai'
import { useQueryClient } from '@tanstack/react-query'
import { Check, Plus, Trash2, LogOut, Settings } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  Button,
} from '@relate/shared/components/ui'
import { Avatar } from './Avatar'
import {
  accountsAtom,
  activeAccountIdAtom,
  switchAccountAtom,
  removeAccountAtom,
  type Account,
} from '@/stores/accounts'
import { logoutAllAtom } from '@/stores/auth'
import { getHostname } from '@/lib/utils'

interface AccountSwitcherProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onAddAccount: () => void
  onOpenSettings: () => void
}

export function AccountSwitcher({
  open,
  onOpenChange,
  onAddAccount,
  onOpenSettings,
}: AccountSwitcherProps) {
  const accounts = useAtomValue(accountsAtom)
  const activeAccountId = useAtomValue(activeAccountIdAtom)
  const switchAccount = useSetAtom(switchAccountAtom)
  const removeAccount = useSetAtom(removeAccountAtom)
  const logoutAll = useSetAtom(logoutAllAtom)
  const queryClient = useQueryClient()
  const [confirmingDelete, setConfirmingDelete] = useState<string | null>(null)

  const handleSelectAccount = async (accountId: string) => {
    if (accountId === activeAccountId) {
      onOpenChange(false)
      return
    }

    await switchAccount(accountId)
    // Clear all cached queries when switching accounts
    queryClient.clear()
    onOpenChange(false)
  }

  const handleRemoveAccount = async (accountId: string) => {
    await removeAccount(accountId)
    setConfirmingDelete(null)
    // If no accounts left, this will trigger login screen via App.tsx
    if (accounts.length <= 1) {
      onOpenChange(false)
    }
  }

  const handleAddAccount = () => {
    onOpenChange(false)
    onAddAccount()
  }

  const handleOpenSettings = () => {
    onOpenChange(false)
    onOpenSettings()
  }

  const handleLogoutAll = async () => {
    await logoutAll()
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Accounts</DialogTitle>
          <DialogDescription>
            Switch between accounts or add a new one.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 py-2">
          {accounts.map((account) => (
            <AccountItem
              key={account.id}
              account={account}
              isActive={account.id === activeAccountId}
              isConfirmingDelete={confirmingDelete === account.id}
              onSelect={() => handleSelectAccount(account.id)}
              onDelete={() => setConfirmingDelete(account.id)}
              onConfirmDelete={() => handleRemoveAccount(account.id)}
              onCancelDelete={() => setConfirmingDelete(null)}
            />
          ))}
        </div>

        <div className="border-t pt-4 space-y-2">
          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={handleAddAccount}
          >
            <Plus className="h-4 w-4 mr-2" />
            Add Another Account
          </Button>

          <Button
            variant="ghost"
            className="w-full justify-start"
            onClick={handleOpenSettings}
          >
            <Settings className="h-4 w-4 mr-2" />
            Preferences
          </Button>

          {accounts.length > 0 && (
            <Button
              variant="ghost"
              className="w-full justify-start text-destructive hover:text-destructive hover:bg-destructive/10"
              onClick={handleLogoutAll}
            >
              <LogOut className="h-4 w-4 mr-2" />
              Sign Out of All Accounts
            </Button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

interface AccountItemProps {
  account: Account
  isActive: boolean
  isConfirmingDelete: boolean
  onSelect: () => void
  onDelete: () => void
  onConfirmDelete: () => void
  onCancelDelete: () => void
}

function AccountItem({
  account,
  isActive,
  isConfirmingDelete,
  onSelect,
  onDelete,
  onConfirmDelete,
  onCancelDelete,
}: AccountItemProps) {
  if (isConfirmingDelete) {
    return (
      <div className="flex items-center gap-3 rounded-lg border border-destructive bg-destructive/5 p-3">
        <div className="flex-1">
          <p className="text-sm font-medium">Remove account?</p>
          <p className="text-xs text-muted-foreground">
            This will delete the stored API key.
          </p>
        </div>
        <div className="flex gap-2">
          <Button size="sm" variant="ghost" onClick={onCancelDelete}>
            Cancel
          </Button>
          <Button size="sm" variant="destructive" onClick={onConfirmDelete}>
            Remove
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div
      className={`flex items-center gap-3 rounded-lg border p-3 cursor-pointer transition-colors ${
        isActive
          ? 'border-primary bg-primary/5'
          : 'border-border bg-card hover:bg-accent'
      }`}
      onClick={onSelect}
    >
      <Avatar
        name={account.display_name}
        email={account.user_email}
        size="md"
      />

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="font-medium truncate">
            {account.display_name || account.user_email}
          </span>
          {isActive && <Check className="h-4 w-4 text-primary flex-shrink-0" />}
        </div>
        <p className="text-sm text-muted-foreground truncate">
          {account.user_email}
        </p>
        <p className="text-xs text-muted-foreground">
          {getHostname(account.server_url)}
        </p>
      </div>

      <button
        className="p-2 rounded hover:bg-destructive/10 transition-colors"
        onClick={(e) => {
          e.stopPropagation()
          onDelete()
        }}
      >
        <Trash2 className="h-4 w-4 text-destructive" />
      </button>
    </div>
  )
}
