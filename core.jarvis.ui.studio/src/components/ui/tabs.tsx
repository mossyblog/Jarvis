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
      "inline-flex h-10 items-center justify-center text-muted-foreground",
      // Add a subtle background and bottom border for visual separation
      "bg-muted/30 border-b border-border",
      // Ensure tabs sit properly on the border
      "relative",
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
      // Base styling
      "relative inline-flex items-center justify-center whitespace-nowrap px-4 py-2 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
      
      // Horizontal tabs (default)
      "rounded-t-md border border-transparent",
      "data-[state=active]:border-border data-[state=active]:border-b-0 data-[state=active]:bg-card data-[state=active]:text-foreground data-[state=active]:z-10",
      // Accent line on top for horizontal active tabs
      "data-[orientation=horizontal]:data-[state=active]:before:absolute data-[orientation=horizontal]:data-[state=active]:before:top-0 data-[orientation=horizontal]:data-[state=active]:before:left-0 data-[orientation=horizontal]:data-[state=active]:before:right-0 data-[orientation=horizontal]:data-[state=active]:before:h-[2px] data-[orientation=horizontal]:data-[state=active]:before:bg-primary data-[orientation=horizontal]:data-[state=active]:before:rounded-t-md",
      
      // Vertical tabs 
      "data-[orientation=vertical]:rounded-t-none data-[orientation=vertical]:rounded-l-md data-[orientation=vertical]:border-r-transparent",
      "data-[orientation=vertical]:data-[state=active]:border-r-0 data-[orientation=vertical]:data-[state=active]:border-b-border",
      // Accent line on left for vertical active tabs
      "data-[orientation=vertical]:data-[state=active]:before:absolute data-[orientation=vertical]:data-[state=active]:before:top-0 data-[orientation=vertical]:data-[state=active]:before:bottom-0 data-[orientation=vertical]:data-[state=active]:before:left-0 data-[orientation=vertical]:data-[state=active]:before:w-[2px] data-[orientation=vertical]:data-[state=active]:before:bg-primary data-[orientation=vertical]:data-[state=active]:before:rounded-l-md",
      
      // Inactive state
      "data-[state=inactive]:border-transparent data-[state=inactive]:bg-transparent data-[state=inactive]:text-muted-foreground",
      
      // Hover state
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
      // Base styling
      "ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
      // Connected background that flows seamlessly from the selected tab
      "bg-card border-l border-r border-b border-border",
      // No top border to connect with TabsList border
      "border-t-0",
      // Rounded corners only on bottom
      "rounded-b-md",
      // Padding for content
      "p-6",
      className
    )}
    {...props}
  />
))
TabsContent.displayName = TabsPrimitive.Content.displayName

export { Tabs, TabsList, TabsTrigger, TabsContent }