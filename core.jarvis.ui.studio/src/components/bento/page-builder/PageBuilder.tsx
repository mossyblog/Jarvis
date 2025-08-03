/**
 * PageBuilder - Main page builder interface
 * 
 * Provides a comprehensive UI for creating and managing Bento pages with
 * visual layout design, component arrangement, and page configuration.
 */

import React, { useState, useCallback, useMemo, useEffect } from 'react';
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
  
  // Device and responsive props
  /** Current device type */
  currentDevice?: DeviceType;
  /** Called when device changes */
  onDeviceChange?: (device: DeviceType) => void;
  
  // Collaboration props
  /** Whether collaboration is enabled */
  collaborationEnabled?: boolean;
  /** Number of connected users */
  connectedUsers?: number;
  /** Connection status */
  isConnected?: boolean;
  
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
  isMobileLayout: boolean;
  showMobileMenu: boolean;
  touchGesturesEnabled: boolean;
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
    compactMode: 'none' as 'none' | 'horizontal' | 'vertical'
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
  currentDevice: externalCurrentDevice,
  onDeviceChange,
  collaborationEnabled = false,
  connectedUsers = 1,
  isConnected = true,
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
    currentDevice: externalCurrentDevice || 'desktop' as DeviceType,
    selectedComponent: null,
    showGrid: true,
    activeTab: 'design',
    isPreviewMode: false,
    hasUnsavedChanges: false,
    isMobileLayout: (externalCurrentDevice || 'desktop') === 'mobile',
    showMobileMenu: false,
    touchGesturesEnabled: 'ontouchstart' in window
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
    setState(prev => ({ 
      ...prev, 
      currentDevice: device,
      isMobileLayout: device === 'mobile',
      showMobileMenu: false // Close mobile menu when switching devices
    }));
    onDeviceChange?.(device);
  }, [onDeviceChange]);

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
  
  const toggleMobileMenu = useCallback(() => {
    setState(prev => ({ ...prev, showMobileMenu: !prev.showMobileMenu }));
  }, []);
  
  // Touch gesture handlers
  const handleLongPress = useCallback((detail: { duration: number; x: number; y: number }) => {
    if (state.isMobileLayout && !readOnly) {
      // Open component palette or context menu on long press
      setState(prev => ({ ...prev, showMobileMenu: true }));
    }
  }, [state.isMobileLayout, readOnly]);
  
  const handleSwipe = useCallback((detail: { direction: string; velocity: number; distance: number }) => {
    if (state.isMobileLayout) {
      if (detail.direction === 'up' && detail.velocity > 0.5) {
        // Swipe up to open component palette
        setState(prev => ({ ...prev, showMobileMenu: true }));
      } else if (detail.direction === 'down' && detail.velocity > 0.5) {
        // Swipe down to close mobile menu
        setState(prev => ({ ...prev, showMobileMenu: false }));
      }
    }
  }, [state.isMobileLayout]);
  
  const handlePinch = useCallback((detail: { scale: number; rotation: number; center: { x: number; y: number } }) => {
    if (state.isMobileLayout && detail.scale > 1.2) {
      // Pinch to zoom - toggle preview mode
      setState(prev => ({ ...prev, isPreviewMode: !prev.isPreviewMode }));
    }
  }, [state.isMobileLayout]);

  // Sync external device changes
  useEffect(() => {
    if (externalCurrentDevice && externalCurrentDevice !== state.currentDevice) {
      setState(prev => ({ 
        ...prev, 
        currentDevice: externalCurrentDevice,
        isMobileLayout: externalCurrentDevice === 'mobile'
      }));
    }
  }, [externalCurrentDevice, state.currentDevice]);
  
  return (
    <div className={cn(
      'page-builder flex h-full',
      state.isMobileLayout ? 'flex-col' : 'flex-col',
      className
    )}>
      {/* Toolbar - Mobile Optimized */}
      <div className={cn(
        'page-builder__toolbar flex items-center justify-between border-b bg-card',
        state.isMobileLayout ? 'p-sm' : 'p-sm'
      )}>
        <div className="flex items-center gap-xs sm:gap-sm min-w-0">
          <h1 className={cn(
            'font-semibold truncate',
            state.isMobileLayout ? 'text-sm' : 'text-lg'
          )}>
            {page.displayName}
          </h1>
          {state.hasUnsavedChanges && (
            <span className="text-xs text-muted-foreground whitespace-nowrap">
              • Unsaved
            </span>
          )}
          {collaborationEnabled && (
            <div className="flex items-center gap-xs">
              <div className={cn(
                'w-2 h-2 rounded-full',
                isConnected ? 'bg-green-500' : 'bg-red-500'
              )} />
              <span className="text-xs text-muted-foreground">
                {connectedUsers}
              </span>
            </div>
          )}
        </div>
        
        <div className="flex items-center gap-xs">
          {/* Mobile Menu Toggle (Mobile Only) */}
          {state.isMobileLayout && (
            <Button
              variant="outline"
              size="sm"
              onClick={toggleMobileMenu}
              className="lg:hidden"
            >
              <Grid3X3 className="h-4 w-4" />
            </Button>
          )}
          
          {/* Device Selector - Hidden on Mobile */}
          <div className={cn(
            'flex items-center gap-xs p-xs bg-muted rounded-md',
            state.isMobileLayout ? 'hidden' : 'flex'
          )}>
            <Button
              variant={state.currentDevice === 'desktop' ? 'secondary' : 'ghost'}
              size={state.isMobileLayout ? 'sm' : 'sm'}
              onClick={() => handleDeviceChange('desktop' as DeviceType)}
            >
              <Monitor className="h-3 w-3 sm:h-4 sm:w-4" />
            </Button>
            <Button
              variant={state.currentDevice === 'tablet' ? 'secondary' : 'ghost'}
              size={state.isMobileLayout ? 'sm' : 'sm'}
              onClick={() => handleDeviceChange('tablet' as DeviceType)}
            >
              <Tablet className="h-3 w-3 sm:h-4 sm:w-4" />
            </Button>
            <Button
              variant={state.currentDevice === 'mobile' ? 'secondary' : 'ghost'}
              size={state.isMobileLayout ? 'sm' : 'sm'}
              onClick={() => handleDeviceChange('mobile' as DeviceType)}
            >
              <Smartphone className="h-3 w-3 sm:h-4 sm:w-4" />
            </Button>
          </div>
          
          <Separator orientation="vertical" className="h-6" />
          
          {/* View Controls - Responsive */}
          <div className={cn(
            'flex items-center gap-xs',
            state.isMobileLayout ? 'hidden' : 'flex'
          )}>
            <Button
              variant="outline"
              size="sm"
              onClick={toggleGridVisibility}
            >
              <Grid3X3 className="h-3 w-3 sm:h-4 sm:w-4 mr-1" />
              <span className="hidden sm:inline">Grid</span>
            </Button>
            
            <Button
              variant={state.isPreviewMode ? 'secondary' : 'outline'}
              size="sm"
              onClick={togglePreviewMode}
            >
              <Eye className="h-3 w-3 sm:h-4 sm:w-4 mr-1" />
              <span className="hidden sm:inline">Preview</span>
            </Button>
          </div>
          
          <Separator orientation="vertical" className="h-6" />
          
          {/* Action Controls - Mobile Optimized */}
          <div className="flex items-center gap-1">
            {/* Undo/Redo - Hidden on Mobile */}
            <div className={cn(
              'flex items-center gap-xs',
              state.isMobileLayout ? 'hidden' : 'flex'
            )}>
              <Button variant="outline" size="sm" disabled>
                <Undo className="h-3 w-3 sm:h-4 sm:w-4" />
              </Button>
              
              <Button variant="outline" size="sm" disabled>
                <Redo className="h-3 w-3 sm:h-4 sm:w-4" />
              </Button>
            </div>
            
            {/* Save - Hidden on Mobile, shown on larger screens */}
            <Button
              variant="outline"
              size="sm"
              onClick={handleSave}
              disabled={!state.hasUnsavedChanges}
              className={cn(state.isMobileLayout ? 'hidden' : 'flex')}
            >
              <Save className="h-3 w-3 sm:h-4 sm:w-4 mr-1" />
              <span className="hidden sm:inline">Save</span>
            </Button>
            
            {/* Preview - Simplified on Mobile */}
            <Button
              variant="outline"
              size="sm"
              onClick={handlePreview}
              className={cn(state.isMobileLayout ? 'hidden' : 'flex')}
            >
              <Share className="h-3 w-3 sm:h-4 sm:w-4 mr-1" />
              <span className="hidden sm:inline">Preview</span>
            </Button>
            
            {/* Publish - Always visible */}
            <Button
              size="sm"
              onClick={handlePublish}
            >
              <Play className="h-3 w-3 sm:h-4 sm:w-4 mr-1" />
              <span className={cn(state.isMobileLayout ? 'hidden sm:inline' : 'inline')}>
                Publish
              </span>
            </Button>
          </div>
        </div>
      </div>

      {/* Main Content - Mobile Responsive */}
      <div className={cn(
        'page-builder__content flex-1 flex overflow-hidden',
        state.isMobileLayout ? 'flex-col' : 'flex-row'
      )}>
        {/* Sidebar - Responsive */}
        <div className={cn(
          'page-builder__sidebar border-r bg-card transition-all duration-300',
          state.isMobileLayout 
            ? cn(
                'w-full border-b border-r-0',
                state.showMobileMenu ? 'h-64' : 'h-0 overflow-hidden'
              )
            : 'w-80 h-full'
        )}>
          <Tabs value={state.activeTab} onValueChange={handleTabChange} className="h-full">
            <TabsList className={cn(
              'grid w-full m-2',
              state.isMobileLayout ? 'grid-cols-3 h-8' : 'grid-cols-3'
            )}>
              <TabsTrigger value="design" className={cn(state.isMobileLayout && 'text-xs')}>
                Design
              </TabsTrigger>
              <TabsTrigger value="settings" className={cn(state.isMobileLayout && 'text-xs')}>
                Settings
              </TabsTrigger>
              <TabsTrigger value="layout" className={cn(state.isMobileLayout && 'text-xs')}>
                Layout
              </TabsTrigger>
            </TabsList>
            
            <TabsContent value="design" className={cn(
              'h-full mt-0',
              state.isMobileLayout ? 'p-sm' : 'p-sm'
            )}>
              <ComponentPalette
                onComponentAdd={handleComponentAdd}
                selectedDevice={state.currentDevice}
                compact={state.isMobileLayout}
              />
            </TabsContent>
            
            <TabsContent value="settings" className={cn(
              'h-full mt-0',
              state.isMobileLayout ? 'p-sm' : 'p-sm'
            )}>
              <PageSettings
                page={page}
                onUpdate={handlePageUpdate}
                readOnly={readOnly}
                compact={state.isMobileLayout}
              />
            </TabsContent>
            
            <TabsContent value="layout" className={cn(
              'h-full mt-0',
              state.isMobileLayout ? 'p-sm' : 'p-sm'
            )}>
              <LayoutSelector
                layouts={layouts}
                selectedLayoutId={page.layoutId}
                onLayoutSelect={(layoutId) => handlePageUpdate({ layoutId })}
                readOnly={readOnly}
                compact={state.isMobileLayout}
              />
            </TabsContent>
          </Tabs>
        </div>

        {/* Canvas Area - Mobile Responsive */}
        <div className="page-builder__canvas flex-1 flex flex-col overflow-hidden">
          {/* Canvas Header - Mobile Optimized */}
          <div className={cn(
            'canvas-header border-b bg-muted/20',
            state.isMobileLayout ? 'p-sm' : 'p-sm'
          )}>
            <div className="flex items-center justify-between">
              <div className={cn(
                'text-muted-foreground',
                state.isMobileLayout ? 'text-xs' : 'text-sm'
              )}>
                {state.currentDevice.charAt(0).toUpperCase() + state.currentDevice.slice(1)} View
                {currentGrid && (
                  <span className={cn(
                    state.isMobileLayout ? 'hidden sm:inline ml-2' : 'ml-2'
                  )}>
                    • {currentGrid.columns} cols • {currentGrid.components.length} items
                  </span>
                )}
              </div>
              
              {/* Mobile Quick Actions */}
              {state.isMobileLayout ? (
                <div className="flex items-center gap-xs">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={toggleGridVisibility}
                  >
                    <Grid3X3 className="h-3 w-3" />
                  </Button>
                  <Button
                    variant={state.isPreviewMode ? 'secondary' : 'outline'}
                    size="sm"
                    onClick={togglePreviewMode}
                  >
                    <Eye className="h-3 w-3" />
                  </Button>
                </div>
              ) : (
                <div className="flex items-center gap-xs">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setState(prev => ({ ...prev, activeTab: 'layout' }))}
                  >
                    <Settings className="h-4 w-4 mr-1" />
                    Configure Layout
                  </Button>
                </div>
              )}
            </div>
          </div>

          {/* Canvas Content - Touch Optimized */}
          <div 
            className={cn(
              'canvas-content flex-1 overflow-auto',
              state.isMobileLayout ? 'p-sm' : 'p-sm'
            )}
            onTouchStart={(e) => {
              if (state.touchGesturesEnabled && state.isMobileLayout) {
                handleLongPress({ duration: 500, x: e.touches[0].clientX, y: e.touches[0].clientY });
              }
            }}
          >
            <Card className="h-full">
              <CardContent className={cn(
                'h-full',
                state.isMobileLayout ? 'p-2' : 'p-6'
              )}>
                {currentGrid ? (
                  <BentoGridComponent
                    grid={currentGrid}
                    deviceType={state.currentDevice}
                    isEditing={!state.isPreviewMode && !readOnly}
                    showGrid={state.showGrid}
                    onComponentMove={handleComponentMove}
                    onComponentSelect={handleComponentSelect}
                    onComponentDelete={handleComponentDelete}
                    className={cn(
                      'h-full',
                      state.isMobileLayout && 'touch-manipulation'
                    )}
                    // Touch gesture props removed for compilation fix
                    // TODO: Re-add touch gesture support
                  />
                ) : (
                  <div className="flex items-center justify-center h-full text-muted-foreground">
                    <div className="text-center">
                      <Grid3X3 className={cn(
                        'mx-auto mb-4 opacity-50',
                        state.isMobileLayout ? 'h-8 w-8' : 'h-12 w-12'
                      )} />
                      <p className={cn(
                        state.isMobileLayout ? 'text-sm' : 'text-base'
                      )}>
                        {state.isMobileLayout 
                          ? 'Tap + to add components'
                          : 'Select a layout to begin designing your page'
                        }
                      </p>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Properties Panel - Mobile Responsive */}
        {state.selectedComponent && !state.isPreviewMode && (
          <div className={cn(
            'page-builder__properties bg-card',
            state.isMobileLayout 
              ? 'border-t w-full' 
              : 'w-80 border-l'
          )}>
            <div className={cn(
              'border-b',
              state.isMobileLayout ? 'p-sm' : 'p-sm'
            )}>
              <h3 className={cn(
                'font-medium',
                state.isMobileLayout ? 'text-sm' : 'text-base'
              )}>
                Component Properties
              </h3>
              <p className="text-xs text-muted-foreground truncate">
                ID: {state.selectedComponent.slice(0, 8)}...
              </p>
            </div>
            <div className={cn(
              state.isMobileLayout ? 'p-sm' : 'p-sm'
            )}>
              {/* Component editor would go here */}
              <p className="text-xs text-muted-foreground">
                Component editor panel will be implemented here
              </p>
              
              {/* Mobile specific quick actions */}
              {state.isMobileLayout && (
                <div className="mt-4 flex gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setState(prev => ({ ...prev, selectedComponent: null }))}
                    className="flex-1"
                  >
                    Close
                  </Button>
                  <Button
                    size="sm"
                    onClick={() => {
                      if (state.selectedComponent) {
                        handleComponentDelete(state.selectedComponent);
                      }
                    }}
                    className="flex-1"
                  >
                    Delete
                  </Button>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
      
      {/* Mobile Touch Gesture Instructions */}
      {state.isMobileLayout && state.touchGesturesEnabled && !state.isPreviewMode && (
        <div className="fixed bottom-4 left-4 right-4 bg-card border rounded-lg p-3 shadow-lg lg:hidden">
          <div className="text-xs text-muted-foreground space-y-1">
            <div>• Long press: Open component palette</div>
            <div>• Swipe up: Show tools</div>
            <div>• Pinch: Toggle preview</div>
            <div>• Double tap: Select component</div>
          </div>
        </div>
      )}
      
      {/* Collaboration Status Overlay */}
      {collaborationEnabled && state.isMobileLayout && (
        <div className="fixed top-16 right-4 bg-card border rounded-lg p-2 shadow-lg">
          <div className="flex items-center gap-xs">
            <div className={cn(
              'w-2 h-2 rounded-full',
              isConnected ? 'bg-green-500' : 'bg-red-500'
            )} />
            <span className="text-xs text-muted-foreground">
              {connectedUsers} user{connectedUsers !== 1 ? 's' : ''}
            </span>
          </div>
        </div>
      )}
    </div>
  );
};

PageBuilder.displayName = 'PageBuilder';