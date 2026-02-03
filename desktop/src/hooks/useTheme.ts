import { useEffect, useState } from 'react'
import { invoke } from '@tauri-apps/api/core'

type Theme = 'light' | 'dark' | 'system'

export function useTheme() {
  const [theme, setTheme] = useState<Theme>('system')
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light')

  // Load saved theme preference on mount
  useEffect(() => {
    invoke<{ theme: string }>('get_settings')
      .then((settings) => {
        const saved = settings.theme as Theme
        if (saved === 'light' || saved === 'dark' || saved === 'system') {
          setTheme(saved)
        }
      })
      .catch(() => {})
  }, [])

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

    const updateResolvedTheme = () => {
      if (theme === 'system') {
        setResolvedTheme(mediaQuery.matches ? 'dark' : 'light')
      } else {
        setResolvedTheme(theme)
      }
    }

    updateResolvedTheme()

    const handler = () => updateResolvedTheme()
    mediaQuery.addEventListener('change', handler)

    return () => mediaQuery.removeEventListener('change', handler)
  }, [theme])

  useEffect(() => {
    const root = document.documentElement
    if (resolvedTheme === 'dark') {
      root.classList.add('dark')
    } else {
      root.classList.remove('dark')
    }
  }, [resolvedTheme])

  return { theme, setTheme, resolvedTheme }
}
