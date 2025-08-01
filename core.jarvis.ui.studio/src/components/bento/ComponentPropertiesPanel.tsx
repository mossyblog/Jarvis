/**
 * ComponentPropertiesPanel - Right-side panel for component editing
 * 
 * Implements the two-tab design from the bento documentation:
 * - Bindings: GraphQL queries, property mappings, write actions
 * - Preview: Component preview with sample data
 */

import React, { useState } from 'react';
import { X, Link, Eye, Settings, Code, ArrowRight, RefreshCw, Monitor, Tablet, Smartphone } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { ScrollArea } from '@/components/ui/scroll-area';
import { cn } from '@/lib/utils';
import type { GridComponent } from '@/types/bento';
import { ComponentRenderer } from './ComponentRenderer';
import { DeviceType } from '@/types/bento';

// ============================================================================
// Types
// ============================================================================

interface ComponentPropertiesPanelProps {
  /** The component being edited */
  component: GridComponent;
  
  /** Whether the panel is open */
  isOpen: boolean;
  
  /** Called when panel should close */
  onClose: () => void;
  
  /** Called when component is updated */
  onUpdate?: (componentId: string, updates: Partial<GridComponent>) => void;
}

// Mock property mappings for demo (currently unused but planned for future implementation)
// interface PropertyMapping {
//   componentProp: string;
//   queryPath: string;
//   transform?: string;
// }

// ============================================================================
// Main Component
// ============================================================================

export const ComponentPropertiesPanel: React.FC<ComponentPropertiesPanelProps> = ({
  component,
  isOpen,
  onClose,
  onUpdate,
}) => {
  const [activeTab, setActiveTab] = useState<'bindings' | 'preview'>('bindings');
  const [query, setQuery] = useState(
    `query GetComponentData {
  metrics {
    ${component.componentType === 'metric' ? `
    value
    previousValue
    trend
    percentage` : 'data'}
  }
}`
  );
  const [previewMode, setPreviewMode] = useState<'desktop' | 'tablet' | 'mobile'>('desktop');

  // Mock property mappings based on component type
  const getComponentProperties = (componentType: string) => {
    switch (componentType) {
      case 'metric':
        return [
          { name: 'title', type: 'string' },
          { name: 'value', type: 'number' },
          { name: 'change', type: 'string' },
          { name: 'trend', type: 'string' },
        ];
      case 'chart':
        return [
          { name: 'title', type: 'string' },
          { name: 'data', type: 'array' },
          { name: 'type', type: 'string' },
        ];
      default:
        return [
          { name: 'title', type: 'string' },
          { name: 'content', type: 'string' },
        ];
    }
  };

  const componentProperties = getComponentProperties(component.componentType);

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div 
        className="fixed inset-0 bg-black/20 backdrop-blur-sm z-40"
        onClick={onClose}
      />
      
      {/* Panel */}
      <div className={cn(
        "fixed top-0 right-0 h-full w-96 bg-background border-l border-border z-50",
        "transform transition-transform duration-300 ease-in-out",
        isOpen ? "translate-x-0" : "translate-x-full"
      )}>
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border">
          <div>
            <h2 className="text-lg font-semibold">Component Properties</h2>
            <p className="text-sm text-muted-foreground">
              {component.componentType} • {component.id.slice(-8)}
            </p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-8 w-8 p-0"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'bindings' | 'preview')} className="flex flex-col h-[calc(100%-73px)]">
          <TabsList className="grid w-full grid-cols-2 m-4 mb-0">
            <TabsTrigger value="bindings" className="flex items-center gap-2">
              <Link className="h-4 w-4" />
              Bindings
            </TabsTrigger>
            <TabsTrigger value="preview" className="flex items-center gap-2">
              <Eye className="h-4 w-4" />
              Preview
            </TabsTrigger>
          </TabsList>

          {/* Bindings Tab */}
          <TabsContent value="bindings" className="flex-1 m-0">
            <ScrollArea className="h-full">
              <div className="p-4 space-y-6">
                {/* Read Query Section */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <Label className="text-sm font-medium">Read Query (GraphQL)</Label>
                    <Button variant="outline" size="sm">
                      Test Query
                    </Button>
                  </div>
                  <Textarea
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    className="font-mono text-xs h-32 resize-none"
                    placeholder="Enter GraphQL query..."
                  />
                </div>

                {/* Property Mappings */}
                <div className="space-y-3">
                  <Label className="text-sm font-medium">Property Mappings</Label>
                  <div className="space-y-3">
                    {componentProperties.map((prop) => (
                      <div key={prop.name} className="flex items-center gap-3 p-3 border border-border rounded-lg">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <Label className="text-sm">{prop.name}</Label>
                            <Badge variant="outline" className="text-xs">
                              {prop.type}
                            </Badge>
                          </div>
                        </div>
                        
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                        
                        <div className="flex-1">
                          <Select defaultValue="">
                            <SelectTrigger className="h-8">
                              <SelectValue placeholder="Select field" />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="metrics.value">metrics.value</SelectItem>
                              <SelectItem value="metrics.previousValue">metrics.previousValue</SelectItem>
                              <SelectItem value="metrics.trend">metrics.trend</SelectItem>
                              <SelectItem value="metrics.percentage">metrics.percentage</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                        
                        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                          <Code className="h-3 w-3" />
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Write Actions */}
                <div className="space-y-3">
                  <Label className="text-sm font-medium">Write Actions</Label>
                  <Button
                    variant="outline"
                    className="w-full justify-start"
                    onClick={() => {
                      // TODO: Open write configuration modal
                      console.log('Configure write actions for', component.id);
                    }}
                  >
                    <Settings className="mr-2 h-4 w-4" />
                    Configure Write Actions
                  </Button>
                  
                  {/* Write Actions Summary */}
                  <div className="text-xs text-muted-foreground p-3 bg-muted rounded-lg">
                    <p>No write actions configured.</p>
                    <p>Click above to map component interactions to ECS components.</p>
                  </div>
                </div>

                {/* Apply Changes */}
                <div className="pt-4 border-t border-border">
                  <Button 
                    className="w-full"
                    onClick={() => {
                      console.log('Apply bindings for', component.id);
                      // TODO: Save bindings to component
                      onUpdate?.(component.id, {
                        // bindings: {
                        //   readQuery: query,
                        //   propertyMappings: [], // TODO: Get actual mappings
                        // }
                      });
                    }}
                  >
                    Apply Bindings
                  </Button>
                </div>
              </div>
            </ScrollArea>
          </TabsContent>

          {/* Preview Tab */}
          <TabsContent value="preview" className="flex-1 m-0">
            <ScrollArea className="h-full">
              <div className="p-4 space-y-4">
                {/* Preview Controls */}
                <div className="flex items-center justify-between">
                  <Label className="text-sm font-medium">Preview Mode</Label>
                  <div className="flex items-center gap-1">
                    <Button
                      variant={previewMode === 'desktop' ? 'default' : 'ghost'}
                      size="sm"
                      className="h-8 w-8 p-0"
                      onClick={() => setPreviewMode('desktop')}
                    >
                      <Monitor className="h-4 w-4" />
                    </Button>
                    <Button
                      variant={previewMode === 'tablet' ? 'default' : 'ghost'}
                      size="sm"
                      className="h-8 w-8 p-0"
                      onClick={() => setPreviewMode('tablet')}
                    >
                      <Tablet className="h-4 w-4" />
                    </Button>
                    <Button
                      variant={previewMode === 'mobile' ? 'default' : 'ghost'}
                      size="sm"
                      className="h-8 w-8 p-0"
                      onClick={() => setPreviewMode('mobile')}
                    >
                      <Smartphone className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 w-8 p-0 ml-2"
                    >
                      <RefreshCw className="h-4 w-4" />
                    </Button>
                  </div>
                </div>

                {/* Preview Viewport */}
                <div className="border border-border rounded-lg p-4 bg-muted/30">
                  <div className={cn(
                    "mx-auto rounded border border-border bg-background overflow-hidden",
                    previewMode === 'desktop' && "w-full",
                    previewMode === 'tablet' && "w-64",
                    previewMode === 'mobile' && "w-48"
                  )}>
                    <div className="p-3">
                      <ComponentRenderer
                        component={component}
                        gridSize={{
                          w: component.position.w,
                          h: component.position.h,
                        }}
                        deviceType={previewMode === 'desktop' ? DeviceType.Desktop : 
                                   previewMode === 'tablet' ? DeviceType.Tablet : DeviceType.Mobile}
                      />
                    </div>
                  </div>
                </div>

                {/* Sample Data */}
                <Collapsible>
                  <CollapsibleTrigger className="flex items-center justify-between w-full p-2 text-sm font-medium hover:bg-muted rounded">
                    Sample Data
                    <RefreshCw className="h-4 w-4" />
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <pre className="text-xs p-3 bg-muted rounded-lg mt-2 overflow-auto">
{JSON.stringify({
  metrics: {
    value: component.props?.value || "1,234", 
    previousValue: "1,100",
    trend: "up",
    percentage: "+12.2%"
  }
}, null, 2)}
                    </pre>
                  </CollapsibleContent>
                </Collapsible>

                {/* Component Info */}
                <div className="pt-4 border-t border-border">
                  <div className="text-xs text-muted-foreground space-y-1">
                    <div><span className="font-medium">Type:</span> {component.componentType}</div>
                    <div><span className="font-medium">ID:</span> {component.id}</div>
                    <div><span className="font-medium">Position:</span> {component.position.x}, {component.position.y}</div>
                    <div><span className="font-medium">Size:</span> {component.position.w}×{component.position.h}</div>
                  </div>
                </div>
              </div>
            </ScrollArea>
          </TabsContent>
        </Tabs>
      </div>
    </>
  );
};

ComponentPropertiesPanel.displayName = 'ComponentPropertiesPanel';