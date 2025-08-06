/**
 * Cache Strategy Hook Tests
 * 
 * Tests for the cache strategy hooks and utilities
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { 
  useCacheStrategy, 
  useRealtimeCacheStrategy,
  useBackgroundCacheStrategy,
  useStaticCacheStrategy
} from '../useCacheStrategy';

// Mock console methods
const mockConsole = {
  group: vi.fn(),
  groupEnd: vi.fn(),
  log: vi.fn(),
  warn: vi.fn(),
  error: vi.fn(),
};

beforeEach(() => {
  vi.stubGlobal('console', mockConsole);
});

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );

  return { Wrapper, queryClient };
};

describe('useCacheStrategy', () => {
  let wrapper: any;
  let queryClient: QueryClient;

  beforeEach(() => {
    const setup = createWrapper();
    wrapper = setup.Wrapper;
    queryClient = setup.queryClient;
  });

  describe('getStrategyConfig', () => {
    it('should return correct config for realtime strategy', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const config = result.current.getStrategyConfig('realtime');
      
      expect(config.staleTime).toBe(30 * 1000); // 30 seconds
      expect(config.refetchInterval).toBe(60 * 1000); // 1 minute
      expect(config.refetchIntervalInBackground).toBe(true);
    });

    it('should return correct config for background strategy', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const config = result.current.getStrategyConfig('background');
      
      expect(config.staleTime).toBe(5 * 60 * 1000); // 5 minutes
      expect(config.refetchInterval).toBe(10 * 60 * 1000); // 10 minutes
      expect(config.refetchOnWindowFocus).toBe(false);
    });

    it('should return correct config for lazy strategy', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const config = result.current.getStrategyConfig('lazy');
      
      expect(config.staleTime).toBe(30 * 60 * 1000); // 30 minutes
      expect(config.refetchInterval).toBe(false);
      expect(config.refetchOnWindowFocus).toBe(false);
      expect(config.refetchOnMount).toBe(false);
    });

    it('should return correct config for aggressive strategy', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const config = result.current.getStrategyConfig('aggressive');
      
      expect(config.staleTime).toBe(0); // Always stale
      expect(config.refetchInterval).toBe(30 * 1000); // 30 seconds
      expect(config.refetchOnMount).toBe('always');
    });

    it('should return correct config for static strategy', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const config = result.current.getStrategyConfig('static');
      
      expect(config.staleTime).toBe(60 * 60 * 1000); // 1 hour
      expect(config.refetchInterval).toBe(false);
      expect(config.refetchOnWindowFocus).toBe(false);
      expect(config.refetchOnReconnect).toBe(false);
    });

    it('should return correct config for user-specific strategy', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const config = result.current.getStrategyConfig('user-specific');
      
      expect(config.staleTime).toBe(2 * 60 * 1000); // 2 minutes
      expect(config.refetchInterval).toBe(5 * 60 * 1000); // 5 minutes
      expect(config.refetchIntervalInBackground).toBe(true);
    });
  });

  describe('strategies object', () => {
    it('should provide strategy configs through strategies object', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      expect(result.current.strategies.realtime()).toEqual(
        result.current.getStrategyConfig('realtime')
      );
      expect(result.current.strategies.background()).toEqual(
        result.current.getStrategyConfig('background')
      );
      expect(result.current.strategies.lazy()).toEqual(
        result.current.getStrategyConfig('lazy')
      );
    });

    it('should allow overriding strategy options', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const customConfig = result.current.strategies.realtime({
        staleTime: 60000,
        refetchInterval: 120000,
      });
      
      expect(customConfig.staleTime).toBe(60000);
      expect(customConfig.refetchInterval).toBe(120000);
      expect(customConfig.refetchIntervalInBackground).toBe(true); // Original value
    });
  });

  describe('performance monitoring', () => {
    it('should calculate performance metrics', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const metrics = result.current.getPerformanceMetrics();
      
      expect(metrics).toHaveProperty('hitRate');
      expect(metrics).toHaveProperty('missRate');
      expect(metrics).toHaveProperty('staleRate');
      expect(metrics).toHaveProperty('memoryUsage');
      expect(metrics).toHaveProperty('errorRate');
      expect(typeof metrics.hitRate).toBe('number');
    });

    it('should log performance report in development mode', () => {
      // Set NODE_ENV to development
      const originalEnv = process.env.NODE_ENV;
      process.env.NODE_ENV = 'development';

      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      act(() => {
        result.current.logPerformanceReport();
      });
      
      expect(mockConsole.group).toHaveBeenCalledWith('📊 Cache Performance Report');
      expect(mockConsole.groupEnd).toHaveBeenCalled();

      // Restore original NODE_ENV
      process.env.NODE_ENV = originalEnv;
    });
  });

  describe('cache operations', () => {
    it('should provide cache manager instance', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      expect(result.current.cacheManager).toBeDefined();
      expect(typeof result.current.cacheManager.invalidateAll).toBe('function');
      expect(typeof result.current.cacheManager.clearAll).toBe('function');
    });

    it('should provide quick actions', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      expect(result.current.quickActions).toBeDefined();
      expect(typeof result.current.quickActions.warmupForUser).toBe('function');
      expect(typeof result.current.quickActions.clearStaleData).toBe('function');
      expect(typeof result.current.quickActions.refreshAllStale).toBe('function');
      expect(typeof result.current.quickActions.emergencyCleanup).toBe('function');
    });

    it('should clear stale data correctly', () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      // This should not throw
      expect(() => {
        result.current.quickActions.clearStaleData();
      }).not.toThrow();
    });
  });

  describe('optimistic updates', () => {
    it('should perform optimistic update with rollback', async () => {
      const { result } = renderHook(() => useCacheStrategy(), { wrapper });
      
      const queryKey = ['test', 'key'];
      const updater = (data: any) => ({ ...data, updated: true });
      
      await act(async () => {
        const rollback = await result.current.optimisticUpdateWithRollback(
          queryKey,
          updater,
          'aggressive'
        );
        
        expect(typeof rollback).toBe('function');
      });
    });
  });
});

describe('specialized cache strategy hooks', () => {
  let wrapper: any;

  beforeEach(() => {
    const setup = createWrapper();
    wrapper = setup.Wrapper;
  });

  describe('useRealtimeCacheStrategy', () => {
    it('should provide realtime strategy config', () => {
      const { result } = renderHook(() => useRealtimeCacheStrategy(), { wrapper });
      
      expect(result.current.queryOptions.staleTime).toBe(30 * 1000);
      expect(result.current.queryOptions.refetchInterval).toBe(60 * 1000);
      expect(typeof result.current.invalidate).toBe('function');
      expect(typeof result.current.prefetch).toBe('function');
    });
  });

  describe('useBackgroundCacheStrategy', () => {
    it('should provide background strategy config', () => {
      const { result } = renderHook(() => useBackgroundCacheStrategy(), { wrapper });
      
      expect(result.current.queryOptions.staleTime).toBe(5 * 60 * 1000);
      expect(result.current.queryOptions.refetchInterval).toBe(10 * 60 * 1000);
      expect(typeof result.current.invalidate).toBe('function');
      expect(typeof result.current.prefetch).toBe('function');
    });
  });

  describe('useStaticCacheStrategy', () => {
    it('should provide static strategy config', () => {
      const { result } = renderHook(() => useStaticCacheStrategy(), { wrapper });
      
      expect(result.current.queryOptions.staleTime).toBe(60 * 60 * 1000);
      expect(result.current.queryOptions.refetchInterval).toBe(false);
      expect(typeof result.current.invalidate).toBe('function');
      expect(typeof result.current.prefetch).toBe('function');
    });
  });
});

describe('cache optimization', () => {
  let wrapper: any;
  let queryClient: QueryClient;

  beforeEach(() => {
    const setup = createWrapper();
    wrapper = setup.Wrapper;
    queryClient = setup.queryClient;
  });

  it('should optimize cache when error rate is high', async () => {
    const { result } = renderHook(() => useCacheStrategy(), { wrapper });
    
    // Mock high error rate scenario
    const mockQueries = Array.from({ length: 10 }, (_, i) => ({
      queryKey: ['uistudio', 'test', i.toString()],
      state: { error: i < 3 ? new Error('test error') : null }, // 30% error rate
      getObserversCount: () => 1,
      isStale: () => false,
      isActive: () => true,
      isDisabled: () => false,
      isFetching: () => false,
      isPaused: () => false,
    }));

    vi.spyOn(queryClient.getQueryCache(), 'getAll').mockReturnValue(mockQueries as any);
    vi.spyOn(queryClient, 'removeQueries').mockImplementation(() => {});

    await act(async () => {
      await result.current.optimizeCache();
    });

    // Should remove error queries when error rate is high
    expect(queryClient.removeQueries).toHaveBeenCalled();
  });

  it('should handle memory optimization when cache is large', async () => {
    const { result } = renderHook(() => useCacheStrategy(), { wrapper });
    
    // Mock large cache scenario
    const mockQueries = Array.from({ length: 250 }, (_, i) => ({
      queryKey: ['uistudio', 'test', i.toString()],
      state: { 
        error: null, 
        data: {}, 
        dataUpdatedAt: Date.now() - (i * 1000) // Older queries have earlier timestamps
      },
      getObserversCount: () => i < 50 ? 1 : 0, // Only first 50 are active
      isStale: () => false,
      isActive: () => i < 50,
      isDisabled: () => false,
      isFetching: () => false,
      isPaused: () => false,
    }));

    vi.spyOn(queryClient.getQueryCache(), 'getAll').mockReturnValue(mockQueries as any);
    vi.spyOn(queryClient, 'removeQueries').mockImplementation(() => {});

    await act(async () => {
      await result.current.optimizeCache();
    });

    // Should remove some inactive queries when memory usage is high
    expect(queryClient.removeQueries).toHaveBeenCalled();
  });
});