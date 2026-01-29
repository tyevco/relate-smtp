import * as React from "react"
import { cn } from "@/lib/utils"

interface PopoverContextType {
  open: boolean
  setOpen: (open: boolean) => void
}

const PopoverContext = React.createContext<PopoverContextType | undefined>(undefined)

const usePopoverContext = () => {
  const context = React.useContext(PopoverContext)
  if (!context) {
    throw new Error("Popover components must be used within Popover")
  }
  return context
}

interface PopoverProps {
  open?: boolean
  onOpenChange?: (open: boolean) => void
  children: React.ReactNode
}

export function Popover({ open: controlledOpen, onOpenChange, children }: PopoverProps) {
  const [internalOpen, setInternalOpen] = React.useState(false)

  const open = controlledOpen !== undefined ? controlledOpen : internalOpen
  const setOpen = onOpenChange || setInternalOpen

  return (
    <PopoverContext.Provider value={{ open, setOpen }}>
      <div className="relative inline-block">{children}</div>
    </PopoverContext.Provider>
  )
}

interface PopoverTriggerProps {
  asChild?: boolean
  children: React.ReactNode
}

export function PopoverTrigger({ asChild, children }: PopoverTriggerProps) {
  const { setOpen } = usePopoverContext()

  if (asChild && React.isValidElement(children)) {
    return React.cloneElement(children, {
      onClick: (e: React.MouseEvent) => {
        setOpen(true)
        ;(children.props as any).onClick?.(e)
      },
    } as any)
  }

  return (
    <button type="button" onClick={() => setOpen(true)}>
      {children}
    </button>
  )
}

interface PopoverContentProps {
  children: React.ReactNode
  className?: string
  align?: "start" | "center" | "end"
}

export function PopoverContent({ children, className, align = "center" }: PopoverContentProps) {
  const { open, setOpen } = usePopoverContext()

  if (!open) return null

  const alignmentClasses = {
    start: "left-0",
    center: "left-1/2 -translate-x-1/2",
    end: "right-0",
  }

  return (
    <>
      <div
        className="fixed inset-0 z-40"
        onClick={() => setOpen(false)}
      />
      <div
        className={cn(
          "absolute z-50 mt-2 rounded-md border bg-popover p-4 text-popover-foreground shadow-md outline-none",
          alignmentClasses[align],
          className
        )}
      >
        {children}
      </div>
    </>
  )
}
