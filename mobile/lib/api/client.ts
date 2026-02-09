import { useAccountStore, useActiveAccount } from "../auth/account-store";
import { getApiKey } from "../auth/secure-storage";
import {
  CertificatePinError,
  extractDomain,
  validatePin,
  pinOnFirstUse,
} from "../security/certificate-pinning";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export { CertificatePinError };

interface ApiClientConfig {
  baseUrl: string;
  apiKey?: string;
  jwtToken?: string;
  /** Skip certificate pin validation (e.g. for initial discovery) */
  skipPinValidation?: boolean;
}

/**
 * Validate that a server URL uses HTTPS.
 * Allows HTTP only for localhost during development.
 */
function validateServerUrl(url: string): void {
  const lowercaseUrl = url.toLowerCase();
  const isHttps = lowercaseUrl.startsWith("https://");
  const isLocalhost =
    lowercaseUrl.startsWith("http://localhost") ||
    lowercaseUrl.startsWith("http://127.0.0.1");

  if (!isHttps && !isLocalhost) {
    throw new Error(
      `Security error: Server URL must use HTTPS. Got: ${url}. ` +
      `HTTP is only allowed for localhost during development.`
    );
  }
}

/**
 * Validate server certificate pin using the X-Certificate-Fingerprint
 * header if provided by the server.
 *
 * This implements a Trust-on-First-Use (TOFU) model:
 * - If the server sends its certificate fingerprint and no pin exists,
 *   the fingerprint is stored automatically.
 * - If a pin exists, the fingerprint is validated against it.
 * - If there is a mismatch, a CertificatePinError is thrown.
 */
async function checkCertificatePin(
  baseUrl: string,
  response: Response
): Promise<void> {
  const fingerprint = response.headers?.get("X-Certificate-Fingerprint");
  if (!fingerprint) {
    return;
  }

  const domain = extractDomain(baseUrl);
  const fingerprints = fingerprint.split(",").map((fp) => fp.trim());
  const result = await validatePin(domain, fingerprints);

  if (result.error === "NO_PINS_CONFIGURED") {
    // TOFU: pin the certificate on first use
    await pinOnFirstUse(domain, fingerprints, {
      includeSubdomains: false,
      expiresInDays: 365,
    });
    if (__DEV__) {
      console.log(`[CertPin] Pinned certificate for ${domain} (TOFU)`);
    }
    return;
  }

  if (!result.valid) {
    throw new CertificatePinError(
      result.error!,
      domain,
      result.message ?? "Certificate pin validation failed"
    );
  }
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

    if (__DEV__) {
      console.log(`[API] ${method} ${url}`);
    }

    // Create AbortController with 30-second timeout
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 30000);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
        headers: {
          ...headers,
          ...(options.headers as Record<string, string>),
        },
      });

      clearTimeout(timeoutId);

      // Validate certificate pin unless explicitly skipped
      if (!config.skipPinValidation) {
        await checkCertificatePin(config.baseUrl, response);
      }

      if (__DEV__) {
        console.log(`[API] Response: ${response.status} ${response.statusText}`);
      }

      if (!response.ok) {
        const message = await response.text();
        if (__DEV__) {
          console.error(`[API] Error response body:`, message);
        }
        throw new ApiError(response.status, message || response.statusText);
      }

      if (response.status === 204) {
        return undefined as T;
      }

      const json = await response.json();
      return json;
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === "AbortError") {
        throw new ApiError(0, "Request timed out");
      }
      throw error;
    }
  };
}

/**
 * Create a complete API client for a specific configuration
 */
function createApiClient(config: ApiClientConfig) {
  // Validate HTTPS requirement for all API clients
  validateServerUrl(config.baseUrl);

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
    jwtToken,
  });
}

/**
 * Create an unauthenticated API client for discovery endpoints.
 * Pin validation is skipped for initial discovery requests since
 * pins are established during the TOFU flow on first authenticated use.
 */
export function createPublicApiClient(serverUrl: string) {
  return createApiClient({
    baseUrl: serverUrl,
    skipPinValidation: true,
  });
}

// Export types
export type ApiClient = ReturnType<typeof createApiClient>;
