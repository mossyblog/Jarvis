import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const iconVariants = cva(
  "flex-shrink-0 inline-block",
  {
    variants: {
      size: {
        xs: "h-xs w-xs", // 8px x 8px
        sm: "h-sm w-sm", // 16px x 16px  
        md: "h-md w-md", // 24px x 24px
        lg: "h-lg w-lg", // 32px x 32px
        xl: "h-xl w-xl", // 40px x 40px
        "2xl": "h-2xl w-2xl", // 48px x 48px
        "3xl": "h-3xl w-3xl", // 64px x 64px
      },
    },
    defaultVariants: {
      size: "sm",
    },
  }
)

export interface IconProps
  extends React.SVGAttributes<SVGElement>,
    VariantProps<typeof iconVariants> {
  children: React.ReactNode
  asChild?: boolean
}

const Icon = React.forwardRef<SVGSVGElement, IconProps>(
  ({ className, size, children, asChild = false, ...props }, ref) => {
    if (asChild) {
      return React.cloneElement(children as React.ReactElement, {
        className: cn(iconVariants({ size }), className),
        ref,
        ...props,
      } as any)
    }

    return (
      <svg
        ref={ref}
        className={cn(iconVariants({ size, className }))}
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        viewBox="0 0 24 24"
        {...props}
      >
        {children}
      </svg>
    )
  }
)
Icon.displayName = "Icon"

// Wrapper for Lucide icons to ensure consistent sizing
export interface LucideIconProps
  extends Omit<React.ComponentProps<any>, 'size'>,
    VariantProps<typeof iconVariants> {
  icon: React.ComponentType<any>
}

const LucideIcon = React.forwardRef<SVGSVGElement, LucideIconProps>(
  ({ className, size, icon: IconComponent, ...props }, ref) => {
    const sizeMap: Record<string, number> = {
      xs: 8,
      sm: 16,
      md: 24,
      lg: 32,
      xl: 40,
      "2xl": 48,
      "3xl": 64,
    }

    const currentSize = size || "sm"
    const iconSize = sizeMap[currentSize] || 16

    return (
      <IconComponent
        ref={ref}
        size={iconSize}
        className={cn(iconVariants({ size }), className)}
        {...props}
      />
    )
  }
)
LucideIcon.displayName = "LucideIcon"

export { Icon, LucideIcon, iconVariants }
