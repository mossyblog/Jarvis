/**
 * BentoGrid - Main grid container with drag-and-drop functionality
 * 
 * This is the primary component that renders the Bento grid layout with
 * support for drag-and-drop operations, component positioning, and
 * responsive grid behavior.
 */

import React, { useMemo, useCallback, useState } from 'react';
import {
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  DragOverlay,
  rectIntersection,
} from '@dnd-kit/core';
import type {
  DragEndEvent,
  DragStartEvent,
  CollisionDetection,
} from '@dnd-kit/core';
// Removed SortableContext - we'll use DndContext directly for grid positioning

import type { BentoGrid as BentoGridType, GridComponent, GridPosition } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { GridComponent as GridComponentWrapper } from './GridComponent';
import { ComponentRenderer } from './ComponentRenderer';
import { GridOverlay } from './GridOverlay';
import { DragPreview } from './DragPreview';

// ============================================================================
// Types
// ============================================================================

export interface BentoGridProps {
  /** Grid configuration and components */
  grid: BentoGridType;
  
  /** Current device type for responsive behavior */
  deviceType?: DeviceType;
  
  /** Whether the grid is in edit mode (allows drag/drop) */
  isEditing?: boolean;
  
  /** Whether to show visual grid lines */
  showGrid?: boolean;
  
  /** Additional CSS classes */
  className?: string;
  
  /** Inline styles */
  style?: React.CSSProperties;
  
  // Event handlers
  /** Called when a component is moved */
  onComponentMove?: (componentId: string, newPosition: { x: number; y: number; w: number; h: number }) => void;
  
  /** Called when a component is resized */
  onComponentResize?: (componentId: string, newSize: { w: number; h: number; x?: number; y?: number }) => void;
  
  /** Called when a component is selected */
  onComponentSelect?: (componentId: string | null) => void;
  
  /** Called when a component is deleted */
  onComponentDelete?: (componentId: string) => void;
  
  /** Called when the grid configuration changes */
  onGridUpdate?: (updatedGrid: BentoGridType) => void;
}

interface DragState {
  activeId: string | null;
  draggedComponent: GridComponent | null;
  isDragging: boolean;
}

// ============================================================================
// Custom Collision Detection
// ============================================================================

/**
 * Custom collision detection that respects grid constraints
 */
const gridCollisionDetection: CollisionDetection = (args) => {
  // Use the default rect intersection for basic collision detection
  const rectCollisions = rectIntersection(args);
  
  // Filter out invalid collisions based on grid constraints
  return rectCollisions.filter(() => {
    // Add custom logic here if needed for grid-specific collision rules
    return true;
  });
};

// ============================================================================
// Main Component
// ============================================================================

export const BentoGrid: React.FC<BentoGridProps> = ({
  grid,
  deviceType = DeviceType.Desktop,
  isEditing = false,
  showGrid = false,
  className,
  style,
  onComponentMove,
  onComponentResize,
  onComponentSelect,
  onComponentDelete,
  // onGridUpdate,
}) => {
  // State for drag operations
  const [dragState, setDragState] = useState<DragState>({
    activeId: null,
    draggedComponent: null,
    isDragging: false,
  });

  // Configure drag sensors
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // Minimum distance before drag starts
      },
    })
  );

  // Calculate grid CSS variables
  const gridStyle = useMemo(() => {
    const baseStyle: React.CSSProperties = {
      display: 'grid',
      gridTemplateColumns: `repeat(${grid.columns}, 1fr)`,
      gap: `${grid.gap}px`,
      minHeight: '100%',
      position: 'relative',
      ...style,
    };

    // Add row height if specified
    if (grid.rowHeight) {
      baseStyle.gridAutoRows = `${grid.rowHeight}px`;
    }

    return baseStyle;
  }, [grid.columns, grid.gap, grid.rowHeight, style]);

  // Removed sortableItems - no longer needed without SortableContext

  // Handle drag start
  const handleDragStart = useCallback((event: DragStartEvent) => {
    const { active } = event;
    const componentId = active.id as string;
    const component = grid.components.find(c => c.id === componentId);

    if (component) {
      setDragState({
        activeId: componentId,
        draggedComponent: { ...component }, // Create a copy to avoid stale reference
        isDragging: true,
      });
    }
  }, [grid.components]);

  // Handle drag over (while dragging)
  const handleDragOver = useCallback(() => {
    // Handle preview updates during drag if needed
    // This is where you might update visual feedback
  }, []);

  // Check for collisions
  const checkCollision = useCallback((position: GridPosition, excludeId?: string): boolean => {
    return grid.components.some(component => {
      if (component.id === excludeId) return false;
      
      const overlap = !(
        position.x >= component.position.x + component.position.w ||
        position.x + position.w <= component.position.x ||
        position.y >= component.position.y + component.position.h ||
        position.y + position.h <= component.position.y
      );
      
      return overlap;
    });
  }, [grid.components]);

  // Handle drag end
  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { delta } = event;

    setDragState({
      activeId: null,
      draggedComponent: null,
      isDragging: false,
    });

    if (!dragState.draggedComponent) {
      return;
    }

    // Find the current component data
    const component = grid.components.find(c => c.id === dragState.draggedComponent!.id);
    if (!component) return;

    // Calculate cell dimensions - we need to find the actual grid container
    // Use a more reliable method to get grid dimensions
    const gridContainer = document.querySelector('.bento-grid') as HTMLElement;
    if (!gridContainer) {
      console.warn('Grid container not found for drag calculation');
      return;
    }

    const gridRect = gridContainer.getBoundingClientRect();
    const cellWidth = (gridRect.width - (grid.gap * (grid.columns - 1))) / grid.columns;
    const cellHeight = grid.rowHeight || 100;

    // Calculate how many grid cells the component moved
    const deltaX = Math.round(delta.x / (cellWidth + grid.gap));
    const deltaY = Math.round(delta.y / (cellHeight + grid.gap));
    
    // Calculate new position based on current position + delta
    const newPosition: GridPosition = {
      x: Math.max(0, Math.min(component.position.x + deltaX, grid.columns - component.position.w)),
      y: Math.max(0, component.position.y + deltaY),
      w: component.position.w,
      h: component.position.h,
    };

    // Only proceed if position actually changed
    if (newPosition.x === component.position.x && newPosition.y === component.position.y) {
      return;
    }

    // Check for collisions
    if (checkCollision(newPosition, component.id)) {
      // Try to find a nearby valid position
      const positions = [
        { x: newPosition.x, y: newPosition.y + 1 }, // Below
        { x: newPosition.x, y: newPosition.y - 1 }, // Above
        { x: newPosition.x + 1, y: newPosition.y }, // Right
        { x: newPosition.x - 1, y: newPosition.y }, // Left
      ];

      let validPosition = null;
      for (const pos of positions) {
        const testPosition = { ...newPosition, ...pos };
        if (
          testPosition.x >= 0 && 
          testPosition.x + testPosition.w <= grid.columns &&
          testPosition.y >= 0 &&
          !checkCollision(testPosition, component.id)
        ) {
          validPosition = testPosition;
          break;
        }
      }

      if (validPosition) {
        onComponentMove?.(component.id, validPosition);
      }
      // If no valid position found, component returns to original position
    } else {
      // Update position if valid
      onComponentMove?.(component.id, newPosition);
    }
  }, [dragState.draggedComponent, grid.columns, grid.gap, grid.rowHeight, grid.components, onComponentMove, checkCollision]);

  // Handle component selection
  const handleComponentSelect = useCallback((componentId: string) => {
    onComponentSelect?.(componentId);
  }, [onComponentSelect]);

  // Handle component deletion
  const handleComponentDelete = useCallback((componentId: string) => {
    onComponentDelete?.(componentId);
  }, [onComponentDelete]);

  // Handle component resize
  const handleComponentResize = useCallback((componentId: string, newSize: { w: number; h: number; x?: number; y?: number }) => {
    onComponentResize?.(componentId, newSize);
  }, [onComponentResize]);

  return (
    <div className={cn('bento-grid-wrapper', className)}>
      <DndContext
        sensors={sensors}
        collisionDetection={gridCollisionDetection}
        onDragStart={handleDragStart}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
      >
        <div className="relative">
          {/* Render grid overlay behind the grid */}
          {isEditing && showGrid && (
            <GridOverlay
              columns={grid.columns}
              rows={grid.rows}
              gap={grid.gap}
              rowHeight={grid.rowHeight}
            />
          )}
          
          <div
            className={cn(
              'bento-grid',
              'relative',
              {
                'bento-grid--editing': isEditing,
                'bento-grid--show-grid': showGrid,
              }
            )}
            style={gridStyle}
          >
            {/* Render grid components */}
            {grid.components.map((component) => (
              <GridComponentWrapper
                key={component.id}
                component={component}
                isEditing={isEditing}
                isSelected={false} // You might want to track selected state
                isDragging={dragState.activeId === component.id}
                deviceType={deviceType}
                onSelect={handleComponentSelect}
                onDelete={handleComponentDelete}
                onResize={handleComponentResize}
              >
                <ComponentRenderer
                  component={component}
                  gridSize={{
                    w: component.position.w,
                    h: component.position.h,
                  }}
                  deviceType={deviceType}
                />
              </GridComponentWrapper>
            ))}
          </div>
        </div>

        {/* Drag overlay for visual feedback */}
        <DragOverlay>
          {dragState.activeId && dragState.draggedComponent ? (
            <DragPreview
              component={dragState.draggedComponent}
              deviceType={deviceType}
            />
          ) : null}
        </DragOverlay>
      </DndContext>
    </div>
  );
};

BentoGrid.displayName = 'BentoGrid';