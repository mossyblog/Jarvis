import * as React from "react"
import * as TabsPrimitive from "@radix-ui/react-tabs"

import { cn } from "../../lib/utils"

const Tabs = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Root>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Root
    ref={ref}
    className={cn("flex flex-col", className)}
    {...props}
  />
))
Tabs.displayName = TabsPrimitive.Root.displayName

const TabsList = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.List>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.List>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.List
    ref={ref}
    className={cn(
      "inline-flex h-10 items-center justify-center bg-transparent text-muted-foreground",
      className
    )}
    {...props}
  />
))
TabsList.displayName = TabsPrimitive.List.displayName

const TabsTrigger = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.Trigger>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Trigger
    ref={ref}
    className={cn(
      "relative inline-flex items-center justify-center whitespace-nowrap rounded-t-md border border-transparent px-4 py-2 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
      "data-[state=active]:border-border data-[state=active]:border-b-0 data-[state=active]:bg-card data-[state=active]:text-foreground",
      // Horizontal tabs - accent line on top
      "data-[orientation=horizontal]:data-[state=active]:before:absolute data-[orientation=horizontal]:data-[state=active]:before:top-[1px] data-[orientation=horizontal]:data-[state=active]:before:left-[1px] data-[orientation=horizontal]:data-[state=active]:before:right-[1px] data-[orientation=horizontal]:data-[state=active]:before:h-[1px] data-[orientation=horizontal]:data-[state=active]:before:bg-accent",
      // Vertical tabs - accent line on right
      "data-[orientation=vertical]:rounded-t-none data-[orientation=vertical]:rounded-l-md data-[orientation=vertical]:data-[state=active]:border-r-0 data-[orientation=vertical]:data-[state=active]:border-b-border",
      "data-[orientation=vertical]:data-[state=active]:before:absolute data-[orientation=vertical]:data-[state=active]:before:top-[1px] data-[orientation=vertical]:data-[state=active]:before:bottom-[1px] data-[orientation=vertical]:data-[state=active]:before:right-[1px] data-[orientation=vertical]:data-[state=active]:before:w-[1px] data-[orientation=vertical]:data-[state=active]:before:bg-accent",
      "data-[state=inactive]:border-transparent data-[state=inactive]:bg-transparent",
      "hover:text-foreground hover:bg-muted/50",
      className
    )}
    {...props}
  />
))
TabsTrigger.displayName = TabsPrimitive.Trigger.displayName

const TabsContent = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Content>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Content
    ref={ref}
    className={cn(
      "ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
      className
    )}
    {...props}
  />
))
TabsContent.displayName = TabsPrimitive.Content.displayName

export { Tabs, TabsList, TabsTrigger, TabsContent }