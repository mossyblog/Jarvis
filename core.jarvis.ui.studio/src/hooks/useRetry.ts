/**
 * Retry Hooks
 * 
 * Custom hooks for implementing retry functionality with exponential backoff,
 * retry counts, and intelligent error handling.
 * 
 * @module UseRetry
 */

import React, { useState, useCallback, useRef } from 'react';
import { 
  UIStudioError, 
  isRetryableError, 
  calculateRetryDelay, 
  sleep,
  DEFAULT_RETRY_CONFIG,
  type RetryConfig 
} from '../utils/uistudioErrors';

// ============================================================================
// Basic Retry Hook
// ============================================================================

interface UseRetryState {
  isRetrying: boolean;
  retryCount: number;
  lastError: Error | null;
  canRetry: boolean;
}

interface UseRetryOptions {
  maxRetries?: number;
  baseDelay?: number;
  maxDelay?: number;
  exponentialBackoff?: boolean;
  retryableErrors?: string[];
  onRetry?: (attempt: number, error: Error) => void;
  onSuccess?: () => void;
  onMaxRetriesReached?: (error: Error) => void;
}

export function useRetry<T extends (...args: unknown[]) => Promise<unknown>>(
  asyncFunction: T,
  options: UseRetryOptions = {}
): {
  execute: (...args: Parameters<T>) => Promise<ReturnType<T>>;
  retry: () => Promise<void>;
  reset: () => void;
  state: UseRetryState;
} {
  const config = React.useMemo<RetryConfig>(() => ({
    maxAttempts: options.maxRetries || DEFAULT_RETRY_CONFIG.maxAttempts,
    baseDelay: options.baseDelay || DEFAULT_RETRY_CONFIG.baseDelay,
    maxDelay: options.maxDelay || DEFAULT_RETRY_CONFIG.maxDelay,
    exponentialBackoff: options.exponentialBackoff ?? DEFAULT_RETRY_CONFIG.exponentialBackoff,
    retryableErrors: options.retryableErrors || DEFAULT_RETRY_CONFIG.retryableErrors
  }), [options.maxRetries, options.baseDelay, options.maxDelay, options.exponentialBackoff, options.retryableErrors]);

  const [state, setState] = useState<UseRetryState>({
    isRetrying: false,
    retryCount: 0,
    lastError: null,
    canRetry: false
  });

  const lastArgsRef = useRef<Parameters<T> | undefined>(undefined);
  const abortControllerRef = useRef<AbortController | null>(null);

  const reset = useCallback(() => {
    setState({
      isRetrying: false,
      retryCount: 0,
      lastError: null,
      canRetry: false
    });
    lastArgsRef.current = undefined as any;
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
  }, []);

  const executeWithRetry = useCallback(async (...args: Parameters<T>): Promise<ReturnType<T>> => {
    lastArgsRef.current = args;
    abortControllerRef.current?.abort();
    abortControllerRef.current = new AbortController();

    let attempt = 0;
    let lastError: Error;

    while (attempt < config.maxAttempts) {
      attempt++;
      
      setState(prev => ({
        ...prev,
        isRetrying: attempt > 1,
        retryCount: attempt - 1
      }));

      try {
        if (abortControllerRef.current?.signal.aborted) {
          throw new Error('Operation was cancelled');
        }

        const result = await asyncFunction(...args);
        
        // Success
        setState({
          isRetrying: false,
          retryCount: attempt - 1,
          lastError: null,
          canRetry: false
        });
        
        options.onSuccess?.();
        return result as ReturnType<T>;

      } catch (error) {
        lastError = error as Error;
        
        setState(prev => ({
          ...prev,
          isRetrying: false,
          lastError
        }));

        // Check if we should retry
        const isUIStudioError = error instanceof UIStudioError;
        const shouldRetry = isUIStudioError 
          ? isRetryableError(error, config)
          : true; // Retry non-UIStudio errors by default

        const hasRetriesLeft = attempt < config.maxAttempts;
        const canRetry = shouldRetry && hasRetriesLeft;

        setState(prev => ({
          ...prev,
          canRetry
        }));

        if (!canRetry) {
          if (!hasRetriesLeft) {
            options.onMaxRetriesReached?.(lastError);
          }
          throw lastError;
        }

        // Wait before retrying
        const delay = calculateRetryDelay(attempt, config);
        options.onRetry?.(attempt, lastError);
        
        await sleep(delay);
      }
    }

    throw lastError!;
  }, [asyncFunction, config, options]);

  const retry = useCallback(async () => {
    if (!lastArgsRef.current || !state.canRetry) {
      return;
    }

    await executeWithRetry(...lastArgsRef.current);
  }, [executeWithRetry, state.canRetry]);

  return {
    execute: executeWithRetry,
    retry,
    reset,
    state
  };
}

// ============================================================================
// Simple Retry Hook
// ============================================================================

interface UseSimpleRetryOptions {
  maxRetries?: number;
  delay?: number;
}

export function useSimpleRetry<T extends (...args: unknown[]) => Promise<unknown>>(
  asyncFunction: T,
  options: UseSimpleRetryOptions = {}
) {
  const [isRetrying, setIsRetrying] = useState(false);
  const [retryCount, setRetryCount] = useState(0);
  const [error, setError] = useState<Error | null>(null);

  const maxRetries = options.maxRetries || 3;
  const delay = options.delay || 1000;

  const executeWithRetry = useCallback(async (...args: Parameters<T>): Promise<ReturnType<T>> => {
    let attempt = 0;
    
    while (attempt < maxRetries) {
      try {
        setIsRetrying(attempt > 0);
        setRetryCount(attempt);
        setError(null);
        
        const result = await asyncFunction(...args);
        
        setIsRetrying(false);
        return result as ReturnType<T>;
        
      } catch (err) {
        attempt++;
        setError(err as Error);
        
        if (attempt >= maxRetries) {
          setIsRetrying(false);
          throw err;
        }
        
        if (attempt < maxRetries) {
          await sleep(delay * attempt); // Simple linear backoff
        }
      }
    }
    
    throw error;
  }, [asyncFunction, maxRetries, delay, error]);

  const reset = useCallback(() => {
    setIsRetrying(false);
    setRetryCount(0);
    setError(null);
  }, []);

  return {
    execute: executeWithRetry,
    reset,
    isRetrying,
    retryCount,
    error,
    canRetry: !!error && retryCount < maxRetries
  };
}

// ============================================================================
// Manual Retry Hook
// ============================================================================

interface UseManualRetryState {
  isRetrying: boolean;
  retryCount: number;
  error: Error | null;
  canRetry: boolean;
}

export function useManualRetry<T extends (...args: unknown[]) => Promise<unknown>>(
  asyncFunction: T,
  options: UseRetryOptions = {}
) {
  const [state, setState] = useState<UseManualRetryState>({
    isRetrying: false,
    retryCount: 0,
    error: null,
    canRetry: false
  });

  const lastArgsRef = useRef<Parameters<T> | undefined>(undefined);
  const maxRetries = options.maxRetries || 3;

  const execute = useCallback(async (...args: Parameters<T>): Promise<ReturnType<T>> => {
    lastArgsRef.current = args;
    
    setState(prev => ({
      ...prev,
      isRetrying: true,
      error: null
    }));

    try {
      const result = await asyncFunction(...args);
      
      setState({
        isRetrying: false,
        retryCount: 0,
        error: null,
        canRetry: false
      });
      
      return result as ReturnType<T>;
      
    } catch (error) {
      const err = error as Error;
      const canRetry = state.retryCount < maxRetries;
      
      setState({
        isRetrying: false,
        retryCount: state.retryCount,
        error: err,
        canRetry
      });
      
      throw err;
    }
  }, [asyncFunction, maxRetries, state.retryCount]);

  const retry = useCallback(async () => {
    if (!lastArgsRef.current || !state.canRetry) {
      return;
    }

    setState(prev => ({
      ...prev,
      isRetrying: true,
      retryCount: prev.retryCount + 1,
      error: null
    }));

    try {
      const result = await asyncFunction(...lastArgsRef.current!);
      
      setState({
        isRetrying: false,
        retryCount: 0,
        error: null,
        canRetry: false
      });
      
      return result;
      
    } catch (error) {
      const err = error as Error;
      const newRetryCount = state.retryCount + 1;
      const canRetry = newRetryCount < maxRetries;
      
      setState({
        isRetrying: false,
        retryCount: newRetryCount,
        error: err,
        canRetry
      });
      
      throw err;
    }
  }, [asyncFunction, state.canRetry, state.retryCount, maxRetries]);

  const reset = useCallback(() => {
    setState({
      isRetrying: false,
      retryCount: 0,
      error: null,
      canRetry: false
    });
    lastArgsRef.current = undefined as any;
  }, []);

  return {
    execute,
    retry,
    reset,
    ...state
  };
}

// ============================================================================
// Async Operation Hook with Built-in Retry
// ============================================================================

interface UseAsyncOperationOptions<T> extends UseRetryOptions {
  initialData?: T;
  immediate?: boolean;
}

interface UseAsyncOperationState<T> {
  data: T | null;
  isLoading: boolean;
  isRetrying: boolean;
  error: Error | null;
  retryCount: number;
  canRetry: boolean;
}

export function useAsyncOperation<T, Args extends unknown[] = []>(
  asyncFunction: (...args: Args) => Promise<T>,
  options: UseAsyncOperationOptions<T> = {}
) {
  const [state, setState] = useState<UseAsyncOperationState<T>>({
    data: options.initialData || null,
    isLoading: false,
    isRetrying: false,
    error: null,
    retryCount: 0,
    canRetry: false
  });

  const retryHook = useRetry(asyncFunction as any, {
    ...options,
    onRetry: (attempt, error) => {
      setState(prev => ({
        ...prev,
        isRetrying: true,
        retryCount: attempt,
        error
      }));
      options.onRetry?.(attempt, error);
    },
    onSuccess: () => {
      setState(prev => ({
        ...prev,
        isRetrying: false,
        error: null,
        retryCount: 0,
        canRetry: false
      }));
      options.onSuccess?.();
    }
  });

  const execute = useCallback(async (...args: Args) => {
    setState(prev => ({
      ...prev,
      isLoading: true,
      error: null
    }));

    try {
      const result = await retryHook.execute(...args);
      
      setState(prev => ({
        ...prev,
        data: result as T,
        isLoading: false,
        isRetrying: false,
        error: null
      }));
      
      return result;
      
    } catch (err) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        isRetrying: false,
        error: err as Error,
        canRetry: retryHook.state.canRetry
      }));
      
      throw err;
    }
  }, [retryHook]);

  const retry = useCallback(async () => {
    setState(prev => ({
      ...prev,
      isRetrying: true
    }));

    try {
      await retryHook.retry();
    } catch {
      // Error handling is done in the execute callback
    }
  }, [retryHook]);

  const reset = useCallback(() => {
    setState({
      data: options.initialData || null,
      isLoading: false,
      isRetrying: false,
      error: null,
      retryCount: 0,
      canRetry: false
    });
    retryHook.reset();
  }, [retryHook, options.initialData]);

  return {
    execute,
    retry,
    reset,
    ...state
  };
}

export type { UseRetryOptions, UseRetryState, UseSimpleRetryOptions };