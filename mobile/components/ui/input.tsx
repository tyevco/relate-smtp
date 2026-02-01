import { TextInput, View, Text, type TextInputProps } from "react-native";
import { cn } from "@/lib/utils";

interface InputProps extends TextInputProps {
  label?: string;
  error?: string;
  containerClassName?: string;
}

export function Input({
  label,
  error,
  className,
  containerClassName,
  ...props
}: InputProps) {
  return (
    <View className={cn("gap-1.5", containerClassName)}>
      {label && (
        <Text className="text-sm font-medium text-foreground">{label}</Text>
      )}
      <TextInput
        className={cn(
          "h-12 rounded-lg border border-input bg-background px-3 text-base text-foreground",
          "placeholder:text-muted-foreground",
          error && "border-destructive",
          className
        )}
        placeholderTextColor="#94a3b8"
        {...props}
      />
      {error && <Text className="text-sm text-destructive">{error}</Text>}
    </View>
  );
}
