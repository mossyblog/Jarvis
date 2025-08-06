/**
 * ComponentEditor - Main editor with vertical tabs
 * 
 * Provides a sophisticated editing interface with vertical tabs on the side,
 * offering access to binding properties, preview functionality, and component
 * configuration options.
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  Link,
  Eye,
  Settings,
  X,
  RotateCcw,
  Trash2
} from 'lucide-react';

import type { GridComponent, DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { ComponentRenderer } from '../ComponentRenderer';
import { BindingsPanel } from './BindingsPanel';
import { PreviewPanel } from './PreviewPanel';
import { WriteConfigModal } from './WriteConfigModal';

// ============================================================================
// Types
// ============================================================================

export interface ComponentEditorProps {
  /** The component being edited */
  component: GridComponent;
  /** Whether the component is currently selected */
  isSelected: boolean;
  /** Current device context for responsive editing */
  deviceType?: DeviceType;
  /** Whether the editor is in read-only mode */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
  
  // Event handlers
  /** Called when component properties are updated */
  onUpdate?: (updates: Partial<GridComponent>) => void;
  /** Called when component should be deleted */
  onDelete?: () => void;
  /** Called when component selection changes */
  onSelectionChange?: (isSelected: boolean) => void;
  /** Called when component bindings are tested */
  onTestBindings?: (component: GridComponent) => Promise<unknown>;
}

interface EditorState {
  activeTab: 'bindings' | 'preview' | 'settings';
  showWriteConfig: boolean;
  hasUnsavedChanges: boolean;
  isTestingBindings: boolean;
  previewDevice: DeviceType;
}

// ============================================================================
// Default Values
// ============================================================================

const createDefaultEditorState = (): EditorState => ({
  activeTab: 'bindings',
  showWriteConfig: false,
  hasUnsavedChanges: false,
  isTestingBindings: false,
  previewDevice: 'desktop' as DeviceType
});

// ============================================================================
// Vertical Tabs Component
// ============================================================================

interface VerticalTabsProps {
  activeTab: string;
  onTabChange: (tab: string) => void;
  className?: string;
}

const VerticalTabs: React.FC<VerticalTabsProps> = ({ activeTab, onTabChange, className }) => {
  const tabs = [
    { id: 'bindings', label: 'Bindings', icon: Link, description: 'Data & Event Bindings' },
    { id: 'preview', label: 'Preview', icon: Eye, description: 'Component Preview' },
    { id: 'settings', label: 'Settings', icon: Settings, description: 'Component Settings' }
  ];

  return (
    <div className={cn('vertical-tabs flex flex-col gap-1', className)}>
      {tabs.map((tab) => {
        const IconComponent = tab.icon;
        const isActive = activeTab === tab.id;
        
        return (
          <Button
            key={tab.id}
            variant={isActive ? 'secondary' : 'ghost'}
            className={cn(
              'justify-start h-auto p-3 flex-col items-start text-left min-h-[60px]',
              isActive && 'bg-secondary'
            )}
            onClick={() => onTabChange(tab.id)}
          >
            <div className="flex items-center gap-2 w-full">
              <IconComponent className="h-xs w-xs flex-shrink-0" />
              <span className="font-medium text-sm">{tab.label}</span>
            </div>
            <span className="text-xs text-muted-foreground mt-1">
              {tab.description}
            </span>
          </Button>
        );
      })}
    </div>
  );
};

// ============================================================================
// Component Settings Panel
// ============================================================================

interface SettingsPanelProps {
  component: GridComponent;
  onUpdate: (updates: Partial<GridComponent>) => void;
  onDelete: () => void;
  readOnly?: boolean;
}

const SettingsPanel: React.FC<SettingsPanelProps> = ({
  component,
  onUpdate,
  onDelete,
  readOnly = false
}) => {
  const handlePositionUpdate = useCallback((field: keyof GridComponent['position'], value: number) => {
    onUpdate({
      position: { ...component.position, [field]: value }
    });
  }, [component.position, onUpdate]);

  const handleDisplayUpdate = useCallback((updates: Partial<GridComponent['display']>) => {
    onUpdate({
      display: { ...component.display, ...updates }
    });
  }, [component.display, onUpdate]);

  return (
    <div className="settings-panel space-y-6">
      {/* Component Info */}
      <div className="space-y-3">
        <h4 className="text-sm font-medium">Component Information</h4>
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <span className="text-muted-foreground">Type:</span>
            <div className="font-mono">{component.componentType}</div>
          </div>
          <div>
            <span className="text-muted-foreground">ID:</span>
            <div className="font-mono text-xs">{component.id}</div>
          </div>
        </div>
      </div>

      <Separator />

      {/* Position & Size */}
      <div className="space-y-3">
        <h4 className="text-sm font-medium">Position & Size</h4>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="text-xs text-muted-foreground">X Position</label>
            <input
              type="number"
              value={component.position.x}
              onChange={(e) => handlePositionUpdate('x', parseInt(e.target.value) || 0)}
              disabled={readOnly}
              className="w-full px-2 py-1 text-sm border rounded"
              min="0"
            />
          </div>
          <div>
            <label className="text-xs text-muted-foreground">Y Position</label>
            <input
              type="number"
              value={component.position.y}
              onChange={(e) => handlePositionUpdate('y', parseInt(e.target.value) || 0)}
              disabled={readOnly}
              className="w-full px-2 py-1 text-sm border rounded"
              min="0"
            />
          </div>
          <div>
            <label className="text-xs text-muted-foreground">Width</label>
            <input
              type="number"
              value={component.position.w}
              onChange={(e) => handlePositionUpdate('w', parseInt(e.target.value) || 1)}
              disabled={readOnly}
              className="w-full px-2 py-1 text-sm border rounded"
              min="1"
            />
          </div>
          <div>
            <label className="text-xs text-muted-foreground">Height</label>
            <input
              type="number"
              value={component.position.h}
              onChange={(e) => handlePositionUpdate('h', parseInt(e.target.value) || 1)}
              disabled={readOnly}
              className="w-full px-2 py-1 text-sm border rounded"
              min="1"
            />
          </div>
        </div>
      </div>

      <Separator />

      {/* Display Options */}
      <div className="space-y-3">
        <h4 className="text-sm font-medium">Display Options</h4>
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-sm">Visible</span>
            <input
              type="checkbox"
              checked={component.display?.visible !== false}
              onChange={(e) => handleDisplayUpdate({ visible: e.target.checked })}
              disabled={readOnly}
            />
          </div>
          <div>
            <label className="text-xs text-muted-foreground">Z-Index</label>
            <input
              type="number"
              value={component.display?.zIndex || 1}
              onChange={(e) => handleDisplayUpdate({ zIndex: parseInt(e.target.value) || 1 })}
              disabled={readOnly}
              className="w-full px-2 py-1 text-sm border rounded"
            />
          </div>
        </div>
      </div>

      <Separator />

      {/* Actions */}
      <div className="space-y-2">
        <h4 className="text-sm font-medium">Actions</h4>
        <div className="flex flex-col gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onUpdate({ props: {} })}
            disabled={readOnly}
          >
            <RotateCcw className="h-xs w-xs mr-2" />
            Reset Properties
          </Button>
          <Button
            variant="destructive"
            size="sm"
            onClick={onDelete}
            disabled={readOnly}
          >
            <Trash2 className="h-xs w-xs mr-2" />
            Delete Component
          </Button>
        </div>
      </div>
    </div>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const ComponentEditor: React.FC<ComponentEditorProps> = ({
  component,
  isSelected,
  deviceType = 'desktop' as DeviceType,
  readOnly = false,
  className,
  onUpdate,
  onDelete,
  onSelectionChange,
  onTestBindings
}) => {
  const [state, setState] = useState<EditorState>(createDefaultEditorState());

  // Component size for grid layout
  const gridSize = useMemo(() => ({
    w: component.position.w,
    h: component.position.h
  }), [component.position]);

  // Handle tab changes
  const handleTabChange = useCallback((tab: string) => {
    setState(prev => ({ ...prev, activeTab: tab as EditorState['activeTab'] }));
  }, []);

  // Handle component updates
  const handleComponentUpdate = useCallback((updates: Partial<GridComponent>) => {
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    onUpdate?.(updates);
  }, [onUpdate]);

  // Handle write configuration
  const handleShowWriteConfig = useCallback(() => {
    setState(prev => ({ ...prev, showWriteConfig: true }));
  }, []);

  const handleCloseWriteConfig = useCallback(() => {
    setState(prev => ({ ...prev, showWriteConfig: false }));
  }, []);

  const handleSaveWriteConfig = useCallback((writeConfig: unknown) => {
    handleComponentUpdate({
      bindings: {
        ...component.bindings,
        write: writeConfig as {
          operation?: string;
          target?: string;
          mapping?: Record<string, unknown>;
          ecsComponent?: string;
          fieldMappings?: Record<string, string>;
          triggers?: string[];
        }
      }
    });
    handleCloseWriteConfig();
  }, [component.bindings, handleComponentUpdate, handleCloseWriteConfig]);

  // Handle component deletion
  const handleDelete = useCallback(() => {
    onDelete?.();
  }, [onDelete]);

  // Handle selection toggle
  const handleSelectionToggle = useCallback(() => {
    onSelectionChange?.(!isSelected);
  }, [isSelected, onSelectionChange]);

  // Handle bindings test
  const handleTestBindings = useCallback(async () => {
    if (!onTestBindings) return;
    
    setState(prev => ({ ...prev, isTestingBindings: true }));
    try {
      await onTestBindings(component);
    } finally {
      setState(prev => ({ ...prev, isTestingBindings: false }));
    }
  }, [component, onTestBindings]);

  // Handle preview device change
  const handlePreviewDeviceChange = useCallback((device: DeviceType) => {
    setState(prev => ({ ...prev, previewDevice: device }));
  }, []);

  if (!isSelected) {
    // Render minimal component wrapper when not selected
    return (
      <div 
        className={cn('component-editor relative', className)}
        onClick={handleSelectionToggle}
      >
        <ComponentRenderer
          component={component}
          gridSize={gridSize}
          deviceType={deviceType}
        />
      </div>
    );
  }

  return (
    <div className={cn('component-editor component-editor--selected', className)}>
      {/* Component Content */}
      <div className="component-editor__content relative">
        <ComponentRenderer
          component={component}
          gridSize={gridSize}
          deviceType={deviceType}
        />
        
        {/* Selection Overlay */}
        <div className="absolute inset-0 border-2 border-primary rounded-md pointer-events-none">
          {/* Selection Header */}
          <div className="absolute -top-8 left-0 right-0 flex items-center justify-between">
            <Badge variant="secondary" className="text-xs">
              {component.componentType}
            </Badge>
            <div className="flex items-center gap-1">
              {state.hasUnsavedChanges && (
                <Badge variant="outline" className="text-xs">
                  Unsaved
                </Badge>
              )}
              <Button
                variant="ghost"
                size="sm"
                className="h-sm w-sm p-0"
                onClick={handleSelectionToggle}
              >
                <X className="h-xs w-xs" />
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Editor Panel */}
      <div className="component-editor__panel fixed right-0 top-0 bottom-0 w-2xlxl bg-card border-l shadow-lg z-50">
        <div className="flex h-full">
          {/* Vertical Tabs */}
          <div className="w-4xl border-r bg-muted/20 p-2">
            <VerticalTabs
              activeTab={state.activeTab}
              onTabChange={handleTabChange}
            />
          </div>

          {/* Panel Content */}
          <div className="flex-1 flex flex-col">
            {/* Panel Header */}
            <div className="border-b p-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="font-semibold">Component Editor</h3>
                  <p className="text-sm text-muted-foreground">
                    {component.componentType}
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleSelectionToggle}
                >
                  <X className="h-xs w-xs" />
                </Button>
              </div>
            </div>

            {/* Panel Body */}
            <div className="flex-1 overflow-auto">
              {state.activeTab === 'bindings' && (
                <div className="p-4">
                  <BindingsPanel
                    component={component}
                    onUpdate={handleComponentUpdate}
                    onShowWriteConfig={handleShowWriteConfig}
                    onTestBindings={handleTestBindings}
                    isTestingBindings={state.isTestingBindings}
                    readOnly={readOnly}
                  />
                </div>
              )}

              {state.activeTab === 'preview' && (
                <div className="p-4">
                  <PreviewPanel
                    component={component}
                    currentDevice={state.previewDevice}
                    onDeviceChange={handlePreviewDeviceChange}
                  />
                </div>
              )}

              {state.activeTab === 'settings' && (
                <div className="p-4">
                  <SettingsPanel
                    component={component}
                    onUpdate={handleComponentUpdate}
                    onDelete={handleDelete}
                    readOnly={readOnly}
                  />
                </div>
              )}
            </div>

            {/* Panel Footer */}
            <div className="border-t p-4">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>Component ID: {component.id.slice(0, 8)}...</span>
                {state.hasUnsavedChanges && (
                  <span className="text-orange-600">Unsaved changes</span>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Write Configuration Modal */}
      {state.showWriteConfig && (
        <WriteConfigModal
          component={component}
          onClose={handleCloseWriteConfig}
          onSave={handleSaveWriteConfig}
        />
      )}
    </div>
  );
};

ComponentEditor.displayName = 'ComponentEditor';