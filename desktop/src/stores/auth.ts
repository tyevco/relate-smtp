import { atom } from 'jotai'
import { invoke } from '@tauri-apps/api/core'

export interface AuthState {
  isAuthenticated: boolean
  serverUrl: string | null
  apiKey: string | null
  userEmail: string | null
}

const initialAuthState: AuthState = {
  isAuthenticated: false,
  serverUrl: null,
  apiKey: null,
  userEmail: null,
}

export const authAtom = atom<AuthState>(initialAuthState)

export const loginAtom = atom(
  null,
  (_get, set, { serverUrl, apiKey, userEmail }: { serverUrl: string; apiKey: string; userEmail: string }) => {
    set(authAtom, {
      isAuthenticated: true,
      serverUrl,
      apiKey,
      userEmail,
    })
  }
)

export const logoutAtom = atom(null, async (_get, set) => {
  try {
    await invoke('clear_credentials')
  } catch (err) {
    console.error('Failed to clear credentials:', err)
  }
  set(authAtom, initialAuthState)
})
