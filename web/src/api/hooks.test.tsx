import { describe, it, expect, vi } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import {
  useEmails,
  useEmail,
  useSearchEmails,
  useMarkEmailRead,
  useDeleteEmail,
  useProfile,
  useUpdateProfile,
  useAddEmailAddress,
  useRemoveEmailAddress,
  useSmtpCredentials,
  useCreateSmtpApiKey,
  useRevokeSmtpApiKey,
  useLabels,
  useCreateLabel,
  useUpdateLabel,
  useDeleteLabel,
  useAddLabelToEmail,
  useRemoveLabelFromEmail,
  useFilters,
  useCreateFilter,
  useUpdateFilter,
  useDeleteFilter,
  usePreferences,
  useUpdatePreferences,
  useInfiniteEmails,
  useBulkMarkRead,
  useBulkDelete,
  useSentEmails,
  useSentFromAddresses,
} from './hooks'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })
}

function createWrapper() {
  const queryClient = createTestQueryClient()
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }
}

describe('Email hooks', () => {
  describe('useEmails', () => {
    it('fetches emails with default pagination', async () => {
      const { result } = renderHook(() => useEmails(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
      expect(result.current.data?.items).toBeInstanceOf(Array)
      expect(result.current.data?.page).toBe(1)
    })

    it('fetches emails with custom pagination', async () => {
      const { result } = renderHook(() => useEmails(2, 10), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data?.page).toBe(2)
      expect(result.current.data?.pageSize).toBe(10)
    })
  })

  describe('useEmail', () => {
    it('fetches a single email by ID', async () => {
      const { result } = renderHook(() => useEmail('1'), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
      expect(result.current.data?.id).toBe('1')
    })

    it('does not fetch when ID is empty', () => {
      const { result } = renderHook(() => useEmail(''), {
        wrapper: createWrapper(),
      })

      expect(result.current.isFetching).toBe(false)
    })
  })

  describe('useSearchEmails', () => {
    it('searches emails with query', async () => {
      const { result } = renderHook(() => useSearchEmails({ query: 'test' }), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
    })

    it('does not search without filters', () => {
      const { result } = renderHook(() => useSearchEmails({}), {
        wrapper: createWrapper(),
      })

      expect(result.current.isFetching).toBe(false)
    })
  })

  describe('useMarkEmailRead', () => {
    it('returns a mutation function', () => {
      const { result } = renderHook(() => useMarkEmailRead(), {
        wrapper: createWrapper(),
      })

      expect(result.current.mutate).toBeDefined()
      expect(result.current.mutateAsync).toBeDefined()
    })

    it('marks an email as read', async () => {
      const { result } = renderHook(() => useMarkEmailRead(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ id: '1', isRead: true })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useDeleteEmail', () => {
    it('deletes an email', async () => {
      const { result } = renderHook(() => useDeleteEmail(), {
        wrapper: createWrapper(),
      })

      result.current.mutate('1')

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useInfiniteEmails', () => {
    it('fetches first page of emails', async () => {
      const { result } = renderHook(() => useInfiniteEmails(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data?.pages).toBeDefined()
      expect(result.current.data?.pages[0]?.items).toBeInstanceOf(Array)
    })
  })

  describe('useBulkMarkRead', () => {
    it('marks multiple emails as read', async () => {
      const { result } = renderHook(() => useBulkMarkRead(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ emailIds: ['1', '2', '3'], isRead: true })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useBulkDelete', () => {
    it('deletes multiple emails', async () => {
      const { result } = renderHook(() => useBulkDelete(), {
        wrapper: createWrapper(),
      })

      result.current.mutate(['1', '2', '3'])

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })
})

describe('Profile hooks', () => {
  describe('useProfile', () => {
    it('fetches user profile', async () => {
      const { result } = renderHook(() => useProfile(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
      expect(result.current.data?.email).toBeDefined()
    })
  })

  describe('useUpdateProfile', () => {
    it('updates user profile', async () => {
      const { result } = renderHook(() => useUpdateProfile(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ displayName: 'New Name' })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useAddEmailAddress', () => {
    it('adds an email address', async () => {
      const { result } = renderHook(() => useAddEmailAddress(), {
        wrapper: createWrapper(),
      })

      result.current.mutate('new@example.com')

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useRemoveEmailAddress', () => {
    it('removes an email address', async () => {
      const { result } = renderHook(() => useRemoveEmailAddress(), {
        wrapper: createWrapper(),
      })

      result.current.mutate('address-id')

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })
})

describe('SMTP Credentials hooks', () => {
  describe('useSmtpCredentials', () => {
    it('fetches SMTP credentials', async () => {
      const { result } = renderHook(() => useSmtpCredentials(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
      expect(result.current.data?.connectionInfo).toBeDefined()
      expect(result.current.data?.keys).toBeInstanceOf(Array)
    })
  })

  describe('useCreateSmtpApiKey', () => {
    it('creates a new API key', async () => {
      const { result } = renderHook(() => useCreateSmtpApiKey(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ name: 'Test Key', scopes: ['smtp', 'pop3'] })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data?.apiKey).toBeDefined()
    })
  })

  describe('useRevokeSmtpApiKey', () => {
    it('revokes an API key', async () => {
      const { result } = renderHook(() => useRevokeSmtpApiKey(), {
        wrapper: createWrapper(),
      })

      result.current.mutate('key-id')

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })
})

describe('Label hooks', () => {
  describe('useLabels', () => {
    it('fetches all labels', async () => {
      const { result } = renderHook(() => useLabels(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeInstanceOf(Array)
    })
  })

  describe('useCreateLabel', () => {
    it('creates a new label', async () => {
      const { result } = renderHook(() => useCreateLabel(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ name: 'New Label', color: '#ff0000' })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useUpdateLabel', () => {
    it('updates a label', async () => {
      const { result } = renderHook(() => useUpdateLabel(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ id: 'label-1', data: { name: 'Updated Label' } })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useDeleteLabel', () => {
    it('deletes a label', async () => {
      const { result } = renderHook(() => useDeleteLabel(), {
        wrapper: createWrapper(),
      })

      result.current.mutate('label-1')

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useAddLabelToEmail', () => {
    it('adds a label to an email', async () => {
      const { result } = renderHook(() => useAddLabelToEmail(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ emailId: '1', labelId: 'label-1' })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useRemoveLabelFromEmail', () => {
    it('removes a label from an email', async () => {
      const { result } = renderHook(() => useRemoveLabelFromEmail(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ emailId: '1', labelId: 'label-1' })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })
})

describe('Filter hooks', () => {
  describe('useFilters', () => {
    it('fetches all filters', async () => {
      const { result } = renderHook(() => useFilters(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeInstanceOf(Array)
    })
  })

  describe('useCreateFilter', () => {
    it('creates a new filter', async () => {
      const { result } = renderHook(() => useCreateFilter(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({
        name: 'New Filter',
        fromAddressContains: '@example.com',
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useUpdateFilter', () => {
    it('updates a filter', async () => {
      const { result } = renderHook(() => useUpdateFilter(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ id: 'filter-1', data: { name: 'Updated Filter' } })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })

  describe('useDeleteFilter', () => {
    it('deletes a filter', async () => {
      const { result } = renderHook(() => useDeleteFilter(), {
        wrapper: createWrapper(),
      })

      result.current.mutate('filter-1')

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })
})

describe('Preference hooks', () => {
  describe('usePreferences', () => {
    it('fetches user preferences', async () => {
      const { result } = renderHook(() => usePreferences(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
      expect(result.current.data?.theme).toBeDefined()
    })
  })

  describe('useUpdatePreferences', () => {
    it('updates user preferences', async () => {
      const { result } = renderHook(() => useUpdatePreferences(), {
        wrapper: createWrapper(),
      })

      result.current.mutate({ theme: 'dark' })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))
    })
  })
})

describe('Sent mail hooks', () => {
  describe('useSentEmails', () => {
    it('fetches sent emails', async () => {
      const { result } = renderHook(() => useSentEmails(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeDefined()
    })
  })

  describe('useSentFromAddresses', () => {
    it('fetches sent from addresses', async () => {
      const { result } = renderHook(() => useSentFromAddresses(), {
        wrapper: createWrapper(),
      })

      await waitFor(() => expect(result.current.isSuccess).toBe(true))

      expect(result.current.data).toBeInstanceOf(Array)
    })
  })
})
