import React, { useState, useEffect } from 'react';
import { 
  FileText,
  Menu,
  PanelLeftClose,
  Plus
} from 'lucide-react';
import * as Icons from 'lucide-react';
import { cn } from '../../lib/utils';
import { useAuth } from '../../contexts/AuthContext';
import { useEditMode } from '../../contexts/EditModeContext';
import { Button } from '../ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '../ui/dialog';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { LucideIcon as Icon } from '../ui/icon';

interface SidebarItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  href: string;
}

// Icon mapping function
const getIcon = (iconName: string) => {
  const IconComponent = Icons[iconName as keyof typeof Icons] as React.ComponentType;
  return <Icon icon={IconComponent || FileText} size="sm" />;
};

type SidebarBehavior = 'expandable' | 'open' | 'closed';
const DEFAULT_SIDEBAR_BEHAVIOR: SidebarBehavior = 'expandable';
const SIDEBAR_BEHAVIOR_KEY = 'jarvis-sidebar-behavior';

interface SidebarProps {
  activeItem?: string;
  onItemClick?: (itemId: string) => void;
}

export function Sidebar({ activeItem = 'home', onItemClick }: SidebarProps) {
  const [behavior, setBehavior] = useState<SidebarBehavior>(DEFAULT_SIDEBAR_BEHAVIOR);
  const [isExpanded, setIsExpanded] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const [showNewPageDialog, setShowNewPageDialog] = useState(false);
  const [newPageData, setNewPageData] = useState({ name: '', route: '' });
  const { navigation } = useAuth();
  const { isEditMode, createPage } = useEditMode();

  // Convert navigation items to sidebar items
  const sidebarItems: SidebarItem[] = navigation.map(item => ({
    id: item.id,
    label: item.label,
    icon: getIcon(item.icon),
    href: item.href
  }));

  // Load sidebar behavior from localStorage
  useEffect(() => {
    const savedBehavior = localStorage.getItem(SIDEBAR_BEHAVIOR_KEY) as SidebarBehavior;
    if (savedBehavior && ['expandable', 'open', 'closed'].includes(savedBehavior)) {
      setBehavior(savedBehavior);
    }
  }, []);

  // Update expanded state based on behavior
  useEffect(() => {
    if (behavior === 'open') setIsExpanded(true);
    else if (behavior === 'closed') setIsExpanded(false);
  }, [behavior]);

  const handleItemClick = (itemId: string) => {
    onItemClick?.(itemId);
    setIsMobileOpen(false);
  };

  const handleBehaviorChange = (newBehavior: SidebarBehavior) => {
    setBehavior(newBehavior);
    localStorage.setItem(SIDEBAR_BEHAVIOR_KEY, newBehavior);
    setShowDropdown(false);
    
    // Dispatch a custom event to notify other components
    window.dispatchEvent(new CustomEvent('sidebar-behavior-change', { 
      detail: { behavior: newBehavior } 
    }));
  };

  // Add click outside handler to close dropdown
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      if (!target.closest('.sidebar-control-dropdown') && !target.closest('.sidebar-control-button')) {
        setShowDropdown(false);
      }
    };

    if (showDropdown) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [showDropdown]);

  // Handle new page creation
  const handleCreatePage = async () => {
    if (!newPageData.name || !newPageData.route) return;
    
    try {
      await createPage({
        displayName: newPageData.name,
        route: newPageData.route.startsWith('/') ? newPageData.route : `/${newPageData.route}`
      });
      setShowNewPageDialog(false);
      setNewPageData({ name: '', route: '' });
    } catch (error) {
      console.error('Failed to create page:', error);
    }
  };

  return (
    <>
      {/* Mobile Menu Button */}
      <button
        onClick={() => setIsMobileOpen(!isMobileOpen)}
        className="fixed top-xs left-xs z-50 p-xs rounded-md bg-dash-sidebar border border-border-stronger lg:hidden"
      >
        <Icon icon={Menu} size="md" />
      </button>

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed left-0 top-3xl h-[calc(100vh-2xsrem)] bg-dash-sidebar border-r border-border-stronger flex flex-col transition-[width] duration-200 ease-out overflow-hidden",
          isExpanded ? "w-4xl" : "w-md",
          "hidden md:flex",
          isMobileOpen && "flex",
          // Higher z-index and shadow when expanded on hover (overlay mode)
          behavior === 'expandable' && isExpanded && "z-50 shadow-2xl",
          // Normal z-index for other modes
          behavior !== 'expandable' && "z-40"
        )}
        role="navigation"
        aria-label="Main navigation"
        onMouseEnter={() => {
          if (behavior === 'expandable' && !showDropdown) {
            setIsExpanded(true);
          }
        }}
        onMouseLeave={() => {
          if (behavior === 'expandable' && !showDropdown) {
            setIsExpanded(false);
          }
        }}
      >
        {/* Navigation Items */}
        <nav className="flex-1 overflow-y-auto py-xs px-xs" role="menubar" aria-label="Primary navigation menu">
          <div className="space-y-xs">
            {sidebarItems.map((item) => (
              <button
                key={item.id}
                onClick={() => handleItemClick(item.id)}
                className={cn(
                  "w-full flex items-center h-sm rounded-md transition-colors relative group",
                  "hover:bg-secondary",
                  activeItem === item.id && "bg-secondary",
                )}
                role="menuitem"
                aria-label={`Navigate to ${item.label}`}
                aria-current={activeItem === item.id ? "page" : undefined}
              >
                {/* Icon container - fixed position */}
                <div className="absolute left-xs w-sm h-sm flex items-center justify-center">
                  <span className={cn(
                    activeItem === item.id ? "text-brand" : "text-muted-foreground",
                    "group-hover:text-foreground/90"
                  )}>
                    {item.icon}
                  </span>
                </div>
                
                {/* Label - only visible when expanded */}
                <div className={cn(
                  "ml-lg mr-xs overflow-hidden transition-opacity duration-200",
                  isExpanded ? "opacity-100" : "opacity-0"
                )}>
                  <span className={cn(
                    "text-sm whitespace-nowrap",
                    activeItem === item.id ? "text-foreground" : "text-muted-foreground",
                    "group-hover:text-foreground/90"
                  )}>
                    {item.label}
                  </span>
                </div>
              </button>
            ))}
            
            {/* New Page Button - Only visible in edit mode */}
            {isEditMode && (
              <Dialog open={showNewPageDialog} onOpenChange={setShowNewPageDialog}>
                <DialogTrigger asChild>
                  <button
                    className={cn(
                      "w-full flex items-center h-sm rounded-md transition-colors relative group",
                      "hover:bg-secondary border-2 border-dashed border-border",
                      "mt-xs"
                    )}
                    role="menuitem"
                    aria-label="Create new page"
                  >
                    {/* Icon container - fixed position */}
                    <div className="absolute left-xs w-sm h-sm flex items-center justify-center">
                      <Icon icon={Plus} size="sm" className="mr-xs" />
                    </div>
                    
                    {/* Label - only visible when expanded */}
                    <div className={cn(
                      "ml-lg mr-xs overflow-hidden transition-opacity duration-200",
                      isExpanded ? "opacity-100" : "opacity-0"
                    )}>
                      <span className="text-sm whitespace-nowrap text-muted-foreground group-hover:text-foreground/90">
                        New Page
                      </span>
                    </div>
                  </button>
                </DialogTrigger>
                
                <DialogContent className="sm:max-w-[425px]">
                  <DialogHeader>
                    <DialogTitle>Create New Page</DialogTitle>
                    <DialogDescription>
                      Add a new page to your application. Choose a display name and route.
                    </DialogDescription>
                  </DialogHeader>
                  
                  <div className="grid gap-xs py-xs">
                    <div className="grid grid-cols-4 items-center gap-xs">
                      <Label htmlFor="page-name" className="text-right">
                        Name
                      </Label>
                      <Input
                        id="page-name"
                        value={newPageData.name}
                        onChange={(e) => setNewPageData({ ...newPageData, name: e.target.value })}
                        placeholder="My New Page"
                        className="col-span-3"
                      />
                    </div>
                    <div className="grid grid-cols-4 items-center gap-xs">
                      <Label htmlFor="page-route" className="text-right">
                        Route
                      </Label>
                      <Input
                        id="page-route"
                        value={newPageData.route}
                        onChange={(e) => setNewPageData({ ...newPageData, route: e.target.value })}
                        placeholder="/my-new-page"
                        className="col-span-3"
                      />
                    </div>
                  </div>
                  
                  <DialogFooter>
                    <Button
                      variant="default"
                      onClick={handleCreatePage}
                      disabled={!newPageData.name || !newPageData.route}
                    >
                      Create Page
                    </Button>
                  </DialogFooter>
                </DialogContent>
              </Dialog>
            )}
          </div>
        </nav>

        {/* Bottom Section with Sidebar Control */}
        <div className="border-t border-border-stronger p-xs relative" role="complementary" aria-label="Sidebar controls">
          {/* Sidebar Control Button */}
          <button
            onClick={() => setShowDropdown(!showDropdown)}
            className="sidebar-control-button w-sm h-sm flex items-center justify-center hover:bg-secondary rounded transition-colors relative"
            title="Sidebar control"
            aria-label="Open sidebar control menu"
            aria-expanded={showDropdown}
            aria-haspopup="menu"
          >
            <Icon icon={PanelLeftClose} size="sm" className="text-muted-foreground" />
          </button>

          {/* Dropdown Menu */}
          {showDropdown && (
            <div 
              className={cn(
                "sidebar-control-dropdown absolute bg-secondary border border-border rounded-md shadow-lg min-w-[180px] z-[100]",
                "bottom-full mb-xs left-0"
              )}
              role="menu"
              aria-label="Sidebar control options"
              >
              <div className="p-xs">
                <div className="px-xs py-xs text-xs font-medium text-muted-foreground">
                  Sidebar control
                </div>
                <div className="h-px bg-border my-xs" />
                <button
                  onClick={() => handleBehaviorChange('open')}
                  className={cn(
                    "w-full flex items-center gap-xs px-xs py-xs text-sm rounded hover:bg-muted transition-colors",
                    behavior === 'open' && "bg-muted"
                  )}
                >
                  <div className={cn(
                    "w-xs h-xs rounded-full border-2",
                    behavior === 'open' ? "border-brand bg-brand" : "border-muted-foreground"
                  )}>
                    {behavior === 'open' && (
                      <div className="w-full h-full rounded-full bg-dash-sidebar scale-[0.4]" />
                    )}
                  </div>
                  Expanded
                </button>
                <button
                  onClick={() => handleBehaviorChange('closed')}
                  className={cn(
                    "w-full flex items-center gap-xs px-xs py-xs text-sm rounded hover:bg-muted transition-colors",
                    behavior === 'closed' && "bg-muted"
                  )}
                >
                  <div className={cn(
                    "w-xs h-xs rounded-full border-2",
                    behavior === 'closed' ? "border-brand bg-brand" : "border-muted-foreground"
                  )}>
                    {behavior === 'closed' && (
                      <div className="w-full h-full rounded-full bg-dash-sidebar scale-[0.4]" />
                    )}
                  </div>
                  Collapsed
                </button>
                <button
                  onClick={() => handleBehaviorChange('expandable')}
                  className={cn(
                    "w-full flex items-center gap-xs px-xs py-xs text-sm rounded hover:bg-muted transition-colors",
                    behavior === 'expandable' && "bg-muted"
                  )}
                >
                  <div className={cn(
                    "w-xs h-xs rounded-full border-2",
                    behavior === 'expandable' ? "border-brand bg-brand" : "border-muted-foreground"
                  )}>
                    {behavior === 'expandable' && (
                      <div className="w-full h-full rounded-full bg-dash-sidebar scale-[0.4]" />
                    )}
                  </div>
                  Expand on hover
                </button>
              </div>
            </div>
          )}
        </div>
      </aside>

      {/* Mobile Overlay */}
      {isMobileOpen && (
        <div 
          className="fixed inset-0 bg-black/50 z-30 lg:hidden"
          onClick={() => setIsMobileOpen(false)}
        />
      )}

      {/* Dropdown Overlay */}
      {showDropdown && (
        <div 
          className="fixed inset-0 z-30"
          onClick={() => setShowDropdown(false)}
        />
      )}
    </>
  );
}