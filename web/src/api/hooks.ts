import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { api } from './client'
import type { EmailListResponse, EmailDetail, Profile, EmailAddress, SmtpCredentials, CreateApiKeyRequest, CreatedApiKey, Label, CreateLabelRequest, UpdateLabelRequest, EmailFilter, CreateEmailFilterRequest, UpdateEmailFilterRequest, UserPreference, UpdateUserPreferenceRequest } from './types'

// Email hooks
export function useEmails(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['emails', page, pageSize],
    queryFn: () => api.get<EmailListResponse>(`/emails?page=${page}&pageSize=${pageSize}`),
  })
}

export interface EmailSearchFilters {
  query?: string
  fromDate?: string
  toDate?: string
  hasAttachments?: boolean
  isRead?: boolean
}

export function useSearchEmails(
  filters: EmailSearchFilters,
  page = 1,
  pageSize = 20
) {
  const params = new URLSearchParams()
  params.set('page', page.toString())
  params.set('pageSize', pageSize.toString())

  if (filters.query) params.set('q', filters.query)
  if (filters.fromDate) params.set('fromDate', filters.fromDate)
  if (filters.toDate) params.set('toDate', filters.toDate)
  if (filters.hasAttachments !== undefined) params.set('hasAttachments', filters.hasAttachments.toString())
  if (filters.isRead !== undefined) params.set('isRead', filters.isRead.toString())

  return useQuery({
    queryKey: ['emails', 'search', filters, page, pageSize],
    queryFn: () => api.get<EmailListResponse>(`/emails/search?${params.toString()}`),
    enabled: !!filters.query || filters.hasAttachments !== undefined || filters.isRead !== undefined,
  })
}

export function useEmail(id: string) {
  return useQuery({
    queryKey: ['email', id],
    queryFn: () => api.get<EmailDetail>(`/emails/${id}`),
    enabled: !!id,
  })
}

export function useMarkEmailRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, isRead }: { id: string; isRead: boolean }) =>
      api.patch<EmailDetail>(`/emails/${id}`, { isRead }),
    onSuccess: (data, variables) => {
      queryClient.setQueryData(['email', variables.id], data)
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

export function useDeleteEmail() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.delete(`/emails/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

// Profile hooks
export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => api.get<Profile>('/profile'),
  })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: { displayName?: string }) =>
      api.put<Profile>('/profile', data),
    onSuccess: (data) => {
      queryClient.setQueryData(['profile'], data)
    },
  })
}

export function useAddEmailAddress() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (address: string) =>
      api.post<EmailAddress>('/profile/addresses', { address }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] })
    },
  })
}

export function useRemoveEmailAddress() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (addressId: string) =>
      api.delete(`/profile/addresses/${addressId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] })
    },
  })
}

// SMTP Credentials hooks
export function useSmtpCredentials() {
  return useQuery({
    queryKey: ['smtp-credentials'],
    queryFn: () => api.get<SmtpCredentials>('/smtp-credentials'),
  })
}

export function useCreateSmtpApiKey() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateApiKeyRequest) =>
      api.post<CreatedApiKey>('/smtp-credentials', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smtp-credentials'] })
    },
  })
}

export function useRevokeSmtpApiKey() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (keyId: string) =>
      api.delete(`/smtp-credentials/${keyId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smtp-credentials'] })
    },
  })
}

// Label hooks
export function useLabels() {
  return useQuery({
    queryKey: ['labels'],
    queryFn: () => api.get<Label[]>('/labels'),
  })
}

export function useCreateLabel() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateLabelRequest) =>
      api.post<Label>('/labels', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['labels'] })
    },
  })
}

export function useUpdateLabel() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLabelRequest }) =>
      api.put<Label>(`/labels/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['labels'] })
    },
  })
}

export function useDeleteLabel() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/labels/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['labels'] })
    },
  })
}

export function useAddLabelToEmail() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ emailId, labelId }: { emailId: string; labelId: string }) =>
      api.post(`/labels/emails/${emailId}`, { labelId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

export function useRemoveLabelFromEmail() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ emailId, labelId }: { emailId: string; labelId: string }) =>
      api.delete(`/labels/emails/${emailId}/${labelId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

export function useEmailsByLabel(labelId: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['emails', 'label', labelId, page, pageSize],
    queryFn: () => api.get<EmailListResponse>(`/labels/${labelId}/emails?page=${page}&pageSize=${pageSize}`),
    enabled: !!labelId,
  })
}

// Filter hooks
export function useFilters() {
  return useQuery({
    queryKey: ['filters'],
    queryFn: () => api.get<EmailFilter[]>('/filters'),
  })
}

export function useCreateFilter() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateEmailFilterRequest) =>
      api.post<EmailFilter>('/filters', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['filters'] })
    },
  })
}

export function useUpdateFilter() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmailFilterRequest }) =>
      api.put<EmailFilter>(`/filters/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['filters'] })
    },
  })
}

export function useDeleteFilter() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/filters/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['filters'] })
    },
  })
}

export function useTestFilter(id: string, limit = 10) {
  return useQuery({
    queryKey: ['filters', 'test', id, limit],
    queryFn: () => api.post<{ matchCount: number; matchedEmailIds: string[] }>(`/filters/${id}/test?limit=${limit}`, {}),
    enabled: false, // Only run when manually triggered
  })
}

// Preference hooks
export function usePreferences() {
  return useQuery({
    queryKey: ['preferences'],
    queryFn: () => api.get<UserPreference>('/preferences'),
  })
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateUserPreferenceRequest) =>
      api.put<UserPreference>('/preferences', data),
    onSuccess: (data) => {
      queryClient.setQueryData(['preferences'], data)
    },
  })
}

// Infinite scroll hook
export function useInfiniteEmails(pageSize = 20) {
  return useInfiniteQuery({
    queryKey: ['emails', 'infinite', pageSize],
    queryFn: ({ pageParam = 1 }) =>
      api.get<EmailListResponse>(`/emails?page=${pageParam}&pageSize=${pageSize}`),
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize)
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined
    },
    initialPageParam: 1,
  })
}

// Bulk operations hooks
export function useBulkMarkRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ emailIds, isRead }: { emailIds: string[]; isRead: boolean }) =>
      api.post<void>('/emails/bulk/mark-read', { emailIds, isRead }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

export function useBulkDelete() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (emailIds: string[]) =>
      api.post<void>('/emails/bulk/delete', { emailIds }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

// Thread hooks
export function useThread(threadId: string) {
  return useQuery({
    queryKey: ['thread', threadId],
    queryFn: () => api.get<EmailDetail[]>(`/emails/threads/${threadId}`),
    enabled: !!threadId,
  })
}
