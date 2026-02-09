import { View, Text, ScrollView, TouchableOpacity, useWindowDimensions } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { router, useLocalSearchParams } from "expo-router";
import { WebView } from "react-native-webview";
import DOMPurify from "dompurify";
import {
  X,
  Trash2,
  Mail,
  MailOpen,
  Paperclip,
  User,
  Users,
} from "lucide-react-native";
import { useEmail, useMarkEmailRead, useDeleteEmail } from "@/lib/api/hooks";
import { Loading } from "@/components/ui/loading";
import { Avatar } from "@/components/ui/avatar";
import { formatDate, formatBytes } from "@/lib/utils";

function sanitizeHtml(html: string | null | undefined): string {
  if (!html) return "";
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: [
      "p", "br", "b", "i", "u", "strong", "em", "a", "ul", "ol", "li", "img",
      "table", "tr", "td", "th", "thead", "tbody", "tfoot", "div", "span",
      "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "code",
      "hr", "sub", "sup", "small", "mark", "del", "ins", "address",
    ],
    ALLOWED_ATTR: ["href", "src", "alt", "class", "style", "target", "rel", "width", "height"],
    ALLOW_DATA_ATTR: false,
    FORBID_TAGS: ["script", "style", "iframe", "object", "embed", "form", "input"],
  });
}

export default function EmailDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { width } = useWindowDimensions();

  const { data: email, isLoading, isError } = useEmail(id ?? "");
  const markEmailRead = useMarkEmailRead();
  const deleteEmail = useDeleteEmail();

  const handleClose = () => {
    router.back();
  };

  const handleDelete = () => {
    if (email) {
      deleteEmail.mutate(email.id);
      router.back();
    }
  };

  const handleToggleRead = () => {
    if (email) {
      markEmailRead.mutate({ id: email.id, isRead: !email.isRead });
    }
  };

  if (isLoading) {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <Loading message="Loading email..." />
      </SafeAreaView>
    );
  }

  if (isError || !email) {
    return (
      <SafeAreaView className="flex-1 bg-background">
        <View className="flex-1 items-center justify-center">
          <Text className="text-muted-foreground">Failed to load email</Text>
          <TouchableOpacity onPress={handleClose} className="mt-4">
            <Text className="text-primary">Go back</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const htmlContent = email.htmlBody
    ? `
      <!DOCTYPE html>
      <html>
        <head>
          <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0">
          <style>
            body {
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
              font-size: 16px;
              line-height: 1.5;
              color: #1e293b;
              padding: 0;
              margin: 0;
              word-wrap: break-word;
              overflow-wrap: break-word;
            }
            img { max-width: 100%; height: auto; }
            a { color: #3b82f6; }
            pre, code {
              overflow-x: auto;
              max-width: 100%;
              white-space: pre-wrap;
            }
            table { max-width: 100%; }
          </style>
        </head>
        <body>${sanitizeHtml(email.htmlBody)}</body>
      </html>
    `
    : null;

  const toRecipients = email.recipients.filter((r) => r.type === "To");
  const ccRecipients = email.recipients.filter((r) => r.type === "Cc");

  return (
    <SafeAreaView className="flex-1 bg-background">
      {/* Header */}
      <View className="flex-row items-center justify-between border-b border-border px-4 py-3">
        <TouchableOpacity onPress={handleClose}>
          <X size={24} color="#1e293b" />
        </TouchableOpacity>

        <View className="flex-row items-center gap-2">
          <TouchableOpacity onPress={handleToggleRead} className="p-2">
            {email.isRead ? (
              <Mail size={22} color="#64748b" />
            ) : (
              <MailOpen size={22} color="#64748b" />
            )}
          </TouchableOpacity>
          <TouchableOpacity onPress={handleDelete} className="p-2">
            <Trash2 size={22} color="#dc2626" />
          </TouchableOpacity>
        </View>
      </View>

      <ScrollView className="flex-1">
        {/* Subject */}
        <View className="border-b border-border px-4 py-4">
          <Text className="text-xl font-semibold text-foreground">
            {email.subject || "(No subject)"}
          </Text>
        </View>

        {/* From */}
        <View className="flex-row items-start gap-3 border-b border-border px-4 py-4">
          <Avatar
            name={email.fromDisplayName}
            email={email.fromAddress}
            size="md"
          />
          <View className="flex-1">
            <Text className="font-medium text-foreground">
              {email.fromDisplayName || email.fromAddress}
            </Text>
            {email.fromDisplayName && (
              <Text className="text-sm text-muted-foreground">
                {email.fromAddress}
              </Text>
            )}
          </View>
          <View className="items-end">
            <Text className="text-sm text-muted-foreground">
              {formatDate(email.receivedAt)}
            </Text>
            <Text className="text-xs text-muted-foreground">
              {formatBytes(email.sizeBytes)}
            </Text>
          </View>
        </View>

        {/* Recipients */}
        {(toRecipients.length > 0 || ccRecipients.length > 0) && (
          <View className="border-b border-border px-4 py-3">
            {toRecipients.length > 0 && (
              <View className="mb-2 flex-row items-start gap-2">
                <View className="flex-row items-center gap-1 pt-0.5">
                  <User size={14} color="#64748b" />
                  <Text className="text-xs text-muted-foreground">To:</Text>
                </View>
                <View className="flex-1 flex-row flex-wrap">
                  {toRecipients.map((r, i) => (
                    <Text key={r.id} className="text-sm text-foreground">
                      {r.displayName || r.address}
                      {i < toRecipients.length - 1 ? ", " : ""}
                    </Text>
                  ))}
                </View>
              </View>
            )}
            {ccRecipients.length > 0 && (
              <View className="flex-row items-start gap-2">
                <View className="flex-row items-center gap-1 pt-0.5">
                  <Users size={14} color="#64748b" />
                  <Text className="text-xs text-muted-foreground">Cc:</Text>
                </View>
                <View className="flex-1 flex-row flex-wrap">
                  {ccRecipients.map((r, i) => (
                    <Text key={r.id} className="text-sm text-foreground">
                      {r.displayName || r.address}
                      {i < ccRecipients.length - 1 ? ", " : ""}
                    </Text>
                  ))}
                </View>
              </View>
            )}
          </View>
        )}

        {/* Attachments */}
        {email.attachments.length > 0 && (
          <View className="border-b border-border px-4 py-3">
            <View className="mb-2 flex-row items-center gap-2">
              <Paperclip size={16} color="#64748b" />
              <Text className="text-sm font-medium text-muted-foreground">
                {email.attachments.length} attachment
                {email.attachments.length !== 1 ? "s" : ""}
              </Text>
            </View>
            <View className="gap-2">
              {email.attachments.map((attachment) => (
                <View
                  key={attachment.id}
                  className="flex-row items-center justify-between rounded-lg bg-secondary/50 px-3 py-2"
                >
                  <Text
                    className="flex-1 text-sm text-foreground"
                    numberOfLines={1}
                  >
                    {attachment.fileName}
                  </Text>
                  <Text className="text-xs text-muted-foreground">
                    {formatBytes(attachment.sizeBytes)}
                  </Text>
                </View>
              ))}
            </View>
          </View>
        )}

        {/* Body */}
        <View className="min-h-[300px] px-4 py-4">
          {htmlContent ? (
            <WebView
              originWhitelist={["https://*", "http://*"]}
              source={{ html: htmlContent }}
              style={{ flex: 1, minHeight: 300, width: width - 32 }}
              scrollEnabled={false}
              scalesPageToFit={false}
              showsVerticalScrollIndicator={false}
              onMessage={() => {}}
            />
          ) : email.textBody ? (
            <Text className="text-foreground">{email.textBody}</Text>
          ) : (
            <Text className="text-muted-foreground italic">
              No content available
            </Text>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
