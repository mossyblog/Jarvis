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

interface SidebarItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  href: string;
}

// Icon mapping function
const getIcon = (iconName: string, size: number = 18) => {
  const IconComponent = Icons[iconName as keyof typeof Icons] as React.ComponentType<{ size?: number }>;
  return IconComponent ? <IconComponent size={size} /> : <FileText size={size} />;
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
        className="fixed top-4 left-4 z-50 p-2 rounded-md bg-gray-900 border border-gray-800 lg:hidden"
      >
        <Menu size={20} />
      </button>

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed left-0 top-12 h-[calc(100vh-3.5rem)] bg-gray-900 border-r border-gray-800 flex flex-col transition-[width] duration-200 ease-out",
          isExpanded ? "w-64" : "w-12",
          "hidden md:flex",
          isMobileOpen && "flex",
          // Higher z-index and shadow when expanded on hover (overlay mode)
          behavior === 'expandable' && isExpanded && "z-50 shadow-2xl",
          // Normal z-index for other modes
          behavior !== 'expandable' && "z-40"
        )}
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
        <nav className="flex-1 overflow-y-auto py-1 px-2">
          <div className="space-y-1">
            {sidebarItems.map((item) => (
              <button
                key={item.id}
                onClick={() => handleItemClick(item.id)}
                className={cn(
                  "w-full flex items-center h-9 rounded-md transition-colors relative group",
                  "hover:bg-gray-800",
                  activeItem === item.id && "bg-gray-800",
                )}
              >
                {/* Icon container - fixed position */}
                <div className="absolute left-2 w-5 h-5 flex items-center justify-center">
                  <span className={cn(
                    activeItem === item.id ? "text-brand" : "text-gray-400",
                    "group-hover:text-gray-200"
                  )}>
                    {item.icon}
                  </span>
                </div>
                
                {/* Label - only visible when expanded */}
                <div className={cn(
                  "ml-9 mr-3 overflow-hidden transition-opacity duration-200",
                  isExpanded ? "opacity-100" : "opacity-0"
                )}>
                  <span className={cn(
                    "text-sm whitespace-nowrap",
                    activeItem === item.id ? "text-gray-100" : "text-gray-400",
                    "group-hover:text-gray-200"
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
                      "w-full flex items-center h-9 rounded-md transition-colors relative group",
                      "hover:bg-gray-800 border-2 border-dashed border-gray-700",
                      "mt-2"
                    )}
                  >
                    {/* Icon container - fixed position */}
                    <div className="absolute left-2 w-5 h-5 flex items-center justify-center">
                      <Plus size={18} className="text-gray-400 group-hover:text-gray-200" />
                    </div>
                    
                    {/* Label - only visible when expanded */}
                    <div className={cn(
                      "ml-9 mr-3 overflow-hidden transition-opacity duration-200",
                      isExpanded ? "opacity-100" : "opacity-0"
                    )}>
                      <span className="text-sm whitespace-nowrap text-gray-400 group-hover:text-gray-200">
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
                  
                  <div className="grid gap-4 py-4">
                    <div className="grid grid-cols-4 items-center gap-4">
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
                    <div className="grid grid-cols-4 items-center gap-4">
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
        <div className="border-t border-gray-800 p-2 relative">
          {/* Sidebar Control Button */}
          <button
            onClick={() => setShowDropdown(!showDropdown)}
            className="sidebar-control-button w-9 h-9 flex items-center justify-center hover:bg-gray-800 rounded transition-colors relative"
            title="Sidebar control"
          >
            <PanelLeftClose size={18} className="text-gray-400" />
          </button>

          {/* Dropdown Menu */}
          {showDropdown && (
            <div 
              className={cn(
                "sidebar-control-dropdown absolute bg-gray-800 border border-gray-700 rounded-md shadow-lg min-w-[180px] z-[100]",
                "bottom-full mb-2 left-0"
              )}>
              <div className="p-1">
                <div className="px-2 py-1.5 text-xs font-medium text-gray-400">
                  Sidebar control
                </div>
                <div className="h-px bg-gray-700 my-1" />
                <button
                  onClick={() => handleBehaviorChange('open')}
                  className={cn(
                    "w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-gray-700 transition-colors",
                    behavior === 'open' && "bg-gray-700"
                  )}
                >
                  <div className={cn(
                    "w-4 h-4 rounded-full border-2",
                    behavior === 'open' ? "border-brand bg-brand" : "border-gray-500"
                  )}>
                    {behavior === 'open' && (
                      <div className="w-full h-full rounded-full bg-gray-900 scale-[0.4]" />
                    )}
                  </div>
                  Expanded
                </button>
                <button
                  onClick={() => handleBehaviorChange('closed')}
                  className={cn(
                    "w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-gray-700 transition-colors",
                    behavior === 'closed' && "bg-gray-700"
                  )}
                >
                  <div className={cn(
                    "w-4 h-4 rounded-full border-2",
                    behavior === 'closed' ? "border-brand bg-brand" : "border-gray-500"
                  )}>
                    {behavior === 'closed' && (
                      <div className="w-full h-full rounded-full bg-gray-900 scale-[0.4]" />
                    )}
                  </div>
                  Collapsed
                </button>
                <button
                  onClick={() => handleBehaviorChange('expandable')}
                  className={cn(
                    "w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-gray-700 transition-colors",
                    behavior === 'expandable' && "bg-gray-700"
                  )}
                >
                  <div className={cn(
                    "w-4 h-4 rounded-full border-2",
                    behavior === 'expandable' ? "border-brand bg-brand" : "border-gray-500"
                  )}>
                    {behavior === 'expandable' && (
                      <div className="w-full h-full rounded-full bg-gray-900 scale-[0.4]" />
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