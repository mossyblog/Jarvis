import { useState, useEffect } from 'react';
import { Sidebar } from './Sidebar';
import LayoutHeader from './LayoutHeader';
import { LayoutToolbar } from './LayoutToolbar';
import { cn } from '../../lib/utils';
import { useDeviceEmulation } from '@/hooks/useDeviceEmulation';

type SidebarBehavior = 'expandable' | 'open' | 'closed';
const SIDEBAR_BEHAVIOR_KEY = 'supabase-sidebar-behavior';

interface DashboardLayoutProps {
  children: React.ReactNode;
  activeItem?: string;
  onItemClick?: (itemId: string) => void;
}

export function DashboardLayout({ children, activeItem, onItemClick }: DashboardLayoutProps) {
  const [sidebarBehavior, setSidebarBehavior] = useState<SidebarBehavior>('expandable');
  const { isEmulating } = useDeviceEmulation();

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
    <div className="flex flex-col h-screen bg-background overflow-hidden">
      {/* Top Header Bar - Always full width */}
      <LayoutHeader showProductMenu />
      
      {/* Device Emulation Container */}
      <div className={cn(
        "flex flex-col flex-1 overflow-hidden",
        isEmulating && "device-emulation-container"
      )}>
        {/* Layout Toolbar - Only visible in edit mode */}
        <div className={cn(
          "transition-all duration-200",
          !isEmulating && sidebarBehavior === 'open' ? "md:ml-6xl" : !isEmulating && "md:ml-3xl"
        )}>
          <LayoutToolbar />
        </div>
        
        {/* Main Content Area with Sidebar */}
        <div className="flex flex-1 min-h-0 overflow-hidden">
          {/* Hide sidebar in device emulation modes */}
          {!isEmulating && <Sidebar activeItem={activeItem} onItemClick={onItemClick} />}
          
          {/* Main Content */}
          <main 
            id="main-content"
            className={cn(
              "flex-1 min-h-0 overflow-y-auto overflow-x-hidden transition-all duration-200 pb-16",
              // Only push content when sidebar is in 'open' mode and not emulating
              !isEmulating && sidebarBehavior === 'open' ? "md:ml-6xl" : !isEmulating && "md:ml-3xl",
              isEmulating && "ml-0 w-full"
            )}
            role="main"
            aria-label="Main content area"
            tabIndex={-1}
          >
            {children}
          </main>
        </div>
      </div>
    </div>
  );
}