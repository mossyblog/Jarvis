/**
 * useViewState Hook - Manages view preferences (list, grid, card) with persistence
 * 
 * Provides state management for different view modes with localStorage persistence.
 * Supports per-page view preferences and responsive behavior.
 * 
 * @module useViewState
 */

import { useState, useEffect, useCallback } from 'react';

// ============================================================================
// Types
// ============================================================================

export type ViewMode = 'list' | 'grid' | 'card';

export interface ViewStateOptions {
  /** Storage key for localStorage persistence. Defaults to 'view-state' */
  storageKey?: string;
  /** Page identifier for per-page view preferences. Optional. */
  pageId?: string;
  /** Default view mode if none is stored. Defaults to 'list' */
  defaultView?: ViewMode;
  /** Whether to persist view state to localStorage. Defaults to true */
  persist?: boolean;
}

export interface ViewState {
  /** Current view mode */
  viewMode: ViewMode;
  /** Whether view mode is being changed (for loading states) */
  isChanging: boolean;
}

export interface ViewStateActions {
  /** Set the view mode */
  setViewMode: (mode: ViewMode) => void;
  /** Toggle between view modes in sequence: list -> grid -> card -> list */
  toggleViewMode: () => void;
  /** Reset to default view mode */
  resetViewMode: () => void;
  /** Get view mode for specific page (if pageId not set in options) */
  getViewModeForPage: (pageId: string) => ViewMode;
  /** Set view mode for specific page (if pageId not set in options) */
  setViewModeForPage: (pageId: string, mode: ViewMode) => void;
}

export interface UseViewStateReturn extends ViewState, ViewStateActions {}

// ============================================================================
// Storage Utilities
// ============================================================================

const DEFAULT_STORAGE_KEY = 'jarvis-ui-studio-view-state';

interface StoredViewState {
  global: ViewMode;
  pages: Record<string, ViewMode>;
  lastUpdated: string;
}

const getStoredViewState = (storageKey: string): StoredViewState & { hasData: boolean } => {
  try {
    const stored = localStorage.getItem(storageKey);
    if (stored) {
      const parsed = JSON.parse(stored);
      return {
        global: parsed.global || 'list',
        pages: parsed.pages || {},
        lastUpdated: parsed.lastUpdated || new Date().toISOString(),
        hasData: true
      };
    }
  } catch (error) {
    console.warn('Failed to parse stored view state:', error);
  }

  return {
    global: 'list',
    pages: {},
    lastUpdated: new Date().toISOString(),
    hasData: false
  };
};

const setStoredViewState = (storageKey: string, state: StoredViewState): void => {
  try {
    localStorage.setItem(storageKey, JSON.stringify({
      ...state,
      lastUpdated: new Date().toISOString()
    }));
  } catch (error) {
    console.warn('Failed to store view state:', error);
  }
};

// ============================================================================
// Hook Implementation
// ============================================================================

/**
 * Hook for managing view state with localStorage persistence
 * 
 * @example
 * ```tsx
 * // Basic usage
 * const { viewMode, setViewMode, toggleViewMode } = useViewState();
 * 
 * // Per-page view state
 * const { viewMode, setViewMode } = useViewState({ 
 *   pageId: 'dashboard',
 *   defaultView: 'grid' 
 * });
 * 
 * // No persistence
 * const { viewMode, setViewMode } = useViewState({ 
 *   persist: false,
 *   defaultView: 'card' 
 * });
 * ```
 */
export function useViewState(options: ViewStateOptions = {}): UseViewStateReturn {
  const {
    storageKey = DEFAULT_STORAGE_KEY,
    pageId,
    defaultView = 'list',
    persist = true
  } = options;

  // ============================================================================
  // State
  // ============================================================================

  const [state, setState] = useState<ViewState>(() => {
    if (!persist) {
      return { viewMode: defaultView, isChanging: false };
    }

    const stored = getStoredViewState(storageKey);
    const viewMode = pageId 
      ? (stored.pages[pageId] || defaultView) 
      : (stored.hasData ? stored.global : defaultView);
    
    return { viewMode, isChanging: false };
  });

  // ============================================================================
  // Actions
  // ============================================================================

  const setViewMode = useCallback((mode: ViewMode) => {
    setState(prev => ({ ...prev, isChanging: true }));

    // Use setTimeout to show loading state briefly for smoother UX
    setTimeout(() => {
      setState(prev => ({ ...prev, viewMode: mode, isChanging: false }));

      if (persist) {
        const stored = getStoredViewState(storageKey);
        
        if (pageId) {
          // Update page-specific view mode
          stored.pages[pageId] = mode;
        } else {
          // Update global view mode
          stored.global = mode;
        }
        
        // Remove hasData before saving
        const { hasData, ...stateToSave } = stored;
        setStoredViewState(storageKey, stateToSave);
      }
    }, 50); // Brief delay for smooth UX
  }, [pageId, persist, storageKey]);

  const toggleViewMode = useCallback(() => {
    const modes: ViewMode[] = ['list', 'grid', 'card'];
    const currentIndex = modes.indexOf(state.viewMode);
    const nextIndex = (currentIndex + 1) % modes.length;
    setViewMode(modes[nextIndex]);
  }, [state.viewMode, setViewMode]);

  const resetViewMode = useCallback(() => {
    setViewMode(defaultView);
  }, [defaultView, setViewMode]);

  const getViewModeForPage = useCallback((targetPageId: string): ViewMode => {
    if (!persist) return defaultView;
    
    const stored = getStoredViewState(storageKey);
    return stored.pages[targetPageId] || (stored.hasData ? stored.global : defaultView);
  }, [defaultView, persist, storageKey]);

  const setViewModeForPage = useCallback((targetPageId: string, mode: ViewMode) => {
    if (!persist) return;
    
    const stored = getStoredViewState(storageKey);
    stored.pages[targetPageId] = mode;
    const { hasData, ...stateToSave } = stored;
    setStoredViewState(storageKey, stateToSave);
  }, [persist, storageKey]);

  // ============================================================================
  // Effects
  // ============================================================================

  // Listen for storage changes from other tabs/windows
  useEffect(() => {
    if (!persist) return;

    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === storageKey && e.newValue) {
        try {
          const parsed = JSON.parse(e.newValue);
          const stored = {
            global: parsed.global || 'list',
            pages: parsed.pages || {},
            lastUpdated: parsed.lastUpdated || new Date().toISOString(),
            hasData: true
          };
          const viewMode = pageId 
            ? (stored.pages[pageId] || defaultView) 
            : (stored.hasData ? stored.global : defaultView);
          
          setState(prev => ({ ...prev, viewMode }));
        } catch (error) {
          console.warn('Failed to sync view state from storage:', error);
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, [storageKey, pageId, defaultView, persist]);

  // ============================================================================
  // Return
  // ============================================================================

  return {
    viewMode: state.viewMode,
    isChanging: state.isChanging,
    setViewMode,
    toggleViewMode,
    resetViewMode,
    getViewModeForPage,
    setViewModeForPage
  };
}

export default useViewState;