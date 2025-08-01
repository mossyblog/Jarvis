# Storage API Reference

## Overview

The Storage API provides persistence for all Bento system data using the Jarvis ECS (Entity Component System) pattern. This ensures consistency with the broader Jarvis architecture.

## ECS Component Models

### BentoPageComponent

```typescript
// ECS Component for page storage
interface BentoPageComponent extends IComponent {
  // Core fields
  displayName: string;
  route: string;
  layoutId: string;
  status: string; // 'draft' | 'published' | 'archived' | 'scheduled'
  version: number;
  
  // JSON serialized fields
  securityBindings: string; // SecurityBindings as JSON
  visibilityBindings: string; // VisibilityBindings as JSON
  dataBindings?: string; // DataBindings as JSON
  metadata?: string; // Additional metadata as JSON
  tags?: string; // string[] as JSON
  
  // Scheduling
  publishAt?: Date;
  unpublishAt?: Date;
  
  // Audit fields
  createdBy: string;
  updatedBy: string;
}

// Handler for page operations
class BentoPageHandler extends ComponentHandler<BentoPageComponent> {
  async createPage(page: Partial<BentoPageComponent>): Promise<BentoPageComponent> {
    const component: BentoPageComponent = {
      ...page,
      id: Guid.newGuid(),
      ownerEntityId: this.ownerEntityId,
      lastUpdated: new Date(),
      version: 1,
      status: page.status || 'draft'
    };
    
    await this.commit(component);
    return component;
  }
  
  async updatePage(
    pageId: string, 
    updates: Partial<BentoPageComponent>
  ): Promise<BentoPageComponent> {
    const existing = await this.get(pageId);
    if (!existing) throw new Error('Page not found');
    
    const updated = {
      ...existing,
      ...updates,
      version: existing.version + 1,
      lastUpdated: new Date(),
      updatedBy: this.context.userId
    };
    
    await this.commit(updated);
    return updated;
  }
  
  async publishPage(pageId: string): Promise<void> {
    await this.updatePage(pageId, { 
      status: 'published',
      publishAt: new Date()
    });
  }
}
```

### BentoLayoutComponent

```typescript
// ECS Component for layout storage
interface BentoLayoutComponent extends IComponent {
  // Core fields
  name: string;
  description?: string;
  category?: string; // 'standard' | 'custom' | 'template'
  
  // Grid references
  desktopGridId: string;
  tabletGridId?: string;
  mobileGridId?: string;
  
  // JSON serialized fields
  settings: string; // LayoutSettings as JSON
  
  // Flags
  isDefault?: boolean;
  isTemplate?: boolean;
  
  // Preview
  thumbnail?: string; // Base64 or URL
  preview?: string; // HTML preview
}

// Handler for layout operations
class BentoLayoutHandler extends ComponentHandler<BentoLayoutComponent> {
  async createLayout(
    layout: Partial<BentoLayoutComponent>
  ): Promise<BentoLayoutComponent> {
    // Validate grid references exist
    if (!layout.desktopGridId) {
      throw new Error('Desktop grid is required');
    }
    
    const component: BentoLayoutComponent = {
      ...layout,
      id: Guid.newGuid(),
      ownerEntityId: this.ownerEntityId,
      lastUpdated: new Date(),
      settings: layout.settings || JSON.stringify({})
    };
    
    await this.commit(component);
    return component;
  }
  
  async getLayoutWithGrids(layoutId: string): Promise<{
    layout: BentoLayoutComponent;
    grids: {
      desktop: BentoGridComponent;
      tablet?: BentoGridComponent;
      mobile?: BentoGridComponent;
    };
  }> {
    const layout = await this.get(layoutId);
    if (!layout) throw new Error('Layout not found');
    
    const gridHandler = this.dataContext.for<BentoGridHandler>(
      this.ownerEntityId
    );
    
    const grids = {
      desktop: await gridHandler.get(layout.desktopGridId),
      tablet: layout.tabletGridId 
        ? await gridHandler.get(layout.tabletGridId) 
        : undefined,
      mobile: layout.mobileGridId 
        ? await gridHandler.get(layout.mobileGridId) 
        : undefined
    };
    
    return { layout, grids };
  }
}
```

### BentoGridComponent

```typescript
// ECS Component for grid storage
interface BentoGridComponent extends IComponent {
  // Core fields
  name: string;
  device: string; // 'desktop' | 'tablet' | 'mobile'
  layoutId: string; // Parent layout reference
  
  // Grid configuration
  columns: number;
  rows?: number;
  gap: number;
  rowHeight?: number;
  
  // JSON serialized fields
  components: string; // GridComponent[] as JSON
  settings: string; // GridSettings as JSON
  zones?: string; // GridZone[] as JSON
}

// Handler for grid operations
class BentoGridHandler extends ComponentHandler<BentoGridComponent> {
  async createGrid(
    grid: Partial<BentoGridComponent>
  ): Promise<BentoGridComponent> {
    const component: BentoGridComponent = {
      ...grid,
      id: Guid.newGuid(),
      ownerEntityId: this.ownerEntityId,
      lastUpdated: new Date(),
      components: grid.components || JSON.stringify([]),
      settings: grid.settings || JSON.stringify({})
    };
    
    await this.commit(component);
    return component;
  }
  
  async addComponent(
    gridId: string,
    component: GridComponent
  ): Promise<void> {
    const grid = await this.get(gridId);
    if (!grid) throw new Error('Grid not found');
    
    const components = JSON.parse(grid.components) as GridComponent[];
    
    // Validate no collisions
    if (this.detectCollisions(component, components).length > 0) {
      throw new Error('Component position causes collision');
    }
    
    components.push(component);
    
    await this.commit({
      ...grid,
      components: JSON.stringify(components),
      lastUpdated: new Date()
    });
  }
  
  private detectCollisions(
    component: GridComponent,
    existing: GridComponent[]
  ): GridComponent[] {
    return existing.filter(c => 
      c.id !== component.id &&
      this.doPositionsOverlap(c.position, component.position)
    );
  }
  
  private doPositionsOverlap(
    pos1: GridPosition,
    pos2: GridPosition
  ): boolean {
    return !(
      pos1.x + pos1.w <= pos2.x ||
      pos2.x + pos2.w <= pos1.x ||
      pos1.y + pos1.h <= pos2.y ||
      pos2.y + pos2.h <= pos1.y
    );
  }
}
```

## Storage Service

### High-Level Storage API

```typescript
// Unified storage service
class BentoStorageService {
  constructor(private dataContext: IDataContext) {}
  
  // Page operations
  async createPage(config: PageConfig): Promise<BentoPage> {
    const pageHandler = this.dataContext.for<BentoPageHandler>(
      Guid.newGuid()
    );
    
    const pageComponent = await pageHandler.createPage({
      displayName: config.displayName,
      route: config.route,
      layoutId: config.layoutId,
      securityBindings: JSON.stringify(config.bindings.security),
      visibilityBindings: JSON.stringify(config.bindings.visibility),
      dataBindings: config.bindings.data 
        ? JSON.stringify(config.bindings.data) 
        : undefined,
      metadata: config.metadata 
        ? JSON.stringify(config.metadata) 
        : undefined,
      createdBy: this.dataContext.userId,
      updatedBy: this.dataContext.userId
    });
    
    return this.componentToPage(pageComponent);
  }
  
  async getPage(pageId: string): Promise<BentoPage | null> {
    const pages = await this.dataContext.query()
      .withAll<BentoPageComponent>(p => p.id === pageId)
      .toEntityComponents();
      
    if (pages.length === 0) return null;
    
    const pageComponent = pages[0].components
      .find(c => c.componentType === 'BentoPageComponent') as BentoPageComponent;
      
    return this.componentToPage(pageComponent);
  }
  
  async listPages(filter?: PageFilter): Promise<BentoPage[]> {
    let query = this.dataContext.query()
      .withAll<BentoPageComponent>();
      
    if (filter?.status) {
      query = query.where<BentoPageComponent>(p => p.status === filter.status);
    }
    
    if (filter?.route) {
      query = query.where<BentoPageComponent>(p => 
        p.route.startsWith(filter.route!)
      );
    }
    
    const results = await query.toEntityComponents();
    
    return results.map(r => {
      const component = r.components
        .find(c => c.componentType === 'BentoPageComponent') as BentoPageComponent;
      return this.componentToPage(component);
    });
  }
  
  // Layout operations
  async createLayout(config: LayoutConfig): Promise<BentoLayout> {
    const entityId = Guid.newGuid();
    
    // Create grids first
    const gridHandler = this.dataContext.for<BentoGridHandler>(entityId);
    
    const desktopGrid = await gridHandler.createGrid({
      name: `${config.name} - Desktop`,
      device: 'desktop',
      layoutId: entityId,
      columns: 12,
      gap: 8,
      rowHeight: 100
    });
    
    // Create layout
    const layoutHandler = this.dataContext.for<BentoLayoutHandler>(entityId);
    
    const layoutComponent = await layoutHandler.createLayout({
      name: config.name,
      description: config.description,
      category: config.category,
      desktopGridId: desktopGrid.id,
      settings: JSON.stringify(config.settings || {})
    });
    
    return this.componentToLayout(layoutComponent);
  }
  
  // Helper conversions
  private componentToPage(component: BentoPageComponent): BentoPage {
    return {
      id: component.id,
      displayName: component.displayName,
      route: component.route,
      layoutId: component.layoutId,
      status: component.status as PageStatus,
      version: component.version,
      bindings: {
        security: JSON.parse(component.securityBindings),
        visibility: JSON.parse(component.visibilityBindings),
        data: component.dataBindings 
          ? JSON.parse(component.dataBindings) 
          : undefined
      },
      metadata: component.metadata 
        ? JSON.parse(component.metadata) 
        : undefined,
      tags: component.tags 
        ? JSON.parse(component.tags) 
        : undefined,
      createdAt: component.createdAt,
      updatedAt: component.lastUpdated.toISOString(),
      createdBy: component.createdBy,
      updatedBy: component.updatedBy
    };
  }
  
  private componentToLayout(component: BentoLayoutComponent): BentoLayout {
    return {
      id: component.id,
      name: component.name,
      description: component.description,
      category: component.category,
      grids: {
        desktop: component.desktopGridId,
        tablet: component.tabletGridId,
        mobile: component.mobileGridId
      },
      settings: JSON.parse(component.settings),
      isDefault: component.isDefault,
      createdAt: component.createdAt,
      updatedAt: component.lastUpdated.toISOString(),
      thumbnail: component.thumbnail,
      preview: component.preview
    };
  }
}
```

## Caching Strategy

### In-Memory Cache

```typescript
// Cache implementation for frequently accessed data
class BentoCache {
  private pageCache = new Map<string, CachedItem<BentoPage>>();
  private layoutCache = new Map<string, CachedItem<BentoLayout>>();
  private gridCache = new Map<string, CachedItem<BentoGrid>>();
  
  // Cache configuration
  private readonly TTL = 5 * 60 * 1000; // 5 minutes
  private readonly MAX_SIZE = 100;
  
  // Page caching
  setPage(page: BentoPage): void {
    this.evictIfNeeded(this.pageCache);
    
    this.pageCache.set(page.id, {
      data: page,
      timestamp: Date.now(),
      hits: 0
    });
  }
  
  getPage(pageId: string): BentoPage | null {
    const cached = this.pageCache.get(pageId);
    
    if (!cached) return null;
    
    if (this.isExpired(cached)) {
      this.pageCache.delete(pageId);
      return null;
    }
    
    cached.hits++;
    return cached.data;
  }
  
  invalidatePage(pageId: string): void {
    this.pageCache.delete(pageId);
  }
  
  // Helper methods
  private isExpired(item: CachedItem<any>): boolean {
    return Date.now() - item.timestamp > this.TTL;
  }
  
  private evictIfNeeded<T>(cache: Map<string, CachedItem<T>>): void {
    if (cache.size < this.MAX_SIZE) return;
    
    // LRU eviction
    const sorted = Array.from(cache.entries())
      .sort((a, b) => a[1].hits - b[1].hits);
      
    const toEvict = sorted.slice(0, Math.floor(cache.size * 0.2));
    toEvict.forEach(([key]) => cache.delete(key));
  }
}

interface CachedItem<T> {
  data: T;
  timestamp: number;
  hits: number;
}
```

### Local Storage Cache

```typescript
// Browser storage for offline support
class BentoLocalStorage {
  private readonly PREFIX = 'bento:';
  
  // Save to local storage
  async savePage(page: BentoPage): Promise<void> {
    const key = `${this.PREFIX}page:${page.id}`;
    const data = {
      page,
      timestamp: Date.now(),
      version: page.version
    };
    
    try {
      localStorage.setItem(key, JSON.stringify(data));
    } catch (e) {
      // Handle quota exceeded
      this.cleanupOldEntries();
      localStorage.setItem(key, JSON.stringify(data));
    }
  }
  
  // Load from local storage
  async loadPage(pageId: string): Promise<BentoPage | null> {
    const key = `${this.PREFIX}page:${pageId}`;
    const stored = localStorage.getItem(key);
    
    if (!stored) return null;
    
    try {
      const data = JSON.parse(stored);
      
      // Check if data is fresh (24 hours)
      if (Date.now() - data.timestamp > 24 * 60 * 60 * 1000) {
        localStorage.removeItem(key);
        return null;
      }
      
      return data.page;
    } catch {
      localStorage.removeItem(key);
      return null;
    }
  }
  
  // Sync local changes
  async syncPendingChanges(): Promise<void> {
    const pending = this.getPendingChanges();
    
    for (const change of pending) {
      try {
        await this.syncChange(change);
        this.removePendingChange(change.id);
      } catch (error) {
        console.error('Failed to sync change:', error);
      }
    }
  }
  
  private getPendingChanges(): PendingChange[] {
    const changes: PendingChange[] = [];
    
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(`${this.PREFIX}pending:`)) {
        const data = localStorage.getItem(key);
        if (data) {
          changes.push(JSON.parse(data));
        }
      }
    }
    
    return changes.sort((a, b) => a.timestamp - b.timestamp);
  }
  
  private cleanupOldEntries(): void {
    const entries: Array<[string, number]> = [];
    
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(this.PREFIX)) {
        try {
          const data = JSON.parse(localStorage.getItem(key)!);
          entries.push([key, data.timestamp || 0]);
        } catch {
          // Remove corrupted entries
          localStorage.removeItem(key);
        }
      }
    }
    
    // Remove oldest 20%
    entries
      .sort((a, b) => a[1] - b[1])
      .slice(0, Math.floor(entries.length * 0.2))
      .forEach(([key]) => localStorage.removeItem(key));
  }
}

interface PendingChange {
  id: string;
  type: 'create' | 'update' | 'delete';
  entity: 'page' | 'layout' | 'grid';
  data: any;
  timestamp: number;
}
```

## Transactions

### Transactional Operations

```typescript
// Ensure consistency across multiple operations
class BentoTransactionManager {
  async createPageWithLayout(
    pageConfig: PageConfig,
    layoutConfig: LayoutConfig
  ): Promise<{ page: BentoPage; layout: BentoLayout }> {
    return await this.dataContext.executeInTransaction(async (ctx) => {
      const storage = new BentoStorageService(ctx);
      
      // Create layout first
      const layout = await storage.createLayout(layoutConfig);
      
      // Create page with layout reference
      const page = await storage.createPage({
        ...pageConfig,
        layoutId: layout.id
      });
      
      return { page, layout };
    });
  }
  
  async duplicatePage(
    sourcePageId: string,
    newName: string,
    newRoute: string
  ): Promise<BentoPage> {
    return await this.dataContext.executeInTransaction(async (ctx) => {
      const storage = new BentoStorageService(ctx);
      
      // Load source page
      const sourcePage = await storage.getPage(sourcePageId);
      if (!sourcePage) throw new Error('Source page not found');
      
      // Load layout and grids
      const layout = await storage.getLayout(sourcePage.layoutId);
      const grids = await storage.getLayoutGrids(sourcePage.layoutId);
      
      // Create new layout
      const newLayout = await storage.createLayout({
        ...layout,
        name: `${newName} Layout`
      });
      
      // Copy grids
      for (const [device, grid] of Object.entries(grids)) {
        await storage.createGrid({
          ...grid,
          layoutId: newLayout.id,
          name: `${newName} - ${device}`
        });
      }
      
      // Create new page
      const newPage = await storage.createPage({
        ...sourcePage,
        displayName: newName,
        route: newRoute,
        layoutId: newLayout.id,
        status: PageStatus.Draft,
        version: 1
      });
      
      return newPage;
    });
  }
}
```

## Backup and Recovery

### Export/Import

```typescript
// Export functionality
class BentoExporter {
  async exportPage(pageId: string): Promise<PageExport> {
    const storage = new BentoStorageService(this.dataContext);
    
    // Load all page data
    const page = await storage.getPage(pageId);
    if (!page) throw new Error('Page not found');
    
    const layout = await storage.getLayout(page.layoutId);
    const grids = await storage.getLayoutGrids(page.layoutId);
    
    // Collect component types
    const componentTypes = new Set<string>();
    Object.values(grids).forEach(grid => {
      grid.components.forEach(c => {
        componentTypes.add(c.componentType);
      });
    });
    
    return {
      version: '1.0',
      timestamp: new Date().toISOString(),
      page,
      layout,
      grids: Object.values(grids),
      components: Array.from(componentTypes).map(type => ({
        type,
        version: '1.0',
        source: 'registry'
      })),
      exportedBy: this.dataContext.userId,
      checksum: this.calculateChecksum({ page, layout, grids })
    };
  }
  
  async importPage(data: PageExport): Promise<BentoPage> {
    // Validate checksum
    if (data.checksum !== this.calculateChecksum(data)) {
      throw new Error('Invalid export data');
    }
    
    return await this.dataContext.executeInTransaction(async (ctx) => {
      const storage = new BentoStorageService(ctx);
      
      // Check for conflicts
      const existingPage = await storage.getPageByRoute(data.page.route);
      if (existingPage) {
        throw new Error(`Page with route ${data.page.route} already exists`);
      }
      
      // Import layout
      const layout = await storage.createLayout({
        ...data.layout,
        id: undefined // Generate new ID
      });
      
      // Import grids
      for (const grid of data.grids) {
        await storage.createGrid({
          ...grid,
          id: undefined,
          layoutId: layout.id
        });
      }
      
      // Import page
      const page = await storage.createPage({
        ...data.page,
        id: undefined,
        layoutId: layout.id,
        status: PageStatus.Draft,
        version: 1
      });
      
      return page;
    });
  }
  
  private calculateChecksum(data: any): string {
    const crypto = require('crypto');
    const hash = crypto.createHash('sha256');
    hash.update(JSON.stringify(data));
    return hash.digest('hex');
  }
}
```

## Migration Support

### Schema Migrations

```typescript
// Handle storage schema changes
class BentoMigrationManager {
  private migrations: Migration[] = [
    {
      version: 1,
      up: async (ctx) => {
        // Initial schema
      },
      down: async (ctx) => {
        // Rollback
      }
    },
    {
      version: 2,
      up: async (ctx) => {
        // Add zones to grid
        const grids = await ctx.query()
          .withAll<BentoGridComponent>()
          .toEntityComponents();
          
        for (const entity of grids) {
          const grid = entity.components[0] as BentoGridComponent;
          if (!grid.zones) {
            grid.zones = JSON.stringify([]);
            await ctx.commit(grid);
          }
        }
      },
      down: async (ctx) => {
        // Remove zones
      }
    }
  ];
  
  async migrate(): Promise<void> {
    const currentVersion = await this.getCurrentVersion();
    const targetVersion = Math.max(...this.migrations.map(m => m.version));
    
    if (currentVersion >= targetVersion) {
      console.log('Already at latest version');
      return;
    }
    
    for (let v = currentVersion + 1; v <= targetVersion; v++) {
      const migration = this.migrations.find(m => m.version === v);
      if (migration) {
        console.log(`Running migration ${v}...`);
        await migration.up(this.dataContext);
        await this.setCurrentVersion(v);
      }
    }
  }
}

interface Migration {
  version: number;
  up: (ctx: IDataContext) => Promise<void>;
  down: (ctx: IDataContext) => Promise<void>;
}
```

## Next Steps

1. Review [Architecture](./02-architecture.md) for system design
2. Check [Security Model](./12-security-model.md) for access control
3. See [Performance Guide](./13-performance-guide.md) for optimization
4. Explore [Testing Strategy](./08-testing-strategy.md) for storage testing