/**
 * Cache Manager Tests
 * 
 * Tests for the cache management utilities and strategies
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { QueryClient } from '@tanstack/react-query';
import { 
  UIStudioCacheManager, 
  cacheKeys, 
  invalidationPatterns,
  createCacheManager,
  matchesQueryKey,
  getCacheKeyDepth,
  formatCacheKey,
  extractEntityId
} from '../cacheManager';

// Mock query client
const createMockQueryClient = () => ({
  invalidateQueries: vi.fn(),
  removeQueries: vi.fn(),
  resetQueries: vi.fn(),
  refetchQueries: vi.fn(),
  prefetchQuery: vi.fn(),
  getQueryData: vi.fn(),
  setQueryData: vi.fn(),
  cancelQueries: vi.fn(),
  getQueryCache: vi.fn(() => ({
    getAll: vi.fn(() => []),
  })),
});

describe('cacheKeys', () => {
  it('should generate correct cache keys', () => {
    expect(cacheKeys.all).toEqual(['uistudio']);
    expect(cacheKeys.pages()).toEqual(['uistudio', 'pages']);
    expect(cacheKeys.page('test-id')).toEqual(['uistudio', 'pages', 'test-id']);
    expect(cacheKeys.pagesByOwner('owner-id')).toEqual(['uistudio', 'pages', 'by-owner', 'owner-id']);
  });

  it('should generate nested cache keys correctly', () => {
    const pageId = 'page-123';
    expect(cacheKeys.pageBindings(pageId)).toEqual(['uistudio', 'pages', pageId, 'bindings']);
    expect(cacheKeys.pageWithBindings(pageId)).toEqual(['uistudio', 'pages', pageId, 'with-bindings']);
  });

  it('should handle query parameters in cache keys', () => {
    const query = { limit: 10, offset: 0 };
    expect(cacheKeys.publishedPages(query)).toEqual(['uistudio', 'pages', 'published', query]);
    expect(cacheKeys.versionHistory('resource-id', query)).toEqual(['uistudio', 'versions', 'history', 'resource-id', query]);
  });
});

describe('invalidationPatterns', () => {
  it('should provide correct invalidation pattern for page creation', () => {
    const createdByEntityId = 'user-123';
    const pattern = invalidationPatterns.createPage(createdByEntityId);
    
    expect(pattern.invalidate).toEqual(expect.arrayContaining([
      cacheKeys.pages(),
      cacheKeys.pagesByOwner(createdByEntityId),
      cacheKeys.publishedPages(),
    ]));
  });

  it('should provide correct invalidation pattern for page update', () => {
    const pageId = 'page-123';
    const pattern = invalidationPatterns.updatePage(pageId);
    
    expect(pattern.invalidate).toEqual(expect.arrayContaining([
      cacheKeys.page(pageId),
      cacheKeys.pages(),
    ]));
    expect(pattern.refetch).toEqual(expect.arrayContaining([
      cacheKeys.publishedPages(),
    ]));
  });

  it('should provide correct invalidation pattern for page deletion', () => {
    const pageId = 'page-123';
    const pattern = invalidationPatterns.deletePage(pageId);
    
    expect(pattern.remove).toEqual(expect.arrayContaining([
      cacheKeys.page(pageId),
    ]));
    expect(pattern.invalidate).toEqual(expect.arrayContaining([
      cacheKeys.pages(),
    ]));
  });

  it('should provide correct invalidation pattern for rollback operation', () => {
    const pattern = invalidationPatterns.rollbackToVersion();
    
    expect(pattern.invalidate).toEqual(expect.arrayContaining([
      cacheKeys.all,
    ]));
  });
});

describe('UIStudioCacheManager', () => {
  let mockQueryClient: any;
  let cacheManager: UIStudioCacheManager;

  beforeEach(() => {
    mockQueryClient = createMockQueryClient();
    cacheManager = new UIStudioCacheManager(mockQueryClient as unknown as QueryClient);
  });

  describe('applyInvalidation', () => {
    it('should remove queries when specified', async () => {
      const config = {
        remove: [cacheKeys.page('test-id')]
      };

      await cacheManager.applyInvalidation(config);

      expect(mockQueryClient.removeQueries).toHaveBeenCalledWith({
        queryKey: cacheKeys.page('test-id')
      });
    });

    it('should invalidate queries when specified', async () => {
      const config = {
        invalidate: [cacheKeys.pages()]
      };

      await cacheManager.applyInvalidation(config);

      expect(mockQueryClient.invalidateQueries).toHaveBeenCalledWith({
        queryKey: cacheKeys.pages()
      });
    });

    it('should refetch queries when specified', async () => {
      const config = {
        refetch: [cacheKeys.publishedPages()]
      };

      await cacheManager.applyInvalidation(config);

      expect(mockQueryClient.refetchQueries).toHaveBeenCalledWith({
        queryKey: cacheKeys.publishedPages(),
        type: 'active'
      });
    });

    it('should reset queries when specified', async () => {
      const config = {
        reset: [cacheKeys.all]
      };

      await cacheManager.applyInvalidation(config);

      expect(mockQueryClient.resetQueries).toHaveBeenCalledWith({
        queryKey: cacheKeys.all
      });
    });
  });

  describe('optimisticUpdate', () => {
    it('should perform optimistic update and return rollback function', async () => {
      const queryKey = cacheKeys.page('test-id');
      const oldData = [{ id: 'test-id', name: 'old-name' }];
      const updater = (data: any) => [{ ...data[0], name: 'new-name' }];

      mockQueryClient.getQueryData.mockReturnValue(oldData);

      const rollback = await cacheManager.optimisticUpdate(queryKey, updater);

      expect(mockQueryClient.cancelQueries).toHaveBeenCalledWith({ queryKey });
      expect(mockQueryClient.setQueryData).toHaveBeenCalledWith(
        queryKey,
        [{ id: 'test-id', name: 'new-name' }]
      );

      // Test rollback
      rollback();
      expect(mockQueryClient.setQueryData).toHaveBeenCalledWith(queryKey, oldData);
    });
  });

  describe('getCacheStats', () => {
    it('should return cache statistics', () => {
      const mockQueries = [
        {
          queryKey: ['uistudio', 'pages'],
          isStale: () => false,
          getObserversCount: () => 1,
          state: { data: {} }
        },
        {
          queryKey: ['uistudio', 'layouts'],
          isStale: () => true,
          getObserversCount: () => 0,
          state: { data: undefined }
        },
        {
          queryKey: ['other', 'query'],
          isStale: () => false,
          getObserversCount: () => 1,
          state: { data: {} }
        }
      ];

      mockQueryClient.getQueryCache.mockReturnValue({
        getAll: () => mockQueries
      });

      const stats = cacheManager.getCacheStats();

      expect(stats.totalQueries).toBe(2); // Only UIStudio queries
      expect(stats.staleQueries).toBe(1);
      expect(stats.activeQueries).toBe(1);
      expect(stats.cachedQueries).toBe(1);
    });
  });

  describe('getCacheHealth', () => {
    it('should return healthy status for good cache state', () => {
      const mockQueries = [
        {
          queryKey: ['uistudio', 'pages'],
          isStale: () => false,
          getObserversCount: () => 1,
          state: { data: {} }
        }
      ];

      mockQueryClient.getQueryCache.mockReturnValue({
        getAll: () => mockQueries
      });

      const health = cacheManager.getCacheHealth();

      expect(health.status).toBe('healthy');
      expect(health.issues).toHaveLength(0);
    });

    it('should return warning status for high stale ratio', () => {
      const mockQueries = Array.from({ length: 10 }, (_, i) => ({
        queryKey: ['uistudio', 'pages', i.toString()],
        isStale: () => i < 6, // 60% stale
        getObserversCount: () => 1,
        state: { data: {} }
      }));

      mockQueryClient.getQueryCache.mockReturnValue({
        getAll: () => mockQueries
      });

      const health = cacheManager.getCacheHealth();

      expect(health.status).toBe('warning');
      expect(health.issues[0]).toContain('High stale query ratio');
    });
  });

  describe('warmupCache', () => {
    it('should prefetch common queries', async () => {
      await cacheManager.warmupCache();

      expect(mockQueryClient.prefetchQuery).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: cacheKeys.publishedPages({ limit: 20 })
        })
      );
      expect(mockQueryClient.prefetchQuery).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: cacheKeys.health()
        })
      );
    });

    it('should prefetch user-specific data when userId provided', async () => {
      const userId = 'user-123';
      await cacheManager.warmupCache(userId);

      expect(mockQueryClient.prefetchQuery).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: cacheKeys.pagesByOwner(userId)
        })
      );
      expect(mockQueryClient.prefetchQuery).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: cacheKeys.templatesByOwner(userId)
        })
      );
    });
  });
});

describe('utility functions', () => {
  describe('matchesQueryKey', () => {
    it('should match exact query keys', () => {
      const queryKey = ['uistudio', 'pages', 'test-id'];
      const pattern = ['uistudio', 'pages', 'test-id'];
      
      expect(matchesQueryKey(queryKey, pattern)).toBe(true);
    });

    it('should match wildcard patterns', () => {
      const queryKey = ['uistudio', 'pages', 'test-id'];
      const pattern = ['uistudio', 'pages', '*'];
      
      expect(matchesQueryKey(queryKey, pattern)).toBe(true);
    });

    it('should not match when pattern is longer', () => {
      const queryKey = ['uistudio', 'pages'];
      const pattern = ['uistudio', 'pages', 'test-id'];
      
      expect(matchesQueryKey(queryKey, pattern)).toBe(false);
    });

    it('should not match different keys', () => {
      const queryKey = ['uistudio', 'pages', 'test-id'];
      const pattern = ['uistudio', 'layouts', 'test-id'];
      
      expect(matchesQueryKey(queryKey, pattern)).toBe(false);
    });
  });

  describe('getCacheKeyDepth', () => {
    it('should return correct depth', () => {
      expect(getCacheKeyDepth(['uistudio'])).toBe(1);
      expect(getCacheKeyDepth(['uistudio', 'pages'])).toBe(2);
      expect(getCacheKeyDepth(['uistudio', 'pages', 'test-id'])).toBe(3);
    });
  });

  describe('formatCacheKey', () => {
    it('should format cache key for display', () => {
      const queryKey = ['uistudio', 'pages', 'test-id'];
      expect(formatCacheKey(queryKey)).toBe('uistudio > pages > test-id');
    });
  });

  describe('extractEntityId', () => {
    it('should extract UUID from cache key', () => {
      const queryKey = ['uistudio', 'pages', '550e8400-e29b-41d4-a716-446655440000'];
      expect(extractEntityId(queryKey)).toBe('550e8400-e29b-41d4-a716-446655440000');
    });

    it('should return null when no UUID found', () => {
      const queryKey = ['uistudio', 'pages', 'not-a-uuid'];
      expect(extractEntityId(queryKey)).toBe(null);
    });

    it('should return null for empty cache key', () => {
      expect(extractEntityId([])).toBe(null);
    });
  });
});

describe('createCacheManager', () => {
  it('should create cache manager instance', () => {
    const mockQueryClient = createMockQueryClient();
    const manager = createCacheManager(mockQueryClient as unknown as QueryClient);
    
    expect(manager).toBeInstanceOf(UIStudioCacheManager);
  });
});