import { View, Text, TouchableOpacity, ScrollView, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { ArrowLeft, Plus, Check, Trash2, Server } from "lucide-react-native";
import { useAccountStore, useAccounts, useActiveAccount } from "@/lib/auth/account-store";
import { Avatar } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatDate } from "@/lib/utils";

export default function AccountsScreen() {
  const accounts = useAccounts();
  const activeAccount = useActiveAccount();
  const setActiveAccount = useAccountStore((state) => state.setActiveAccount);
  const removeAccount = useAccountStore((state) => state.removeAccount);

  const handleSelectAccount = (accountId: string) => {
    setActiveAccount(accountId);
    router.back();
  };

  const handleRemoveAccount = (accountId: string, displayName: string) => {
    Alert.alert(
      "Remove Account",
      `Are you sure you want to remove "${displayName}"? This will delete the stored API key.`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Remove",
          style: "destructive",
          onPress: async () => {
            await removeAccount(accountId);
          },
        },
      ]
    );
  };

  const handleAddAccount = () => {
    router.push("/(auth)/add-account");
  };

  return (
    <SafeAreaView className="flex-1 bg-background">
      {/* Header */}
      <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
        <TouchableOpacity onPress={() => router.back()}>
          <ArrowLeft size={24} color="#1e293b" />
        </TouchableOpacity>
        <Text className="flex-1 text-xl font-semibold text-foreground">
          Accounts
        </Text>
        <TouchableOpacity onPress={handleAddAccount}>
          <Plus size={24} color="#1e293b" />
        </TouchableOpacity>
      </View>

      {accounts.length === 0 ? (
        <EmptyState
          icon={<Server size={48} color="#94a3b8" />}
          title="No accounts"
          description="Add an account to get started"
          action={
            <Button onPress={handleAddAccount}>
              Add Account
            </Button>
          }
        />
      ) : (
        <ScrollView className="flex-1 p-4">
          <Text className="mb-2 px-1 text-sm font-medium text-muted-foreground">
            {accounts.length} account{accounts.length !== 1 ? "s" : ""}
          </Text>

          <View className="gap-3">
            {accounts.map((account) => (
              <TouchableOpacity
                key={account.id}
                onPress={() => handleSelectAccount(account.id)}
                className={`flex-row items-center gap-3 rounded-lg border p-4 ${
                  account.id === activeAccount?.id
                    ? "border-primary bg-primary/5"
                    : "border-border bg-card"
                }`}
                activeOpacity={0.7}
              >
                <Avatar
                  name={account.displayName}
                  email={account.userEmail}
                  size="lg"
                />

                <View className="flex-1">
                  <View className="flex-row items-center gap-2">
                    <Text className="font-semibold text-foreground">
                      {account.displayName}
                    </Text>
                    {account.id === activeAccount?.id && (
                      <Check size={16} color="#16a34a" />
                    )}
                  </View>
                  <Text className="text-sm text-muted-foreground">
                    {account.userEmail}
                  </Text>
                  <Text className="text-xs text-muted-foreground">
                    {new URL(account.serverUrl).host}
                  </Text>
                  <Text className="mt-1 text-xs text-muted-foreground">
                    Last used: {formatDate(account.lastUsedAt)}
                  </Text>
                </View>

                <TouchableOpacity
                  onPress={() =>
                    handleRemoveAccount(account.id, account.displayName)
                  }
                  className="p-2"
                  hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
                >
                  <Trash2 size={20} color="#dc2626" />
                </TouchableOpacity>
              </TouchableOpacity>
            ))}
          </View>

          <Button
            variant="outline"
            onPress={handleAddAccount}
            className="mt-6"
          >
            <View className="flex-row items-center gap-2">
              <Plus size={20} color="#1e293b" />
              <Text className="font-semibold text-foreground">
                Add Another Account
              </Text>
            </View>
          </Button>
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
