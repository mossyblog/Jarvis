/**
 * PreviewPanel - Component preview with device switching
 * 
 * Provides a preview interface for components with device-specific views,
 * sample data rendering, and interactive preview controls.
 */

import React, { useState, useCallback, useMemo, useEffect } from 'react';
import {
  Monitor,
  Tablet,
  Smartphone,
  RefreshCw,
  Eye,
  Download,
  Share,
  ChevronDown,
  Database
} from 'lucide-react';

import type { GridComponent, DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { ComponentRenderer } from '../ComponentRenderer';

// ============================================================================
// Types
// ============================================================================

export interface PreviewPanelProps {
  /** Component to preview */
  component: GridComponent;
  /** Current device for responsive preview */
  currentDevice?: DeviceType;
  /** Called when device selection changes */
  onDeviceChange?: (device: DeviceType) => void;
  /** Additional CSS classes */
  className?: string;
}

interface PreviewState {
  isLoading: boolean;
  sampleData: unknown;
  error: string | null;
  showData: boolean;
  dataMode: 'live' | 'sample' | 'empty';
}

// ============================================================================
// Sample Data Generator
// ============================================================================

const generateSampleData = (componentType: string): unknown => {
  switch (componentType) {
    case 'metric-card':
      return {
        title: 'Revenue',
        value: 125450,
        previousValue: 112300,
        trend: 'up',
        percentage: 11.7,
        format: 'currency'
      };
      
    case 'line-chart':
      return {
        data: [
          { date: '2024-01-01', value: 100 },
          { date: '2024-01-02', value: 120 },
          { date: '2024-01-03', value: 90 },
          { date: '2024-01-04', value: 140 },
          { date: '2024-01-05', value: 160 },
          { date: '2024-01-06', value: 130 },
          { date: '2024-01-07', value: 180 }
        ],
        xField: 'date',
        yField: 'value',
        title: 'Weekly Performance'
      };
      
    case 'user-list':
      return {
        users: [
          { id: 1, name: 'John Doe', email: 'john@example.com', status: 'online', avatar: '/avatars/john.jpg' },
          { id: 2, name: 'Jane Smith', email: 'jane@example.com', status: 'offline', avatar: '/avatars/jane.jpg' },
          { id: 3, name: 'Bob Johnson', email: 'bob@example.com', status: 'online', avatar: '/avatars/bob.jpg' },
          { id: 4, name: 'Alice Brown', email: 'alice@example.com', status: 'away', avatar: '/avatars/alice.jpg' }
        ],
        showAvatar: true,
        showStatus: true
      };
      
    case 'data-table':
      return {
        data: [
          { id: 1, name: 'Product A', category: 'Electronics', price: 299.99, stock: 45 },
          { id: 2, name: 'Product B', category: 'Clothing', price: 79.99, stock: 120 },
          { id: 3, name: 'Product C', category: 'Books', price: 19.99, stock: 200 },
          { id: 4, name: 'Product D', category: 'Electronics', price: 599.99, stock: 12 }
        ],
        columns: [
          { key: 'name', title: 'Name', sortable: true },
          { key: 'category', title: 'Category', sortable: true },
          { key: 'price', title: 'Price', sortable: true },
          { key: 'stock', title: 'Stock', sortable: true }
        ],
        pagination: { total: 4, page: 1, pageSize: 10 }
      };
      
    case 'text-block':
      return {
        content: 'This is a sample text block with some content to demonstrate how the component renders text.',
        variant: 'p',
        align: 'left'
      };
      
    default:
      return {
        text: `Sample data for ${componentType}`,
        value: Math.floor(Math.random() * 1000),
        items: ['Item 1', 'Item 2', 'Item 3']
      };
  }
};

// ============================================================================
// Device Frame Component
// ============================================================================

interface DeviceFrameProps {
  device: DeviceType;
  children: React.ReactNode;
  className?: string;
}

const DeviceFrame: React.FC<DeviceFrameProps> = ({ device, children, className }) => {
  const frameStyles = useMemo(() => {
    switch (device) {
      case 'desktop':
        return {
          width: '100%',
          maxWidth: '1200px',
          aspectRatio: '16/10',
          padding: '20px'
        };
      case 'tablet':
        return {
          width: '100%',
          maxWidth: '768px',
          aspectRatio: '4/3',
          padding: '16px'
        };
      case 'mobile':
        return {
          width: '100%',
          maxWidth: '375px',
          aspectRatio: '9/16',
          padding: '12px'
        };
    }
  }, [device]);

  return (
    <div
      className={cn(
        'device-frame mx-auto bg-background border rounded-lg overflow-hidden',
        device === 'desktop' && 'border-2',
        device === 'tablet' && 'border-2 shadow-md',
        device === 'mobile' && 'border-2 shadow-lg rounded-3xl',
        className
      )}
      style={frameStyles}
    >
      {device === 'mobile' && (
        <div className="bg-muted h-1 w-16 mx-auto mb-2 rounded-full" />
      )}
      <div className="h-full w-full overflow-auto">
        {children}
      </div>
      {device === 'mobile' && (
        <div className="bg-muted h-1 w-8 mx-auto mt-2 rounded-full" />
      )}
    </div>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const PreviewPanel: React.FC<PreviewPanelProps> = ({
  component,
  currentDevice = 'desktop' as DeviceType,
  onDeviceChange,
  className
}) => {
  const [state, setState] = useState<PreviewState>({
    isLoading: false,
    sampleData: null,
    error: null,
    showData: false,
    dataMode: 'sample'
  });

  // Generate sample data on mount and when component type changes
  useEffect(() => {
    setState(prev => ({
      ...prev,
      isLoading: true
    }));

    // Simulate data loading
    const timer = setTimeout(() => {
      const sampleData = generateSampleData(component.componentType);
      setState(prev => ({
        ...prev,
        isLoading: false,
        sampleData,
        error: null
      }));
    }, 500);

    return () => clearTimeout(timer);
  }, [component.componentType]);

  // Device selector options
  const deviceOptions = [
    { value: 'desktop' as DeviceType, label: 'Desktop', icon: Monitor },
    { value: 'tablet' as DeviceType, label: 'Tablet', icon: Tablet },
    { value: 'mobile' as DeviceType, label: 'Mobile', icon: Smartphone }
  ];

  // Handle device change
  const handleDeviceChange = useCallback((device: DeviceType) => {
    onDeviceChange?.(device);
  }, [onDeviceChange]);

  // Handle data mode change
  const handleDataModeChange = useCallback((mode: PreviewState['dataMode']) => {
    setState(prev => ({ ...prev, dataMode: mode }));
  }, []);

  // Refresh sample data
  const handleRefreshData = useCallback(() => {
    setState(prev => ({ ...prev, isLoading: true }));
    
    setTimeout(() => {
      const sampleData = generateSampleData(component.componentType);
      setState(prev => ({
        ...prev,
        isLoading: false,
        sampleData
      }));
    }, 300);
  }, [component.componentType]);

  // Get preview data based on mode
  const previewData = useMemo(() => {
    switch (state.dataMode) {
      case 'live':
        // In a real implementation, this would fetch live data
        return state.sampleData;
      case 'sample':
        return state.sampleData;
      case 'empty':
        return null;
      default:
        return state.sampleData;
    }
  }, [state.dataMode, state.sampleData]);

  // Create preview component with data
  const previewComponent = useMemo(() => {
    const mappedProps: Record<string, unknown> = { ...component.props };
    
    // Apply property mappings if available
    if (component.bindings?.propertyMappings && previewData) {
      component.bindings.propertyMappings.forEach(mapping => {
        const value = getNestedValue(previewData, mapping.queryPath);
        if (value !== undefined) {
          mappedProps[mapping.componentProp] = mapping.transform 
            ? evaluateTransform(value, mapping.transform)
            : value;
        }
      });
    }

    return {
      ...component,
      props: mappedProps
    };
  }, [component, previewData]);

  return (
    <div className={cn('preview-panel space-y-4', className)}>
      {/* Header */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold flex items-center gap-2">
            <Eye className="h-5 w-5" />
            Component Preview
          </h3>
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefreshData}
            disabled={state.isLoading}
          >
            <RefreshCw className={cn('h-4 w-4 mr-1', state.isLoading && 'animate-spin')} />
            Refresh
          </Button>
        </div>

        {/* Controls */}
        <div className="flex items-center justify-between">
          {/* Device Selector */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Device:</span>
            <div className="flex items-center gap-1 p-1 bg-muted rounded-md">
              {deviceOptions.map((option) => {
                const IconComponent = option.icon;
                return (
                  <Button
                    key={option.value}
                    variant={currentDevice === option.value ? 'secondary' : 'ghost'}
                    size="sm"
                    onClick={() => handleDeviceChange(option.value)}
                    className="h-8"
                  >
                    <IconComponent className="h-4 w-4" />
                  </Button>
                );
              })}
            </div>
          </div>

          {/* Data Mode Selector */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Data:</span>
            <Select value={state.dataMode} onValueChange={handleDataModeChange}>
              <SelectTrigger className="w-32">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="sample">Sample</SelectItem>
                <SelectItem value="live">Live</SelectItem>
                <SelectItem value="empty">Empty</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Preview Area */}
      <Card>
        <CardContent className="p-6">
          <DeviceFrame device={currentDevice}>
            {state.isLoading ? (
              <div className="flex items-center justify-center h-32">
                <div className="text-center">
                  <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-2 text-muted-foreground" />
                  <p className="text-sm text-muted-foreground">Loading preview...</p>
                </div>
              </div>
            ) : state.error ? (
              <div className="flex items-center justify-center h-32">
                <div className="text-center">
                  <div className="text-destructive mb-2">Preview Error</div>
                  <p className="text-sm text-muted-foreground">{state.error}</p>
                </div>
              </div>
            ) : (
              <ComponentRenderer
                component={previewComponent}
                gridSize={{ w: component.position.w, h: component.position.h }}
                deviceType={currentDevice}
                loading={state.isLoading}
              />
            )}
          </DeviceFrame>
        </CardContent>
      </Card>

      {/* Preview Info */}
      <div className="grid grid-cols-2 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Component Info</CardTitle>
          </CardHeader>
          <CardContent className="text-sm space-y-1">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Type:</span>
              <Badge variant="outline">{component.componentType}</Badge>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Size:</span>
              <span>{component.position.w}×{component.position.h}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Position:</span>
              <span>({component.position.x}, {component.position.y})</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Data Bindings</CardTitle>
          </CardHeader>
          <CardContent className="text-sm space-y-1">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Query:</span>
              <Badge variant={component.bindings?.readQuery ? 'secondary' : 'outline'}>
                {component.bindings?.readQuery ? 'Yes' : 'No'}
              </Badge>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Mappings:</span>
              <span>{component.bindings?.propertyMappings?.length || 0}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Write Actions:</span>
              <Badge variant={component.bindings?.write ? 'secondary' : 'outline'}>
                {component.bindings?.write ? 'Yes' : 'No'}
              </Badge>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Sample Data */}
      <Collapsible open={state.showData} onOpenChange={(open) => setState(prev => ({ ...prev, showData: open }))}>
        <Card>
          <CollapsibleTrigger asChild>
            <CardHeader className="pb-2 cursor-pointer hover:bg-muted/50 transition-colors">
              <CardTitle className="text-sm flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Database className="h-4 w-4" />
                  Sample Data
                </div>
                <ChevronDown className={cn('h-4 w-4 transition-transform', state.showData && 'transform rotate-180')} />
              </CardTitle>
            </CardHeader>
          </CollapsibleTrigger>
          <CollapsibleContent>
            <CardContent className="pt-0">
              <div className="bg-muted/50 rounded-md p-3">
                <pre className="text-xs overflow-auto max-h-48">
                  {JSON.stringify(previewData, null, 2)}
                </pre>
              </div>
            </CardContent>
          </CollapsibleContent>
        </Card>
      </Collapsible>

      {/* Actions */}
      <div className="flex items-center justify-between pt-2 border-t">
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled>
            <Share className="h-4 w-4 mr-1" />
            Share Preview
          </Button>
          <Button variant="outline" size="sm" disabled>
            <Download className="h-4 w-4 mr-1" />
            Export
          </Button>
        </div>
        
        <div className="text-xs text-muted-foreground">
          Preview updated {new Date().toLocaleTimeString()}
        </div>
      </div>
    </div>
  );
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Get nested value from object using dot notation
 */
function getNestedValue(obj: unknown, path: string): unknown {
  if (!obj || typeof obj !== 'object') return undefined;
  
  const keys = path.split('.');
  let current = obj;
  
  for (const key of keys) {
    if (current && typeof current === 'object' && key in current) {
      current = (current as Record<string, unknown>)[key] as Record<string, unknown>;
    } else {
      return undefined;
    }
  }
  
  return current;
}

/**
 * Evaluate transform expression (simplified)
 */
function evaluateTransform(value: unknown, transform: string): unknown {
  try {
    // Create a simple evaluation context
    // In a real implementation, this would use a safe expression evaluator
    const func = new Function('value', `return ${transform}`);
    return func(value);
  } catch (error) {
    console.warn('Transform evaluation failed:', error);
    return value;
  }
}

PreviewPanel.displayName = 'PreviewPanel';