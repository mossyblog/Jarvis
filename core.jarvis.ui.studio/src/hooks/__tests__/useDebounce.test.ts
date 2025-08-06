/**
 * Tests for useDebounce hook
 * 
 * @module UseDebounceTest
 */

import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useDebounce, useSearchDebounce, useDebouncedCallback } from '../useDebounce';

// Mock timers
vi.useFakeTimers();

describe('useDebounce', () => {
  beforeEach(() => {
    vi.clearAllTimers();
  });

  it('should return initial value immediately', () => {
    const { result } = renderHook(() => useDebounce('initial', 300));
    
    expect(result.current).toBe('initial');
  });

  it('should debounce value changes', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    );
    
    // Change value
    rerender({ value: 'updated' });
    
    // Should still be initial value
    expect(result.current).toBe('initial');
    
    // Fast forward time
    act(() => {
      vi.advanceTimersByTime(300);
    });
    
    // Should now be updated value
    expect(result.current).toBe('updated');
  });

  it('should reset timer on subsequent changes', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    );
    
    // Change value
    rerender({ value: 'updated1' });
    
    // Advance time partially
    act(() => {
      vi.advanceTimersByTime(150);
    });
    
    // Change value again
    rerender({ value: 'updated2' });
    
    // Advance time partially again
    act(() => {
      vi.advanceTimersByTime(150);
    });
    
    // Should still be initial (timer was reset)
    expect(result.current).toBe('initial');
    
    // Advance full time
    act(() => {
      vi.advanceTimersByTime(300);
    });
    
    // Should now be final value
    expect(result.current).toBe('updated2');
  });
});

describe('useSearchDebounce', () => {
  beforeEach(() => {
    vi.clearAllTimers();
  });

  it('should initialize with proper default values', () => {
    const { result } = renderHook(() => useSearchDebounce());
    
    expect(result.current.searchTerm).toBe('');
    expect(result.current.debouncedSearchTerm).toBe('');
    expect(result.current.canSearch).toBe(false);
    expect(result.current.isSearchPending).toBe(false);
  });

  it('should handle search term changes', () => {
    const onSearch = vi.fn();
    const { result } = renderHook(() => 
      useSearchDebounce('', { onSearch, delay: 300 })
    );
    
    act(() => {
      result.current.setSearchTerm('test');
    });
    
    expect(result.current.searchTerm).toBe('test');
    expect(result.current.canSearch).toBe(true);
    expect(result.current.isSearchPending).toBe(true);
    
    // Advance timer
    act(() => {
      vi.advanceTimersByTime(300);
    });
    
    expect(result.current.debouncedSearchTerm).toBe('test');
    expect(result.current.isSearchPending).toBe(false);
    expect(onSearch).toHaveBeenCalledWith('test');
  });

  it('should respect minimum length requirement', () => {
    const onSearch = vi.fn();
    const { result } = renderHook(() => 
      useSearchDebounce('', { onSearch, minLength: 3 })
    );
    
    act(() => {
      result.current.setSearchTerm('ab'); // Too short
    });
    
    expect(result.current.canSearch).toBe(false);
    
    act(() => {
      result.current.setSearchTerm('abc'); // Meets minimum
    });
    
    expect(result.current.canSearch).toBe(true);
  });

  it('should trim whitespace when enabled', () => {
    const { result } = renderHook(() => 
      useSearchDebounce('', { trim: true })
    );
    
    act(() => {
      result.current.setSearchTerm('  test  ');
    });
    
    act(() => {
      vi.advanceTimersByTime(300);
    });
    
    expect(result.current.debouncedSearchTerm).toBe('test');
  });

  it('should clear search', () => {
    const { result } = renderHook(() => useSearchDebounce('initial'));
    
    act(() => {
      result.current.clearSearch();
    });
    
    expect(result.current.searchTerm).toBe('');
    expect(result.current.canSearch).toBe(false);
  });

  it('should trigger search immediately', () => {
    const onSearch = vi.fn();
    const { result } = renderHook(() => 
      useSearchDebounce('', { onSearch })
    );
    
    act(() => {
      result.current.setSearchTerm('test');
    });
    
    expect(onSearch).not.toHaveBeenCalled();
    
    act(() => {
      result.current.triggerSearch();
    });
    
    expect(onSearch).toHaveBeenCalledWith('test');
  });
});

describe('useDebouncedCallback', () => {
  beforeEach(() => {
    vi.clearAllTimers();
  });

  it('should debounce callback execution', () => {
    const callback = vi.fn();
    const { result } = renderHook(() => 
      useDebouncedCallback(callback, 300)
    );
    
    // Call multiple times
    act(() => {
      result.current('arg1');
      result.current('arg2');
      result.current('arg3');
    });
    
    // Should not have been called yet
    expect(callback).not.toHaveBeenCalled();
    
    // Advance timer
    act(() => {
      vi.advanceTimersByTime(300);
    });
    
    // Should be called once with last arguments
    expect(callback).toHaveBeenCalledTimes(1);
    expect(callback).toHaveBeenCalledWith('arg3');
  });

  it('should cancel previous calls', () => {
    const callback = vi.fn();
    const { result } = renderHook(() => 
      useDebouncedCallback(callback, 300)
    );
    
    act(() => {
      result.current('first');
    });
    
    // Advance time partially
    act(() => {
      vi.advanceTimersByTime(150);
    });
    
    act(() => {
      result.current('second');
    });
    
    // Advance full time
    act(() => {
      vi.advanceTimersByTime(300);
    });
    
    // Should only be called once with the last call
    expect(callback).toHaveBeenCalledTimes(1);
    expect(callback).toHaveBeenCalledWith('second');
  });
});