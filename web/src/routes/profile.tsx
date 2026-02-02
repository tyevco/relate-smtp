import { useState, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from 'react-oidc-context'
import { getConfig } from '@/config'
import { useProfile, useUpdateProfile, useAddEmailAddress, useRemoveEmailAddress } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Trash2, Plus, Check, X } from 'lucide-react'

export const Route = createFileRoute('/profile')({
  component: ProfilePage,
})

function ProfilePage() {
  const auth = useAuth()
  const { data: profile, isLoading } = useProfile()
  const updateProfile = useUpdateProfile()
  const addAddress = useAddEmailAddress()
  const removeAddress = useRemoveEmailAddress()

  const [isEditingName, setIsEditingName] = useState(false)
  const [displayName, setDisplayName] = useState('')
  const [newAddress, setNewAddress] = useState('')
  const [isAddingAddress, setIsAddingAddress] = useState(false)

  // Redirect to login if not authenticated
  useEffect(() => {
    const config = getConfig()
    if (config.oidcAuthority && !auth.isLoading && !auth.isAuthenticated) {
      window.location.href = '/login'
    }
  }, [auth.isAuthenticated, auth.isLoading])

  const handleSaveName = () => {
    updateProfile.mutate({ displayName }, {
      onSuccess: () => setIsEditingName(false),
    })
  }

  const handleAddAddress = () => {
    if (newAddress.trim()) {
      addAddress.mutate(newAddress.trim(), {
        onSuccess: () => {
          setNewAddress('')
          setIsAddingAddress(false)
        },
      })
    }
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-6">
        <div className="text-center text-muted-foreground">Loading...</div>
      </div>
    )
  }

  if (!profile) {
    return (
      <div className="container mx-auto px-4 py-6">
        <div className="text-center text-muted-foreground">
          Please log in to view your profile
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6 max-w-2xl">
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Profile</h1>

      <div className="space-y-4 sm:space-y-6">
        {/* Basic Info */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg">Basic Information</CardTitle>
            <CardDescription className="text-xs sm:text-sm">Your account details</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-xs sm:text-sm font-medium text-muted-foreground">Display Name</label>
              {isEditingName ? (
                <div className="flex flex-col sm:flex-row gap-2 mt-1">
                  <Input
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    placeholder="Enter display name"
                    className="text-sm"
                  />
                  <div className="flex gap-2">
                    <Button size="icon" onClick={handleSaveName} className="min-h-[44px]">
                      <Check className="h-4 w-4" />
                    </Button>
                    <Button size="icon" variant="outline" onClick={() => setIsEditingName(false)} className="min-h-[44px]">
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-between mt-1 gap-2">
                  <span className="text-sm break-words">{profile.displayName || '(Not set)'}</span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setDisplayName(profile.displayName || '')
                      setIsEditingName(true)
                    }}
                    className="min-h-[44px] whitespace-nowrap"
                  >
                    Edit
                  </Button>
                </div>
              )}
            </div>

            <div>
              <label className="text-xs sm:text-sm font-medium text-muted-foreground">Primary Email</label>
              <p className="mt-1 text-sm break-all">{profile.email}</p>
            </div>

            <div>
              <label className="text-xs sm:text-sm font-medium text-muted-foreground">Member Since</label>
              <p className="mt-1 text-sm">
                {new Date(profile.createdAt).toLocaleDateString()}
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Additional Email Addresses */}
        <Card>
          <CardHeader>
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 sm:gap-4">
              <div className="flex-1">
                <CardTitle className="text-base sm:text-lg">Email Addresses</CardTitle>
                <CardDescription className="text-xs sm:text-sm">
                  Add additional email addresses to receive emails from
                </CardDescription>
              </div>
              {!isAddingAddress && (
                <Button onClick={() => setIsAddingAddress(true)} className="w-full sm:w-auto min-h-[44px]">
                  <Plus className="h-4 w-4 mr-1" />
                  Add
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {/* Primary email */}
              <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-2 rounded bg-muted gap-2">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="text-sm break-all">{profile.email}</span>
                  <Badge variant="secondary" className="text-xs">Primary</Badge>
                </div>
              </div>

              {/* Additional addresses */}
              {profile.additionalAddresses.map((addr) => (
                <div key={addr.id} className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-2 rounded border gap-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm break-all">{addr.address}</span>
                    {addr.isVerified ? (
                      <Badge variant="secondary" className="text-xs">Verified</Badge>
                    ) : (
                      <Badge variant="outline" className="text-xs">Unverified</Badge>
                    )}
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => removeAddress.mutate(addr.id)}
                    className="min-h-[44px]"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              ))}

              {/* Add new address form */}
              {isAddingAddress && (
                <div className="flex flex-col sm:flex-row gap-2 p-2">
                  <Input
                    type="email"
                    value={newAddress}
                    onChange={(e) => setNewAddress(e.target.value)}
                    placeholder="email@example.com"
                    className="text-sm"
                  />
                  <div className="flex gap-2">
                    <Button onClick={handleAddAddress} disabled={addAddress.isPending} className="flex-1 sm:flex-none min-h-[44px]">
                      Add
                    </Button>
                    <Button variant="outline" onClick={() => setIsAddingAddress(false)} className="flex-1 sm:flex-none min-h-[44px]">
                      Cancel
                    </Button>
                  </div>
                </div>
              )}

              {profile.additionalAddresses.length === 0 && !isAddingAddress && (
                <p className="text-xs sm:text-sm text-muted-foreground text-center py-4">
                  No additional email addresses
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
