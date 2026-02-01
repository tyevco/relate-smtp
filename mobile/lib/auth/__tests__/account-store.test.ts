import { useAccountStore, useActiveAccount, useAccounts, useHasAccounts, type Account } from '../account-store'
import { renderHook, act } from '@testing-library/react-native'
import { deleteApiKey } from '../secure-storage'

// Mock secure-storage
jest.mock('../secure-storage', () => ({
  deleteApiKey: jest.fn().mockResolvedValue(undefined),
}))

function createMockAccount(overrides: Partial<Account> = {}): Omit<Account, 'isActive'> {
  return {
    id: 'account-' + Math.random().toString(36).substr(2, 9),
    displayName: 'Test Account',
    serverUrl: 'https://example.com',
    userEmail: 'test@example.com',
    apiKeyId: 'key-123',
    scopes: ['smtp', 'pop3', 'imap'],
    createdAt: new Date().toISOString(),
    lastUsedAt: new Date().toISOString(),
    ...overrides,
  }
}

describe('useAccountStore', () => {
  beforeEach(() => {
    // Reset store state
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
    jest.clearAllMocks()
  })

  describe('addAccount', () => {
    it('adds a new account', () => {
      const account = createMockAccount({ id: 'test-1' })

      act(() => {
        useAccountStore.getState().addAccount(account)
      })

      const state = useAccountStore.getState()
      expect(state.accounts).toHaveLength(1)
      expect(state.accounts[0].id).toBe('test-1')
    })

    it('sets new account as active', () => {
      const account = createMockAccount({ id: 'test-1' })

      act(() => {
        useAccountStore.getState().addAccount(account)
      })

      const state = useAccountStore.getState()
      expect(state.activeAccountId).toBe('test-1')
      expect(state.accounts[0].isActive).toBe(true)
    })

    it('deactivates other accounts when adding new one', () => {
      const account1 = createMockAccount({ id: 'test-1' })
      const account2 = createMockAccount({ id: 'test-2' })

      act(() => {
        useAccountStore.getState().addAccount(account1)
        useAccountStore.getState().addAccount(account2)
      })

      const state = useAccountStore.getState()
      expect(state.accounts[0].isActive).toBe(false)
      expect(state.accounts[1].isActive).toBe(true)
      expect(state.activeAccountId).toBe('test-2')
    })
  })

  describe('removeAccount', () => {
    it('removes an account', async () => {
      const account = createMockAccount({ id: 'test-1' })

      act(() => {
        useAccountStore.getState().addAccount(account)
      })

      await act(async () => {
        await useAccountStore.getState().removeAccount('test-1')
      })

      const state = useAccountStore.getState()
      expect(state.accounts).toHaveLength(0)
    })

    it('deletes the API key from secure storage', async () => {
      const account = createMockAccount({ id: 'test-1' })

      act(() => {
        useAccountStore.getState().addAccount(account)
      })

      await act(async () => {
        await useAccountStore.getState().removeAccount('test-1')
      })

      expect(deleteApiKey).toHaveBeenCalledWith('test-1')
    })

    it('activates first remaining account when active account is removed', async () => {
      const account1 = createMockAccount({ id: 'test-1' })
      const account2 = createMockAccount({ id: 'test-2' })

      act(() => {
        useAccountStore.getState().addAccount(account1)
        useAccountStore.getState().addAccount(account2)
      })

      await act(async () => {
        await useAccountStore.getState().removeAccount('test-2')
      })

      const state = useAccountStore.getState()
      expect(state.activeAccountId).toBe('test-1')
      expect(state.accounts[0].isActive).toBe(true)
    })

    it('sets activeAccountId to null when last account is removed', async () => {
      const account = createMockAccount({ id: 'test-1' })

      act(() => {
        useAccountStore.getState().addAccount(account)
      })

      await act(async () => {
        await useAccountStore.getState().removeAccount('test-1')
      })

      const state = useAccountStore.getState()
      expect(state.activeAccountId).toBeNull()
    })
  })

  describe('setActiveAccount', () => {
    it('sets a specific account as active', () => {
      const account1 = createMockAccount({ id: 'test-1' })
      const account2 = createMockAccount({ id: 'test-2' })

      act(() => {
        useAccountStore.getState().addAccount(account1)
        useAccountStore.getState().addAccount(account2)
        useAccountStore.getState().setActiveAccount('test-1')
      })

      const state = useAccountStore.getState()
      expect(state.activeAccountId).toBe('test-1')
      expect(state.accounts.find(a => a.id === 'test-1')?.isActive).toBe(true)
      expect(state.accounts.find(a => a.id === 'test-2')?.isActive).toBe(false)
    })
  })

  describe('updateAccount', () => {
    it('updates account properties', () => {
      const account = createMockAccount({ id: 'test-1', displayName: 'Original' })

      act(() => {
        useAccountStore.getState().addAccount(account)
        useAccountStore.getState().updateAccount('test-1', { displayName: 'Updated' })
      })

      const state = useAccountStore.getState()
      expect(state.accounts[0].displayName).toBe('Updated')
    })

    it('does not affect other accounts', () => {
      const account1 = createMockAccount({ id: 'test-1', displayName: 'Account 1' })
      const account2 = createMockAccount({ id: 'test-2', displayName: 'Account 2' })

      act(() => {
        useAccountStore.getState().addAccount(account1)
        useAccountStore.getState().addAccount(account2)
        useAccountStore.getState().updateAccount('test-1', { displayName: 'Updated' })
      })

      const state = useAccountStore.getState()
      expect(state.accounts.find(a => a.id === 'test-2')?.displayName).toBe('Account 2')
    })
  })

  describe('updateLastUsed', () => {
    it('updates lastUsedAt timestamp', () => {
      const oldDate = '2020-01-01T00:00:00Z'
      const account = createMockAccount({ id: 'test-1', lastUsedAt: oldDate })

      act(() => {
        useAccountStore.getState().addAccount(account)
        useAccountStore.getState().updateLastUsed('test-1')
      })

      const state = useAccountStore.getState()
      expect(state.accounts[0].lastUsedAt).not.toBe(oldDate)
      expect(new Date(state.accounts[0].lastUsedAt).getTime()).toBeGreaterThan(new Date(oldDate).getTime())
    })
  })

  describe('getActiveAccount', () => {
    it('returns the active account', () => {
      const account = createMockAccount({ id: 'test-1' })

      act(() => {
        useAccountStore.getState().addAccount(account)
      })

      const result = useAccountStore.getState().getActiveAccount()
      expect(result).not.toBeNull()
      expect(result?.id).toBe('test-1')
    })

    it('returns null when no accounts exist', () => {
      const result = useAccountStore.getState().getActiveAccount()
      expect(result).toBeNull()
    })
  })
})

describe('useActiveAccount hook', () => {
  beforeEach(() => {
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
  })

  it('returns null when no active account', () => {
    const { result } = renderHook(() => useActiveAccount())
    expect(result.current).toBeNull()
  })

  it('returns the active account', () => {
    const account = createMockAccount({ id: 'test-1' })

    act(() => {
      useAccountStore.getState().addAccount(account)
    })

    const { result } = renderHook(() => useActiveAccount())
    expect(result.current?.id).toBe('test-1')
  })
})

describe('useAccounts hook', () => {
  beforeEach(() => {
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
  })

  it('returns empty array when no accounts', () => {
    const { result } = renderHook(() => useAccounts())
    expect(result.current).toEqual([])
  })

  it('returns all accounts', () => {
    const account1 = createMockAccount({ id: 'test-1' })
    const account2 = createMockAccount({ id: 'test-2' })

    act(() => {
      useAccountStore.getState().addAccount(account1)
      useAccountStore.getState().addAccount(account2)
    })

    const { result } = renderHook(() => useAccounts())
    expect(result.current).toHaveLength(2)
  })
})

describe('useHasAccounts hook', () => {
  beforeEach(() => {
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
  })

  it('returns false when no accounts', () => {
    const { result } = renderHook(() => useHasAccounts())
    expect(result.current).toBe(false)
  })

  it('returns true when accounts exist', () => {
    const account = createMockAccount({ id: 'test-1' })

    act(() => {
      useAccountStore.getState().addAccount(account)
    })

    const { result } = renderHook(() => useHasAccounts())
    expect(result.current).toBe(true)
  })
})
