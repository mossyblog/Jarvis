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
  Bell
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


const NotificationsPopoverV2 = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button variant="ghost" size="icon" className="relative">
        <Bell className="h-4 w-4" />
        <span className="absolute top-0 right-0 h-2 w-2 bg-red-500 rounded-full" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" className="w-80">
      <DropdownMenuLabel>Notifications</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <div className="p-4 text-sm text-muted-foreground">
        No new notifications
      </div>
    </DropdownMenuContent>
  </DropdownMenu>
)

const HelpPopover = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button variant="ghost" size="icon">
        <HelpCircle className="h-4 w-4" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end">
      <DropdownMenuLabel>Help & Support</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuItem>Documentation</DropdownMenuItem>
      <DropdownMenuItem>API Reference</DropdownMenuItem>
      <DropdownMenuItem>Contact Support</DropdownMenuItem>
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
    <svg
      viewBox="0 0 24 24"
      width="16"
      height="16"
      stroke="currentColor"
      strokeWidth="1"
      strokeLinecap="round"
      strokeLinejoin="round"
      fill="none"
      shapeRendering="geometricPrecision"
      className="text-muted-foreground/40"
    >
      <path d="M16 3.549L7.12 20.600" />
    </svg>
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
    <header className={cn('flex h-12 items-center flex-shrink-0 border-b bg-background')}>
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
          'flex items-center justify-between h-full px-3 flex-1 overflow-x-auto'
        )}
      >
        <div className="flex items-center text-sm gap-1">
          <HomeIcon />
          <LayoutHeaderDivider />
          <OrganizationDropdown />
        </div>
        
        <div className="flex items-center gap-x-1">
          {customHeaderComponents && customHeaderComponents}
          <>
            <ThemeSwitcher />
            <LayoutHeaderDivider />
            <DeviceSelector />
            <LayoutHeaderDivider />
            <NotificationsPopoverV2 />
            <HelpPopover />
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
