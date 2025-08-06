import * as React from "react"
import { cn } from "@/lib/utils"
import { type VariantProps, cva } from "class-variance-authority"

const iconButtonVariants = cva(
  "inline-flex items-center justify-center rounded-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 active:scale-95 active:transition-none",
  {
    variants: {
      variant: {
        default: "bg-transparent hover:bg-muted/60 hover:shadow-sm",
        ghost: "bg-transparent hover:bg-muted/50 hover:shadow-xs",
        solid: "bg-muted hover:bg-muted/80 hover:shadow-sm",
      },
      size: {
        sm: "h-xs w-xs p-xs", // 32px button, 4px padding, 16px icon
        md: "h-sm w-sm p-xs", // 40px button, 4px padding, 20px icon  
        lg: "h-md w-md p-xs", // 48px button, 4px padding, 24px icon
      },
    },
    defaultVariants: {
      variant: "default",
      size: "sm",
    },
  }
)

export interface IconButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof iconButtonVariants> {
  asChild?: boolean
}

const IconButton = React.forwardRef<HTMLButtonElement, IconButtonProps>(
  ({ className, variant, size, ...props }, ref) => {
    return (
      <button
        className={cn(iconButtonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
IconButton.displayName = "IconButton"

export { IconButton, iconButtonVariants }