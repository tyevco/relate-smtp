import * as AuthSession from "expo-auth-session";
import * as WebBrowser from "expo-web-browser";
import { Platform } from "react-native";
import type { OidcConfig, ServerDiscovery } from "../api/types";

// Ensure browser redirect is handled properly
WebBrowser.maybeCompleteAuthSession();

// Redirect URI for OAuth
const redirectUri = AuthSession.makeRedirectUri({
  scheme: "relate-mail",
  path: "auth/callback",
});

// Debug: log the redirect URI at module load (dev only)
if (__DEV__) {
  console.log("[OIDC] Redirect URI:", redirectUri);
}

export interface OidcResult {
  accessToken: string;
  idToken?: string;
  refreshToken?: string;
  expiresIn?: number;
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
 * Fetch with timeout to prevent indefinite hangs
 */
async function fetchWithTimeout(url: string, timeoutMs = 10000): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(url, { signal: controller.signal });
  } catch (err) {
    if (err instanceof Error && err.name === "AbortError") {
      throw new Error(`Request to ${url} timed out after ${timeoutMs}ms`);
    }
    throw err;
  } finally {
    clearTimeout(timeoutId);
  }
}

/**
 * Parse JSON response with better error handling
 */
async function parseJsonResponse<T>(response: Response, url: string): Promise<T> {
  const contentType = response.headers.get("content-type");
  if (!contentType?.includes("application/json")) {
    const text = await response.text();
    throw new Error(
      `Server returned non-JSON response (${contentType || "unknown"}). ` +
      `Expected JSON from ${url}. Response: ${text.slice(0, 100)}...`
    );
  }
  return response.json();
}

/**
 * Discover server capabilities and OIDC configuration
 */
export async function discoverServer(
  serverUrl: string
): Promise<{ discovery: ServerDiscovery; oidcConfig?: OidcConfig }> {
  // Validate HTTPS requirement
  validateServerUrl(serverUrl);

  // Normalize URL
  const baseUrl = serverUrl.replace(/\/$/, "");
  const discoveryUrl = `${baseUrl}/api/discovery`;

  // Fetch server discovery
  let discoveryResponse: Response;
  try {
    discoveryResponse = await fetchWithTimeout(discoveryUrl);
  } catch (err) {
    if (err instanceof Error && err.message.includes("timed out")) {
      throw err;
    }
    throw new Error(
      `Cannot connect to server at ${baseUrl}. Please check the URL and your network connection.`
    );
  }

  if (!discoveryResponse.ok) {
    throw new Error(
      `Server returned ${discoveryResponse.status}: ${discoveryResponse.statusText}. ` +
      `Make sure ${baseUrl} is a valid Relate server.`
    );
  }

  const discovery = await parseJsonResponse<ServerDiscovery>(
    discoveryResponse,
    discoveryUrl
  );

  // If OIDC is enabled, fetch the config
  let oidcConfig: OidcConfig | undefined;
  if (discovery.oidcEnabled) {
    const configUrl = `${baseUrl}/config/config.json`;
    const configResponse = await fetchWithTimeout(configUrl);
    if (configResponse.ok) {
      const config = await parseJsonResponse<{
        oidcAuthority?: string;
        oidcClientId?: string;
        oidcScope?: string;
      }>(configResponse, configUrl);
      // Only set oidcConfig if required fields are present
      if (config.oidcAuthority && config.oidcClientId) {
        oidcConfig = {
          authority: config.oidcAuthority,
          clientId: config.oidcClientId,
          scopes: config.oidcScope?.split(" ") || ["openid", "profile", "email"],
        };
      }
    }
  }

  return { discovery, oidcConfig };
}

/**
 * Perform OIDC authentication with PKCE
 */
export async function performOidcAuth(
  oidcConfig: OidcConfig
): Promise<OidcResult> {
  if (__DEV__) {
    console.log("[OIDC] Starting auth with config:", {
      authority: oidcConfig.authority,
      clientId: oidcConfig.clientId,
      scopes: oidcConfig.scopes,
    });
  }

  // Validate required config
  if (!oidcConfig.authority) {
    throw new Error("OIDC authority URL is not configured on the server");
  }
  if (!oidcConfig.clientId) {
    throw new Error("OIDC client ID is not configured on the server");
  }

  // Discover OIDC endpoints
  const discovery = await AuthSession.fetchDiscoveryAsync(oidcConfig.authority);

  // Create auth request with PKCE (AuthSession handles verifier/challenge internally)
  const authRequest = new AuthSession.AuthRequest({
    clientId: oidcConfig.clientId,
    scopes: oidcConfig.scopes,
    redirectUri,
    usePKCE: true,
  });

  // Prompt for authorization
  const result = await authRequest.promptAsync(discovery);

  if (result.type !== "success") {
    throw new Error(`Authentication failed: ${result.type}`);
  }

  if (!result.params.code) {
    throw new Error("No authorization code received");
  }

  // Exchange code for tokens using the AuthRequest's internal codeVerifier
  const tokenResult = await AuthSession.exchangeCodeAsync(
    {
      clientId: oidcConfig.clientId,
      code: result.params.code,
      redirectUri,
      extraParams: {
        code_verifier: authRequest.codeVerifier!,
      },
    },
    discovery
  );

  return {
    accessToken: tokenResult.accessToken,
    idToken: tokenResult.idToken,
    refreshToken: tokenResult.refreshToken ?? undefined,
    expiresIn: tokenResult.expiresIn ?? undefined,
  };
}

/**
 * Get the redirect URI for this app
 */
export function getRedirectUri(): string {
  return redirectUri;
}

/**
 * Get the platform identifier for API key creation
 */
export function getPlatform(): "ios" | "android" | "windows" | "macos" | "web" {
  switch (Platform.OS) {
    case "ios":
      return "ios";
    case "android":
      return "android";
    case "windows":
      return "windows";
    case "macos":
      return "macos";
    default:
      return "web";
  }
}
