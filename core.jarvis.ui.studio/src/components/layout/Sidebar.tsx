import React, { useState, useEffect } from 'react';
import { 
  Home, 
  Database, 
  FileCode2, 
  Shield, 
  HardDrive, 
  Radio, 
  AlertTriangle,
  FileText,
  ScrollText,
  Plug,
  Settings,
  Menu,
  Table,
  PanelLeftClose
} from 'lucide-react';
import { cn } from '../../lib/utils';

interface SidebarItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  href: string;
}

const sidebarItems: SidebarItem[] = [
  { id: 'home', label: 'Project overview', icon: <Home size={18} />, href: '/' },
  { id: 'table-editor', label: 'Table Editor', icon: <Table size={18} />, href: '/editor' },
  { id: 'sql-editor', label: 'SQL Editor', icon: <FileCode2 size={18} />, href: '/sql' },
  { id: 'database', label: 'Database', icon: <Database size={18} />, href: '/database' },
  { id: 'auth', label: 'Authentication', icon: <Shield size={18} />, href: '/auth' },
  { id: 'storage', label: 'Storage', icon: <HardDrive size={18} />, href: '/storage' },
  { id: 'functions', label: 'Edge Functions', icon: <FileText size={18} />, href: '/functions' },
  { id: 'realtime', label: 'Realtime', icon: <Radio size={18} />, href: '/realtime' },
  { id: 'advisors', label: 'Advisors', icon: <AlertTriangle size={18} />, href: '/advisors' },
  { id: 'reports', label: 'Reports', icon: <ScrollText size={18} />, href: '/reports' },
  { id: 'logs', label: 'Logs', icon: <FileText size={18} />, href: '/logs' },
  { id: 'api', label: 'API Docs', icon: <FileCode2 size={18} />, href: '/api' },
  { id: 'integrations', label: 'Integrations', icon: <Plug size={18} />, href: '/integrations' },
  { id: 'settings', label: 'Project Settings', icon: <Settings size={18} />, href: '/settings' },
];

type SidebarBehavior = 'expandable' | 'open' | 'closed';
const DEFAULT_SIDEBAR_BEHAVIOR: SidebarBehavior = 'expandable';
const SIDEBAR_BEHAVIOR_KEY = 'supabase-sidebar-behavior';

interface SidebarProps {
  activeItem?: string;
  onItemClick?: (itemId: string) => void;
}

export function Sidebar({ activeItem = 'home', onItemClick }: SidebarProps) {
  const [behavior, setBehavior] = useState<SidebarBehavior>(DEFAULT_SIDEBAR_BEHAVIOR);
  const [isExpanded, setIsExpanded] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);

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
          "fixed left-0 top-12 h-[calc(100vh-3.5rem)] bg-gray-900 border-r border-gray-800 z-40 flex flex-col transition-[width] duration-200 ease-out",
          isExpanded ? "w-64" : "w-12",
          "hidden md:flex",
          isMobileOpen && "flex",
          // Add shadow when expanded on hover
          behavior === 'expandable' && isExpanded && "shadow-2xl"
        )}
        onMouseEnter={() => {
          if (behavior === 'expandable' && !showDropdown) setIsExpanded(true);
        }}
        onMouseLeave={() => {
          if (behavior === 'expandable' && !showDropdown) setIsExpanded(false);
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