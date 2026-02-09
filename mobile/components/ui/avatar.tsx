import { View, Text } from "react-native";
import { cn, getInitials, stringToColor } from "@/lib/utils";

interface AvatarProps {
  name: string | null;
  email: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

const sizeClasses = {
  sm: "h-8 w-8",
  md: "h-10 w-10",
  lg: "h-12 w-12",
};

const textSizeClasses = {
  sm: "text-xs",
  md: "text-sm",
  lg: "text-base",
};

export function Avatar({
  name,
  email,
  size = "md",
  className,
}: AvatarProps) {
  const initials = getInitials(name, email);
  const backgroundColor = stringToColor(email);

  return (
    <View
      className={cn(
        "items-center justify-center rounded-full",
        sizeClasses[size],
        className
      )}
      style={{ backgroundColor }}
    >
      <Text className={cn("font-semibold text-white", textSizeClasses[size])}>
        {initials}
      </Text>
    </View>
  );
}
