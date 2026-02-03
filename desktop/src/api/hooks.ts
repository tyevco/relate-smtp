import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPatch, apiDelete } from './client'
import type { EmailListResponse, EmailDetail, Profile, SmtpCredentials, CreateApiKeyRequest, CreatedApiKey } from '@relate/shared/api/types'

// Emails
export function useEmails(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['emails', page, pageSize],
    queryFn: () => apiGet<EmailListResponse>(`/emails?page=${page}&pageSize=${pageSize}`),
  })
}

export function useEmail(id: string) {
  return useQuery({
    queryKey: ['email', id],
    queryFn: () => apiGet<EmailDetail>(`/emails/${id}`),
    enabled: !!id,
  })
}

export function useMarkEmailRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, isRead }: { id: string; isRead: boolean }) =>
      apiPatch(`/emails/${id}/read`, { isRead }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

export function useDeleteEmail() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => apiDelete(`/emails/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emails'] })
    },
  })
}

// Profile
export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => apiGet<Profile>('/profile'),
  })
}

// Search
export interface EmailSearchFilters {
  query?: string
  fromDate?: string
  toDate?: string
  hasAttachments?: boolean
  isRead?: boolean
}

// SMTP Credentials
export function useSmtpCredentials() {
  return useQuery({
    queryKey: ['smtp-credentials'],
    queryFn: () => apiGet<SmtpCredentials>('/smtp-credentials'),
  })
}

export function useCreateSmtpApiKey() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateApiKeyRequest) =>
      apiPost<CreatedApiKey>('/smtp-credentials', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smtp-credentials'] })
    },
  })
}

export function useRevokeSmtpApiKey() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (keyId: string) =>
      apiDelete(`/smtp-credentials/${keyId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smtp-credentials'] })
    },
  })
}

export function useSearchEmails(filters: EmailSearchFilters, page = 1) {
  const params = new URLSearchParams()
  if (filters.query) params.set('query', filters.query)
  if (filters.fromDate) params.set('fromDate', filters.fromDate)
  if (filters.toDate) params.set('toDate', filters.toDate)
  if (filters.hasAttachments !== undefined) params.set('hasAttachments', String(filters.hasAttachments))
  if (filters.isRead !== undefined) params.set('isRead', String(filters.isRead))
  params.set('page', String(page))

  return useQuery({
    queryKey: ['emails', 'search', filters, page],
    queryFn: () => apiGet<EmailListResponse>(`/emails/search?${params.toString()}`),
    enabled: Object.values(filters).some(v => v !== undefined && v !== ''),
  })
}
