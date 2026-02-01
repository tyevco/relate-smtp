import { useEffect } from "react";
import { SafeAreaView } from "react-native-safe-area-context";
import { router, useLocalSearchParams } from "expo-router";
import { Loading } from "@/components/ui/loading";

/**
 * This screen handles the OIDC callback redirect.
 * In most cases, expo-auth-session handles the redirect internally,
 * but this screen provides a fallback for deep link handling.
 */
export default function OidcCallbackScreen() {
  const params = useLocalSearchParams();

  useEffect(() => {
    // The callback is typically handled by expo-auth-session's promptAsync
    // This screen is mainly for handling edge cases or manual redirects
    console.log("OIDC Callback received:", params);

    // Redirect back to add-account flow
    // The actual token exchange happens in the add-account screen
    router.replace("/(auth)/add-account");
  }, [params]);

  return (
    <SafeAreaView className="flex-1 bg-background">
      <Loading message="Completing authentication..." />
    </SafeAreaView>
  );
}
