import { useState, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
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
    const authority = import.meta.env.VITE_OIDC_AUTHORITY
    if (authority && !auth.isLoading && !auth.isAuthenticated) {
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
    <div className="container mx-auto px-4 py-6 max-w-3xl">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Preferences</h1>
        <Button onClick={handleSave} disabled={updatePreferences.isPending}>
          <Save className="h-4 w-4 mr-2" />
          Save Changes
        </Button>
      </div>

      <div className="space-y-6">
        {/* Appearance */}
        <Card>
          <CardHeader>
            <CardTitle>Appearance</CardTitle>
            <CardDescription>Customize how the interface looks and feels</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="theme">Theme</Label>
              <Select value={theme} onValueChange={(value) => setTheme(value as 'light' | 'dark' | 'system')}>
                <SelectTrigger>
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
              <Label htmlFor="density">Display Density</Label>
              <Select value={displayDensity} onValueChange={(value) => setDisplayDensity(value as 'compact' | 'comfortable' | 'spacious')}>
                <SelectTrigger>
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
            <CardTitle>Email List</CardTitle>
            <CardDescription>Configure how emails are displayed in the inbox</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="perPage">Emails per Page</Label>
              <Input
                id="perPage"
                type="number"
                min="10"
                max="100"
                step="10"
                value={emailsPerPage}
                onChange={(e) => setEmailsPerPage(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="defaultSort">Default Sort Order</Label>
              <Select value={defaultSort} onValueChange={setDefaultSort}>
                <SelectTrigger>
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

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Show Email Preview</Label>
                <p className="text-sm text-muted-foreground">
                  Display a preview of email content in the list
                </p>
              </div>
              <Switch
                checked={showPreview}
                onCheckedChange={setShowPreview}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Group by Date</Label>
                <p className="text-sm text-muted-foreground">
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
            <CardTitle>Notifications</CardTitle>
            <CardDescription>Control how and when you receive notifications</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Desktop Notifications</Label>
                <p className="text-sm text-muted-foreground">
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
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Browser Push Notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Receive notifications even when the app is closed
                    </p>
                  </div>
                  {pushNotifications.isSubscribed ? (
                    <Button
                      onClick={pushNotifications.unsubscribe}
                      disabled={pushNotifications.isLoading}
                      variant="outline"
                      size="sm"
                    >
                      <BellOff className="h-4 w-4 mr-2" />
                      Disable
                    </Button>
                  ) : (
                    <Button
                      onClick={pushNotifications.subscribe}
                      disabled={pushNotifications.isLoading}
                      size="sm"
                    >
                      <Bell className="h-4 w-4 mr-2" />
                      Enable
                    </Button>
                  )}
                </div>
                {pushNotifications.error && (
                  <p className="text-sm text-red-600 dark:text-red-400">
                    {pushNotifications.error}
                  </p>
                )}
              </div>
            )}

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Email Digest</Label>
                <p className="text-sm text-muted-foreground">
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
                  <Label htmlFor="digestFrequency">Digest Frequency</Label>
                  <Select value={digestFrequency} onValueChange={(value) => setDigestFrequency(value as 'daily' | 'weekly')}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="daily">Daily</SelectItem>
                      <SelectItem value="weekly">Weekly</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="digestTime">Digest Time</Label>
                  <Input
                    id="digestTime"
                    type="time"
                    value={digestTime}
                    onChange={(e) => setDigestTime(e.target.value)}
                  />
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {updatePreferences.isSuccess && (
        <div className="mt-4 p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-800 dark:text-green-200">
          Preferences saved successfully!
        </div>
      )}
    </div>
  )
}
