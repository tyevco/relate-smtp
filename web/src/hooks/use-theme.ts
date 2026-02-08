import { useEffect } from 'react'
import { usePreferences } from '@/api/hooks'

export function useTheme() {
  const { data: preferences } = usePreferences()

  useEffect(() => {
    if (!preferences) return

    const root = document.documentElement
    const theme = preferences.theme

    // Remove existing theme classes
    root.classList.remove('light', 'dark')

    if (theme === 'system') {
      // Use system preference
      const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      root.classList.add(isDark ? 'dark' : 'light')
    } else {
      // Use user preference
      root.classList.add(theme)
    }
  }, [preferences, preferences?.theme])

  return preferences
}
