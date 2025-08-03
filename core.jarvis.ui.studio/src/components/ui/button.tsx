import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center whitespace-nowrap font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 active:scale-95 active:transition-none",
  {
    variants: {
      variant: {
        default:
          "bg-primary text-primary-foreground shadow hover:bg-primary/90",
        destructive:
          "bg-destructive text-destructive-foreground shadow-sm hover:bg-destructive/90",
        outline:
          "border border-border/60 bg-transparent hover:bg-muted/50 hover:border-border transition-colors",
        secondary:
          "bg-muted/50 text-foreground hover:bg-muted/70 transition-colors",
        ghost: "hover:bg-muted/40 hover:text-foreground transition-colors",
        link: "text-primary underline-offset-4 hover:underline",
      },
      size: {
        xs: "h-lg px-sm text-xs gap-xs [&_svg]:h-md [&_svg]:w-md rounded-sm",
        sm: "h-xl px-md text-sm gap-sm [&_svg]:h-md [&_svg]:w-md rounded-sm", 
        default: "h-xl px-lg text-sm gap-sm [&_svg]:h-md [&_svg]:w-md rounded-sm",
        lg: "h-2xl px-xl text-md gap-md [&_svg]:h-lg [&_svg]:w-lg rounded-md",
        xl: "h-3xl px-2xl text-lg gap-md [&_svg]:h-xl [&_svg]:w-xl rounded-md",
        icon: "h-xl w-xl min-w-xl min-h-xl p-sm flex-shrink-0 [&_svg]:h-md [&_svg]:w-md [&_svg]:flex-shrink-0 rounded-sm",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
  /** Custom keyboard shortcut hint */
  shortcut?: string
  /** Handle keyboard activation */
  onKeyboardActivate?: () => void
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, shortcut, onKeyboardActivate, onKeyDown, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    
    const handleKeyDown = React.useCallback((event: React.KeyboardEvent<HTMLButtonElement>) => {
      // Handle Enter and Space for activation
      if ((event.key === 'Enter' || event.key === ' ') && !event.defaultPrevented) {
        event.preventDefault()
        onKeyboardActivate?.()
        // If no custom handler, trigger click
        if (!onKeyboardActivate && event.currentTarget.click) {
          event.currentTarget.click()
        }
      }
      
      // Call original onKeyDown if provided
      onKeyDown?.(event)
    }, [onKeyDown, onKeyboardActivate])
    
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        onKeyDown={handleKeyDown}
        aria-keyshortcuts={shortcut}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

// eslint-disable-next-line react-refresh/only-export-components
export { Button, buttonVariants }