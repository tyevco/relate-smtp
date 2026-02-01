import { useAccountStore, useActiveAccount } from "../auth/account-store";
import { getApiKey } from "../auth/secure-storage";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message);
    this.name = "ApiError";
  }
}

interface ApiClientConfig {
  baseUrl: string;
  apiKey?: string;
}

/**
 * Create an API request function for a specific account configuration
 */
function createApiRequest(config: ApiClientConfig) {
  return async function apiRequest<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };

    if (config.apiKey) {
      headers["Authorization"] = `Bearer ${config.apiKey}`;
    }

    const response = await fetch(`${config.baseUrl}/api${endpoint}`, {
      ...options,
      headers: {
        ...headers,
        ...(options.headers as Record<string, string>),
      },
    });

    if (!response.ok) {
      const message = await response.text();
      throw new ApiError(response.status, message || response.statusText);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return response.json();
  };
}

/**
 * Create a complete API client for a specific configuration
 */
function createApiClient(config: ApiClientConfig) {
  const request = createApiRequest(config);

  return {
    baseUrl: config.baseUrl,
    get: <T>(endpoint: string) => request<T>(endpoint),
    post: <T>(endpoint: string, data: unknown) =>
      request<T>(endpoint, {
        method: "POST",
        body: JSON.stringify(data),
      }),
    put: <T>(endpoint: string, data: unknown) =>
      request<T>(endpoint, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    patch: <T>(endpoint: string, data: unknown) =>
      request<T>(endpoint, {
        method: "PATCH",
        body: JSON.stringify(data),
      }),
    delete: (endpoint: string) =>
      request<void>(endpoint, {
        method: "DELETE",
      }),
  };
}

/**
 * Get an API client for the active account
 */
export async function getActiveApiClient() {
  const state = useAccountStore.getState();
  const activeAccount = state.accounts.find(
    (a) => a.id === state.activeAccountId
  );

  if (!activeAccount) {
    throw new Error("No active account");
  }

  const apiKey = await getApiKey(activeAccount.id);
  if (!apiKey) {
    throw new Error("API key not found for account");
  }

  // Update last used timestamp
  state.updateLastUsed(activeAccount.id);

  return createApiClient({
    baseUrl: activeAccount.serverUrl,
    apiKey,
  });
}

/**
 * Get an API client for a specific account
 */
export async function getApiClientForAccount(accountId: string) {
  const state = useAccountStore.getState();
  const account = state.accounts.find((a) => a.id === accountId);

  if (!account) {
    throw new Error(`Account not found: ${accountId}`);
  }

  const apiKey = await getApiKey(accountId);
  if (!apiKey) {
    throw new Error("API key not found for account");
  }

  return createApiClient({
    baseUrl: account.serverUrl,
    apiKey,
  });
}

/**
 * Create an API client for a server URL with a temporary JWT token
 * Used during the OIDC flow before an API key is created
 */
export function createTempApiClient(serverUrl: string, jwtToken: string) {
  return createApiClient({
    baseUrl: serverUrl,
    apiKey: jwtToken, // JWT is sent as Bearer token same as API key
  });
}

/**
 * Create an unauthenticated API client for discovery endpoints
 */
export function createPublicApiClient(serverUrl: string) {
  return createApiClient({
    baseUrl: serverUrl,
  });
}

// Export types
export type ApiClient = ReturnType<typeof createApiClient>;
