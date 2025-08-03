/**
 * BottomSheet - Mobile-optimized bottom sheet component
 * 
 * A mobile-first bottom sheet component that provides an intuitive way to
 * display component palettes, settings, and other content on mobile devices.
 * Supports swipe gestures, backdrop dismissal, and smooth animations.
 */

import React, { useEffect, useRef, useState, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { X, GripHorizontal } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { useTouchGestures } from '@/hooks/useTouchGestures';

// ============================================================================
// Types
// ============================================================================

export interface BottomSheetProps {
  /** Whether the bottom sheet is open */
  isOpen: boolean;
  /** Called when the sheet should be closed */
  onClose: () => void;
  /** Content to display in the sheet */
  children: React.ReactNode;
  /** Title for the sheet */
  title?: string;
  /** Initial height as percentage of viewport height (0-1) */
  initialHeight?: number;
  /** Maximum height as percentage of viewport height (0-1) */
  maxHeight?: number;
  /** Minimum height as percentage of viewport height (0-1) */
  minHeight?: number;
  /** Whether the sheet can be dismissed by tapping the backdrop */
  dismissOnBackdrop?: boolean;
  /** Whether to show the drag handle */
  showHandle?: boolean;
  /** Additional CSS classes */
  className?: string;
  /** Disable swipe to dismiss */
  disableSwipeDown?: boolean;
  /** Custom z-index */
  zIndex?: number;
}

interface DragState {
  isDragging: boolean;
  startY: number;
  startHeight: number;
  currentHeight: number;
}

// ============================================================================
// Constants
// ============================================================================

const SNAP_THRESHOLD = 0.1; // 10% of viewport height
const ANIMATION_DURATION = 300; // ms
const BACKDROP_OPACITY = 0.5;

// ============================================================================
// Main Component
// ============================================================================

export const BottomSheet: React.FC<BottomSheetProps> = ({
  isOpen,
  onClose,
  children,
  title,
  initialHeight = 0.5,
  maxHeight = 0.9,
  minHeight = 0.2,
  dismissOnBackdrop = true,
  showHandle = true,
  className,
  disableSwipeDown = false,
  zIndex = 1000,
}) => {
  const sheetRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const backdropRef = useRef<HTMLDivElement>(null);
  
  const [dragState, setDragState] = useState<DragState>({
    isDragging: false,
    startY: 0,
    startHeight: initialHeight,
    currentHeight: initialHeight,
  });
  
  const [isAnimating, setIsAnimating] = useState(false);
  const [mountNode, setMountNode] = useState<HTMLElement | null>(null);

  // Create portal mount point
  useEffect(() => {
    const node = document.createElement('div');
    node.id = 'bottom-sheet-portal';
    document.body.appendChild(node);
    setMountNode(node);
    
    return () => {
      if (document.body.contains(node)) {
        document.body.removeChild(node);
      }
    };
  }, []);

  // Handle keyboard navigation
  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      switch (event.key) {
        case 'Escape':
          event.preventDefault();
          onClose();
          break;
        case 'Tab':
          // Trap focus within the sheet
          const focusableElements = sheetRef.current?.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
          ) as NodeListOf<HTMLElement>;
          
          if (focusableElements && focusableElements.length > 0) {
            const firstElement = focusableElements[0];
            const lastElement = focusableElements[focusableElements.length - 1];
            
            if (event.shiftKey) {
              if (document.activeElement === firstElement) {
                event.preventDefault();
                lastElement.focus();
              }
            } else {
              if (document.activeElement === lastElement) {
                event.preventDefault();
                firstElement.focus();
              }
            }
          }
          break;
        case 'ArrowUp':
          if (event.ctrlKey || event.metaKey) {
            event.preventDefault();
            animateHeight(maxHeight);
          }
          break;
        case 'ArrowDown':
          if (event.ctrlKey || event.metaKey) {
            event.preventDefault();
            if (dragState.currentHeight > minHeight) {
              animateHeight(minHeight);
            } else {
              handleClose();
            }
          }
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, maxHeight, minHeight, dragState.currentHeight]); // Removed forward references

  // Prevent body scroll when open
  useEffect(() => {
    if (isOpen) {
      const originalOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
      
      return () => {
        document.body.style.overflow = originalOverflow;
      };
    }
  }, [isOpen]);

  // Animation helper
  const animateHeight = useCallback((targetHeight: number, duration = ANIMATION_DURATION) => {
    if (!sheetRef.current) return;

    setIsAnimating(true);
    sheetRef.current.style.transition = `transform ${duration}ms cubic-bezier(0.4, 0.0, 0.2, 1)`;
    
    setDragState(prev => ({ ...prev, currentHeight: targetHeight }));
    
    setTimeout(() => {
      setIsAnimating(false);
      if (sheetRef.current) {
        sheetRef.current.style.transition = '';
      }
    }, duration);
  }, []);

  // Close with animation
  const handleClose = useCallback(() => {
    if (isAnimating) return;
    
    animateHeight(0, ANIMATION_DURATION);
    setTimeout(onClose, ANIMATION_DURATION);
  }, [animateHeight, onClose, isAnimating]);

  // Snap to nearest position
  const snapToPosition = useCallback((currentHeight: number) => {
    const positions = [minHeight, initialHeight, maxHeight];
    let closestPosition = positions[0];
    let minDistance = Math.abs(currentHeight - positions[0]);

    for (const position of positions) {
      const distance = Math.abs(currentHeight - position);
      if (distance < minDistance) {
        minDistance = distance;
        closestPosition = position;
      }
    }

    // If dragged below minimum, close the sheet
    if (currentHeight < minHeight - SNAP_THRESHOLD) {
      handleClose();
      return;
    }

    animateHeight(closestPosition);
  }, [minHeight, initialHeight, maxHeight, animateHeight, handleClose]);

  // Touch gesture handlers
  const { attachListeners } = useTouchGestures(
    {
      enableSwipe: !disableSwipeDown,
      enableLongPress: false,
      enablePinch: false,
      enableDoubleTap: false,
      swipeThreshold: 20,
    },
    {
      onSwipe: (detail, event) => {
        if (detail.direction === 'down' && detail.velocity > 0.5) {
          event.preventDefault();
          handleClose();
        }
      },
    }
  );

  // Mouse/touch drag handlers
  const handleDragStart = useCallback((clientY: number) => {
    if (isAnimating) return;

    setDragState(prev => ({
      ...prev,
      isDragging: true,
      startY: clientY,
      startHeight: prev.currentHeight,
    }));
  }, [isAnimating]);

  const handleDragMove = useCallback((clientY: number) => {
    if (!dragState.isDragging) return;

    const deltaY = dragState.startY - clientY;
    const viewportHeight = window.innerHeight;
    const deltaHeight = deltaY / viewportHeight;
    const newHeight = Math.max(0, Math.min(maxHeight, dragState.startHeight + deltaHeight));

    setDragState(prev => ({ ...prev, currentHeight: newHeight }));
  }, [dragState.isDragging, dragState.startY, dragState.startHeight, maxHeight]);

  const handleDragEnd = useCallback(() => {
    if (!dragState.isDragging) return;

    setDragState(prev => ({ ...prev, isDragging: false }));
    snapToPosition(dragState.currentHeight);
  }, [dragState.isDragging, dragState.currentHeight, snapToPosition]);

  // Mouse event handlers
  const handleMouseDown = useCallback((event: React.MouseEvent) => {
    event.preventDefault();
    handleDragStart(event.clientY);
  }, [handleDragStart]);

  // Touch event handlers for the handle
  const handleTouchStart = useCallback((event: React.TouchEvent) => {
    if (event.touches.length === 1) {
      event.preventDefault();
      handleDragStart(event.touches[0].clientY);
    }
  }, [handleDragStart]);

  // Global mouse/touch move and end handlers
  useEffect(() => {
    if (!dragState.isDragging) return;

    const handleMouseMove = (event: MouseEvent) => {
      event.preventDefault();
      handleDragMove(event.clientY);
    };

    const handleTouchMove = (event: TouchEvent) => {
      if (event.touches.length === 1) {
        event.preventDefault();
        handleDragMove(event.touches[0].clientY);
      }
    };

    const handleEnd = () => {
      handleDragEnd();
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleEnd);
    document.addEventListener('touchmove', handleTouchMove, { passive: false });
    document.addEventListener('touchend', handleEnd);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleEnd);
      document.removeEventListener('touchmove', handleTouchMove);
      document.removeEventListener('touchend', handleEnd);
    };
  }, [dragState.isDragging, handleDragMove, handleDragEnd]);

  // Reset height when sheet opens
  useEffect(() => {
    if (isOpen && !dragState.isDragging) {
      setDragState(prev => ({ ...prev, currentHeight: initialHeight }));
    }
  }, [isOpen, initialHeight, dragState.isDragging]);

  // Attach touch listeners to the sheet
  useEffect(() => {
    if (sheetRef.current && !disableSwipeDown) {
      return attachListeners(sheetRef.current);
    }
  }, [attachListeners, disableSwipeDown]);


  // Backdrop click handler
  const handleBackdropClick = useCallback((event: React.MouseEvent) => {
    if (dismissOnBackdrop && event.target === event.currentTarget) {
      handleClose();
    }
  }, [dismissOnBackdrop, handleClose]);

  if (!isOpen || !mountNode) {
    return null;
  }

  return createPortal(
    <div
      className="fixed inset-0 touch-none"
      style={{ zIndex }}
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? 'bottom-sheet-title' : undefined}
    >
      {/* Backdrop */}
      <div
        ref={backdropRef}
        className="absolute inset-0 bg-black transition-opacity duration-300"
        style={{
          opacity: isOpen ? BACKDROP_OPACITY * dragState.currentHeight : 0,
        }}
        onClick={handleBackdropClick}
      />
      
      {/* Sheet */}
      <div
        ref={sheetRef}
        className={cn(
          'absolute bottom-0 left-0 right-0 bg-background border-t border-border',
          'rounded-t-xl shadow-2xl',
          'will-change-transform',
          {
            'transition-none': dragState.isDragging,
          },
          className
        )}
        style={{
          height: `${dragState.currentHeight * 100}vh`,
          transform: isOpen ? 'translateY(0)' : 'translateY(100%)',
          maxHeight: `${maxHeight * 100}vh`,
          minHeight: `${minHeight * 100}vh`,
        }}
      >
        {/* Drag Handle */}
        {showHandle && (
          <div
            className="flex justify-center py-3 cursor-grab active:cursor-grabbing touch-none"
            onMouseDown={handleMouseDown}
            onTouchStart={handleTouchStart}
            style={{
              minHeight: '44px', // Minimum touch target
            }}
          >
            <GripHorizontal
              className="text-muted-foreground"
              size={24}
            />
          </div>
        )}
        
        {/* Header */}
        {title && (
          <div className="flex items-center justify-between px-4 py-3 border-b border-border">
            <h2
              id="bottom-sheet-title"
              className="text-lg font-semibold text-foreground"
            >
              {title}
            </h2>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClose}
              className="h-9 w-9 p-0 hover:bg-muted"
              aria-label="Close sheet"
            >
              <X size={18} />
            </Button>
          </div>
        )}
        
        {/* Content */}
        <div
          ref={contentRef}
          className="flex-1 overflow-auto overscroll-contain"
          style={{
            height: title
              ? `calc(${dragState.currentHeight * 100}vh - 120px)` // Account for header and handle
              : `calc(${dragState.currentHeight * 100}vh - 72px)`, // Account for handle only
          }}
        >
          {children}
        </div>
      </div>
    </div>,
    mountNode
  );
};

// ============================================================================
// Hook for BottomSheet State Management
// ============================================================================

// eslint-disable-next-line react-refresh/only-export-components
export const useBottomSheet = (initialState = false) => {
  const [isOpen, setIsOpen] = useState(initialState);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen(prev => !prev), []);

  return {
    isOpen,
    open,
    close,
    toggle,
  };
};

BottomSheet.displayName = 'BottomSheet';
export default BottomSheet;