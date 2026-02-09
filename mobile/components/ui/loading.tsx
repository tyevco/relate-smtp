import { View, ActivityIndicator, Text } from "react-native";
import { cn } from "@/lib/utils";

interface LoadingProps {
  message?: string;
  className?: string;
}

export function Loading({ message, className }: LoadingProps) {
  return (
    <View
      className={cn("flex-1 items-center justify-center gap-3", className)}
    >
      <ActivityIndicator size="large" color="#1e293b" />
      {message && (
        <Text className="text-muted-foreground">{message}</Text>
      )}
    </View>
  );
}

export function LoadingOverlay({ message }: LoadingProps) {
  return (
    <View className="absolute inset-0 items-center justify-center bg-background/80">
      <ActivityIndicator size="large" color="#1e293b" />
      {message && (
        <Text className="mt-3 text-muted-foreground">{message}</Text>
      )}
    </View>
  );
}
