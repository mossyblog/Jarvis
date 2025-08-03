/**
 * UIStudio API Client
 * 
 * Comprehensive TypeScript client for all UIStudio APIs with error handling,
 * retry logic, and authentication integration.
 * 
 * @module UIStudioApiClient
 */

import type {
  UIStudioPage,
  UIStudioLayout,
  UIStudioComponentBinding,
  UIStudioTemplate,
  UIStudioPermission,
  UIStudioVersion,
  UIStudioApiResponse,
  CreatePageRequest,
  UpdatePageRequest,
  CreateLayoutRequest,
  UpdateLayoutRequest,
  CreateBindingRequest,
  UpdateBindingRequest,
  CreateTemplateRequest,
  GrantPermissionRequest,
  CreateVersionRequest,
  DuplicatePageRequest,
  ApplyTemplateRequest,
  GetPublishedPagesQuery,
  GetResourcePermissionsQuery,
  GetVersionHistoryQuery,
  UIStudioEntityId
} from '../../types/uistudio';

import {
  UIStudioError,
  UIStudioNetworkError,
  createErrorFromResponse,
  createNetworkError,
  withRetry,
  createErrorContext,
  logError,
  type RetryConfig
} from '../../utils/uistudioErrors';

import { getStoredTokens } from '../../utils/tokenUtils';

// ============================================================================
// Configuration
// ============================================================================

/** API client configuration */
export interface UIStudioApiConfig {
  baseUrl: string;
  timeout: number;
  retryConfig: RetryConfig;
  enableLogging: boolean;
  authTokenKey?: string;
}

/** Default configuration */
const DEFAULT_CONFIG: UIStudioApiConfig = {
  baseUrl: '/api/uistudio',
  timeout: 10000, // 10 seconds
  retryConfig: {
    maxAttempts: 3,
    baseDelay: 1000,
    maxDelay: 10000,
    exponentialBackoff: true,
    retryableErrors: ['NETWORK_ERROR', 'SERVER_ERROR', 'RATE_LIMIT']
  },
  enableLogging: import.meta.env.DEV
};

// ============================================================================
// API Client Class
// ============================================================================

export class UIStudioApiClient {
  private config: UIStudioApiConfig;

  constructor(config: Partial<UIStudioApiConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
  }

  // ========================================================================
  // Private Utility Methods
  // ========================================================================

  /** Get authentication headers */
  private getAuthHeaders(): HeadersInit {
    const { accessToken } = getStoredTokens();
    
    return {
      'Content-Type': 'application/json',
      ...(accessToken ? { 'Authorization': `Bearer ${accessToken}` } : {})
    };
  }

  /** Make HTTP request with error handling and retries */
  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    operationName?: string
  ): Promise<UIStudioApiResponse<T>> {
    const url = `${this.config.baseUrl}${endpoint}`;
    const context = createErrorContext(operationName || 'api_request', {
      url,
      resource: endpoint.split('/')[1]
    });

    return withRetry(async () => {
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.config.timeout);

        const response = await fetch(url, {
          ...options,
          headers: {
            ...this.getAuthHeaders(),
            ...options.headers
          },
          signal: controller.signal
        });

        clearTimeout(timeoutId);

        if (!response.ok) {
          let errorData;
          try {
            errorData = await response.json();
          } catch {
            errorData = { message: 'Request failed' };
          }
          
          const error = createErrorFromResponse(response.status, errorData);
          
          if (this.config.enableLogging) {
            logError(error, context);
          }
          
          throw error;
        }

        const data = await response.json();
        return data as UIStudioApiResponse<T>;

      } catch (error) {
        if (error instanceof UIStudioError) {
          throw error;
        }

        if (error instanceof Error) {
          if (error.name === 'AbortError') {
            const timeoutError = new UIStudioError(
              'Request timed out',
              'TIMEOUT_ERROR',
              408
            );
            
            if (this.config.enableLogging) {
              logError(timeoutError, context);
            }
            
            throw timeoutError;
          }

          const networkError = createNetworkError(error);
          
          if (this.config.enableLogging) {
            logError(networkError, context);
          }
          
          throw networkError;
        }

        throw new UIStudioError('Unknown error occurred');
      }
    }, this.config.retryConfig);
  }

  // ========================================================================
  // Page Management APIs
  // ========================================================================

  /** Create a new page */
  async createPage(request: CreatePageRequest): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>('/pages', {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'create_page');
  }

  /** Update an existing page */
  async updatePage(
    pageEntityId: UIStudioEntityId,
    request: UpdatePageRequest
  ): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/pages/${pageEntityId}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    }, 'update_page');
  }

  /** Get a specific page */
  async getPage(pageEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/pages/${pageEntityId}`, {
      method: 'GET'
    }, 'get_page');
  }

  /** Get pages by owner */
  async getPagesByOwner(ownerEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/pages/by-owner/${ownerEntityId}`, {
      method: 'GET'
    }, 'get_pages_by_owner');
  }

  /** Get published pages with optional filtering */
  async getPublishedPages(query: GetPublishedPagesQuery = {}): Promise<UIStudioApiResponse<UIStudioPage>> {
    const searchParams = new URLSearchParams();
    
    if (query.limit) searchParams.append('limit', query.limit.toString());
    if (query.offset) searchParams.append('offset', query.offset.toString());
    if (query.pageType) searchParams.append('pageType', query.pageType);
    if (query.search) searchParams.append('search', query.search);

    const queryString = searchParams.toString();
    const endpoint = `/pages/published${queryString ? `?${queryString}` : ''}`;

    return this.request<UIStudioPage>(endpoint, {
      method: 'GET'
    }, 'get_published_pages');
  }

  /** Publish a page */
  async publishPage(
    pageEntityId: UIStudioEntityId,
    publishedByEntityId: UIStudioEntityId
  ): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/pages/${pageEntityId}/publish/${publishedByEntityId}`, {
      method: 'POST'
    }, 'publish_page');
  }

  /** Delete a page */
  async deletePage(
    pageEntityId: UIStudioEntityId,
    deletedByEntityId: UIStudioEntityId
  ): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/pages/${pageEntityId}/${deletedByEntityId}`, {
      method: 'DELETE'
    }, 'delete_page');
  }

  /** Duplicate a page */
  async duplicatePage(
    pageEntityId: UIStudioEntityId,
    request: DuplicatePageRequest
  ): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/pages/${pageEntityId}/duplicate`, {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'duplicate_page');
  }

  // ========================================================================
  // Layout Management APIs
  // ========================================================================

  /** Create a new layout */
  async createLayout(request: CreateLayoutRequest): Promise<UIStudioApiResponse<UIStudioLayout>> {
    return this.request<UIStudioLayout>('/layouts', {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'create_layout');
  }

  /** Update an existing layout */
  async updateLayout(
    layoutEntityId: UIStudioEntityId,
    request: UpdateLayoutRequest
  ): Promise<UIStudioApiResponse<UIStudioLayout>> {
    return this.request<UIStudioLayout>(`/layouts/${layoutEntityId}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    }, 'update_layout');
  }

  /** Update layout grid configuration */
  async updateLayoutGrid(
    layoutEntityId: UIStudioEntityId,
    gridConfig: NonNullable<UIStudioLayout['gridConfig']>
  ): Promise<UIStudioApiResponse<UIStudioLayout>> {
    return this.request<UIStudioLayout>(`/layouts/${layoutEntityId}/grid`, {
      method: 'PUT',
      body: JSON.stringify(gridConfig)
    }, 'update_layout_grid');
  }

  /** Update layout responsive configuration */
  async updateLayoutResponsive(
    layoutEntityId: UIStudioEntityId,
    responsiveConfig: NonNullable<UIStudioLayout['responsiveConfig']>
  ): Promise<UIStudioApiResponse<UIStudioLayout>> {
    return this.request<UIStudioLayout>(`/layouts/${layoutEntityId}/responsive`, {
      method: 'PUT',
      body: JSON.stringify(responsiveConfig)
    }, 'update_layout_responsive');
  }

  /** Get a specific layout */
  async getLayout(layoutEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioLayout>> {
    return this.request<UIStudioLayout>(`/layouts/${layoutEntityId}`, {
      method: 'GET'
    }, 'get_layout');
  }

  // ========================================================================
  // Component Binding APIs
  // ========================================================================

  /** Create a component binding */
  async createBinding(request: CreateBindingRequest): Promise<UIStudioApiResponse<UIStudioComponentBinding>> {
    return this.request<UIStudioComponentBinding>('/bindings', {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'create_binding');
  }

  /** Create multiple component bindings in bulk */
  async createBindingsBulk(
    requests: CreateBindingRequest[]
  ): Promise<UIStudioApiResponse<UIStudioComponentBinding>> {
    return this.request<UIStudioComponentBinding>('/bindings/bulk', {
      method: 'POST',
      body: JSON.stringify(requests)
    }, 'create_bindings_bulk');
  }

  /** Create page-specific component bindings */
  async createPageBindings(
    pageEntityId: UIStudioEntityId,
    requests: CreateBindingRequest[]
  ): Promise<UIStudioApiResponse<UIStudioComponentBinding>> {
    return this.request<UIStudioComponentBinding>(`/pages/${pageEntityId}/bindings`, {
      method: 'POST',
      body: JSON.stringify(requests)
    }, 'create_page_bindings');
  }

  /** Update a component binding */
  async updateBinding(
    bindingEntityId: UIStudioEntityId,
    request: UpdateBindingRequest
  ): Promise<UIStudioApiResponse<UIStudioComponentBinding>> {
    return this.request<UIStudioComponentBinding>(`/bindings/${bindingEntityId}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    }, 'update_binding');
  }

  /** Delete a component binding */
  async deleteBinding(bindingEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioComponentBinding>> {
    return this.request<UIStudioComponentBinding>(`/bindings/${bindingEntityId}`, {
      method: 'DELETE'
    }, 'delete_binding');
  }

  /** Get bindings for a specific page */
  async getPageBindings(pageEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioComponentBinding>> {
    return this.request<UIStudioComponentBinding>(`/pages/${pageEntityId}/bindings`, {
      method: 'GET'
    }, 'get_page_bindings');
  }

  // ========================================================================
  // Template Management APIs
  // ========================================================================

  /** Create a new template */
  async createTemplate(request: CreateTemplateRequest): Promise<UIStudioApiResponse<UIStudioTemplate>> {
    return this.request<UIStudioTemplate>('/templates', {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'create_template');
  }

  /** Update an existing template */
  async updateTemplate(
    templateEntityId: UIStudioEntityId,
    request: Partial<CreateTemplateRequest>
  ): Promise<UIStudioApiResponse<UIStudioTemplate>> {
    return this.request<UIStudioTemplate>(`/templates/${templateEntityId}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    }, 'update_template');
  }

  /** Get a specific template */
  async getTemplate(templateEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioTemplate>> {
    return this.request<UIStudioTemplate>(`/templates/${templateEntityId}`, {
      method: 'GET'
    }, 'get_template');
  }

  /** Get templates by owner */
  async getTemplatesByOwner(ownerEntityId: UIStudioEntityId): Promise<UIStudioApiResponse<UIStudioTemplate>> {
    return this.request<UIStudioTemplate>(`/templates/by-owner/${ownerEntityId}`, {
      method: 'GET'
    }, 'get_templates_by_owner');
  }

  /** Apply a template to create a new page */
  async applyTemplate(
    templateEntityId: UIStudioEntityId,
    request: ApplyTemplateRequest
  ): Promise<UIStudioApiResponse<UIStudioPage>> {
    return this.request<UIStudioPage>(`/templates/${templateEntityId}/apply`, {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'apply_template');
  }

  // ========================================================================
  // Permission Management APIs
  // ========================================================================

  /** Grant permission for a resource */
  async grantPermission(request: GrantPermissionRequest): Promise<UIStudioApiResponse<UIStudioPermission>> {
    return this.request<UIStudioPermission>('/permissions', {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'grant_permission');
  }

  /** Update permission level or expiration */
  async updatePermission(
    permissionEntityId: UIStudioEntityId,
    request: Partial<GrantPermissionRequest>
  ): Promise<UIStudioApiResponse<UIStudioPermission>> {
    return this.request<UIStudioPermission>(`/permissions/${permissionEntityId}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    }, 'update_permission');
  }

  /** Revoke permission for a resource */
  async revokePermission(
    permissionEntityId: UIStudioEntityId,
    revokedByEntityId: UIStudioEntityId,
    reason?: string
  ): Promise<UIStudioApiResponse<UIStudioPermission>> {
    const searchParams = new URLSearchParams({
      revokedByEntityId
    });
    
    if (reason) {
      searchParams.append('reason', reason);
    }

    return this.request<UIStudioPermission>(`/permissions/${permissionEntityId}?${searchParams.toString()}`, {
      method: 'DELETE'
    }, 'revoke_permission');
  }

  /** Get permissions for a resource */
  async getResourcePermissions(
    resourceEntityId: UIStudioEntityId,
    query: GetResourcePermissionsQuery
  ): Promise<UIStudioApiResponse<UIStudioPermission>> {
    const searchParams = new URLSearchParams({
      resourceType: query.resourceType
    });

    return this.request<UIStudioPermission>(`/resources/${resourceEntityId}/permissions?${searchParams.toString()}`, {
      method: 'GET'
    }, 'get_resource_permissions');
  }

  // ========================================================================
  // Version Control APIs
  // ========================================================================

  /** Create a version snapshot */
  async createVersionSnapshot(request: CreateVersionRequest): Promise<UIStudioApiResponse<UIStudioVersion>> {
    return this.request<UIStudioVersion>('/versions/snapshots', {
      method: 'POST',
      body: JSON.stringify(request)
    }, 'create_version_snapshot');
  }

  /** Rollback to a specific version */
  async rollbackToVersion(
    versionId: UIStudioEntityId,
    rolledBackById: UIStudioEntityId
  ): Promise<UIStudioApiResponse<UIStudioVersion>> {
    return this.request<UIStudioVersion>(`/versions/${versionId}/rollback/${rolledBackById}`, {
      method: 'POST'
    }, 'rollback_to_version');
  }

  /** Publish a specific version */
  async publishVersion(
    versionId: UIStudioEntityId,
    publishedById: UIStudioEntityId
  ): Promise<UIStudioApiResponse<UIStudioVersion>> {
    return this.request<UIStudioVersion>(`/versions/${versionId}/publish/${publishedById}`, {
      method: 'POST'
    }, 'publish_version');
  }

  /** Get version history for a resource */
  async getVersionHistory(
    resourceId: UIStudioEntityId,
    query: GetVersionHistoryQuery = {}
  ): Promise<UIStudioApiResponse<UIStudioVersion>> {
    const searchParams = new URLSearchParams();
    
    if (query.limit) searchParams.append('limit', query.limit.toString());
    if (query.offset) searchParams.append('offset', query.offset.toString());

    const queryString = searchParams.toString();
    const endpoint = `/resources/${resourceId}/versions${queryString ? `?${queryString}` : ''}`;

    return this.request<UIStudioVersion>(endpoint, {
      method: 'GET'
    }, 'get_version_history');
  }

  // ========================================================================
  // Utility Methods
  // ========================================================================

  /** Update configuration */
  updateConfig(config: Partial<UIStudioApiConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /** Get current configuration */
  getConfig(): UIStudioApiConfig {
    return { ...this.config };
  }

  /** Health check endpoint */
  async healthCheck(): Promise<{ status: 'ok' | 'error'; timestamp: string }> {
    try {
      // Simple health check - just try to fetch pages with minimal data
      await this.getPublishedPages({ limit: 1 });
      return {
        status: 'ok',
        timestamp: new Date().toISOString()
      };
    } catch {
      return {
        status: 'error',
        timestamp: new Date().toISOString()
      };
    }
  }
}

// ============================================================================
// Singleton Instance
// ============================================================================

/** Default UIStudio API client instance */
export const uistudioApiClient = new UIStudioApiClient();

/** Create a new UIStudio API client with custom configuration */
export function createUIStudioApiClient(config: Partial<UIStudioApiConfig> = {}): UIStudioApiClient {
  return new UIStudioApiClient(config);
}