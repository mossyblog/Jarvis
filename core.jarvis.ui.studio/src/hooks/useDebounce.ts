/**
 * useDebounce Hook
 * 
 * A React hook that delays the execution of value updates until after
 * a specified delay period has passed without further changes.
 * 
 * This is especially useful for:
 * - Search input handling
 * - API call optimization
 * - Reducing unnecessary re-renders
 * - Improving performance with expensive computations
 * 
 * @module UseDebounce
 */

import { useState, useEffect, useRef, useCallback, useMemo } from 'react';

// ============================================================================
// Types
// ============================================================================

/** Configuration options for useDebounce hook */
export interface UseDebounceOptions<T> {
  /** Enable leading execution (execute immediately on first change) */
  leading?: boolean;
  
  /** Enable trailing execution (execute after delay) */
  trailing?: boolean;
  
  /** Maximum wait time before forced execution */
  maxWait?: number;
  
  /** Custom equality function for comparing values */
  equalityFn?: (left: T, right: T) => boolean;
  
  /** Callback fired when debounced value changes */
  onChange?: (value: T) => void;
}

/** Result object returned by useDebounce hook */
export interface UseDebounceResult<T> {
  /** The debounced value */
  debouncedValue: T;
  
  /** Whether the debounce is currently pending */
  isPending: boolean;
  
  /** Manually flush the pending debounced value immediately */
  flush: () => void;
  
  /** Cancel any pending debounced update */
  cancel: () => void;
}

// ============================================================================
// Basic useDebounce Hook
// ============================================================================

/**
 * Basic debounce hook that delays value updates
 * 
 * @param value - The value to debounce
 * @param delay - Delay in milliseconds
 * @returns The debounced value
 */
export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

// ============================================================================
// Advanced useDebounce Hook with Options
// ============================================================================

/**
 * Advanced debounce hook with comprehensive options and control
 * 
 * @param initialValue - Initial value or function that returns initial value
 * @param delay - Delay in milliseconds
 * @param options - Configuration options
 * @returns Object with debounced value and control functions
 */
export function useAdvancedDebounce<T>(
  initialValue: T | (() => T),
  delay: number,
  options: UseDebounceOptions<T> = {}
): UseDebounceResult<T> {
  const {
    leading = false,
    trailing = true,
    maxWait,
    equalityFn = (left: T, right: T) => left === right,
    onChange
  } = options;

  const unwrappedInitialValue = 
    initialValue instanceof Function ? initialValue() : initialValue;
  
  const [debouncedValue, setDebouncedValue] = useState<T>(unwrappedInitialValue);
  const [isPending, setIsPending] = useState(false);
  
  const lastValueRef = useRef<T>(unwrappedInitialValue);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const maxTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const lastCallTimeRef = useRef<number>(0);
  const hasExecutedRef = useRef(false);

  // Function to execute the debounced action
  const execute = useCallback((value: T) => {
    if (!equalityFn(debouncedValue, value)) {
      setDebouncedValue(value);
      onChange?.(value);
    }
    setIsPending(false);
    hasExecutedRef.current = true;
  }, [debouncedValue, equalityFn, onChange]);

  // Clear all timeouts
  const clearTimeouts = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
    if (maxTimeoutRef.current) {
      clearTimeout(maxTimeoutRef.current);
      maxTimeoutRef.current = null;
    }
  }, []);

  // Flush function to immediately execute pending update
  const flush = useCallback(() => {
    if (isPending) {
      clearTimeouts();
      execute(lastValueRef.current);
    }
  }, [isPending, clearTimeouts, execute]);

  // Cancel function to cancel pending update
  const cancel = useCallback(() => {
    clearTimeouts();
    setIsPending(false);
  }, [clearTimeouts]);

  // Main effect that handles the debouncing logic
  useEffect(() => {
    const currentValue = unwrappedInitialValue;
    
    // Skip if value hasn't changed
    if (equalityFn(lastValueRef.current, currentValue)) {
      return;
    }

    lastValueRef.current = currentValue;
    const now = Date.now();
    lastCallTimeRef.current = now;
    
    // Handle leading execution
    if (leading && !hasExecutedRef.current) {
      execute(currentValue);
      return;
    }

    setIsPending(true);
    clearTimeouts();

    // Set up trailing execution
    if (trailing) {
      timeoutRef.current = setTimeout(() => {
        execute(currentValue);
      }, delay);
    }

    // Set up max wait execution
    if (maxWait !== undefined) {
      maxTimeoutRef.current = setTimeout(() => {
        execute(currentValue);
      }, maxWait);
    }

    // Cleanup function
    return () => {
      clearTimeouts();
    };
  }, [unwrappedInitialValue, delay, leading, trailing, maxWait, equalityFn, execute, clearTimeouts]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      clearTimeouts();
    };
  }, [clearTimeouts]);

  return {
    debouncedValue,
    isPending,
    flush,
    cancel
  };
}

// ============================================================================
// Specialized Search Debounce Hook
// ============================================================================

/** Options for search-specific debouncing */
export interface UseSearchDebounceOptions {
  /** Delay in milliseconds (default: 300) */
  delay?: number;
  
  /** Minimum characters before triggering search (default: 1) */
  minLength?: number;
  
  /** Maximum characters to prevent overly long searches (default: 100) */
  maxLength?: number;
  
  /** Trim whitespace before processing (default: true) */
  trim?: boolean;
  
  /** Convert to lowercase before processing (default: false) */
  lowercase?: boolean;
  
  /** Callback when search term changes */
  onSearchChange?: (term: string) => void;
  
  /** Callback when search is triggered */
  onSearch?: (term: string) => void;
}

/** Result from search debounce hook */
export interface UseSearchDebounceResult {
  /** Current search term (immediate) */
  searchTerm: string;
  
  /** Debounced search term */
  debouncedSearchTerm: string;
  
  /** Whether search is pending */
  isSearchPending: boolean;
  
  /** Whether search term meets minimum requirements */
  canSearch: boolean;
  
  /** Update search term */
  setSearchTerm: (term: string) => void;
  
  /** Clear search term */
  clearSearch: () => void;
  
  /** Trigger search immediately */
  triggerSearch: () => void;
  
  /** Cancel pending search */
  cancelSearch: () => void;
}

/**
 * Specialized debounce hook for search functionality
 * 
 * @param initialTerm - Initial search term
 * @param options - Search-specific options
 * @returns Search debounce state and controls
 */
export function useSearchDebounce(
  initialTerm: string = '',
  options: UseSearchDebounceOptions = {}
): UseSearchDebounceResult {
  const {
    delay = 300,
    minLength = 1,
    maxLength = 100,
    trim = true,
    lowercase = false,
    onSearchChange,
    onSearch
  } = options;

  const [searchTerm, setSearchTermState] = useState(initialTerm);
  
  // Process search term based on options
  const processedTerm = useMemo(() => {
    let term = searchTerm;
    if (trim) term = term.trim();
    if (lowercase) term = term.toLowerCase();
    return term.slice(0, maxLength);
  }, [searchTerm, trim, lowercase, maxLength]);

  // Use advanced debounce for the processed term
  const { debouncedValue: debouncedSearchTerm, isPending, flush, cancel } = 
    useAdvancedDebounce(processedTerm, delay, {
      onChange: (term) => {
        if (term.length >= minLength) {
          onSearch?.(term);
        }
      }
    });

  // Check if search can be performed
  const canSearch = processedTerm.length >= minLength;

  // Handle search term updates
  const setSearchTerm = useCallback((term: string) => {
    setSearchTermState(term);
    onSearchChange?.(term);
  }, [onSearchChange]);

  // Clear search
  const clearSearch = useCallback(() => {
    setSearchTerm('');
    cancel();
  }, [setSearchTerm, cancel]);

  // Trigger search immediately
  const triggerSearch = useCallback(() => {
    if (canSearch) {
      flush();
    }
  }, [canSearch, flush]);

  return {
    searchTerm,
    debouncedSearchTerm,
    isSearchPending: isPending,
    canSearch,
    setSearchTerm,
    clearSearch,
    triggerSearch,
    cancelSearch: cancel
  };
}

// ============================================================================
// Debounced Callback Hook
// ============================================================================

/**
 * Hook for debouncing callback functions
 * 
 * @param callback - Function to debounce
 * @param delay - Delay in milliseconds
 * @param deps - Dependencies array (like useCallback)
 * @returns Debounced callback function
 */
export function useDebouncedCallback<T extends (...args: any[]) => any>(
  callback: T,
  delay: number,
  deps: React.DependencyList = []
): T {
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  const debouncedCallback = useCallback(
    (...args: Parameters<T>) => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      timeoutRef.current = setTimeout(() => {
        callback(...args);
      }, delay);
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [callback, delay, ...deps]
  ) as T;

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return debouncedCallback;
}

// ============================================================================
// Export Default
// ============================================================================

export default useDebounce;