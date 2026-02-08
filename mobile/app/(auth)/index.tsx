import { View, Text } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Link } from "expo-router";
import { Mail } from "lucide-react-native";
import { Button } from "@/components/ui/button";

export default function WelcomeScreen() {
  return (
    <SafeAreaView className="flex-1 bg-background">
      <View className="flex-1 items-center justify-center px-6">
        {/* Logo/Icon */}
        <View className="mb-8 h-24 w-24 items-center justify-center rounded-3xl bg-primary">
          <Mail size={48} color="white" />
        </View>

        {/* Title */}
        <Text className="mb-2 text-center text-3xl font-bold text-foreground">
          Welcome to Relate Mail
        </Text>

        {/* Subtitle */}
        <Text className="mb-12 text-center text-lg text-muted-foreground">
          Secure, self-hosted email for your organization
        </Text>

        {/* Features */}
        <View className="mb-12 gap-4">
          <FeatureItem
            title="Multi-Account Support"
            description="Connect to multiple Relate servers"
          />
          <FeatureItem
            title="Secure Authentication"
            description="OIDC login with API key storage"
          />
          <FeatureItem
            title="Real-time Updates"
            description="Get notified when new emails arrive"
          />
        </View>
      </View>

      {/* Bottom Action */}
      <View className="px-6 pb-6">
        <Link href="/(auth)/add-account" asChild>
          <Button size="lg" className="w-full">
            Add Account
          </Button>
        </Link>
      </View>
    </SafeAreaView>
  );
}

function FeatureItem({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <View className="flex-row items-start gap-3">
      <View className="mt-1 h-2 w-2 rounded-full bg-primary" />
      <View className="flex-1">
        <Text className="font-medium text-foreground">{title}</Text>
        <Text className="text-sm text-muted-foreground">{description}</Text>
      </View>
    </View>
  );
}
