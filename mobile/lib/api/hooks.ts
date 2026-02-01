import {
  useQuery,
  useMutation,
  useQueryClient,
  useInfiniteQuery,
} from "@tanstack/react-query";
import { getActiveApiClient } from "./client";
import type {
  EmailListResponse,
  EmailDetail,
  Profile,
  SmtpCredentials,
  CreateApiKeyRequest,
  CreatedApiKey,
  Label,
  UserPreference,
  UpdateUserPreferenceRequest,
} from "./types";
import { useActiveAccount } from "../auth/account-store";

// Helper to get API client within query functions
async function withApi<T>(fn: (api: Awaited<ReturnType<typeof getActiveApiClient>>) => Promise<T>) {
  const api = await getActiveApiClient();
  return fn(api);
}

// Email hooks
export function useEmails(page = 1, pageSize = 20) {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["emails", account?.id, page, pageSize],
    queryFn: () =>
      withApi((api) =>
        api.get<EmailListResponse>(`/emails?page=${page}&pageSize=${pageSize}`)
      ),
    enabled: !!account,
  });
}

export interface EmailSearchFilters {
  query?: string;
  fromDate?: string;
  toDate?: string;
  hasAttachments?: boolean;
  isRead?: boolean;
}

export function useSearchEmails(
  filters: EmailSearchFilters,
  page = 1,
  pageSize = 20
) {
  const account = useActiveAccount();
  const params = new URLSearchParams();
  params.set("page", page.toString());
  params.set("pageSize", pageSize.toString());

  if (filters.query) params.set("q", filters.query);
  if (filters.fromDate) params.set("fromDate", filters.fromDate);
  if (filters.toDate) params.set("toDate", filters.toDate);
  if (filters.hasAttachments !== undefined)
    params.set("hasAttachments", filters.hasAttachments.toString());
  if (filters.isRead !== undefined)
    params.set("isRead", filters.isRead.toString());

  return useQuery({
    queryKey: ["emails", "search", account?.id, filters, page, pageSize],
    queryFn: () =>
      withApi((api) =>
        api.get<EmailListResponse>(`/emails/search?${params.toString()}`)
      ),
    enabled:
      !!account &&
      (!!filters.query ||
        filters.hasAttachments !== undefined ||
        filters.isRead !== undefined),
  });
}

export function useEmail(id: string) {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["email", account?.id, id],
    queryFn: () => withApi((api) => api.get<EmailDetail>(`/emails/${id}`)),
    enabled: !!account && !!id,
  });
}

export function useMarkEmailRead() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async ({ id, isRead }: { id: string; isRead: boolean }) =>
      withApi((api) => api.patch<EmailDetail>(`/emails/${id}`, { isRead })),
    onSuccess: (data, variables) => {
      queryClient.setQueryData(["email", account?.id, variables.id], data);
      queryClient.invalidateQueries({ queryKey: ["emails", account?.id] });
    },
  });
}

export function useDeleteEmail() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async (id: string) =>
      withApi((api) => api.delete(`/emails/${id}`)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["emails", account?.id] });
    },
  });
}

// Profile hooks
export function useProfile() {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["profile", account?.id],
    queryFn: () => withApi((api) => api.get<Profile>("/profile")),
    enabled: !!account,
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async (data: { displayName?: string }) =>
      withApi((api) => api.put<Profile>("/profile", data)),
    onSuccess: (data) => {
      queryClient.setQueryData(["profile", account?.id], data);
    },
  });
}

// SMTP Credentials hooks
export function useSmtpCredentials() {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["smtp-credentials", account?.id],
    queryFn: () => withApi((api) => api.get<SmtpCredentials>("/smtp-credentials")),
    enabled: !!account,
  });
}

export function useCreateSmtpApiKey() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async (data: CreateApiKeyRequest) =>
      withApi((api) => api.post<CreatedApiKey>("/smtp-credentials", data)),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["smtp-credentials", account?.id],
      });
    },
  });
}

export function useRevokeSmtpApiKey() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async (keyId: string) =>
      withApi((api) => api.delete(`/smtp-credentials/${keyId}`)),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["smtp-credentials", account?.id],
      });
    },
  });
}

// Label hooks
export function useLabels() {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["labels", account?.id],
    queryFn: () => withApi((api) => api.get<Label[]>("/labels")),
    enabled: !!account,
  });
}

// Preference hooks
export function usePreferences() {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["preferences", account?.id],
    queryFn: () => withApi((api) => api.get<UserPreference>("/preferences")),
    enabled: !!account,
  });
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async (data: UpdateUserPreferenceRequest) =>
      withApi((api) => api.put<UserPreference>("/preferences", data)),
    onSuccess: (data) => {
      queryClient.setQueryData(["preferences", account?.id], data);
    },
  });
}

// Infinite scroll hook
export function useInfiniteEmails(pageSize = 20) {
  const account = useActiveAccount();

  return useInfiniteQuery({
    queryKey: ["emails", "infinite", account?.id, pageSize],
    queryFn: async ({ pageParam = 1 }) =>
      withApi((api) =>
        api.get<EmailListResponse>(
          `/emails?page=${pageParam}&pageSize=${pageSize}`
        )
      ),
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
    initialPageParam: 1,
    enabled: !!account,
  });
}

// Bulk operations hooks
export function useBulkMarkRead() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async ({
      emailIds,
      isRead,
    }: {
      emailIds: string[];
      isRead: boolean;
    }) => withApi((api) => api.post<void>("/emails/bulk/mark-read", { emailIds, isRead })),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["emails", account?.id] });
    },
  });
}

export function useBulkDelete() {
  const queryClient = useQueryClient();
  const account = useActiveAccount();

  return useMutation({
    mutationFn: async (emailIds: string[]) =>
      withApi((api) => api.post<void>("/emails/bulk/delete", { emailIds })),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["emails", account?.id] });
    },
  });
}

// Sent mail hooks
export function useSentEmails(fromAddress?: string, page = 1, pageSize = 20) {
  const account = useActiveAccount();
  const params = new URLSearchParams();
  params.set("page", page.toString());
  params.set("pageSize", pageSize.toString());
  if (fromAddress) params.set("fromAddress", fromAddress);

  return useQuery({
    queryKey: ["emails", "sent", account?.id, fromAddress, page, pageSize],
    queryFn: () =>
      withApi((api) =>
        api.get<EmailListResponse>(`/emails/sent?${params.toString()}`)
      ),
    enabled: !!account,
  });
}

export function useSentFromAddresses() {
  const account = useActiveAccount();

  return useQuery({
    queryKey: ["emails", "sent", "addresses", account?.id],
    queryFn: () => withApi((api) => api.get<string[]>("/emails/sent/addresses")),
    enabled: !!account,
  });
}
