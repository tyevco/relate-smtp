import * as LocalAuthentication from "expo-local-authentication";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { Platform } from "react-native";
import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";

const BIOMETRIC_ENABLED_KEY = "relate_biometric_enabled";

/**
 * Check if biometric hardware is available on the device.
 */
export async function isBiometricAvailable(): Promise<boolean> {
  if (Platform.OS === "web") {
    return false;
  }

  const compatible = await LocalAuthentication.hasHardwareAsync();
  if (!compatible) {
    return false;
  }

  const enrolled = await LocalAuthentication.isEnrolledAsync();
  return enrolled;
}

/**
 * Get the supported biometric authentication types.
 * Returns human-readable names (e.g., "Face ID", "Fingerprint").
 */
export async function getBiometricType(): Promise<string | null> {
  if (Platform.OS === "web") {
    return null;
  }

  const types =
    await LocalAuthentication.supportedAuthenticationTypesAsync();

  if (types.includes(LocalAuthentication.AuthenticationType.FACIAL_RECOGNITION)) {
    return Platform.OS === "ios" ? "Face ID" : "Face Recognition";
  }

  if (types.includes(LocalAuthentication.AuthenticationType.FINGERPRINT)) {
    return Platform.OS === "ios" ? "Touch ID" : "Fingerprint";
  }

  if (types.includes(LocalAuthentication.AuthenticationType.IRIS)) {
    return "Iris";
  }

  return null;
}

/**
 * Prompt the user for biometric authentication.
 * Returns true if authentication succeeded, false otherwise.
 */
export async function authenticateWithBiometrics(
  promptMessage = "Authenticate to access Relate Mail"
): Promise<boolean> {
  const result = await LocalAuthentication.authenticateAsync({
    promptMessage,
    cancelLabel: "Cancel",
    disableDeviceFallback: false,
    fallbackLabel: "Use Passcode",
  });

  return result.success;
}

interface BiometricState {
  enabled: boolean;
  setEnabled: (enabled: boolean) => void;
}

/**
 * Zustand store for biometric preference.
 * Persisted to AsyncStorage so the setting survives app restarts.
 */
export const useBiometricStore = create<BiometricState>()(
  persist(
    (set) => ({
      enabled: false,

      setEnabled: (enabled: boolean) => {
        set({ enabled });
      },
    }),
    {
      name: BIOMETRIC_ENABLED_KEY,
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        enabled: state.enabled,
      }),
    }
  )
);

/**
 * Hook to get the current biometric enabled state.
 */
export function useBiometricEnabled() {
  return useBiometricStore((state) => state.enabled);
}
