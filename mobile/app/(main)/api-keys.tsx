import { useState } from "react";
import { View, Text, TouchableOpacity, ScrollView, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { ArrowLeft, Key, Trash2, RefreshCw, AlertTriangle } from "lucide-react-native";
import { useSmtpCredentials, useRevokeSmtpApiKey, useRotateSmtpApiKey } from "@/lib/api/hooks";
import { useActiveAccount, useAccountStore } from "@/lib/auth/account-store";
import { storeApiKey } from "@/lib/auth/secure-storage";
import { Loading } from "@/components/ui/loading";
import { EmptyState } from "@/components/ui/empty-state";
import { Card, CardContent } from "@/components/ui/card";
import { formatDate } from "@/lib/utils";
import type { SmtpApiKey } from "@/lib/api/types";

const KEY_AGE_WARNING_DAYS = 90;

function getKeyAgeDays(createdAt: string): number {
  const created = new Date(createdAt);
  const now = new Date();
  return Math.floor((now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24));
}

function isKeyOld(createdAt: string): boolean {
  return getKeyAgeDays(createdAt) >= KEY_AGE_WARNING_DAYS;
}

export default function ApiKeysScreen() {
  const { data, isLoading, isError } = useSmtpCredentials();
  const revokeKey = useRevokeSmtpApiKey();
  const rotateKey = useRotateSmtpApiKey();
  const account = useActiveAccount();
  const updateAccount = useAccountStore((state) => state.updateAccount);
  const [rotatingKeyId, setRotatingKeyId] = useState<string | null>(null);

  const handleRevokeKey = (keyId: string, keyName: string) => {
    if (account && keyId === account.apiKeyId) {
      Alert.alert(
        "Cannot Revoke Active Key",
        "This key is used by this device. Rotate it instead to replace it with a new key."
      );
      return;
    }

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

  const handleRotateKey = (apiKey: SmtpApiKey) => {
    const isActiveKey = account && apiKey.id === account.apiKeyId;

    Alert.alert(
      "Rotate API Key",
      isActiveKey
        ? `This will replace "${apiKey.name}" with a new key. This device will automatically use the new key. The old key will be revoked.`
        : `This will replace "${apiKey.name}" with a new key. Any devices using the old key will need to be updated. The old key will be revoked.`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Rotate",
          onPress: async () => {
            setRotatingKeyId(apiKey.id);
            try {
              const result = await rotateKey.mutateAsync(apiKey.id);

              // If this was the active account's key, update local storage
              if (isActiveKey && account) {
                await storeApiKey(account.id, result.apiKey);
                updateAccount(account.id, {
                  apiKeyId: result.id,
                  createdAt: result.createdAt,
                });
              }

              Alert.alert(
                "Key Rotated",
                isActiveKey
                  ? "Your API key has been rotated. This device is now using the new key."
                  : "The API key has been rotated. Update any devices that were using the old key."
              );
            } catch {
              Alert.alert(
                "Rotation Failed",
                "Failed to rotate the API key. Please try again."
              );
            } finally {
              setRotatingKeyId(null);
            }
          },
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
  const oldKeys = keys.filter((k) => isKeyOld(k.createdAt));

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
          {/* Key age warning banner */}
          {oldKeys.length > 0 && (
            <Card className="mb-4 border-amber-300 bg-amber-50">
              <CardContent className="flex-row items-start gap-3 py-3">
                <AlertTriangle size={20} color="#d97706" />
                <View className="flex-1">
                  <Text className="font-medium text-amber-800">
                    {oldKeys.length === 1
                      ? "1 key is older than 90 days"
                      : `${oldKeys.length} keys are older than 90 days`}
                  </Text>
                  <Text className="mt-1 text-sm text-amber-700">
                    Consider rotating old keys to maintain security. Tap the
                    rotate button on a key to replace it.
                  </Text>
                </View>
              </CardContent>
            </Card>
          )}

          <Text className="mb-2 px-1 text-sm font-medium text-muted-foreground">
            {keys.length} key{keys.length !== 1 ? "s" : ""}
          </Text>

          <View className="gap-3">
            {keys.map((key) => {
              const ageDays = getKeyAgeDays(key.createdAt);
              const old = ageDays >= KEY_AGE_WARNING_DAYS;
              const isActive = account?.apiKeyId === key.id;
              const isRotating = rotatingKeyId === key.id;

              return (
                <Card key={key.id} className={old ? "border-amber-300" : ""}>
                  <CardContent className="py-4">
                    <View className="flex-row items-start justify-between">
                      <View className="flex-1">
                        <View className="flex-row items-center gap-2">
                          <Key size={18} color="#64748b" />
                          <Text className="font-semibold text-foreground">
                            {key.name}
                          </Text>
                          {isActive && (
                            <View className="rounded bg-primary/10 px-2 py-0.5">
                              <Text className="text-xs font-medium text-primary">
                                This device
                              </Text>
                            </View>
                          )}
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
                          <View className="flex-row items-center gap-1">
                            <Text className="text-xs text-muted-foreground">
                              Created: {formatDate(key.createdAt)}
                            </Text>
                            {old && (
                              <View className="flex-row items-center gap-1 rounded bg-amber-100 px-1.5 py-0.5">
                                <AlertTriangle size={10} color="#d97706" />
                                <Text className="text-xs font-medium text-amber-700">
                                  {ageDays}d old
                                </Text>
                              </View>
                            )}
                          </View>
                          {key.lastUsedAt && (
                            <Text className="text-xs text-muted-foreground">
                              Last used: {formatDate(key.lastUsedAt)}
                            </Text>
                          )}
                        </View>
                      </View>

                      <View className="flex-row items-center gap-1">
                        <TouchableOpacity
                          onPress={() => handleRotateKey(key)}
                          className="p-2"
                          hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
                          disabled={isRotating}
                        >
                          <RefreshCw
                            size={20}
                            color={isRotating ? "#94a3b8" : "#3b82f6"}
                          />
                        </TouchableOpacity>
                        <TouchableOpacity
                          onPress={() => handleRevokeKey(key.id, key.name)}
                          className="p-2"
                          hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
                        >
                          <Trash2 size={20} color="#dc2626" />
                        </TouchableOpacity>
                      </View>
                    </View>
                  </CardContent>
                </Card>
              );
            })}
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
