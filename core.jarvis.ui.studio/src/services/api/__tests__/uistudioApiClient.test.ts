/**
 * UIStudio API Client Tests
 * 
 * Comprehensive tests for the UIStudio API client functionality,
 * error handling, and React Query integration.
 * 
 * @module UIStudioApiClientTests
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { UIStudioApiClient, createUIStudioApiClient, uistudioApiClient } from '../uistudioApiClient';
import { UIStudioError, UIStudioNetworkError, UIStudioAuthError } from '../../../utils/uistudioErrors';
import type { CreatePageRequest, UpdatePageRequest, CreateBindingRequest } from '../../../types/uistudio';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock token utils
vi.mock('../../../utils/tokenUtils', () => ({
  getStoredTokens: vi.fn(() => ({ accessToken: 'test-token', refreshToken: 'test-refresh' }))
}));

describe('UIStudioApiClient', () => {
  let client: UIStudioApiClient;

  beforeEach(() => {
    client = createUIStudioApiClient({
      baseUrl: '/api/uistudio',
      timeout: 5000,
      enableLogging: false
    });
    
    mockFetch.mockClear();
  });

  afterEach(() => {
    vi.clearAllTimers();
  });

  // ========================================================================
  // Configuration Tests
  // ========================================================================

  describe('Configuration', () => {
    it('should create client with default configuration', () => {
      const defaultClient = new UIStudioApiClient();
      const config = defaultClient.getConfig();
      
      expect(config.baseUrl).toBe('/api/uistudio');
      expect(config.timeout).toBe(10000);
      expect(config.retryConfig.maxAttempts).toBe(3);
    });

    it('should create client with custom configuration', () => {
      const customClient = createUIStudioApiClient({
        baseUrl: '/custom/api',
        timeout: 15000
      });
      
      const config = customClient.getConfig();
      expect(config.baseUrl).toBe('/custom/api');
      expect(config.timeout).toBe(15000);
    });

    it('should update configuration', () => {
      client.updateConfig({ baseUrl: '/updated/api' });
      const config = client.getConfig();
      expect(config.baseUrl).toBe('/updated/api');
    });
  });

  // ========================================================================
  // Success Response Tests
  // ========================================================================

  describe('Successful API Calls', () => {
    it('should create a page successfully', async () => {
      const mockPage = {
        id: 'page-123',
        ownerEntityId: 'entity-123',
        pageName: 'Test Page',
        pageSlug: 'test-page',
        pageType: 'dynamic',
        isPublished: false,
        createdAt: '2024-01-01T00:00:00Z',
        lastUpdated: '2024-01-01T00:00:00Z',
        createdByEntityId: 'user-123'
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [mockPage]
      });

      const request: CreatePageRequest = {
        pageName: 'Test Page',
        pageSlug: 'test-page',
        pageType: 'dynamic',
        createdByEntityId: 'user-123'
      };

      const result = await client.createPage(request);
      
      expect(result).toEqual([mockPage]);
      expect(mockFetch).toHaveBeenCalledWith(
        '/api/uistudio/pages',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Authorization': 'Bearer test-token'
          }),
          body: JSON.stringify(request)
        })
      );
    });

    it('should get page bindings successfully', async () => {
      const mockBindings = [
        {
          id: 'binding-123',
          ownerEntityId: 'entity-123',
          pageSlug: 'test-page',
          componentType: 'table',
          componentInstanceId: 'table-1',
          boundComponentType: 'TaskComponent',
          createdByEntityId: 'user-123',
          lastUpdated: '2024-01-01T00:00:00Z'
        }
      ];

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockBindings
      });

      const result = await client.getPageBindings('page-entity-123');
      
      expect(result).toEqual(mockBindings);
      expect(mockFetch).toHaveBeenCalledWith(
        '/api/uistudio/pages/page-entity-123/bindings',
        expect.objectContaining({
          method: 'GET'
        })
      );
    });

    it('should update layout grid configuration', async () => {
      const mockLayout = {
        id: 'layout-123',
        ownerEntityId: 'entity-123',
        layoutType: 'grid',
        maxColumns: 12,
        isResponsive: true,
        gridConfig: {
          columns: 6,
          gap: '20px',
          padding: '15px',
          minItemWidth: '250px'
        },
        createdByEntityId: 'user-123',
        lastUpdated: '2024-01-01T00:00:00Z'
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [mockLayout]
      });

      const gridConfig = {
        columns: 6,
        gap: '20px',
        padding: '15px',
        minItemWidth: '250px'
      };

      const result = await client.updateLayoutGrid('layout-entity-123', gridConfig);
      
      expect(result).toEqual([mockLayout]);
      expect(mockFetch).toHaveBeenCalledWith(
        '/api/uistudio/layouts/layout-entity-123/grid',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify(gridConfig)
        })
      );
    });
  });

  // ========================================================================
  // Error Handling Tests
  // ========================================================================

  describe('Error Handling', () => {
    it('should handle 401 authentication errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: async () => ({ error: 'Unauthorized', message: 'Invalid token' })
      });

      await expect(client.getPage('page-123')).rejects.toThrow(UIStudioAuthError);
    });

    it('should handle 404 not found errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({ error: 'Not Found', message: 'Page not found' })
      });

      await expect(client.getPage('nonexistent-page')).rejects.toThrow(UIStudioError);
    });

    it('should handle network errors', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network failed'));

      await expect(client.getPage('page-123')).rejects.toThrow(UIStudioNetworkError);
    });

    it('should handle timeout errors', async () => {
      vi.useFakeTimers();
      
      // Mock a request that never resolves
      mockFetch.mockImplementationOnce(() => new Promise(() => {}));

      const timeoutClient = createUIStudioApiClient({ timeout: 100 });
      const requestPromise = timeoutClient.getPage('page-123');
      
      // Fast-forward time to trigger timeout
      vi.advanceTimersByTime(200);

      await expect(requestPromise).rejects.toThrow(UIStudioError);
      
      vi.useRealTimers();
    });

    it('should handle malformed JSON responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => { throw new Error('Invalid JSON'); }
      });

      await expect(client.getPage('page-123')).rejects.toThrow(UIStudioError);
    });
  });

  // ========================================================================
  // Retry Logic Tests
  // ========================================================================

  describe('Retry Logic', () => {
    it('should retry on server errors', async () => {
      // First call fails with 500, second succeeds
      mockFetch
        .mockResolvedValueOnce({
          ok: false,
          status: 500,
          json: async () => ({ error: 'Server Error' })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [{ id: 'page-123', pageName: 'Test' }]
        });

      const result = await client.getPage('page-123');
      
      expect(result).toEqual([{ id: 'page-123', pageName: 'Test' }]);
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    it('should not retry on client errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ error: 'Bad Request' })
      });

      await expect(client.getPage('page-123')).rejects.toThrow();
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    it('should respect maximum retry attempts', async () => {
      // Always fail with server error
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        json: async () => ({ error: 'Server Error' })
      });

      await expect(client.getPage('page-123')).rejects.toThrow();
      // Should be called 3 times (initial + 2 retries) based on default config
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });
  });

  // ========================================================================
  // Request Building Tests
  // ========================================================================

  describe('Request Building', () => {
    it('should include authentication headers', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => []
      });

      await client.getPage('page-123');

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': 'Bearer test-token'
          })
        })
      );
    });

    it('should build query parameters correctly', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => []
      });

      await client.getPublishedPages({
        limit: 10,
        offset: 20,
        pageType: 'dynamic',
        search: 'dashboard'
      });

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/uistudio/pages/published?limit=10&offset=20&pageType=dynamic&search=dashboard',
        expect.any(Object)
      );
    });

    it('should serialize request bodies correctly', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => []
      });

      const bindingRequest: CreateBindingRequest = {
        pageSlug: 'test-page',
        componentType: 'table',
        componentInstanceId: 'table-1',
        boundComponentType: 'TaskComponent',
        fieldMappings: {
          title: '$.name',
          description: '$.description'
        },
        dataSourceConfig: {
          filters: [{ field: 'status', operator: 'eq', value: 'active' }],
          sorting: [{ field: 'createdAt', direction: 'desc' }],
          pagination: { pageSize: 20, enabled: true }
        },
        createdByEntityId: 'user-123'
      };

      await client.createBinding(bindingRequest);

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify(bindingRequest)
        })
      );
    });
  });

  // ========================================================================
  // Bulk Operations Tests
  // ========================================================================

  describe('Bulk Operations', () => {
    it('should create multiple bindings in bulk', async () => {
      const mockBindings = [
        { id: 'binding-1', componentType: 'table' },
        { id: 'binding-2', componentType: 'chart' }
      ];

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockBindings
      });

      const requests: CreateBindingRequest[] = [
        {
          pageSlug: 'test-page',
          componentType: 'table',
          componentInstanceId: 'table-1',
          boundComponentType: 'TaskComponent',
          createdByEntityId: 'user-123'
        },
        {
          pageSlug: 'test-page',
          componentType: 'chart',
          componentInstanceId: 'chart-1',
          boundComponentType: 'MetricsComponent',
          createdByEntityId: 'user-123'
        }
      ];

      const result = await client.createBindingsBulk(requests);
      
      expect(result).toEqual(mockBindings);
      expect(mockFetch).toHaveBeenCalledWith(
        '/api/uistudio/bindings/bulk',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(requests)
        })
      );
    });
  });

  // ========================================================================
  // Health Check Tests
  // ========================================================================

  describe('Health Check', () => {
    it('should return ok status when API is healthy', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => []
      });

      const result = await client.healthCheck();
      
      expect(result.status).toBe('ok');
      expect(result.timestamp).toBeDefined();
    });

    it('should return error status when API is unhealthy', async () => {
      mockFetch.mockRejectedValueOnce(new Error('API down'));

      const result = await client.healthCheck();
      
      expect(result.status).toBe('error');
      expect(result.timestamp).toBeDefined();
    });
  });

  // ========================================================================
  // Singleton Instance Tests
  // ========================================================================

  describe('Singleton Instance', () => {
    it('should provide a default singleton instance', () => {
      expect(uistudioApiClient).toBeInstanceOf(UIStudioApiClient);
    });

    it('should use the same instance across imports', () => {
      const client1 = uistudioApiClient;
      const client2 = uistudioApiClient;
      expect(client1).toBe(client2);
    });
  });

  // ========================================================================
  // Complex Workflow Tests
  // ========================================================================

  describe('Complex Workflows', () => {
    it('should handle page creation and binding workflow', async () => {
      // Mock page creation
      const mockPage = {
        id: 'page-123',
        ownerEntityId: 'entity-123',
        pageName: 'Test Page',
        pageSlug: 'test-page',
        pageType: 'dynamic',
        isPublished: false,
        createdAt: '2024-01-01T00:00:00Z',
        lastUpdated: '2024-01-01T00:00:00Z',
        createdByEntityId: 'user-123'
      };

      // Mock binding creation
      const mockBinding = {
        id: 'binding-123',
        ownerEntityId: 'binding-entity-123',
        pageSlug: 'test-page',
        componentType: 'table',
        componentInstanceId: 'table-1',
        boundComponentType: 'TaskComponent',
        createdByEntityId: 'user-123',
        lastUpdated: '2024-01-01T00:00:00Z'
      };

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [mockPage]
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [mockBinding]
        });

      // Create page
      const pageResult = await client.createPage({
        pageName: 'Test Page',
        pageSlug: 'test-page',
        pageType: 'dynamic',
        createdByEntityId: 'user-123'
      });

      expect(pageResult[0]).toEqual(mockPage);

      // Create binding for the page
      const bindingResult = await client.createBinding({
        pageSlug: 'test-page',
        componentType: 'table',
        componentInstanceId: 'table-1',
        boundComponentType: 'TaskComponent',
        createdByEntityId: 'user-123'
      });

      expect(bindingResult[0]).toEqual(mockBinding);
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });
  });
});