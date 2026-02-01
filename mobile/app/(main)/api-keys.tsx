import { View, Text, TouchableOpacity, ScrollView, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { ArrowLeft, Key, Trash2 } from "lucide-react-native";
import { useSmtpCredentials, useRevokeSmtpApiKey } from "@/lib/api/hooks";
import { Loading } from "@/components/ui/loading";
import { EmptyState } from "@/components/ui/empty-state";
import { Card, CardContent } from "@/components/ui/card";
import { formatDate } from "@/lib/utils";

export default function ApiKeysScreen() {
  const { data, isLoading, isError } = useSmtpCredentials();
  const revokeKey = useRevokeSmtpApiKey();

  const handleRevokeKey = (keyId: string, keyName: string) => {
    Alert.alert(
      "Revoke API Key",
      `Are you sure you want to revoke "${keyName}"? This action cannot be undone, and any devices using this key will lose access.`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Revoke",
          style: "destructive",
          onPress: () => revokeKey.mutate(keyId),
        },
      ]
    );
  };

  if (isLoading) {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
          <TouchableOpacity onPress={() => router.back()}>
            <ArrowLeft size={24} color="#1e293b" />
          </TouchableOpacity>
          <Text className="text-xl font-semibold text-foreground">
            API Keys
          </Text>
        </View>
        <Loading message="Loading API keys..." />
      </SafeAreaView>
    );
  }

  if (isError) {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
          <TouchableOpacity onPress={() => router.back()}>
            <ArrowLeft size={24} color="#1e293b" />
          </TouchableOpacity>
          <Text className="text-xl font-semibold text-foreground">
            API Keys
          </Text>
        </View>
        <EmptyState
          title="Failed to load API keys"
          description="Please try again later"
        />
      </SafeAreaView>
    );
  }

  const keys = data?.keys ?? [];

  return (
    <SafeAreaView className="flex-1 bg-background">
      {/* Header */}
      <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
        <TouchableOpacity onPress={() => router.back()}>
          <ArrowLeft size={24} color="#1e293b" />
        </TouchableOpacity>
        <Text className="text-xl font-semibold text-foreground">API Keys</Text>
      </View>

      {keys.length === 0 ? (
        <EmptyState
          icon={<Key size={48} color="#94a3b8" />}
          title="No API keys"
          description="Create API keys from the web interface to use SMTP, POP3, and IMAP"
        />
      ) : (
        <ScrollView className="flex-1 p-4">
          <Text className="mb-2 px-1 text-sm font-medium text-muted-foreground">
            {keys.length} key{keys.length !== 1 ? "s" : ""}
          </Text>

          <View className="gap-3">
            {keys.map((key) => (
              <Card key={key.id}>
                <CardContent className="py-4">
                  <View className="flex-row items-start justify-between">
                    <View className="flex-1">
                      <View className="flex-row items-center gap-2">
                        <Key size={18} color="#64748b" />
                        <Text className="font-semibold text-foreground">
                          {key.name}
                        </Text>
                      </View>

                      <View className="mt-2 flex-row flex-wrap gap-1">
                        {key.scopes.map((scope) => (
                          <View
                            key={scope}
                            className="rounded bg-secondary px-2 py-0.5"
                          >
                            <Text className="text-xs text-secondary-foreground">
                              {scope}
                            </Text>
                          </View>
                        ))}
                      </View>

                      <View className="mt-2 gap-0.5">
                        <Text className="text-xs text-muted-foreground">
                          Created: {formatDate(key.createdAt)}
                        </Text>
                        {key.lastUsedAt && (
                          <Text className="text-xs text-muted-foreground">
                            Last used: {formatDate(key.lastUsedAt)}
                          </Text>
                        )}
                      </View>
                    </View>

                    <TouchableOpacity
                      onPress={() => handleRevokeKey(key.id, key.name)}
                      className="p-2"
                      hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
                    >
                      <Trash2 size={20} color="#dc2626" />
                    </TouchableOpacity>
                  </View>
                </CardContent>
              </Card>
            ))}
          </View>

          <Card className="mt-6 bg-secondary/30">
            <CardContent className="py-3">
              <Text className="text-sm text-muted-foreground">
                To create new API keys, use the web interface at your Relate
                server. New keys can be created with specific scopes for SMTP,
                POP3, IMAP, and API access.
              </Text>
            </CardContent>
          </Card>
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
