const API_BASE = import.meta.env.VITE_API_URL || '/api'

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
    this.name = 'ApiError'
  }
}

function safeStorageAccess<T>(
  accessor: () => T,
  fallback: T
): T {
  try {
    return accessor();
  } catch {
    return fallback;
  }
}

async function getAuthHeader(): Promise<Record<string, string>> {
  // Try sessionStorage first (where OIDC stores tokens), then localStorage as fallback
  // Use safe storage access to handle private browsing mode where storage may throw
  const storageKey = safeStorageAccess(
    () => Object.keys(sessionStorage).find(key => key.startsWith('oidc.user:')),
    undefined
  );

  const token = safeStorageAccess(
    () => storageKey ? sessionStorage.getItem(storageKey) : localStorage.getItem('oidc.user'),
    null
  );

  if (token) {
    try {
      const user = JSON.parse(token);
      const accessToken = user?.access_token;
      if (accessToken) {
        return { Authorization: `Bearer ${accessToken}` };
      }
    } catch (error) {
      console.error('Failed to parse user token:', error);
    }
  }

  return {};
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const authHeaders = await getAuthHeader()

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...authHeaders,
      ...options.headers,
    },
  })

  if (!response.ok) {
    const message = await response.text()
    throw new ApiError(response.status, message || response.statusText)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json()
}

export const api = {
  baseUrl: API_BASE,
  getHeaders: async () => {
    const authHeaders = await getAuthHeader()
    return {
      'Content-Type': 'application/json',
      ...authHeaders,
    }
  },
  get: <T>(endpoint: string) => apiRequest<T>(endpoint),
  post: <T>(endpoint: string, data: unknown) =>
    apiRequest<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  put: <T>(endpoint: string, data: unknown) =>
    apiRequest<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  patch: <T>(endpoint: string, data: unknown) =>
    apiRequest<T>(endpoint, {
      method: 'PATCH',
      body: JSON.stringify(data),
    }),
  delete: (endpoint: string) =>
    apiRequest<undefined>(endpoint, {
      method: 'DELETE',
    }),
}
