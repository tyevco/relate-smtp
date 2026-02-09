import { useCallback, useEffect, useState } from "react";
import { View, Text, TouchableOpacity, AppState } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Lock, Fingerprint } from "lucide-react-native";
import {
  useBiometricEnabled,
  authenticateWithBiometrics,
} from "@/lib/auth/biometric";
import { useHasAccounts } from "@/lib/auth/account-store";

/**
 * BiometricGate wraps the app content and requires biometric authentication
 * when the feature is enabled and the user has accounts.
 *
 * It re-prompts when the app returns from the background.
 */
export function BiometricGate({ children }: { children: React.ReactNode }) {
  const biometricEnabled = useBiometricEnabled();
  const hasAccounts = useHasAccounts();
  const [authenticated, setAuthenticated] = useState(false);

  const shouldRequireAuth = biometricEnabled && hasAccounts;

  const authenticate = useCallback(async () => {
    if (!shouldRequireAuth) {
      setAuthenticated(true);
      return;
    }

    const success = await authenticateWithBiometrics();
    if (success) {
      setAuthenticated(true);
    }
  }, [shouldRequireAuth]);

  // Authenticate on mount when biometrics are enabled
  useEffect(() => {
    if (shouldRequireAuth && !authenticated) {
      authenticate();
    } else if (!shouldRequireAuth) {
      setAuthenticated(true);
    }
  }, [shouldRequireAuth, authenticated, authenticate]);

  // Re-lock when app comes back from background
  useEffect(() => {
    if (!shouldRequireAuth) {
      return;
    }

    const subscription = AppState.addEventListener("change", (nextState) => {
      if (nextState === "background") {
        setAuthenticated(false);
      }
    });

    return () => {
      subscription.remove();
    };
  }, [shouldRequireAuth]);

  if (!shouldRequireAuth || authenticated) {
    return <>{children}</>;
  }

  return (
    <SafeAreaView className="flex-1 bg-background">
      <View className="flex-1 items-center justify-center px-8">
        <View className="mb-8 h-20 w-20 items-center justify-center rounded-full bg-primary/10">
          <Lock size={40} color="#1e293b" />
        </View>

        <Text className="mb-2 text-2xl font-bold text-foreground">
          Relate Mail
        </Text>
        <Text className="mb-8 text-center text-base text-muted-foreground">
          Authenticate to access your email
        </Text>

        <TouchableOpacity
          onPress={authenticate}
          className="flex-row items-center gap-3 rounded-xl bg-primary px-8 py-4"
          activeOpacity={0.8}
        >
          <Fingerprint size={24} color="#ffffff" />
          <Text className="text-lg font-semibold text-primary-foreground">
            Unlock
          </Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}
