import { useCallback, useState } from "react";
import {
  View,
  Text,
  FlatList,
  RefreshControl,
  TouchableOpacity,
  TextInput,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router } from "expo-router";
import { Search, X, ChevronDown } from "lucide-react-native";
import { useInfiniteEmails, useMarkEmailRead, useDeleteEmail } from "@/lib/api/hooks";
import { useActiveAccount } from "@/lib/auth/account-store";
import { EmailListItemComponent } from "@/components/mail/email-list-item";
import { Loading } from "@/components/ui/loading";
import { EmptyState } from "@/components/ui/empty-state";
import { Avatar } from "@/components/ui/avatar";
import type { EmailListItem } from "@/lib/api/types";

export default function InboxScreen() {
  const account = useActiveAccount();
  const [isSearching, setIsSearching] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  const {
    data,
    isLoading,
    isError,
    refetch,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteEmails(20);

  const markEmailRead = useMarkEmailRead();
  const deleteEmail = useDeleteEmail();

  const emails: EmailListItem[] =
    data?.pages.flatMap((page) => page.items) ?? [];

  const totalCount = data?.pages[0]?.totalCount ?? 0;
  const unreadCount = data?.pages[0]?.unreadCount ?? 0;

  const handleEmailPress = useCallback(
    (email: EmailListItem) => {
      // Mark as read if unread
      if (!email.isRead) {
        markEmailRead.mutate({ id: email.id, isRead: true });
      }
      // Navigate to email detail
      router.push(`/(main)/emails/${email.id}`);
    },
    [markEmailRead]
  );

  const handleDeleteEmail = useCallback(
    (emailId: string) => {
      deleteEmail.mutate(emailId);
    },
    [deleteEmail]
  );

  const handleToggleRead = useCallback(
    (email: EmailListItem) => {
      markEmailRead.mutate({ id: email.id, isRead: !email.isRead });
    },
    [markEmailRead]
  );

  const handleRefresh = useCallback(() => {
    refetch();
  }, [refetch]);

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  const filteredEmails = searchQuery
    ? emails.filter(
        (e) =>
          e.subject.toLowerCase().includes(searchQuery.toLowerCase()) ||
          e.fromAddress.toLowerCase().includes(searchQuery.toLowerCase()) ||
          e.fromDisplayName?.toLowerCase().includes(searchQuery.toLowerCase())
      )
    : emails;

  const renderHeader = () => (
    <View className="border-b border-border bg-background px-4 py-3">
      {isSearching ? (
        <View className="flex-row items-center gap-2">
          <Search size={20} color="#64748b" />
          <TextInput
            className="flex-1 text-base text-foreground"
            placeholder="Search emails..."
            placeholderTextColor="#94a3b8"
            value={searchQuery}
            onChangeText={setSearchQuery}
            autoFocus
          />
          <TouchableOpacity
            onPress={() => {
              setIsSearching(false);
              setSearchQuery("");
            }}
          >
            <X size={20} color="#64748b" />
          </TouchableOpacity>
        </View>
      ) : (
        <View className="flex-row items-center justify-between">
          <TouchableOpacity
            onPress={() => router.push("/(main)/accounts")}
            className="flex-row items-center gap-2"
          >
            {account && (
              <Avatar
                name={account.displayName}
                email={account.userEmail}
                size="sm"
              />
            )}
            <View>
              <View className="flex-row items-center gap-1">
                <Text className="font-semibold text-foreground">
                  {account?.displayName || "Inbox"}
                </Text>
                <ChevronDown size={16} color="#64748b" />
              </View>
              <Text className="text-xs text-muted-foreground">
                {unreadCount > 0 ? `${unreadCount} unread` : `${totalCount} emails`}
              </Text>
            </View>
          </TouchableOpacity>

          <TouchableOpacity onPress={() => setIsSearching(true)}>
            <Search size={24} color="#1e293b" />
          </TouchableOpacity>
        </View>
      )}
    </View>
  );

  if (isLoading) {
    return (
      <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
        {renderHeader()}
        <Loading message="Loading emails..." />
      </SafeAreaView>
    );
  }

  if (isError) {
    return (
      <SafeAreaView className="flex-1 bg-background" edges={["top"]}>
        {renderHeader()}
        <EmptyState
          title="Failed to load emails"
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

      {filteredEmails.length === 0 ? (
        <EmptyState
          title={searchQuery ? "No matching emails" : "Your inbox is empty"}
          description={
            searchQuery
              ? "Try a different search term"
              : "New emails will appear here"
          }
        />
      ) : (
        <FlatList
          data={filteredEmails}
          keyExtractor={(item) => item.id}
          renderItem={({ item }) => (
            <EmailListItemComponent
              email={item}
              onPress={() => handleEmailPress(item)}
              onDelete={() => handleDeleteEmail(item.id)}
              onToggleRead={() => handleToggleRead(item)}
            />
          )}
          refreshControl={
            <RefreshControl
              refreshing={false}
              onRefresh={handleRefresh}
              tintColor="#1e293b"
            />
          }
          onEndReached={handleLoadMore}
          onEndReachedThreshold={0.5}
          ListFooterComponent={
            isFetchingNextPage ? (
              <View className="py-4">
                <Loading />
              </View>
            ) : null
          }
        />
      )}
    </SafeAreaView>
  );
}
