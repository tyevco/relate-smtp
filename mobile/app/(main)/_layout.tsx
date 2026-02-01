import { Stack } from "expo-router";
import { useHasAccounts } from "@/lib/auth/account-store";
import { Redirect } from "expo-router";

export default function MainLayout() {
  const hasAccounts = useHasAccounts();

  // Redirect to auth flow if no accounts
  if (!hasAccounts) {
    return <Redirect href="/(auth)" />;
  }

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="(tabs)" />
      <Stack.Screen
        name="emails/[id]"
        options={{
          presentation: "modal",
          animation: "slide_from_bottom",
        }}
      />
      <Stack.Screen name="accounts" />
      <Stack.Screen name="api-keys" />
      <Stack.Screen name="preferences" />
    </Stack>
  );
}
