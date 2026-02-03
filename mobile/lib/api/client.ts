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
  jwtToken?: string;
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

    if (config.jwtToken) {
      // JWT tokens use Bearer prefix
      headers["Authorization"] = `Bearer ${config.jwtToken}`;
    } else if (config.apiKey) {
      // API keys use ApiKey prefix to avoid JWT handler intercepting
      headers["Authorization"] = `ApiKey ${config.apiKey}`;
    }

    const url = `${config.baseUrl}/api${endpoint}`;
    const method = options.method || "GET";

    console.log(`[API] ${method} ${url}`);
    console.log(`[API] Auth: ${config.jwtToken ? "JWT" : config.apiKey ? "ApiKey" : "none"}, length: ${(config.jwtToken || config.apiKey)?.length || 0}`);
    if (options.body) {
      console.log("[API] Request body:", options.body);
    }

    const response = await fetch(url, {
      ...options,
      headers: {
        ...headers,
        ...(options.headers as Record<string, string>),
      },
    });

    console.log(`[API] Response: ${response.status} ${response.statusText}`);

    if (!response.ok) {
      const message = await response.text();
      console.error(`[API] Error response body:`, message);
      throw new ApiError(response.status, message || response.statusText);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const json = await response.json();
    console.log("[API] Response body:", JSON.stringify(json).slice(0, 200));
    return json;
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

  console.log("[API] getActiveApiClient - activeAccountId:", state.activeAccountId);
  console.log("[API] getActiveApiClient - found account:", !!activeAccount);

  if (!activeAccount) {
    throw new Error("No active account");
  }

  const apiKey = await getApiKey(activeAccount.id);
  console.log("[API] getActiveApiClient - got API key:", !!apiKey, "length:", apiKey?.length || 0);

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
    jwtToken,
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
