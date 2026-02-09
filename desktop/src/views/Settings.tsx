import { useState, useEffect } from 'react'
import { useAtom } from 'jotai'
import { invoke } from '@tauri-apps/api/core'
import { authAtom } from '@/stores/auth'
import { useProfile } from '@/api/hooks'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, Switch } from '@relate/shared/components/ui'

interface AppSettings {
  theme: string
  minimize_to_tray: boolean
  show_notifications: boolean
  window_width: number | null
  window_height: number | null
  window_x: number | null
  window_y: number | null
}

const defaultSettings: AppSettings = {
  theme: 'system',
  minimize_to_tray: false,
  show_notifications: true,
  window_width: null,
  window_height: null,
  window_x: null,
  window_y: null,
}

export function Settings() {
  const [auth] = useAtom(authAtom)
  const { data: profile, isLoading } = useProfile()
  const [settings, setSettings] = useState<AppSettings>(defaultSettings)

  useEffect(() => {
    invoke<AppSettings>('get_settings').then(setSettings).catch(() => {})
  }, [])

  async function updateSetting<K extends keyof AppSettings>(key: K, value: AppSettings[K]) {
    const updated = { ...settings, [key]: value }
    setSettings(updated)
    try {
      await invoke('save_settings', { settings: updated })

      // Apply theme change immediately
      if (key === 'theme') {
        applyTheme(value as string)
      }
    } catch {
      // Revert on error
      setSettings(settings)
    }
  }

  return (
    <div className="h-full overflow-auto p-6 max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Preferences</h1>

      <div className="space-y-6">
        {/* Appearance */}
        <Card>
          <CardHeader>
            <CardTitle>Appearance</CardTitle>
            <CardDescription>Customize how the app looks</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Theme</label>
              <div className="flex gap-2">
                {(['system', 'light', 'dark'] as const).map((theme) => (
                  <button
                    key={theme}
                    onClick={() => updateSetting('theme', theme)}
                    className={`px-4 py-2 rounded border text-sm capitalize ${
                      settings.theme === theme
                        ? 'bg-primary text-primary-foreground border-primary'
                        : 'bg-card border-border hover:bg-accent'
                    }`}
                  >
                    {theme}
                  </button>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Behavior */}
        <Card>
          <CardHeader>
            <CardTitle>Behavior</CardTitle>
            <CardDescription>Control how the app behaves</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Minimize to tray</p>
                <p className="text-sm text-muted-foreground">
                  Keep the app running in the system tray when you close the window
                </p>
              </div>
              <Switch
                checked={settings.minimize_to_tray}
                onCheckedChange={(checked) => updateSetting('minimize_to_tray', checked)}
              />
            </div>

            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Desktop notifications</p>
                <p className="text-sm text-muted-foreground">
                  Show notifications when new emails arrive
                </p>
              </div>
              <Switch
                checked={settings.show_notifications}
                onCheckedChange={(checked) => updateSetting('show_notifications', checked)}
              />
            </div>
          </CardContent>
        </Card>

        {/* Account */}
        <Card>
          <CardHeader>
            <CardTitle>Account</CardTitle>
            <CardDescription>Your account information</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground">Server</label>
              <p className="text-sm">{auth.serverUrl}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">Email</label>
              <p className="text-sm">{auth.userEmail}</p>
            </div>
            {!isLoading && profile && (
              <>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Display Name</label>
                  <p className="text-sm">{profile.displayName || 'Not set'}</p>
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Member Since</label>
                  <p className="text-sm">{new Date(profile.createdAt).toLocaleDateString()}</p>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        {/* About */}
        <Card>
          <CardHeader>
            <CardTitle>About</CardTitle>
            <CardDescription>Application information</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground">Version</label>
              <p className="text-sm">0.1.0</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">Platform</label>
              <p className="text-sm">Desktop (Tauri)</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function applyTheme(theme: string) {
  const root = document.documentElement
  if (theme === 'dark') {
    root.classList.add('dark')
  } else if (theme === 'light') {
    root.classList.remove('dark')
  } else {
    // System
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      root.classList.add('dark')
    } else {
      root.classList.remove('dark')
    }
  }
}
