/**
 * ViewStateContext - Global view state management with responsive behavior
 * 
 * Provides global view state management that can adapt to device size changes
 * and coordinate view preferences across the application.
 */

import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { useViewState } from '@/hooks/useViewState';
import type { ViewMode } from '@/hooks/useViewState';

// ============================================================================
// Types
// ============================================================================

export type DeviceSize = 'mobile' | 'tablet' | 'desktop';

export interface ViewStateContextValue {
  /** Current view mode */
  viewMode: ViewMode;
  /** Set view mode */
  setViewMode: (mode: ViewMode) => void;
  /** Toggle view mode */
  toggleViewMode: () => void;
  /** Current device size */
  deviceSize: DeviceSize;
  /** Whether view is changing */
  isChanging: boolean;
  /** Get optimal view mode for current device */
  getOptimalViewMode: () => ViewMode;
  /** Get view mode for specific page */
  getViewModeForPage: (pageId: string) => ViewMode;
  /** Set view mode for specific page */
  setViewModeForPage: (pageId: string, mode: ViewMode) => void;
}

// ============================================================================
// Context
// ============================================================================

const ViewStateContext = createContext<ViewStateContextValue | null>(null);

// ============================================================================
// Responsive Configuration
// ============================================================================

const DEVICE_BREAKPOINTS = {
  mobile: 768,   // < 768px
  tablet: 1024,  // 768px - 1024px
  desktop: 1024  // >= 1024px
} as const;

const OPTIMAL_VIEW_MODES: Record<DeviceSize, ViewMode> = {
  mobile: 'list',    // List view works best on mobile
  tablet: 'grid',    // Grid view for tablets
  desktop: 'grid'    // Grid view for desktop (user can override)
} as const;

// ============================================================================
// Utilities
// ============================================================================

// Note: Currently unused but available for future features
// const getDeviceSize = (width: number): DeviceSize => {
//   if (width < DEVICE_BREAKPOINTS.mobile) return 'mobile';
//   if (width < DEVICE_BREAKPOINTS.desktop) return 'tablet';
//   return 'desktop';
// };

const useMediaQuery = (query: string): boolean => {
  const [matches, setMatches] = useState(false);

  useEffect(() => {
    const media = window.matchMedia(query);
    if (media.matches !== matches) {
      setMatches(media.matches);
    }
    
    const listener = () => setMatches(media.matches);
    media.addEventListener('change', listener);
    
    return () => media.removeEventListener('change', listener);
  }, [matches, query]);

  return matches;
};

// ============================================================================
// Provider Component
// ============================================================================

interface ViewStateProviderProps {
  children: React.ReactNode;
  /** Optional page ID for page-specific view state */
  pageId?: string;
  /** Default view mode */
  defaultView?: ViewMode;
  /** Whether to auto-adapt to device size */
  adaptToDevice?: boolean;
}

export const ViewStateProvider: React.FC<ViewStateProviderProps> = ({
  children,
  pageId,
  defaultView = 'grid',
  adaptToDevice = true
}) => {
  // ============================================================================
  // Device Detection
  // ============================================================================

  const isMobile = useMediaQuery(`(max-width: ${DEVICE_BREAKPOINTS.mobile - 1}px)`);
  const isTablet = useMediaQuery(
    `(min-width: ${DEVICE_BREAKPOINTS.mobile}px) and (max-width: ${DEVICE_BREAKPOINTS.desktop - 1}px)`
  );
  
  const deviceSize: DeviceSize = isMobile ? 'mobile' : isTablet ? 'tablet' : 'desktop';

  // ============================================================================
  // View State Management
  // ============================================================================

  const {
    viewMode,
    setViewMode: setViewModeHook,
    toggleViewMode,
    isChanging,
    getViewModeForPage,
    setViewModeForPage
  } = useViewState({
    pageId,
    defaultView,
    persist: true
  });

  // ============================================================================
  // Responsive Adaptation
  // ============================================================================

  const [hasUserOverride, setHasUserOverride] = useState(false);

  // Get optimal view mode for current device
  const getOptimalViewMode = useCallback((): ViewMode => {
    return OPTIMAL_VIEW_MODES[deviceSize];
  }, [deviceSize]);

  // Auto-adapt to device size on first load or device change
  useEffect(() => {
    if (!adaptToDevice || hasUserOverride) return;

    const optimalMode = getOptimalViewMode();
    if (viewMode !== optimalMode) {
      setViewModeHook(optimalMode);
    }
  }, [deviceSize, adaptToDevice, hasUserOverride, viewMode, setViewModeHook, getOptimalViewMode]);

  // Enhanced setViewMode that tracks user overrides
  const setViewMode = (mode: ViewMode) => {
    setHasUserOverride(true);
    setViewModeHook(mode);
    
    // Reset user override after some time to allow auto-adaptation
    setTimeout(() => {
      setHasUserOverride(false);
    }, 5 * 60 * 1000); // 5 minutes
  };

  // ============================================================================
  // Context Value
  // ============================================================================

  const contextValue: ViewStateContextValue = {
    viewMode,
    setViewMode,
    toggleViewMode,
    deviceSize,
    isChanging,
    getOptimalViewMode,
    getViewModeForPage,
    setViewModeForPage
  };

  return (
    <ViewStateContext.Provider value={contextValue}>
      {children}
    </ViewStateContext.Provider>
  );
};

// ============================================================================
// Hook
// ============================================================================

export const useViewStateContext = (): ViewStateContextValue => {
  const context = useContext(ViewStateContext);
  if (!context) {
    throw new Error('useViewStateContext must be used within a ViewStateProvider');
  }
  return context;
};

// ============================================================================
// Convenience Hook for Page-Specific View State
// ============================================================================

export const usePageViewState = (pageId: string, defaultView: ViewMode = 'grid') => {
  const context = useContext(ViewStateContext);
  
  // Always call hooks at the top level
  const {
    viewMode: fallbackViewMode,
    setViewMode: fallbackSetViewMode,
    isChanging: fallbackIsChanging
  } = useViewState({
    pageId,
    defaultView,
    persist: true
  });

  const isMobile = useMediaQuery(`(max-width: ${DEVICE_BREAKPOINTS.mobile - 1}px)`);
  const isTablet = useMediaQuery(
    `(min-width: ${DEVICE_BREAKPOINTS.mobile}px) and (max-width: ${DEVICE_BREAKPOINTS.desktop - 1}px)`
  );
  
  const deviceSize: DeviceSize = isMobile ? 'mobile' : isTablet ? 'tablet' : 'desktop';
  
  if (context) {
    // If we're within a ViewStateProvider, use it
    return {
      viewMode: context.getViewModeForPage(pageId),
      setViewMode: (mode: ViewMode) => context.setViewModeForPage(pageId, mode),
      deviceSize: context.deviceSize,
      isChanging: context.isChanging,
      getOptimalViewMode: context.getOptimalViewMode
    };
  } else {
    // Fallback to direct hook usage
    return {
      viewMode: fallbackViewMode,
      setViewMode: fallbackSetViewMode,
      deviceSize,
      isChanging: fallbackIsChanging,
      getOptimalViewMode: () => OPTIMAL_VIEW_MODES[deviceSize]
    };
  }
};

export default ViewStateContext;