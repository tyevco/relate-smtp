import { useState, useEffect } from 'react'
import { api } from '@/api/client'

export function usePushNotifications() {
  const [isSupported, setIsSupported] = useState(false)
  const [isSubscribed, setIsSubscribed] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    // Check if push notifications are supported
    const supported = 'serviceWorker' in navigator && 'PushManager' in window
    setIsSupported(supported)

    if (supported) {
      checkSubscriptionStatus()
    }
  }, [])

  async function checkSubscriptionStatus() {
    try {
      const registration = await navigator.serviceWorker.ready
      const subscription = await registration.pushManager.getSubscription()
      setIsSubscribed(!!subscription)
    } catch (err) {
      console.error('Failed to check subscription status:', err)
    }
  }

  async function subscribe() {
    if (!isSupported) {
      setError('Push notifications are not supported in your browser')
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      // Request notification permission
      const permission = await Notification.requestPermission()

      if (permission !== 'granted') {
        throw new Error('Notification permission denied')
      }

      // Register service worker
      const registration = await navigator.serviceWorker.register('/sw.js')
      await navigator.serviceWorker.ready

      // Get VAPID public key from server
      const { publicKey } = await api.get<{ publicKey: string }>('/push-subscriptions/vapid-public-key')

      // Subscribe to push notifications
      const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey) as BufferSource,
      })

      // Send subscription to server
      const subscriptionData = subscription.toJSON()
      await api.post('/push-subscriptions', {
        endpoint: subscriptionData.endpoint,
        p256dhKey: subscriptionData.keys?.p256dh || '',
        authKey: subscriptionData.keys?.auth || '',
      })

      setIsSubscribed(true)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to subscribe to push notifications'
      setError(message)
      console.error('Push subscription error:', err)
    } finally {
      setIsLoading(false)
    }
  }

  async function unsubscribe() {
    if (!isSupported) {
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const registration = await navigator.serviceWorker.ready
      const subscription = await registration.pushManager.getSubscription()

      if (subscription) {
        await subscription.unsubscribe()
        // Note: We can't easily get the subscription ID to delete from server
        // The server will clean up expired subscriptions automatically
      }

      setIsSubscribed(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to unsubscribe from push notifications'
      setError(message)
      console.error('Push unsubscription error:', err)
    } finally {
      setIsLoading(false)
    }
  }

  return {
    isSupported,
    isSubscribed,
    isLoading,
    error,
    subscribe,
    unsubscribe,
  }
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')

  const rawData = window.atob(base64)
  const outputArray = new Uint8Array(rawData.length)

  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }
  return outputArray
}
