import { View, Text, type ViewProps } from "react-native";
import { cn } from "@/lib/utils";

interface CardProps extends ViewProps {
  children: React.ReactNode;
}

export function Card({ className, children, ...props }: CardProps) {
  return (
    <View
      className={cn(
        "rounded-lg border border-border bg-card p-4",
        className
      )}
      {...props}
    >
      {children}
    </View>
  );
}

interface CardHeaderProps extends ViewProps {
  children: React.ReactNode;
}

export function CardHeader({ className, children, ...props }: CardHeaderProps) {
  return (
    <View className={cn("pb-2", className)} {...props}>
      {children}
    </View>
  );
}

interface CardTitleProps {
  children: React.ReactNode;
  className?: string;
}

export function CardTitle({ className, children }: CardTitleProps) {
  return (
    <Text className={cn("text-lg font-semibold text-card-foreground", className)}>
      {children}
    </Text>
  );
}

interface CardDescriptionProps {
  children: React.ReactNode;
  className?: string;
}

export function CardDescription({ className, children }: CardDescriptionProps) {
  return (
    <Text className={cn("text-sm text-muted-foreground", className)}>
      {children}
    </Text>
  );
}

interface CardContentProps extends ViewProps {
  children: React.ReactNode;
}

export function CardContent({
  className,
  children,
  ...props
}: CardContentProps) {
  return (
    <View className={cn("", className)} {...props}>
      {children}
    </View>
  );
}

interface CardFooterProps extends ViewProps {
  children: React.ReactNode;
}

export function CardFooter({ className, children, ...props }: CardFooterProps) {
  return (
    <View className={cn("flex-row items-center pt-4", className)} {...props}>
      {children}
    </View>
  );
}
