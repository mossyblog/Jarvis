/**
 * PageBuilder - Main page builder interface
 * 
 * Provides a comprehensive UI for creating and managing Bento pages with
 * visual layout design, component arrangement, and page configuration.
 */

import React, { useState, useCallback, useMemo } from 'react';
import { 
  Eye, 
  Settings, 
  Grid3X3, 
  Save, 
  Undo, 
  Redo,
  Play,
  Share,
  Monitor,
  Tablet,
  Smartphone
} from 'lucide-react';

import type { 
  BentoPage, 
  BentoGrid, 
  DeviceType, 
  GridComponent,
  BentoLayout 
} from '@/types/bento';
import { PageStatus } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Separator } from '@/components/ui/separator';
import { BentoGrid as BentoGridComponent } from '../BentoGrid';
import { ComponentPalette } from './ComponentPalette';
import { PageSettings } from './PageSettings';
import { LayoutSelector } from './LayoutSelector';

// ============================================================================
// Types
// ============================================================================

export interface PageBuilderProps {
  /** Current page being edited */
  page?: BentoPage;
  /** Available layouts */
  layouts?: BentoLayout[];
  /** Whether the builder is in read-only mode */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
  
  // Event handlers
  /** Called when page configuration changes */
  onPageUpdate?: (page: BentoPage) => void;
  /** Called when a component is added to the grid */
  onComponentAdd?: (componentType: string, position: { x: number; y: number }) => void;
  /** Called when a component is moved */
  onComponentMove?: (componentId: string, newPosition: { x: number; y: number; w: number; h: number }) => void;
  /** Called when a component is selected */
  onComponentSelect?: (componentId: string | null) => void;
  /** Called when a component is deleted */
  onComponentDelete?: (componentId: string) => void;
  /** Called when page should be saved */
  onSave?: (page: BentoPage) => void;
  /** Called when page should be published */
  onPublish?: (page: BentoPage) => void;
  /** Called when page should be previewed */
  onPreview?: (page: BentoPage, device: DeviceType) => void;
}

interface PageBuilderState {
  currentDevice: DeviceType;
  selectedComponent: string | null;
  showGrid: boolean;
  activeTab: 'design' | 'settings' | 'layout';
  isPreviewMode: boolean;
  hasUnsavedChanges: boolean;
}

// ============================================================================
// Default Values
// ============================================================================

const createDefaultPage = (): BentoPage => ({
  id: crypto.randomUUID(),
  displayName: 'New Page',
  route: '/new-page',
  layoutId: 'default',
  status: PageStatus.Draft,
  version: 1,
  bindings: {
    security: {
      isPublic: false,
      requiredRoles: [],
      requiredPermissions: []
    },
    visibility: {
      showInNavigation: true,
      navigationOrder: 0
    }
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'current-user',
  updatedBy: 'current-user'
});

const createDefaultGrid = (device: DeviceType): BentoGrid => ({
  id: crypto.randomUUID(),
  name: `${device} Grid`,
  device,
  columns: device === 'mobile' ? 4 : device === 'tablet' ? 8 : 12,
  rows: 20,
  gap: 16,
  rowHeight: 100,
  components: [],
  settings: {
    enableSnapping: true,
    snapToGrid: true,
    enableGuides: true,
    compactMode: 'none' as any
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString()
});

// ============================================================================
// Main Component
// ============================================================================

export const PageBuilder: React.FC<PageBuilderProps> = ({
  page: initialPage,
  layouts = [],
  readOnly = false,
  className,
  onPageUpdate,
  onComponentAdd,
  onComponentMove,
  onComponentSelect,
  onComponentDelete,
  onSave,
  onPublish,
  onPreview
}) => {
  // State management
  const [page, setPage] = useState<BentoPage>(initialPage || createDefaultPage());
  const [state, setState] = useState<PageBuilderState>({
    currentDevice: 'desktop' as DeviceType,
    selectedComponent: null,
    showGrid: true,
    activeTab: 'design',
    isPreviewMode: false,
    hasUnsavedChanges: false
  });

  // Create default grids for each device
  const grids = useMemo<Record<DeviceType, BentoGrid>>(() => ({
    desktop: createDefaultGrid('desktop' as DeviceType),
    tablet: createDefaultGrid('tablet' as DeviceType),
    mobile: createDefaultGrid('mobile' as DeviceType)
  }), []);

  // Get current grid based on selected device
  const currentGrid = grids[state.currentDevice];

  // Handle device switch
  const handleDeviceChange = useCallback((device: DeviceType) => {
    setState(prev => ({ ...prev, currentDevice: device }));
  }, []);

  // Handle tab change
  const handleTabChange = useCallback((tab: string) => {
    setState(prev => ({ ...prev, activeTab: tab as PageBuilderState['activeTab'] }));
  }, []);

  // Handle page update
  const handlePageUpdate = useCallback((updates: Partial<BentoPage>) => {
    const updatedPage = { ...page, ...updates, updatedAt: new Date().toISOString() };
    setPage(updatedPage);
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    onPageUpdate?.(updatedPage);
  }, [page, onPageUpdate]);

  // Handle component operations
  const handleComponentAdd = useCallback((componentType: string, position: { x: number; y: number }) => {
    const newComponent: GridComponent = {
      id: crypto.randomUUID(),
      componentType,
      position: { ...position, w: 2, h: 2 },
      props: {},
      bindings: {},
      display: {
        visible: true,
        zIndex: 1
      }
    };

    // Add to current grid
    const updatedGrid = {
      ...currentGrid,
      components: [...currentGrid.components, newComponent]
    };

    // Update grids
    grids[state.currentDevice] = updatedGrid;
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    
    onComponentAdd?.(componentType, position);
  }, [currentGrid, state.currentDevice, grids, onComponentAdd]);

  const handleComponentMove = useCallback((componentId: string, newPosition: { x: number; y: number; w: number; h: number }) => {
    const updatedGrid = {
      ...currentGrid,
      components: currentGrid.components.map(comp => 
        comp.id === componentId 
          ? { ...comp, position: newPosition }
          : comp
      )
    };

    grids[state.currentDevice] = updatedGrid;
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    
    onComponentMove?.(componentId, newPosition);
  }, [currentGrid, state.currentDevice, grids, onComponentMove]);

  const handleComponentSelect = useCallback((componentId: string | null) => {
    setState(prev => ({ ...prev, selectedComponent: componentId }));
    onComponentSelect?.(componentId);
  }, [onComponentSelect]);

  const handleComponentDelete = useCallback((componentId: string) => {
    const updatedGrid = {
      ...currentGrid,
      components: currentGrid.components.filter(comp => comp.id !== componentId)
    };

    grids[state.currentDevice] = updatedGrid;
    setState(prev => ({ 
      ...prev, 
      selectedComponent: prev.selectedComponent === componentId ? null : prev.selectedComponent,
      hasUnsavedChanges: true 
    }));
    
    onComponentDelete?.(componentId);
  }, [currentGrid, state.currentDevice, grids, onComponentDelete]);

  // Handle manual save (placeholder - UIStudio integration not complete)
  const handleSave = useCallback(() => {
    // TODO: Implement snapshot creation with UIStudio API
    onSave?.(page);
    console.log('Save functionality placeholder');
  }, [page, onSave]);

  const handlePublish = useCallback(() => {
    const publishedPage = { ...page, status: PageStatus.Published };
    setPage(publishedPage);
    setState(prev => ({ ...prev, hasUnsavedChanges: false }));
    onPublish?.(publishedPage);
  }, [page, onPublish]);

  const handlePreview = useCallback(() => {
    onPreview?.(page, state.currentDevice);
  }, [page, state.currentDevice, onPreview]);

  const togglePreviewMode = useCallback(() => {
    setState(prev => ({ ...prev, isPreviewMode: !prev.isPreviewMode }));
  }, []);

  const toggleGridVisibility = useCallback(() => {
    setState(prev => ({ ...prev, showGrid: !prev.showGrid }));
  }, []);

  return (
    <div className={cn('page-builder flex flex-col h-full', className)}>
      {/* Toolbar */}
      <div className="page-builder__toolbar flex items-center justify-between p-4 border-b bg-card">
        <div className="flex items-center gap-4">
          <h1 className="text-lg font-semibold">{page.displayName}</h1>
          {state.hasUnsavedChanges && (
            <span className="text-xs text-muted-foreground">• Unsaved changes</span>
          )}
        </div>
        
        <div className="flex items-center gap-2">
          {/* Device Selector */}
          <div className="flex items-center gap-1 p-1 bg-muted rounded-md">
            <Button
              variant={state.currentDevice === 'desktop' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => handleDeviceChange('desktop' as DeviceType)}
            >
              <Monitor className="h-4 w-4" />
            </Button>
            <Button
              variant={state.currentDevice === 'tablet' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => handleDeviceChange('tablet' as DeviceType)}
            >
              <Tablet className="h-4 w-4" />
            </Button>
            <Button
              variant={state.currentDevice === 'mobile' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => handleDeviceChange('mobile' as DeviceType)}
            >
              <Smartphone className="h-4 w-4" />
            </Button>
          </div>
          
          <Separator orientation="vertical" className="h-6" />
          
          {/* View Controls */}
          <Button
            variant="outline"
            size="sm"
            onClick={toggleGridVisibility}
          >
            <Grid3X3 className="h-4 w-4 mr-1" />
            Grid
          </Button>
          
          <Button
            variant={state.isPreviewMode ? 'secondary' : 'outline'}
            size="sm"
            onClick={togglePreviewMode}
          >
            <Eye className="h-4 w-4 mr-1" />
            Preview
          </Button>
          
          <Separator orientation="vertical" className="h-6" />
          
          {/* Action Controls */}
          <Button variant="outline" size="sm" disabled>
            <Undo className="h-4 w-4" />
          </Button>
          
          <Button variant="outline" size="sm" disabled>
            <Redo className="h-4 w-4" />
          </Button>
          
          <Button
            variant="outline"
            size="sm"
            onClick={handleSave}
            disabled={!state.hasUnsavedChanges}
          >
            <Save className="h-4 w-4 mr-1" />
            Save
          </Button>
          
          <Button
            variant="outline"
            size="sm"
            onClick={handlePreview}
          >
            <Share className="h-4 w-4 mr-1" />
            Preview
          </Button>
          
          <Button
            size="sm"
            onClick={handlePublish}
          >
            <Play className="h-4 w-4 mr-1" />
            Publish
          </Button>
        </div>
      </div>

      {/* Main Content */}
      <div className="page-builder__content flex-1 flex overflow-hidden">
        {/* Sidebar */}
        <div className="page-builder__sidebar w-80 border-r bg-card">
          <Tabs value={state.activeTab} onValueChange={handleTabChange} className="h-full">
            <TabsList className="grid w-full grid-cols-3 m-2">
              <TabsTrigger value="design">Design</TabsTrigger>
              <TabsTrigger value="settings">Settings</TabsTrigger>
              <TabsTrigger value="layout">Layout</TabsTrigger>
            </TabsList>
            
            <TabsContent value="design" className="h-full mt-0 p-4">
              <ComponentPalette
                onComponentAdd={handleComponentAdd}
                selectedDevice={state.currentDevice}
              />
            </TabsContent>
            
            <TabsContent value="settings" className="h-full mt-0 p-4">
              <PageSettings
                page={page}
                onUpdate={handlePageUpdate}
                readOnly={readOnly}
              />
            </TabsContent>
            
            <TabsContent value="layout" className="h-full mt-0 p-4">
              <LayoutSelector
                layouts={layouts}
                selectedLayoutId={page.layoutId}
                onLayoutSelect={(layoutId) => handlePageUpdate({ layoutId })}
                readOnly={readOnly}
              />
            </TabsContent>
          </Tabs>
        </div>

        {/* Canvas Area */}
        <div className="page-builder__canvas flex-1 flex flex-col overflow-hidden">
          {/* Canvas Header */}
          <div className="canvas-header p-4 border-b bg-muted/20">
            <div className="flex items-center justify-between">
              <div className="text-sm text-muted-foreground">
                {state.currentDevice.charAt(0).toUpperCase() + state.currentDevice.slice(1)} View
                {currentGrid && (
                  <span className="ml-2">
                    • {currentGrid.columns} columns • {currentGrid.components.length} components
                  </span>
                )}
              </div>
              
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setState(prev => ({ ...prev, activeTab: 'layout' }))}
                >
                  <Settings className="h-4 w-4 mr-1" />
                  Configure Layout
                </Button>
              </div>
            </div>
          </div>

          {/* Canvas Content */}
          <div className="canvas-content flex-1 overflow-auto p-4">
            <Card className="h-full">
              <CardContent className="p-6 h-full">
                {currentGrid ? (
                  <BentoGridComponent
                    grid={currentGrid}
                    deviceType={state.currentDevice}
                    isEditing={!state.isPreviewMode && !readOnly}
                    showGrid={state.showGrid}
                    onComponentMove={handleComponentMove}
                    onComponentSelect={handleComponentSelect}
                    onComponentDelete={handleComponentDelete}
                    className="h-full"
                  />
                ) : (
                  <div className="flex items-center justify-center h-full text-muted-foreground">
                    <div className="text-center">
                      <Grid3X3 className="h-12 w-12 mx-auto mb-4 opacity-50" />
                      <p>Select a layout to begin designing your page</p>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Properties Panel */}
        {state.selectedComponent && !state.isPreviewMode && (
          <div className="page-builder__properties w-80 border-l bg-card">
            <div className="p-4 border-b">
              <h3 className="font-medium">Component Properties</h3>
              <p className="text-sm text-muted-foreground">
                Component ID: {state.selectedComponent}
              </p>
            </div>
            <div className="p-4">
              {/* Component editor would go here */}
              <p className="text-sm text-muted-foreground">
                Component editor panel will be implemented here
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

PageBuilder.displayName = 'PageBuilder';