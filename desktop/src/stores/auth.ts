import { atom } from 'jotai'
import {
  activeAccountAtom,
  activeAccountIdAtom,
  accountsAtom,
  removeAccountAtom,
  getAccountApiKey,
} from './accounts'

export interface AuthState {
  isAuthenticated: boolean
  serverUrl: string | null
  apiKey: string | null
  userEmail: string | null
  displayName: string | null
}

// Simple synchronous auth state derived from active account
export const authStateAtom = atom((get) => {
  const activeAccount = get(activeAccountAtom)

  return {
    isAuthenticated: !!activeAccount,
    serverUrl: activeAccount?.server_url ?? null,
    userEmail: activeAccount?.user_email ?? null,
    displayName: activeAccount?.display_name ?? null,
  }
})

// For backwards compatibility, export authStateAtom as authAtom
export const authAtom = authStateAtom

// Action: Logout current account (remove it)
export const logoutAtom = atom(null, async (get, set) => {
  const activeId = get(activeAccountIdAtom)
  if (activeId) {
    await set(removeAccountAtom, activeId)
  }
})

// Action: Logout all accounts
export const logoutAllAtom = atom(null, async (get, set) => {
  const accounts = get(accountsAtom)
  for (const account of accounts) {
    await set(removeAccountAtom, account.id)
  }
})

// Legacy login action - now just for backwards compatibility
// New code should use addAccountAtom directly
export const loginAtom = atom(
  null,
  (_get, _set, _params: { serverUrl: string; apiKey: string; userEmail: string }) => {
    // This is now a no-op - use addAccountAtom instead
    console.warn('loginAtom is deprecated, use addAccountAtom instead')
  }
)

// Re-export for convenience
export { getAccountApiKey }
