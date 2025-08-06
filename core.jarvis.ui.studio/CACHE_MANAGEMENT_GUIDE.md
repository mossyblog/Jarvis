# UIStudio Cache Management Guide

This guide provides comprehensive documentation for the cache management system implemented in UIStudio, which provides intelligent caching, background refresh, and performance optimization for API responses.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Cache Strategies](#cache-strategies)
4. [Implementation](#implementation)
5. [Performance Monitoring](#performance-monitoring)
6. [Best Practices](#best-practices)
7. [API Reference](#api-reference)

## Overview

The UIStudio cache management system builds on top of TanStack Query (React Query) to provide:

- **Intelligent Cache Strategies**: Different caching patterns for different types of data
- **Background Refresh**: Automatic background updates using stale-while-revalidate patterns
- **Smart Invalidation**: Cascade invalidation based on data relationships
- **Performance Optimization**: Automatic cache cleanup and memory management
- **Real-time Monitoring**: Development tools for cache performance tracking

### Key Features

- 🚀 **Multiple Cache Strategies**: Realtime, background, lazy, aggressive, static, and user-specific
- 🔄 **Stale-While-Revalidate**: Serve cached data while refreshing in background
- 🧠 **Smart Invalidation**: Relationship-aware cache invalidation patterns
- 📊 **Performance Monitoring**: Real-time cache metrics and health monitoring
- 🔧 **Developer Tools**: Visual cache monitor for development
- 🎯 **Optimistic Updates**: Fast UI updates with automatic rollback on failure

## Architecture

### Core Components

```
Cache Management System
├── Cache Strategies (useCacheStrategy)
├── Cache Manager (UIStudioCacheManager)
├── Query Provider (Enhanced)
├── Cache Keys Factory
├── Invalidation Patterns
└── Development Tools
```

### Data Flow

```
User Action → Hook → API Call → Cache Update → UI Update
     ↑                              ↓
Cache Invalidation ← Smart Patterns ← Response
```

## Cache Strategies

The system provides 6 different cache strategies optimized for different use cases:

### 1. Realtime Strategy
- **Use Case**: Frequently changing data, live updates
- **Stale Time**: 30 seconds
- **Background Refresh**: Every 60 seconds
- **Example**: Live metrics, real-time notifications

```typescript
const { queryOptions } = useRealtimeCacheStrategy();

const { data } = useQuery({
  queryKey: ['live-metrics'],
  queryFn: fetchLiveMetrics,
  ...queryOptions
});
```

### 2. Background Strategy  
- **Use Case**: Regular updates without blocking UI
- **Stale Time**: 5 minutes
- **Background Refresh**: Every 10 minutes
- **Example**: User preferences, dashboard widgets

```typescript
const { queryOptions } = useBackgroundCacheStrategy();

const { data } = useQuery({
  queryKey: ['user-preferences'],
  queryFn: fetchUserPreferences,
  ...queryOptions
});
```

### 3. Lazy Strategy
- **Use Case**: Rarely changing data, load on demand
- **Stale Time**: 30 minutes
- **Background Refresh**: Disabled
- **Example**: Static configurations, help content

```typescript
const { queryOptions } = useStaticCacheStrategy();

const { data } = useQuery({
  queryKey: ['help-content'],
  queryFn: fetchHelpContent,
  ...queryOptions
});
```

### 4. Aggressive Strategy
- **Use Case**: Critical data requiring immediate updates
- **Stale Time**: 0 (always stale)
- **Background Refresh**: Every 30 seconds
- **Example**: Security settings, critical alerts

```typescript
const { strategies } = useCacheStrategy();

const { data } = useQuery({
  queryKey: ['security-settings'],
  queryFn: fetchSecuritySettings,
  ...strategies.aggressive()
});
```

### 5. Static Strategy
- **Use Case**: Rarely changing reference data
- **Stale Time**: 1 hour
- **Background Refresh**: Disabled
- **Example**: Country lists, static configurations

### 6. User-Specific Strategy
- **Use Case**: User-dependent data
- **Stale Time**: 2 minutes
- **Background Refresh**: Every 5 minutes
- **Example**: User pages, personal settings

## Implementation

### Basic Usage

```typescript
import { useUIStudioPage } from '../hooks/useUIStudio';

function PageComponent({ pageId }: { pageId: string }) {
  // Automatically uses pageDataQueryOptions (background refresh strategy)
  const { data, isLoading, error } = useUIStudioPage(pageId);
  
  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;
  
  return <div>{data?.pageName}</div>;
}
```

### Advanced Cache Management

```typescript
import { useUIStudioCacheManager } from '../hooks/useUIStudio';

function AdminPanel() {
  const cacheManager = useUIStudioCacheManager();
  
  const handleRefreshAll = async () => {
    await cacheManager.refreshStaleData();
  };
  
  const handleClearCache = () => {
    cacheManager.clearAll();
  };
  
  const handleWarmupForUser = async (userId: string) => {
    await cacheManager.warmupForUser(userId);
  };
  
  return (
    <div>
      <button onClick={handleRefreshAll}>Refresh Stale Data</button>
      <button onClick={handleClearCache}>Clear Cache</button>
      <button onClick={() => handleWarmupForUser('user-123')}>
        Warmup Cache
      </button>
    </div>
  );
}
```

### Custom Cache Strategy

```typescript
import { useCacheStrategy } from '../hooks/useCacheStrategy';

function CustomComponent() {
  const { getStrategyConfig } = useCacheStrategy();
  
  // Create a custom strategy
  const customStrategy = {
    ...getStrategyConfig('background'),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 5 * 60 * 1000, // 5 minutes
  };
  
  const { data } = useQuery({
    queryKey: ['custom-data'],
    queryFn: fetchCustomData,
    ...customStrategy
  });
  
  return <div>{data?.value}</div>;
}
```

### Optimistic Updates

```typescript
import { useUpdateUIStudioPage } from '../hooks/useUIStudio';

function PageEditor({ pageId }: { pageId: string }) {
  const updatePage = useUpdateUIStudioPage(pageId);
  
  const handleSave = async (updates: UpdatePageRequest) => {
    // Optimistic update with automatic rollback on error
    await updatePage.mutateAsync(updates);
  };
  
  return (
    <form onSubmit={(e) => {
      e.preventDefault();
      handleSave({ pageName: 'Updated Name', updatedByEntityId: 'user-123' });
    }}>
      <input type="text" defaultValue="Page Name" />
      <button type="submit" disabled={updatePage.isPending}>
        {updatePage.isPending ? 'Saving...' : 'Save'}
      </button>
    </form>
  );
}
```

## Performance Monitoring

### Development Cache Monitor

The system includes a visual cache monitor for development:

```typescript
import CacheMonitor from '../components/dev/CacheMonitor';

function App() {
  return (
    <div>
      {/* Your app content */}
      
      {/* Cache monitor (only in development) */}
      {process.env.NODE_ENV === 'development' && (
        <CacheMonitor 
          detailed={true}
          position="bottom-right"
          updateInterval={3000}
        />
      )}
    </div>
  );
}
```

### Programmatic Monitoring

```typescript
import { useCacheStrategy } from '../hooks/useCacheStrategy';

function PerformancePanel() {
  const { getPerformanceMetrics, logPerformanceReport } = useCacheStrategy();
  
  const metrics = getPerformanceMetrics();
  
  return (
    <div>
      <h3>Cache Performance</h3>
      <p>Hit Rate: {metrics.hitRate.toFixed(1)}%</p>
      <p>Miss Rate: {metrics.missRate.toFixed(1)}%</p>
      <p>Stale Rate: {metrics.staleRate.toFixed(1)}%</p>
      <p>Memory Usage: {metrics.memoryUsage} queries</p>
      <p>Error Rate: {metrics.errorRate.toFixed(1)}%</p>
      
      <button onClick={logPerformanceReport}>
        Log Detailed Report
      </button>
    </div>
  );
}
```

### Cache Health Monitoring

```typescript
import { useUIStudioCacheManager } from '../hooks/useUIStudio';

function SystemHealth() {
  const cacheManager = useUIStudioCacheManager();
  const health = cacheManager.getCacheHealth();
  
  return (
    <div className={`status-${health.status}`}>
      <h3>Cache Health: {health.status}</h3>
      
      {health.issues.length > 0 && (
        <div>
          <h4>Issues:</h4>
          <ul>
            {health.issues.map((issue, index) => (
              <li key={index}>{issue}</li>
            ))}
          </ul>
        </div>
      )}
      
      {health.recommendations.length > 0 && (
        <div>
          <h4>Recommendations:</h4>
          <ul>
            {health.recommendations.map((rec, index) => (
              <li key={index}>{rec}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
```

## Best Practices

### 1. Choose the Right Strategy

```typescript
// ✅ Good: Use appropriate strategy for data type
const { data: staticConfig } = useQuery({
  queryKey: ['app-config'],
  queryFn: fetchAppConfig,
  ...staticQueryOptions  // Rarely changes, long cache
});

const { data: liveData } = useQuery({
  queryKey: ['live-metrics'],
  queryFn: fetchLiveMetrics,
  ...realtimeQueryOptions  // Frequently changes, short cache
});

// ❌ Bad: Using realtime strategy for static data
const { data: staticConfig } = useQuery({
  queryKey: ['app-config'],
  queryFn: fetchAppConfig,
  ...realtimeQueryOptions  // Wasteful, unnecessary requests
});
```

### 2. Use Smart Invalidation

```typescript
// ✅ Good: Use provided hooks with smart invalidation
const createPage = useCreateUIStudioPage();

// Automatically invalidates related caches
await createPage.mutateAsync(newPageData);

// ❌ Bad: Manual invalidation that might miss relationships
const queryClient = useQueryClient();
await createPageManually(newPageData);
queryClient.invalidateQueries(['pages']); // Might miss related data
```

### 3. Implement Error Handling

```typescript
// ✅ Good: Proper error handling with recovery
const { data, error, refetch } = useUIStudioPage(pageId);

if (error) {
  return (
    <div>
      <p>Error loading page: {error.message}</p>
      <button onClick={() => refetch()}>Retry</button>
    </div>
  );
}

// ❌ Bad: No error handling
const { data } = useUIStudioPage(pageId);
return <div>{data.pageName}</div>; // Will crash if data is undefined
```

### 4. Optimize Memory Usage

```typescript
// ✅ Good: Use appropriate garbage collection times
const longLivedData = useQuery({
  queryKey: ['reference-data'],
  queryFn: fetchReferenceData,
  staleTime: 30 * 60 * 1000,   // 30 minutes
  gcTime: 2 * 60 * 60 * 1000,  // 2 hours - keeps data longer
});

const shortLivedData = useQuery({
  queryKey: ['temporary-data'],
  queryFn: fetchTemporaryData,
  staleTime: 60 * 1000,        // 1 minute
  gcTime: 5 * 60 * 1000,       // 5 minutes - cleans up quickly
});
```

### 5. Use Prefetching Strategically

```typescript
// ✅ Good: Prefetch related data
const { data: page } = useUIStudioPage(pageId, {
  onSuccess: (data) => {
    // Prefetch related bindings
    queryClient.prefetchQuery({
      queryKey: cacheKeys.pageBindings(data.id),
      queryFn: () => fetchPageBindings(data.id),
    });
  }
});

// ❌ Bad: Over-prefetching unused data
const { data: page } = useUIStudioPage(pageId, {
  onSuccess: (data) => {
    // Prefetching everything wastes bandwidth and memory
    queryClient.prefetchQuery(['all-pages']);
    queryClient.prefetchQuery(['all-layouts']);
    queryClient.prefetchQuery(['all-templates']);
  }
});
```

## API Reference

### Cache Strategies

#### `useCacheStrategy()`
Main hook for accessing cache strategies and management functions.

```typescript
const {
  getStrategyConfig,
  strategies,
  cacheManager,
  getPerformanceMetrics,
  logPerformanceReport,
  optimizeCache,
  quickActions
} = useCacheStrategy();
```

#### `useRealtimeCacheStrategy()`
Hook for real-time data with aggressive caching.

```typescript
const { queryOptions, invalidate, prefetch } = useRealtimeCacheStrategy();
```

#### `useBackgroundCacheStrategy()`
Hook for background refresh strategy.

```typescript
const { queryOptions, invalidate, prefetch } = useBackgroundCacheStrategy();
```

#### `useStaticCacheStrategy()`
Hook for static data with long cache times.

```typescript
const { queryOptions, invalidate, prefetch } = useStaticCacheStrategy();
```

### Cache Manager

#### `UIStudioCacheManager`
Core cache management class with advanced operations.

```typescript
const cacheManager = createCacheManager(queryClient);

// Invalidation
await cacheManager.applyInvalidation(config);
await cacheManager.invalidateAll();
cacheManager.clearAll();

// Performance
const stats = cacheManager.getCacheStats();
const health = cacheManager.getCacheHealth();
cacheManager.logCacheMetrics();

// Optimization
await cacheManager.backgroundRefresh();
await cacheManager.warmupCache(userId);
```

### Cache Keys

#### `cacheKeys`
Factory for generating consistent cache keys.

```typescript
import { cacheKeys } from '../utils/cacheManager';

// Basic keys
cacheKeys.all           // ['uistudio']
cacheKeys.pages()       // ['uistudio', 'pages']
cacheKeys.page(id)      // ['uistudio', 'pages', id]

// Query-based keys
cacheKeys.pagesByOwner(ownerId)
cacheKeys.publishedPages(query)
cacheKeys.pageBindings(pageId)

// Related data keys
cacheKeys.pageWithBindings(pageId)
cacheKeys.layoutWithPages(layoutId)
```

### Monitoring Components

#### `<CacheMonitor />`
Development component for visual cache monitoring.

```typescript
<CacheMonitor 
  detailed={boolean}
  updateInterval={number}
  position="top-left" | "top-right" | "bottom-left" | "bottom-right"
  autoHide={boolean}
/>
```

#### `<CacheHealthBadge />`
Simple health indicator component.

```typescript
<CacheHealthBadge 
  detailed={boolean}
  size="sm" | "md" | "lg"
/>
```

### Performance Metrics

```typescript
interface CachePerformanceMetrics {
  hitRate: number;        // Percentage of cache hits
  missRate: number;       // Percentage of cache misses
  staleRate: number;      // Percentage of stale queries
  memoryUsage: number;    // Number of cached queries
  avgResponseTime: number; // Average response time (if tracked)
  errorRate: number;      // Percentage of error queries
}
```

### Cache Health

```typescript
interface CacheHealth {
  status: 'healthy' | 'warning' | 'error';
  issues: string[];
  recommendations: string[];
}
```

This comprehensive cache management system provides the foundation for building high-performance, responsive UIStudio applications with intelligent data caching and optimal user experience.