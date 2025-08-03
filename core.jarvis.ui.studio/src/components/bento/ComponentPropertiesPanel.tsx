/**
 * ComponentPropertiesPanel - Right-side panel for component editing
 * 
 * Implements the two-tab design from the bento documentation:
 * - Bindings: GraphQL queries, property mappings, write actions
 * - Preview: Component preview with sample data
 */

import React, { useState } from 'react';
import { X, Eye, Settings, Code, ArrowRight, RefreshCw, Monitor, Tablet, Smartphone, Copy, Trash2, Move, Database } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Input } from '@/components/ui/input';
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
  
  /** Called when component should be deleted */
  onDelete?: (componentId: string) => void;
  
  /** Called when component should be duplicated */
  onDuplicate?: (componentId: string) => void;
  
  /** Called when component should be moved */
  onMove?: (componentId: string, direction: 'up' | 'down' | 'left' | 'right') => void;
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
  onDelete,
  onDuplicate,
  onMove,
}) => {
  const [activeTab, setActiveTab] = useState<'properties' | 'bindings' | 'preview'>('properties');
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
  const [componentProps, setComponentProps] = useState(component.props || {});
  const [componentStyles, setComponentStyles] = useState(component.display?.style || {});

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
        "fixed top-0 right-0 h-full w-6xl bg-background border-l border-border z-50",
        "transform transition-transform duration-300 ease-in-out",
        isOpen ? "translate-x-0" : "translate-x-full"
      )}>
        {/* Header */}
        <div className="flex items-center justify-between p-sm border-b border-border">
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
            <X className="icon-xs" />
          </Button>
        </div>

        {/* Component Actions Bar */}
        <div className="flex items-center gap-xs px-sm py-sm border-b border-border bg-muted/30">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onDuplicate?.(component.id)}
            className="h-sm px-sm"
            title="Duplicate component"
          >
            <Copy className="icon-xs" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onMove?.(component.id, 'up')}
            className="h-sm px-sm"
            title="Move up"
          >
            <Move className="icon-xs" />
          </Button>
          <div className="flex-1" />
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onDelete?.(component.id)}
            className="h-7 px-2 text-destructive hover:text-destructive"
            title="Delete component"
          >
            <Trash2 className="icon-xs" />
          </Button>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'properties' | 'bindings' | 'preview')} className="flex flex-col h-[calc(100%-122px)]">
          <TabsList className="grid w-full grid-cols-3 m-4 mb-0">
            <TabsTrigger value="properties" className="flex items-center gap-xs">
              <Settings className="icon-xs" />
              Props
            </TabsTrigger>
            <TabsTrigger value="bindings" className="flex items-center gap-xs">
              <Database className="icon-xs" />
              Data
            </TabsTrigger>
            <TabsTrigger value="preview" className="flex items-center gap-xs">
              <Eye className="icon-xs" />
              Preview
            </TabsTrigger>
          </TabsList>

          {/* Properties Tab */}
          <TabsContent value="properties" className="flex-1 m-0">
            <ScrollArea className="h-full">
              <div className="p-sm space-y-xl">
                {/* Component Info */}
                <div className="space-y-md">
                  <Label className="text-sm font-medium">Component Info</Label>
                  <div className="grid grid-cols-2 gap-md text-sm">
                    <div>
                      <span className="text-muted-foreground">Type:</span>
                      <div className="font-mono">{component.componentType}</div>
                    </div>
                    <div>
                      <span className="text-muted-foreground">ID:</span>
                      <div className="font-mono text-xs">{component.id.slice(-8)}</div>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Position:</span>
                      <div className="font-mono">{component.position.x}, {component.position.y}</div>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Size:</span>
                      <div className="font-mono">{component.position.w}×{component.position.h}</div>
                    </div>
                  </div>
                </div>

                {/* Component Properties */}
                <div className="space-y-md">
                  <Label className="text-sm font-medium">Properties</Label>
                  <div className="space-y-lg">
                    {getComponentProperties(component.componentType).map((prop) => (
                      <div key={prop.name} className="space-y-sm">
                        <Label className="text-sm">{prop.name}</Label>
                        {prop.type === 'string' && (
                          <Input
                            value={(componentProps as Record<string, unknown>)[prop.name] as string || ''}
                            onChange={(e) => {
                              const newProps = { ...componentProps, [prop.name]: e.target.value };
                              setComponentProps(newProps);
                              onUpdate?.(component.id, { props: newProps });
                            }}
                            placeholder={`Enter ${prop.name}...`}
                          />
                        )}
                        {prop.type === 'number' && (
                          <Input
                            type="number"
                            value={(componentProps as Record<string, unknown>)[prop.name] as number || 0}
                            onChange={(e) => {
                              const newProps = { ...componentProps, [prop.name]: Number(e.target.value) };
                              setComponentProps(newProps);
                              onUpdate?.(component.id, { props: newProps });
                            }}
                            placeholder={`Enter ${prop.name}...`}
                          />
                        )}
                        {prop.type === 'boolean' && (
                          <div className="flex items-center space-x-xs">
                            <input
                              type="checkbox"
                              checked={(componentProps as Record<string, unknown>)[prop.name] as boolean || false}
                              onChange={(e) => {
                                const newProps = { ...componentProps, [prop.name]: e.target.checked };
                                setComponentProps(newProps);
                                onUpdate?.(component.id, { props: newProps });
                              }}
                              className="rounded"
                            />
                            <span className="text-sm">Enable {prop.name}</span>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>

                {/* Layout & Spacing */}
                <div className="space-y-md">
                  <Label className="text-sm font-medium">Layout & Spacing</Label>
                  <div className="grid grid-cols-2 gap-md">
                    <div className="space-y-sm">
                      <Label className="text-xs">Padding (px)</Label>
                      <Input
                        type="number"
                        value={componentStyles.padding ? parseInt(String(componentStyles.padding)) : 16}
                        onChange={(e) => {
                          const newStyles = { ...componentStyles, padding: `${e.target.value}px` };
                          setComponentStyles(newStyles);
                          onUpdate?.(component.id, { display: { ...component.display, style: newStyles } });
                        }}
                        min={0}
                        max={48}
                        className="w-full"
                      />
                    </div>
                    <div className="space-y-sm">
                      <Label className="text-xs">Border Radius (px)</Label>
                      <Input
                        type="number"
                        value={componentStyles.borderRadius ? parseInt(String(componentStyles.borderRadius)) : 8}
                        onChange={(e) => {
                          const newStyles = { ...componentStyles, borderRadius: `${e.target.value}px` };
                          setComponentStyles(newStyles);
                          onUpdate?.(component.id, { display: { ...component.display, style: newStyles } });
                        }}
                        min={0}
                        max={24}
                        className="w-full"
                      />
                    </div>
                  </div>
                </div>

                {/* Styling */}
                <div className="space-y-md">
                  <Label className="text-sm font-medium">Styling</Label>
                  <div className="space-y-lg">
                    <div className="space-y-sm">
                      <Label className="text-xs">Background Color</Label>
                      <div className="flex gap-xs">
                        <Button
                          variant="outline"
                          size="sm"
                          className="flex-1 justify-start"
                          onClick={() => {
                            // Color picker functionality would go here
                            const newStyles = { ...componentStyles, backgroundColor: '#f3f4f6' };
                            setComponentStyles(newStyles);
                            onUpdate?.(component.id, { display: { ...component.display, style: newStyles } });
                          }}
                        >
                          <div className="w-xs h-xs rounded border mr-2" style={{ backgroundColor: componentStyles.backgroundColor || '#ffffff' }} />
                          {componentStyles.backgroundColor || 'Default'}
                        </Button>
                      </div>
                    </div>
                    
                    <div className="space-y-sm">
                      <Label className="text-xs">Text Color</Label>
                      <div className="flex gap-xs">
                        <Button
                          variant="outline"
                          size="sm"
                          className="flex-1 justify-start"
                          onClick={() => {
                            // Color picker functionality would go here
                            const newStyles = { ...componentStyles, color: '#1f2937' };
                            setComponentStyles(newStyles);
                            onUpdate?.(component.id, { display: { ...component.display, style: newStyles } });
                          }}
                        >
                          <div className="w-xs h-xs rounded border mr-2" style={{ backgroundColor: componentStyles.color || '#000000' }} />
                          {componentStyles.color || 'Default'}
                        </Button>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Advanced */}
                <Collapsible>
                  <CollapsibleTrigger className="flex items-center justify-between w-full p-sm text-sm font-medium hover:bg-muted rounded">
                    Advanced Settings
                    <ArrowRight className="icon-xs" />
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <div className="space-y-lg mt-3 p-md bg-muted/30 rounded">
                      <div className="space-y-sm">
                        <Label className="text-xs">Custom CSS Class</Label>
                        <Input
                          placeholder="custom-class-name"
                          value={component.display?.className || ''}
                          onChange={(e) => {
                            onUpdate?.(component.id, { 
                              display: { 
                                ...component.display, 
                                className: e.target.value 
                              } 
                            });
                          }}
                        />
                      </div>
                      
                      <div className="space-y-sm">
                        <Label className="text-xs">Z-Index</Label>
                        <Input
                          type="number"
                          value={component.display?.zIndex || 1}
                          onChange={(e) => {
                            onUpdate?.(component.id, { 
                              display: { 
                                ...component.display, 
                                zIndex: Number(e.target.value) 
                              } 
                            });
                          }}
                          min={-10}
                          max={50}
                          className="w-full"
                        />
                      </div>
                    </div>
                  </CollapsibleContent>
                </Collapsible>
              </div>
            </ScrollArea>
          </TabsContent>

          {/* Bindings Tab */}
          <TabsContent value="bindings" className="flex-1 m-0">
            <ScrollArea className="h-full">
              <div className="p-sm space-y-xl">
                {/* Read Query Section */}
                <div className="space-y-md">
                  <div className="flex items-center justify-between">
                    <Label className="text-sm font-medium">Read Query (GraphQL)</Label>
                    <Button variant="outline" size="sm">
                      Test Query
                    </Button>
                  </div>
                  <Textarea
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    className="font-mono text-xs h-3xl resize-none"
                    placeholder="Enter GraphQL query..."
                  />
                </div>

                {/* Property Mappings */}
                <div className="space-y-md">
                  <Label className="text-sm font-medium">Property Mappings</Label>
                  <div className="space-y-md">
                    {componentProperties.map((prop) => (
                      <div key={prop.name} className="flex items-center gap-md p-md border border-border rounded-lg">
                        <div className="flex-1">
                          <div className="flex items-center gap-xs">
                            <Label className="text-sm">{prop.name}</Label>
                            <Badge variant="outline" className="text-xs">
                              {prop.type}
                            </Badge>
                          </div>
                        </div>
                        
                        <ArrowRight className="icon-xs text-muted-foreground" />
                        
                        <div className="flex-1">
                          <Select defaultValue="">
                            <SelectTrigger className="h-lg">
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
                        
                        <Button variant="ghost" size="sm" className="h-lg w-lg p-0">
                          <Code className="icon-xs" />
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Write Actions */}
                <div className="space-y-md">
                  <Label className="text-sm font-medium">Write Actions</Label>
                  <Button
                    variant="outline"
                    className="w-full justify-start"
                    onClick={() => {
                      // TODO: Open write configuration modal
                      console.log('Configure write actions for', component.id);
                    }}
                  >
                    <Settings className="mr-2 icon-xs" />
                    Configure Write Actions
                  </Button>
                  
                  {/* Write Actions Summary */}
                  <div className="text-xs text-muted-foreground p-md bg-muted rounded-lg">
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
              <div className="p-sm space-y-lg">
                {/* Preview Controls */}
                <div className="flex items-center justify-between">
                  <Label className="text-sm font-medium">Preview Mode</Label>
                  <div className="flex items-center gap-xs">
                    <Button
                      variant={previewMode === 'desktop' ? 'default' : 'ghost'}
                      size="sm"
                      className="h-lg w-lg p-0"
                      onClick={() => setPreviewMode('desktop')}
                    >
                      <Monitor className="icon-xs" />
                    </Button>
                    <Button
                      variant={previewMode === 'tablet' ? 'default' : 'ghost'}
                      size="sm"
                      className="h-lg w-lg p-0"
                      onClick={() => setPreviewMode('tablet')}
                    >
                      <Tablet className="icon-xs" />
                    </Button>
                    <Button
                      variant={previewMode === 'mobile' ? 'default' : 'ghost'}
                      size="sm"
                      className="h-lg w-lg p-0"
                      onClick={() => setPreviewMode('mobile')}
                    >
                      <Smartphone className="icon-xs" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 w-8 p-0 ml-2"
                    >
                      <RefreshCw className="icon-xs" />
                    </Button>
                  </div>
                </div>

                {/* Preview Viewport */}
                <div className="border border-border rounded-lg p-sm bg-muted/30">
                  <div className={cn(
                    "mx-auto rounded border border-border bg-background overflow-hidden",
                    previewMode === 'desktop' && "w-full",
                    previewMode === 'tablet' && "w-4xl",
                    previewMode === 'mobile' && "w-3xl"
                  )}>
                    <div className="p-md">
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
                  <CollapsibleTrigger className="flex items-center justify-between w-full p-sm text-sm font-medium hover:bg-muted rounded">
                    Sample Data
                    <RefreshCw className="icon-xs" />
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <pre className="text-xs p-md bg-muted rounded-lg mt-2 overflow-auto">
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