import React from 'react'
import { renderHook, waitFor } from '@testing-library/react-native'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useAccountStore } from '../../auth/account-store'
import type { Account } from '../../auth/account-store'

// Import hooks
import {
  useEmails,
  useEmail,
  useSearchEmails,
  useMarkEmailRead,
  useDeleteEmail,
  useInfiniteEmails,
  useProfile,
  useUpdateProfile,
  useSmtpCredentials,
  useCreateSmtpApiKey,
  useRevokeSmtpApiKey,
  useLabels,
  usePreferences,
  useUpdatePreferences,
  useBulkMarkRead,
  useBulkDelete,
  useSentEmails,
  useSentFromAddresses,
  type EmailSearchFilters,
} from '../hooks'

// Mock the API client module
jest.mock('../client', () => ({
  getActiveApiClient: jest.fn().mockImplementation(() =>
    Promise.resolve({
      get: jest.fn().mockResolvedValue({ data: [], total: 0 }),
      post: jest.fn().mockResolvedValue({}),
      put: jest.fn().mockResolvedValue({}),
      patch: jest.fn().mockResolvedValue({}),
      delete: jest.fn().mockResolvedValue({}),
      baseUrl: 'https://example.com',
    })
  ),
}))

function createMockAccount(): Account {
  return {
    id: 'test-account-1',
    displayName: 'Test Account',
    serverUrl: 'https://example.com',
    userEmail: 'test@example.com',
    apiKeyId: 'key-123',
    scopes: ['smtp', 'pop3', 'imap'],
    createdAt: new Date().toISOString(),
    lastUsedAt: new Date().toISOString(),
    isActive: true,
  }
}

// Create a wrapper with QueryClientProvider
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('API Hooks', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    useAccountStore.setState({
      accounts: [createMockAccount()],
      activeAccountId: 'test-account-1',
    })
  })

  afterEach(() => {
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
  })

  describe('Query Hooks', () => {
    describe('useEmails', () => {
      it('is a function', () => {
        expect(typeof useEmails).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => useEmails(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.isLoading).toBeDefined()
      })
    })

    describe('useEmail', () => {
      it('is a function', () => {
        expect(typeof useEmail).toBe('function')
      })

      it('returns query result when called with id', async () => {
        const { result } = renderHook(() => useEmail('email-1'), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })

    describe('useSearchEmails', () => {
      it('is a function', () => {
        expect(typeof useSearchEmails).toBe('function')
      })

      it('returns query result when called with filters', async () => {
        const { result } = renderHook(
          () => useSearchEmails({ query: 'test' }),
          { wrapper: createWrapper() }
        )
        expect(result.current).toBeDefined()
      })
    })

    describe('useInfiniteEmails', () => {
      it('is a function', () => {
        expect(typeof useInfiniteEmails).toBe('function')
      })

      it('returns infinite query result when called', async () => {
        const { result } = renderHook(() => useInfiniteEmails(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.fetchNextPage).toBeDefined()
      })
    })

    describe('useProfile', () => {
      it('is a function', () => {
        expect(typeof useProfile).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => useProfile(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })

    describe('useSmtpCredentials', () => {
      it('is a function', () => {
        expect(typeof useSmtpCredentials).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => useSmtpCredentials(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })

    describe('useLabels', () => {
      it('is a function', () => {
        expect(typeof useLabels).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => useLabels(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })

    describe('usePreferences', () => {
      it('is a function', () => {
        expect(typeof usePreferences).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => usePreferences(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })

    describe('useSentEmails', () => {
      it('is a function', () => {
        expect(typeof useSentEmails).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => useSentEmails(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })

    describe('useSentFromAddresses', () => {
      it('is a function', () => {
        expect(typeof useSentFromAddresses).toBe('function')
      })

      it('returns query result when called', async () => {
        const { result } = renderHook(() => useSentFromAddresses(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
      })
    })
  })

  describe('Mutation Hooks', () => {
    describe('useMarkEmailRead', () => {
      it('is a function', () => {
        expect(typeof useMarkEmailRead).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useMarkEmailRead(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useDeleteEmail', () => {
      it('is a function', () => {
        expect(typeof useDeleteEmail).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useDeleteEmail(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useUpdateProfile', () => {
      it('is a function', () => {
        expect(typeof useUpdateProfile).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useUpdateProfile(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useCreateSmtpApiKey', () => {
      it('is a function', () => {
        expect(typeof useCreateSmtpApiKey).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useCreateSmtpApiKey(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useRevokeSmtpApiKey', () => {
      it('is a function', () => {
        expect(typeof useRevokeSmtpApiKey).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useRevokeSmtpApiKey(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useUpdatePreferences', () => {
      it('is a function', () => {
        expect(typeof useUpdatePreferences).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useUpdatePreferences(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useBulkMarkRead', () => {
      it('is a function', () => {
        expect(typeof useBulkMarkRead).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useBulkMarkRead(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })

    describe('useBulkDelete', () => {
      it('is a function', () => {
        expect(typeof useBulkDelete).toBe('function')
      })

      it('returns mutation result when called', async () => {
        const { result } = renderHook(() => useBulkDelete(), {
          wrapper: createWrapper(),
        })
        expect(result.current).toBeDefined()
        expect(result.current.mutateAsync).toBeDefined()
      })
    })
  })

  describe('EmailSearchFilters interface', () => {
    it('supports query filter', () => {
      const filters: EmailSearchFilters = {
        query: 'test search',
      }
      expect(filters.query).toBe('test search')
    })

    it('supports all filters together', () => {
      const filters: EmailSearchFilters = {
        query: 'test',
        fromDate: '2024-01-01',
        toDate: '2024-12-31',
        hasAttachments: true,
        isRead: false,
      }
      expect(filters.query).toBe('test')
      expect(filters.fromDate).toBe('2024-01-01')
      expect(filters.toDate).toBe('2024-12-31')
      expect(filters.hasAttachments).toBe(true)
      expect(filters.isRead).toBe(false)
    })
  })
})
