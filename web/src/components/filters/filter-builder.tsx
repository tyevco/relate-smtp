import { useState } from 'react'
import { useLabels } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { LabelBadge } from '@/components/mail/label-badge'
import type { EmailFilter, CreateEmailFilterRequest, UpdateEmailFilterRequest } from '@/api/types'

interface FilterBuilderProps {
  filter?: EmailFilter
  onSave: (data: CreateEmailFilterRequest | UpdateEmailFilterRequest) => void
  onCancel: () => void
}

export function FilterBuilder({ filter, onSave, onCancel }: FilterBuilderProps) {
  const { data: labels = [] } = useLabels()

  const [formData, setFormData] = useState({
    name: filter?.name || '',
    isEnabled: filter?.isEnabled ?? true,
    priority: filter?.priority ?? 100,
    fromAddressContains: filter?.fromAddressContains || '',
    subjectContains: filter?.subjectContains || '',
    bodyContains: filter?.bodyContains || '',
    hasAttachments: filter?.hasAttachments,
    markAsRead: filter?.markAsRead || false,
    assignLabelId: filter?.assignLabelId || '',
    delete: filter?.delete || false,
  })

  const selectedLabel = labels.find((l) => l.id === formData.assignLabelId)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    const data: CreateEmailFilterRequest = {
      name: formData.name,
      isEnabled: formData.isEnabled,
      priority: formData.priority,
      fromAddressContains: formData.fromAddressContains || undefined,
      subjectContains: formData.subjectContains || undefined,
      bodyContains: formData.bodyContains || undefined,
      hasAttachments: formData.hasAttachments,
      markAsRead: formData.markAsRead,
      assignLabelId: formData.assignLabelId || undefined,
      delete: formData.delete,
    }

    onSave(data)
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Filter Name */}
      <div className="space-y-2">
        <Label>Filter Name *</Label>
        <Input
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          placeholder="e.g., Work Emails"
          required
        />
      </div>

      {/* Priority and Enabled */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Priority</Label>
          <Input
            type="number"
            value={formData.priority}
            onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) || 0 })}
            min="0"
          />
          <p className="text-xs text-muted-foreground">Lower number = higher priority</p>
        </div>

        <div className="space-y-2">
          <Label>Status</Label>
          <div className="flex items-center space-x-2 pt-2">
            <Switch
              checked={formData.isEnabled}
              onCheckedChange={(checked) => setFormData({ ...formData, isEnabled: checked })}
            />
            <span className="text-sm">{formData.isEnabled ? 'Enabled' : 'Disabled'}</span>
          </div>
        </div>
      </div>

      {/* Conditions Section */}
      <div className="space-y-4 p-4 border rounded-md">
        <h3 className="font-medium">Conditions (When to apply this filter)</h3>

        <div className="space-y-2">
          <Label>From address contains</Label>
          <Input
            value={formData.fromAddressContains}
            onChange={(e) => setFormData({ ...formData, fromAddressContains: e.target.value })}
            placeholder="e.g., @company.com"
          />
        </div>

        <div className="space-y-2">
          <Label>Subject contains</Label>
          <Input
            value={formData.subjectContains}
            onChange={(e) => setFormData({ ...formData, subjectContains: e.target.value })}
            placeholder="e.g., [URGENT]"
          />
        </div>

        <div className="space-y-2">
          <Label>Body contains</Label>
          <Input
            value={formData.bodyContains}
            onChange={(e) => setFormData({ ...formData, bodyContains: e.target.value })}
            placeholder="e.g., invoice"
          />
        </div>

        <div className="space-y-2">
          <Label>Has attachments</Label>
          <Select
            value={
              formData.hasAttachments === undefined
                ? 'any'
                : formData.hasAttachments
                ? 'yes'
                : 'no'
            }
            onValueChange={(value) =>
              setFormData({
                ...formData,
                hasAttachments: value === 'any' ? undefined : value === 'yes',
              })
            }
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="any">Any</SelectItem>
              <SelectItem value="yes">Yes</SelectItem>
              <SelectItem value="no">No</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Actions Section */}
      <div className="space-y-4 p-4 border rounded-md">
        <h3 className="font-medium">Actions (What to do with matching emails)</h3>

        <div className="flex items-center space-x-2">
          <Switch
            checked={formData.markAsRead}
            onCheckedChange={(checked) => setFormData({ ...formData, markAsRead: checked })}
          />
          <Label>Mark as read</Label>
        </div>

        <div className="space-y-2">
          <Label>Assign label</Label>
          <Select
            value={formData.assignLabelId || 'none'}
            onValueChange={(value) =>
              setFormData({ ...formData, assignLabelId: value === 'none' ? '' : value })
            }
          >
            <SelectTrigger>
              <SelectValue>
                {selectedLabel ? (
                  <LabelBadge name={selectedLabel.name} color={selectedLabel.color} />
                ) : (
                  'None'
                )}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="none">None</SelectItem>
              {labels.map((label) => (
                <SelectItem key={label.id} value={label.id}>
                  <LabelBadge name={label.name} color={label.color} />
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex items-center space-x-2">
          <Switch
            checked={formData.delete}
            onCheckedChange={(checked) => setFormData({ ...formData, delete: checked })}
          />
          <Label className="text-destructive">Delete email</Label>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex gap-2">
        <Button type="submit" className="flex-1">
          {filter ? 'Update Filter' : 'Create Filter'}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel} className="flex-1">
          Cancel
        </Button>
      </div>
    </form>
  )
}
