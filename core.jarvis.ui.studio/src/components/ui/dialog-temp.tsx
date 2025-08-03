import * as React from "react"
import { X } from "lucide-react"
import { cn } from "@/lib/utils"

// TEMPORARY: Non-Radix Dialog component due to React 19 compatibility issues

export interface DialogProps {
  open?: boolean
  onOpenChange?: (open: boolean) => void
  children?: React.ReactNode
}

const DialogContext = React.createContext<{
  open: boolean
  onOpenChange?: (open: boolean) => void
}>({
  open: false,
})

export function Dialog({ open = false, onOpenChange, children }: DialogProps) {
  return (
    <DialogContext.Provider value={{ open, onOpenChange }}>
      {children}
    </DialogContext.Provider>
  )
}

export function DialogTrigger({ children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) {
  const { onOpenChange } = React.useContext(DialogContext)
  
  return (
    <button
      onClick={() => onOpenChange?.(true)}
      {...props}
    >
      {children}
    </button>
  )
}

export function DialogContent({
  className,
  children,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  const { open, onOpenChange } = React.useContext(DialogContext)
  
  if (!open) return null
  
  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-50 bg-black/80"
        onClick={() => onOpenChange?.(false)}
      />
      
      {/* Dialog */}
      <div className="fixed inset-0 z-50 flex items-center justify-center">
        <div
          className={cn(
            "relative max-h-[90vh] w-full max-w-lg overflow-auto rounded-lg bg-background p-6 shadow-lg",
            className
          )}
          {...props}
        >
          {/* Close button */}
          <button
            className="absolute right-4 top-4 rounded-sm opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:pointer-events-none data-[state=open]:bg-accent data-[state=open]:text-muted-foreground"
            onClick={() => onOpenChange?.(false)}
          >
            <X className="h-4 w-4" />
            <span className="sr-only">Close</span>
          </button>
          
          {children}
        </div>
      </div>
    </>
  )
}

export function DialogHeader({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex flex-col space-y-1.5 text-center sm:text-left", className)}
      {...props}
    />
  )
}

export function DialogFooter({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2",
        className
      )}
      {...props}
    />
  )
}

export function DialogTitle({
  className,
  ...props
}: React.HTMLAttributes<HTMLHeadingElement>) {
  return (
    <h2
      className={cn("text-lg font-semibold leading-none tracking-tight", className)}
      {...props}
    />
  )
}

export function DialogDescription({
  className,
  ...props
}: React.HTMLAttributes<HTMLParagraphElement>) {
  return <p className={cn("text-sm text-muted-foreground", className)} {...props} />
}