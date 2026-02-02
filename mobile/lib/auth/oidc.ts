import * as AuthSession from "expo-auth-session";
import * as WebBrowser from "expo-web-browser";
import * as Crypto from "expo-crypto";
import { Platform } from "react-native";
import type { OidcConfig, ServerDiscovery } from "../api/types";

// Ensure browser redirect is handled properly
WebBrowser.maybeCompleteAuthSession();

// Redirect URI for OAuth
const redirectUri = AuthSession.makeRedirectUri({
  scheme: "relate-mail",
  path: "auth/callback",
});

export interface OidcResult {
  accessToken: string;
  idToken?: string;
  refreshToken?: string;
  expiresIn?: number;
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
  // Normalize URL
  const baseUrl = serverUrl.replace(/\/$/, "");
  const discoveryUrl = `${baseUrl}/api/discovery`;

  // Fetch server discovery
  let discoveryResponse: Response;
  try {
    discoveryResponse = await fetch(discoveryUrl);
  } catch (err) {
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
    const configResponse = await fetch(configUrl);
    if (configResponse.ok) {
      const config = await parseJsonResponse<{
        oidc?: { authority?: string; clientId?: string; scopes?: string[] };
      }>(configResponse, configUrl);
      oidcConfig = {
        authority: config.oidc?.authority,
        clientId: config.oidc?.clientId,
        scopes: config.oidc?.scopes || ["openid", "profile", "email"],
      };
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
  // Discover OIDC endpoints
  const discovery = await AuthSession.fetchDiscoveryAsync(oidcConfig.authority);

  // Generate PKCE code verifier and challenge
  const codeVerifier = generateCodeVerifier();
  const codeChallenge = await generateCodeChallenge(codeVerifier);

  // Create auth request
  const authRequest = new AuthSession.AuthRequest({
    clientId: oidcConfig.clientId,
    scopes: oidcConfig.scopes,
    redirectUri,
    codeChallenge,
    codeChallengeMethod: AuthSession.CodeChallengeMethod.S256,
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

  // Exchange code for tokens
  const tokenResult = await AuthSession.exchangeCodeAsync(
    {
      clientId: oidcConfig.clientId,
      code: result.params.code,
      redirectUri,
      extraParams: {
        code_verifier: codeVerifier,
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
 * Generate a random code verifier for PKCE
 */
function generateCodeVerifier(): string {
  const randomBytes = Crypto.getRandomBytes(32);
  return base64UrlEncode(randomBytes);
}

/**
 * Generate code challenge from verifier using SHA-256
 */
async function generateCodeChallenge(verifier: string): Promise<string> {
  const digest = await Crypto.digestStringAsync(
    Crypto.CryptoDigestAlgorithm.SHA256,
    verifier,
    { encoding: Crypto.CryptoEncoding.BASE64 }
  );
  // Convert base64 to base64url
  return digest.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

/**
 * Base64 URL encode
 */
function base64UrlEncode(bytes: Uint8Array): string {
  const base64 = btoa(String.fromCharCode(...bytes));
  return base64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
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
