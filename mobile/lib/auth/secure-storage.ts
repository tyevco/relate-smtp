import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const API_KEY_PREFIX = "relate_api_key_";

/**
 * Securely store an API key for an account
 */
export async function storeApiKey(
  accountId: string,
  apiKey: string
): Promise<void> {
  const key = `${API_KEY_PREFIX}${accountId}`;

  if (Platform.OS === "web") {
    // Web fallback - use localStorage (less secure)
    localStorage.setItem(key, apiKey);
    return;
  }

  await SecureStore.setItemAsync(key, apiKey, {
    keychainAccessible: SecureStore.WHEN_UNLOCKED,
  });
}

/**
 * Retrieve an API key for an account
 */
export async function getApiKey(accountId: string): Promise<string | null> {
  const key = `${API_KEY_PREFIX}${accountId}`;

  if (Platform.OS === "web") {
    return localStorage.getItem(key);
  }

  return await SecureStore.getItemAsync(key);
}

/**
 * Delete an API key for an account
 */
export async function deleteApiKey(accountId: string): Promise<void> {
  const key = `${API_KEY_PREFIX}${accountId}`;

  if (Platform.OS === "web") {
    localStorage.removeItem(key);
    return;
  }

  await SecureStore.deleteItemAsync(key);
}

/**
 * Check if secure storage is available
 */
export async function isSecureStorageAvailable(): Promise<boolean> {
  if (Platform.OS === "web") {
    return typeof localStorage !== "undefined";
  }

  return await SecureStore.isAvailableAsync();
}
