/**
 * useTouchGestures - Touch gesture detection and handling hook
 * 
 * Provides comprehensive touch gesture support including:
 * - Long-press detection for mobile drag mode
 * - Pinch-to-zoom functionality
 * - Swipe gestures for UI interactions
 * - Touch target validation
 */

import { useCallback, useRef, useEffect, useState, useMemo } from 'react';

// ============================================================================
// Types
// ============================================================================

export interface TouchPoint {
  id: number;
  x: number;
  y: number;
  timestamp: number;
}

export interface GestureEvent {
  type: 'longpress' | 'pinch' | 'swipe' | 'tap';
  detail: unknown;
  originalEvent: TouchEvent;
}

export interface LongPressDetail {
  x: number;
  y: number;
  duration: number;
}

export interface PinchDetail {
  scale: number;
  deltaScale: number;
  center: { x: number; y: number };
}

export interface SwipeDetail {
  direction: 'up' | 'down' | 'left' | 'right';
  distance: number;
  velocity: number;
  duration: number;
}

export interface TapDetail {
  x: number;
  y: number;
  tapCount: number;
}

export interface TouchGestureConfig {
  /** Minimum duration for long press in ms */
  longPressDelay?: number;
  /** Minimum distance for swipe gesture */
  swipeThreshold?: number;
  /** Maximum time for swipe gesture */
  swipeTimeout?: number;
  /** Minimum scale change for pinch */
  pinchThreshold?: number;
  /** Maximum time between taps for double tap */
  doubleTapDelay?: number;
  /** Enable specific gestures */
  enableLongPress?: boolean;
  enablePinch?: boolean;
  enableSwipe?: boolean;
  enableDoubleTap?: boolean;
}

export interface TouchGestureHandlers {
  onLongPress?: (detail: LongPressDetail, event: TouchEvent) => void;
  onPinch?: (detail: PinchDetail, event: TouchEvent) => void;
  onSwipe?: (detail: SwipeDetail, event: TouchEvent) => void;
  onTap?: (detail: TapDetail, event: TouchEvent) => void;
  onDoubleTap?: (detail: TapDetail, event: TouchEvent) => void;
}

// ============================================================================
// Constants
// ============================================================================

const DEFAULT_CONFIG: Required<TouchGestureConfig> = {
  longPressDelay: 500,
  swipeThreshold: 50,
  swipeTimeout: 300,
  pinchThreshold: 0.1,
  doubleTapDelay: 300,
  enableLongPress: true,
  enablePinch: true,
  enableSwipe: true,
  enableDoubleTap: true,
};

// Minimum touch target size according to accessibility guidelines
export const MINIMUM_TOUCH_TARGET_SIZE = 44; // 44px minimum

// ============================================================================
// Helper Functions
// ============================================================================

const getDistance = (p1: TouchPoint, p2: TouchPoint): number => {
  const dx = p1.x - p2.x;
  const dy = p1.y - p2.y;
  return Math.sqrt(dx * dx + dy * dy);
};

const getCenter = (p1: TouchPoint, p2: TouchPoint): { x: number; y: number } => ({
  x: (p1.x + p2.x) / 2,
  y: (p1.y + p2.y) / 2,
});

const getTouchPoint = (touch: Touch): TouchPoint => ({
  id: touch.identifier,
  x: touch.clientX,
  y: touch.clientY,
  timestamp: Date.now(),
});

const getSwipeDirection = (startX: number, startY: number, endX: number, endY: number): SwipeDetail['direction'] => {
  const deltaX = endX - startX;
  const deltaY = endY - startY;
  
  if (Math.abs(deltaX) > Math.abs(deltaY)) {
    return deltaX > 0 ? 'right' : 'left';
  } else {
    return deltaY > 0 ? 'down' : 'up';
  }
};

const isValidTouchTarget = (element: HTMLElement): boolean => {
  // const rect = element.getBoundingClientRect();  // Unused variable
  const minSize = MINIMUM_TOUCH_TARGET_SIZE;
  
  // Check if the element or its clickable parent meets minimum size requirements
  let currentElement: HTMLElement | null = element;
  
  while (currentElement) {
    const currentRect = currentElement.getBoundingClientRect();
    
    if (currentRect.width >= minSize && currentRect.height >= minSize) {
      return true;
    }
    
    // Check if element is interactive
    const isInteractive = currentElement.matches('button, input, select, textarea, a, [role="button"], [tabindex]');
    if (isInteractive) {
      // For interactive elements, we can be more lenient but still warn
      console.warn(`Touch target "${currentElement.tagName}" is smaller than recommended ${minSize}px`, currentElement);
      return true;
    }
    
    currentElement = currentElement.parentElement;
  }
  
  return false;
};

// ============================================================================
// Main Hook
// ============================================================================

export const useTouchGestures = (
  config: TouchGestureConfig = {},
  handlers: TouchGestureHandlers = {}
) => {
  const mergedConfig = useMemo(() => ({ ...DEFAULT_CONFIG, ...config }), [config]);
  const touchPoints = useRef<Map<number, TouchPoint>>(new Map());
  const longPressTimer = useRef<NodeJS.Timeout | undefined>();
  const lastTapTime = useRef<number>(0);
  const tapCount = useRef<number>(0);
  const initialDistance = useRef<number>(0);
  const lastScale = useRef<number>(1);
  const startTouchPoint = useRef<TouchPoint | null>(null);
  
  // State for gesture detection
  const [isLongPressing, setIsLongPressing] = useState(false);
  const [isPinching, setIsPinching] = useState(false);
  const [currentScale, setCurrentScale] = useState(1);

  // Clear long press timer
  const clearLongPressTimer = useCallback(() => {
    if (longPressTimer.current) {
      clearTimeout(longPressTimer.current);
      longPressTimer.current = undefined;
    }
    setIsLongPressing(false);
  }, []);

  // Handle touch start
  const handleTouchStart = useCallback((event: TouchEvent) => {
    const touches = Array.from(event.touches).map(getTouchPoint);
    
    // Update touch points
    touches.forEach(touch => {
      touchPoints.current.set(touch.id, touch);
    });

    if (touches.length === 1) {
      // Single touch - potential tap or long press
      const touch = touches[0];
      startTouchPoint.current = touch;
      
      // Validate touch target
      const target = event.target as HTMLElement;
      if (!isValidTouchTarget(target)) {
        console.warn('Touch target is too small or not interactive', target);
      }
      
      // Start long press timer
      if (mergedConfig.enableLongPress) {
        clearLongPressTimer();
        longPressTimer.current = setTimeout(() => {
          setIsLongPressing(true);
          handlers.onLongPress?.({
            x: touch.x,
            y: touch.y,
            duration: mergedConfig.longPressDelay,
          }, event);
        }, mergedConfig.longPressDelay);
      }
      
    } else if (touches.length === 2) {
      // Two touches - potential pinch
      clearLongPressTimer();
      
      if (mergedConfig.enablePinch) {
        const [touch1, touch2] = touches;
        initialDistance.current = getDistance(touch1, touch2);
        lastScale.current = 1;
        setIsPinching(true);
      }
    } else {
      // More than two touches - clear all gestures
      clearLongPressTimer();
      setIsPinching(false);
    }
  }, [mergedConfig, handlers, clearLongPressTimer]);

  // Handle touch move
  const handleTouchMove = useCallback((event: TouchEvent) => {
    const touches = Array.from(event.touches).map(getTouchPoint);
    
    // Update touch points
    touches.forEach(touch => {
      touchPoints.current.set(touch.id, touch);
    });

    if (touches.length === 1) {
      // Single touch movement - cancel long press if moved too far
      const touch = touches[0];
      const startTouch = startTouchPoint.current;
      
      if (startTouch) {
        const distance = getDistance(startTouch, touch);
        if (distance > 10) { // 10px threshold for movement
          clearLongPressTimer();
        }
      }
    } else if (touches.length === 2 && mergedConfig.enablePinch && isPinching) {
      // Two touches - handle pinch
      const [touch1, touch2] = touches;
      const currentDistance = getDistance(touch1, touch2);
      const scale = currentDistance / initialDistance.current;
      const deltaScale = scale - lastScale.current;
      
      if (Math.abs(deltaScale) >= mergedConfig.pinchThreshold) {
        const center = getCenter(touch1, touch2);
        
        setCurrentScale(scale);
        lastScale.current = scale;
        
        handlers.onPinch?.({
          scale,
          deltaScale,
          center,
        }, event);
      }
    }
  }, [mergedConfig, handlers, isPinching, clearLongPressTimer]);

  // Handle touch end
  const handleTouchEnd = useCallback((event: TouchEvent) => {
    const endTime = Date.now();
    const remainingTouches = Array.from(event.touches);
    
    if (remainingTouches.length === 0) {
      // All touches ended
      const wasLongPressing = isLongPressing;
      
      clearLongPressTimer();
      setIsPinching(false);
      
      // Handle tap/double tap if not long pressing
      if (!wasLongPressing && startTouchPoint.current) {
        const startTouch = startTouchPoint.current;
        
        // Check for swipe gesture
        if (mergedConfig.enableSwipe) {
          const changedTouches = Array.from(event.changedTouches).map(getTouchPoint);
          const endTouch = changedTouches[0];
          
          if (endTouch) {
            const distance = getDistance(startTouch, endTouch);
            const duration = endTime - startTouch.timestamp;
            
            if (distance >= mergedConfig.swipeThreshold && duration <= mergedConfig.swipeTimeout) {
              const direction = getSwipeDirection(startTouch.x, startTouch.y, endTouch.x, endTouch.y);
              const velocity = distance / duration;
              
              handlers.onSwipe?.({
                direction,
                distance,
                velocity,
                duration,
              }, event);
              
              // Don't process tap if we detected a swipe
              touchPoints.current.clear();
              startTouchPoint.current = null;
              return;
            }
          }
        }
        
        // Handle tap
        const timeSinceLastTap = endTime - lastTapTime.current;
        
        if (timeSinceLastTap <= mergedConfig.doubleTapDelay) {
          tapCount.current++;
        } else {
          tapCount.current = 1;
        }
        
        lastTapTime.current = endTime;
        
        const tapDetail: TapDetail = {
          x: startTouch.x,
          y: startTouch.y,
          tapCount: tapCount.current,
        };
        
        if (tapCount.current === 2 && mergedConfig.enableDoubleTap) {
          handlers.onDoubleTap?.(tapDetail, event);
          tapCount.current = 0; // Reset after double tap
        } else {
          // Single tap - wait a bit to see if there's a second tap
          setTimeout(() => {
            if (tapCount.current === 1) {
              handlers.onTap?.(tapDetail, event);
              tapCount.current = 0;
            }
          }, mergedConfig.doubleTapDelay);
        }
      }
      
      // Clear state
      touchPoints.current.clear();
      startTouchPoint.current = null;
      setCurrentScale(1);
    }
  }, [
    mergedConfig,
    handlers,
    isLongPressing,
    clearLongPressTimer,
  ]);

  // Handle touch cancel
  const handleTouchCancel = useCallback(() => {
    clearLongPressTimer();
    setIsPinching(false);
    setCurrentScale(1);
    touchPoints.current.clear();
    startTouchPoint.current = null;
  }, [clearLongPressTimer]);

  // Setup event listeners
  const attachListeners = useCallback((element: HTMLElement) => {
    element.addEventListener('touchstart', handleTouchStart, { passive: false });
    element.addEventListener('touchmove', handleTouchMove, { passive: false });
    element.addEventListener('touchend', handleTouchEnd, { passive: false });
    element.addEventListener('touchcancel', handleTouchCancel, { passive: false });
    
    return () => {
      element.removeEventListener('touchstart', handleTouchStart);
      element.removeEventListener('touchmove', handleTouchMove);
      element.removeEventListener('touchend', handleTouchEnd);
      element.removeEventListener('touchcancel', handleTouchCancel);
    };
  }, [handleTouchStart, handleTouchMove, handleTouchEnd, handleTouchCancel]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      clearLongPressTimer();
    };
  }, [clearLongPressTimer]);

  // Detect if device supports touch
  const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

  return {
    // State
    isLongPressing,
    isPinching,
    currentScale,
    isTouchDevice,
    
    // Methods
    attachListeners,
    isValidTouchTarget,
    
    // Touch event handlers (for manual attachment)
    handlers: {
      onTouchStart: handleTouchStart,
      onTouchMove: handleTouchMove,
      onTouchEnd: handleTouchEnd,
      onTouchCancel: handleTouchCancel,
    },
    
    // Configuration
    config: mergedConfig,
  };
};

// ============================================================================
// Touch Target Validation Hook
// ============================================================================

/**
 * Hook to validate and enhance touch targets
 */
export const useTouchTargetValidation = (elementRef: React.RefObject<HTMLElement>) => {
  const [isValidTarget, setIsValidTarget] = useState(true);
  const [suggestions, setSuggestions] = useState<string[]>([]);

  useEffect(() => {
    if (!elementRef.current) return;

    const element = elementRef.current;
    const rect = element.getBoundingClientRect();
    const minSize = MINIMUM_TOUCH_TARGET_SIZE;
    
    const valid = rect.width >= minSize && rect.height >= minSize;
    setIsValidTarget(valid);

    if (!valid) {
      const newSuggestions: string[] = [];
      
      if (rect.width < minSize) {
        newSuggestions.push(`Increase width to at least ${minSize}px (current: ${Math.round(rect.width)}px)`);
      }
      
      if (rect.height < minSize) {
        newSuggestions.push(`Increase height to at least ${minSize}px (current: ${Math.round(rect.height)}px)`);
      }
      
      newSuggestions.push('Consider adding padding or increasing font-size');
      newSuggestions.push('Ensure adequate spacing between interactive elements');
      
      setSuggestions(newSuggestions);
    } else {
      setSuggestions([]);
    }
  }, [elementRef]);

  return {
    isValidTarget,
    suggestions,
    minSize: MINIMUM_TOUCH_TARGET_SIZE,
  };
};

export default useTouchGestures;