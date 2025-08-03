import * as React from "react"
import { cn } from "../../lib/utils"

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "secondary" | "destructive" | "success" | "warning" | "outline"
}

export function Badge({ className, variant = "default", ...props }: BadgeProps) {
  return (
    <div
      className={cn(
        "inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium transition-colors focus:outline-none focus:ring-1 focus:ring-ring focus:ring-offset-1",
        variant === "default" && "bg-primary/10 text-primary border border-primary/20",
        variant === "secondary" && "bg-muted/50 text-muted-foreground border border-border/40",
        variant === "destructive" && "bg-destructive/10 text-destructive border border-destructive/20",
        variant === "success" && "bg-green-50 text-green-700 border border-green-200",
        variant === "warning" && "bg-orange-50 text-orange-700 border border-orange-200",
        variant === "outline" && "border border-border/60 bg-transparent text-foreground/80 hover:bg-muted/30",
        className
      )}
      {...props}
    />
  )
}
