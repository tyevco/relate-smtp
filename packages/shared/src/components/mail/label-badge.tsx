import { Badge } from '../ui/badge'
import { X } from 'lucide-react'
import { cn } from '../../lib/utils'

interface LabelBadgeProps {
  name: string
  color: string
  onRemove?: () => void
  className?: string
}

export function LabelBadge({ name, color, onRemove, className }: LabelBadgeProps) {
  return (
    <Badge
      variant="outline"
      className={cn(
        'px-2 py-0.5 text-xs font-medium',
        className
      )}
      style={{
        backgroundColor: `${color}20`,
        borderColor: color,
        color: color,
      }}
    >
      {name}
      {onRemove && (
        <button
          onClick={(e) => {
            e.stopPropagation()
            onRemove()
          }}
          className="ml-1 hover:opacity-70"
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </Badge>
  )
}
