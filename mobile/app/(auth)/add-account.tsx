import { useState } from "react";
import { View, Text, Alert, KeyboardAvoidingView, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { ArrowLeft, Server, ExternalLink } from "lucide-react-native";
import * as Crypto from "expo-crypto";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Loading } from "@/components/ui/loading";
import { useAccountStore } from "@/lib/auth/account-store";
import { storeApiKey } from "@/lib/auth/secure-storage";
import {
  discoverServer,
  performOidcAuth,
  getPlatform,
} from "@/lib/auth/oidc";
import { createTempApiClient } from "@/lib/api/client";
import type { CreatedApiKey, Profile } from "@/lib/api/types";

type Step = "url" | "discovering" | "authenticating" | "creating-key";

export default function AddAccountScreen() {
  const [serverUrl, setServerUrl] = useState("");
  const [step, setStep] = useState<Step>("url");
  const [error, setError] = useState<string | null>(null);
  const addAccount = useAccountStore((state) => state.addAccount);

  const normalizeUrl = (url: string): string => {
    let normalized = url.trim();
    if (!normalized.startsWith("http://") && !normalized.startsWith("https://")) {
      normalized = `https://${normalized}`;
    }
    return normalized.replace(/\/$/, "");
  };

  const handleConnect = async () => {
    if (!serverUrl.trim()) {
      setError("Please enter a server URL");
      return;
    }

    const normalizedUrl = normalizeUrl(serverUrl);
    setError(null);

    try {
      // Step 1: Discover server
      setStep("discovering");
      const { discovery, oidcConfig } = await discoverServer(normalizedUrl);

      if (!discovery.oidcEnabled || !oidcConfig) {
        Alert.alert(
          "Server Error",
          "This server does not have OIDC authentication enabled. Please contact your administrator.",
          [{ text: "OK", onPress: () => setStep("url") }]
        );
        return;
      }

      // Step 2: Perform OIDC authentication
      setStep("authenticating");
      const oidcResult = await performOidcAuth(oidcConfig);

      // Step 3: Create API key using the JWT
      setStep("creating-key");
      const tempApi = createTempApiClient(normalizedUrl, oidcResult.accessToken);

      // Get user profile first
      const profile = await tempApi.get<Profile>("/profile");

      // Create mobile API key
      const platform = getPlatform();
      const deviceName = Platform.OS === "web" ? "Web Browser" : `Mobile ${platform}`;

      const apiKeyResponse = await tempApi.post<CreatedApiKey>(
        "/smtp-credentials/mobile",
        {
          deviceName,
          platform,
        }
      );

      // Store the API key securely
      const accountId = Crypto.randomUUID();
      await storeApiKey(accountId, apiKeyResponse.apiKey);

      // Add account to store
      addAccount({
        id: accountId,
        displayName: profile.displayName || profile.email,
        serverUrl: normalizedUrl,
        userEmail: profile.email,
        apiKeyId: apiKeyResponse.id,
        scopes: apiKeyResponse.scopes,
        createdAt: new Date().toISOString(),
        lastUsedAt: new Date().toISOString(),
      });

      // Navigate to main app
      router.replace("/(main)/(tabs)/inbox");
    } catch (err) {
      console.error("Add account error:", err);
      setError(
        err instanceof Error
          ? err.message
          : "Failed to connect to server. Please check the URL and try again."
      );
      setStep("url");
    }
  };

  if (step !== "url") {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <Loading
          message={
            step === "discovering"
              ? "Discovering server..."
              : step === "authenticating"
                ? "Authenticating..."
                : "Setting up your account..."
          }
        />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-background">
      <KeyboardAvoidingView
        behavior={Platform.OS === "ios" ? "padding" : "height"}
        className="flex-1"
      >
        {/* Header */}
        <View className="flex-row items-center gap-2 px-4 py-2">
          <Button
            variant="ghost"
            size="icon"
            onPress={() => router.back()}
          >
            <ArrowLeft size={24} color="#1e293b" />
          </Button>
          <Text className="text-xl font-semibold text-foreground">
            Add Account
          </Text>
        </View>

        {/* Content */}
        <View className="flex-1 px-6 pt-8">
          <View className="mb-8 items-center">
            <View className="mb-4 h-16 w-16 items-center justify-center rounded-full bg-secondary">
              <Server size={32} color="#1e293b" />
            </View>
            <Text className="text-center text-lg font-medium text-foreground">
              Connect to your Relate server
            </Text>
            <Text className="mt-1 text-center text-muted-foreground">
              Enter your server URL to get started
            </Text>
          </View>

          <Input
            label="Server URL"
            placeholder="mail.example.com"
            value={serverUrl}
            onChangeText={setServerUrl}
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="url"
            error={error ?? undefined}
            containerClassName="mb-4"
          />

          <Card className="mb-6 bg-secondary/50">
            <CardContent className="flex-row items-start gap-3 py-3">
              <ExternalLink size={18} color="#64748b" className="mt-0.5" />
              <Text className="flex-1 text-sm text-muted-foreground">
                You&apos;ll be redirected to your organization&apos;s login page to
                authenticate securely.
              </Text>
            </CardContent>
          </Card>

          <Button onPress={handleConnect} size="lg" className="w-full">
            Connect
          </Button>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
