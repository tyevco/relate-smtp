import { useState, useMemo } from 'react'
import { useLabels, useAddLabelToEmail, useRemoveLabelFromEmail } from '@/api/hooks'
import { Button } from '@/components/ui/button'
import { LabelBadge } from './label-badge'
import { Check, Tag } from 'lucide-react'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'

interface LabelSelectorProps {
  emailId: string
  currentLabels: { id: string; name: string; color: string }[]
  onLabelChange?: () => void
}

export function LabelSelector({ emailId, currentLabels, onLabelChange }: LabelSelectorProps) {
  const { data: allLabels = [] } = useLabels()
  const addLabel = useAddLabelToEmail()
  const removeLabel = useRemoveLabelFromEmail()
  const [open, setOpen] = useState(false)

  const currentLabelIds = useMemo(() => new Set(currentLabels.map((l) => l.id)), [currentLabels])

  const handleToggleLabel = async (labelId: string) => {
    if (currentLabelIds.has(labelId)) {
      await removeLabel.mutateAsync({ emailId, labelId })
    } else {
      await addLabel.mutateAsync({ emailId, labelId })
    }
    onLabelChange?.()
  }

  return (
    <div className="flex items-center gap-2">
      {/* Display current labels */}
      {currentLabels.map((label) => (
        <LabelBadge
          key={label.id}
          name={label.name}
          color={label.color}
          onRemove={() => handleToggleLabel(label.id)}
        />
      ))}

      {/* Add label button */}
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button variant="outline" size="sm">
            <Tag className="h-4 w-4 mr-1" />
            Labels
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-56 p-2">
          <div className="space-y-1">
            {allLabels.length === 0 ? (
              <div className="text-sm text-muted-foreground p-2">
                No labels created yet
              </div>
            ) : (
              allLabels.map((label) => {
                const isSelected = currentLabelIds.has(label.id)
                return (
                  <button
                    key={label.id}
                    className="flex items-center justify-between w-full p-2 rounded-md hover:bg-muted"
                    onClick={() => handleToggleLabel(label.id)}
                  >
                    <LabelBadge name={label.name} color={label.color} />
                    {isSelected && <Check className="h-4 w-4" />}
                  </button>
                )
              })
            )}
          </div>
        </PopoverContent>
      </Popover>
    </div>
  )
}
