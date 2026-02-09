import * as signalR from "@microsoft/signalr";
import { useAccountStore } from "../auth/account-store";
import { getApiKey } from "../auth/secure-storage";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef } from "react";

let connection: signalR.HubConnection | null = null;

/**
 * Create a SignalR connection to the email hub
 */
async function createConnection(
  serverUrl: string,
  apiKey: string
): Promise<signalR.HubConnection> {
  const hubUrl = `${serverUrl}/hubs/email`;

  const newConnection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => apiKey,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0s, 2s, 4s, 8s, max 30s
        const delay = Math.min(
          Math.pow(2, retryContext.previousRetryCount) * 1000,
          30000
        );
        return delay;
      },
    })
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  return newConnection;
}

/**
 * Connect to SignalR hub for the active account
 */
export async function connectSignalR(): Promise<void> {
  // Disconnect any existing connection
  await disconnectSignalR();

  const state = useAccountStore.getState();
  const activeAccount = state.accounts.find(
    (a) => a.id === state.activeAccountId
  );

  if (!activeAccount) {
    console.log("No active account, skipping SignalR connection");
    return;
  }

  const apiKey = await getApiKey(activeAccount.id);
  if (!apiKey) {
    console.error("No API key found for active account");
    return;
  }

  try {
    connection = await createConnection(activeAccount.serverUrl, apiKey);
    await connection.start();
    console.log("SignalR connected to", activeAccount.serverUrl);
  } catch (error) {
    console.error("SignalR connection failed:", error);
    connection = null;
  }
}

/**
 * Disconnect from SignalR hub
 */
export async function disconnectSignalR(): Promise<void> {
  if (connection) {
    try {
      await connection.stop();
    } catch (error) {
      console.error("Error disconnecting SignalR:", error);
    }
    connection = null;
  }
}

/**
 * Get the current SignalR connection
 */
export function getConnection(): signalR.HubConnection | null {
  return connection;
}

// Handler tracking for cleanup
interface TrackedHandler {
  event: string;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  handler: (...args: any[]) => void;
}

/**
 * Hook to manage SignalR connection and email updates
 */
export function useSignalREmails() {
  const queryClient = useQueryClient();
  const activeAccount = useAccountStore((state) =>
    state.accounts.find((a) => a.id === state.activeAccountId)
  );
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const handlersRef = useRef<TrackedHandler[]>([]);

  useEffect(() => {
    if (!activeAccount) {
      return;
    }

    const setupConnection = async () => {
      const apiKey = await getApiKey(activeAccount.id);
      if (!apiKey) {
        return;
      }

      try {
        const conn = await createConnection(activeAccount.serverUrl, apiKey);

        // Track handlers for cleanup to prevent memory leaks
        const handlers: TrackedHandler[] = [];

        // Handle new email event
        const newEmailHandler = (emailId: string) => {
          console.log("New email received:", emailId);
          // Invalidate email queries to refresh the list
          queryClient.invalidateQueries({
            queryKey: ["emails", activeAccount.id],
          });
        };
        conn.on("NewEmail", newEmailHandler);
        handlers.push({ event: "NewEmail", handler: newEmailHandler });

        // Handle email updated event
        const emailUpdatedHandler = (emailId: string) => {
          console.log("Email updated:", emailId);
          queryClient.invalidateQueries({
            queryKey: ["email", activeAccount.id, emailId],
          });
          queryClient.invalidateQueries({
            queryKey: ["emails", activeAccount.id],
          });
        };
        conn.on("EmailUpdated", emailUpdatedHandler);
        handlers.push({ event: "EmailUpdated", handler: emailUpdatedHandler });

        // Handle email deleted event
        const emailDeletedHandler = (emailId: string) => {
          console.log("Email deleted:", emailId);
          queryClient.invalidateQueries({
            queryKey: ["emails", activeAccount.id],
          });
        };
        conn.on("EmailDeleted", emailDeletedHandler);
        handlers.push({ event: "EmailDeleted", handler: emailDeletedHandler });

        handlersRef.current = handlers;

        // Handle connection state changes
        conn.onreconnecting((error) => {
          console.log("SignalR reconnecting:", error);
        });

        conn.onreconnected((connectionId) => {
          console.log("SignalR reconnected:", connectionId);
        });

        conn.onclose((error) => {
          console.log("SignalR connection closed:", error);
        });

        await conn.start();
        connectionRef.current = conn;
        console.log("SignalR connected");
      } catch (error) {
        console.error("Failed to connect SignalR:", error);
      }
    };

    setupConnection();

    return () => {
      // Remove all tracked handlers to prevent memory leaks
      const conn = connectionRef.current;
      if (conn) {
        handlersRef.current.forEach(({ event, handler }) => {
          conn.off(event, handler);
        });
        handlersRef.current = [];
        conn.stop();
        connectionRef.current = null;
      }
    };
  }, [activeAccount?.id, activeAccount?.serverUrl, queryClient]);

  return {
    isConnected: connectionRef.current?.state === signalR.HubConnectionState.Connected,
  };
}
