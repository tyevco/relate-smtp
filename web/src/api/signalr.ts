import * as signalR from '@microsoft/signalr';

export type NewEmailHandler = (email: {
  id: string;
  from: string;
  fromDisplay?: string;
  subject: string;
  receivedAt: string;
  hasAttachments: boolean;
}) => void;

export type EmailUpdatedHandler = (update: {
  id: string;
  isRead: boolean;
}) => void;

export type EmailDeletedHandler = (emailId: string) => void;

export type UnreadCountChangedHandler = (count: number) => void;

class SignalRConnection {
  private connection: signalR.HubConnection | null = null;
  private connectionPromise: Promise<signalR.HubConnection> | null = null;

  async connect(apiUrl: string): Promise<signalR.HubConnection> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    // If already connecting, wait for the existing promise
    if (this.connectionPromise) {
      return this.connectionPromise;
    }

    // Create a new connection promise
    this.connectionPromise = this.createConnection(apiUrl);

    try {
      const connection = await this.connectionPromise;
      return connection;
    } catch (error) {
      // Clear the promise on failure so next attempt can retry
      this.connectionPromise = null;
      throw error;
    }
  }

  private async createConnection(apiUrl: string): Promise<signalR.HubConnection> {
    const hubUrl = `${apiUrl}/hubs/email`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    await this.connection.start();

    this.connection.onreconnecting(() => {
      // Connection lost, attempting to reconnect
    });

    this.connection.onreconnected(() => {
      // Successfully reconnected
    });

    this.connection.onclose(() => {
      // Clear promises on disconnect so reconnection can be attempted
      this.connectionPromise = null;
    });

    return this.connection;
  }

  onNewEmail(handler: NewEmailHandler): () => void {
    if (!this.connection) {
      console.warn('SignalR connection not established');
      return () => {};
    }
    this.connection.on('NewEmail', handler);
    return () => this.connection?.off('NewEmail', handler);
  }

  onEmailUpdated(handler: EmailUpdatedHandler): () => void {
    if (!this.connection) {
      console.warn('SignalR connection not established');
      return () => {};
    }
    this.connection.on('EmailUpdated', handler);
    return () => this.connection?.off('EmailUpdated', handler);
  }

  onEmailDeleted(handler: EmailDeletedHandler): () => void {
    if (!this.connection) {
      console.warn('SignalR connection not established');
      return () => {};
    }
    this.connection.on('EmailDeleted', handler);
    return () => this.connection?.off('EmailDeleted', handler);
  }

  onUnreadCountChanged(handler: UnreadCountChangedHandler): () => void {
    if (!this.connection) {
      console.warn('SignalR connection not established');
      return () => {};
    }
    this.connection.on('UnreadCountChanged', handler);
    return () => this.connection?.off('UnreadCountChanged', handler);
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRConnection = new SignalRConnection();
