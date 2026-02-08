import { useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { invoke } from '@tauri-apps/api/core'
import { isPermissionGranted, requestPermission, sendNotification } from '@tauri-apps/plugin-notification'
import {
  connect,
  disconnect,
  onNewEmail,
  onEmailUpdated,
  onEmailDeleted,
  onUnreadCountChanged,
  onReconnecting,
  onReconnected,
  onClose,
} from '../api/signalr'

export function useSignalR(serverUrl: string | null, apiKey: string | null) {
  const queryClient = useQueryClient()
  const [isConnected, setIsConnected] = useState(false)
  const [connectionError, setConnectionError] = useState<Error | null>(null)
  const setupDoneRef = useRef(false)
  const unsubscribersRef = useRef<(() => void)[]>([])

  useEffect(() => {
    if (!serverUrl || !apiKey) {
      return
    }

    // Capture values after null check to ensure type narrowing
    const currentServerUrl = serverUrl
    const currentApiKey = apiKey

    let isCancelled = false

    async function setup() {
      try {
        await connect(currentServerUrl, currentApiKey)

        // Check if effect was cancelled during the async connect
        if (isCancelled) {
          await disconnect()
          return
        }

        setupDoneRef.current = true
        setIsConnected(true)

        // Store unsubscribe functions to prevent memory leaks
        unsubscribersRef.current = [
          onNewEmail(() => {
            queryClient.invalidateQueries({ queryKey: ['emails'] })
            notifyNewEmails()
          }),
          onEmailUpdated((emailId: string) => {
            queryClient.invalidateQueries({ queryKey: ['emails'] })
            queryClient.invalidateQueries({ queryKey: ['email', emailId] })
          }),
          onEmailDeleted(() => {
            queryClient.invalidateQueries({ queryKey: ['emails'] })
          }),
          onUnreadCountChanged((count: number) => {
            invoke('set_badge_count', { count }).catch(() => {})
          }),
        ]

        onReconnecting(() => {
          setIsConnected(false)
        })

        onReconnected(() => {
          setIsConnected(true)
          // Refresh data after reconnection gap
          queryClient.invalidateQueries({ queryKey: ['emails'] })
        })

        onClose(() => {
          setIsConnected(false)
        })
      } catch (error) {
        setIsConnected(false)
        if (!isCancelled) {
          setConnectionError(error instanceof Error ? error : new Error(String(error)))
        }
      }
    }

    setup()

    return () => {
      isCancelled = true
      // Unsubscribe from all event handlers to prevent memory leaks
      unsubscribersRef.current.forEach(unsub => unsub())
      unsubscribersRef.current = []
      if (setupDoneRef.current) {
        disconnect()
        setupDoneRef.current = false
      }
      setIsConnected(false)
    }
  }, [serverUrl, apiKey, queryClient])

  return { isConnected, connectionError }
}

async function notifyNewEmails() {
  try {
    const settings = await invoke<{ show_notifications: boolean }>('get_settings')
    if (!settings.show_notifications) return
  } catch {
    // If settings unavailable, still show notification
  }

  try {
    let permitted = await isPermissionGranted()
    if (!permitted) {
      const permission = await requestPermission()
      permitted = permission === 'granted'
    }

    if (permitted) {
      sendNotification({
        title: 'Relate Mail',
        body: 'You have a new email',
      })
    }
  } catch {
    // Notifications not available
  }
}
