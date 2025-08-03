/**
 * useViewState Hook Tests
 * 
 * Tests for the view state management hook functionality.
 */

import { renderHook, act } from '@testing-library/react';
import { vi, beforeEach, afterEach, describe, it, expect } from 'vitest';
import { useViewState } from '../useViewState';
import type { ViewMode } from '../useViewState';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value;
    },
    removeItem: (key: string) => {
      delete store[key];
    },
    clear: () => {
      store = {};
    }
  };
})();

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
});

describe('useViewState', () => {
  beforeEach(() => {
    localStorageMock.clear();
    vi.clearAllTimers();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  // ============================================================================
  // Basic Functionality
  // ============================================================================

  it('should initialize with default view mode', () => {
    const { result } = renderHook(() => useViewState());
    
    expect(result.current.viewMode).toBe('list');
    expect(result.current.isChanging).toBe(false);
  });

  it('should initialize with custom default view mode', () => {
    const { result } = renderHook(() => useViewState({ 
      defaultView: 'grid',
      storageKey: 'test-custom-default' // Use unique storage key
    }));
    
    expect(result.current.viewMode).toBe('grid');
  });

  it('should change view mode', async () => {
    const { result } = renderHook(() => useViewState());
    
    act(() => {
      result.current.setViewMode('grid');
    });
    
    expect(result.current.isChanging).toBe(true);
    
    // Fast forward the timeout
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('grid');
    expect(result.current.isChanging).toBe(false);
  });

  it('should toggle view modes in sequence', () => {
    const { result } = renderHook(() => useViewState({ defaultView: 'list' }));
    
    // list -> grid
    act(() => {
      result.current.toggleViewMode();
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('grid');
    
    // grid -> card
    act(() => {
      result.current.toggleViewMode();
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('card');
    
    // card -> list
    act(() => {
      result.current.toggleViewMode();
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('list');
  });

  it('should reset to default view mode', () => {
    const { result } = renderHook(() => useViewState({ defaultView: 'grid' }));
    
    // Change to card
    act(() => {
      result.current.setViewMode('card');
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('card');
    
    // Reset to default
    act(() => {
      result.current.resetViewMode();
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('grid');
  });

  // ============================================================================
  // Persistence
  // ============================================================================

  it('should persist view mode to localStorage', () => {
    const { result } = renderHook(() => useViewState({ storageKey: 'test-key' }));
    
    act(() => {
      result.current.setViewMode('card');
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    const stored = JSON.parse(localStorageMock.getItem('test-key') || '{}');
    expect(stored.global).toBe('card');
  });

  it('should restore view mode from localStorage', () => {
    // Pre-populate localStorage
    localStorageMock.setItem('test-restore', JSON.stringify({
      global: 'grid',
      pages: {},
      lastUpdated: new Date().toISOString()
    }));
    
    const { result } = renderHook(() => useViewState({ storageKey: 'test-restore' }));
    
    expect(result.current.viewMode).toBe('grid');
  });

  it('should handle page-specific view modes', () => {
    const { result } = renderHook(() => useViewState({ 
      pageId: 'dashboard',
      storageKey: 'test-pages'
    }));
    
    act(() => {
      result.current.setViewMode('card');
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    const stored = JSON.parse(localStorageMock.getItem('test-pages') || '{}');
    expect(stored.pages.dashboard).toBe('card');
  });

  it('should work without persistence', () => {
    const { result } = renderHook(() => useViewState({ persist: false }));
    
    act(() => {
      result.current.setViewMode('grid');
    });
    
    act(() => {
      vi.advanceTimersByTime(100);
    });
    
    expect(result.current.viewMode).toBe('grid');
    // Should not have stored anything
    expect(localStorageMock.getItem('jarvis-ui-studio-view-state')).toBeNull();
  });

  // ============================================================================
  // Cross-Page Functionality
  // ============================================================================

  it('should get view mode for specific page', () => {
    // Pre-populate with page data
    localStorageMock.setItem('cross-page-test', JSON.stringify({
      global: 'list',
      pages: {
        'page1': 'grid',
        'page2': 'card'
      },
      lastUpdated: new Date().toISOString()
    }));
    
    const { result } = renderHook(() => useViewState({ storageKey: 'cross-page-test' }));
    
    expect(result.current.getViewModeForPage('page1')).toBe('grid');
    expect(result.current.getViewModeForPage('page2')).toBe('card');
    expect(result.current.getViewModeForPage('page3')).toBe('list'); // Falls back to global
  });

  it('should set view mode for specific page', () => {
    const { result } = renderHook(() => useViewState({ storageKey: 'set-page-test' }));
    
    act(() => {
      result.current.setViewModeForPage('settings', 'card');
    });
    
    const stored = JSON.parse(localStorageMock.getItem('set-page-test') || '{}');
    expect(stored.pages.settings).toBe('card');
  });

  // ============================================================================
  // Error Handling
  // ============================================================================

  it('should handle corrupted localStorage data gracefully', () => {
    localStorageMock.setItem('corrupted-test', 'invalid-json');
    
    const { result } = renderHook(() => useViewState({ storageKey: 'corrupted-test' }));
    
    // Should fall back to default
    expect(result.current.viewMode).toBe('list');
  });

  it('should handle localStorage errors gracefully', () => {
    // Mock localStorage to throw errors
    const originalSetItem = localStorageMock.setItem;
    localStorageMock.setItem = vi.fn(() => {
      throw new Error('Storage quota exceeded');
    });
    
    const { result } = renderHook(() => useViewState());
    
    // Should not throw when trying to persist
    expect(() => {
      act(() => {
        result.current.setViewMode('grid');
      });
    }).not.toThrow();
    
    // Restore original method
    localStorageMock.setItem = originalSetItem;
  });

  // ============================================================================
  // View Mode Types
  // ============================================================================

  it('should accept all valid view modes', () => {
    const viewModes: ViewMode[] = ['list', 'grid', 'card'];
    
    viewModes.forEach((mode, index) => {
      const { result } = renderHook(() => useViewState({ 
        defaultView: mode,
        storageKey: `test-valid-modes-${index}` // Use unique storage key for each
      }));
      expect(result.current.viewMode).toBe(mode);
    });
  });
});