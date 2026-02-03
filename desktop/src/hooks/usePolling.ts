import { useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { invoke } from '@tauri-apps/api/core'
import { isPermissionGranted, requestPermission, sendNotification } from '@tauri-apps/plugin-notification'
import { apiGet } from '../api/client'
import type { EmailListResponse } from '@relate/shared/api/types'

const POLL_INTERVAL = 60_000 // 1 minute

export function usePolling(enabled = true) {
  const queryClient = useQueryClient()
  const previousUnreadRef = useRef<number | null>(null)

  useEffect(() => {
    if (!enabled) return

    let active = true

    async function poll() {
      if (!active) return

      try {
        const data = await apiGet<EmailListResponse>('/emails?page=1&pageSize=1')
        const unreadCount = data.unreadCount

        // Update badge
        await invoke('set_badge_count', { count: unreadCount }).catch(() => {})

        // Send notification if new unread emails arrived (check setting)
        if (
          previousUnreadRef.current !== null &&
          unreadCount > previousUnreadRef.current
        ) {
          const newCount = unreadCount - previousUnreadRef.current
          try {
            const settings = await invoke<{ show_notifications: boolean }>('get_settings')
            if (settings.show_notifications) {
              await notifyNewEmails(newCount)
            }
          } catch {
            await notifyNewEmails(newCount)
          }
        }

        previousUnreadRef.current = unreadCount

        // Invalidate email queries so UI refreshes
        queryClient.invalidateQueries({ queryKey: ['emails'] })
      } catch {
        // Silently ignore polling errors
      }
    }

    const interval = setInterval(poll, POLL_INTERVAL)

    // Initial poll
    poll()

    return () => {
      active = false
      clearInterval(interval)
    }
  }, [queryClient, enabled])
}

async function notifyNewEmails(count: number) {
  try {
    let permitted = await isPermissionGranted()
    if (!permitted) {
      const permission = await requestPermission()
      permitted = permission === 'granted'
    }

    if (permitted) {
      sendNotification({
        title: 'Relate Mail',
        body: count === 1
          ? 'You have 1 new email'
          : `You have ${count} new emails`,
      })
    }
  } catch {
    // Notifications not available
  }
}
