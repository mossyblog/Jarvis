/**
 * GridComponent - Individual component wrapper for grid items
 * 
 * This component wraps each individual component in the grid, providing
 * drag-and-drop functionality, resize handles, selection states, and
 * positioning within the CSS Grid layout.
 */

import React, { useMemo, useCallback, useState, useRef, useEffect } from 'react';
import { useDraggable } from '@dnd-kit/core';
import { CSS } from '@dnd-kit/utilities';
import { Trash2, Move, MoreHorizontal } from 'lucide-react';

import type { GridComponent as GridComponentType } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';

// ============================================================================
// Types
// ============================================================================

export interface GridComponentProps {
  /** The grid component configuration */
  component: GridComponentType;
  
  /** Whether the grid is in edit mode */
  isEditing?: boolean;
  
  /** Whether this component is currently selected */
  isSelected?: boolean;
  
  /** Whether this component is being dragged */
  isDragging?: boolean;
  
  /** Current device type */
  deviceType?: DeviceType;
  
  /** Component content to render */
  children: React.ReactNode;
  
  // Event handlers
  /** Called when component is selected */
  onSelect?: (componentId: string) => void;
  
  /** Called when component is deleted */
  onDelete?: (componentId: string) => void;
  
  /** Called when component is resized */
  onResize?: (componentId: string, newSize: { w: number; h: number; x?: number; y?: number }) => void;
  
  /** Called when component properties should be shown */
  onShowProperties?: (componentId: string) => void;
}

interface ResizeHandle {
  direction: 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w' | 'nw';
  className: string;
  cursor: string;
}

// ============================================================================
// Constants
// ============================================================================

const RESIZE_HANDLES: ResizeHandle[] = [
  { direction: 'n', className: 'resize-handle-n', cursor: 'ns-resize' },
  { direction: 'ne', className: 'resize-handle-ne', cursor: 'nesw-resize' },
  { direction: 'e', className: 'resize-handle-e', cursor: 'ew-resize' },
  { direction: 'se', className: 'resize-handle-se', cursor: 'nwse-resize' },
  { direction: 's', className: 'resize-handle-s', cursor: 'ns-resize' },
  { direction: 'sw', className: 'resize-handle-sw', cursor: 'nesw-resize' },
  { direction: 'w', className: 'resize-handle-w', cursor: 'ew-resize' },
  { direction: 'nw', className: 'resize-handle-nw', cursor: 'nwse-resize' },
];

// ============================================================================
// Main Component
// ============================================================================

export const GridComponent: React.FC<GridComponentProps> = ({
  component,
  isEditing = false,
  isSelected = false,
  isDragging = false,
  deviceType = DeviceType.Desktop,
  children,
  onSelect,
  onDelete,
  onResize,
  onShowProperties,
}) => {
  const [isResizing, setIsResizing] = useState(false);
  const [resizePreview, setResizePreview] = useState<{ w: number; h: number } | null>(null);
  
  // Use ref to track resize state and avoid stale closures
  const isResizingRef = useRef(false);

  // Setup draggable functionality
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    isDragging: isDraggableDragging,
  } = useDraggable({
    id: component.id,
    disabled: !isEditing || component.locked,
  });

  // Calculate grid positioning styles
  const gridStyles = useMemo(() => {
    const { position } = component;
    
    const baseStyles: React.CSSProperties = {
      gridColumn: `${position.x + 1} / span ${position.w}`,
      gridRow: `${position.y + 1} / span ${position.h}`,
      zIndex: isDragging || isDraggableDragging ? 1000 : 1,
    };

    // Apply transform only while actively dragging
    if (transform && (isDragging || isDraggableDragging)) {
      baseStyles.transform = CSS.Transform.toString(transform);
    }

    return baseStyles;
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [component.position, transform, isDragging, isDraggableDragging]);

  // Handle component selection
  const handleSelect = useCallback((event: React.MouseEvent) => {
    if (!isEditing) return;
    
    event.stopPropagation();
    onSelect?.(component.id);
  }, [isEditing, component.id, onSelect]);

  // Handle component deletion
  const handleDelete = useCallback((event: React.MouseEvent) => {
    event.stopPropagation();
    onDelete?.(component.id);
  }, [component.id, onDelete]);

  // Handle show properties
  const handleShowProperties = useCallback((event: React.MouseEvent) => {
    event.stopPropagation();
    onShowProperties?.(component.id);
  }, [component.id, onShowProperties]);

  // Handle resize start
  const handleResizeStart = useCallback((event: React.MouseEvent, direction: string, cursor: string) => {
    // Prevent default behaviors
    event.preventDefault();
    event.stopPropagation();
    
    // Set resize state
    isResizingRef.current = true;
    setIsResizing(true);
    
    const startSize = { w: component.position.w, h: component.position.h };
    const startPosition = { x: component.position.x, y: component.position.y };
    
    const startX = event.clientX;
    const startY = event.clientY;
    
    // Get the grid container to calculate cell dimensions
    const gridContainer = document.querySelector('.bento-grid') as HTMLElement;
    if (!gridContainer) return;
    
    const gridRect = gridContainer.getBoundingClientRect();
    const gridStyles = window.getComputedStyle(gridContainer);
    const gap = parseInt(gridStyles.gap || '16');
    const columns = parseInt(gridStyles.gridTemplateColumns.split(' ').length.toString() || '12');
    
    // Calculate cell dimensions for snapping
    const cellWidth = (gridRect.width - (gap * (columns - 1))) / columns;
    const cellHeight = 100; // This should match the rowHeight from grid config
    
    const handleMouseMove = (moveEvent: MouseEvent) => {
      // Simple delta calculation
      const deltaX = moveEvent.clientX - startX;
      const deltaY = moveEvent.clientY - startY;
      
      // Convert to grid units and snap
      const gridDeltaX = Math.round(deltaX / (cellWidth + gap));
      const gridDeltaY = Math.round(deltaY / (cellHeight + gap));
      
      let newW = startSize.w;
      let newH = startSize.h;
      let newX = startPosition.x;
      let newY = startPosition.y;
      
      // Simple resize logic based on direction
      if (direction.includes('e')) {
        newW = Math.max(1, startSize.w + gridDeltaX);
        newW = Math.min(newW, columns - startPosition.x); // Stay within grid
      }
      
      if (direction.includes('w')) {
        const deltaW = -gridDeltaX; // Invert for west
        newW = Math.max(1, startSize.w + deltaW);
        newX = Math.max(0, startPosition.x - deltaW);
        
        // Ensure we don't exceed boundaries
        if (newX < 0) {
          newX = 0;
          newW = startPosition.x + startSize.w;
        }
      }
      
      if (direction.includes('s')) {
        newH = Math.max(1, startSize.h + gridDeltaY);
      }
      
      if (direction.includes('n')) {
        const deltaH = -gridDeltaY; // Invert for north
        newH = Math.max(1, startSize.h + deltaH);
        newY = Math.max(0, startPosition.y - deltaH);
      }
      
      // Keep within grid bounds
      if (newX + newW > columns) {
        if (direction.includes('w')) {
          newX = columns - newW;
        } else {
          newW = columns - newX;
        }
      }
      
      // Update preview
      setResizePreview({ w: newW, h: newH });
      
      // Update component position in real-time for fluid feedback
      if (direction.includes('w') || direction.includes('n')) {
        onResize?.(component.id, { w: newW, h: newH, x: newX, y: newY });
      } else {
        onResize?.(component.id, { w: newW, h: newH });
      }
    };
    
    const handleMouseUp = () => {
      // Only cleanup on mouseup - this is the user's signal they're done
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      
      isResizingRef.current = false;
      setIsResizing(false);
      setResizePreview(null);
      
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
      document.body.classList.remove('resizing');
    };
    
    // Set styles for resize operation
    document.body.style.userSelect = 'none';
    document.body.style.cursor = cursor;
    document.body.classList.add('resizing');
    
    // Listen for mouse events on document to track anywhere
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [component.id, component.position, onResize]);

  // Cleanup effect to ensure resize is stopped on unmount
  useEffect(() => {
    return () => {
      // Clean up any active resize operation
      if (isResizingRef.current) {
        isResizingRef.current = false;
        document.body.style.cursor = '';
        document.body.classList.remove('resizing');
        document.body.style.userSelect = '';
      }
    };
  }, []);
  
  // Check if component should be hidden on current device
  const isHidden = useMemo(() => {
    if (!component.display) return false;
    
    const { hideOn, showOnly } = component.display;
    
    if (hideOn && hideOn.includes(deviceType)) {
      return true;
    }
    
    if (showOnly && !showOnly.includes(deviceType)) {
      return true;
    }
    
    return false;
  }, [component.display, deviceType]);
  
  
  // Don't render if component is hidden on current device
  if (isHidden) {
    return null;
  }
  
  return (
    <div
      ref={setNodeRef}
      className={cn(
        'bento-component',
        'relative',
        'group',
        {
          'bento-component--editing': isEditing,
          'bento-component--selected': isSelected,
          'bento-component--dragging': isDragging || isDraggableDragging,
          'bento-component--resizing': isResizing,
          'bento-component--locked': component.locked,
        },
        component.display?.className
      )}
      style={{
        ...gridStyles,
        ...component.display?.style,
      }}
      onClick={handleSelect}
      {...attributes}
    >
      {/* Component content */}
      <div className="bento-component__content h-full w-full overflow-hidden">
        {children}
      </div>

      {/* Edit mode overlays */}
      {isEditing && !component.locked && (
        <>
          {/* Drag handle */}
          <div
            className={cn(
              'bento-component__drag-handle',
              'absolute top-1 left-1 opacity-0 group-hover:opacity-100',
              'transition-opacity duration-200',
              'cursor-move z-10'
            )}
            {...listeners}
          >
            <Button
              variant="secondary"
              size="sm"
              className="h-6 w-6 p-0"
            >
              <Move className="h-3 w-3" />
            </Button>
          </div>

          {/* Component actions */}
          <div
            className={cn(
              'bento-component__actions',
              'absolute top-1 right-1 opacity-0 group-hover:opacity-100',
              'transition-opacity duration-200',
              'flex gap-1 z-10'
            )}
          >
            <Button
              variant="secondary"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={handleDelete}
            >
              <Trash2 className="h-3 w-3" />
            </Button>
            
            <Button
              variant="secondary"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={handleShowProperties}
              title="Component Properties"
            >
              <MoreHorizontal className="h-3 w-3" />
            </Button>
          </div>

          {/* Resize handles */}
          {RESIZE_HANDLES.map((handle) => (
            <div
              key={handle.direction}
              className={cn(
                'bento-component__resize-handle',
                handle.className,
                'absolute opacity-0 group-hover:opacity-100',
                'transition-opacity duration-200',
                'bg-primary rounded-sm',
                'z-20'
              )}
              style={{
                cursor: handle.cursor,
                // Corner handles are larger
                width: handle.direction.length === 2 ? '16px' : '12px',
                height: handle.direction.length === 2 ? '16px' : '12px',
                // Position based on direction
                ...(handle.direction.includes('n') && { top: handle.direction.length === 2 ? '-8px' : '-6px' }),
                ...(handle.direction.includes('s') && { bottom: handle.direction.length === 2 ? '-8px' : '-6px' }),
                ...(handle.direction.includes('e') && { right: handle.direction.length === 2 ? '-8px' : '-6px' }),
                ...(handle.direction.includes('w') && { left: handle.direction.length === 2 ? '-8px' : '-6px' }),
                ...(handle.direction === 'n' && { left: '50%', transform: 'translateX(-50%)' }),
                ...(handle.direction === 's' && { left: '50%', transform: 'translateX(-50%)' }),
                ...(handle.direction === 'e' && { top: '50%', transform: 'translateY(-50%)' }),
                ...(handle.direction === 'w' && { top: '50%', transform: 'translateY(-50%)' }),
              }}
              onMouseDown={(e) => handleResizeStart(e, handle.direction, handle.cursor)}
              // Prevent any default drag behavior
              draggable={false}
              onDragStart={(e) => e.preventDefault()}
              data-no-dnd="true"
            />
          ))}

          {/* Selection border */}
          {isSelected && (
            <div
              className={cn(
                'bento-component__selection-border',
                'absolute inset-0 pointer-events-none',
                'border-2 border-primary rounded-md',
                'z-10'
              )}
            />
          )}
          
          {/* Resize preview */}
          {isResizing && resizePreview && (
            <div className="absolute bottom-2 right-2 bg-background/90 backdrop-blur-sm border border-border rounded px-2 py-1 text-xs font-mono z-20">
              {resizePreview.w} × {resizePreview.h}
            </div>
          )}
        </>
      )}
    </div>
  );
};

GridComponent.displayName = 'GridComponent';