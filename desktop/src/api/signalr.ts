import * as signalR from '@microsoft/signalr'

let connection: signalR.HubConnection | null = null

/**
 * Create and start a SignalR connection to the email hub.
 * No-ops if already connected.
 */
export async function connect(serverUrl: string, apiKey: string): Promise<void> {
  if (connection?.state === signalR.HubConnectionState.Connected) {
    return
  }

  await disconnect()

  const hubUrl = `${serverUrl}/hubs/email`

  connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => apiKey,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0s, 2s, 4s, 8s, max 30s
        return Math.min(
          Math.pow(2, retryContext.previousRetryCount) * 1000,
          30000,
        )
      },
    })
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  await connection.start()
  console.log('SignalR connected to', hubUrl)
}

/**
 * Stop and discard the current connection.
 */
export async function disconnect(): Promise<void> {
  if (connection) {
    try {
      await connection.stop()
    } catch (error) {
      console.error('Error disconnecting SignalR:', error)
    }
    connection = null
  }
}

/**
 * Register a handler for new email events.
 * @throws Error if connection is not established
 */
export function onNewEmail(handler: (emailId: string) => void): () => void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.on('NewEmail', handler)
  return () => connection?.off('NewEmail', handler)
}

/**
 * Register a handler for email updated events.
 * @throws Error if connection is not established
 */
export function onEmailUpdated(handler: (emailId: string) => void): () => void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.on('EmailUpdated', handler)
  return () => connection?.off('EmailUpdated', handler)
}

/**
 * Register a handler for email deleted events.
 * @throws Error if connection is not established
 */
export function onEmailDeleted(handler: (emailId: string) => void): () => void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.on('EmailDeleted', handler)
  return () => connection?.off('EmailDeleted', handler)
}

/**
 * Register a handler for unread count changes.
 * @throws Error if connection is not established
 */
export function onUnreadCountChanged(handler: (count: number) => void): () => void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.on('UnreadCountChanged', handler)
  return () => connection?.off('UnreadCountChanged', handler)
}

/**
 * Register a handler for reconnecting events.
 * @throws Error if connection is not established
 */
export function onReconnecting(handler: (error?: Error) => void): void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.onreconnecting(handler)
}

/**
 * Register a handler for reconnected events.
 * @throws Error if connection is not established
 */
export function onReconnected(handler: (connectionId?: string) => void): void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.onreconnected(handler)
}

/**
 * Register a handler for connection close events.
 * @throws Error if connection is not established
 */
export function onClose(handler: (error?: Error) => void): void {
  if (!connection) {
    throw new Error('SignalR connection not established. Call connect() first.')
  }
  connection.onclose(handler)
}

export function getConnection(): signalR.HubConnection | null {
  return connection
}
