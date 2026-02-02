import { useState, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { usePreferences, useUpdatePreferences } from '@/api/hooks'
import { usePushNotifications } from '@/hooks/use-push-notifications'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { Input } from '@/components/ui/input'
import { Save, Bell, BellOff } from 'lucide-react'

export const Route = createFileRoute('/preferences')({
  component: PreferencesPage,
})

function PreferencesPage() {
  const auth = useAuth()
  const { data: preferences, isLoading } = usePreferences()
  const updatePreferences = useUpdatePreferences()
  const pushNotifications = usePushNotifications()

  const [theme, setTheme] = useState<'light' | 'dark' | 'system'>('system')
  const [displayDensity, setDisplayDensity] = useState<'compact' | 'comfortable' | 'spacious'>('comfortable')
  const [emailsPerPage, setEmailsPerPage] = useState('20')
  const [defaultSort, setDefaultSort] = useState('receivedAt-desc')
  const [showPreview, setShowPreview] = useState(true)
  const [groupByDate, setGroupByDate] = useState(false)
  const [desktopNotifications, setDesktopNotifications] = useState(false)
  const [emailDigest, setEmailDigest] = useState(false)
  const [digestFrequency, setDigestFrequency] = useState<'daily' | 'weekly'>('daily')
  const [digestTime, setDigestTime] = useState('09:00')

  // Redirect to login if not authenticated
  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      window.location.href = '/login'
    }
  }, [auth.isAuthenticated, auth.isLoading])

  // Load preferences into state
  useEffect(() => {
    if (preferences) {
      setTheme(preferences.theme)
      setDisplayDensity(preferences.displayDensity)
      setEmailsPerPage(preferences.emailsPerPage.toString())
      setDefaultSort(preferences.defaultSort)
      setShowPreview(preferences.showPreview)
      setGroupByDate(preferences.groupByDate)
      setDesktopNotifications(preferences.desktopNotifications)
      setEmailDigest(preferences.emailDigest)
      setDigestFrequency(preferences.digestFrequency)
      setDigestTime(preferences.digestTime.substring(0, 5)) // Extract HH:mm from HH:mm:ss
    }
  }, [preferences])

  const handleSave = () => {
    updatePreferences.mutate({
      theme,
      displayDensity,
      emailsPerPage: parseInt(emailsPerPage, 10),
      defaultSort,
      showPreview,
      groupByDate,
      desktopNotifications,
      emailDigest,
      digestFrequency,
      digestTime,
    })
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-6">
        <div className="text-center text-muted-foreground">Loading...</div>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6 max-w-3xl">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-4 sm:mb-6 gap-3">
        <h1 className="text-xl sm:text-2xl font-bold">Preferences</h1>
        <Button onClick={handleSave} disabled={updatePreferences.isPending} className="w-full sm:w-auto min-h-[44px]">
          <Save className="h-4 w-4 mr-2" />
          Save Changes
        </Button>
      </div>

      <div className="space-y-4 sm:space-y-6">
        {/* Appearance */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg">Appearance</CardTitle>
            <CardDescription className="text-xs sm:text-sm">Customize how the interface looks and feels</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4 sm:space-y-6">
            <div className="space-y-2">
              <Label htmlFor="theme" className="text-xs sm:text-sm">Theme</Label>
              <Select value={theme} onValueChange={(value) => setTheme(value as 'light' | 'dark' | 'system')}>
                <SelectTrigger className="text-sm">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="light">Light</SelectItem>
                  <SelectItem value="dark">Dark</SelectItem>
                  <SelectItem value="system">System</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="density" className="text-xs sm:text-sm">Display Density</Label>
              <Select value={displayDensity} onValueChange={(value) => setDisplayDensity(value as 'compact' | 'comfortable' | 'spacious')}>
                <SelectTrigger className="text-sm">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="compact">Compact</SelectItem>
                  <SelectItem value="comfortable">Comfortable</SelectItem>
                  <SelectItem value="spacious">Spacious</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        {/* Email List */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg">Email List</CardTitle>
            <CardDescription className="text-xs sm:text-sm">Configure how emails are displayed in the inbox</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4 sm:space-y-6">
            <div className="space-y-2">
              <Label htmlFor="perPage" className="text-xs sm:text-sm">Emails per Page</Label>
              <Input
                id="perPage"
                type="number"
                min="10"
                max="100"
                step="10"
                value={emailsPerPage}
                onChange={(e) => setEmailsPerPage(e.target.value)}
                className="text-sm"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="defaultSort" className="text-xs sm:text-sm">Default Sort Order</Label>
              <Select value={defaultSort} onValueChange={setDefaultSort}>
                <SelectTrigger className="text-sm">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="receivedAt-desc">Newest First</SelectItem>
                  <SelectItem value="receivedAt-asc">Oldest First</SelectItem>
                  <SelectItem value="subject-asc">Subject (A-Z)</SelectItem>
                  <SelectItem value="subject-desc">Subject (Z-A)</SelectItem>
                  <SelectItem value="from-asc">From (A-Z)</SelectItem>
                  <SelectItem value="from-desc">From (Z-A)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0">
              <div className="space-y-0.5 flex-1">
                <Label className="text-xs sm:text-sm">Show Email Preview</Label>
                <p className="text-xs sm:text-sm text-muted-foreground">
                  Display a preview of email content in the list
                </p>
              </div>
              <Switch
                checked={showPreview}
                onCheckedChange={setShowPreview}
              />
            </div>

            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0">
              <div className="space-y-0.5 flex-1">
                <Label className="text-xs sm:text-sm">Group by Date</Label>
                <p className="text-xs sm:text-sm text-muted-foreground">
                  Group emails by date (Today, Yesterday, etc.)
                </p>
              </div>
              <Switch
                checked={groupByDate}
                onCheckedChange={setGroupByDate}
              />
            </div>
          </CardContent>
        </Card>

        {/* Notifications */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg">Notifications</CardTitle>
            <CardDescription className="text-xs sm:text-sm">Control how and when you receive notifications</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4 sm:space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0">
              <div className="space-y-0.5 flex-1">
                <Label className="text-xs sm:text-sm">Desktop Notifications</Label>
                <p className="text-xs sm:text-sm text-muted-foreground">
                  Show desktop notifications for new emails
                </p>
              </div>
              <Switch
                checked={desktopNotifications}
                onCheckedChange={setDesktopNotifications}
              />
            </div>

            {pushNotifications.isSupported && (
              <div className="space-y-4 pt-4 border-t">
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0">
                  <div className="space-y-0.5 flex-1">
                    <Label className="text-xs sm:text-sm">Browser Push Notifications</Label>
                    <p className="text-xs sm:text-sm text-muted-foreground">
                      Receive notifications even when the app is closed
                    </p>
                  </div>
                  {pushNotifications.isSubscribed ? (
                    <Button
                      onClick={pushNotifications.unsubscribe}
                      disabled={pushNotifications.isLoading}
                      variant="outline"
                      size="sm"
                      className="min-h-[44px] w-full sm:w-auto"
                    >
                      <BellOff className="h-4 w-4 mr-2" />
                      Disable
                    </Button>
                  ) : (
                    <Button
                      onClick={pushNotifications.subscribe}
                      disabled={pushNotifications.isLoading}
                      size="sm"
                      className="min-h-[44px] w-full sm:w-auto"
                    >
                      <Bell className="h-4 w-4 mr-2" />
                      Enable
                    </Button>
                  )}
                </div>
                {pushNotifications.error && (
                  <p className="text-xs sm:text-sm text-red-600 dark:text-red-400">
                    {pushNotifications.error}
                  </p>
                )}
              </div>
            )}

            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0">
              <div className="space-y-0.5 flex-1">
                <Label className="text-xs sm:text-sm">Email Digest</Label>
                <p className="text-xs sm:text-sm text-muted-foreground">
                  Receive periodic email summaries
                </p>
              </div>
              <Switch
                checked={emailDigest}
                onCheckedChange={setEmailDigest}
              />
            </div>

            {emailDigest && (
              <>
                <div className="space-y-2">
                  <Label htmlFor="digestFrequency" className="text-xs sm:text-sm">Digest Frequency</Label>
                  <Select value={digestFrequency} onValueChange={(value) => setDigestFrequency(value as 'daily' | 'weekly')}>
                    <SelectTrigger className="text-sm">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="daily">Daily</SelectItem>
                      <SelectItem value="weekly">Weekly</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="digestTime" className="text-xs sm:text-sm">Digest Time</Label>
                  <Input
                    id="digestTime"
                    type="time"
                    value={digestTime}
                    onChange={(e) => setDigestTime(e.target.value)}
                    className="text-sm"
                  />
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {updatePreferences.isSuccess && (
        <div className="mt-4 p-3 sm:p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-800 dark:text-green-200 text-xs sm:text-sm">
          Preferences saved successfully!
        </div>
      )}
    </div>
  )
}
