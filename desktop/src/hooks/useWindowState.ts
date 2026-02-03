import { useEffect } from 'react'
import { getCurrentWindow } from '@tauri-apps/api/window'
import { invoke } from '@tauri-apps/api/core'

interface AppSettings {
  theme: string
  minimize_to_tray: boolean
  show_notifications: boolean
  window_width: number | null
  window_height: number | null
  window_x: number | null
  window_y: number | null
}

export function useWindowState() {
  useEffect(() => {
    const window = getCurrentWindow()

    // Restore saved window state on mount
    async function restore() {
      try {
        const settings: AppSettings = await invoke('get_settings')
        if (settings.window_width && settings.window_height) {
          await window.setSize({
            type: 'Logical',
            width: settings.window_width,
            height: settings.window_height,
          })
        }
        if (settings.window_x !== null && settings.window_y !== null) {
          await window.setPosition({
            type: 'Logical',
            x: settings.window_x,
            y: settings.window_y,
          })
        }
      } catch {
        // Use default window state
      }
    }

    restore()

    // Save window state periodically and on close
    let saveTimeout: ReturnType<typeof setTimeout> | null = null

    async function saveState() {
      try {
        const size = await window.innerSize()
        const position = await window.outerPosition()
        const settings: AppSettings = await invoke('get_settings')

        await invoke('save_settings', {
          settings: {
            ...settings,
            window_width: size.width,
            window_height: size.height,
            window_x: position.x,
            window_y: position.y,
          },
        })
      } catch {
        // Ignore save errors
      }
    }

    function debouncedSave() {
      if (saveTimeout) clearTimeout(saveTimeout)
      saveTimeout = setTimeout(saveState, 500)
    }

    const unlistenMove = window.onMoved(debouncedSave)
    const unlistenResize = window.onResized(debouncedSave)

    return () => {
      if (saveTimeout) clearTimeout(saveTimeout)
      unlistenMove.then(fn => fn())
      unlistenResize.then(fn => fn())
    }
  }, [])
}
