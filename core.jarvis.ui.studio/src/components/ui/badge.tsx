import * as React from "react"
import { cn } from "../../lib/utils"

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "secondary" | "destructive" | "success" | "warning" | "outline"
}

export function Badge({ className, variant = "default", ...props }: BadgeProps) {
  return (
    <div
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
        variant === "default" && "bg-primary text-primary-foreground",
        variant === "secondary" && "bg-muted text-muted-foreground",
        variant === "destructive" && "bg-destructive text-destructive-foreground",
        variant === "success" && "bg-green-600 text-white dark:bg-green-400 dark:text-black",
        variant === "warning" && "bg-yellow-500 text-black dark:bg-yellow-300 dark:text-black",
        variant === "outline" && "border border-border bg-transparent",
        className
      )}
      {...props}
    />
  )
}
