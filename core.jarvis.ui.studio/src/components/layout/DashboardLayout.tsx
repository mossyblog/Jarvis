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
      if (newBehavior) setSidebarBehavior(newBehavior);
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
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
          sidebarBehavior === 'open' ? "md:ml-64" : "md:ml-12"
        )}>
          {children}
        </main>
      </div>
    </div>
  );
}