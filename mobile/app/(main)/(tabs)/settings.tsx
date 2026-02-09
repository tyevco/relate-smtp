import { View, Text, TouchableOpacity, ScrollView, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import {
  User,
  Key,
  Settings as SettingsIcon,
  Users,
  ChevronRight,
  LogOut,
  Info,
  AlertTriangle,
} from "lucide-react-native";
import { useProfile } from "@/lib/api/hooks";
import { useActiveAccount, useAccountStore } from "@/lib/auth/account-store";
import { Avatar } from "@/components/ui/avatar";
import { Card, CardContent } from "@/components/ui/card";

const KEY_AGE_WARNING_DAYS = 90;

function isKeyOld(createdAt: string): boolean {
  const created = new Date(createdAt);
  const now = new Date();
  const ageDays = Math.floor(
    (now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24)
  );
  return ageDays >= KEY_AGE_WARNING_DAYS;
}

export default function SettingsScreen() {
  const account = useActiveAccount();
  const { data: profile } = useProfile();
  const removeAccount = useAccountStore((state) => state.removeAccount);
  const activeKeyIsOld = account ? isKeyOld(account.createdAt) : false;

  const handleLogout = () => {
    Alert.alert(
      "Sign Out",
      "Are you sure you want to sign out of this account?",
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Sign Out",
          style: "destructive",
          onPress: async () => {
            if (account) {
              await removeAccount(account.id);
            }
          },
        },
      ]
    );
  };

  return (
    <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
      <ScrollView className="flex-1">
        {/* Header */}
        <View className="border-b border-border bg-background px-4 py-3">
          <View className="flex-row items-center gap-3">
            <SettingsIcon size={24} color="#1e293b" />
            <Text className="text-xl font-semibold text-foreground">
              Settings
            </Text>
          </View>
        </View>

        {/* Profile Section */}
        <View className="p-4">
          <Card className="mb-6">
            <CardContent className="flex-row items-center gap-4 py-4">
              <Avatar
                name={profile?.displayName ?? account?.displayName ?? null}
                email={profile?.email ?? account?.userEmail ?? ""}
                size="lg"
              />
              <View className="flex-1">
                <Text className="text-lg font-semibold text-foreground">
                  {profile?.displayName || account?.displayName || "User"}
                </Text>
                <Text className="text-sm text-muted-foreground">
                  {profile?.email || account?.userEmail}
                </Text>
                <Text className="text-xs text-muted-foreground">
                  {account?.serverUrl}
                </Text>
              </View>
            </CardContent>
          </Card>

          {/* Key age warning */}
          {activeKeyIsOld && (
            <TouchableOpacity
              onPress={() => router.push("/(main)/api-keys")}
              activeOpacity={0.7}
            >
              <Card className="mb-6 border-amber-300 bg-amber-50">
                <CardContent className="flex-row items-center gap-3 py-3">
                  <AlertTriangle size={20} color="#d97706" />
                  <View className="flex-1">
                    <Text className="font-medium text-amber-800">
                      API key rotation recommended
                    </Text>
                    <Text className="text-sm text-amber-700">
                      Your active API key is over 90 days old. Tap to rotate it.
                    </Text>
                  </View>
                  <ChevronRight size={20} color="#d97706" />
                </CardContent>
              </Card>
            </TouchableOpacity>
          )}

          {/* Menu Items */}
          <Text className="mb-2 px-1 text-sm font-medium text-muted-foreground">
            Account
          </Text>
          <View className="mb-6 rounded-lg border border-border bg-card">
            <MenuItem
              icon={<Users size={20} color="#64748b" />}
              title="Accounts"
              subtitle="Manage connected accounts"
              onPress={() => router.push("/(main)/accounts")}
            />
            <View className="mx-4 h-px bg-border" />
            <MenuItem
              icon={<Key size={20} color="#64748b" />}
              title="API Keys"
              subtitle="Manage your API keys"
              onPress={() => router.push("/(main)/api-keys")}
            />
            <View className="mx-4 h-px bg-border" />
            <MenuItem
              icon={<User size={20} color="#64748b" />}
              title="Preferences"
              subtitle="Theme, notifications, display"
              onPress={() => router.push("/(main)/preferences")}
            />
          </View>

          <Text className="mb-2 px-1 text-sm font-medium text-muted-foreground">
            About
          </Text>
          <View className="mb-6 rounded-lg border border-border bg-card">
            <MenuItem
              icon={<Info size={20} color="#64748b" />}
              title="About Relate Mail"
              subtitle="Version 1.0.0"
              onPress={() => {}}
              showChevron={false}
            />
          </View>

          {/* Sign Out */}
          <TouchableOpacity
            onPress={handleLogout}
            className="flex-row items-center justify-center gap-2 rounded-lg border border-destructive bg-destructive/10 py-4"
          >
            <LogOut size={20} color="#dc2626" />
            <Text className="font-medium text-destructive">Sign Out</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

interface MenuItemProps {
  icon: React.ReactNode;
  title: string;
  subtitle: string;
  onPress: () => void;
  showChevron?: boolean;
}

function MenuItem({
  icon,
  title,
  subtitle,
  onPress,
  showChevron = true,
}: MenuItemProps) {
  return (
    <TouchableOpacity
      onPress={onPress}
      className="flex-row items-center gap-3 px-4 py-3"
      activeOpacity={0.7}
    >
      {icon}
      <View className="flex-1">
        <Text className="font-medium text-foreground">{title}</Text>
        <Text className="text-sm text-muted-foreground">{subtitle}</Text>
      </View>
      {showChevron && <ChevronRight size={20} color="#94a3b8" />}
    </TouchableOpacity>
  );
}
