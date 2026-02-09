import { Redirect } from "expo-router";
import { useHasAccounts } from "@/lib/auth/account-store";

export default function Index() {
  const hasAccounts = useHasAccounts();

  // Redirect to auth flow if no accounts, otherwise to main app
  if (hasAccounts) {
    return <Redirect href="/(main)/(tabs)/inbox" />;
  }

  return <Redirect href="/(auth)" />;
}
