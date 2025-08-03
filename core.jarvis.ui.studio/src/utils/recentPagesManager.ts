/**
 * Recent Pages Manager
 * 
 * Utility for tracking and managing user's recently accessed pages
 * with localStorage persistence and metadata management.
 * 
 * @module RecentPagesManager
 */

import type { BentoPage } from '@/types/bento';

// ============================================================================
// Types and Interfaces
// ============================================================================

export interface RecentPageMetadata {
  /** Page ID */
  id: string;
  /** Page display name */
  displayName: string;
  /** Page route/URL */
  route: string;
  /** Page slug for URL generation */
  pageSlug?: string;
  /** Page status */
  status: 'draft' | 'published' | 'archived';
  /** Last accessed timestamp */
  lastAccessed: string;
  /** Access count for this session */
  accessCount: number;
  /** Thumbnail URL if available */
  thumbnailUrl?: string;
  /** Page description */
  description?: string;
  /** Tags associated with the page */
  tags?: string[];
  /** Created date */
  createdAt: string;
  /** Last modified date */
  updatedAt: string;
  /** Created by user ID */
  createdBy: string;
}

export interface RecentPagesStorage {
  /** Array of recent page metadata */
  pages: RecentPageMetadata[];
  /** Last updated timestamp for the storage */
  lastUpdated: string;
  /** Version for migration compatibility */
  version: number;
}

// ============================================================================
// Configuration
// ============================================================================

const STORAGE_KEY = 'jarvis_recent_pages';
const STORAGE_VERSION = 1;
const MAX_RECENT_PAGES = 20;
const CLEANUP_INTERVAL_DAYS = 30;

// ============================================================================
// Recent Pages Manager Class
// ============================================================================

export class RecentPagesManager {
  private static instance: RecentPagesManager;
  private storage: RecentPagesStorage;

  private constructor() {
    this.storage = this.loadFromStorage();
    this.cleanup();
  }

  // Singleton pattern
  public static getInstance(): RecentPagesManager {
    if (!RecentPagesManager.instance) {
      RecentPagesManager.instance = new RecentPagesManager();
    }
    return RecentPagesManager.instance;
  }

  // ------------------------------------------------------------------------
  // Storage Management
  // ------------------------------------------------------------------------

  private loadFromStorage(): RecentPagesStorage {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) {
        return this.createEmptyStorage();
      }

      const parsed = JSON.parse(stored) as RecentPagesStorage;
      
      // Version compatibility check
      if (parsed.version !== STORAGE_VERSION) {
        console.log('Recent pages storage version mismatch, resetting...');
        return this.createEmptyStorage();
      }

      // Validate structure
      if (!Array.isArray(parsed.pages)) {
        return this.createEmptyStorage();
      }

      return parsed;
    } catch (error) {
      console.warn('Failed to load recent pages from storage:', error);
      return this.createEmptyStorage();
    }
  }

  private saveToStorage(): void {
    try {
      this.storage.lastUpdated = new Date().toISOString();
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.storage));
    } catch (error) {
      console.warn('Failed to save recent pages to storage:', error);
    }
  }

  private createEmptyStorage(): RecentPagesStorage {
    return {
      pages: [],
      lastUpdated: new Date().toISOString(),
      version: STORAGE_VERSION
    };
  }

  private cleanup(): void {
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - CLEANUP_INTERVAL_DAYS);
    
    const initialCount = this.storage.pages.length;
    this.storage.pages = this.storage.pages.filter(page => 
      new Date(page.lastAccessed) > cutoffDate
    );

    if (this.storage.pages.length !== initialCount) {
      console.log(`Cleaned up ${initialCount - this.storage.pages.length} old recent pages`);
      this.saveToStorage();
    }
  }

  // ------------------------------------------------------------------------
  // Page Tracking
  // ------------------------------------------------------------------------

  /**
   * Add or update a page in recent history
   */
  public addPage(page: BentoPage | RecentPageMetadata): void {
    const now = new Date().toISOString();
    
    // Convert BentoPage to RecentPageMetadata if needed
    const pageMetadata: RecentPageMetadata = this.isBentoPage(page) 
      ? this.convertBentoPageToMetadata(page, now)
      : { ...page, lastAccessed: now };

    // Find existing page
    const existingIndex = this.storage.pages.findIndex(p => p.id === pageMetadata.id);

    if (existingIndex >= 0) {
      // Update existing page
      const existing = this.storage.pages[existingIndex];
      this.storage.pages[existingIndex] = {
        ...existing,
        ...pageMetadata,
        lastAccessed: now,
        accessCount: existing.accessCount + 1
      };
    } else {
      // Add new page
      this.storage.pages.unshift({
        ...pageMetadata,
        lastAccessed: now,
        accessCount: 1
      });
    }

    // Maintain max limit
    if (this.storage.pages.length > MAX_RECENT_PAGES) {
      this.storage.pages = this.storage.pages.slice(0, MAX_RECENT_PAGES);
    }

    // Sort by last accessed (most recent first)
    this.storage.pages.sort((a, b) => 
      new Date(b.lastAccessed).getTime() - new Date(a.lastAccessed).getTime()
    );

    this.saveToStorage();
  }

  /**
   * Remove a page from recent history
   */
  public removePage(pageId: string): void {
    const initialLength = this.storage.pages.length;
    this.storage.pages = this.storage.pages.filter(page => page.id !== pageId);
    
    if (this.storage.pages.length !== initialLength) {
      this.saveToStorage();
    }
  }

  /**
   * Clear all recent pages
   */
  public clearAll(): void {
    this.storage.pages = [];
    this.saveToStorage();
  }

  /**
   * Update page metadata (useful when page details change)
   */
  public updatePageMetadata(pageId: string, updates: Partial<RecentPageMetadata>): void {
    const pageIndex = this.storage.pages.findIndex(p => p.id === pageId);
    if (pageIndex >= 0) {
      this.storage.pages[pageIndex] = {
        ...this.storage.pages[pageIndex],
        ...updates
      };
      this.saveToStorage();
    }
  }

  // ------------------------------------------------------------------------
  // Data Retrieval
  // ------------------------------------------------------------------------

  /**
   * Get all recent pages
   */
  public getRecentPages(): RecentPageMetadata[] {
    return [...this.storage.pages];
  }

  /**
   * Get recent pages with optional filtering
   */
  public getRecentPagesFiltered(options: {
    limit?: number;
    status?: 'draft' | 'published' | 'archived';
    searchTerm?: string;
  } = {}): RecentPageMetadata[] {
    let pages = [...this.storage.pages];

    // Filter by status
    if (options.status) {
      pages = pages.filter(page => page.status === options.status);
    }

    // Filter by search term
    if (options.searchTerm) {
      const searchLower = options.searchTerm.toLowerCase();
      pages = pages.filter(page => 
        page.displayName.toLowerCase().includes(searchLower) ||
        page.route.toLowerCase().includes(searchLower) ||
        page.description?.toLowerCase().includes(searchLower) ||
        page.tags?.some(tag => tag.toLowerCase().includes(searchLower))
      );
    }

    // Apply limit
    if (options.limit && options.limit > 0) {
      pages = pages.slice(0, options.limit);
    }

    return pages;
  }

  /**
   * Get a specific recent page by ID
   */
  public getRecentPage(pageId: string): RecentPageMetadata | null {
    return this.storage.pages.find(page => page.id === pageId) || null;
  }

  /**
   * Check if a page is in recent history
   */
  public hasRecentPage(pageId: string): boolean {
    return this.storage.pages.some(page => page.id === pageId);
  }

  /**
   * Get statistics about recent pages
   */
  public getStatistics(): {
    totalPages: number;
    draftPages: number;
    publishedPages: number;
    archivedPages: number;
    mostAccessedPage?: RecentPageMetadata;
    oldestAccess?: string;
    newestAccess?: string;
  } {
    const pages = this.storage.pages;
    
    const stats = {
      totalPages: pages.length,
      draftPages: pages.filter(p => p.status === 'draft').length,
      publishedPages: pages.filter(p => p.status === 'published').length,
      archivedPages: pages.filter(p => p.status === 'archived').length,
      mostAccessedPage: pages.reduce((max, page) => 
        page.accessCount > (max?.accessCount || 0) ? page : max, 
        pages[0]
      ),
      oldestAccess: pages.length > 0 ? 
        pages.reduce((oldest, page) => 
          page.lastAccessed < oldest ? page.lastAccessed : oldest, 
          pages[0].lastAccessed
        ) : undefined,
      newestAccess: pages.length > 0 ? 
        pages.reduce((newest, page) => 
          page.lastAccessed > newest ? page.lastAccessed : newest, 
          pages[0].lastAccessed
        ) : undefined
    };

    return stats;
  }

  // ------------------------------------------------------------------------
  // Helper Methods
  // ------------------------------------------------------------------------

  private isBentoPage(obj: any): obj is BentoPage {
    return obj && 
           typeof obj.id === 'string' &&
           typeof obj.displayName === 'string' &&
           typeof obj.route === 'string' &&
           typeof obj.status === 'string' &&
           typeof obj.createdAt === 'string' &&
           typeof obj.updatedAt === 'string';
  }

  private convertBentoPageToMetadata(page: BentoPage, accessTime: string): RecentPageMetadata {
    return {
      id: page.id,
      displayName: page.displayName,
      route: page.route,
      pageSlug: page.route.startsWith('/') ? page.route.slice(1) : page.route,
      status: page.status as 'draft' | 'published' | 'archived',
      lastAccessed: accessTime,
      accessCount: 1,
      description: page.description,
      tags: page.tags,
      createdAt: page.createdAt,
      updatedAt: page.updatedAt,
      createdBy: page.createdBy
    };
  }

  /**
   * Export recent pages data (useful for backup/migration)
   */
  public exportData(): RecentPagesStorage {
    return { ...this.storage };
  }

  /**
   * Import recent pages data (useful for restore/migration)
   */
  public importData(data: RecentPagesStorage): void {
    if (data.version === STORAGE_VERSION && Array.isArray(data.pages)) {
      this.storage = { ...data };
      this.saveToStorage();
    } else {
      throw new Error('Invalid recent pages data format');
    }
  }
}

// ============================================================================
// Convenience Functions
// ============================================================================

/**
 * Get the singleton instance of RecentPagesManager
 */
export const getRecentPagesManager = (): RecentPagesManager => {
  return RecentPagesManager.getInstance();
};

/**
 * Add a page to recent history (convenience function)
 */
export const addToRecentPages = (page: BentoPage | RecentPageMetadata): void => {
  getRecentPagesManager().addPage(page);
};

/**
 * Get recent pages (convenience function)
 */
export const getRecentPages = (options?: {
  limit?: number;
  status?: 'draft' | 'published' | 'archived';
  searchTerm?: string;
}): RecentPageMetadata[] => {
  return getRecentPagesManager().getRecentPagesFiltered(options);
};

/**
 * Remove a page from recent history (convenience function)
 */
export const removeFromRecentPages = (pageId: string): void => {
  getRecentPagesManager().removePage(pageId);
};

/**
 * Clear all recent pages (convenience function)
 */
export const clearRecentPages = (): void => {
  getRecentPagesManager().clearAll();
};

// ============================================================================
// React Hook
// ============================================================================

import { useState, useEffect, useCallback, useMemo } from 'react';

/**
 * React hook for managing recent pages
 */
export const useRecentPages = (options?: {
  limit?: number;
  status?: 'draft' | 'published' | 'archived';
  searchTerm?: string;
  autoRefresh?: boolean;
}) => {
  const [recentPages, setRecentPages] = useState<RecentPageMetadata[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  
  const manager = useMemo(() => getRecentPagesManager(), []);
  
  // Destructure options to prevent infinite loops
  const { limit, status, searchTerm, autoRefresh } = options || {};

  const refreshRecentPages = useCallback(() => {
    setIsLoading(true);
    try {
      const pages = manager.getRecentPagesFiltered({
        limit,
        status,
        searchTerm
      });
      setRecentPages(pages);
    } catch (error) {
      console.error('Failed to load recent pages:', error);
      setRecentPages([]);
    } finally {
      setIsLoading(false);
    }
  }, [manager, limit, status, searchTerm]);

  const addPage = useCallback((page: BentoPage | RecentPageMetadata) => {
    manager.addPage(page);
    refreshRecentPages();
  }, [manager, refreshRecentPages]);

  const removePage = useCallback((pageId: string) => {
    manager.removePage(pageId);
    refreshRecentPages();
  }, [manager, refreshRecentPages]);

  const clearAll = useCallback(() => {
    manager.clearAll();
    refreshRecentPages();
  }, [manager, refreshRecentPages]);

  const updatePageMetadata = useCallback((pageId: string, updates: Partial<RecentPageMetadata>) => {
    manager.updatePageMetadata(pageId, updates);
    refreshRecentPages();
  }, [manager, refreshRecentPages]);

  // Initial load
  useEffect(() => {
    refreshRecentPages();
  }, [refreshRecentPages]);

  // Auto-refresh on storage changes (when autoRefresh is enabled)
  useEffect(() => {
    if (autoRefresh) {
      const handleStorageChange = () => {
        refreshRecentPages();
      };

      window.addEventListener('storage', handleStorageChange);
      
      // Custom event for cross-tab communication
      window.addEventListener('recent-pages-updated', handleStorageChange);
      
      return () => {
        window.removeEventListener('storage', handleStorageChange);
        window.removeEventListener('recent-pages-updated', handleStorageChange);
      };
    }
  }, [autoRefresh, refreshRecentPages]);

  const statistics = manager.getStatistics();

  return {
    recentPages,
    isLoading,
    statistics,
    actions: {
      addPage,
      removePage,
      clearAll,
      updatePageMetadata,
      refresh: refreshRecentPages
    }
  };
};