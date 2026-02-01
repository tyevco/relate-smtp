import { View, Text, TouchableOpacity, Animated } from "react-native";
import { Swipeable } from "react-native-gesture-handler";
import { Trash2, Mail, MailOpen, Paperclip } from "lucide-react-native";
import { Avatar } from "@/components/ui/avatar";
import { cn, formatDate, truncate } from "@/lib/utils";
import type { EmailListItem } from "@/lib/api/types";

interface EmailListItemProps {
  email: EmailListItem;
  onPress: () => void;
  onDelete: () => void;
  onToggleRead: () => void;
}

export function EmailListItemComponent({
  email,
  onPress,
  onDelete,
  onToggleRead,
}: EmailListItemProps) {
  const renderRightActions = (
    progress: Animated.AnimatedInterpolation<number>,
    dragX: Animated.AnimatedInterpolation<number>
  ) => {
    const scale = dragX.interpolate({
      inputRange: [-100, 0],
      outputRange: [1, 0],
      extrapolate: "clamp",
    });

    return (
      <TouchableOpacity
        onPress={onDelete}
        className="w-20 items-center justify-center bg-destructive"
      >
        <Animated.View style={{ transform: [{ scale }] }}>
          <Trash2 size={24} color="white" />
        </Animated.View>
      </TouchableOpacity>
    );
  };

  const renderLeftActions = (
    progress: Animated.AnimatedInterpolation<number>,
    dragX: Animated.AnimatedInterpolation<number>
  ) => {
    const scale = dragX.interpolate({
      inputRange: [0, 100],
      outputRange: [0, 1],
      extrapolate: "clamp",
    });

    return (
      <TouchableOpacity
        onPress={onToggleRead}
        className="w-20 items-center justify-center bg-primary"
      >
        <Animated.View style={{ transform: [{ scale }] }}>
          {email.isRead ? (
            <Mail size={24} color="white" />
          ) : (
            <MailOpen size={24} color="white" />
          )}
        </Animated.View>
      </TouchableOpacity>
    );
  };

  return (
    <Swipeable
      renderRightActions={renderRightActions}
      renderLeftActions={renderLeftActions}
      overshootRight={false}
      overshootLeft={false}
    >
      <TouchableOpacity
        onPress={onPress}
        activeOpacity={0.7}
        className={cn(
          "flex-row items-start gap-3 border-b border-border bg-background px-4 py-3",
          !email.isRead && "bg-accent/30"
        )}
      >
        <Avatar
          name={email.fromDisplayName}
          email={email.fromAddress}
          size="md"
        />

        <View className="flex-1 gap-1">
          <View className="flex-row items-center justify-between">
            <Text
              className={cn(
                "flex-1 text-sm",
                !email.isRead ? "font-semibold text-foreground" : "text-foreground"
              )}
              numberOfLines={1}
            >
              {email.fromDisplayName || email.fromAddress}
            </Text>
            <View className="flex-row items-center gap-2">
              {email.attachmentCount > 0 && (
                <Paperclip size={14} color="#64748b" />
              )}
              <Text className="text-xs text-muted-foreground">
                {formatDate(email.receivedAt)}
              </Text>
            </View>
          </View>

          <Text
            className={cn(
              "text-sm",
              !email.isRead ? "font-medium text-foreground" : "text-foreground"
            )}
            numberOfLines={1}
          >
            {email.subject || "(No subject)"}
          </Text>
        </View>

        {!email.isRead && (
          <View className="mt-2 h-2 w-2 rounded-full bg-primary" />
        )}
      </TouchableOpacity>
    </Swipeable>
  );
}
