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
import { Trash2, MoreHorizontal } from 'lucide-react';

// Mobile touch support
import { useTouchTargetValidation } from '@/hooks/useTouchGestures';

import type { GridComponent as GridComponentType } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import {
  getTooltipText,
  getContextualHelp,
  // applyComponentSnapping, // Not used yet
  type HelpMessage
} from '@/utils/gridHelpers';

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
  
  // Mobile-specific props
  /** Whether we're on a mobile device */
  isMobile?: boolean;
  
  /** Whether this is a touch device */
  isTouchDevice?: boolean;
  
  /** Whether mobile drag mode is active */
  dragMode?: boolean;
  
  // Delightful props
  /** Whether this component should wobble for personality */
  shouldWobble?: boolean;
  
  /** Animation delay for staggered entrances */
  animationDelay?: number;
  
  // Event handlers
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
  isMobile = false,
  isTouchDevice = false,
  dragMode = false,
  shouldWobble = false,
  onDelete,
  onResize,
  onShowProperties,
  ...otherProps
}) => {
  const [isResizing, setIsResizing] = useState(false);
  const [isHovering, setIsHovering] = useState(false);
  const [resizePreview, setResizePreview] = useState<{ w: number; h: number } | null>(null);
  const [justAdded, setJustAdded] = useState(false);
  const [celebrateResize] = useState(false);  // setCelebrateResize removed as unused
  
  // Trigger entry animation for new components
  useEffect(() => {
    if (!component.id.includes('temp') && !justAdded) {
      setJustAdded(true);
      const timer = setTimeout(() => setJustAdded(false), 1000);
      return () => clearTimeout(timer);
    }
  }, [component.id, justAdded]);
  const [helpMessage, setHelpMessage] = useState<HelpMessage | null>(null);
  
  // Use ref to track resize state and avoid stale closures
  const isResizingRef = useRef(false);
  const componentRef = useRef<HTMLElement>(null!);
  
  // Touch target validation for mobile
  const { isValidTarget } = useTouchTargetValidation(componentRef);
  
  // Calculate minimum touch-friendly sizes using the sizing system
  const shouldEnforceTouchTargets = isTouchDevice && isEditing;
  
  // Use CSS custom properties for consistent sizing
  const touchTargetSize = 'var(--touch-target-min)';
  const hoverTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Setup draggable functionality with enhanced options
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    isDragging: isDraggableDragging,
  } = useDraggable({
    id: component.id,
    disabled: !isEditing || component.locked,
    // Enhanced drag data for better feedback
    data: {
      type: 'grid-component',
      component,
      originalPosition: component.position,
    },
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


  // Handle component deletion with a playful farewell
  const handleDelete = useCallback((event: React.MouseEvent) => {
    event.stopPropagation();
    
    // Add a little bounce before deletion
    const element = event.currentTarget.closest('.bento-component') as HTMLElement;
    if (element) {
      element.style.animation = 'farewell-bounce 0.3s ease-out';
      setTimeout(() => {
        onDelete?.(component.id);
      }, 300);
    } else {
      onDelete?.(component.id);
    }
  }, [component.id, onDelete]);

  // Handle show properties
  const handleShowProperties = useCallback((event: React.MouseEvent) => {
    event.stopPropagation();
    onShowProperties?.(component.id);
  }, [component.id, onShowProperties]);

  // Handle mouse interactions for enhanced hover states with immediate response
  const handleMouseEnter = useCallback(() => {
    if (!isEditing || component.locked) return;
    
    if (hoverTimeoutRef.current) {
      clearTimeout(hoverTimeoutRef.current);
    }
    
    setIsHovering(true);
  }, [isEditing, component.locked]);
  
  const handleMouseLeave = useCallback(() => {
    if (!isEditing || component.locked) return;
    
    // Immediate response for better UX - no delay
    setIsHovering(false);
  }, [isEditing, component.locked]);

  // Handle resize start
  const handleResizeStart = useCallback((event: React.MouseEvent, direction: string, cursor: string) => {
    // Prevent default behaviors
    event.preventDefault();
    event.stopPropagation();
    
    // Set resize state and show contextual help
    isResizingRef.current = true;
    setIsResizing(true);
    
    // Show contextual help for resize operation
    const helpMsg = getContextualHelp('resize-start', {
      isResizing: true,
      componentCount: 1
    });
    if (helpMsg) {
      setHelpMessage(helpMsg);
    }
    
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
        // For east/south resizes, maintain original position
        onResize?.(component.id, { w: newW, h: newH, x: component.position.x, y: component.position.y });
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

  // Auto-hide help messages
  useEffect(() => {
    if (helpMessage && helpMessage.duration) {
      const timer = setTimeout(() => {
        setHelpMessage(null);
      }, helpMessage.duration);
      
      return () => clearTimeout(timer);
    }
  }, [helpMessage]);

  // Cleanup effect to ensure resize is stopped on unmount
  useEffect(() => {
    const hoverTimeout = hoverTimeoutRef.current;
    return () => {
      // Clean up any active resize operation
      if (isResizingRef.current) {
        isResizingRef.current = false;
        document.body.style.cursor = '';
        document.body.classList.remove('resizing');
        document.body.style.userSelect = '';
      }
      
      // Clean up hover timeout
      if (hoverTimeout) {
        clearTimeout(hoverTimeout);
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
      ref={(node) => {
        setNodeRef(node);
        componentRef.current = node as HTMLElement;
      }}
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
          'bento-component--hovering': isHovering && isEditing && !component.locked,
          'bento-component--wobble': shouldWobble,
          'bento-component--celebrate-resize': celebrateResize,
          'bento-component--just-added': justAdded,
          'bento-component--mobile': isMobile,
          'bento-component--touch-device': isTouchDevice,
          'bento-component--drag-mode': dragMode,
          'bento-component--invalid-target': shouldEnforceTouchTargets && !isValidTarget,
        },
        component.display?.className
      )}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      style={{
        ...gridStyles,
        ...component.display?.style,
        // Smooth opacity transition during drag with better visual feedback
        opacity: isDragging || isDraggableDragging ? 0.3 : 1,
        transform: isDragging || isDraggableDragging ? 'scale(0.95)' : 'scale(1)',
        transition: isDragging || isDraggableDragging ? 'none' : 'opacity 0.2s ease, transform 0.2s ease',
        // Ensure touch-friendly sizing on mobile
        ...(shouldEnforceTouchTargets && {
          minHeight: touchTargetSize,
          minWidth: touchTargetSize,
        }),
      }}
      {...attributes}
      {...otherProps}
      data-component-type={component.componentType}
      data-component-id={component.id}
      data-mobile={isMobile}
      data-touch-device={isTouchDevice}
      data-drag-mode={dragMode}
    >
      {/* Context-aware help messages */}
      {helpMessage && (
        <div
          className={cn(
            'absolute top-0 left-0 z-30 p-2 rounded-md shadow-md typography-caption max-w-48',
            'transition-all duration-300 ease-in-out transform -translate-y-full',
            {
              'bg-success/10 border border-success text-success': helpMessage.type === 'success',
              'bg-primary/10 border border-primary text-primary': helpMessage.type === 'info',
              'bg-warning/10 border border-warning text-warning': helpMessage.type === 'warning',
              'bg-destructive/10 border border-destructive text-destructive': helpMessage.type === 'error',
            }
          )}
        >
          {helpMessage.message}
        </div>
      )}

      {/* Component content with top margin for handle bar in edit mode */}
      <div 
        className={cn(
          "bento-component__content h-full w-full overflow-hidden transition-all duration-200",
          {
            'pt-lg md:pt-xl': isEditing && !component.locked, // Add top padding for handle bar
            'pt-0': !isEditing || component.locked,
          }
        )}
      >
        {children}
      </div>

      {/* Edit mode overlays */}
      {isEditing && !component.locked && (
        <>
          {/* Full-width drag handle bar */}
          <div
            className={cn(
              'bento-component__handle-bar',
              'absolute top-0 left-0 right-0',
              'transition-all duration-200 ease-out',
              'cursor-grab active:cursor-grabbing z-20',
              'flex items-center justify-center',
              {
                'opacity-100 translate-y-0': isHovering || isSelected || dragMode || isEditing,
                'opacity-90 -translate-y-1': !isHovering && !isSelected && !dragMode && isEditing,
              }
            )}
            {...listeners}
            style={{
              height: isTouchDevice ? 'var(--height-sm)' : 'var(--spacing-md)',
              minHeight: shouldEnforceTouchTargets ? touchTargetSize : undefined,
            }}
            title="Drag to move component"
          >
            {/* Handle bar background with subtle gradient */}
            <div className="absolute inset-0 bg-gradient-to-r from-primary/10 via-primary/20 to-primary/10 backdrop-blur-sm border-b border-primary/20 rounded-t-sm" />
            
            {/* Grip indicators spanning full width */}
            <div className="grip-container px-lg">
              {[...Array(16)].map((_, i) => (
                <div 
                  key={i} 
                  className="grip-dot" 
                />
              ))}
            </div>
          </div>


          {/* Component actions with enhanced animation and mobile touch targets */}
          <div
            className={cn(
              'bento-component__actions',
              'absolute top-0 right-0',
              'transition-all duration-150 ease-out',
              'flex bg-background/95 backdrop-blur-sm border border-border rounded-sm shadow-lg',
              isTouchDevice ? 'gap-sm p-xs' : 'gap-xs p-xs',
              {
                'opacity-100 scale-100 translate-x-0 translate-y-0': isHovering || isSelected || dragMode || isEditing,
                'opacity-80 scale-95 translate-x-1 -translate-y-1': !isHovering && !isSelected && !dragMode && isEditing,
              }
            )}
          >
            <Button
              variant="secondary"
              size={isTouchDevice ? "default" : "xs"}
              className={cn(
                isTouchDevice ? "h-lg w-lg p-0" : "h-sm w-sm p-0",
                "touch-manipulation"
              )}
              style={{
                minHeight: shouldEnforceTouchTargets ? touchTargetSize : undefined,
                minWidth: shouldEnforceTouchTargets ? touchTargetSize : undefined,
              }}
              onClick={handleDelete}
              title={getTooltipText('delete-button')}
            >
              <Trash2 className={isTouchDevice ? "icon-sm" : "icon-xs"} />
            </Button>
            
            <Button
              variant="secondary"
              size={isTouchDevice ? "default" : "xs"}
              className={cn(
                isTouchDevice ? "h-lg w-lg p-0" : "h-sm w-sm p-0",
                "touch-manipulation"
              )}
              style={{
                minHeight: shouldEnforceTouchTargets ? touchTargetSize : undefined,
                minWidth: shouldEnforceTouchTargets ? touchTargetSize : undefined,
              }}
              onClick={handleShowProperties}
              title={getTooltipText('properties-button')}
            >
              <MoreHorizontal className={isTouchDevice ? "icon-sm" : "icon-xs"} />
            </Button>
          </div>

          {/* Enhanced resize handles with better grabbability */}
          {RESIZE_HANDLES.map((handle) => {
            const isCorner = handle.direction.length === 2;
            const handleSize = {
              width: isTouchDevice 
                ? (isCorner ? 'var(--height-sm)' : 'var(--spacing-3xl)')
                : (isCorner ? 'var(--spacing-md)' : 'var(--spacing-sm)'),
              height: isTouchDevice 
                ? (isCorner ? 'var(--height-sm)' : 'var(--spacing-3xl)')
                : (isCorner ? 'var(--spacing-md)' : 'var(--spacing-sm)'),
            };
            
            const handleOffset = isTouchDevice ? 'calc(var(--spacing-sm) * -1)' : 'calc(var(--spacing-xs) * -1.25)';
            
            return (
              <div
                key={handle.direction}
                className={cn(
                  'bento-component__resize-handle',
                  handle.className,
                  'absolute',
                  'transition-all duration-150 ease-out',
                  'bg-gradient-to-br from-primary to-primary/80',
                  'border-2 border-primary-foreground/30',
                  'shadow-sm hover:shadow-md',
                  'z-20',
                  'touch-manipulation',
                  {
                    'opacity-100 scale-100': isHovering || isSelected || isResizing || dragMode || isEditing,
                    'opacity-60 scale-90': !isHovering && !isSelected && !isResizing && !dragMode && isEditing,
                    'rounded-full': !isCorner,
                    'rounded-md': isCorner,
                  }
                )}
                style={{
                  cursor: handle.cursor,
                  ...handleSize,
                  // Position based on direction with better spacing
                  ...(handle.direction.includes('n') && { top: handleOffset }),
                  ...(handle.direction.includes('s') && { bottom: handleOffset }),
                  ...(handle.direction.includes('e') && { right: handleOffset }),
                  ...(handle.direction.includes('w') && { left: handleOffset }),
                  ...(handle.direction === 'n' && { left: '50%', transform: 'translateX(-50%)' }),
                  ...(handle.direction === 's' && { left: '50%', transform: 'translateX(-50%)' }),
                  ...(handle.direction === 'e' && { top: '50%', transform: 'translateY(-50%)' }),
                  ...(handle.direction === 'w' && { top: '50%', transform: 'translateY(-50%)' }),
                  // Ensure minimum touch target size
                  ...(shouldEnforceTouchTargets && {
                    minWidth: touchTargetSize,
                    minHeight: touchTargetSize,
                  }),
                }}
                onMouseDown={(e) => handleResizeStart(e, handle.direction, handle.cursor)}
                onTouchStart={(e) => {
                  e.preventDefault();
                  const touch = e.touches[0];
                  const mouseEvent = new MouseEvent('mousedown', {
                    clientX: touch.clientX,
                    clientY: touch.clientY,
                    bubbles: true,
                  });
                  handleResizeStart(mouseEvent as React.MouseEvent, handle.direction, handle.cursor);
                }}
                title={getTooltipText('resize-handle')}
                draggable={false}
                onDragStart={(e) => e.preventDefault()}
                data-no-dnd="true"
              >
                {/* Visual indicator for resize direction */}
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className={cn(
                    "bg-primary-foreground/60 transition-all duration-150",
                    {
                      "w-xs h-md": handle.direction === 'e' || handle.direction === 'w',
                      "w-md h-xs": handle.direction === 'n' || handle.direction === 's',
                      "w-sm h-sm rounded-full": isCorner,
                    }
                  )} />
                </div>
              </div>
            );
          })}

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
            <div className="absolute bottom-sm right-sm bg-background/90 backdrop-blur-sm border border-border rounded-sm px-sm py-xs text-xs z-20">
              {resizePreview.w} × {resizePreview.h}
            </div>
          )}
        </>
      )}
    </div>
  );
};

GridComponent.displayName = 'GridComponent';