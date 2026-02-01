import { useCallback, useState } from "react";
import {
  View,
  Text,
  FlatList,
  RefreshControl,
  TouchableOpacity,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { Send } from "lucide-react-native";
import { useSentEmails, useDeleteEmail } from "@/lib/api/hooks";
import { EmailListItemComponent } from "@/components/mail/email-list-item";
import { Loading } from "@/components/ui/loading";
import { EmptyState } from "@/components/ui/empty-state";
import type { EmailListItem } from "@/lib/api/types";

export default function SentScreen() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, refetch } = useSentEmails(undefined, page, 20);
  const deleteEmail = useDeleteEmail();

  const emails: EmailListItem[] = data?.items ?? [];

  const handleEmailPress = useCallback((email: EmailListItem) => {
    router.push(`/(main)/emails/${email.id}`);
  }, []);

  const handleDeleteEmail = useCallback(
    (emailId: string) => {
      deleteEmail.mutate(emailId);
    },
    [deleteEmail]
  );

  const handleRefresh = useCallback(() => {
    refetch();
  }, [refetch]);

  const renderHeader = () => (
    <View className="border-b border-border bg-background px-4 py-3">
      <View className="flex-row items-center gap-3">
        <Send size={24} color="#1e293b" />
        <View>
          <Text className="text-xl font-semibold text-foreground">Sent</Text>
          <Text className="text-xs text-muted-foreground">
            {data?.totalCount ?? 0} emails
          </Text>
        </View>
      </View>
    </View>
  );

  if (isLoading) {
    return (
      <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
        {renderHeader()}
        <Loading message="Loading sent emails..." />
      </SafeAreaView>
    );
  }

  if (isError) {
    return (
      <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
        {renderHeader()}
        <EmptyState
          title="Failed to load sent emails"
          description="Please check your connection and try again."
          action={
            <TouchableOpacity onPress={handleRefresh}>
              <Text className="text-primary">Tap to retry</Text>
            </TouchableOpacity>
          }
        />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
      {renderHeader()}

      {emails.length === 0 ? (
        <EmptyState
          title="No sent emails"
          description="Emails you send will appear here"
        />
      ) : (
        <FlatList
          data={emails}
          keyExtractor={(item) => item.id}
          renderItem={({ item }) => (
            <EmailListItemComponent
              email={item}
              onPress={() => handleEmailPress(item)}
              onDelete={() => handleDeleteEmail(item.id)}
              onToggleRead={() => {}}
            />
          )}
          refreshControl={
            <RefreshControl
              refreshing={false}
              onRefresh={handleRefresh}
              tintColor="#1e293b"
            />
          }
        />
      )}
    </SafeAreaView>
  );
}
