/**
 * Component Registry Service
 * 
 * Provides cached access to UIStudio component registry with intelligent
 * caching, search capabilities, and real-time updates.
 */

import { graphqlService } from './graphql/graphqlService';

// ============================================================================
// Types
// ============================================================================

export interface ComponentRegistryComponent {
  id: string;
  name: string;
  description: string;
  category: string;
  icon: string;
  tags: string[];
  isPremium?: boolean;
  isNew?: boolean;
  isFromRegistry: boolean;
  usageCount?: number;
  lastUsed?: Date;
  minSize: { w: number; h: number };
  maxSize: { w: number; h: number };
  supportedDevices: string[];
  requiredDataSources?: string[];
  configurationSchema?: Record<string, unknown>;
}

export interface ComponentRegistryOptions {
  category?: string;
  device?: string;
  search?: string;
  enableCache?: boolean;
  cacheTimeout?: number;
}

export interface ComponentSearchOptions {
  category?: string;
  tags?: string[];
  limit?: number;
  enableCache?: boolean;
}

// ============================================================================
// Cache Implementation
// ============================================================================

interface CacheEntry<T> {
  data: T;
  timestamp: number;
  key: string;
}

class ComponentRegistryCache {
  private cache = new Map<string, CacheEntry<unknown>>();
  private defaultTimeout = 5 * 60 * 1000; // 5 minutes

  private generateKey(operation: string, params: Record<string, unknown>): string {
    const sortedParams = Object.keys(params)
      .sort()
      .reduce((result, key) => {
        result[key] = params[key];
        return result;
      }, {} as Record<string, unknown>);
    
    return `${operation}:${JSON.stringify(sortedParams)}`;
  }

  get<T>(operation: string, params: Record<string, unknown>, timeout?: number): T | null {
    const key = this.generateKey(operation, params);
    const entry = this.cache.get(key);
    
    if (!entry) {
      return null;
    }
    
    const maxAge = timeout || this.defaultTimeout;
    const isExpired = Date.now() - entry.timestamp > maxAge;
    
    if (isExpired) {
      this.cache.delete(key);
      return null;
    }
    
    return entry.data as T;
  }

  set<T>(operation: string, params: Record<string, unknown>, data: T): void {
    const key = this.generateKey(operation, params);
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      key
    });
  }

  invalidate(pattern?: string): void {
    if (!pattern) {
      this.cache.clear();
      return;
    }
    
    for (const [key] of this.cache) {
      if (key.includes(pattern)) {
        this.cache.delete(key);
      }
    }
  }

  getStats(): { size: number; keys: string[] } {
    return {
      size: this.cache.size,
      keys: Array.from(this.cache.keys())
    };
  }
}

// ============================================================================
// Component Registry Service
// ============================================================================

export class ComponentRegistryService {
  private cache = new ComponentRegistryCache();
  private subscribers = new Set<(components: ComponentRegistryComponent[]) => void>();
  private lastFetchTime = 0;
  private isLiveUpdatesEnabled = false;
  private refreshInterval: NodeJS.Timeout | null = null;

  constructor() {
    // Start with live updates disabled by default
    // They can be enabled via enableLiveUpdates()
  }

  /**
   * Get components from the registry with caching
   */
  async getComponents(options: ComponentRegistryOptions = {}): Promise<ComponentRegistryComponent[]> {
    const { enableCache = true, cacheTimeout, ...apiOptions } = options;
    
    // Check cache first
    if (enableCache) {
      const cached = this.cache.get<ComponentRegistryComponent[]>('getComponents', apiOptions, cacheTimeout);
      if (cached) {
        return cached;
      }
    }

    try {
      // Fetch from GraphQL service
      const rawComponents = await graphqlService.getComponentRegistry(apiOptions);
      
      // Transform to typed components
      const components = this.transformComponents(rawComponents);
      
      // Cache the result
      if (enableCache) {
        this.cache.set('getComponents', apiOptions, components);
      }
      
      // Update last fetch time
      this.lastFetchTime = Date.now();
      
      // Notify subscribers
      this.notifySubscribers(components);
      
      return components;
    } catch (error) {
      console.error('Failed to get components from registry:', error);
      
      // Try to return cached data even if expired
      const staleCache = this.cache.get<ComponentRegistryComponent[]>('getComponents', apiOptions, Infinity);
      return staleCache || [];
    }
  }

  /**
   * Search components with caching
   */
  async searchComponents(searchQuery: string, options: ComponentSearchOptions = {}): Promise<ComponentRegistryComponent[]> {
    const { enableCache = true, ...searchOptions } = options;
    const searchParams = { searchQuery, ...searchOptions };
    
    // Check cache first
    if (enableCache) {
      const cached = this.cache.get<ComponentRegistryComponent[]>('searchComponents', searchParams);
      if (cached) {
        return cached;
      }
    }

    try {
      // Fetch from GraphQL service
      const rawComponents = await graphqlService.searchComponentRegistry(searchQuery, searchOptions);
      
      // Transform to typed components
      const components = this.transformComponents(rawComponents);
      
      // Cache the result
      if (enableCache) {
        this.cache.set('searchComponents', searchParams, components);
      }
      
      return components;
    } catch (error) {
      console.error('Failed to search components in registry:', error);
      
      // Try to return cached data even if expired
      const staleCache = this.cache.get<ComponentRegistryComponent[]>('searchComponents', searchParams, Infinity);
      return staleCache || [];
    }
  }

  /**
   * Get metadata for a specific component
   */
  async getComponentMetadata(componentType: string, enableCache = true): Promise<ComponentRegistryComponent | null> {
    const params = { componentType };
    
    // Check cache first
    if (enableCache) {
      const cached = this.cache.get<ComponentRegistryComponent | null>('getComponentMetadata', params);
      if (cached !== undefined) {
        return cached;
      }
    }

    try {
      // Fetch from GraphQL service
      const rawComponent = await graphqlService.getComponentMetadata(componentType);
      
      // Transform to typed component
      const component = rawComponent ? this.transformComponent(rawComponent) : null;
      
      // Cache the result
      if (enableCache) {
        this.cache.set('getComponentMetadata', params, component);
      }
      
      return component;
    } catch (error) {
      console.error('Failed to get component metadata:', error);
      
      // Try to return cached data even if expired
      const staleCache = this.cache.get<ComponentRegistryComponent | null>('getComponentMetadata', params, Infinity);
      return staleCache || null;
    }
  }

  /**
   * Enable live updates with automatic refresh
   */
  enableLiveUpdates(intervalMs = 30000): void {
    this.isLiveUpdatesEnabled = true;
    
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
    
    this.refreshInterval = setInterval(async () => {
      try {
        // Refresh the most common query (all components)
        await this.getComponents({ enableCache: false });
      } catch (error) {
        console.error('Live update failed:', error);
      }
    }, intervalMs);
  }

  /**
   * Disable live updates
   */
  disableLiveUpdates(): void {
    this.isLiveUpdatesEnabled = false;
    
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }

  /**
   * Subscribe to component updates
   */
  subscribe(callback: (components: ComponentRegistryComponent[]) => void): () => void {
    this.subscribers.add(callback);
    
    return () => {
      this.subscribers.delete(callback);
    };
  }

  /**
   * Invalidate cache
   */
  invalidateCache(pattern?: string): void {
    this.cache.invalidate(pattern);
  }

  /**
   * Get cache statistics
   */
  getCacheStats(): { size: number; keys: string[]; lastFetchTime: number; isLiveUpdatesEnabled: boolean } {
    return {
      ...this.cache.getStats(),
      lastFetchTime: this.lastFetchTime,
      isLiveUpdatesEnabled: this.isLiveUpdatesEnabled
    };
  }

  /**
   * Force refresh all cached data
   */
  async refreshAll(): Promise<void> {
    this.invalidateCache();
    await this.getComponents({ enableCache: false });
  }

  /**
   * Transform raw API components to typed components
   */
  private transformComponents(rawComponents: unknown[]): ComponentRegistryComponent[] {
    return rawComponents.map(comp => this.transformComponent(comp)).filter(Boolean) as ComponentRegistryComponent[];
  }

  /**
   * Transform a single raw component to typed component
   */
  private transformComponent(rawComponent: unknown): ComponentRegistryComponent | null {
    try {
      const comp = rawComponent as Record<string, unknown>;
      
      return {
        id: String(comp.id || comp.type || 'unknown'),
        name: String(comp.name || 'Unknown Component'),
        description: String(comp.description || ''),
        category: String(comp.category || 'Custom'),
        icon: String(comp.icon || 'Package2'),
        tags: Array.isArray(comp.tags) ? comp.tags as string[] : [],
        isPremium: Boolean(comp.isPremium),
        isNew: Boolean(comp.isNew),
        isFromRegistry: Boolean(comp.isFromRegistry),
        usageCount: Number(comp.usageCount) || 0,
        lastUsed: comp.lastUsed ? new Date(comp.lastUsed as string) : undefined,
        minSize: (comp.minSize as { w: number; h: number }) || { w: 2, h: 2 },
        maxSize: (comp.maxSize as { w: number; h: number }) || { w: 6, h: 6 },
        supportedDevices: Array.isArray(comp.supportedDevices) ? comp.supportedDevices as string[] : ['desktop'],
        requiredDataSources: Array.isArray(comp.requiredDataSources) ? comp.requiredDataSources as string[] : [],
        configurationSchema: (comp.configurationSchema as Record<string, unknown>) || {}
      };
    } catch (error) {
      console.error('Failed to transform component:', error, rawComponent);
      return null;
    }
  }

  /**
   * Notify all subscribers of component updates
   */
  private notifySubscribers(components: ComponentRegistryComponent[]): void {
    this.subscribers.forEach(callback => {
      try {
        callback(components);
      } catch (error) {
        console.error('Subscriber callback failed:', error);
      }
    });
  }

  /**
   * Clean up resources
   */
  destroy(): void {
    this.disableLiveUpdates();
    this.subscribers.clear();
    this.cache.invalidate();
  }
}

// ============================================================================
// Singleton Instance
// ============================================================================

export const componentRegistryService = new ComponentRegistryService();

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Get available component categories
 */
export async function getComponentCategories(): Promise<string[]> {
  const components = await componentRegistryService.getComponents();
  const categories = new Set(components.map(c => c.category));
  return Array.from(categories).sort();
}

/**
 * Get popular components (by usage count)
 */
export async function getPopularComponents(limit = 10): Promise<ComponentRegistryComponent[]> {
  const components = await componentRegistryService.getComponents();
  return components
    .filter(c => c.usageCount && c.usageCount > 0)
    .sort((a, b) => (b.usageCount || 0) - (a.usageCount || 0))
    .slice(0, limit);
}

/**
 * Get recently used components
 */
export async function getRecentComponents(limit = 10): Promise<ComponentRegistryComponent[]> {
  const components = await componentRegistryService.getComponents();
  return components
    .filter(c => c.lastUsed)
    .sort((a, b) => {
      const dateA = a.lastUsed ? a.lastUsed.getTime() : 0;
      const dateB = b.lastUsed ? b.lastUsed.getTime() : 0;
      return dateB - dateA;
    })
    .slice(0, limit);
}

/**
 * Filter components by device compatibility
 */
export function filterComponentsByDevice(components: ComponentRegistryComponent[], device: string): ComponentRegistryComponent[] {
  return components.filter(c => c.supportedDevices.includes(device));
}