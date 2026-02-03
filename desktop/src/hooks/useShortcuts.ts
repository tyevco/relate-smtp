import { useEffect, useCallback } from 'react'

interface ShortcutHandlers {
  onRefresh?: () => void
  onDelete?: () => void
  onEscape?: () => void
  onSearch?: () => void
  onMarkRead?: () => void
  onMarkUnread?: () => void
}

export function useShortcuts(handlers: ShortcutHandlers) {
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Ignore if user is typing in an input
      const target = event.target as HTMLElement
      if (
        target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.isContentEditable
      ) {
        // Only handle Escape in inputs
        if (event.key === 'Escape' && handlers.onEscape) {
          handlers.onEscape()
        }
        return
      }

      const isMod = event.ctrlKey || event.metaKey

      // Ctrl/Cmd + R - Refresh
      if (isMod && event.key === 'r') {
        event.preventDefault()
        handlers.onRefresh?.()
        return
      }

      // Delete or Backspace - Delete email
      if ((event.key === 'Delete' || event.key === 'Backspace') && handlers.onDelete) {
        event.preventDefault()
        handlers.onDelete()
        return
      }

      // Escape - Go back / clear selection
      if (event.key === 'Escape' && handlers.onEscape) {
        handlers.onEscape()
        return
      }

      // / or Ctrl/Cmd + F - Focus search
      if ((event.key === '/' || (isMod && event.key === 'f')) && handlers.onSearch) {
        event.preventDefault()
        handlers.onSearch()
        return
      }

      // Shift + U - Mark as unread
      if (event.shiftKey && event.key === 'U' && handlers.onMarkUnread) {
        event.preventDefault()
        handlers.onMarkUnread()
        return
      }

      // Shift + I - Mark as read
      if (event.shiftKey && event.key === 'I' && handlers.onMarkRead) {
        event.preventDefault()
        handlers.onMarkRead()
        return
      }
    },
    [handlers]
  )

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])
}
