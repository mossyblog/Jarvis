import { useState, useEffect } from 'react'
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
  Mail,
  Settings,
  Menu,
  X,
  Wifi,
  WifiOff,
  Activity,
  User,
  Zap
} from 'lucide-react'
import { OrganizationDropdown } from './dropdowns/OrganizationDropdown'
import { UserMenu } from './UserMenu'
import { ThemeSwitcher } from '@/components/ui/theme-switcher'
import { Switch } from '@/components/ui/switch'
import { useEditMode } from '@/contexts/EditModeContext'
import { useAuth } from '@/contexts/AuthContext'
import { DeviceSelector } from './DeviceSelector'
import { Separator } from '@/components/ui/separator'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'

// Component implementations
const HomeIcon = () => (
  <a href="/" className="flex items-center">
    <Home size={14} strokeWidth={1.5} className="text-foreground-lighter" />
  </a>
)


const ConnectionStatus = () => {
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [networkQuality, setNetworkQuality] = useState<'good' | 'poor' | 'offline'>('good');

  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true);
      setNetworkQuality('good');
    };
    const handleOffline = () => {
      setIsOnline(false);
      setNetworkQuality('offline');
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const getStatusIcon = () => {
    if (!isOnline) return <WifiOff className="h-4 w-4 text-destructive" />;
    if (networkQuality === 'poor') return <Wifi className="h-4 w-4 text-warning" />;
    return <Wifi className="h-4 w-4 text-brand" />;
  };

  const getStatusText = () => {
    if (!isOnline) return 'Offline';
    if (networkQuality === 'poor') return 'Poor connection';
    return 'Connected';
  };

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button 
            variant="ghost" 
            size="sm"
            className="h-8 w-8 p-0 rounded-md"
            aria-label={`Network status: ${getStatusText()}`}
          >
            {getStatusIcon()}
          </Button>
        </TooltipTrigger>
        <TooltipContent>
          <p>{getStatusText()}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
};

const NotificationsPopover = () => {
  const [notifications, setNotifications] = useState([
    {
      id: '1',
      type: 'info' as const,
      title: 'System Update Available',
      message: 'A new version of the system is ready to install.',
      timestamp: new Date(Date.now() - 2 * 60 * 1000),
      read: false
    },
    {
      id: '2',
      type: 'success' as const,
      title: 'Deployment Successful',
      message: 'Your latest changes have been deployed successfully.',
      timestamp: new Date(Date.now() - 60 * 60 * 1000),
      read: false
    },
    {
      id: '3',
      type: 'warning' as const,
      title: 'Storage Warning',
      message: 'Your storage is 85% full. Consider upgrading your plan.',
      timestamp: new Date(Date.now() - 3 * 60 * 60 * 1000),
      read: true
    }
  ]);

  const unreadCount = notifications.filter(n => !n.read).length;
  const hasNotifications = notifications.length > 0;

  const markAsRead = (id: string) => {
    setNotifications(prev => 
      prev.map(n => n.id === id ? { ...n, read: true } : n)
    );
  };

  const markAllAsRead = () => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })));
  };

  const getNotificationIcon = (type: 'info' | 'success' | 'warning' | 'error') => {
    const colors = {
      info: 'bg-blue-500',
      success: 'bg-green-500',
      warning: 'bg-amber-500',
      error: 'bg-red-500'
    };
    return colors[type];
  };

  const formatTimeAgo = (date: Date) => {
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
    
    if (diffInMinutes < 60) {
      return `${diffInMinutes} minute${diffInMinutes !== 1 ? 's' : ''} ago`;
    }
    
    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) {
      return `${diffInHours} hour${diffInHours !== 1 ? 's' : ''} ago`;
    }
    
    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays} day${diffInDays !== 1 ? 's' : ''} ago`;
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button 
          variant="ghost" 
          size="sm"
          className={cn(
            "relative h-8 w-8 p-0 rounded-md transition-all duration-200",
            "hover:bg-accent hover:shadow-sm",
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
            "active:scale-95 active:transition-none",
            "disabled:opacity-50 disabled:pointer-events-none"
          )}
          aria-label={`Notifications ${unreadCount > 0 ? `(${unreadCount} unread)` : ''}`}
        >
          <Bell className="h-4 w-4 text-muted-foreground transition-colors group-hover:text-foreground" />
          {unreadCount > 0 && (
            <>
              <Badge 
                variant="destructive" 
                className={cn(
                  "absolute -top-2 -right-2 flex items-center justify-center",
                  "min-w-[18px] h-[18px] px-1 text-xs font-semibold",
                  "animate-in zoom-in-50 duration-200"
                )}
              >
                {unreadCount > 99 ? '99+' : unreadCount}
              </Badge>
            </>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-80 max-h-96 overflow-hidden">
        <DropdownMenuLabel className="flex items-center justify-between px-4 py-2">
          <span>Notifications</span>
          <div className="flex items-center gap-2">
            {unreadCount > 0 && (
              <Badge variant="secondary" className="text-xs">
                {unreadCount} new
              </Badge>
            )}
            {unreadCount > 0 && (
              <Button 
                variant="ghost" 
                size="sm" 
                onClick={markAllAsRead}
                className="h-6 px-2 text-xs"
              >
                Mark all read
              </Button>
            )}
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {hasNotifications ? (
          <div className="max-h-80 overflow-y-auto">
            {notifications.map((notification) => (
              <DropdownMenuItem 
                key={notification.id}
                className={cn(
                  "p-4 cursor-pointer focus:bg-accent",
                  !notification.read && "bg-muted/50"
                )}
                onClick={() => markAsRead(notification.id)}
              >
                <div className="flex items-start gap-3 w-full">
                  <div 
                    className={cn(
                      "w-2 h-2 rounded-full mt-2 flex-shrink-0",
                      getNotificationIcon(notification.type)
                    )}
                  />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <p className={cn(
                        "text-sm truncate",
                        !notification.read && "font-medium"
                      )}>
                        {notification.title}
                      </p>
                      {!notification.read && (
                        <div className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0 mt-2" />
                      )}
                    </div>
                    <p className="text-xs text-muted-foreground line-clamp-2 mt-1">
                      {notification.message}
                    </p>
                    <p className="text-xs text-muted-foreground mt-2">
                      {formatTimeAgo(notification.timestamp)}
                    </p>
                  </div>
                </div>
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuItem className="p-3 text-center text-sm text-primary hover:text-primary/80 cursor-pointer">
              View all notifications
            </DropdownMenuItem>
          </div>
        ) : (
          <div className="p-6 text-center">
            <Bell className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
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

const QuickActionsPopover = () => {
  const { user } = useAuth();
  const shortcuts = [
    { key: '/', description: 'Open command palette' },
    { key: '?', description: 'Show help' },
    { key: 'E', description: 'Toggle edit mode' },
    { key: 'N', description: 'New page' },
    { key: 'S', description: 'Save changes' }
  ];

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button 
          variant="ghost" 
          size="sm"
          className={cn(
            "h-8 w-8 p-0 rounded-md transition-all duration-200",
            "hover:bg-accent hover:shadow-sm",
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
            "active:scale-95 active:transition-none",
            "disabled:opacity-50 disabled:pointer-events-none"
          )}
          aria-label="Quick actions and help"
        >
          <Zap className="h-sm w-sm text-muted-foreground transition-colors group-hover:text-foreground" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuLabel className="px-4 py-2">
          Quick Actions
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <FileText className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>Documentation</span>
          <ExternalLink className="h-4 w-4 ml-auto text-muted-foreground" />
        </DropdownMenuItem>
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <Activity className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>API Reference</span>
          <ExternalLink className="h-4 w-4 ml-auto text-muted-foreground" />
        </DropdownMenuItem>
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <MessageCircle className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>Community Forum</span>
          <ExternalLink className="h-4 w-4 ml-auto text-muted-foreground" />
        </DropdownMenuItem>
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <Mail className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>Contact Support</span>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <div className="px-4 py-2">
          <p className="text-xs font-medium text-muted-foreground mb-2">Keyboard Shortcuts</p>
          <div className="space-y-1">
            {shortcuts.map((shortcut) => (
              <div key={shortcut.key} className="flex items-center justify-between text-xs">
                <span className="text-muted-foreground">{shortcut.description}</span>
                <kbd className="px-1.5 py-0.5 bg-muted rounded text-muted-foreground font-mono text-xs">
                  {shortcut.key}
                </kbd>
              </div>
            ))}
          </div>
        </div>
        {user && (
          <>
            <DropdownMenuSeparator />
            <div className="px-4 py-2">
              <p className="text-xs font-medium text-muted-foreground mb-1">User Info</p>
              <div className="text-xs text-muted-foreground">
                <p>Role: {user.roles.map(r => r.name).join(', ')}</p>
                <p>ID: {user.id.slice(0, 8)}...</p>
              </div>
            </div>
          </>
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
        className={cn(
          "h-8 w-8 p-0 rounded-md transition-all duration-200",
          "hover:bg-accent hover:shadow-sm",
          "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
          "active:scale-95 active:transition-none",
          "disabled:opacity-50 disabled:pointer-events-none"
        )}
        aria-label="Help and support"
      >
        <HelpCircle className="h-sm w-sm text-muted-foreground transition-colors group-hover:text-foreground" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" className="w-56">
      <DropdownMenuLabel className="px-4 py-2">
        Help & Support
      </DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
        <FileText className="h-4 w-4 mr-2 text-muted-foreground" />
        <span>Documentation</span>
        <ExternalLink className="h-4 w-4 ml-auto text-muted-foreground" />
      </DropdownMenuItem>
      <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
        <FileText className="h-4 w-4 mr-2 text-muted-foreground" />
        <span>API Reference</span>
        <ExternalLink className="h-4 w-4 ml-auto text-muted-foreground" />
      </DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
        <MessageCircle className="h-4 w-4 mr-2 text-muted-foreground" />
        <span>Community Forum</span>
        <ExternalLink className="h-4 w-4 ml-auto text-muted-foreground" />
      </DropdownMenuItem>
      <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
        <Mail className="h-4 w-4 mr-2 text-muted-foreground" />
        <span>Contact Support</span>
      </DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
)

// Replaced with UserMenu component

const SettingsPopover = () => {
  const { user } = useAuth();
  
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button 
          variant="ghost" 
          size="sm"
          className={cn(
            "h-8 w-8 p-0 rounded-md transition-all duration-200",
            "hover:bg-accent hover:shadow-sm",
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
            "active:scale-95 active:transition-none",
            "disabled:opacity-50 disabled:pointer-events-none"
          )}
          aria-label="Settings and preferences"
        >
          <Settings className="h-sm w-sm text-muted-foreground transition-colors group-hover:text-foreground" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="px-4 py-2">
          Settings
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <User className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>Profile</span>
        </DropdownMenuItem>
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <Settings className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>Preferences</span>
        </DropdownMenuItem>
        <DropdownMenuItem className="px-4 py-2 cursor-pointer focus:bg-accent">
          <Bell className="h-4 w-4 mr-2 text-muted-foreground" />
          <span>Notifications</span>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <div className="px-4 py-2">
          <p className="text-xs font-medium text-muted-foreground mb-2">Account</p>
          <div className="text-xs text-muted-foreground space-y-1">
            <p>{user?.name}</p>
            <p>{user?.email}</p>
            <p className="text-xs">ID: {user?.id.slice(0, 8)}...</p>
          </div>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

const EditModeToggle = () => {
  const { isEditMode, toggleEditMode, hasUnsavedChanges } = useEditMode();

  return (
    <div className="flex items-center gap-2">
      {hasUnsavedChanges && (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Badge variant="destructive" className="text-xs px-2 py-0.5 animate-pulse">
                Unsaved
              </Badge>
            </TooltipTrigger>
            <TooltipContent>
              <p>You have unsaved changes</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )}
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <label 
              htmlFor="edit-mode-switch" 
              className="flex items-center gap-2 cursor-pointer select-none"
            >
              <span className="text-sm font-medium text-muted-foreground">
                Edit
              </span>
              <Switch
                id="edit-mode-switch"
                checked={isEditMode}
                onCheckedChange={toggleEditMode}
                className="data-[state=checked]:bg-blue-600"
                aria-label="Toggle edit mode"
              />
            </label>
          </TooltipTrigger>
          <TooltipContent>
            <p>{isEditMode ? 'Exit edit mode' : 'Enter edit mode'}</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </div>
  );
};

const LayoutHeaderDivider = ({ className, ...props }: React.HTMLProps<HTMLDivElement>) => (
  <Separator orientation="vertical" className={cn('h-4 mx-2', className)} {...props} />
)

const MobileMenuButton = ({ onClick }: { onClick: () => void }) => (
  <Button
    variant="ghost"
    size="sm"
    onClick={onClick}
    className={cn(
      "h-8 w-8 p-0 rounded-md transition-all duration-200 md:hidden",
      "hover:bg-accent hover:shadow-sm",
      "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
      "active:scale-95 active:transition-none"
    )}
    aria-label="Open mobile menu"
  >
    <Menu className="h-sm w-sm" />
  </Button>
)

interface LayoutHeaderProps {
  customHeaderComponents?: ReactNode
  showProductMenu?: boolean
  className?: string
}

const LayoutHeader = ({
  customHeaderComponents,
  showProductMenu = true,
  className
}: LayoutHeaderProps) => {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const { user, isAuthenticated } = useAuth()

  // Enhanced keyboard navigation with context
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Skip if user is typing in an input
      const target = e.target as HTMLElement
      const isInput = target.tagName === 'INPUT' || 
                     target.tagName === 'TEXTAREA' || 
                     target.contentEditable === 'true'
      
      if (isInput) return

      // Ctrl/Cmd + K for command palette
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        console.log('🎹 Open command palette')
        // TODO: Implement command palette
      }
      
      // ? for help
      if (e.key === '?' && !e.ctrlKey && !e.metaKey && !e.altKey) {
        e.preventDefault()
        console.log('🎹 Open keyboard shortcuts help')
        // TODO: Open help modal with keyboard shortcuts
      }
      
      // Alt + M for mobile menu toggle
      if (e.altKey && e.key === 'm') {
        e.preventDefault()
        setMobileMenuOpen(prev => !prev)
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [])

  return (
    <header 
      className={cn(
        'flex h-12 items-center flex-shrink-0 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60',
        'sticky top-0 z-50 w-full',
        className
      )}
      role="banner"
    >
      {/* Mobile menu button */}
      {showProductMenu && (
        <div className="flex items-center justify-center border-r h-full w-12 md:hidden">
          <MobileMenuButton onClick={() => setMobileMenuOpen(true)} />
        </div>
      )}
      
      {/* Main header content */}
      <div className="flex items-center justify-between h-full px-3 flex-1 overflow-hidden min-w-0">
        {/* Left section - Navigation */}
        <div className="flex items-center gap-1 min-w-0 flex-shrink-0">
          <HomeIcon />
          <LayoutHeaderDivider />
          <OrganizationDropdown />
        </div>
        
        {/* Center section - Custom components */}
        {customHeaderComponents && (
          <div className="flex items-center justify-center flex-1 min-w-0 px-4">
            {customHeaderComponents}
          </div>
        )}
        
        {/* Right section - Actions and user */}
        <div className="flex items-center gap-1 min-w-0 flex-shrink-0">
          {/* Connection status - only on larger screens */}
          <div className="hidden lg:block">
            <ConnectionStatus />
          </div>
          
          {/* Theme switcher */}
          <ThemeSwitcher />
          
          {/* Device selector - only show in edit mode */}
          <div className="hidden sm:block">
            <DeviceSelector />
          </div>
          
          <LayoutHeaderDivider className="hidden sm:block" />
          
          {/* Notifications */}
          {isAuthenticated && (
            <NotificationsPopover />
          )}
          
          {/* Quick actions */}
          <QuickActionsPopover />
          
          {/* Help */}
          <HelpPopover />
          
          {/* Settings - only show for authenticated users */}
          {isAuthenticated && (
            <SettingsPopover />
          )}
          
          <LayoutHeaderDivider />
          
          {/* User menu */}
          {isAuthenticated ? (
            <UserMenu />
          ) : (
            <Button variant="outline" size="sm" className="h-8">
              Sign In
            </Button>
          )}
          
          {/* Edit mode toggle - only for authenticated users */}
          {isAuthenticated && (
            <>
              <LayoutHeaderDivider className="hidden sm:block" />
              <div className="hidden sm:block">
                <EditModeToggle />
              </div>
            </>
          )}
        </div>
      </div>
      
      {/* Mobile menu overlay */}
      {mobileMenuOpen && (
        <div 
          className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 md:hidden"
          onClick={() => setMobileMenuOpen(false)}
        >
          <div 
            className="fixed top-0 left-0 h-full w-64 bg-background border-r shadow-lg"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between p-4 border-b">
              <h2 className="text-lg font-semibold">Menu</h2>
              <Button 
                variant="ghost" 
                size="sm"
                onClick={() => setMobileMenuOpen(false)}
                className="h-8 w-8 p-0"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <div className="p-4 space-y-4">
              {/* Mobile-specific navigation items can go here */}
              <div className="space-y-2">
                <EditModeToggle />
                <DeviceSelector />
              </div>
            </div>
          </div>
        </div>
      )}
    </header>
  )
}

export default LayoutHeader
