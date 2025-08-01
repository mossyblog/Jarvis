/**
 * ComponentPalette - Draggable component library
 * 
 * Provides a palette of available components that can be dragged onto
 * the grid canvas to build pages visually.
 */

import React, { useState, useMemo, useCallback } from 'react';
import { useDraggable } from '@dnd-kit/core';
import {
  Search,
  Filter,
  BarChart3,
  Grid3X3,
  Type,
  Image,
  Video,
  Calendar,
  User,
  Settings,
  Database,
  PieChart,
  TrendingUp,
  Activity,
  Clock,
  Mail,
  Bell,
  Table,
  Map,
  MessageSquare,
  Star
} from 'lucide-react';

import type { DeviceType, ComponentCategory } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

// ============================================================================
// Types
// ============================================================================

export interface ComponentPaletteProps {
  /** Current selected device for responsive filtering */
  selectedDevice?: DeviceType;
  /** Called when a component is dropped onto the canvas */
  onComponentAdd?: (componentType: string, position: { x: number; y: number }) => void;
  /** Additional CSS classes */
  className?: string;
}

interface ComponentDefinition {
  id: string;
  name: string;
  description: string;
  category: ComponentCategory;
  icon: React.ComponentType<{ className?: string }>;
  tags: string[];
  isPremium?: boolean;
  isNew?: boolean;
  minSize: { w: number; h: number };
  maxSize: { w: number; h: number };
  supportedDevices: DeviceType[];
}

interface DraggableComponentProps {
  component: ComponentDefinition;
  isDisabled?: boolean;
}

// ============================================================================
// Component Registry
// ============================================================================

const COMPONENT_REGISTRY: ComponentDefinition[] = [
  // Analytics Components
  {
    id: 'metric-card',
    name: 'Metric Card',
    description: 'Display key performance indicators',
    category: 'Analytics' as ComponentCategory,
    icon: TrendingUp,
    tags: ['metric', 'kpi', 'number', 'statistics'],
    minSize: { w: 2, h: 2 },
    maxSize: { w: 4, h: 3 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'line-chart',
    name: 'Line Chart',
    description: 'Time series data visualization',
    category: 'Analytics' as ComponentCategory,
    icon: BarChart3,
    tags: ['chart', 'graph', 'time', 'trend'],
    minSize: { w: 4, h: 3 },
    maxSize: { w: 12, h: 8 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },
  {
    id: 'pie-chart',
    name: 'Pie Chart',
    description: 'Proportional data visualization',
    category: 'Analytics' as ComponentCategory,
    icon: PieChart,
    tags: ['chart', 'pie', 'proportion', 'percentage'],
    minSize: { w: 3, h: 3 },
    maxSize: { w: 6, h: 6 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'activity-feed',
    name: 'Activity Feed',
    description: 'Recent activity and events',
    category: 'Analytics' as ComponentCategory,
    icon: Activity,
    tags: ['activity', 'feed', 'timeline', 'events'],
    minSize: { w: 3, h: 4 },
    maxSize: { w: 6, h: 12 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },

  // Data Components
  {
    id: 'data-table',
    name: 'Data Table',
    description: 'Tabular data with sorting and filtering',
    category: 'Data' as ComponentCategory,
    icon: Table,
    tags: ['table', 'data', 'grid', 'list'],
    minSize: { w: 6, h: 4 },
    maxSize: { w: 12, h: 12 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },
  {
    id: 'user-list',
    name: 'User List',
    description: 'List of users with avatars and details',
    category: 'Data' as ComponentCategory,
    icon: User,
    tags: ['users', 'people', 'contacts', 'directory'],
    minSize: { w: 3, h: 4 },
    maxSize: { w: 8, h: 12 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'database-status',
    name: 'Database Status',
    description: 'Database connection and health status',
    category: 'Data' as ComponentCategory,
    icon: Database,
    tags: ['database', 'status', 'health', 'monitoring'],
    minSize: { w: 2, h: 2 },
    maxSize: { w: 4, h: 3 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },

  // Status Components
  {
    id: 'system-health',
    name: 'System Health',
    description: 'Overall system status indicator',
    category: 'Status' as ComponentCategory,
    icon: Activity,
    tags: ['health', 'status', 'system', 'monitoring'],
    minSize: { w: 2, h: 2 },
    maxSize: { w: 4, h: 3 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'alert-panel',
    name: 'Alert Panel',
    description: 'Critical alerts and notifications',
    category: 'Status' as ComponentCategory,
    icon: Bell,
    tags: ['alerts', 'notifications', 'warnings', 'errors'],
    minSize: { w: 3, h: 2 },
    maxSize: { w: 6, h: 4 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'uptime-monitor',
    name: 'Uptime Monitor',
    description: 'Service uptime tracking',
    category: 'Status' as ComponentCategory,
    icon: Clock,
    tags: ['uptime', 'availability', 'monitoring', 'sla'],
    minSize: { w: 3, h: 2 },
    maxSize: { w: 6, h: 3 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },

  // Forms Components
  {
    id: 'contact-form',
    name: 'Contact Form',
    description: 'Contact form with validation',
    category: 'Forms' as ComponentCategory,
    icon: Mail,
    tags: ['form', 'contact', 'email', 'validation'],
    minSize: { w: 4, h: 6 },
    maxSize: { w: 8, h: 12 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'settings-panel',
    name: 'Settings Panel',
    description: 'Configuration settings interface',
    category: 'Forms' as ComponentCategory,
    icon: Settings,
    tags: ['settings', 'configuration', 'preferences', 'options'],
    minSize: { w: 4, h: 6 },
    maxSize: { w: 8, h: 12 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },

  // Layout Components
  {
    id: 'text-block',
    name: 'Text Block',
    description: 'Rich text content area',
    category: 'Layout' as ComponentCategory,
    icon: Type,
    tags: ['text', 'content', 'paragraph', 'typography'],
    minSize: { w: 2, h: 2 },
    maxSize: { w: 12, h: 8 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'card-layout',
    name: 'Card Layout',
    description: 'Flexible card container',
    category: 'Layout' as ComponentCategory,
    icon: Grid3X3,
    tags: ['card', 'container', 'layout', 'wrapper'],
    minSize: { w: 2, h: 2 },
    maxSize: { w: 6, h: 6 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },

  // Media Components
  {
    id: 'image-gallery',
    name: 'Image Gallery',
    description: 'Photo gallery with lightbox',
    category: 'Media' as ComponentCategory,
    icon: Image,
    tags: ['images', 'gallery', 'photos', 'media'],
    minSize: { w: 4, h: 4 },
    maxSize: { w: 8, h: 8 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  },
  {
    id: 'video-player',
    name: 'Video Player',
    description: 'Embedded video player',
    category: 'Media' as ComponentCategory,
    icon: Video,
    tags: ['video', 'player', 'media', 'streaming'],
    minSize: { w: 4, h: 3 },
    maxSize: { w: 8, h: 6 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },

  // Custom Components
  {
    id: 'calendar-widget',
    name: 'Calendar Widget',
    description: 'Interactive calendar display',
    category: 'Custom' as ComponentCategory,
    icon: Calendar,
    tags: ['calendar', 'events', 'schedule', 'dates'],
    minSize: { w: 4, h: 4 },
    maxSize: { w: 8, h: 8 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },
  {
    id: 'map-widget',
    name: 'Map Widget',
    description: 'Interactive map component',
    category: 'Custom' as ComponentCategory,
    icon: Map,
    tags: ['map', 'location', 'geography', 'interactive'],
    minSize: { w: 4, h: 4 },
    maxSize: { w: 8, h: 8 },
    supportedDevices: ['desktop', 'tablet'] as DeviceType[]
  },
  {
    id: 'chat-widget',
    name: 'Chat Widget',
    description: 'Real-time chat interface',
    category: 'Custom' as ComponentCategory,
    icon: MessageSquare,
    tags: ['chat', 'messaging', 'communication', 'realtime'],
    isNew: true,
    minSize: { w: 3, h: 4 },
    maxSize: { w: 6, h: 8 },
    supportedDevices: ['desktop', 'tablet', 'mobile'] as DeviceType[]
  }
];

// ============================================================================
// Draggable Component
// ============================================================================

const DraggableComponent: React.FC<DraggableComponentProps> = ({ component, isDisabled = false }) => {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: component.id,
    disabled: isDisabled,
    data: {
      type: 'component',
      componentType: component.id
    }
  });

  const style = transform ? {
    transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
    zIndex: isDragging ? 1000 : 1
  } : {};

  const IconComponent = component.icon;

  return (
    <Card
      ref={setNodeRef}
      className={cn(
        'component-palette-item cursor-grab active:cursor-grabbing transition-all',
        'hover:shadow-md hover:border-primary/50',
        isDragging && 'shadow-lg border-primary opacity-50',
        isDisabled && 'opacity-50 cursor-not-allowed'
      )}
      style={style}
      {...listeners}
      {...attributes}
    >
      <CardContent className="p-3">
        <div className="flex items-start gap-2">
          <div className="flex-shrink-0">
            <IconComponent className="h-5 w-5 text-primary" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-1 mb-1">
              <h4 className="text-sm font-medium truncate">{component.name}</h4>
              {component.isNew && (
                <Badge variant="secondary" className="text-xs">New</Badge>
              )}
              {component.isPremium && (
                <Star className="h-3 w-3 text-yellow-500" />
              )}
            </div>
            <p className="text-xs text-muted-foreground line-clamp-2">
              {component.description}
            </p>
            <div className="flex flex-wrap gap-1 mt-2">
              {component.tags.slice(0, 2).map(tag => (
                <Badge key={tag} variant="outline" className="text-xs px-1">
                  {tag}
                </Badge>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const ComponentPalette: React.FC<ComponentPaletteProps> = ({
  selectedDevice = 'desktop' as DeviceType,
  // onComponentAdd,
  className
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<ComponentCategory | 'all'>('all');

  // Filter components based on search and category
  const filteredComponents = useMemo(() => {
    return COMPONENT_REGISTRY.filter(component => {
      // Device compatibility check
      if (!component.supportedDevices.includes(selectedDevice)) {
        return false;
      }

      // Category filter
      if (selectedCategory !== 'all' && component.category !== selectedCategory) {
        return false;
      }

      // Search query filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        return (
          component.name.toLowerCase().includes(query) ||
          component.description.toLowerCase().includes(query) ||
          component.tags.some(tag => tag.toLowerCase().includes(query))
        );
      }

      return true;
    });
  }, [searchQuery, selectedCategory, selectedDevice]);

  // Group components by category
  const componentsByCategory = useMemo(() => {
    const grouped: Record<ComponentCategory, ComponentDefinition[]> = {
      Analytics: [],
      Data: [],
      Status: [],
      Navigation: [],
      Forms: [],
      Layout: [],
      Media: [],
      Custom: []
    };

    filteredComponents.forEach(component => {
      if (!grouped[component.category]) {
        grouped[component.category] = [];
      }
      grouped[component.category].push(component);
    });

    return grouped;
  }, [filteredComponents]);

  // Get categories with components
  const availableCategories = useMemo(() => {
    return Object.entries(componentsByCategory)
      .filter(([, components]) => components.length > 0)
      .map(([category]) => category as ComponentCategory);
  }, [componentsByCategory]);

  const handleSearchChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(event.target.value);
  }, []);

  const handleCategoryChange = useCallback((category: string) => {
    setSelectedCategory(category === 'all' ? 'all' : category as ComponentCategory);
  }, []);

  return (
    <div className={cn('component-palette h-full flex flex-col', className)}>
      {/* Header */}
      <div className="component-palette__header mb-4">
        <h3 className="text-lg font-semibold mb-3">Components</h3>
        
        {/* Search */}
        <div className="relative mb-3">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search components..."
            value={searchQuery}
            onChange={handleSearchChange}
            className="pl-9"
          />
        </div>

        {/* Category Filter */}
        <div className="flex flex-wrap gap-1">
          <Button
            variant={selectedCategory === 'all' ? 'secondary' : 'outline'}
            size="sm"
            onClick={() => handleCategoryChange('all')}
          >
            All
          </Button>
          {availableCategories.map(category => (
            <Button
              key={category}
              variant={selectedCategory === category ? 'secondary' : 'outline'}
              size="sm"
              onClick={() => handleCategoryChange(category)}
            >
              {category}
            </Button>
          ))}
        </div>
      </div>

      {/* Component List */}
      <div className="component-palette__content flex-1 overflow-auto">
        {selectedCategory === 'all' ? (
          // Show all categories
          <div className="space-y-6">
            {availableCategories.map(category => (
              <div key={category}>
                <h4 className="text-sm font-medium mb-2 text-muted-foreground">
                  {category}
                </h4>
                <div className="grid gap-2">
                  {componentsByCategory[category].map(component => (
                    <DraggableComponent
                      key={component.id}
                      component={component}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        ) : (
          // Show selected category only
          <div className="grid gap-2">
            {filteredComponents.map(component => (
              <DraggableComponent
                key={component.id}
                component={component}
              />
            ))}
          </div>
        )}

        {filteredComponents.length === 0 && (
          <div className="flex flex-col items-center justify-center h-32 text-muted-foreground">
            <Filter className="h-8 w-8 mb-2 opacity-50" />
            <p className="text-sm">No components found</p>
            {searchQuery && (
              <p className="text-xs">Try adjusting your search query</p>
            )}
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="component-palette__footer mt-4 pt-4 border-t">
        <div className="text-xs text-muted-foreground">
          <p>Showing {filteredComponents.length} components for {selectedDevice}</p>
          <p className="mt-1">Drag components onto the canvas to add them</p>
        </div>
      </div>
    </div>
  );
};

ComponentPalette.displayName = 'ComponentPalette';