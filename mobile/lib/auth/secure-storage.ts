import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";
import nacl from "tweetnacl";
import naclUtil from "tweetnacl-util";

const API_KEY_PREFIX = "relate_api_key_";
const WEB_ENCRYPTION_KEY = "_ek";

/**
 * Get or generate encryption key for web platform.
 * Key is stored in sessionStorage and cleared when browser closes.
 */
function getWebEncryptionKey(): Uint8Array {
  if (typeof sessionStorage === "undefined") {
    throw new Error("sessionStorage not available");
  }

  const stored = sessionStorage.getItem(WEB_ENCRYPTION_KEY);
  if (stored) {
    return naclUtil.decodeBase64(stored);
  }

  // Generate new random key
  const key = nacl.randomBytes(32);
  sessionStorage.setItem(WEB_ENCRYPTION_KEY, naclUtil.encodeBase64(key));
  return key;
}

/**
 * Encrypt a value for web storage using NaCl secretbox.
 */
function encryptForWeb(value: string): string {
  const key = getWebEncryptionKey();
  const nonce = nacl.randomBytes(24);
  const messageBytes = naclUtil.decodeUTF8(value);
  const encrypted = nacl.secretbox(messageBytes, nonce, key);

  return JSON.stringify({
    n: naclUtil.encodeBase64(nonce),
    d: naclUtil.encodeBase64(encrypted),
  });
}

/**
 * Decrypt a value from web storage using NaCl secretbox.
 */
function decryptForWeb(payload: string): string | null {
  try {
    const key = getWebEncryptionKey();
    const { n, d } = JSON.parse(payload);
    const nonce = naclUtil.decodeBase64(n);
    const encrypted = naclUtil.decodeBase64(d);

    const decrypted = nacl.secretbox.open(encrypted, nonce, key);
    if (!decrypted) {
      return null;
    }

    return naclUtil.encodeUTF8(decrypted);
  } catch {
    return null;
  }
}

/**
 * Securely store an API key for an account.
 * On native: Uses Expo SecureStore (Keychain/Keystore).
 * On web: Uses sessionStorage with NaCl encryption (cleared on browser close).
 */
export async function storeApiKey(
  accountId: string,
  apiKey: string
): Promise<void> {
  const key = `${API_KEY_PREFIX}${accountId}`;

  if (Platform.OS === "web") {
    // Web - use encrypted sessionStorage (cleared on browser close)
    const encrypted = encryptForWeb(apiKey);
    sessionStorage.setItem(key, encrypted);
    return;
  }

  await SecureStore.setItemAsync(key, apiKey, {
    keychainAccessible: SecureStore.WHEN_UNLOCKED,
  });
}

/**
 * Retrieve an API key for an account.
 */
export async function getApiKey(accountId: string): Promise<string | null> {
  const key = `${API_KEY_PREFIX}${accountId}`;

  if (Platform.OS === "web") {
    const encrypted = sessionStorage.getItem(key);
    if (!encrypted) {
      return null;
    }
    return decryptForWeb(encrypted);
  }

  return await SecureStore.getItemAsync(key);
}

/**
 * Delete an API key for an account.
 */
export async function deleteApiKey(accountId: string): Promise<void> {
  const key = `${API_KEY_PREFIX}${accountId}`;

  if (Platform.OS === "web") {
    sessionStorage.removeItem(key);
    return;
  }

  await SecureStore.deleteItemAsync(key);
}

/**
 * Check if secure storage is available.
 */
export async function isSecureStorageAvailable(): Promise<boolean> {
  if (Platform.OS === "web") {
    return typeof sessionStorage !== "undefined";
  }

  return await SecureStore.isAvailableAsync();
}
