import { cn, getInitials, stringToColor } from '@/lib/utils'

interface AvatarProps {
  name: string | null
  email: string
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

const sizeClasses = {
  sm: 'h-8 w-8 text-xs',
  md: 'h-10 w-10 text-sm',
  lg: 'h-12 w-12 text-base',
}

export function Avatar({ name, email, size = 'md', className }: AvatarProps) {
  const initials = getInitials(name, email)
  const backgroundColor = stringToColor(email)

  return (
    <div
      className={cn(
        'flex items-center justify-center rounded-full font-semibold text-white',
        sizeClasses[size],
        className
      )}
      style={{ backgroundColor }}
    >
      {initials}
    </div>
  )
}
