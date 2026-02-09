import { useEffect, useState } from "react";
import { View, Text, TouchableOpacity, ScrollView, Switch, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { ArrowLeft, Sun, Moon, Smartphone, Check, Fingerprint } from "lucide-react-native";
import { usePreferences, useUpdatePreferences } from "@/lib/api/hooks";
import { Loading } from "@/components/ui/loading";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { UserPreference } from "@/lib/api/types";
import {
  isBiometricAvailable,
  getBiometricType,
  authenticateWithBiometrics,
  useBiometricStore,
  useBiometricEnabled,
} from "@/lib/auth/biometric";

export default function PreferencesScreen() {
  const { data: preferences, isLoading, isError } = usePreferences();
  const updatePreferences = useUpdatePreferences();
  const biometricEnabled = useBiometricEnabled();
  const setBiometricEnabled = useBiometricStore((s) => s.setEnabled);
  const [biometricAvailable, setBiometricAvailable] = useState(false);
  const [biometricLabel, setBiometricLabel] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      const available = await isBiometricAvailable();
      setBiometricAvailable(available);
      if (available) {
        const type = await getBiometricType();
        setBiometricLabel(type);
      }
    })();
  }, []);

  const handleBiometricToggle = async () => {
    if (!biometricEnabled) {
      // Verify identity before enabling
      const success = await authenticateWithBiometrics(
        "Authenticate to enable biometric lock"
      );
      if (success) {
        setBiometricEnabled(true);
      } else {
        Alert.alert(
          "Authentication Failed",
          "Biometric authentication could not be verified. Please try again."
        );
      }
    } else {
      setBiometricEnabled(false);
    }
  };

  const handleThemeChange = (theme: "light" | "dark" | "system") => {
    updatePreferences.mutate({ theme });
  };

  const handleToggle = (
    key: keyof Pick<
      UserPreference,
      "showPreview" | "groupByDate" | "desktopNotifications"
    >
  ) => {
    if (preferences) {
      updatePreferences.mutate({ [key]: !preferences[key] });
    }
  };

  if (isLoading) {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
          <TouchableOpacity onPress={() => router.back()}>
            <ArrowLeft size={24} color="#1e293b" />
          </TouchableOpacity>
          <Text className="text-xl font-semibold text-foreground">
            Preferences
          </Text>
        </View>
        <Loading message="Loading preferences..." />
      </SafeAreaView>
    );
  }

  if (isError || !preferences) {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
          <TouchableOpacity onPress={() => router.back()}>
            <ArrowLeft size={24} color="#1e293b" />
          </TouchableOpacity>
          <Text className="text-xl font-semibold text-foreground">
            Preferences
          </Text>
        </View>
        <View className="flex-1 items-center justify-center">
          <Text className="text-muted-foreground">
            Failed to load preferences
          </Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-background">
      {/* Header */}
      <View className="flex-row items-center gap-2 border-b border-border px-4 py-3">
        <TouchableOpacity onPress={() => router.back()}>
          <ArrowLeft size={24} color="#1e293b" />
        </TouchableOpacity>
        <Text className="text-xl font-semibold text-foreground">
          Preferences
        </Text>
      </View>

      <ScrollView className="flex-1 p-4">
        {/* Theme */}
        <Card className="mb-4">
          <CardHeader>
            <CardTitle>Theme</CardTitle>
          </CardHeader>
          <CardContent className="gap-2">
            <ThemeOption
              icon={<Sun size={20} color="#64748b" />}
              label="Light"
              selected={preferences.theme === "light"}
              onPress={() => handleThemeChange("light")}
            />
            <ThemeOption
              icon={<Moon size={20} color="#64748b" />}
              label="Dark"
              selected={preferences.theme === "dark"}
              onPress={() => handleThemeChange("dark")}
            />
            <ThemeOption
              icon={<Smartphone size={20} color="#64748b" />}
              label="System"
              selected={preferences.theme === "system"}
              onPress={() => handleThemeChange("system")}
            />
          </CardContent>
        </Card>

        {/* Display */}
        <Card className="mb-4">
          <CardHeader>
            <CardTitle>Display</CardTitle>
          </CardHeader>
          <CardContent className="gap-4">
            <ToggleOption
              label="Show Preview"
              description="Show email preview in list"
              value={preferences.showPreview}
              onToggle={() => handleToggle("showPreview")}
            />
            <ToggleOption
              label="Group by Date"
              description="Group emails by date"
              value={preferences.groupByDate}
              onToggle={() => handleToggle("groupByDate")}
            />
          </CardContent>
        </Card>

        {/* Display Density */}
        <Card className="mb-4">
          <CardHeader>
            <CardTitle>Display Density</CardTitle>
          </CardHeader>
          <CardContent className="gap-2">
            {(["compact", "comfortable", "spacious"] as const).map((density) => (
              <TouchableOpacity
                key={density}
                onPress={() => updatePreferences.mutate({ displayDensity: density })}
                className={`flex-row items-center justify-between rounded-lg px-3 py-2 ${
                  preferences.displayDensity === density
                    ? "bg-primary/10"
                    : ""
                }`}
              >
                <Text
                  className={`capitalize ${
                    preferences.displayDensity === density
                      ? "font-medium text-primary"
                      : "text-foreground"
                  }`}
                >
                  {density}
                </Text>
                {preferences.displayDensity === density && (
                  <Check size={18} color="#1e293b" />
                )}
              </TouchableOpacity>
            ))}
          </CardContent>
        </Card>

        {/* Security */}
        {biometricAvailable && (
          <Card className="mb-4">
            <CardHeader>
              <CardTitle>Security</CardTitle>
            </CardHeader>
            <CardContent>
              <View className="flex-row items-center justify-between">
                <View className="flex-row flex-1 items-center gap-3">
                  <Fingerprint size={20} color="#64748b" />
                  <View className="flex-1">
                    <Text className="font-medium text-foreground">
                      {biometricLabel ?? "Biometric"} Lock
                    </Text>
                    <Text className="text-sm text-muted-foreground">
                      Require {biometricLabel?.toLowerCase() ?? "biometric"} to
                      open the app
                    </Text>
                  </View>
                </View>
                <Switch
                  value={biometricEnabled}
                  onValueChange={handleBiometricToggle}
                  trackColor={{ false: "#e2e8f0", true: "#1e293b" }}
                  thumbColor="#ffffff"
                />
              </View>
            </CardContent>
          </Card>
        )}

        {/* Notifications */}
        <Card>
          <CardHeader>
            <CardTitle>Notifications</CardTitle>
          </CardHeader>
          <CardContent>
            <ToggleOption
              label="Push Notifications"
              description="Get notified when new emails arrive"
              value={preferences.desktopNotifications}
              onToggle={() => handleToggle("desktopNotifications")}
            />
          </CardContent>
        </Card>
      </ScrollView>
    </SafeAreaView>
  );
}

interface ThemeOptionProps {
  icon: React.ReactNode;
  label: string;
  selected: boolean;
  onPress: () => void;
}

function ThemeOption({ icon, label, selected, onPress }: ThemeOptionProps) {
  return (
    <TouchableOpacity
      onPress={onPress}
      className={`flex-row items-center justify-between rounded-lg px-3 py-2 ${
        selected ? "bg-primary/10" : ""
      }`}
    >
      <View className="flex-row items-center gap-3">
        {icon}
        <Text
          className={
            selected ? "font-medium text-primary" : "text-foreground"
          }
        >
          {label}
        </Text>
      </View>
      {selected && <Check size={18} color="#1e293b" />}
    </TouchableOpacity>
  );
}

interface ToggleOptionProps {
  label: string;
  description: string;
  value: boolean;
  onToggle: () => void;
}

function ToggleOption({
  label,
  description,
  value,
  onToggle,
}: ToggleOptionProps) {
  return (
    <View className="flex-row items-center justify-between">
      <View className="flex-1">
        <Text className="font-medium text-foreground">{label}</Text>
        <Text className="text-sm text-muted-foreground">{description}</Text>
      </View>
      <Switch
        value={value}
        onValueChange={onToggle}
        trackColor={{ false: "#e2e8f0", true: "#1e293b" }}
        thumbColor="#ffffff"
      />
    </View>
  );
}
