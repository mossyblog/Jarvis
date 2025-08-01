/**
 * BentoGrid - Main grid container with drag-and-drop functionality
 * 
 * This is the primary component that renders the Bento grid layout with
 * support for drag-and-drop operations, component positioning, and
 * responsive grid behavior.
 */

import React, { useMemo, useCallback, useState, useRef, useEffect } from 'react';
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

// Mobile touch support
import { useTouchGestures } from '@/hooks/useTouchGestures';

// Bottom sheet for mobile component palette
import BottomSheet, { useBottomSheet } from '@/components/mobile/BottomSheet';

import type { BentoGrid as BentoGridType, GridComponent, GridPosition } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { GridComponent as GridComponentWrapper } from './GridComponent';
import { ComponentRenderer } from './ComponentRenderer';
import { GridOverlay } from './GridOverlay';
import { DragPreview } from './DragPreview';
import {
  applyMagneticSnapping,
  getOptimizedDropZones,
  generateStrategicDropZones,
  isValidPlacement,
  getContextualHelp,
  getTooltipText,
  throttle,
  type DropZone,
  type HelpMessage
} from '@/utils/gridHelpers';

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
  
  /** Whether to show grid overlay */
  showGrid?: boolean;
  
  /** External drag preview (from toolbar) */
  externalDragPreview?: {
    position: { x: number; y: number; w: number; h: number };
    componentType: string;
  } | null;
  
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
  
  /** Called when component properties should be shown */
  onShowProperties?: (componentId: string) => void;
}

interface DragState {
  activeId: string | null;
  draggedComponent: GridComponent | null;
  isDragging: boolean;
  previewPosition: GridPosition | null;
  dropZones: DropZone[];
  isSnapping: boolean;
  helpMessage: HelpMessage | null;
}

interface MobileState {
  isLongPressing: boolean;
  dragMode: boolean;
  zoomLevel: number;
  showMobilePalette: boolean;
}

interface TouchDragState {
  isActive: boolean;
  componentId: string | null;
  startPosition: { x: number; y: number } | null;
  currentPosition: { x: number; y: number } | null;
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
  externalDragPreview,
  className,
  style,
  onComponentMove,
  onComponentResize,
  onComponentSelect,
  onComponentDelete,
  onShowProperties,
  // onGridUpdate,
}) => {
  // State for drag operations and visual feedback
  const [dragState, setDragState] = useState<DragState>({
    activeId: null,
    draggedComponent: null,
    isDragging: false,
    previewPosition: null,
    dropZones: [],
    isSnapping: false,
    helpMessage: null,
  });

  // State for delightful moments
  const [confetti, setConfetti] = useState<{ x: number; y: number; id: string }[]>([]);

  // Mobile-specific state
  const [mobileState, setMobileState] = useState<MobileState>({
    isLongPressing: false,
    dragMode: false,
    zoomLevel: 1,
    showMobilePalette: false,
  });

  // Touch drag state
  const [touchDragState, setTouchDragState] = useState<TouchDragState>({
    isActive: false,
    componentId: null,
    startPosition: null,
    currentPosition: null,
  });

  // Bottom sheet for mobile palette
  const mobilePalette = useBottomSheet();
  const [successMessage, setSuccessMessage] = useState<string>('');
  const [wobbleComponents, setWobbleComponents] = useState<Set<string>>(new Set());
  
  // Grid interaction state for progressive visibility
  const [gridInteractionState, setGridInteractionState] = useState<'idle' | 'hovering' | 'interacting'>('idle');

  // Refs for immediate mouse tracking
  const gridContainerRef = useRef<HTMLDivElement>(null);
  const touchStartTimeRef = useRef<number>(0);
  const longPressTimerRef = useRef<NodeJS.Timeout>();
  const hoverTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Configure enhanced drag sensors with better mobile support
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: deviceType === DeviceType.Mobile ? 10 : 5, // Optimized distances
        delay: deviceType === DeviceType.Mobile ? 150 : 50, // Slight delay for better UX
        tolerance: deviceType === DeviceType.Mobile ? 8 : 5, // Touch tolerance
      },
    })
  );

  // Detect if we're on a touch device
  const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  const isMobile = deviceType === DeviceType.Mobile || (isTouchDevice && window.innerWidth <= 768);

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

  // Enhanced grid style with mobile zoom support
  const enhancedGridStyle = useMemo(() => {
    const baseStyle = { ...gridStyle };
    
    if (isMobile && mobileState.zoomLevel !== 1) {
      baseStyle.transform = `scale(${mobileState.zoomLevel})`;
      baseStyle.transformOrigin = 'top left';
    }
    
    return baseStyle;
  }, [gridStyle, isMobile, mobileState.zoomLevel]);

  // Touch gesture configuration for mobile interactions
  const touchGestureConfig = {
    longPressDelay: 500, // 500ms for long press to enable drag mode
    enablePinch: true, // Enable pinch to zoom
    enableSwipe: true, // Enable swipe gestures
    enableLongPress: isEditing && isMobile, // Only enable long press in edit mode on mobile
  };

  // Touch gesture handlers
  const touchGestureHandlers = {
    onLongPress: useCallback((detail, event) => {
      if (!isEditing || !isMobile) return;
      
      // Find the component under the touch point
      const element = document.elementFromPoint(detail.x, detail.y);
      const componentElement = element?.closest('[data-component-id]') as HTMLElement;
      
      if (componentElement) {
        const componentId = componentElement.dataset.componentId;
        const component = grid.components.find(c => c.id === componentId);
        
        if (component) {
          // Provide haptic feedback if available
          if ('vibrate' in navigator) {
            navigator.vibrate(50);
          }
          
          setMobileState(prev => ({ ...prev, dragMode: true, isLongPressing: true }));
          setTouchDragState({
            isActive: true,
            componentId,
            startPosition: { x: detail.x, y: detail.y },
            currentPosition: { x: detail.x, y: detail.y },
          });
          
          // Prevent default behavior
          event.preventDefault();
        }
      }
    }, [isEditing, isMobile, grid.components]),
    
    onPinch: useCallback((detail, event) => {
      if (!isMobile) return;
      
      const newZoom = Math.max(0.5, Math.min(2, mobileState.zoomLevel * detail.scale));
      setMobileState(prev => ({ ...prev, zoomLevel: newZoom }));
      
      event.preventDefault();
    }, [isMobile, mobileState.zoomLevel]),
    
    onSwipe: useCallback((detail, event) => {
      if (!isMobile) return;
      
      // Swipe up to show component palette
      if (detail.direction === 'up' && detail.velocity > 0.5 && isEditing) {
        mobilePalette.open();
        event.preventDefault();
      }
      
      // Swipe down to hide UI elements or exit edit mode
      if (detail.direction === 'down' && detail.velocity > 0.5) {
        if (mobilePalette.isOpen) {
          mobilePalette.close();
        } else if (mobileState.dragMode) {
          setMobileState(prev => ({ ...prev, dragMode: false }));
        }
        event.preventDefault();
      }
    }, [isMobile, isEditing, mobilePalette, mobileState.dragMode]),
  };

  // Touch gesture hook
  const { attachListeners, isValidTouchTarget } = useTouchGestures(
    touchGestureConfig,
    touchGestureHandlers
  );

  // Attach touch listeners to grid container
  useEffect(() => {
    if (gridContainerRef.current && isMobile) {
      return attachListeners(gridContainerRef.current);
    }
  }, [attachListeners, isMobile]);

  // Handle drag start with smart interaction features
  const handleDragStart = useCallback((event: DragStartEvent) => {
    const { active } = event;
    const componentId = active.id as string;
    const component = grid.components.find(c => c.id === componentId);

    if (component) {
      // Generate strategic drop zones to minimize visual clutter
      const dropZones = generateStrategicDropZones(
        { w: component.position.w, h: component.position.h },
        grid,
        componentId,
        6 // Limit to 6 strategic zones for cleaner UI
      );
      
      // Get contextual help message
      const helpMessage = getContextualHelp('drag-start', {
        isDragging: true,
        componentCount: grid.components.length
      });

      setDragState({
        activeId: componentId,
        draggedComponent: { ...component },
        isDragging: true,
        previewPosition: { ...component.position },
        dropZones,
        isSnapping: false,
        helpMessage,
      });
      
      // Update interaction state for enhanced grid visibility
      setGridInteractionState('interacting');
    }
  }, [grid.components]);

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

  // Optimized real-time mouse position tracker with smooth magnetic snapping
  const updatePreviewPosition = useCallback((mouseX: number, mouseY: number) => {
    if (!dragState.isDragging || !dragState.draggedComponent || !gridContainerRef.current) return;
    
    const gridRect = gridContainerRef.current.getBoundingClientRect();
    const componentSize = { w: dragState.draggedComponent.position.w, h: dragState.draggedComponent.position.h };
    
    // Apply enhanced magnetic snapping with smoother behavior
    const snapResult = applyMagneticSnapping(
      { x: mouseX, y: mouseY },
      gridRect,
      grid,
      componentSize
    );
    
    const newPosition = snapResult.position;
    
    // Check if position is valid with better collision detection
    const otherComponents = grid.components.filter(c => c.id !== dragState.activeId);
    const isValid = isValidPlacement(newPosition, otherComponents, grid.columns);
    
    // Only update if position actually changed to reduce re-renders
    const positionChanged = !dragState.previewPosition || 
      newPosition.x !== dragState.previewPosition.x || 
      newPosition.y !== dragState.previewPosition.y;
    
    const snapChanged = snapResult.snapped !== dragState.isSnapping;
    
    if (positionChanged || snapChanged) {
      // Use requestAnimationFrame for smoother updates
      requestAnimationFrame(() => {
        setDragState(prev => ({
          ...prev,
          previewPosition: newPosition,
          isSnapping: snapResult.snapped,
          helpMessage: isValid ? null : {
            type: 'warning',
            message: 'Cannot place here - position is occupied',
            duration: 1000
          },
        }));
      });
    }
  }, [dragState.isDragging, dragState.draggedComponent, dragState.previewPosition, dragState.activeId, dragState.isSnapping, grid]);

  // Handle drag over (simplified - real tracking happens via mousemove)
  const handleDragOver = useCallback(() => {
    // Keep this for DnD compatibility but use mousemove for immediate feedback
    if (!dragState.draggedComponent) return;
    // The actual preview update happens in mousemove listener for speed
  }, [dragState.draggedComponent]);

  // Trigger confetti celebration
  const triggerCelebration = useCallback((x: number, y: number, message: string) => {
    const confettiPieces = Array.from({ length: 8 }, (_, i) => ({
      x: x + (Math.random() - 0.5) * 100,
      y: y + (Math.random() - 0.5) * 100,
      id: `confetti-${Date.now()}-${i}`,
    }));
    
    setConfetti(confettiPieces);
    setSuccessMessage(message);
    
    // Clear celebration after animation
    setTimeout(() => {
      setConfetti([]);
      setSuccessMessage('');
    }, 2000);
  }, []);

  // Enhanced drag end with smoother position calculation and better snapping
  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { delta } = event;

    const wasSuccessfulMove = Math.abs(delta.x) > 5 || Math.abs(delta.y) > 5;
    
    // Use the preview position if available (more accurate than delta calculation)
    const finalPosition = dragState.previewPosition;
    
    setDragState({
      activeId: null,
      draggedComponent: null,
      isDragging: false,
      previewPosition: null,
      dropZones: [],
      isSnapping: false,
      helpMessage: null,
    });
    
    // Reset interaction state with smooth transition
    setGridInteractionState('idle');

    if (!dragState.draggedComponent || !finalPosition) {
      return;
    }

    // Find the current component data
    const component = grid.components.find(c => c.id === dragState.draggedComponent!.id);
    if (!component) return;

    // Check if position actually changed
    if (finalPosition.x === component.position.x && finalPosition.y === component.position.y) {
      return;
    }

    // Validate the final position
    const otherComponents = grid.components.filter(c => c.id !== component.id);
    const isValidFinalPosition = isValidPlacement(finalPosition, otherComponents, grid.columns);

    if (isValidFinalPosition) {
      // Apply the move with smooth animation
      onComponentMove?.(component.id, finalPosition);
      
      // Trigger celebration for successful moves
      if (wasSuccessfulMove && gridContainerRef.current) {
        const rect = gridContainerRef.current.getBoundingClientRect();
        const centerX = rect.left + (rect.width / 2);
        const centerY = rect.top + (rect.height / 2);
        
        const celebrationMessages = [
          '🎯 Perfect snap!',
          '✨ Smooth move!',
          '🎨 Great positioning!',
          '🚀 Nailed it!',
          '⭐ Perfect fit!',
        ];
        
        const message = celebrationMessages[Math.floor(Math.random() * celebrationMessages.length)];
        triggerCelebration(centerX, centerY, message);
      }
    } else {
      // Position is invalid - component snaps back to original position automatically
      // Show feedback about why the move failed
      setSuccessMessage('❌ Cannot place there - position is occupied');
      setTimeout(() => setSuccessMessage(''), 2000);
    }
  }, [dragState.draggedComponent, dragState.previewPosition, grid.components, grid.columns, onComponentMove, triggerCelebration]);

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

  // Handle show properties
  const handleShowProperties = useCallback((componentId: string) => {
    onShowProperties?.(componentId);
  }, [onShowProperties]);

  // Render icon based on component type - matches toolbar icons
  const renderComponentSkeleton = (componentType: string) => {
    const iconMap: Record<string, string> = {
      'metric-card': '📊',
      'chart': '📈',
      'kpi': '🎯',
      'gauge': '🌡️',
      'table': '📋',
      'list': '📝',
      'grid-view': '🗂️',
      'text-block': '📄',
      'heading': '🔤',
      'card': '🎴',
      'image': '🖼️',
      'video': '🎬',
      'gallery': '🖼️',
      'button': '🔘',
      'button-group': '🎛️',
      'form': '📝'
    };

    const icon = iconMap[componentType] || '📦';
    
    return (
      <div className="h-full w-full flex items-center justify-center">
        <div className="text-4xl opacity-50 animate-bounce">{icon}</div>
      </div>
    );
  };

  // Render empty state with personality
  const renderEmptyState = () => {
    const emptyMessages = [
      { emoji: '🎨', title: 'Ready to create?', subtitle: 'Drag components here to start building' },
      { emoji: '✨', title: 'Your canvas awaits', subtitle: 'Time to make something amazing' },
      { emoji: '🚀', title: 'Launch pad ready', subtitle: 'Drop components to begin your journey' },
      { emoji: '🏗️', title: 'Construction zone', subtitle: 'Build your dream layout here' },
    ];
    
    const message = emptyMessages[Math.floor(Math.random() * emptyMessages.length)];
    
    return (
      <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
        <div className="text-center p-8">
          <div className="text-6xl mb-4 animate-bounce">{message.emoji}</div>
          <h3 className="text-lg font-semibold text-muted-foreground mb-2">{message.title}</h3>
          <p className="text-sm text-muted-foreground">{message.subtitle}</p>
        </div>
      </div>
    );
  };

  // Mouse interaction handlers for progressive grid visibility
  const handleGridMouseEnter = useCallback(() => {
    if (!isEditing) return;
    
    if (hoverTimeoutRef.current) {
      clearTimeout(hoverTimeoutRef.current);
    }
    
    setGridInteractionState('hovering');
  }, [isEditing]);
  
  const handleGridMouseLeave = useCallback(() => {
    if (!isEditing) return;
    
    hoverTimeoutRef.current = setTimeout(() => {
      if (dragState.isDragging) return; // Don't hide during drag
      setGridInteractionState('idle');
    }, 300); // Delay to prevent flickering
  }, [isEditing, dragState.isDragging]);
  
  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (hoverTimeoutRef.current) {
        clearTimeout(hoverTimeoutRef.current);
      }
    };
  }, []);

  // Optimized preview position update with adaptive throttling
  const throttledUpdatePreviewPosition = useMemo(
    () => throttle((x: number, y: number) => updatePreviewPosition(x, y), 8), // ~120fps for smoother feel
    [updatePreviewPosition]
  );

  // Auto-hide help messages
  useEffect(() => {
    if (dragState.helpMessage && dragState.helpMessage.duration) {
      const timer = setTimeout(() => {
        setDragState(prev => ({ ...prev, helpMessage: null }));
      }, dragState.helpMessage.duration);
      
      return () => clearTimeout(timer);
    }
  }, [dragState.helpMessage]);

  // Add mousemove listener for immediate drag feedback
  useEffect(() => {
    const handleMouseMove = (event: MouseEvent) => {
      if (dragState.isDragging) {
        throttledUpdatePreviewPosition(event.clientX, event.clientY);
      }
    };

    if (dragState.isDragging) {
      // Add mousemove listener to document for global tracking
      document.addEventListener('mousemove', handleMouseMove, { passive: true });
      
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
      };
    }
  }, [dragState.isDragging, throttledUpdatePreviewPosition]);

  // Handle touch drag for mobile
  useEffect(() => {
    if (!touchDragState.isActive || !isMobile) return;

    const handleTouchMove = (event: TouchEvent) => {
      if (event.touches.length === 1) {
        const touch = event.touches[0];
        setTouchDragState(prev => ({
          ...prev,
          currentPosition: { x: touch.clientX, y: touch.clientY },
        }));
        
        // Update preview position for touch drag
        updatePreviewPosition(touch.clientX, touch.clientY);
        event.preventDefault();
      }
    };

    const handleTouchEnd = () => {
      setTouchDragState({
        isActive: false,
        componentId: null,
        startPosition: null,
        currentPosition: null,
      });
      setMobileState(prev => ({ ...prev, dragMode: false, isLongPressing: false }));
    };

    document.addEventListener('touchmove', handleTouchMove, { passive: false });
    document.addEventListener('touchend', handleTouchEnd);

    return () => {
      document.removeEventListener('touchmove', handleTouchMove);
      document.removeEventListener('touchend', handleTouchEnd);
    };
  }, [touchDragState.isActive, isMobile, updatePreviewPosition]);

  // Clear long press timer on component mount/unmount
  useEffect(() => {
    return () => {
      if (longPressTimerRef.current) {
        clearTimeout(longPressTimerRef.current);
      }
    };
  }, []);

  return (
    <div className={cn('bento-grid-wrapper', className)}>
      {/* Context-aware help messages */}
      {dragState.helpMessage && (
        <div
          className={cn(
            'fixed top-4 right-4 z-50 p-3 rounded-lg shadow-lg max-w-sm',
            'transition-all duration-300 ease-in-out',
            {
              'bg-green-50 border border-green-200 text-green-800': dragState.helpMessage.type === 'success',
              'bg-blue-50 border border-blue-200 text-blue-800': dragState.helpMessage.type === 'info',
              'bg-yellow-50 border border-yellow-200 text-yellow-800': dragState.helpMessage.type === 'warning',
              'bg-red-50 border border-red-200 text-red-800': dragState.helpMessage.type === 'error',
            }
          )}
        >
          <div className="text-sm font-medium">
            {dragState.helpMessage.message}
          </div>
        </div>
      )}
      <DndContext
        sensors={sensors}
        collisionDetection={gridCollisionDetection}
        onDragStart={handleDragStart}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
      >
        <div 
          className="relative"
          onMouseEnter={handleGridMouseEnter}
          onMouseLeave={handleGridMouseLeave}
        >
          {/* Render grid overlay with progressive visibility */}
          {isEditing && (
            <GridOverlay
              columns={grid.columns}
              gap={grid.gap}
              rowHeight={grid.rowHeight}
              interactionState={gridInteractionState}
              forceVisible={showGrid}
            />
          )}
          
          
          <div
            ref={gridContainerRef}
            className={cn(
              'bento-grid',
              'relative',
              {
                'bento-grid--editing': isEditing,
                'bento-grid--show-grid': showGrid,
                'bento-grid--hovering': gridInteractionState === 'hovering',
                'bento-grid--interacting': gridInteractionState === 'interacting',
                'bento-grid--mobile': isMobile,
                'bento-grid--touch-device': isTouchDevice,
                'bento-grid--drag-mode': mobileState.dragMode,
                'bento-grid--zoomed': mobileState.zoomLevel !== 1,
                'bento-grid--empty': grid.components.length === 0,
              }
            )}
            style={enhancedGridStyle}
            data-touch-enabled={isTouchDevice}
          >
            {/* Empty state with personality */}
            {grid.components.length === 0 && isEditing && renderEmptyState()}
            
            {/* Strategic drop zone indicators - non-overlapping and clearly positioned */}
            {dragState.isDragging && dragState.dropZones.map((zone, index) => {
              return (
                <div
                  key={`drop-zone-${zone.position.x}-${zone.position.y}-${index}`}
                  className={cn(
                    "pointer-events-none z-[3] rounded-lg border-2 border-dashed transition-all duration-200",
                    "border-green-400 bg-green-400/8 shadow-sm"
                  )}
                  style={{
                    gridColumn: `${zone.position.x + 1} / span ${zone.position.w}`,
                    gridRow: `${zone.position.y + 1} / span ${zone.position.h}`,
                    opacity: 0.7,
                    animation: `drop-zone-breathe 2s ease-in-out infinite ${index * 0.2}s`,
                  }}
                  title={getTooltipText('drop-zone-valid')}
                >
                  {/* Subtle indicator dot */}
                  <div className="absolute top-2 left-2 w-2 h-2 bg-green-500 rounded-full opacity-60" />
                </div>
              );
            })}
            
            {/* Enhanced drag preview with clear z-index layering */}
            {dragState.isDragging && dragState.previewPosition && dragState.draggedComponent && (() => {
              const otherComponents = grid.components.filter(c => c.id !== dragState.activeId);
              const isValidPos = isValidPlacement(dragState.previewPosition!, otherComponents, grid.columns);
              
              return (
                <div
                  className={cn(
                    "pointer-events-none z-[15] rounded-lg border-2 border-dashed",
                    "transition-all duration-75 ease-out",
                    dragState.isSnapping && "magnetic-snap"
                  )}
                  style={{
                    gridColumn: `${dragState.previewPosition.x + 1} / span ${dragState.previewPosition.w}`,
                    gridRow: `${dragState.previewPosition.y + 1} / span ${dragState.previewPosition.h}`,
                    borderColor: isValidPos ? '#22c55e' : '#ef4444',
                    backgroundColor: isValidPos 
                      ? 'rgba(34, 197, 94, 0.15)' 
                      : 'rgba(239, 68, 68, 0.15)',
                    boxShadow: dragState.isSnapping 
                      ? '0 0 0 4px rgba(59, 130, 246, 0.4), 0 8px 25px -5px rgba(0, 0, 0, 0.1)' 
                      : isValidPos 
                        ? '0 4px 12px rgba(34, 197, 94, 0.25)'
                        : '0 4px 12px rgba(239, 68, 68, 0.25)',
                  }}
                >
                  {/* Enhanced skeleton preview with better visual feedback */}
                  <div className="h-full w-full relative opacity-95 bg-background/98 backdrop-blur-md rounded-lg p-4 border border-border/40">
                    {renderComponentSkeleton(dragState.draggedComponent.componentType)}
                    
                    {/* Enhanced snapping indicator */}
                    {dragState.isSnapping && (
                      <div className="absolute top-2 left-2 flex items-center gap-1 text-xs font-medium text-blue-600 bg-blue-50 px-2 py-1 rounded-full border border-blue-200">
                        <div className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-pulse" />
                        Snapped
                      </div>
                    )}
                    
                    {/* Enhanced status indicator */}
                    <div className={cn(
                      'absolute top-2 right-2 w-7 h-7 rounded-full border-2',
                      'flex items-center justify-center text-sm font-bold',
                      'transition-all duration-150 shadow-md',
                      isValidPos 
                        ? 'bg-green-500 border-green-400 text-white animate-pulse' 
                        : 'bg-red-500 border-red-400 text-white'
                    )}>
                      {isValidPos ? '✓' : '✕'}
                    </div>
                    
                    {/* Grid position indicator */}
                    <div className="absolute bottom-2 left-2 text-xs font-mono bg-background/90 px-2 py-1 rounded border border-border/60 shadow-sm">
                      ({dragState.previewPosition.x}, {dragState.previewPosition.y})
                    </div>
                  </div>
                </div>
              );
            })()}

            {/* External drag preview with clear layering */}
            {externalDragPreview && (() => {
              const isValidPlacement = !checkCollision(externalDragPreview.position);
              const feedbackClass = isValidPlacement 
                ? 'drop-zone-valid'
                : 'drop-zone-invalid';
              
              return (
                <div
                  className={cn(
                    'pointer-events-none z-[12] rounded-lg border-2 border-dashed',
                    'drop-zone-preview transition-all duration-200',
                    feedbackClass
                  )}
                  style={{
                    gridColumn: `${externalDragPreview.position.x + 1} / span ${externalDragPreview.position.w}`,
                    gridRow: `${externalDragPreview.position.y + 1} / span ${externalDragPreview.position.h}`,
                  }}
                >
                  {/* Enhanced preview skeleton */}
                  <div className="h-full w-full relative opacity-85 bg-background/95 backdrop-blur-md rounded-lg p-4 border border-border/50 shadow-lg">
                    {renderComponentSkeleton(externalDragPreview.componentType)}
                    
                    {/* Status indicator */}
                    <div className={cn(
                      'absolute top-2 right-2 w-5 h-5 rounded-full border',
                      'flex items-center justify-center text-xs font-bold shadow-sm',
                      isValidPlacement 
                        ? 'bg-green-500 border-green-400 text-white' 
                        : 'bg-red-500 border-red-400 text-white'
                    )}>
                      {isValidPlacement ? '✓' : '✕'}
                    </div>
                  </div>
                </div>
              );
            })()}

            {/* Render grid components with delightful touches */}
            {grid.components.map((component, index) => (
              <GridComponentWrapper
                key={component.id}
                component={component}
                isEditing={isEditing}
                isSelected={false} // You might want to track selected state
                isDragging={dragState.activeId === component.id || touchDragState.componentId === component.id}
                deviceType={deviceType}
                isMobile={isMobile}
                isTouchDevice={isTouchDevice}
                dragMode={mobileState.dragMode}
                onSelect={handleComponentSelect}
                onDelete={handleComponentDelete}
                onResize={handleComponentResize}
                onShowProperties={handleShowProperties}
                // Add delightful props
                shouldWobble={wobbleComponents.has(component.id)}
                animationDelay={index * 50} // Stagger component animations
                data-component-id={component.id}
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
            
            {/* Mobile drag indicator */}
            {isMobile && mobileState.dragMode && (
              <div className="fixed top-4 left-4 right-4 z-50 bg-primary text-primary-foreground px-4 py-2 rounded-lg text-sm font-medium text-center">
                Drag mode active - Move components around
              </div>
            )}
          </div>
        </div>

        {/* Delightful celebration overlay */}
        {confetti.length > 0 && (
          <div className="fixed inset-0 pointer-events-none z-[9999]">
            {confetti.map((piece) => (
              <div
                key={piece.id}
                className="absolute animate-confetti"
                style={{
                  left: piece.x,
                  top: piece.y,
                  fontSize: '1.5rem',
                }}
              >
                {['🎉', '✨', '🌟', '🎊', '💫'][Math.floor(Math.random() * 5)]}
              </div>
            ))}
          </div>
        )}

        {/* Success message overlay */}
        {successMessage && (
          <div className="fixed top-4 left-1/2 transform -translate-x-1/2 z-[9999] pointer-events-none">
            <div className="bg-green-500 text-white px-4 py-2 rounded-full shadow-lg animate-bounce-in font-medium">
              {successMessage}
            </div>
          </div>
        )}

        {/* Drag overlay for visual feedback */}
        <DragOverlay>
          {dragState.activeId && dragState.draggedComponent ? (
            <DragPreview
              component={dragState.draggedComponent}
              deviceType={deviceType}
              simplified={true}
            />
          ) : null}
        </DragOverlay>
      </DndContext>
      
      {/* Mobile component palette bottom sheet */}
      {isMobile && (
        <BottomSheet
          isOpen={mobilePalette.isOpen}
          onClose={mobilePalette.close}
          title="Components"
          initialHeight={0.4}
          maxHeight={0.8}
          minHeight={0.2}
          showHandle={true}
        >
          <div className="p-4">
            <div className="grid grid-cols-2 gap-4">
              {/* Component palette items with proper touch targets */}
              {[
                { type: 'metric-card', icon: '📊', name: 'Metric Card' },
                { type: 'chart', icon: '📈', name: 'Chart' },
                { type: 'kpi', icon: '🎯', name: 'KPI' },
                { type: 'gauge', icon: '🌡️', name: 'Gauge' },
                { type: 'table', icon: '📋', name: 'Table' },
                { type: 'list', icon: '📝', name: 'List' },
                { type: 'text-block', icon: '📄', name: 'Text Block' },
                { type: 'button', icon: '🔘', name: 'Button' },
              ].map((componentType) => (
                <button
                  key={componentType.type}
                  className="flex flex-col items-center justify-center p-4 bg-card border border-border rounded-lg hover:bg-accent transition-colors"
                  style={{ minHeight: '88px', minWidth: '88px' }} // Double minimum touch target for easier selection
                  onClick={() => {
                    // Add component logic here
                    console.log('Add component:', componentType.type);
                    mobilePalette.close();
                  }}
                >
                  <div className="text-2xl mb-2">{componentType.icon}</div>
                  <div className="text-xs text-center font-medium">{componentType.name}</div>
                </button>
              ))}
            </div>
            
            {/* Mobile-specific actions */}
            <div className="mt-6 space-y-3">
              <button
                className="w-full flex items-center justify-center gap-2 p-3 bg-secondary text-secondary-foreground rounded-lg font-medium"
                style={{ minHeight: '48px' }}
                onClick={() => {
                  setMobileState(prev => ({ ...prev, dragMode: !prev.dragMode }));
                  mobilePalette.close();
                }}
              >
                {mobileState.dragMode ? '🔒 Exit Drag Mode' : '✋ Enable Drag Mode'}
              </button>
              
              <button
                className="w-full flex items-center justify-center gap-2 p-3 bg-secondary text-secondary-foreground rounded-lg font-medium"
                style={{ minHeight: '48px' }}
                onClick={() => {
                  setMobileState(prev => ({ ...prev, zoomLevel: prev.zoomLevel === 1 ? 0.8 : 1 }));
                }}
              >
                {mobileState.zoomLevel === 1 ? '🔍 Zoom Out' : '🔍 Reset Zoom'}
              </button>
            </div>
          </div>
        </BottomSheet>
      )}
    </div>
  );
};

BentoGrid.displayName = 'BentoGrid';