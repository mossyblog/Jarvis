import { useState } from 'react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { 
  Home, 
  HelpCircle, 
  Bell,
  Slash,
  ExternalLink,
  MessageCircle,
  FileText,
  Mail
} from 'lucide-react'
import { OrganizationDropdown } from './dropdowns/OrganizationDropdown'
import { UserMenu } from './UserMenu'
import { ThemeSwitcher } from '@/components/ui/theme-switcher'
import { Switch } from '@/components/ui/switch'
import { useEditMode } from '@/contexts/EditModeContext'
import { DeviceSelector } from './DeviceSelector'

// Component implementations
const HomeIcon = () => (
  <a href="/" className="flex items-center">
    <Home size={14} strokeWidth={1.5} className="text-foreground-lighter" />
  </a>
)


const NotificationsPopover = () => {
  const hasNotifications = true; // Demo state
  const notificationCount = 3; // Demo count

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button 
          variant="ghost" 
          size="sm"
          title={`Notifications ${hasNotifications ? `(${notificationCount})` : ''}`}
          className={cn(
            "relative h-8 w-8 p-0 rounded-md transition-all duration-200",
            "hover:bg-accent hover:shadow-sm",
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
            "active:scale-95 active:transition-none",
            "disabled:opacity-50 disabled:pointer-events-none",
            hasNotifications && "animate-pulse-subtle"
          )}
        >
          <Bell className="icon-sm text-muted-foreground transition-colors group-hover:text-foreground" />
          {hasNotifications && (
            <>
              {/* Notification badge */}
              <span className={cn(
                "absolute -top-1 -right-1 flex items-center justify-center",
                "min-w-[18px] h-[18px] px-1",
                "bg-destructive text-destructive-foreground",
                "text-xs font-semibold leading-none",
                "rounded-full border-2 border-background",
                "animate-in zoom-in-50 duration-200",
                "shadow-sm"
              )}>
                {notificationCount > 99 ? '99+' : notificationCount}
              </span>
              {/* Pulse ring for new notifications */}
              <span className={cn(
                "absolute -top-1 -right-1 w-[18px] h-[18px]",
                "bg-destructive rounded-full",
                "animate-pulse-subtle opacity-50"
              )} />
            </>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-80 animate-in slide-in-from-top-2 duration-200">
        <DropdownMenuLabel className="flex items-center justify-between px-lg py-sm">
          <span>Notifications</span>
          {hasNotifications && (
            <span className="text-xs font-normal text-muted-foreground">
              {notificationCount} new
            </span>
          )}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {hasNotifications ? (
          <div className="max-h-96 overflow-y-auto overflow-x-hidden">
            {/* Demo notifications */}
            <DropdownMenuItem className="p-lg focus:bg-accent cursor-pointer">
              <div className="flex items-start gap-sm w-full">
                <div className="w-2 h-2 bg-blue-500 rounded-full mt-2 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">System Update Available</p>
                  <p className="text-xs text-muted-foreground line-clamp-2">
                    A new version of the system is ready to install.
                  </p>
                  <p className="text-xs text-muted-foreground mt-1">2 minutes ago</p>
                </div>
              </div>
            </DropdownMenuItem>
            <DropdownMenuItem className="p-lg focus:bg-accent cursor-pointer">
              <div className="flex items-start gap-sm w-full">
                <div className="w-2 h-2 bg-green-500 rounded-full mt-2 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">Deployment Successful</p>
                  <p className="text-xs text-muted-foreground line-clamp-2">
                    Your latest changes have been deployed successfully.
                  </p>
                  <p className="text-xs text-muted-foreground mt-1">1 hour ago</p>
                </div>
              </div>
            </DropdownMenuItem>
            <DropdownMenuItem className="p-lg focus:bg-accent cursor-pointer">
              <div className="flex items-start gap-sm w-full">
                <div className="w-2 h-2 bg-amber-500 rounded-full mt-2 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">Storage Warning</p>
                  <p className="text-xs text-muted-foreground line-clamp-2">
                    Your storage is 85% full. Consider upgrading your plan.
                  </p>
                  <p className="text-xs text-muted-foreground mt-1">3 hours ago</p>
                </div>
              </div>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem className="p-md text-center text-sm text-primary hover:text-primary/80 cursor-pointer">
              View all notifications
            </DropdownMenuItem>
          </div>
        ) : (
          <div className="p-lg text-center">
            <Bell className="icon-lg text-muted-foreground mx-auto mb-xs" />
            <p className="text-sm text-muted-foreground">
              No new notifications
            </p>
            <p className="text-xs text-muted-foreground mt-1">
              We'll notify you when something happens
            </p>
          </div>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

const HelpPopover = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button 
        variant="ghost" 
        size="sm"
        title="Help & Support"
        className={cn(
          "h-8 w-8 p-0 rounded-md transition-all duration-200",
          "hover:bg-accent hover:shadow-sm",
          "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
          "active:scale-95 active:transition-none",
          "disabled:opacity-50 disabled:pointer-events-none"
        )}
      >
        <HelpCircle className="icon-sm text-muted-foreground transition-colors group-hover:text-foreground" />
      </Button>
    </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56 animate-in slide-in-from-top-2 duration-200">
        <DropdownMenuLabel className="px-lg py-sm">
          Help & Support
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem className="px-lg py-sm cursor-pointer focus:bg-accent">
          <FileText className="icon-xs mr-2 text-muted-foreground" />
          <span>Documentation</span>
          <ExternalLink className="icon-xs ml-auto text-muted-foreground" />
        </DropdownMenuItem>
        <DropdownMenuItem className="px-lg py-sm cursor-pointer focus:bg-accent">
          <FileText className="icon-xs mr-2 text-muted-foreground" />
          <span>API Reference</span>
          <ExternalLink className="icon-xs ml-auto text-muted-foreground" />
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem className="px-lg py-sm cursor-pointer focus:bg-accent">
          <MessageCircle className="icon-xs mr-2 text-muted-foreground" />
          <span>Community Forum</span>
          <ExternalLink className="icon-xs ml-auto text-muted-foreground" />
        </DropdownMenuItem>
        <DropdownMenuItem className="px-lg py-sm cursor-pointer focus:bg-accent">
          <Mail className="icon-xs mr-2 text-muted-foreground" />
          <span>Contact Support</span>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <div className="px-lg py-sm">
          <p className="text-xs text-muted-foreground mb-1">Keyboard Shortcuts</p>
          <div className="flex items-center justify-between text-xs">
            <span className="text-muted-foreground">Open Help</span>
            <kbd className="px-1 py-0.5 bg-muted rounded text-muted-foreground font-mono text-xs">?</kbd>
          </div>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  )

// Replaced with UserMenu component

const EditModeToggle = () => {
  const { isEditMode, toggleEditMode, hasUnsavedChanges } = useEditMode();

  return (
    <div className="flex items-center gap-3">
      {hasUnsavedChanges && (
        <span className="text-xs text-amber-500 font-medium">
          Unsaved
        </span>
      )}
      <label htmlFor="edit-mode-switch" className="flex items-center gap-2 cursor-pointer">
        <span className="text-sm font-medium">Edit</span>
        <Switch
          id="edit-mode-switch"
          checked={isEditMode}
          onCheckedChange={toggleEditMode}
          className="data-[state=checked]:bg-blue-600"
        />
      </label>
    </div>
  );
};

const LayoutHeaderDivider = ({ className, ...props }: React.HTMLProps<HTMLSpanElement>) => (
  <span className={cn('px-2', className)} {...props}>
    <Slash size={16} strokeWidth={1} className="text-muted-foreground/40" />
  </span>
)

interface LayoutHeaderProps {
  customHeaderComponents?: ReactNode
  showProductMenu?: boolean
}


const LayoutHeader = ({
  customHeaderComponents,
  showProductMenu,
}: LayoutHeaderProps) => {
  const [, setMobileMenuOpen] = useState(false)

  return (
    <header className={cn('flex h-12 items-center flex-shrink-0 border-b bg-background overflow-hidden')}>
      {showProductMenu && (
        <div className="flex items-center justify-center border-r flex-0 md:hidden h-full aspect-square">
          <button
            title="Menu dropdown button"
            className={cn(
              'group/view-toggle flex justify-center flex-col border-none space-x-0 items-center gap-1 !bg-transparent rounded-md w-12 h-12'
            )}
            onClick={() => setMobileMenuOpen(true)}
          >
            <div className="h-px w-4 transition-all ease-out bg-foreground-lighter group-hover/view-toggle:bg-foreground" />
            <div className="h-px w-3 transition-all ease-out bg-foreground-lighter group-hover/view-toggle:bg-foreground" />
          </button>
        </div>
      )}
      <div
        className={cn(
          'flex items-center justify-between h-full px-3 flex-1 overflow-hidden'
        )}
      >
        <div className="flex items-center text-sm gap-1 min-w-0 flex-shrink-0 no-horizontal-scroll">
          <HomeIcon />
          <LayoutHeaderDivider />
          <OrganizationDropdown />
        </div>
        
        <div className="flex items-center gap-sm min-w-0 flex-shrink-0 flex-no-overflow">
          {customHeaderComponents && customHeaderComponents}
          <>
            <ThemeSwitcher />
            <LayoutHeaderDivider />
            <DeviceSelector />
            <LayoutHeaderDivider />
            <div className="flex items-center gap-xs">
              <NotificationsPopover />
              <HelpPopover />
            </div>
            <LayoutHeaderDivider />
            <UserMenu />
            <LayoutHeaderDivider />
            <EditModeToggle />
          </>
        </div>
      </div>
    </header>
  )
}

export default LayoutHeader
