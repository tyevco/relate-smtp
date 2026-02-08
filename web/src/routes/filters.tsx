import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useFilters, useCreateFilter, useUpdateFilter, useDeleteFilter } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { FilterBuilder } from '@/components/filters/filter-builder'
import { LabelBadge } from '@/components/mail/label-badge'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Plus, Pencil, Trash2, ToggleLeft, ToggleRight } from 'lucide-react'
import type { EmailFilter, CreateEmailFilterRequest, UpdateEmailFilterRequest } from '@/api/types'

export const Route = createFileRoute('/filters')({
  component: FiltersPage,
})

function FiltersPage() {
  const { data: filters = [], isLoading } = useFilters()
  const createFilter = useCreateFilter()
  const updateFilter = useUpdateFilter()
  const deleteFilter = useDeleteFilter()

  const [editingFilter, setEditingFilter] = useState<EmailFilter | null>(null)
  const [isCreating, setIsCreating] = useState(false)

  const handleCreate = async (data: CreateEmailFilterRequest | UpdateEmailFilterRequest) => {
    await createFilter.mutateAsync(data as CreateEmailFilterRequest)
    setIsCreating(false)
  }

  const handleUpdate = async (data: CreateEmailFilterRequest | UpdateEmailFilterRequest) => {
    if (!editingFilter) return
    await updateFilter.mutateAsync({ id: editingFilter.id, data: data as UpdateEmailFilterRequest })
    setEditingFilter(null)
  }

  const handleDelete = async (id: string) => {
    // eslint-disable-next-line no-alert -- TODO: replace with confirmation dialog component
    if (window.confirm('Are you sure you want to delete this filter?')) {
      await deleteFilter.mutateAsync(id)
    }
  }

  const handleToggleEnabled = async (filter: EmailFilter) => {
    await updateFilter.mutateAsync({
      id: filter.id,
      data: { isEnabled: !filter.isEnabled },
    })
  }

  if (isCreating || editingFilter) {
    return (
      <div className="container mx-auto px-4 py-6 max-w-3xl">
        <Card>
          <CardHeader>
            <CardTitle>{editingFilter ? 'Edit Filter' : 'Create Filter'}</CardTitle>
            <CardDescription>
              {editingFilter
                ? 'Update the filter rules and actions'
                : 'Create a new filter to automatically organize your emails'}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <FilterBuilder
              filter={editingFilter || undefined}
              onSave={editingFilter ? handleUpdate : handleCreate}
              onCancel={() => {
                setIsCreating(false)
                setEditingFilter(null)
              }}
            />
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-4 py-6 max-w-5xl">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Email Filters</h1>
          <p className="text-muted-foreground">
            Automatically organize incoming emails with custom rules
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Create Filter
        </Button>
      </div>

      {isLoading ? (
        <div className="text-center py-8 text-muted-foreground">Loading filters...</div>
      ) : filters.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground mb-4">No filters created yet</p>
            <Button onClick={() => setIsCreating(true)} variant="outline">
              <Plus className="h-4 w-4 mr-2" />
              Create Your First Filter
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {filters.map((filter) => (
            <Card key={filter.id}>
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <h3 className="font-medium">{filter.name}</h3>
                      <Badge variant={filter.isEnabled ? 'default' : 'secondary'}>
                        {filter.isEnabled ? 'Enabled' : 'Disabled'}
                      </Badge>
                      <Badge variant="outline">Priority: {filter.priority}</Badge>
                      {filter.timesApplied > 0 && (
                        <Badge variant="outline">Applied {filter.timesApplied}x</Badge>
                      )}
                    </div>

                    <div className="space-y-2 text-sm">
                      {/* Conditions */}
                      <div>
                        <span className="font-medium text-muted-foreground">When:</span>
                        <ul className="ml-4 mt-1 space-y-1">
                          {filter.fromAddressContains && (
                            <li>From contains "{filter.fromAddressContains}"</li>
                          )}
                          {filter.subjectContains && (
                            <li>Subject contains "{filter.subjectContains}"</li>
                          )}
                          {filter.bodyContains && (
                            <li>Body contains "{filter.bodyContains}"</li>
                          )}
                          {filter.hasAttachments !== undefined && (
                            <li>
                              {filter.hasAttachments ? 'Has' : 'No'} attachments
                            </li>
                          )}
                        </ul>
                      </div>

                      {/* Actions */}
                      <div>
                        <span className="font-medium text-muted-foreground">Then:</span>
                        <ul className="ml-4 mt-1 space-y-1">
                          {filter.markAsRead && <li>Mark as read</li>}
                          {filter.assignLabelId && filter.assignLabelName && (
                            <li className="flex items-center gap-2">
                              Assign label:{' '}
                              <LabelBadge
                                name={filter.assignLabelName}
                                color={filter.assignLabelColor || '#3b82f6'}
                              />
                            </li>
                          )}
                          {filter.delete && <li className="text-destructive">Delete</li>}
                        </ul>
                      </div>
                    </div>
                  </div>

                  <div className="flex gap-1 ml-4">
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleToggleEnabled(filter)}
                      title={filter.isEnabled ? 'Disable' : 'Enable'}
                    >
                      {filter.isEnabled ? (
                        <ToggleRight className="h-4 w-4" />
                      ) : (
                        <ToggleLeft className="h-4 w-4" />
                      )}
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => setEditingFilter(filter)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleDelete(filter.id)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
