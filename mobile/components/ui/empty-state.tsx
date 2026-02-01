import { View, Text } from "react-native";
import { cn } from "@/lib/utils";

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

export function EmptyState({
  icon,
  title,
  description,
  action,
  className,
}: EmptyStateProps) {
  return (
    <View
      className={cn(
        "flex-1 items-center justify-center px-6 py-12",
        className
      )}
    >
      {icon && <View className="mb-4">{icon}</View>}
      <Text className="text-center text-lg font-semibold text-foreground">
        {title}
      </Text>
      {description && (
        <Text className="mt-2 text-center text-muted-foreground">
          {description}
        </Text>
      )}
      {action && <View className="mt-6">{action}</View>}
    </View>
  );
}
