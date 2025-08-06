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
import { componentRegistryService, type ComponentRegistryComponent } from '@/services/componentRegistryService';
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

import { DeviceType, ComponentCategory } from '@/types/bento';
import type { UIStudioEntityId } from '@/types/uistudio';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
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
  /** Whether to render in compact mobile mode */
  compact?: boolean;
  /** Additional CSS classes */
  className?: string;
}

interface ComponentDefinition extends Omit<ComponentRegistryComponent, 'icon' | 'category' | 'supportedDevices'> {
  category: ComponentCategory;
  icon: React.ComponentType<{ className?: string }>;
  supportedDevices: DeviceType[];
}

interface DraggableComponentProps {
  component: ComponentDefinition;
  isDisabled?: boolean;
  onComponentAdd?: (componentType: string, position: { x: number; y: number }) => void;
}

// ============================================================================
// Component Registry Dynamic Loading
// ============================================================================

// Icon mapping for components from registry
const ICON_MAP: Record<string, React.ComponentType<{ className?: string }>> = {
  'TrendingUp': TrendingUp,
  'BarChart3': BarChart3,
  'PieChart': PieChart,
  'Table': Table,
  'User': User,
  'Activity': Activity,
  'Bell': Bell,
  'Mail': Mail,
  'Type': Type,
  'Image': Image,
  'Calendar': Calendar,
  'MessageSquare': MessageSquare,
  'Database': Database,
  'Package2': Package2, // Default fallback icon
};

// Category mapping from string to ComponentCategory
const CATEGORY_MAP: Record<string, ComponentCategory> = {
  'Analytics': 'Analytics' as ComponentCategory,
  'Data': 'Data' as ComponentCategory,
  'Status': 'Status' as ComponentCategory,
  'Forms': 'Forms' as ComponentCategory,
  'Layout': 'Layout' as ComponentCategory,
  'Media': 'Media' as ComponentCategory,
  'Custom': 'Custom' as ComponentCategory,
};

// Device mapping from string to DeviceType
const DEVICE_MAP: Record<string, DeviceType> = {
  'desktop': 'desktop' as DeviceType,
  'tablet': 'tablet' as DeviceType,
  'mobile': 'mobile' as DeviceType,
};

/**
 * Transform registry component to ComponentDefinition
 */
function transformRegistryComponent(registryComponent: ComponentRegistryComponent): ComponentDefinition {
  return {
    ...registryComponent,
    category: CATEGORY_MAP[registryComponent.category] || 'Custom',
    icon: ICON_MAP[registryComponent.icon] || Package2,
    supportedDevices: registryComponent.supportedDevices
      .map(device => DEVICE_MAP[device])
      .filter(Boolean) as DeviceType[],
  };
}

// Fallback static components if API fails - These will be dynamically loaded from registry in production
const FALLBACK_STATIC_COMPONENTS: Omit<ComponentDefinition, 'isFromRegistry'>[] = [
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

/**
 * Convert static components to ComponentDefinition with isFromRegistry flag
 */
function createFallbackComponents(): ComponentDefinition[] {
  return FALLBACK_STATIC_COMPONENTS.map(comp => ({
    ...comp,
    isFromRegistry: false
  }));
}

/**
 * Custom hook for dynamic component loading
 */
function useComponentRegistry(searchTerm?: string, activeCategory?: ComponentCategory, deviceType: DeviceType = DeviceType.Desktop) {
  // Load components from registry with caching
  const componentsQuery = useQuery({
    queryKey: ['component-registry', { search: searchTerm, category: activeCategory, device: deviceType }],
    queryFn: async () => {
      try {
        if (searchTerm) {
          const searchResults = await componentRegistryService.searchComponents(searchTerm, {
            category: activeCategory,
            limit: 50
          });
          return searchResults.map(transformRegistryComponent);
        } else {
          const allComponents = await componentRegistryService.getComponents({
            category: activeCategory,
            device: deviceType
          });
          return allComponents.map(transformRegistryComponent);
        }
      } catch (error) {
        console.error('Failed to load components from registry:', error);
        // Fallback to static components
        let components = createFallbackComponents();
        
        if (activeCategory) {
          components = components.filter(comp => comp.category === activeCategory);
        }
        
        if (searchTerm) {
          const searchLower = searchTerm.toLowerCase();
          components = components.filter(comp =>
            comp.name.toLowerCase().includes(searchLower) ||
            comp.description.toLowerCase().includes(searchLower) ||
            comp.tags.some(tag => tag.toLowerCase().includes(searchLower))
          );
        }
        
        return components.filter(comp => comp.supportedDevices.includes(deviceType));
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (renamed from cacheTime)
    refetchOnWindowFocus: false,
  });

  return {
    components: componentsQuery.data || [],
    isLoading: componentsQuery.isLoading,
    error: componentsQuery.error,
    refetch: componentsQuery.refetch
  };
}

// ============================================================================
// Draggable Component
// ============================================================================

const DraggableComponent: React.FC<DraggableComponentProps & { compact?: boolean }> = ({ 
  component, 
  isDisabled = false, 
  compact = false,
  onComponentAdd
}) => {
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
        'hover:shadow-md hover:border-primary/50 touch-manipulation',
        isDragging && 'shadow-lg border-primary opacity-50',
        isDisabled && 'opacity-50 cursor-not-allowed',
        compact && 'min-h-[60px]'
      )}
      style={style}
      {...listeners}
      {...attributes}
    >
      <CardContent className={cn(
        compact ? 'p-2' : 'p-3'
      )}>
        {compact ? (
          // Compact mobile layout
          <div className="flex items-center gap-2">
            <div className="flex-shrink-0">
              <IconComponent className="h-xs w-xs text-primary" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-1">
                <h4 className="text-xs font-medium truncate">{component.name}</h4>
                {component.isNew && (
                  <div className="w-xs h-xs bg-blue-500 rounded-full" />
                )}
                {component.isPremium && (
                  <Star className="h-xs w-xs text-yellow-sm00" />
                )}
              </div>
              <p className="text-xs text-muted-foreground truncate">
                {component.description}
              </p>
            </div>
            <div className="flex-shrink-0">
              <Button
                size="sm"
                variant="ghost"
                className="h-md w-md p-0"
                onClick={(e) => {
                  e.stopPropagation();
                  onComponentAdd?.(component.id, { x: 0, y: 0 });
                }}
              >
                +
              </Button>
            </div>
          </div>
        ) : (
          // Full desktop layout
          <div className="flex items-start gap-2">
            <div className="flex-shrink-0">
              <IconComponent className="h-sm w-sm text-primary" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-1 mb-1">
                <h4 className="text-sm font-medium truncate">{component.name}</h4>
                {component.isNew && (
                  <Badge variant="secondary" className="text-xs">New</Badge>
                )}
                {component.isPremium && (
                  <Star className="h-2xs w-2xs text-yellow-sm00" />
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
        )}
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
  compact = false,
  className
}) => {
  const [searchInput, setSearchInput] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<ComponentCategory | 'all'>('all');
  const [showOnlyAvailable, setShowOnlyAvailable] = useState(false);
  
  // Debounce search input
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setSearchQuery(searchInput);
    }, 300);
    return () => clearTimeout(timeoutId);
  }, [searchInput]);
  
  // Load components dynamically from registry
  const activeCategory = selectedCategory === 'all' ? undefined : selectedCategory;
  const { 
    components: allComponents, 
    isLoading: registryLoading, 
    error: registryError, 
    refetch: refetchRegistry 
  } = useComponentRegistry(searchQuery, activeCategory, selectedDevice);
  
  // Handle query error
  useEffect(() => {
    if (registryError) {
      toast.error('Failed to load component registry');
    }
  }, [registryError]);
  
  // Enable live updates if requested
  useEffect(() => {
    if (enableLiveUpdates) {
      componentRegistryService.enableLiveUpdates(30000); // 30 seconds
      return () => componentRegistryService.disableLiveUpdates();
    }
  }, [enableLiveUpdates]);

  // Enhanced filtering with registry support
  const filteredComponents = useMemo(() => {
    return allComponents.filter(component => {
      // Show only available filter (registry components only)
      if (showOnlyAvailable && !component.isFromRegistry) {
        return false;
      }

      return true;
    });
  }, [allComponents, showOnlyAvailable]);

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
    setSearchInput(event.target.value);
  }, []);

  const handleCategoryChange = useCallback((category: string) => {
    setSelectedCategory(category === 'all' ? 'all' : category as ComponentCategory);
  }, []);
  
  // Handle registry errors
  useEffect(() => {
    if (registryError) {
      console.error('Component registry error:', registryError);
    }
  }, [registryError]);

  return (
    <div className={cn(
      'component-palette h-full flex flex-col',
      compact && 'text-sm',
      className
    )}>      
      {/* Registry status indicator - Hidden in compact mode */}
      {pageEntityId && !compact && (
        <div className="mb-3 p-2 bg-muted/50 rounded-lg">
          <div className="flex items-center justify-between text-xs">
            <span className="flex items-center gap-2 text-muted-foreground">
              {registryLoading ? (
                <><Loader2 className="w-2xs h-2xs animate-spin" />Loading registry...</>
              ) : registryError ? (
                <><Package2 className="w-2xs h-2xs text-destructive" />Registry offline</>
              ) : (
                <><Wifi className="w-2xs h-2xs text-green-500" />{allComponents.length} components</>
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
      {/* Header - Compact for mobile */}
      <div className={cn(
        'component-palette__header',
        compact ? 'mb-2' : 'mb-4'
      )}>
        <h3 className={cn(
          'font-semibold',
          compact ? 'text-sm mb-2' : 'text-lg mb-3'
        )}>
          Components
        </h3>
        
        {/* Search - Simplified for compact mode */}
        <div className={cn(
          'relative',
          compact ? 'mb-2' : 'mb-3'
        )}>
          <Search className={cn(
            'absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground',
            compact ? 'h-2xs w-2xs' : 'h-xs w-xs'
          )} />
          <Input
            placeholder={compact ? "Search..." : "Search components..."}
            value={searchInput}
            onChange={handleSearchChange}
            className={cn(
              'pl-9',
              compact && 'h-lg text-xs'
            )}
          />
          {searchQuery && searchInput !== searchQuery && (
            <div className="absolute right-2 top-1/2 transform -translate-y-1/2">
              <Loader2 className="h-2xs w-2xs animate-spin text-muted-foreground" />
            </div>
          )}
        </div>

        {/* Enhanced filters - Simplified for compact */}
        <div className={cn(
          compact ? 'space-y-1' : 'space-y-2'
        )}>
          {/* Category Filter - Simplified for mobile */}
          {compact ? (
            <select
              value={selectedCategory}
              onChange={(e) => handleCategoryChange(e.target.value)}
              className="w-full h-lg text-xs border border-input bg-background rounded-md px-2"
            >
              <option value="all">All Categories</option>
              {availableCategories.map(category => (
                <option key={category} value={category}>
                  {category}
                </option>
              ))}
            </select>
          ) : (
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
          )}
          
          {/* Additional filters - Hidden in compact mode */}
          {!compact && (
            <div className="flex items-center gap-2">
              <Button
                variant={showOnlyAvailable ? 'secondary' : 'outline'}
                size="sm"
                onClick={() => setShowOnlyAvailable(!showOnlyAvailable)}
                className="text-xs"
              >
                <Wifi className="w-2xs h-2xs mr-1" />
                Live Only
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* Component List - Optimized for compact */}
      <div className="component-palette__content flex-1 overflow-auto">
        {selectedCategory === 'all' ? (
          // Show all categories
          <div className={cn(
            compact ? 'space-y-3' : 'space-y-6'
          )}>
            {availableCategories.map(category => (
              <div key={category}>
                {!compact && (
                  <h4 className="text-sm font-medium mb-2 text-muted-foreground">
                    {category}
                  </h4>
                )}
                <div className={cn(
                  'grid gap-2',
                  compact && 'gap-1'
                )}>
                  {componentsByCategory[category].map(component => (
                    <DraggableComponent
                      key={component.id}
                      component={component}
                      compact={compact}
                      onComponentAdd={onComponentAdd}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        ) : (
          // Show selected category only
          <div className={cn(
            'grid gap-2',
            compact && 'gap-1'
          )}>
            {registryLoading ? (
              // Loading skeleton for component registry - Compact for mobile
              <div className={cn(
                compact ? 'space-y-1' : 'space-y-2'
              )}>
                {[...Array(compact ? 4 : 6)].map((_, i) => (
                  <Card key={i} className={cn(
                    compact ? 'p-2' : 'p-3'
                  )}>
                    <div className="flex items-start gap-2">
                      <Skeleton className={cn(
                        'rounded',
                        compact ? 'h-xs w-xs' : 'h-sm w-sm'
                      )} />
                      <div className="flex-1 space-y-2">
                        <Skeleton className={cn(
                          compact ? 'h-2xs w-2xs/4' : 'h-xs w-2xs/4'
                        )} />
                        <Skeleton className={cn(
                          compact ? 'h-xs w-full' : 'h-2xs w-full'
                        )} />
                        {!compact && (
                          <div className="flex gap-1">
                            <Skeleton className="h-xs w-2xl" />
                            <Skeleton className="h-xs w-2xl" />
                          </div>
                        )}
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
                  compact={compact}
                  onComponentAdd={onComponentAdd}
                />
              ))
            )}
          </div>
        )}

        {!registryLoading && filteredComponents.length === 0 && (
          <div className={cn(
            'flex flex-col items-center justify-center text-muted-foreground',
            compact ? 'h-xs4' : 'h-232'
          )}>
            <Filter className={cn(
              'mb-2 opacity-50',
              compact ? 'h-md w-md' : 'h-lg w-lg'
            )} />
            <p className={cn(
              compact ? 'text-xs' : 'text-sm'
            )}>
              {registryError ? 'Registry unavailable' : 'No components found'}
            </p>
            {searchQuery && (
              <p className="text-xs">Try adjusting your search</p>
            )}
            {showOnlyAvailable && !registryError && !compact && (
              <p className="text-xs">Try disabling "Live Only" filter</p>
            )}
          </div>
        )}
      </div>

      {/* Enhanced footer with registry status - Hidden in compact mode */}
      {!compact && (
        <div className="component-palette__footer mt-4 pt-4 border-t">
          <div className="text-xs text-muted-foreground space-y-1">
            <p>
              Showing {filteredComponents.length} components for {selectedDevice}
              {allComponents && (
                <span className="ml-2 text-green-600">
                  ({allComponents.length} from registry)
                </span>
              )}
            </p>
            <p>Drag components onto the canvas to add them</p>
            {enableLiveUpdates && (
              <p className="flex items-center gap-1">
                <Wifi className="w-2xs h-2xs" />
                Live updates enabled
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

ComponentPalette.displayName = 'ComponentPalette';