/**
 * ComponentPalette - Draggable component library
 * 
 * Enhanced version with UIStudio integration for production use.
 * Loads available components from UIStudio component registry,
 * supports real-time updates, and provides component binding preview.
 * 
 * Features:
 * - Load components from UIStudio component registry
 * - Real-time component availability updates
 * - Component binding configuration preview
 * - Enhanced drag-and-drop with UIStudio integration
 * - Component usage analytics and recommendations
 */

import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { useDraggable } from '@dnd-kit/core';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
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
  Star,
  Wifi,
  Loader2,
  Package2
} from 'lucide-react';

import type { DeviceType, ComponentCategory } from '@/types/bento';
import type { UIStudioComponentBinding, UIStudioEntityId } from '@/types/uistudio';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

// ============================================================================
// Types
// ============================================================================

export interface ComponentPaletteProps {
  /** Current selected device for responsive filtering */
  selectedDevice?: DeviceType;
  /** Page entity ID for component loading */
  pageEntityId?: UIStudioEntityId;
  /** Enable live component registry updates */
  enableLiveUpdates?: boolean;
  /** Called when a component is dropped onto the canvas */
  onComponentAdd?: (componentType: string, position: { x: number; y: number }) => void;
  /** Called when a component preview is requested */
  onComponentPreview?: (componentType: string) => void;
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
  isFromRegistry?: boolean;
  usageCount?: number;
  lastUsed?: Date;
  minSize: { w: number; h: number };
  maxSize: { w: number; h: number };
  supportedDevices: DeviceType[];
  requiredDataSources?: string[];
  configurationSchema?: Record<string, unknown>;
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
  pageEntityId,
  enableLiveUpdates = true,
  onComponentAdd,
  onComponentPreview,
  className
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<ComponentCategory | 'all'>('all');
  const [showOnlyAvailable, setShowOnlyAvailable] = useState(false);
  
  // Load component registry from UIStudio (mock implementation)
  const {
    data: registryComponents,
    isLoading: registryLoading,
    error: registryError,
    refetch: refetchRegistry
  } = useQuery({
    queryKey: ['component-registry', pageEntityId],
    queryFn: async () => {
      // Mock API call to get available components from UIStudio
      // In production, this would query the UIStudio component registry
      const response = await fetch('/api/uistudio/components/registry');
      if (!response.ok) throw new Error('Failed to load component registry');
      
      const data = await response.json();
      return data.components as ComponentDefinition[];
    },
    enabled: !!pageEntityId,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: enableLiveUpdates ? 30000 : false // Refetch every 30 seconds if live updates enabled
  });
  
  // Handle query error
  useEffect(() => {
    if (registryError) {
      toast.error('Failed to load component registry');
    }
  }, [registryError]);
  
  // Merge static components with registry components
  const allComponents = useMemo(() => {
    const staticComponents = COMPONENT_REGISTRY.map(comp => ({ ...comp, isFromRegistry: false }));
    const dynamicComponents = Array.isArray(registryComponents) ? registryComponents : [];
    
    // Merge and deduplicate by component ID
    const merged = [...staticComponents];
    dynamicComponents.forEach(regComp => {
      const existingIndex = merged.findIndex(comp => comp.id === regComp.id);
      if (existingIndex >= 0) {
        // Update existing component with registry data
        merged[existingIndex] = { ...merged[existingIndex], ...regComp, isFromRegistry: true };
      } else {
        // Add new component from registry
        merged.push({ ...regComp, isFromRegistry: true });
      }
    });
    
    return merged;
  }, [registryComponents]);

  // Enhanced filtering with registry support
  const filteredComponents = useMemo(() => {
    return allComponents.filter(component => {
      // Device compatibility check
      if (!component.supportedDevices.includes(selectedDevice)) {
        return false;
      }

      // Category filter
      if (selectedCategory !== 'all' && component.category !== selectedCategory) {
        return false;
      }
      
      // Show only available filter
      if (showOnlyAvailable && !component.isFromRegistry) {
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
  }, [allComponents, searchQuery, selectedCategory, selectedDevice, showOnlyAvailable]);

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
  
  const handleComponentDrop = useCallback((componentType: string, position: { x: number; y: number }) => {
    onComponentAdd?.(componentType, position);
    
    // Update usage analytics (mock)
    const component = allComponents.find(c => c.id === componentType);
    if (component) {
      toast.success(`Added ${component.name} to your page`);
    }
  }, [onComponentAdd, allComponents]);
  
  // Handle registry errors
  useEffect(() => {
    if (registryError) {
      console.error('Component registry error:', registryError);
    }
  }, [registryError]);

  return (
    <div className={cn('component-palette h-full flex flex-col', className)}>      
      {/* Registry status indicator */}
      {pageEntityId && (
        <div className="mb-3 p-2 bg-muted/50 rounded-lg">
          <div className="flex items-center justify-between text-xs">
            <span className="flex items-center gap-2 text-muted-foreground">
              {registryLoading ? (
                <><Loader2 className="w-3 h-3 animate-spin" />Loading registry...</>
              ) : registryError ? (
                <><Package2 className="w-3 h-3 text-destructive" />Registry offline</>
              ) : (
                <><Wifi className="w-3 h-3 text-green-500" />{allComponents.length} components</>
              )}
            </span>
            {registryError && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => refetchRegistry()}
                className="h-auto p-1 text-xs"
              >
                Retry
              </Button>
            )}
          </div>
        </div>
      )}
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

        {/* Enhanced filters */}
        <div className="space-y-2">
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
          
          {/* Additional filters */}
          <div className="flex items-center gap-2">
            <Button
              variant={showOnlyAvailable ? 'secondary' : 'outline'}
              size="sm"
              onClick={() => setShowOnlyAvailable(!showOnlyAvailable)}
              className="text-xs"
            >
              <Wifi className="w-3 h-3 mr-1" />
              Live Only
            </Button>
          </div>
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
            {registryLoading ? (
              // Loading skeleton for component registry
              <div className="space-y-2">
                {[...Array(6)].map((_, i) => (
                  <Card key={i} className="p-3">
                    <div className="flex items-start gap-2">
                      <Skeleton className="h-5 w-5 rounded" />
                      <div className="flex-1 space-y-2">
                        <Skeleton className="h-4 w-3/4" />
                        <Skeleton className="h-3 w-full" />
                        <div className="flex gap-1">
                          <Skeleton className="h-4 w-12" />
                          <Skeleton className="h-4 w-12" />
                        </div>
                      </div>
                    </div>
                  </Card>
                ))}
              </div>
            ) : (
              filteredComponents.map(component => (
                <DraggableComponent
                  key={component.id}
                  component={component}
                />
              ))
            )}
          </div>
        )}

        {!registryLoading && filteredComponents.length === 0 && (
          <div className="flex flex-col items-center justify-center h-32 text-muted-foreground">
            <Filter className="h-8 w-8 mb-2 opacity-50" />
            <p className="text-sm">
              {registryError ? 'Component registry unavailable' : 'No components found'}
            </p>
            {searchQuery && (
              <p className="text-xs">Try adjusting your search query</p>
            )}
            {showOnlyAvailable && !registryError && (
              <p className="text-xs">Try disabling "Live Only" filter</p>
            )}
          </div>
        )}
      </div>

      {/* Enhanced footer with registry status */}
      <div className="component-palette__footer mt-4 pt-4 border-t">
        <div className="text-xs text-muted-foreground space-y-1">
          <p>
            Showing {filteredComponents.length} components for {selectedDevice}
            {registryComponents && (
              <span className="ml-2 text-green-600">
                ({registryComponents.length} from registry)
              </span>
            )}
          </p>
          <p>Drag components onto the canvas to add them</p>
          {enableLiveUpdates && (
            <p className="flex items-center gap-1">
              <Wifi className="w-3 h-3" />
              Live updates enabled
            </p>
          )}
        </div>
      </div>
    </div>
  );
};

ComponentPalette.displayName = 'ComponentPalette';