/**
 * EditableContent - Universal content renderer for pages
 * 
 * When edit mode is OFF: Renders the provided children (normal page content)
 * When edit mode is ON: Renders the BentoGrid system for editing the page layout
 */

import React from 'react';
import { useEditMode } from '@/contexts/EditModeContext';
import { BentoGrid } from '@/components/bento/BentoGrid';
import { ComponentPropertiesPanel } from '@/components/bento/ComponentPropertiesPanel';
import type { BentoGrid as BentoGridType, DeviceType } from '@/types/bento';
import { motion, AnimatePresence } from 'framer-motion';

// Type for external drag data
interface ExternalDragData {
  type: string;
  componentType?: string;
  defaultSize?: { w: number; h: number };
}

declare global {
  interface Window {
    __bentoExternalDrag?: ExternalDragData;
  }
}

// ============================================================================
// Types
// ============================================================================

interface EditableContentProps {
  /** Normal page content (rendered when edit mode is OFF) */
  children: React.ReactNode;
  /** Page identifier for loading/saving bento configuration */
  pageId?: string;
  /** Custom class name */
  className?: string;
}

// ============================================================================
// Mock Grid Data (In production, this would come from your backend)
// ============================================================================

const createMockGrid = (device: DeviceType): BentoGridType => ({
  id: `grid-${device}`,
  name: `${device} Grid`,
  device,
  layoutId: 'default',
  columns: device === 'mobile' ? 4 : device === 'tablet' ? 8 : 12,
  rows: 20,
  gap: 16,
  rowHeight: 100,
  components: [
    {
      id: 'total-users',
      componentType: 'metric-card',
      position: { x: 0, y: 0, w: 3, h: 2 },
      props: {
        title: 'Total Users',
        value: '2,543',
        change: '+12%',
        trend: 'up'
      },
      bindings: {},
      display: {
        className: '',
        style: {}
      }
    },
    {
      id: 'revenue-chart',
      componentType: 'chart',
      position: { x: 3, y: 0, w: 6, h: 4 },
      props: {
        title: 'Revenue Trends',
        type: 'line',
        data: [100, 120, 140, 110, 160, 180, 200]
      },
      bindings: {},
      display: {
        className: '',
        style: {}
      }
    },
    {
      id: 'recent-activity',
      componentType: 'table',
      position: { x: 0, y: 2, w: 3, h: 3 },
      props: {
        title: 'Recent Activity',
        maxRows: 5
      },
      bindings: {},
      display: {
        className: '',
        style: {}
      }
    }
  ],
  settings: {
    enableSnapping: true,
    snapToGrid: true,
    enableGuides: true,
    compactMode: 'none' as const
  }
});

// ============================================================================
// Main Component
// ============================================================================

export const EditableContent: React.FC<EditableContentProps> = ({
  children,
  className
}) => {
  const { 
    isEditMode, 
    currentDevice, 
    showGrid, 
    selectedComponentId,
    selectComponent,
    moveComponent,
    deleteComponent,
    addComponent
  } = useEditMode();

  // State for external drag preview
  const [externalDragPreview, setExternalDragPreview] = React.useState<{
    position: { x: number; y: number; w: number; h: number };
    componentType: string;
  } | null>(null);

  // Create and manage grid state
  const [gridComponents, setGridComponents] = React.useState(() => createMockGrid(currentDevice as DeviceType).components);
  
  // Update grid when device changes
  const currentGrid = React.useMemo(() => {
    const grid = createMockGrid(currentDevice as DeviceType);
    return {
      ...grid,
      components: gridComponents
    };
  }, [currentDevice, gridComponents]);

  // Handle component operations
  const handleComponentMove = React.useCallback((
    componentId: string, 
    newPosition: { x: number; y: number; w: number; h: number }
  ) => {
    setGridComponents(prev => prev.map(comp => 
      comp.id === componentId ? { ...comp, position: newPosition } : comp
    ));
    moveComponent(componentId, newPosition);
  }, [moveComponent]);

  const handleComponentSelect = React.useCallback((componentId: string | null) => {
    selectComponent(componentId);
  }, [selectComponent]);

  const handleComponentDelete = React.useCallback((componentId: string) => {
    setGridComponents(prev => prev.filter(comp => comp.id !== componentId));
    deleteComponent(componentId);
  }, [deleteComponent]);

  // Handle drop from toolbar
  const handleToolbarDrop = React.useCallback((
    event: React.DragEvent,
    position: { x: number; y: number }
  ) => {
    try {
      const data = JSON.parse(event.dataTransfer.getData('application/json'));
      if (data.type === 'component') {
        // Add component at the calculated position
        const { componentType, defaultSize } = data;
        addComponent(componentType, {
          x: position.x,
          y: position.y,
          w: defaultSize.w,
          h: defaultSize.h
        });
      }
    } catch (error) {
      console.error('Failed to parse drop data:', error);
    }
  }, [addComponent]);

  // Listen for component add events
  React.useEffect(() => {
    const handleAddComponent = (event: CustomEvent) => {
      const { component, device } = event.detail;
      if (device === currentDevice) {
        setGridComponents(prev => [...prev, component]);
      }
    };

    window.addEventListener('bento-add-component', handleAddComponent as EventListener);
    return () => {
      window.removeEventListener('bento-add-component', handleAddComponent as EventListener);
    };
  }, [currentDevice]);

  return (
    <AnimatePresence mode="wait">
      {!isEditMode ? (
        // Render normal page content with fade transition
        <motion.div
          key="view-mode"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.3, ease: 'easeInOut' }}
          className={className}
        >
          {children}
        </motion.div>
      ) : (
        // Render bento grid editor with slide transition
        <motion.div
          key="edit-mode"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -20 }}
          transition={{ duration: 0.3, ease: 'easeInOut' }}
          className={`flex h-full ${className || ''}`}
        >
          {/* Main Grid Area */}
          <div 
            className="flex-1 p-4 overflow-hidden"
            onDrop={(e) => {
              e.preventDefault();
              setExternalDragPreview(null);
              // Clean up global drag data
              if ('__bentoExternalDrag' in window) {
                delete window.__bentoExternalDrag;
              }
              // Calculate drop position based on mouse coordinates
              const rect = e.currentTarget.getBoundingClientRect();
              const cellWidth = (rect.width - 32 - (currentGrid.gap * (currentGrid.columns - 1))) / currentGrid.columns;
              const cellHeight = currentGrid.rowHeight || 100;
              const x = Math.floor((e.clientX - rect.left - 16) / (cellWidth + currentGrid.gap));
              const y = Math.floor((e.clientY - rect.top - 16) / (cellHeight + currentGrid.gap));
              handleToolbarDrop(e, { x: Math.max(0, x), y: Math.max(0, y) });
            }}
            onDragOver={(e) => {
              e.preventDefault();
              // Update external drag preview position
              const data = window.__bentoExternalDrag || null;
              
              if (data && data.type === 'component') {
                const rect = e.currentTarget.getBoundingClientRect();
                const cellWidth = (rect.width - 32 - (currentGrid.gap * (currentGrid.columns - 1))) / currentGrid.columns;
                const cellHeight = currentGrid.rowHeight || 100;
                
                const x = Math.floor((e.clientX - rect.left - 16) / (cellWidth + currentGrid.gap));
                const y = Math.floor((e.clientY - rect.top - 16) / (cellHeight + currentGrid.gap));
                
                setExternalDragPreview({
                  position: {
                    x: Math.max(0, Math.min(x, currentGrid.columns - data.defaultSize.w)),
                    y: Math.max(0, y),
                    w: data.defaultSize.w,
                    h: data.defaultSize.h
                  },
                  componentType: data.componentType
                });
              }
            }}
            onDragLeave={(e) => {
              // Only clear preview if leaving the grid area completely
              if (!e.currentTarget.contains(e.relatedTarget as Node)) {
                setExternalDragPreview(null);
              }
            }}
          >
            <motion.div 
              className="h-full bg-card rounded-lg p-6"
              initial={{ scale: 0.95 }}
              animate={{ scale: 1 }}
              transition={{ duration: 0.3, ease: 'easeInOut' }}
            >
              <BentoGrid
                grid={currentGrid}
                deviceType={currentDevice as DeviceType}
                isEditing={true}
                showGrid={showGrid}
                onComponentMove={handleComponentMove}
                onComponentResize={handleComponentMove}
                onComponentSelect={handleComponentSelect}
                onComponentDelete={handleComponentDelete}
                onShowProperties={handleComponentSelect}
                externalDragPreview={externalDragPreview}
                className="h-full"
              />
            </motion.div>
          </div>

          {/* Properties Panel */}
          {selectedComponentId && (
            <ComponentPropertiesPanel
              isOpen={true}
              onClose={() => selectComponent(null)}
              component={currentGrid.components.find(c => c.id === selectedComponentId)}
            />
          )}
        </motion.div>
      )}
    </AnimatePresence>
  );
};

export default EditableContent;