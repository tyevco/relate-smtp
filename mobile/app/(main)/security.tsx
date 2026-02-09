import { useState, useCallback } from "react";
import { View, Text, TouchableOpacity, ScrollView, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import {
  ArrowLeft,
  Shield,
  ShieldCheck,
  ShieldX,
  Trash2,
  RefreshCw,
} from "lucide-react-native";
import { useFocusEffect } from "expo-router";
import { useAccounts } from "@/lib/auth/account-store";
import {
  extractDomain,
  getPin,
  removePin,
  type CertificatePin,
  isPinExpired,
} from "@/lib/security/certificate-pinning";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

interface DomainPin {
  domain: string;
  pin: CertificatePin;
  accountNames: string[];
}

export default function SecurityScreen() {
  const accounts = useAccounts();
  const [domainPins, setDomainPins] = useState<DomainPin[]>([]);
  const [loading, setLoading] = useState(true);

  const loadPins = useCallback(async () => {
    setLoading(true);
    const seenDomains = new Map<string, string[]>();

    // Collect unique domains from all accounts
    for (const account of accounts) {
      const domain = extractDomain(account.serverUrl);
      const existing = seenDomains.get(domain) ?? [];
      existing.push(account.displayName);
      seenDomains.set(domain, existing);
    }

    // Load pins for each domain
    const pins: DomainPin[] = [];
    for (const [domain, accountNames] of seenDomains) {
      const pin = await getPin(domain);
      if (pin) {
        pins.push({ domain, pin, accountNames });
      }
    }

    setDomainPins(pins);
    setLoading(false);
  }, [accounts]);

  useFocusEffect(
    useCallback(() => {
      loadPins();
    }, [loadPins])
  );

  const handleRemovePin = (domain: string) => {
    Alert.alert(
      "Remove Certificate Pin",
      `Remove the stored certificate pin for ${domain}? The certificate will be re-pinned on the next connection.`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Remove",
          style: "destructive",
          onPress: async () => {
            await removePin(domain);
            await loadPins();
          },
        },
      ]
    );
  };

  return (
    <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
      <ScrollView className="flex-1">
        {/* Header */}
        <View className="flex-row items-center gap-2 border-b border-border bg-background px-4 py-3">
          <Button variant="ghost" size="icon" onPress={() => router.back()}>
            <ArrowLeft size={24} color="#1e293b" />
          </Button>
          <Shield size={24} color="#1e293b" />
          <Text className="text-xl font-semibold text-foreground">
            Security
          </Text>
        </View>

        <View className="p-4">
          {/* Info Card */}
          <Card className="mb-6 bg-secondary/50">
            <CardContent className="py-3">
              <Text className="text-sm text-muted-foreground">
                Certificate pinning protects against man-in-the-middle attacks
                by verifying that the server&apos;s TLS certificate matches a
                previously trusted certificate. Pins are established
                automatically on first connection.
              </Text>
            </CardContent>
          </Card>

          {/* Pinned Certificates */}
          <Text className="mb-2 px-1 text-sm font-medium text-muted-foreground">
            Pinned Certificates
          </Text>

          {loading ? (
            <View className="items-center py-8">
              <RefreshCw size={24} color="#94a3b8" />
              <Text className="mt-2 text-sm text-muted-foreground">
                Loading...
              </Text>
            </View>
          ) : domainPins.length === 0 ? (
            <Card className="mb-4">
              <CardContent className="items-center py-6">
                <Shield size={32} color="#94a3b8" />
                <Text className="mt-2 text-center text-muted-foreground">
                  No certificate pins stored yet. Pins are created
                  automatically when the server provides its certificate
                  fingerprint.
                </Text>
              </CardContent>
            </Card>
          ) : (
            <View className="mb-4 rounded-lg border border-border bg-card">
              {domainPins.map((item, index) => (
                <View key={item.domain}>
                  {index > 0 && <View className="mx-4 h-px bg-border" />}
                  <PinItem
                    domainPin={item}
                    onRemove={() => handleRemovePin(item.domain)}
                  />
                </View>
              ))}
            </View>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

function PinItem({
  domainPin,
  onRemove,
}: {
  domainPin: DomainPin;
  onRemove: () => void;
}) {
  const { domain, pin } = domainPin;
  const expired = isPinExpired(pin);
  const pinDate = new Date(pin.createdAt).toLocaleDateString();
  const truncatedFingerprint = pin.sha256Fingerprints[0]
    ? `${pin.sha256Fingerprints[0].slice(0, 16)}...`
    : "Unknown";

  return (
    <View className="flex-row items-center gap-3 px-4 py-3">
      {expired ? (
        <ShieldX size={20} color="#dc2626" />
      ) : (
        <ShieldCheck size={20} color="#16a34a" />
      )}
      <View className="flex-1">
        <Text className="font-medium text-foreground">{domain}</Text>
        <Text className="text-xs text-muted-foreground">
          SHA-256: {truncatedFingerprint}
        </Text>
        <Text className="text-xs text-muted-foreground">
          {pin.trustOnFirstUse ? "Pinned automatically" : "Pinned manually"} on{" "}
          {pinDate}
          {expired ? " (expired)" : ""}
        </Text>
      </View>
      <TouchableOpacity onPress={onRemove} hitSlop={8}>
        <Trash2 size={18} color="#dc2626" />
      </TouchableOpacity>
    </View>
  );
}
