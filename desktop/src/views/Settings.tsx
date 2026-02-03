import { useAtom } from 'jotai'
import { authAtom } from '@/stores/auth'
import { useProfile } from '@/api/hooks'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@relate/shared/components/ui'

export function Settings() {
  const [auth] = useAtom(authAtom)
  const { data: profile, isLoading } = useProfile()

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Settings</h1>

      <div className="space-y-6">
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
