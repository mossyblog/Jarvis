/**
 * LayoutToolbar - Toolbar that appears when edit mode is active
 * 
 * Provides component search, palette, and layout tools for editing
 * pages using the Bento Grid system. Only visible in edit mode.
 */

import React, { useState, useMemo } from 'react';
import { 
  Grid3X3,
  Save,
  BarChart3,
  Database,
  FileText,
  Image,
  MousePointer2,
  TrendingUp,
  Target,
  Gauge,
  Table,
  Grid3X3 as GridView,
  Type,
  CreditCard,
  Video,
  Circle,
  Sliders,
  Edit
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { useEditMode } from '@/contexts/EditModeContext';
import { motion, AnimatePresence } from 'framer-motion';
import { ComponentTile } from './ComponentTile';

// ============================================================================
// Types
// ============================================================================

interface ComponentDefinition {
  id: string;
  name: string;
  category: string;
  description: string;
  icon: React.ReactNode;
  defaultSize: { w: number; h: number };
}

// ============================================================================
// Component Definitions
// ============================================================================

const COMPONENT_DEFINITIONS: ComponentDefinition[] = [
  // Dashboard Components
  {
    id: 'metric-card',
    name: 'Metric Card',
    category: 'Dashboard',
    description: 'Display key metrics with charts',
    icon: <BarChart3 size={16} />,
    defaultSize: { w: 2, h: 2 }
  },
  {
    id: 'chart',
    name: 'Chart',
    category: 'Dashboard',
    description: 'Data visualization component',
    icon: <TrendingUp size={16} />,
    defaultSize: { w: 4, h: 3 }
  },
  {
    id: 'kpi',
    name: 'KPI',
    category: 'Dashboard',
    description: 'Key performance indicator',
    icon: <Target size={16} />,
    defaultSize: { w: 2, h: 1 }
  },
  {
    id: 'gauge',
    name: 'Gauge',
    category: 'Dashboard',
    description: 'Progress gauge visualization',
    icon: <Gauge size={16} />,
    defaultSize: { w: 2, h: 2 }
  },
  
  // Data Components
  {
    id: 'table',
    name: 'Data Table',
    category: 'Data',
    description: 'Tabular data display',
    icon: <Table size={16} />,
    defaultSize: { w: 6, h: 4 }
  },
  {
    id: 'list',
    name: 'List View',
    category: 'Data',
    description: 'Scrollable list of items',
    icon: <FileText size={16} />,
    defaultSize: { w: 3, h: 4 }
  },
  {
    id: 'grid-view',
    name: 'Grid View',
    category: 'Data',
    description: 'Card grid layout',
    icon: <GridView size={16} />,
    defaultSize: { w: 4, h: 3 }
  },
  
  // Content Components
  {
    id: 'text-block',
    name: 'Text Block',
    category: 'Content',
    description: 'Rich text content',
    icon: <FileText size={16} />,
    defaultSize: { w: 3, h: 2 }
  },
  {
    id: 'heading',
    name: 'Heading',
    category: 'Content',
    description: 'Section heading',
    icon: <Type size={16} />,
    defaultSize: { w: 4, h: 1 }
  },
  {
    id: 'card',
    name: 'Card',
    category: 'Content',
    description: 'Content card container',
    icon: <CreditCard size={16} />,
    defaultSize: { w: 2, h: 3 }
  },
  
  // Media Components
  {
    id: 'image',
    name: 'Image',
    category: 'Media',
    description: 'Image display component',
    icon: <Image size={16} />,
    defaultSize: { w: 2, h: 2 }
  },
  {
    id: 'video',
    name: 'Video',
    category: 'Media',
    description: 'Video player component',
    icon: <Video size={16} />,
    defaultSize: { w: 3, h: 2 }
  },
  {
    id: 'gallery',
    name: 'Gallery',
    category: 'Media',
    description: 'Image gallery',
    icon: <Image size={16} />,
    defaultSize: { w: 4, h: 3 }
  },
  
  // Action Components
  {
    id: 'button',
    name: 'Button',
    category: 'Actions',
    description: 'Action button',
    icon: <Circle size={16} />,
    defaultSize: { w: 1, h: 1 }
  },
  {
    id: 'button-group',
    name: 'Button Group',
    category: 'Actions',
    description: 'Group of action buttons',
    icon: <Sliders size={16} />,
    defaultSize: { w: 2, h: 1 }
  },
  {
    id: 'form',
    name: 'Form',
    category: 'Actions',
    description: 'Input form',
    icon: <Edit size={16} />,
    defaultSize: { w: 3, h: 4 }
  }
];

// ============================================================================
// Main Component
// ============================================================================

export const LayoutToolbar: React.FC = () => {
  const { 
    isEditMode,
    showGrid,
    toggleGrid,
    hasUnsavedChanges,
    savePage
  } = useEditMode();

  const [activeTab, setActiveTab] = useState('dashboard');

  // Get unique categories
  const categories = useMemo(() => {
    const cats = [...new Set(COMPONENT_DEFINITIONS.map(comp => comp.category))];
    return cats.sort();
  }, []);

  // Group components by category
  const componentsByCategory = useMemo(() => {
    const grouped: Record<string, ComponentDefinition[]> = {};
    COMPONENT_DEFINITIONS.forEach(comp => {
      if (!grouped[comp.category]) {
        grouped[comp.category] = [];
      }
      grouped[comp.category].push(comp);
    });
    return grouped;
  }, []);

  // Handle save
  const handleSave = async () => {
    try {
      await savePage();
    } catch (error) {
      console.error('Failed to save:', error);
      // TODO: Show error toast
    }
  };

  return (
    <AnimatePresence>
      {isEditMode && (
        <motion.div
          initial={{ height: 0, opacity: 0 }}
          animate={{ height: 'auto', opacity: 1 }}
          exit={{ height: 0, opacity: 0 }}
          transition={{ duration: 0.3, ease: 'easeInOut' }}
          className="layout-toolbar border-b bg-card/95 backdrop-blur supports-[backdrop-filter]:bg-card/60 overflow-hidden"
        >
          {/* Ribbon Header with Tabs and Controls */}
          <div className="flex items-center justify-between px-4 h-12 border-b">
            {/* Tabs for Categories */}
            <div className="flex items-center gap-1 relative h-full">
              <button
                type="button"
                onClick={() => setActiveTab('dashboard')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors h-full",
                  activeTab === 'dashboard' 
                    ? "bg-blue-500/20 text-blue-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <BarChart3 size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Dashboard</span>
              </button>
              
              <button
                type="button"
                onClick={() => setActiveTab('data')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors h-full",
                  activeTab === 'data' 
                    ? "bg-green-500/20 text-green-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Database size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Data</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('content')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors h-full",
                  activeTab === 'content' 
                    ? "bg-purple-500/20 text-purple-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <FileText size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Content</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('media')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors h-full",
                  activeTab === 'media' 
                    ? "bg-orange-500/20 text-orange-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Image size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Media</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('actions')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors h-full",
                  activeTab === 'actions' 
                    ? "bg-red-500/20 text-red-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <MousePointer2 size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Actions</span>
              </button>
              
              {/* Colored line under active tab */}
              <div className={cn(
                "absolute bottom-0 left-0 right-0 h-0.5 transition-colors",
                activeTab === 'dashboard' && "bg-blue-500",
                activeTab === 'data' && "bg-green-500",
                activeTab === 'content' && "bg-purple-500",
                activeTab === 'media' && "bg-orange-500",
                activeTab === 'actions' && "bg-red-500"
              )} />
            </div>

            {/* Right Controls */}
            <div className="flex items-center gap-2">

              {/* Grid Toggle */}
              <Button
                variant={showGrid ? "secondary" : "ghost"}
                size="sm"
                onClick={toggleGrid}
                className="h-8 px-2"
              >
                <Grid3X3 className="h-4 w-4" />
              </Button>

              <Separator orientation="vertical" className="h-6" />

              {/* Save Action */}
              <div className="flex items-center gap-2">
                {hasUnsavedChanges && (
                  <Badge variant="secondary" className="text-xs">
                    Unsaved
                  </Badge>
                )}
                <Button
                  variant="default"
                  size="sm"
                  onClick={handleSave}
                  disabled={!hasUnsavedChanges}
                  className="h-8 px-3"
                >
                  <Save className="h-4 w-4 mr-1" />
                  Save
                </Button>
              </div>
            </div>
          </div>

          {/* Ribbon Content - Component Tiles */}
          <div className="px-4 py-2">
            {/* Show components for active tab */}
            <div className="flex flex-wrap gap-2">
              {(() => {
                const category = categories.find(c => c.toLowerCase() === activeTab);
                return category ? componentsByCategory[category]?.map((component: any) => (
                <ComponentTile
                  key={component.id}
                  id={component.id}
                  name={component.name}
                  description={component.description}
                  icon={component.icon}
                  defaultSize={component.defaultSize}
                />
              )) : null;
              })()}
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
};

export default LayoutToolbar;