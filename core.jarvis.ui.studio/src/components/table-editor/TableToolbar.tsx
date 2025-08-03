import { Filter, Plus, Table2, X, PanelLeftClose, PanelLeft } from 'lucide-react';
import { Button } from '../ui/button';
import { useState } from 'react';

interface TableToolbarProps {
  tableName: string;
  isSidebarCollapsed?: boolean;
  onToggleSidebar?: () => void;
}

export function TableToolbar({ tableName, isSidebarCollapsed = false, onToggleSidebar }: TableToolbarProps) {
  const [activeTab, setActiveTab] = useState(0);
  const tabs = [
    { name: tableName, icon: Table2 },
    { name: 'test_component', icon: Table2 }
  ];

  return (
    <div className="flex flex-col">
      {/* Tab Bar */}
      <div className="flex items-center bg-[#0a0a0a] border-b border-[#1e1e1e]">
        {/* Collapse/Expand Button */}
        <button
          onClick={onToggleSidebar}
          className="px-xs py-sm hover:bg-[#141414] transition-colors border-r border-[#1e1e1e]"
          title={isSidebarCollapsed ? "Show sidebar" : "Hide sidebar"}
        >
          {isSidebarCollapsed ? (
            <PanelLeft className="h-xs w-xs text-[#666666]" />
          ) : (
            <PanelLeftClose className="h-xs w-xs text-[#666666]" />
          )}
        </button>
        
        <div className="flex items-center">
          {tabs.map((tab, index) => (
            <div
              key={index}
              onClick={() => setActiveTab(index)}
              className={`
                relative group flex items-center gap-sm px-md py-sm border-r border-[#1e1e1e] min-w-[140px] cursor-pointer
                ${activeTab === index 
                  ? 'bg-[#141414]' 
                  : 'bg-transparent hover:bg-[#0a0a0a]'
                }
              `}
            >
              {/* Top decal indicator */}
              {activeTab === index && (
                <div className="absolute top-0 left-0 right-0 h-px bg-[#3fcf8e]"></div>
              )}
              <tab.icon className={`h-xs w-xs flex-shrink-0 ${
                activeTab === index ? 'text-[#888888]' : 'text-[#555555]'
              }`} />
              <span className={`text-sm truncate ${
                activeTab === index ? 'text-[#fafafa]' : 'text-[#888888]'
              }`}>
                {tab.name}
              </span>
              <button 
                className="ml-auto opacity-0 group-hover:opacity-100 transition-opacity hover:bg-[#262626] rounded p-xs"
                onClick={(e) => {
                  e.stopPropagation();
                  // Handle close
                }}
              >
                <X className="h-xs w-xs text-[#666666]" />
              </button>
            </div>
          ))}
          {/* Add Tab Button */}
          <button className="px-xs py-sm hover:bg-[#141414] transition-colors">
            <Plus className="h-xs w-xs text-[#666666]" />
          </button>
        </div>
      </div>

      {/* Toolbar */}
      <div className="border-b border-[#1e1e1e] bg-[#141414]">
        <div className="flex items-center justify-between px-md py-sm">
          <div className="flex items-center gap-sm">
            <Button variant="ghost" size="sm" className="h-xs px-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              <Filter className="h-xs w-xs mr-xs" />
              <span className="text-sm">Filter</span>
            </Button>
            <Button variant="ghost" size="sm" className="h-xs px-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              <span className="text-sm">Sort</span>
            </Button>
            <Button size="sm" className="h-xs px-xs bg-[#3fcf8e] hover:bg-[#3fcf8e]/90 text-[#0a0a0a] font-medium">
              <Plus className="h-xs w-xs mr-xs" />
              <span className="text-sm">Insert</span>
            </Button>
          </div>
          <div className="flex items-center gap-xs">
            <Button variant="ghost" size="sm" className="h-xs px-xs text-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              Auth policies
            </Button>
            <div className="h-xs w-px bg-[#262626]"></div>
            <Button variant="ghost" size="sm" className="h-xs px-xs text-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              postgres
            </Button>
            <Button variant="ghost" size="sm" className="h-xs px-xs text-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              Realtime off
            </Button>
            <Button variant="ghost" size="sm" className="h-xs px-xs text-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              API Docs
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}