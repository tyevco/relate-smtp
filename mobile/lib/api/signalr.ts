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

/**
 * Hook to manage SignalR connection and email updates
 */
export function useSignalREmails() {
  const queryClient = useQueryClient();
  const activeAccount = useAccountStore((state) =>
    state.accounts.find((a) => a.id === state.activeAccountId)
  );
  const connectionRef = useRef<signalR.HubConnection | null>(null);

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

        // Handle new email event
        conn.on("NewEmail", (emailId: string) => {
          console.log("New email received:", emailId);
          // Invalidate email queries to refresh the list
          queryClient.invalidateQueries({
            queryKey: ["emails", activeAccount.id],
          });
        });

        // Handle email updated event
        conn.on("EmailUpdated", (emailId: string) => {
          console.log("Email updated:", emailId);
          queryClient.invalidateQueries({
            queryKey: ["email", activeAccount.id, emailId],
          });
          queryClient.invalidateQueries({
            queryKey: ["emails", activeAccount.id],
          });
        });

        // Handle email deleted event
        conn.on("EmailDeleted", (emailId: string) => {
          console.log("Email deleted:", emailId);
          queryClient.invalidateQueries({
            queryKey: ["emails", activeAccount.id],
          });
        });

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
      if (connectionRef.current) {
        connectionRef.current.stop();
        connectionRef.current = null;
      }
    };
  }, [activeAccount?.id, activeAccount?.serverUrl, queryClient]);

  return {
    isConnected: connectionRef.current?.state === signalR.HubConnectionState.Connected,
  };
}
