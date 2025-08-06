/**
 * Cache Management Utilities
 * 
 * Advanced cache management for UIStudio API responses with intelligent
 * invalidation, background refresh, and performance optimizations.
 * 
 * @module CacheManager
 */

import { QueryClient, QueryKey, QueryFilters } from '@tanstack/react-query';
import type { 
  UIStudioEntityId, 
  UIStudioResourceType,
  GetPublishedPagesQuery,
  GetVersionHistoryQuery,
  GetResourcePermissionsQuery
} from '../types/uistudio';

// ============================================================================
// Cache Key Management
// ============================================================================

/** Cache key factory for UIStudio resources */
export const cacheKeys = {
  // Root keys
  all: ['uistudio'] as const,
  
  // Entity-based keys
  pages: () => [...cacheKeys.all, 'pages'] as const,
  layouts: () => [...cacheKeys.all, 'layouts'] as const,
  bindings: () => [...cacheKeys.all, 'bindings'] as const,
  templates: () => [...cacheKeys.all, 'templates'] as const,
  permissions: () => [...cacheKeys.all, 'permissions'] as const,
  versions: () => [...cacheKeys.all, 'versions'] as const,
  
  // Specific resource keys
  page: (id: UIStudioEntityId) => [...cacheKeys.pages(), id] as const,
  layout: (id: UIStudioEntityId) => [...cacheKeys.layouts(), id] as const,
  binding: (id: UIStudioEntityId) => [...cacheKeys.bindings(), id] as const,
  template: (id: UIStudioEntityId) => [...cacheKeys.templates(), id] as const,
  permission: (id: UIStudioEntityId) => [...cacheKeys.permissions(), id] as const,
  version: (id: UIStudioEntityId) => [...cacheKeys.versions(), id] as const,
  
  // Query-based keys
  pagesByOwner: (ownerId: UIStudioEntityId) => 
    [...cacheKeys.pages(), 'by-owner', ownerId] as const,
  publishedPages: (query?: GetPublishedPagesQuery) => 
    [...cacheKeys.pages(), 'published', query] as const,
  pageBindings: (pageId: UIStudioEntityId) => 
    [...cacheKeys.pages(), pageId, 'bindings'] as const,
  pageBindingsBySlug: (pageSlug: string) => 
    [...cacheKeys.bindings(), 'by-page-slug', pageSlug] as const,
  templatesByOwner: (ownerId: UIStudioEntityId) => 
    [...cacheKeys.templates(), 'by-owner', ownerId] as const,
  resourcePermissions: (resourceId: UIStudioEntityId, resourceType: string) => 
    [...cacheKeys.permissions(), 'resource', resourceId, resourceType] as const,
  versionHistory: (resourceId: UIStudioEntityId, query?: GetVersionHistoryQuery) => 
    [...cacheKeys.versions(), 'history', resourceId, query] as const,
  
  // Health and status
  health: () => [...cacheKeys.all, 'health'] as const,
  status: () => [...cacheKeys.all, 'status'] as const,
  
  // Related data patterns
  pageWithBindings: (pageId: UIStudioEntityId) => 
    [...cacheKeys.pages(), pageId, 'with-bindings'] as const,
  pageWithLayout: (pageId: UIStudioEntityId) => 
    [...cacheKeys.pages(), pageId, 'with-layout'] as const,
  layoutWithPages: (layoutId: UIStudioEntityId) => 
    [...cacheKeys.layouts(), layoutId, 'with-pages'] as const,
} as const;

// ============================================================================
// Cache Invalidation Strategies
// ============================================================================

/** Cache invalidation configuration for different operations */
export interface CacheInvalidationConfig {
  /** Keys to invalidate immediately */
  invalidate?: QueryKey[];
  /** Keys to remove from cache */
  remove?: QueryKey[];
  /** Keys to refetch in background */
  refetch?: QueryKey[];
  /** Keys to reset to initial state */
  reset?: QueryKey[];
}

/** Cache invalidation patterns for UIStudio operations */
export const invalidationPatterns = {
  // Page operations
  createPage: (createdByEntityId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.pages(),
      cacheKeys.pagesByOwner(createdByEntityId),
      cacheKeys.publishedPages(),
    ],
  }),
  
  updatePage: (pageId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.page(pageId),
      cacheKeys.pages(),
      cacheKeys.pageWithBindings(pageId),
      cacheKeys.pageWithLayout(pageId),
    ],
    refetch: [
      cacheKeys.publishedPages(),
    ],
  }),
  
  deletePage: (pageId: UIStudioEntityId): CacheInvalidationConfig => ({
    remove: [
      cacheKeys.page(pageId),
      cacheKeys.pageWithBindings(pageId),
      cacheKeys.pageWithLayout(pageId),
    ],
    invalidate: [
      cacheKeys.pages(),
      cacheKeys.publishedPages(),
    ],
  }),
  
  publishPage: (pageId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.page(pageId),
      cacheKeys.publishedPages(),
    ],
  }),
  
  // Layout operations
  createLayout: (): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.layouts(),
    ],
  }),
  
  updateLayout: (layoutId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.layout(layoutId),
      cacheKeys.layouts(),
      cacheKeys.layoutWithPages(layoutId),
    ],
  }),
  
  // Binding operations
  createBinding: (pageSlug: string): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.bindings(),
      cacheKeys.pageBindingsBySlug(pageSlug),
    ],
  }),
  
  updateBinding: (bindingId: UIStudioEntityId, pageSlug?: string): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.binding(bindingId),
      cacheKeys.bindings(),
      ...(pageSlug ? [cacheKeys.pageBindingsBySlug(pageSlug)] : []),
    ],
  }),
  
  deleteBinding: (bindingId: UIStudioEntityId, pageSlug?: string): CacheInvalidationConfig => ({
    remove: [
      cacheKeys.binding(bindingId),
    ],
    invalidate: [
      cacheKeys.bindings(),
      ...(pageSlug ? [cacheKeys.pageBindingsBySlug(pageSlug)] : []),
    ],
  }),
  
  // Template operations
  createTemplate: (createdByEntityId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.templates(),
      cacheKeys.templatesByOwner(createdByEntityId),
    ],
  }),
  
  applyTemplate: (createdByEntityId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.pages(),
      cacheKeys.pagesByOwner(createdByEntityId),
    ],
  }),
  
  // Version operations
  createVersion: (resourceId: UIStudioEntityId): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.versionHistory(resourceId),
    ],
  }),
  
  rollbackToVersion: (): CacheInvalidationConfig => ({
    invalidate: [
      cacheKeys.all, // Rollback affects everything
    ],
  }),
} as const;

// ============================================================================
// Cache Manager Class
// ============================================================================

export class UIStudioCacheManager {
  constructor(private queryClient: QueryClient) {}

  // ------------------------------------------------------------------------
  // Cache Invalidation Methods
  // ------------------------------------------------------------------------

  /** Apply cache invalidation configuration */
  async applyInvalidation(config: CacheInvalidationConfig): Promise<void> {
    const promises: Promise<void>[] = [];

    // Remove specific keys
    if (config.remove) {
      config.remove.forEach(key => {
        this.queryClient.removeQueries({ queryKey: key });
      });
    }

    // Reset specific keys
    if (config.reset) {
      config.reset.forEach(key => {
        this.queryClient.resetQueries({ queryKey: key });
      });
    }

    // Invalidate and refetch
    if (config.invalidate) {
      config.invalidate.forEach(key => {
        promises.push(
          this.queryClient.invalidateQueries({ queryKey: key })
        );
      });
    }

    // Background refetch
    if (config.refetch) {
      config.refetch.forEach(key => {
        promises.push(
          this.queryClient.refetchQueries({ 
            queryKey: key,
            type: 'active' // Only refetch active queries
          })
        );
      });
    }

    await Promise.all(promises);
  }

  /** Invalidate all UIStudio cache */
  async invalidateAll(): Promise<void> {
    await this.queryClient.invalidateQueries({ queryKey: cacheKeys.all });
  }

  /** Clear all UIStudio cache */
  clearAll(): void {
    this.queryClient.removeQueries({ queryKey: cacheKeys.all });
  }

  // ------------------------------------------------------------------------
  // Smart Cache Operations
  // ------------------------------------------------------------------------

  /** Prefetch related data based on current operation */
  async prefetchRelatedData(
    operation: 'page' | 'layout' | 'binding' | 'template',
    entityId: UIStudioEntityId
  ): Promise<void> {
    const promises: Promise<unknown>[] = [];

    switch (operation) {
      case 'page':
        // Prefetch page bindings when loading a page
        promises.push(
          this.queryClient.prefetchQuery({
            queryKey: cacheKeys.pageBindings(entityId),
            staleTime: 5 * 60 * 1000, // 5 minutes
          })
        );
        break;

      case 'layout':
        // Prefetch pages using this layout
        promises.push(
          this.queryClient.prefetchQuery({
            queryKey: cacheKeys.layoutWithPages(entityId),
            staleTime: 10 * 60 * 1000, // 10 minutes
          })
        );
        break;

      case 'binding':
        // Could prefetch related component data
        break;

      case 'template':
        // Could prefetch template usage data
        break;
    }

    await Promise.allSettled(promises);
  }

  /** Background refresh for stale data */
  async backgroundRefresh(resourceType?: UIStudioResourceType): Promise<void> {
    let queryKey: any;
    
    if (resourceType) {
      // Get the appropriate cache key function based on resource type
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
    } else {
      queryKey = cacheKeys.all;
    }
    
    const filters: QueryFilters = {
      queryKey,
      stale: true,
      type: 'active',
    };

    await this.queryClient.refetchQueries(filters);
  }

  /** Optimistic update with rollback capability */
  async optimisticUpdate<T>(
    queryKey: QueryKey,
    updater: (oldData: T | undefined) => T,
    rollbackData?: T
  ): Promise<() => void> {
    // Cancel outgoing refetches
    await this.queryClient.cancelQueries({ queryKey });

    // Snapshot the previous value
    const previousData = this.queryClient.getQueryData<T>(queryKey);

    // Optimistically update
    this.queryClient.setQueryData(queryKey, updater(previousData));

    // Return rollback function
    return () => {
      this.queryClient.setQueryData(queryKey, rollbackData ?? previousData);
    };
  }

  // ------------------------------------------------------------------------
  // Cache Analytics and Monitoring
  // ------------------------------------------------------------------------

  /** Get cache statistics */
  getCacheStats(): {
    totalQueries: number;
    staleQueries: number;
    activeQueries: number;
    cachedQueries: number;
  } {
    const allQueries = this.queryClient.getQueryCache().getAll();
    const uistudioQueries = allQueries.filter(query => 
      query.queryKey[0] === 'uistudio'
    );

    return {
      totalQueries: uistudioQueries.length,
      staleQueries: uistudioQueries.filter(q => q.isStale()).length,
      activeQueries: uistudioQueries.filter(q => q.getObserversCount() > 0).length,
      cachedQueries: uistudioQueries.filter(q => q.state.data !== undefined).length,
    };
  }

  /** Get cache health information */
  getCacheHealth(): {
    status: 'healthy' | 'warning' | 'error';
    issues: string[];
    recommendations: string[];
  } {
    const stats = this.getCacheStats();
    const issues: string[] = [];
    const recommendations: string[] = [];
    let status: 'healthy' | 'warning' | 'error' = 'healthy';

    // Check for too many stale queries
    const staleRatio = stats.staleQueries / stats.totalQueries;
    if (staleRatio > 0.5) {
      issues.push(`High stale query ratio: ${(staleRatio * 100).toFixed(1)}%`);
      recommendations.push('Consider adjusting staleTime configuration');
      status = 'warning';
    }

    // Check for memory usage (too many cached queries)
    if (stats.cachedQueries > 100) {
      issues.push(`Large number of cached queries: ${stats.cachedQueries}`);
      recommendations.push('Consider reducing gcTime for less critical data');
      if (stats.cachedQueries > 200) {
        status = 'error';
      }
    }

    // Check for inactive cached data
    const inactiveRatio = (stats.cachedQueries - stats.activeQueries) / stats.cachedQueries;
    if (inactiveRatio > 0.7) {
      issues.push(`High inactive cache ratio: ${(inactiveRatio * 100).toFixed(1)}%`);
      recommendations.push('Consider more aggressive garbage collection');
      status = status === 'error' ? 'error' : 'warning';
    }

    return { status, issues, recommendations };
  }

  /** Log cache performance metrics */
  logCacheMetrics(): void {
    if (import.meta.env.DEV) {
      const stats = this.getCacheStats();
      const health = this.getCacheHealth();
      
      console.group('🔄 UIStudio Cache Metrics');
      console.log('📊 Statistics:', stats);
      console.log('🏥 Health:', health);
      
      if (health.issues.length > 0) {
        console.warn('⚠️ Issues:', health.issues);
        console.info('💡 Recommendations:', health.recommendations);
      }
      
      console.groupEnd();
    }
  }

  // ------------------------------------------------------------------------
  // Cache Warming
  // ------------------------------------------------------------------------

  /** Warm up cache with commonly accessed data */
  async warmupCache(userId?: UIStudioEntityId): Promise<void> {
    const promises: Promise<unknown>[] = [];

    // Always prefetch published pages
    promises.push(
      this.queryClient.prefetchQuery({
        queryKey: cacheKeys.publishedPages({ limit: 20 }),
        staleTime: 10 * 60 * 1000, // 10 minutes
      })
    );

    // Always prefetch health status
    promises.push(
      this.queryClient.prefetchQuery({
        queryKey: cacheKeys.health(),
        staleTime: 5 * 60 * 1000, // 5 minutes
      })
    );

    // If user is provided, prefetch user-specific data
    if (userId) {
      promises.push(
        this.queryClient.prefetchQuery({
          queryKey: cacheKeys.pagesByOwner(userId),
          staleTime: 5 * 60 * 1000, // 5 minutes
        })
      );

      promises.push(
        this.queryClient.prefetchQuery({
          queryKey: cacheKeys.templatesByOwner(userId),
          staleTime: 15 * 60 * 1000, // 15 minutes
        })
      );
    }

    await Promise.allSettled(promises);
  }
}

// ============================================================================
// Hook for Cache Manager
// ============================================================================

/** Hook to access cache manager instance */
export function createCacheManager(queryClient: QueryClient): UIStudioCacheManager {
  return new UIStudioCacheManager(queryClient);
}

// ============================================================================
// Cache Utilities
// ============================================================================

/** Check if a query key matches a pattern */
export function matchesQueryKey(
  queryKey: QueryKey,
  pattern: QueryKey
): boolean {
  if (pattern.length > queryKey.length) return false;
  
  return pattern.every((part, index) => 
    part === queryKey[index] || part === '*'
  );
}

/** Get cache key depth for debugging */
export function getCacheKeyDepth(queryKey: QueryKey): number {
  return queryKey.length;
}

/** Format cache key for logging */
export function formatCacheKey(queryKey: QueryKey): string {
  return queryKey.join(' > ');
}

/** Extract entity ID from cache key if present */
export function extractEntityId(queryKey: QueryKey): UIStudioEntityId | null {
  // Look for UUID pattern in the key
  const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  
  for (const part of queryKey) {
    if (typeof part === 'string' && uuidRegex.test(part)) {
      return part;
    }
  }
  
  return null;
}