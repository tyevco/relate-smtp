import { atom } from 'jotai'
import { invoke } from '@tauri-apps/api/core'

export interface Account {
  id: string
  display_name: string
  server_url: string
  user_email: string
  api_key_id: string
  scopes: string[]
  created_at: string
  last_used_at: string
}

export interface AccountsData {
  accounts: Account[]
  active_account_id: string | null
}

// Combined state for atomic updates - prevents partial state renders
export interface AccountsState {
  accounts: Account[]
  activeAccountId: string | null
  loaded: boolean
}

// Single source of truth for accounts state
export const accountsStateAtom = atom<AccountsState>({
  accounts: [],
  activeAccountId: null,
  loaded: false,
})

// Derived atoms for backward compatibility
export const accountsAtom = atom((get) => get(accountsStateAtom).accounts)
export const activeAccountIdAtom = atom((get) => get(accountsStateAtom).activeAccountId)
export const accountsLoadedAtom = atom((get) => get(accountsStateAtom).loaded)

// Derived atom for active account
export const activeAccountAtom = atom((get) => {
  const { accounts, activeAccountId } = get(accountsStateAtom)
  return accounts.find((a) => a.id === activeAccountId) ?? null
})

// Derived atom for checking if user has any accounts
export const hasAccountsAtom = atom((get) => {
  const { accounts } = get(accountsStateAtom)
  return accounts.length > 0
})

// Action: Load accounts from Tauri backend - single atomic update
export const loadAccountsAtom = atom(null, async (_get, set) => {
  try {
    const data = await invoke<AccountsData>('load_accounts')
    // Atomic update - all state changes happen in one render
    set(accountsStateAtom, {
      accounts: data.accounts,
      activeAccountId: data.active_account_id,
      loaded: true,
    })
    return data
  } catch (err) {
    set(accountsStateAtom, (prev) => ({ ...prev, loaded: true }))
    throw err
  }
})

// Action: Add a new account - atomic update
export const addAccountAtom = atom(
  null,
  async (
    _get,
    set,
    {
      account,
      apiKey,
    }: {
      account: Account
      apiKey: string
    }
  ) => {
    const data = await invoke<AccountsData>('save_account', {
      account,
      apiKey,
    })
    // Atomic update from Rust response
    set(accountsStateAtom, (prev) => ({
      ...prev,
      accounts: data.accounts,
      activeAccountId: data.active_account_id,
    }))
    return data
  }
)

// Action: Remove an account - atomic update
export const removeAccountAtom = atom(null, async (_get, set, accountId: string) => {
  const data = await invoke<AccountsData>('delete_account', { accountId })
  // Atomic update from Rust response
  set(accountsStateAtom, (prev) => ({
    ...prev,
    accounts: data.accounts,
    activeAccountId: data.active_account_id,
  }))
  return data
})

// Action: Switch active account - atomic update
export const switchAccountAtom = atom(null, async (_get, set, accountId: string) => {
  const account = await invoke<Account>('set_active_account', { accountId })
  // Atomic update - both activeAccountId and last_used_at change together
  set(accountsStateAtom, (prev) => ({
    ...prev,
    activeAccountId: accountId,
    accounts: prev.accounts.map((a) =>
      a.id === accountId ? { ...a, last_used_at: new Date().toISOString() } : a
    ),
  }))
  return account
})

// Helper: Generate a new account ID
export async function generateAccountId(): Promise<string> {
  return invoke<string>('generate_account_id')
}

// Helper: Get API key for an account
export async function getAccountApiKey(accountId: string): Promise<string | null> {
  return invoke<string | null>('get_account_api_key', { accountId })
}
