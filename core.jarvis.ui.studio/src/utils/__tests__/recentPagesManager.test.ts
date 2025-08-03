/**
 * Recent Pages Manager Tests
 * 
 * Unit tests for the RecentPagesManager class and related utilities.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { 
  RecentPagesManager, 
  getRecentPagesManager, 
  addToRecentPages, 
  getRecentPages, 
  removeFromRecentPages, 
  clearRecentPages 
} from '../recentPagesManager';
import type { RecentPageMetadata } from '../recentPagesManager';

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  writable: true
});

// Mock data
const mockPage1: RecentPageMetadata = {
  id: 'page-1',
  displayName: 'Test Page 1',
  route: '/test-1',
  pageSlug: 'test-1',
  status: 'published',
  lastAccessed: new Date().toISOString(),
  accessCount: 1,
  description: 'Test page 1 description',
  tags: ['test', 'page1'],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'user-1'
};

const mockPage2: RecentPageMetadata = {
  id: 'page-2',
  displayName: 'Test Page 2',
  route: '/test-2',
  pageSlug: 'test-2',
  status: 'draft',
  lastAccessed: new Date(Date.now() - 1000 * 60 * 60).toISOString(), // 1 hour ago
  accessCount: 3,
  description: 'Test page 2 description',
  tags: ['test', 'page2'],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'user-2'
};

describe('RecentPagesManager', () => {
  let manager: RecentPagesManager;

  beforeEach(() => {
    // Clear all mocks
    vi.clearAllMocks();
    
    // Mock empty localStorage
    localStorageMock.getItem.mockReturnValue(null);
    
    // Get a fresh instance (it's a singleton, but we can test it)
    manager = getRecentPagesManager();
    
    // Clear any existing data
    manager.clearAll();
  });

  afterEach(() => {
    // Clean up
    manager.clearAll();
  });

  describe('Singleton Pattern', () => {
    it('should return the same instance', () => {
      const instance1 = getRecentPagesManager();
      const instance2 = getRecentPagesManager();
      expect(instance1).toBe(instance2);
    });
  });

  describe('Adding Pages', () => {
    it('should add a new page', () => {
      manager.addPage(mockPage1);
      const pages = manager.getRecentPages();
      
      expect(pages).toHaveLength(1);
      expect(pages[0].id).toBe(mockPage1.id);
      expect(pages[0].displayName).toBe(mockPage1.displayName);
    });

    it('should update existing page access count', () => {
      // Add page first time
      manager.addPage(mockPage1);
      let pages = manager.getRecentPages();
      expect(pages[0].accessCount).toBe(1);

      // Add same page again
      manager.addPage(mockPage1);
      pages = manager.getRecentPages();
      expect(pages[0].accessCount).toBe(2);
    });

    it('should maintain order by last accessed', () => {
      // Add older page first
      manager.addPage(mockPage2);
      // Add newer page
      manager.addPage(mockPage1);
      
      const pages = manager.getRecentPages();
      expect(pages[0].id).toBe(mockPage1.id); // Most recent first
      expect(pages[1].id).toBe(mockPage2.id);
    });

    it('should limit the number of pages', () => {
      // Add more than the limit (assuming limit is 20)
      for (let i = 0; i < 25; i++) {
        const page: RecentPageMetadata = {
          ...mockPage1,
          id: `page-${i}`,
          displayName: `Test Page ${i}`
        };
        manager.addPage(page);
      }

      const pages = manager.getRecentPages();
      expect(pages.length).toBeLessThanOrEqual(20); // MAX_RECENT_PAGES
    });
  });

  describe('Retrieving Pages', () => {
    beforeEach(() => {
      manager.addPage(mockPage1);
      manager.addPage(mockPage2);
    });

    it('should get all recent pages', () => {
      const pages = manager.getRecentPages();
      expect(pages).toHaveLength(2);
    });

    it('should filter by status', () => {
      const publishedPages = manager.getRecentPagesFiltered({ status: 'published' });
      const draftPages = manager.getRecentPagesFiltered({ status: 'draft' });
      
      expect(publishedPages).toHaveLength(1);
      expect(publishedPages[0].status).toBe('published');
      
      expect(draftPages).toHaveLength(1);
      expect(draftPages[0].status).toBe('draft');
    });

    it('should filter by search term', () => {
      const searchResults = manager.getRecentPagesFiltered({ searchTerm: 'Page 1' });
      expect(searchResults).toHaveLength(1);
      expect(searchResults[0].displayName).toBe('Test Page 1');
    });

    it('should limit results', () => {
      const limitedResults = manager.getRecentPagesFiltered({ limit: 1 });
      expect(limitedResults).toHaveLength(1);
    });

    it('should get specific page by ID', () => {
      const page = manager.getRecentPage(mockPage1.id);
      expect(page).toBeTruthy();
      expect(page?.id).toBe(mockPage1.id);
    });

    it('should return null for non-existent page', () => {
      const page = manager.getRecentPage('non-existent');
      expect(page).toBeNull();
    });

    it('should check if page exists', () => {
      expect(manager.hasRecentPage(mockPage1.id)).toBe(true);
      expect(manager.hasRecentPage('non-existent')).toBe(false);
    });
  });

  describe('Removing Pages', () => {
    beforeEach(() => {
      manager.addPage(mockPage1);
      manager.addPage(mockPage2);
    });

    it('should remove a specific page', () => {
      manager.removePage(mockPage1.id);
      const pages = manager.getRecentPages();
      
      expect(pages).toHaveLength(1);
      expect(pages[0].id).toBe(mockPage2.id);
    });

    it('should clear all pages', () => {
      manager.clearAll();
      const pages = manager.getRecentPages();
      expect(pages).toHaveLength(0);
    });
  });

  describe('Statistics', () => {
    beforeEach(() => {
      manager.addPage(mockPage1); // published
      manager.addPage(mockPage2); // draft
      
      // Add an archived page
      const archivedPage: RecentPageMetadata = {
        ...mockPage1,
        id: 'page-3',
        status: 'archived'
      };
      manager.addPage(archivedPage);
    });

    it('should provide accurate statistics', () => {
      const stats = manager.getStatistics();
      
      expect(stats.totalPages).toBe(3);
      expect(stats.publishedPages).toBe(1);
      expect(stats.draftPages).toBe(1);
      expect(stats.archivedPages).toBe(1);
      expect(stats.mostAccessedPage).toBeTruthy();
      expect(stats.oldestAccess).toBeTruthy();
      expect(stats.newestAccess).toBeTruthy();
    });
  });

  describe('Updating Page Metadata', () => {
    beforeEach(() => {
      manager.addPage(mockPage1);
    });

    it('should update page metadata', () => {
      const updates = {
        displayName: 'Updated Page Name',
        description: 'Updated description'
      };
      
      manager.updatePageMetadata(mockPage1.id, updates);
      const page = manager.getRecentPage(mockPage1.id);
      
      expect(page?.displayName).toBe(updates.displayName);
      expect(page?.description).toBe(updates.description);
    });

    it('should not update non-existent page', () => {
      const initialCount = manager.getRecentPages().length;
      manager.updatePageMetadata('non-existent', { displayName: 'New Name' });
      
      expect(manager.getRecentPages().length).toBe(initialCount);
    });
  });

  describe('Storage Integration', () => {
    it('should call localStorage.setItem when adding pages', () => {
      manager.addPage(mockPage1);
      expect(localStorageMock.setItem).toHaveBeenCalled();
    });

    it('should handle localStorage errors gracefully', () => {
      localStorageMock.setItem.mockImplementation(() => {
        throw new Error('Storage full');
      });

      // Should not throw error
      expect(() => manager.addPage(mockPage1)).not.toThrow();
    });

    it('should handle corrupted storage data', () => {
      localStorageMock.getItem.mockReturnValue('invalid json');
      
      // Should not throw error and should create new storage
      expect(() => getRecentPagesManager()).not.toThrow();
    });
  });

  describe('Export/Import', () => {
    beforeEach(() => {
      manager.addPage(mockPage1);
      manager.addPage(mockPage2);
    });

    it('should export data correctly', () => {
      const exported = manager.exportData();
      
      expect(exported.pages).toHaveLength(2);
      expect(exported.version).toBeDefined();
      expect(exported.lastUpdated).toBeDefined();
    });

    it('should import data correctly', () => {
      const exported = manager.exportData();
      manager.clearAll();
      
      expect(manager.getRecentPages()).toHaveLength(0);
      
      manager.importData(exported);
      const pages = manager.getRecentPages();
      
      expect(pages).toHaveLength(2);
    });

    it('should reject invalid import data', () => {
      const invalidData = { pages: 'invalid', version: 0, lastUpdated: '' };
      
      expect(() => manager.importData(invalidData as any)).toThrow();
    });
  });
});

describe('Convenience Functions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorageMock.getItem.mockReturnValue(null);
    clearRecentPages();
  });

  it('should add page using convenience function', () => {
    addToRecentPages(mockPage1);
    const pages = getRecentPages();
    
    expect(pages).toHaveLength(1);
    expect(pages[0].id).toBe(mockPage1.id);
  });

  it('should get pages using convenience function', () => {
    addToRecentPages(mockPage1);
    addToRecentPages(mockPage2);
    
    const allPages = getRecentPages();
    const limitedPages = getRecentPages({ limit: 1 });
    const publishedPages = getRecentPages({ status: 'published' });
    
    expect(allPages).toHaveLength(2);
    expect(limitedPages).toHaveLength(1);
    expect(publishedPages).toHaveLength(1);
  });

  it('should remove page using convenience function', () => {
    addToRecentPages(mockPage1);
    addToRecentPages(mockPage2);
    
    removeFromRecentPages(mockPage1.id);
    const pages = getRecentPages();
    
    expect(pages).toHaveLength(1);
    expect(pages[0].id).toBe(mockPage2.id);
  });

  it('should clear all pages using convenience function', () => {
    addToRecentPages(mockPage1);
    addToRecentPages(mockPage2);
    
    clearRecentPages();
    const pages = getRecentPages();
    
    expect(pages).toHaveLength(0);
  });
});