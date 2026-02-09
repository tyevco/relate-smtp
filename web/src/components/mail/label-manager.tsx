import { useState } from 'react'
import { useLabels, useCreateLabel, useUpdateLabel, useDeleteLabel } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label as LabelUI } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmationDialog } from '@relate/shared/components/ui'
import { LabelBadge } from './label-badge'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import type { Label } from '@/api/types'

const PRESET_COLORS = [
  '#ef4444', // red
  '#f97316', // orange
  '#f59e0b', // amber
  '#84cc16', // lime
  '#22c55e', // green
  '#14b8a6', // teal
  '#3b82f6', // blue
  '#6366f1', // indigo
  '#a855f7', // purple
  '#ec4899', // pink
]

interface LabelManagerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function LabelManager({ open, onOpenChange }: LabelManagerProps) {
  const { data: labels = [] } = useLabels()
  const createLabel = useCreateLabel()
  const updateLabel = useUpdateLabel()
  const deleteLabel = useDeleteLabel()

  const [editingLabel, setEditingLabel] = useState<Label | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [deletingLabelId, setDeletingLabelId] = useState<string | null>(null)
  const [formData, setFormData] = useState({ name: '', color: PRESET_COLORS[0] })

  const handleCreate = async () => {
    if (!formData.name.trim()) return

    await createLabel.mutateAsync({
      name: formData.name,
      color: formData.color,
    })

    setFormData({ name: '', color: PRESET_COLORS[0] })
    setIsCreating(false)
  }

  const handleUpdate = async () => {
    if (!editingLabel || !formData.name.trim()) return

    await updateLabel.mutateAsync({
      id: editingLabel.id,
      data: {
        name: formData.name,
        color: formData.color,
      },
    })

    setEditingLabel(null)
    setFormData({ name: '', color: PRESET_COLORS[0] })
  }

  const handleDelete = async () => {
    if (!deletingLabelId) return
    await deleteLabel.mutateAsync(deletingLabelId)
    setDeletingLabelId(null)
  }

  const startEdit = (label: Label) => {
    setEditingLabel(label)
    setFormData({ name: label.name, color: label.color })
    setIsCreating(false)
  }

  const startCreate = () => {
    setIsCreating(true)
    setEditingLabel(null)
    setFormData({ name: '', color: PRESET_COLORS[0] })
  }

  const cancelEdit = () => {
    setIsCreating(false)
    setEditingLabel(null)
    setFormData({ name: '', color: PRESET_COLORS[0] })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Manage Labels</DialogTitle>
          <DialogDescription>
            Create and organize labels to categorize your emails.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Label List */}
          <div className="space-y-2 max-h-[300px] overflow-y-auto">
            {labels.map((label) => (
              <div
                key={label.id}
                className="flex items-center justify-between p-2 rounded-md hover:bg-muted"
              >
                <LabelBadge name={label.name} color={label.color} />
                <div className="flex gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => startEdit(label)}
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => setDeletingLabelId(label.id)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {/* Create/Edit Form */}
          {(isCreating || editingLabel) && (
            <div className="space-y-3 p-4 border rounded-md">
              <div className="space-y-2">
                <LabelUI>Label Name</LabelUI>
                <Input
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="Enter label name"
                />
              </div>

              <div className="space-y-2">
                <LabelUI>Color</LabelUI>
                <div className="flex gap-2 flex-wrap">
                  {PRESET_COLORS.map((color) => (
                    <button
                      key={color}
                      className={`w-8 h-8 rounded-full border-2 ${
                        formData.color === color ? 'border-foreground' : 'border-transparent'
                      }`}
                      style={{ backgroundColor: color }}
                      onClick={() => setFormData({ ...formData, color })}
                    />
                  ))}
                </div>
              </div>

              <div className="flex gap-2">
                <Button
                  onClick={editingLabel ? handleUpdate : handleCreate}
                  className="flex-1"
                >
                  {editingLabel ? 'Update' : 'Create'}
                </Button>
                <Button variant="outline" onClick={cancelEdit} className="flex-1">
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {/* Create Button */}
          {!isCreating && !editingLabel && (
            <Button onClick={startCreate} variant="outline" className="w-full">
              <Plus className="h-4 w-4 mr-2" />
              Create New Label
            </Button>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Close
          </Button>
        </DialogFooter>
      </DialogContent>

      <ConfirmationDialog
        open={!!deletingLabelId}
        onOpenChange={(open) => !open && setDeletingLabelId(null)}
        title="Delete Label"
        description="Are you sure you want to delete this label? It will be removed from all emails."
        confirmLabel="Delete"
        variant="destructive"
        onConfirm={handleDelete}
        isLoading={deleteLabel.isPending}
      />
    </Dialog>
  )
}
