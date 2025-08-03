/**
 * UIStudioHeader Component
 * 
 * Dedicated header component for UIStudio interface with user context and actions.
 * Provides responsive design, accessibility features, and comprehensive user actions.
 * 
 * Features:
 * - User profile and context display
 * - Quick action buttons (Create Page, Templates, etc.)
 * - Responsive design (mobile-first approach)
 * - Keyboard navigation support
 * - ARIA landmarks for accessibility
 * - Mobile menu integration
 * 
 * @module UIStudioHeader
 */

import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { cn } from '../../lib/utils';

// UI Components
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '../ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '../ui/tooltip';
import { Separator } from '../ui/separator';

// Icons
import {
  Menu,
  Plus,
  Layers,
  Search,
  Bell,
  Settings,
  User,
  LogOut,
  ChevronDown,
  Home,
  HelpCircle,
  Activity,
  Users,
  BarChart3,
  Star
} from 'lucide-react';

// Contexts
import { useAuth } from '../../contexts/AuthContext';
import { useEditMode } from '../../contexts/EditModeContext';

// Types
import type { UIStudioEntityId } from '../../types/uistudio';

// ============================================================================
// Component Props Interface
// ============================================================================

export interface UIStudioHeaderProps {
  /** Current user entity ID */
  userEntityId: UIStudioEntityId;
  
  /** Callback to open mobile sidebar */
  onOpenMobileSidebar?: () => void;
  
  /** Callback to create new page */
  onCreatePage?: () => void;
  
  /** Callback to open template gallery */
  onOpenTemplates?: () => void;
  
  /** Callback to open search */
  onOpenSearch?: () => void;
  
  /** Optional custom CSS classes */
  className?: string;
  
  /** Show or hide specific action buttons */
  showActions?: {
    createPage?: boolean;
    templates?: boolean;
    search?: boolean;
    notifications?: boolean;
    settings?: boolean;
    help?: boolean;
  };
  
  /** Header title override */
  title?: string;
  
  /** Header subtitle override */
  subtitle?: string;
  
  /** Show project/workspace selector */
  showProjectSelector?: boolean;
}

// ============================================================================
// Notification Interface (mock data for demo)
// ============================================================================

interface Notification {
  id: string;
  type: 'info' | 'success' | 'warning' | 'error';
  title: string;
  message: string;
  timestamp: Date;
  read: boolean;
  actionUrl?: string;
}

// ============================================================================
// Component Implementation
// ============================================================================

export const UIStudioHeader: React.FC<UIStudioHeaderProps> = ({
  onOpenMobileSidebar,
  onCreatePage,
  onOpenTemplates,
  onOpenSearch,
  className,
  showActions = {
    createPage: true,
    templates: true,
    search: true,
    notifications: true,
    settings: true,
    help: true,
  },
  title = "UIStudio",
  subtitle = "Dashboard",
  showProjectSelector = false,
}) => {
  const navigate = useNavigate();
  const { user, isAuthenticated, logout } = useAuth();
  const { isEditMode, toggleEditMode, hasUnsavedChanges } = useEditMode();
  
  // State for notifications (mock data for demo)
  const [notifications, setNotifications] = useState<Notification[]>([
    {
      id: '1',
      type: 'info',
      title: 'System Update Available',
      message: 'A new version of UIStudio is available with enhanced performance.',
      timestamp: new Date(Date.now() - 5 * 60 * 1000), // 5 minutes ago
      read: false,
    },
    {
      id: '2',
      type: 'success',
      title: 'Page Published',
      message: 'Your "Landing Page" has been successfully published.',
      timestamp: new Date(Date.now() - 30 * 60 * 1000), // 30 minutes ago
      read: false,
    },
    {
      id: '3',
      type: 'warning',
      title: 'Storage Warning',
      message: 'You are approaching your storage limit. Consider upgrading.',
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
      read: true,
    },
  ]);

  const unreadNotifications = notifications.filter(n => !n.read);

  // ============================================================================
  // Event Handlers
  // ============================================================================

  const handleCreatePage = useCallback(() => {
    onCreatePage?.();
  }, [onCreatePage]);

  const handleOpenTemplates = useCallback(() => {
    onOpenTemplates?.();
  }, [onOpenTemplates]);

  const handleOpenSearch = useCallback(() => {
    onOpenSearch?.();
  }, [onOpenSearch]);

  const handleMobileMenuToggle = () => {
    onOpenMobileSidebar?.();
  };

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/login');
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  const handleNotificationClick = (notification: Notification) => {
    // Mark as read
    setNotifications(prev => 
      prev.map(n => n.id === notification.id ? { ...n, read: true } : n)
    );
    
    // Navigate to action URL if available
    if (notification.actionUrl) {
      navigate(notification.actionUrl);
    }
  };

  const markAllNotificationsRead = () => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })));
  };

  // ============================================================================
  // User Info Helpers
  // ============================================================================

  const getUserInitials = (name: string): string => {
    return name
      .split(' ')
      .map(part => part.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const getUserRole = (): string => {
    if (!user?.roles?.length) return 'User';
    return user.roles.map(role => role.name).join(', ');
  };

  // ============================================================================
  // Keyboard Navigation
  // ============================================================================

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Skip if user is typing in an input
      const target = e.target as HTMLElement;
      const isInput = target.tagName === 'INPUT' || 
                     target.tagName === 'TEXTAREA' || 
                     target.contentEditable === 'true';
      
      if (isInput) return;

      // Global shortcuts
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        handleOpenSearch();
      }
      
      if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        handleCreatePage();
      }
      
      if (e.key === '?' && !e.ctrlKey && !e.metaKey && !e.altKey) {
        e.preventDefault();
        // TODO: Open help modal
        console.log('🎹 Help shortcut triggered');
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleCreatePage, handleOpenSearch]);

  // ============================================================================
  // Render Component
  // ============================================================================

  return (
    <header 
      className={cn(
        'border-b bg-card/50 backdrop-blur-sm sticky top-0 z-40',
        'flex h-16 items-center justify-between px-4 sm:px-6',
        'transition-all duration-200',
        className
      )}
      role="banner"
      aria-label="UIStudio navigation header"
    >
      {/* Left Section - Mobile Menu + Logo/Title */}
      <div className="flex items-center space-x-3 min-w-0">
        {/* Mobile Menu Button */}
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                className="lg:hidden h-9 w-9 p-0"
                onClick={handleMobileMenuToggle}
                aria-label="Open navigation menu"
              >
                <Menu className="h-5 w-5" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="bottom">
              <p>Open menu (Alt+M)</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>

        {/* Logo and Title */}
        <div className="flex items-center space-x-2 min-w-0">
          <Button
            variant="ghost"
            size="sm"
            className="h-9 w-9 p-0 hidden lg:flex"
            onClick={() => navigate('/studio')}
            aria-label="Go to UIStudio dashboard"
          >
            <Home className="h-5 w-5" />
          </Button>
          
          <div className="flex flex-col min-w-0">
            <h1 className="text-lg font-bold sm:text-xl truncate">
              {title}
            </h1>
            <p className="text-xs text-muted-foreground sm:text-sm lg:hidden truncate">
              {subtitle}
            </p>
          </div>
        </div>

        {/* Project Selector (if enabled) */}
        {showProjectSelector && (
          <>
            <Separator orientation="vertical" className="h-4" />
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="hidden sm:flex">
                  <span className="text-sm">Current Project</span>
                  <ChevronDown className="h-4 w-4 ml-1" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start">
                <DropdownMenuLabel>Switch Project</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  <Star className="h-4 w-4 mr-2" />
                  Personal Workspace
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <Users className="h-4 w-4 mr-2" />
                  Team Project
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </>
        )}
      </div>

      {/* Center Section - Status Indicators */}
      <div className="hidden lg:flex items-center space-x-2">
        {/* Edit Mode Toggle */}
        {isAuthenticated && (
          <div className="flex items-center space-x-2">
            {hasUnsavedChanges && (
              <Badge variant="destructive" className="text-xs animate-pulse">
                Unsaved
              </Badge>
            )}
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant={isEditMode ? "default" : "outline"}
                    size="sm"
                    onClick={toggleEditMode}
                    className="h-8 px-3"
                  >
                    <Activity className="h-4 w-4 mr-1" />
                    <span className="text-xs">
                      {isEditMode ? 'Editing' : 'Edit Mode'}
                    </span>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <p>{isEditMode ? 'Exit edit mode' : 'Enter edit mode'} (E)</p>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </div>
        )}
      </div>

      {/* Right Section - Actions and User */}
      <div className="flex items-center space-x-1 sm:space-x-2">
        {/* Search Button */}
        {showActions.search && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-9 w-9 p-0 hidden sm:flex"
                  onClick={handleOpenSearch}
                  aria-label="Search pages and content"
                >
                  <Search className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Search (Ctrl+K)</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        {/* Create Page Button */}
        {showActions.createPage && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  size="sm"
                  onClick={handleCreatePage}
                  className="hidden sm:flex h-8"
                  aria-label="Create new page"
                >
                  <Plus className="h-4 w-4 mr-1" />
                  <span className="hidden md:inline">Create</span>
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Create new page (Ctrl+N)</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        {/* Templates Button */}
        {showActions.templates && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleOpenTemplates}
                  className="hidden sm:flex h-8"
                  aria-label="Browse templates"
                >
                  <Layers className="h-4 w-4 mr-1" />
                  <span className="hidden lg:inline">Templates</span>
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Browse templates</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        {/* Mobile Actions Dropdown */}
        <div className="sm:hidden">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-9 w-9 p-0">
                <Plus className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>Quick Actions</DropdownMenuLabel>
              <DropdownMenuSeparator />
              {showActions.createPage && (
                <DropdownMenuItem onClick={handleCreatePage}>
                  <Plus className="h-4 w-4 mr-2" />
                  Create Page
                </DropdownMenuItem>
              )}
              {showActions.templates && (
                <DropdownMenuItem onClick={handleOpenTemplates}>
                  <Layers className="h-4 w-4 mr-2" />
                  Templates
                </DropdownMenuItem>
              )}
              {showActions.search && (
                <DropdownMenuItem onClick={handleOpenSearch}>
                  <Search className="h-4 w-4 mr-2" />
                  Search
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <Separator orientation="vertical" className="h-4 hidden sm:block" />

        {/* Notifications */}
        {showActions.notifications && isAuthenticated && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-9 w-9 p-0 relative">
                <Bell className="h-4 w-4" />
                {unreadNotifications.length > 0 && (
                  <Badge 
                    variant="destructive" 
                    className="absolute -top-2 -right-2 h-5 w-5 p-0 flex items-center justify-center text-xs"
                  >
                    {unreadNotifications.length > 9 ? '9+' : unreadNotifications.length}
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-80">
              <DropdownMenuLabel className="flex items-center justify-between">
                <span>Notifications</span>
                {unreadNotifications.length > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={markAllNotificationsRead}
                    className="h-6 px-2 text-xs"
                  >
                    Mark all read
                  </Button>
                )}
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              {notifications.length > 0 ? (
                <>
                  {notifications.slice(0, 5).map((notification) => (
                    <DropdownMenuItem
                      key={notification.id}
                      className={cn(
                        "flex flex-col items-start p-3 cursor-pointer",
                        !notification.read && "bg-muted/50"
                      )}
                      onClick={() => handleNotificationClick(notification)}
                    >
                      <div className="flex items-start justify-between w-full">
                        <div className="flex-1 min-w-0">
                          <p className={cn(
                            "text-sm truncate",
                            !notification.read && "font-medium"
                          )}>
                            {notification.title}
                          </p>
                          <p className="text-xs text-muted-foreground line-clamp-2 mt-1">
                            {notification.message}
                          </p>
                          <p className="text-xs text-muted-foreground mt-1">
                            {notification.timestamp.toLocaleTimeString()}
                          </p>
                        </div>
                        {!notification.read && (
                          <div className="w-2 h-2 bg-blue-500 rounded-full mt-1 ml-2" />
                        )}
                      </div>
                    </DropdownMenuItem>
                  ))}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem className="text-center text-primary">
                    View all notifications
                  </DropdownMenuItem>
                </>
              ) : (
                <div className="p-6 text-center">
                  <Bell className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                  <p className="text-sm text-muted-foreground">No notifications</p>
                </div>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        {/* Help */}
        {showActions.help && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-9 w-9 p-0"
                  onClick={() => console.log('Help clicked')}
                  aria-label="Help and support"
                >
                  <HelpCircle className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Help and support (?)</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        {/* Settings */}
        {showActions.settings && isAuthenticated && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-9 w-9 p-0"
                  onClick={() => navigate('/studio/settings')}
                  aria-label="Settings"
                >
                  <Settings className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Settings</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        {/* User Menu */}
        {isAuthenticated && user ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button 
                variant="ghost" 
                className="flex items-center space-x-2 h-9 px-2"
                aria-label="User menu"
              >
                <Avatar className="h-7 w-7">
                  <AvatarImage src={user.avatar} alt={user.name} />
                  <AvatarFallback className="bg-primary text-primary-foreground text-xs">
                    {getUserInitials(user.name)}
                  </AvatarFallback>
                </Avatar>
                <div className="hidden sm:flex flex-col items-start min-w-0">
                  <span className="text-sm font-medium truncate max-w-32">
                    {user.name}
                  </span>
                  <span className="text-xs text-muted-foreground truncate max-w-32">
                    {getUserRole()}
                  </span>
                </div>
                <ChevronDown className="h-3 w-3 opacity-50" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuLabel>
                <div className="flex flex-col space-y-1">
                  <p className="text-sm font-medium">{user.name}</p>
                  <p className="text-xs text-muted-foreground">{user.email}</p>
                  <div className="flex items-center space-x-1">
                    <Badge variant="secondary" className="text-xs">
                      {getUserRole()}
                    </Badge>
                  </div>
                </div>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              
              {/* Mobile Edit Mode Toggle */}
              <div className="sm:hidden">
                <DropdownMenuItem onClick={toggleEditMode}>
                  <Activity className="h-4 w-4 mr-2" />
                  {isEditMode ? 'Exit Edit Mode' : 'Enter Edit Mode'}
                  {hasUnsavedChanges && (
                    <Badge variant="destructive" className="ml-auto text-xs">
                      Unsaved
                    </Badge>
                  )}
                </DropdownMenuItem>
                <DropdownMenuSeparator />
              </div>
              
              <DropdownMenuItem onClick={() => navigate('/studio/profile')}>
                <User className="h-4 w-4 mr-2" />
                Profile
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => navigate('/studio/team')}>
                <Users className="h-4 w-4 mr-2" />
                Team
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => navigate('/studio/analytics')}>
                <BarChart3 className="h-4 w-4 mr-2" />
                Analytics
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => navigate('/studio/settings')}>
                <Settings className="h-4 w-4 mr-2" />
                Settings
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem 
                onClick={handleLogout}
                className="text-destructive focus:text-destructive"
              >
                <LogOut className="h-4 w-4 mr-2" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        ) : (
          <Button 
            variant="outline" 
            size="sm" 
            onClick={() => navigate('/login')}
            className="h-8"
          >
            Sign In
          </Button>
        )}
      </div>
    </header>
  );
};

export default UIStudioHeader;