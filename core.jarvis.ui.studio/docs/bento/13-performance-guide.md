# Performance Guide

## Overview

This guide provides optimization strategies and best practices for ensuring the Bento Grid System performs efficiently at scale.

## Performance Goals

```
┌─────────────────────────────────────────────────────────────────┐
│                     Performance Targets                           │
├─────────────────────────────────────────────────────────────────┤
│  Initial Page Load:          < 1000ms                            │
│  Grid Render (50 components): < 100ms                            │
│  Component Drag:             < 16ms per frame (60 FPS)           │
│  API Response:               < 200ms                             │
│  Memory Usage:               < 50MB baseline                     │
│  Bundle Size:                < 500KB (gzipped)                   │
└─────────────────────────────────────────────────────────────────┘
```

## Component Optimization

### 1. React.memo and Memoization

```typescript
// Optimize component re-renders
export const OptimizedComponent = memo<ComponentProps>(
  ({ data, size, onAction }) => {
    // Expensive calculations memoized
    const processedData = useMemo(() => {
      return expensiveDataProcessing(data);
    }, [data]);
    
    // Callbacks memoized to prevent child re-renders
    const handleClick = useCallback(() => {
      onAction?.({ type: 'click', data: processedData });
    }, [onAction, processedData]);
    
    return (
      <div onClick={handleClick}>
        {/* Component content */}
      </div>
    );
  },
  // Custom comparison function for deep prop checking
  (prevProps, nextProps) => {
    return (
      prevProps.data === nextProps.data &&
      prevProps.size.w === nextProps.size.w &&
      prevProps.size.h === nextProps.size.h
    );
  }
);
```

### 2. Lazy Loading Components

```typescript
// Dynamic imports for heavy components
const LazyChart = lazy(() => 
  import(/* webpackChunkName: "chart" */ './components/Chart')
);

const LazyDataTable = lazy(() => 
  import(/* webpackChunkName: "datatable" */ './components/DataTable')
);

// Component registry with lazy loading
const componentRegistry = {
  Chart: {
    component: LazyChart,
    displayName: 'Chart',
    loading: ChartSkeleton, // Placeholder while loading
    preload: () => import('./components/Chart') // Optional preloading
  }
};

// Preload components when idle
const preloadComponents = () => {
  if ('requestIdleCallback' in window) {
    requestIdleCallback(() => {
      Object.values(componentRegistry).forEach(config => {
        config.preload?.();
      });
    });
  }
};
```

### 3. Virtual Scrolling

```typescript
// Virtual grid for large number of components
import { VariableSizeGrid } from 'react-window';

const VirtualBentoGrid: React.FC<{
  components: GridComponent[];
  columns: number;
}> = ({ components, columns }) => {
  // Calculate row assignments
  const rows = useMemo(() => {
    const rowMap = new Map<number, GridComponent[]>();
    
    components.forEach(component => {
      const row = component.position.y;
      if (!rowMap.has(row)) {
        rowMap.set(row, []);
      }
      rowMap.get(row)!.push(component);
    });
    
    return Array.from(rowMap.entries())
      .sort(([a], [b]) => a - b);
  }, [components]);
  
  const rowHeight = useCallback((index: number) => {
    const [, rowComponents] = rows[index];
    const maxHeight = Math.max(
      ...rowComponents.map(c => c.position.h * 100)
    );
    return maxHeight;
  }, [rows]);
  
  const Cell = ({ rowIndex, columnIndex, style }) => {
    const [, rowComponents] = rows[rowIndex];
    const component = rowComponents.find(
      c => c.position.x === columnIndex
    );
    
    if (!component) return null;
    
    return (
      <div style={style}>
        <ComponentRenderer component={component} />
      </div>
    );
  };
  
  return (
    <VariableSizeGrid
      columnCount={columns}
      columnWidth={() => 100} // Fixed column width
      height={800} // Viewport height
      rowCount={rows.length}
      rowHeight={rowHeight}
      width={1200} // Viewport width
    >
      {Cell}
    </VariableSizeGrid>
  );
};
```

## Grid Performance

### 1. Efficient Collision Detection

```typescript
// Spatial indexing for fast collision detection
class SpatialIndex {
  private grid: Map<string, Set<string>> = new Map();
  private cellSize: number;
  
  constructor(cellSize: number = 100) {
    this.cellSize = cellSize;
  }
  
  // Add component to spatial index
  add(component: GridComponent): void {
    const cells = this.getCellsForComponent(component);
    cells.forEach(cell => {
      if (!this.grid.has(cell)) {
        this.grid.set(cell, new Set());
      }
      this.grid.get(cell)!.add(component.id);
    });
  }
  
  // Find potential collisions
  getPotentialCollisions(component: GridComponent): string[] {
    const cells = this.getCellsForComponent(component);
    const potentialIds = new Set<string>();
    
    cells.forEach(cell => {
      const ids = this.grid.get(cell);
      if (ids) {
        ids.forEach(id => {
          if (id !== component.id) {
            potentialIds.add(id);
          }
        });
      }
    });
    
    return Array.from(potentialIds);
  }
  
  private getCellsForComponent(component: GridComponent): string[] {
    const cells: string[] = [];
    const { x, y, w, h } = component.position;
    
    for (let cx = x; cx < x + w; cx++) {
      for (let cy = y; cy < y + h; cy++) {
        cells.push(`${cx},${cy}`);
      }
    }
    
    return cells;
  }
}

// Usage in grid manager
class PerformantGridManager extends GridManager {
  private spatialIndex = new SpatialIndex();
  
  detectCollisions(component: GridComponent): GridComponent[] {
    // Get potential collisions from spatial index
    const potentialIds = this.spatialIndex.getPotentialCollisions(component);
    
    // Only check actual collisions for nearby components
    return potentialIds
      .map(id => this.getComponent(id))
      .filter(other => 
        other && this.doPositionsOverlap(component.position, other.position)
      ) as GridComponent[];
  }
}
```

### 2. Batch Updates

```typescript
// Batch DOM updates
class BatchUpdateManager {
  private pendingUpdates: Map<string, () => void> = new Map();
  private rafId?: number;
  
  scheduleUpdate(id: string, update: () => void): void {
    this.pendingUpdates.set(id, update);
    
    if (!this.rafId) {
      this.rafId = requestAnimationFrame(() => {
        this.flushUpdates();
      });
    }
  }
  
  private flushUpdates(): void {
    // Execute all pending updates in one frame
    this.pendingUpdates.forEach(update => update());
    this.pendingUpdates.clear();
    this.rafId = undefined;
  }
}

// Usage in grid
const updateManager = new BatchUpdateManager();

const moveComponent = (id: string, position: GridPosition) => {
  updateManager.scheduleUpdate(id, () => {
    const element = document.getElementById(`component-${id}`);
    if (element) {
      element.style.transform = `translate(${position.x}px, ${position.y}px)`;
    }
  });
};
```

### 3. CSS-Based Animations

```typescript
// Use CSS transitions for smooth animations
const gridStyles = css`
  .bento-component {
    transition: transform 200ms ease-out;
    will-change: transform;
    
    &.dragging {
      transition: none;
      z-index: 1000;
    }
    
    &.resizing {
      transition: width 200ms ease-out, height 200ms ease-out;
    }
  }
  
  /* GPU acceleration for transforms */
  .bento-grid {
    transform: translateZ(0);
    backface-visibility: hidden;
  }
`;

// Component position updates
const updateComponentPosition = (
  component: GridComponent,
  immediate: boolean = false
) => {
  const element = componentRefs.current[component.id];
  if (!element) return;
  
  if (immediate) {
    element.classList.add('dragging');
  } else {
    element.classList.remove('dragging');
  }
  
  // Use transform for positioning (GPU accelerated)
  const x = component.position.x * (columnWidth + gap);
  const y = component.position.y * (rowHeight + gap);
  element.style.transform = `translate3d(${x}px, ${y}px, 0)`;
};
```

## Data Loading Optimization

### 1. Parallel Data Fetching

```typescript
// Fetch component data in parallel
class ParallelDataLoader {
  async loadPageData(page: BentoPage): Promise<ComponentDataMap> {
    const layout = await this.getLayout(page.layoutId);
    const grid = await this.getGrid(layout.grids.desktop);
    
    // Group components by data source
    const dataGroups = this.groupByDataSource(grid.components);
    
    // Fetch all data sources in parallel
    const dataPromises = Object.entries(dataGroups).map(
      async ([source, components]) => {
        const data = await this.fetchDataSource(source);
        return { source, data, components };
      }
    );
    
    const results = await Promise.all(dataPromises);
    
    // Map data to components
    const componentData = new Map<string, any>();
    results.forEach(({ data, components }) => {
      components.forEach(component => {
        const extracted = this.extractComponentData(
          data,
          component.bindings?.dataPath
        );
        componentData.set(component.id, extracted);
      });
    });
    
    return componentData;
  }
  
  private groupByDataSource(
    components: GridComponent[]
  ): Record<string, GridComponent[]> {
    return components.reduce((groups, component) => {
      const source = component.bindings?.dataSource || 'static';
      if (!groups[source]) groups[source] = [];
      groups[source].push(component);
      return groups;
    }, {} as Record<string, GridComponent[]>);
  }
}
```

### 2. Data Caching Strategy

```typescript
// Multi-layer caching
class DataCacheManager {
  private memoryCache = new Map<string, CacheEntry>();
  private cacheDB?: IDBDatabase;
  
  async get(key: string): Promise<any | null> {
    // L1: Memory cache
    const memoryEntry = this.memoryCache.get(key);
    if (memoryEntry && !this.isExpired(memoryEntry)) {
      memoryEntry.hits++;
      return memoryEntry.data;
    }
    
    // L2: IndexedDB cache
    const dbEntry = await this.getFromDB(key);
    if (dbEntry && !this.isExpired(dbEntry)) {
      // Promote to memory cache
      this.memoryCache.set(key, dbEntry);
      return dbEntry.data;
    }
    
    return null;
  }
  
  async set(
    key: string, 
    data: any, 
    ttl: number = 300000 // 5 minutes default
  ): Promise<void> {
    const entry: CacheEntry = {
      data,
      timestamp: Date.now(),
      ttl,
      hits: 0
    };
    
    // Save to both caches
    this.memoryCache.set(key, entry);
    await this.saveToB(key, entry);
    
    // Manage cache size
    this.evictIfNeeded();
  }
  
  private evictIfNeeded(): void {
    const MAX_MEMORY_ENTRIES = 100;
    
    if (this.memoryCache.size > MAX_MEMORY_ENTRIES) {
      // LRU eviction
      const entries = Array.from(this.memoryCache.entries())
        .sort((a, b) => a[1].hits - b[1].hits);
        
      // Remove least used 20%
      const toRemove = Math.floor(entries.length * 0.2);
      entries.slice(0, toRemove).forEach(([key]) => {
        this.memoryCache.delete(key);
      });
    }
  }
}
```

### 3. Request Deduplication

```typescript
// Prevent duplicate requests
class RequestDeduplicator {
  private pending = new Map<string, Promise<any>>();
  
  async fetch<T>(
    key: string,
    fetcher: () => Promise<T>
  ): Promise<T> {
    // Check if request is already pending
    const existing = this.pending.get(key);
    if (existing) {
      return existing as Promise<T>;
    }
    
    // Create new request
    const promise = fetcher()
      .then(result => {
        this.pending.delete(key);
        return result;
      })
      .catch(error => {
        this.pending.delete(key);
        throw error;
      });
    
    this.pending.set(key, promise);
    return promise;
  }
}

// Usage
const deduplicator = new RequestDeduplicator();

const fetchComponentData = async (componentId: string) => {
  return deduplicator.fetch(
    `component:${componentId}`,
    async () => {
      const response = await fetch(`/api/components/${componentId}/data`);
      return response.json();
    }
  );
};
```

## Bundle Optimization

### 1. Code Splitting

```typescript
// Route-based code splitting
const routes = [
  {
    path: '/admin/pages',
    component: lazy(() => 
      import(/* webpackChunkName: "page-builder" */ './pages/PageBuilder')
    )
  },
  {
    path: '/admin/pages/:id/edit',
    component: lazy(() => 
      import(/* webpackChunkName: "grid-editor" */ './pages/GridEditor')
    )
  },
  {
    path: '/*',
    component: lazy(() => 
      import(/* webpackChunkName: "page-renderer" */ './pages/PageRenderer')
    )
  }
];

// Component-based splitting
const componentModules = {
  MetricCard: () => import('./components/MetricCard'),
  DataTable: () => import('./components/DataTable'),
  Chart: () => import('./components/Chart'),
  // ... more components
};
```

### 2. Tree Shaking

```typescript
// Modular imports
// ❌ Bad - imports entire library
import * as Icons from 'lucide-react';

// ✅ Good - imports only needed icons
import { Edit, Trash, Plus } from 'lucide-react';

// Component registry with tree shaking
export { MetricCard } from './components/MetricCard';
export { DataTable } from './components/DataTable';
// Only imported components are bundled
```

### 3. Bundle Analysis

```json
// package.json scripts
{
  "scripts": {
    "analyze": "ANALYZE=true vite build",
    "bundle-report": "vite-bundle-visualizer"
  }
}
```

```typescript
// vite.config.ts
export default defineConfig({
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor': ['react', 'react-dom'],
          'ui': ['@/components/ui'],
          'bento': ['@/components/bento'],
          'utils': ['@/lib/utils']
        }
      }
    }
  }
});
```

## Runtime Performance

### 1. Web Workers for Heavy Operations

```typescript
// Grid calculation worker
// worker/gridCalculator.ts
self.addEventListener('message', (event) => {
  const { type, data } = event.data;
  
  switch (type) {
    case 'PACK_COMPONENTS':
      const packed = packComponents(data.components, data.config);
      self.postMessage({ type: 'PACKED', data: packed });
      break;
      
    case 'DETECT_COLLISIONS':
      const collisions = detectAllCollisions(data.components);
      self.postMessage({ type: 'COLLISIONS', data: collisions });
      break;
  }
});

// Main thread usage
const gridWorker = new Worker('/workers/gridCalculator.js');

const packComponentsAsync = (
  components: GridComponent[]
): Promise<GridComponent[]> => {
  return new Promise((resolve) => {
    gridWorker.postMessage({
      type: 'PACK_COMPONENTS',
      data: { components, config: gridConfig }
    });
    
    gridWorker.addEventListener('message', (event) => {
      if (event.data.type === 'PACKED') {
        resolve(event.data.data);
      }
    }, { once: true });
  });
};
```

### 2. Performance Monitoring

```typescript
// Performance metrics collection
class PerformanceMonitor {
  private metrics: Map<string, PerformanceMetric[]> = new Map();
  
  measure<T>(name: string, fn: () => T): T {
    const start = performance.now();
    
    try {
      const result = fn();
      const duration = performance.now() - start;
      
      this.recordMetric(name, {
        duration,
        timestamp: Date.now(),
        success: true
      });
      
      return result;
    } catch (error) {
      const duration = performance.now() - start;
      
      this.recordMetric(name, {
        duration,
        timestamp: Date.now(),
        success: false,
        error: error.message
      });
      
      throw error;
    }
  }
  
  async measureAsync<T>(
    name: string, 
    fn: () => Promise<T>
  ): Promise<T> {
    const start = performance.now();
    
    try {
      const result = await fn();
      const duration = performance.now() - start;
      
      this.recordMetric(name, {
        duration,
        timestamp: Date.now(),
        success: true
      });
      
      return result;
    } catch (error) {
      const duration = performance.now() - start;
      
      this.recordMetric(name, {
        duration,
        timestamp: Date.now(),
        success: false,
        error: error.message
      });
      
      throw error;
    }
  }
  
  getMetrics(name: string): PerformanceStats {
    const metrics = this.metrics.get(name) || [];
    
    if (metrics.length === 0) {
      return { count: 0, avg: 0, min: 0, max: 0, p95: 0 };
    }
    
    const durations = metrics.map(m => m.duration).sort((a, b) => a - b);
    
    return {
      count: metrics.length,
      avg: durations.reduce((a, b) => a + b) / durations.length,
      min: durations[0],
      max: durations[durations.length - 1],
      p95: durations[Math.floor(durations.length * 0.95)]
    };
  }
}

// Usage
const monitor = new PerformanceMonitor();

const renderGrid = monitor.measure('grid:render', () => {
  return <BentoGrid components={components} />;
});

const data = await monitor.measureAsync('data:fetch', async () => {
  return await fetchPageData(pageId);
});
```

### 3. Memory Management

```typescript
// Memory leak prevention
class ComponentManager {
  private components = new Map<string, ComponentInstance>();
  private observers = new WeakMap<Element, ResizeObserver>();
  
  mountComponent(id: string, element: Element): void {
    const instance = {
      id,
      element,
      cleanup: []
    };
    
    // Set up resize observer
    const resizeObserver = new ResizeObserver((entries) => {
      // Handle resize
    });
    resizeObserver.observe(element);
    this.observers.set(element, resizeObserver);
    
    // Track for cleanup
    instance.cleanup.push(() => {
      resizeObserver.disconnect();
      this.observers.delete(element);
    });
    
    this.components.set(id, instance);
  }
  
  unmountComponent(id: string): void {
    const instance = this.components.get(id);
    if (!instance) return;
    
    // Clean up all resources
    instance.cleanup.forEach(fn => fn());
    
    // Remove references
    this.components.delete(id);
  }
  
  // Clean up all components
  cleanup(): void {
    this.components.forEach((_, id) => {
      this.unmountComponent(id);
    });
  }
}
```

## Performance Checklist

### Development
- [ ] Components wrapped in React.memo
- [ ] Heavy components lazy loaded
- [ ] useMemo for expensive calculations
- [ ] useCallback for event handlers
- [ ] Virtual scrolling for large lists
- [ ] Batch DOM updates
- [ ] Debounce user inputs

### Build
- [ ] Code splitting configured
- [ ] Tree shaking enabled
- [ ] Bundle size < 500KB
- [ ] Source maps in production
- [ ] Compression enabled

### Runtime
- [ ] Initial load < 1s
- [ ] 60 FPS during interactions
- [ ] Memory usage stable
- [ ] No memory leaks
- [ ] Performance monitoring active

### Data
- [ ] Parallel data fetching
- [ ] Request deduplication
- [ ] Appropriate caching
- [ ] Optimistic updates
- [ ] Pagination implemented

## Next Steps

1. Review [Architecture](./02-architecture.md) for performance design
2. Check [Testing Strategy](./08-testing-strategy.md) for performance tests
3. See [Implementation Plan](./07-implementation-plan.md) for optimization tasks
4. Explore [Component API](./09-component-api.md) for optimization patterns