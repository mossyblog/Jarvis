/**
 * Cache Strategy Hook
 * 
 * Provides easy access to different cache strategies and management
 * functions for optimal performance in UIStudio applications.
 * 
 * @module UseCacheStrategy
 */

import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useCallback, useEffect, useMemo } from 'react';
import { 
  cacheKeys, 
  createCacheManager,
  type CacheInvalidationConfig 
} from '../utils/cacheManager';
import type { UIStudioEntityId, UIStudioResourceType } from '../types/uistudio';

// ============================================================================
// Cache Strategy Types
// ============================================================================

export type CacheStrategy = 
  | 'realtime'     // Frequent updates, short stale time
  | 'background'   // Background refresh, moderate stale time
  | 'lazy'         // On-demand loading, long stale time
  | 'aggressive'   // Immediate updates, no stale tolerance
  | 'static'       // Rarely changes, very long stale time
  | 'user-specific'; // User-dependent data

export interface CacheStrategyOptions {
  /** Strategy type */
  strategy: CacheStrategy;
  /** Custom stale time override */
  staleTime?: number;
  /** Custom garbage collection time override */
  gcTime?: number;
  /** Enable background refresh */
  backgroundRefresh?: boolean;
  /** Custom refetch interval */
  refetchInterval?: number | false;
  /** Prefetch related data */
  prefetchRelated?: boolean;
}

export interface CachePerformanceMetrics {
  hitRate: number;
  missRate: number;
  staleRate: number;
  memoryUsage: number;
  avgResponseTime: number;
  errorRate: number;
}

// ============================================================================
// Cache Strategy Hook
// ============================================================================

export function useCacheStrategy() {
  const queryClient = useQueryClient();
  const cacheManager = useMemo(() => createCacheManager(queryClient), [queryClient]);

  // -------------------------------------------------------------------------
  // Strategy Configuration
  // -------------------------------------------------------------------------

  const getStrategyConfig = useCallback((strategy: CacheStrategy) => {
    const baseConfig = {
      refetchOnWindowFocus: true,
      refetchOnReconnect: true,
      refetchOnMount: true,
    };

    switch (strategy) {
      case 'realtime':
        return {
          ...baseConfig,
          staleTime: 30 * 1000, // 30 seconds
          gcTime: 2 * 60 * 1000, // 2 minutes
          refetchInterval: 60 * 1000, // 1 minute
          refetchIntervalInBackground: true,
        };

      case 'background':
        return {
          ...baseConfig,
          staleTime: 5 * 60 * 1000, // 5 minutes
          gcTime: 15 * 60 * 1000, // 15 minutes
          refetchInterval: 10 * 60 * 1000, // 10 minutes
          refetchIntervalInBackground: true,
          refetchOnWindowFocus: false,
        };

      case 'lazy':
        return {
          ...baseConfig,
          staleTime: 30 * 60 * 1000, // 30 minutes
          gcTime: 2 * 60 * 60 * 1000, // 2 hours
          refetchInterval: false,
          refetchOnWindowFocus: false,
          refetchOnMount: false,
        };

      case 'aggressive':
        return {
          ...baseConfig,
          staleTime: 0, // Always stale
          gcTime: 60 * 1000, // 1 minute
          refetchInterval: 30 * 1000, // 30 seconds
          refetchIntervalInBackground: true,
          refetchOnMount: 'always' as const,
        };

      case 'static':
        return {
          staleTime: 60 * 60 * 1000, // 1 hour
          gcTime: 4 * 60 * 60 * 1000, // 4 hours
          refetchInterval: false,
          refetchOnWindowFocus: false,
          refetchOnMount: false,
          refetchOnReconnect: false,
        };

      case 'user-specific':
        return {
          ...baseConfig,
          staleTime: 2 * 60 * 1000, // 2 minutes
          gcTime: 10 * 60 * 1000, // 10 minutes
          refetchInterval: 5 * 60 * 1000, // 5 minutes
          refetchIntervalInBackground: true,
        };

      default:
        return baseConfig;
    }
  }, []);

  // -------------------------------------------------------------------------
  // Cache Operations
  // -------------------------------------------------------------------------

  const invalidateByStrategy = useCallback(async (
    resourceType: UIStudioResourceType,
    strategy: CacheStrategy = 'background'
  ) => {
    let queryKey: QueryKey;
    
    // Get the appropriate cache key based on resource type
    switch (resourceType) {
      case 'page':
        queryKey = cacheKeys.pages();
        break;
      case 'layout':
        queryKey = cacheKeys.layouts();
        break;
      case 'binding':
        queryKey = cacheKeys.bindings();
        break;
      case 'template':
        queryKey = cacheKeys.templates();
        break;
      case 'permission':
        queryKey = cacheKeys.permissions();
        break;
      case 'version':
        queryKey = cacheKeys.versions();
        break;
      default:
        queryKey = cacheKeys.all;
    }
    
    const config: CacheInvalidationConfig = {
      invalidate: [queryKey],
    };

    if (strategy === 'aggressive') {
      // Aggressive strategy also refetches immediately
      config.refetch = config.invalidate;
    }

    await cacheManager.applyInvalidation(config);
  }, [cacheManager]);

  const prefetchWithStrategy = useCallback(async (
    queryKey: QueryKey,
    queryFn: () => Promise<unknown>,
    strategy: CacheStrategy = 'background'
  ) => {
    const strategyConfig = getStrategyConfig(strategy);
    
    await queryClient.prefetchQuery({
      queryKey,
      queryFn,
      ...strategyConfig,
    });
  }, [queryClient, getStrategyConfig]);

  const optimisticUpdateWithRollback = useCallback(async <T>(
    queryKey: QueryKey,
    updater: (oldData: T | undefined) => T,
    strategy: CacheStrategy = 'aggressive'
  ): Promise<() => void> => {
    return cacheManager.optimisticUpdate(queryKey, updater);
  }, [cacheManager]);

  // -------------------------------------------------------------------------
  // Performance Monitoring
  // -------------------------------------------------------------------------

  const getPerformanceMetrics = useCallback((): CachePerformanceMetrics => {
    const allQueries = queryClient.getQueryCache().getAll();
    const uistudioQueries = allQueries.filter(query => 
      query.queryKey[0] === 'uistudio'
    );

    const totalQueries = uistudioQueries.length;
    const staleQueries = uistudioQueries.filter(q => q.isStale()).length;
    const errorQueries = uistudioQueries.filter(q => q.state.error).length;
    const cachedQueries = uistudioQueries.filter(q => q.state.data !== undefined).length;

    return {
      hitRate: totalQueries > 0 ? (cachedQueries / totalQueries) * 100 : 0,
      missRate: totalQueries > 0 ? ((totalQueries - cachedQueries) / totalQueries) * 100 : 0,
      staleRate: totalQueries > 0 ? (staleQueries / totalQueries) * 100 : 0,
      memoryUsage: cachedQueries, // Simplified metric
      avgResponseTime: 0, // Would need to track this separately
      errorRate: totalQueries > 0 ? (errorQueries / totalQueries) * 100 : 0,
    };
  }, [queryClient]);

  const logPerformanceReport = useCallback(() => {
    if (import.meta.env.DEV) {
      const metrics = getPerformanceMetrics();
      const cacheStats = cacheManager.getCacheStats();
      const cacheHealth = cacheManager.getCacheHealth();

      console.group('📊 Cache Performance Report');
      console.log('🎯 Hit Rate:', `${metrics.hitRate.toFixed(1)}%`);
      console.log('❌ Miss Rate:', `${metrics.missRate.toFixed(1)}%`);
      console.log('⏰ Stale Rate:', `${metrics.staleRate.toFixed(1)}%`);
      console.log('🗄️ Memory Usage:', `${metrics.memoryUsage} cached queries`);
      console.log('💔 Error Rate:', `${metrics.errorRate.toFixed(1)}%`);
      console.log('📈 Cache Stats:', cacheStats);
      console.log('🏥 Cache Health:', cacheHealth);
      console.groupEnd();
    }
  }, [getPerformanceMetrics, cacheManager]);

  // -------------------------------------------------------------------------
  // Auto Cleanup & Optimization
  // -------------------------------------------------------------------------

  const optimizeCache = useCallback(async () => {
    const metrics = getPerformanceMetrics();
    const health = cacheManager.getCacheHealth();

    // If error rate is high, clear problematic queries
    if (metrics.errorRate > 20) {
      const allQueries = queryClient.getQueryCache().getAll();
      const errorQueries = allQueries.filter(q => 
        q.queryKey[0] === 'uistudio' && q.state.error
      );
      
      errorQueries.forEach(query => {
        queryClient.removeQueries({ queryKey: query.queryKey });
      });
    }

    // If memory usage is high, clean up inactive queries
    if (health.status === 'error' || metrics.memoryUsage > 200) {
      const allQueries = queryClient.getQueryCache().getAll();
      const inactiveQueries = allQueries.filter(q => 
        q.queryKey[0] === 'uistudio' && 
        q.getObserversCount() === 0 &&
        q.state.data !== undefined
      );

      // Remove oldest inactive queries
      inactiveQueries
        .sort((a, b) => (a.state.dataUpdatedAt || 0) - (b.state.dataUpdatedAt || 0))
        .slice(0, Math.floor(inactiveQueries.length * 0.5))
        .forEach(query => {
          queryClient.removeQueries({ queryKey: query.queryKey });
        });
    }

    // Background refresh stale data if hit rate is low
    if (metrics.hitRate < 60) {
      await cacheManager.backgroundRefresh();
    }
  }, [getPerformanceMetrics, cacheManager, queryClient]);

  // -------------------------------------------------------------------------
  // Auto-optimization Effect
  // -------------------------------------------------------------------------

  useEffect(() => {
    const interval = setInterval(() => {
      optimizeCache();
    }, 10 * 60 * 1000); // Run every 10 minutes

    return () => clearInterval(interval);
  }, [optimizeCache]);

  // -------------------------------------------------------------------------
  // Return API
  // -------------------------------------------------------------------------

  return {
    // Strategy configuration
    getStrategyConfig,
    
    // Cache operations
    invalidateByStrategy,
    prefetchWithStrategy,
    optimisticUpdateWithRollback,
    
    // Direct cache manager access
    cacheManager,
    
    // Performance monitoring
    getPerformanceMetrics,
    logPerformanceReport,
    optimizeCache,
    
    // Convenience methods for common patterns
    strategies: {
      realtime: (options?: Partial<CacheStrategyOptions>) => ({
        ...getStrategyConfig('realtime'),
        ...options,
      }),
      background: (options?: Partial<CacheStrategyOptions>) => ({
        ...getStrategyConfig('background'),
        ...options,
      }),
      lazy: (options?: Partial<CacheStrategyOptions>) => ({
        ...getStrategyConfig('lazy'),
        ...options,
      }),
      aggressive: (options?: Partial<CacheStrategyOptions>) => ({
        ...getStrategyConfig('aggressive'),
        ...options,
      }),
      static: (options?: Partial<CacheStrategyOptions>) => ({
        ...getStrategyConfig('static'),
        ...options,
      }),
      userSpecific: (options?: Partial<CacheStrategyOptions>) => ({
        ...getStrategyConfig('user-specific'),
        ...options,
      }),
    },
    
    // Quick actions
    quickActions: {
      warmupForUser: (userId: UIStudioEntityId) => cacheManager.warmupCache(userId),
      clearStaleData: () => {
        const allQueries = queryClient.getQueryCache().getAll();
        const staleQueries = allQueries.filter(q => 
          q.queryKey[0] === 'uistudio' && q.isStale()
        );
        staleQueries.forEach(query => {
          queryClient.removeQueries({ queryKey: query.queryKey });
        });
      },
      refreshAllStale: () => cacheManager.backgroundRefresh(),
      emergencyCleanup: () => {
        queryClient.removeQueries({ 
          queryKey: cacheKeys.all,
          type: 'inactive'
        });
      },
    },
  };
}

// ============================================================================
// Specialized Cache Strategy Hooks
// ============================================================================

/** Hook for real-time data with aggressive cache strategy */
export function useRealtimeCacheStrategy() {
  const { strategies, invalidateByStrategy, prefetchWithStrategy } = useCacheStrategy();
  
  return {
    queryOptions: strategies.realtime(),
    invalidate: (resourceType: UIStudioResourceType) => 
      invalidateByStrategy(resourceType, 'aggressive'),
    prefetch: (queryKey: QueryKey, queryFn: () => Promise<unknown>) =>
      prefetchWithStrategy(queryKey, queryFn, 'realtime'),
  };
}

/** Hook for background refresh cache strategy */
export function useBackgroundCacheStrategy() {
  const { strategies, invalidateByStrategy, prefetchWithStrategy } = useCacheStrategy();
  
  return {
    queryOptions: strategies.background(),
    invalidate: (resourceType: UIStudioResourceType) => 
      invalidateByStrategy(resourceType, 'background'),
    prefetch: (queryKey: QueryKey, queryFn: () => Promise<unknown>) =>
      prefetchWithStrategy(queryKey, queryFn, 'background'),
  };
}

/** Hook for static data cache strategy */
export function useStaticCacheStrategy() {
  const { strategies, invalidateByStrategy, prefetchWithStrategy } = useCacheStrategy();
  
  return {
    queryOptions: strategies.static(),
    invalidate: (resourceType: UIStudioResourceType) => 
      invalidateByStrategy(resourceType, 'lazy'),
    prefetch: (queryKey: QueryKey, queryFn: () => Promise<unknown>) =>
      prefetchWithStrategy(queryKey, queryFn, 'static'),
  };
}