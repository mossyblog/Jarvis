import { useState, useEffect } from 'react';
import { Sidebar } from './Sidebar';
import LayoutHeader from './LayoutHeader';
import { cn } from '../../lib/utils';

type SidebarBehavior = 'expandable' | 'open' | 'closed';
const SIDEBAR_BEHAVIOR_KEY = 'supabase-sidebar-behavior';

interface DashboardLayoutProps {
  children: React.ReactNode;
  activeItem?: string;
  onItemClick?: (itemId: string) => void;
}

export function DashboardLayout({ children, activeItem, onItemClick }: DashboardLayoutProps) {
  const [sidebarBehavior, setSidebarBehavior] = useState<SidebarBehavior>('expandable');

  useEffect(() => {
    const savedBehavior = localStorage.getItem(SIDEBAR_BEHAVIOR_KEY) as SidebarBehavior;
    if (savedBehavior && ['expandable', 'open', 'closed'].includes(savedBehavior)) {
      setSidebarBehavior(savedBehavior);
    }

    // Listen for storage changes
    const handleStorageChange = () => {
      const newBehavior = localStorage.getItem(SIDEBAR_BEHAVIOR_KEY) as SidebarBehavior;
      if (newBehavior) {
        setSidebarBehavior(newBehavior);
      }
    };

    // Listen for custom sidebar behavior change events
    const handleSidebarBehaviorChange = (event: CustomEvent) => {
      const newBehavior = event.detail.behavior as SidebarBehavior;
      setSidebarBehavior(newBehavior);
    };

    window.addEventListener('storage', handleStorageChange);
    window.addEventListener('sidebar-behavior-change', handleSidebarBehaviorChange as EventListener);
    
    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('sidebar-behavior-change', handleSidebarBehaviorChange as EventListener);
    };
  }, []);

  return (
    <div className="flex flex-col h-screen bg-background">
      {/* Top Header Bar */}
      <LayoutHeader showProductMenu />
      
      {/* Main Content Area with Sidebar */}
      <div className="flex flex-1 overflow-hidden">
        <Sidebar activeItem={activeItem} onItemClick={onItemClick} />
        
        {/* Main Content */}
        <main className={cn(
          "flex-1 overflow-y-auto overflow-x-hidden transition-all duration-200",
          // Only push content when sidebar is in 'open' mode
          // In 'expandable' mode, sidebar overlays content
          sidebarBehavior === 'open' ? "md:ml-32" : "md:ml-12"
        )}>
          {children}
        </main>
      </div>
    </div>
  );
}