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

export function onNewEmail(handler: (emailId: string) => void): void {
  connection?.on('NewEmail', handler)
}

export function onEmailUpdated(handler: (emailId: string) => void): void {
  connection?.on('EmailUpdated', handler)
}

export function onEmailDeleted(handler: (emailId: string) => void): void {
  connection?.on('EmailDeleted', handler)
}

export function onUnreadCountChanged(handler: (count: number) => void): void {
  connection?.on('UnreadCountChanged', handler)
}

export function onReconnecting(handler: (error?: Error) => void): void {
  connection?.onreconnecting(handler)
}

export function onReconnected(handler: (connectionId?: string) => void): void {
  connection?.onreconnected(handler)
}

export function onClose(handler: (error?: Error) => void): void {
  connection?.onclose(handler)
}

export function getConnection(): signalR.HubConnection | null {
  return connection
}
