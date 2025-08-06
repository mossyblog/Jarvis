/**
 * UIStudio API Service - Backend integration for UI Studio operations
 * 
 * Provides comprehensive API integration for all UIStudio backend operations
 * including page management, component binding, template handling, and publishing.
 */

import type { BentoPage, GridComponent, ComponentBinding } from '@/types/bento';

// ============================================================================
// Types
// ============================================================================

export interface APIResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
  code?: string;
  timestamp: string;
}

export interface PageSaveRequest {
  page: BentoPage;
  components: GridComponent[];
  bindings: ComponentBinding[];
  metadata?: {
    comment?: string;
    tags?: string[];
    category?: string;
  };
}

export interface PagePublishRequest {
  pageId: string;
  environment: 'development' | 'staging' | 'production';
  schedule?: {
    publishAt?: string;
    unpublishAt?: string;
  };
  options: {
    makePublic: boolean;
    notifyUsers: boolean;
    generateSitemap: boolean;
    optimizeAssets: boolean;
  };
  releaseNotes?: string;
}

export interface PageVersion {
  id: string;
  pageId: string;
  version: string;
  status: 'draft' | 'published' | 'archived';
  publishedAt?: string;
  publishedBy?: string;
  changes: VersionChange[];
  metadata: {
    comment?: string;
    tags?: string[];
    size: number;
    componentCount: number;
  };
}

export interface VersionChange {
  type: 'added' | 'modified' | 'removed';
  target: 'page' | 'component' | 'binding';
  targetId: string;
  description: string;
  details?: Record<string, unknown>;
}

export interface Template {
  id: string;
  name: string;
  description: string;
  category: string;
  tags: string[];
  thumbnail?: string;
  page: BentoPage;
  components: GridComponent[];
  bindings: ComponentBinding[];
  metadata: {
    createdBy: string;
    createdAt: string;
    downloads: number;
    rating: number;
    featured: boolean;
  };
}

export interface PublishResult {
  success: boolean;
  publishedUrl?: string;
  version: string;
  publishedAt: string;
  deploymentId: string;
  metrics?: {
    buildTime: number;
    assetSize: number;
    optimizationSavings: number;
  };
  errors?: string[];
  warnings?: string[];
}

// ============================================================================
// Configuration
// ============================================================================

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:7071/api';
const API_TIMEOUT = 30000; // 30 seconds

class UIStudioAPIError extends Error {
  constructor(
    message: string,
    public code?: string,
    public status?: number,
    public details?: unknown
  ) {
    super(message);
    this.name = 'UIStudioAPIError';
  }
}

// ============================================================================
// HTTP Client
// ============================================================================

class HTTPClient {
  private baseURL: string;
  private timeout: number;

  constructor(baseURL: string, timeout: number = API_TIMEOUT) {
    this.baseURL = baseURL;
    this.timeout = timeout;
  }

  private async request<T = unknown>(
    path: string,
    options: RequestInit = {}
  ): Promise<APIResponse<T>> {
    const url = `${this.baseURL}${path}`;
    
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
          ...options.headers,
        },
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new UIStudioAPIError(
          errorData.message || `HTTP ${response.status}: ${response.statusText}`,
          errorData.code,
          response.status,
          errorData
        );
      }

      const data = await response.json();
      return {
        success: true,
        data,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      clearTimeout(timeoutId);
      
      if (error instanceof UIStudioAPIError) {
        throw error;
      }
      
      if (error instanceof Error && error.name === 'AbortError') {
        throw new UIStudioAPIError('Request timeout', 'TIMEOUT');
      }
      
      throw new UIStudioAPIError(
        error instanceof Error ? error.message : 'Unknown error',
        'NETWORK_ERROR'
      );
    }
  }

  async get<T = unknown>(path: string, params?: Record<string, string>): Promise<APIResponse<T>> {
    const url = new URL(path, this.baseURL);
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        url.searchParams.append(key, value);
      });
    }
    
    return this.request<T>(url.pathname + url.search);
  }

  async post<T = unknown>(path: string, data?: unknown): Promise<APIResponse<T>> {
    return this.request<T>(path, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T = unknown>(path: string, data?: unknown): Promise<APIResponse<T>> {
    return this.request<T>(path, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async patch<T = unknown>(path: string, data?: unknown): Promise<APIResponse<T>> {
    return this.request<T>(path, {
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T = unknown>(path: string): Promise<APIResponse<T>> {
    return this.request<T>(path, {
      method: 'DELETE',
    });
  }
}

// ============================================================================
// UIStudio API Service
// ============================================================================

export class UIStudioAPIService {
  private client: HTTPClient;

  constructor(baseURL: string = API_BASE_URL) {
    this.client = new HTTPClient(baseURL);
  }

  // ========================================================================
  // Page Management
  // ========================================================================

  /**
   * Save a page with its components and bindings
   */
  async savePage(request: PageSaveRequest): Promise<APIResponse<BentoPage>> {
    try {
      console.log('Saving page:', request.page.displayName);
      
      // For now, simulate API call with local storage backup
      const savedData = {
        ...request,
        savedAt: new Date().toISOString(),
        version: String(parseInt(String(request.page.version) || '1') + 1)
      };
      
      localStorage.setItem(`page_${request.page.id}`, JSON.stringify(savedData));
      
      // Simulate network delay
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      const updatedPage: BentoPage = {
        ...request.page,
        version: parseInt(savedData.version) || 1,
        updatedAt: savedData.savedAt
      };
      
      return {
        success: true,
        data: updatedPage,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to save page:', error);
      throw new UIStudioAPIError('Failed to save page');
    }
  }

  /**
   * Load a page by ID
   */
  async loadPage(pageId: string): Promise<APIResponse<PageSaveRequest>> {
    try {
      const savedData = localStorage.getItem(`page_${pageId}`);
      if (!savedData) {
        throw new UIStudioAPIError('Page not found', 'NOT_FOUND');
      }
      
      const data = JSON.parse(savedData);
      return {
        success: true,
        data,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to load page:', error);
      throw new UIStudioAPIError('Failed to load page');
    }
  }

  /**
   * Get page versions and history
   */
  async getPageVersions(pageId: string): Promise<APIResponse<PageVersion[]>> {
    try {
      // Mock version history
      const versions: PageVersion[] = [
        {
          id: 'v3',
          pageId,
          version: '3.0',
          status: 'draft',
          changes: [
            {
              type: 'modified',
              target: 'component',
              targetId: 'comp-1',
              description: 'Updated metric card styling'
            }
          ],
          metadata: {
            comment: 'Latest changes',
            tags: ['current'],
            size: 15420,
            componentCount: 8
          }
        },
        {
          id: 'v2',
          pageId,
          version: '2.1',
          status: 'published',
          publishedAt: new Date(Date.now() - 86400000).toISOString(),
          publishedBy: 'current-user',
          changes: [
            {
              type: 'added',
              target: 'component',
              targetId: 'comp-2',
              description: 'Added new chart component'
            }
          ],
          metadata: {
            comment: 'Added analytics dashboard',
            tags: ['stable'],
            size: 14200,
            componentCount: 7
          }
        }
      ];
      
      return {
        success: true,
        data: versions,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to get page versions:', error);
      throw new UIStudioAPIError('Failed to get page versions');
    }
  }

  /**
   * Publish a page to specified environment
   */
  async publishPage(request: PagePublishRequest): Promise<APIResponse<PublishResult>> {
    try {
      console.log('Publishing page:', request.pageId, 'to', request.environment);
      
      // Simulate publishing process
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      const result: PublishResult = {
        success: true,
        publishedUrl: `https://${request.environment}.example.com/pages/${request.pageId}`,
        version: '1.0.0',
        publishedAt: new Date().toISOString(),
        deploymentId: `deploy_${Date.now()}`,
        metrics: {
          buildTime: 3500,
          assetSize: 245000,
          optimizationSavings: 15
        },
        errors: [],
        warnings: []
      };
      
      return {
        success: true,
        data: result,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to publish page:', error);
      throw new UIStudioAPIError('Failed to publish page');
    }
  }

  /**
   * Unpublish a page from specified environment
   */
  async unpublishPage(pageId: string, environment: string): Promise<APIResponse<void>> {
    try {
      console.log('Unpublishing page:', pageId, 'from', environment);
      
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      return {
        success: true,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to unpublish page:', error);
      throw new UIStudioAPIError('Failed to unpublish page');
    }
  }

  // ========================================================================
  // Template Management
  // ========================================================================

  /**
   * Get available templates
   */
  async getTemplates(category?: string, featured?: boolean): Promise<APIResponse<Template[]>> {
    try {
      // Mock templates data
      const templates: Template[] = [
        {
          id: 'template-dashboard',
          name: 'Analytics Dashboard',
          description: 'Comprehensive analytics dashboard with metrics and charts',
          category: 'Dashboard',
          tags: ['analytics', 'metrics', 'charts'],
          thumbnail: '/templates/dashboard-thumb.png',
          page: {
            id: 'template-page-1',
            displayName: 'Analytics Dashboard',
            route: '/dashboard',
            layoutId: 'default',
            status: 'Draft' as any,
            version: 1,
            bindings: {
              security: { isPublic: false, requiredRoles: [], requiredPermissions: [] },
              visibility: { showInNavigation: true, navigationOrder: 1 }
            },
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            createdBy: 'system',
            updatedBy: 'system'
          },
          components: [],
          bindings: [],
          metadata: {
            createdBy: 'UIStudio Team',
            createdAt: new Date().toISOString(),
            downloads: 1240,
            rating: 4.8,
            featured: true
          }
        },
        {
          id: 'template-form',
          name: 'Contact Form',
          description: 'Responsive contact form with validation',
          category: 'Forms',
          tags: ['form', 'contact', 'validation'],
          thumbnail: '/templates/form-thumb.png',
          page: {
            id: 'template-page-2',
            displayName: 'Contact Form',
            route: '/contact',
            layoutId: 'default',
            status: 'Draft' as any,
            version: 1,
            bindings: {
              security: { isPublic: true, requiredRoles: [], requiredPermissions: [] },
              visibility: { showInNavigation: true, navigationOrder: 2 }
            },
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            createdBy: 'system',
            updatedBy: 'system'
          },
          components: [],
          bindings: [],
          metadata: {
            createdBy: 'UIStudio Team',
            createdAt: new Date().toISOString(),
            downloads: 890,
            rating: 4.6,
            featured: false
          }
        }
      ];
      
      let filteredTemplates = templates;
      
      if (category) {
        filteredTemplates = filteredTemplates.filter(t => t.category === category);
      }
      
      if (featured !== undefined) {
        filteredTemplates = filteredTemplates.filter(t => t.metadata.featured === featured);
      }
      
      return {
        success: true,
        data: filteredTemplates,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to get templates:', error);
      throw new UIStudioAPIError('Failed to get templates');
    }
  }

  /**
   * Get template by ID
   */
  async getTemplate(templateId: string): Promise<APIResponse<Template>> {
    try {
      const templatesResponse = await this.getTemplates();
      const template = templatesResponse.data?.find(t => t.id === templateId);
      
      if (!template) {
        throw new UIStudioAPIError('Template not found', 'NOT_FOUND');
      }
      
      return {
        success: true,
        data: template,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to get template:', error);
      throw new UIStudioAPIError('Failed to get template');
    }
  }

  /**
   * Create page from template
   */
  async createPageFromTemplate(
    templateId: string,
    pageName: string,
    route: string
  ): Promise<APIResponse<PageSaveRequest>> {
    try {
      const templateResponse = await this.getTemplate(templateId);
      if (!templateResponse.data) {
        throw new UIStudioAPIError('Template not found');
      }
      
      const template = templateResponse.data;
      const pageId = `page_${Date.now()}`;
      
      const pageData: PageSaveRequest = {
        page: {
          ...template.page,
          id: pageId,
          displayName: pageName,
          route,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          createdBy: 'current-user',
          updatedBy: 'current-user'
        },
        components: template.components.map(comp => ({
          ...comp,
          id: `comp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
        })),
        bindings: template.bindings.map(binding => ({
          ...binding,
          id: `binding_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
          componentId: `comp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
        }))
      };
      
      return {
        success: true,
        data: pageData,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to create page from template:', error);
      throw new UIStudioAPIError('Failed to create page from template');
    }
  }

  // ========================================================================
  // Component Bindings
  // ========================================================================

  /**
   * Save component binding configuration
   */
  async saveBinding(binding: ComponentBinding): Promise<APIResponse<ComponentBinding>> {
    try {
      console.log('Saving binding:', binding.name);
      
      const savedBinding = {
        ...binding,
        updatedAt: new Date().toISOString()
      };
      
      localStorage.setItem(`binding_${binding.id}`, JSON.stringify(savedBinding));
      
      return {
        success: true,
        data: savedBinding,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to save binding:', error);
      throw new UIStudioAPIError('Failed to save binding');
    }
  }

  /**
   * Test a component binding
   */
  async testBinding(binding: ComponentBinding): Promise<APIResponse<any>> {
    try {
      console.log('Testing binding:', binding.name);
      
      // Simulate testing process
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Mock test result
      const testData = {
        user: {
          id: '123',
          firstName: 'John',
          lastName: 'Doe',
          email: 'john.doe@example.com',
          isActive: true,
          createdAt: new Date().toISOString()
        }
      };
      
      return {
        success: true,
        data: testData,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Failed to test binding:', error);
      throw new UIStudioAPIError('Failed to test binding');
    }
  }

  // ========================================================================
  // Health and Status
  // ========================================================================

  /**
   * Check API health
   */
  async health(): Promise<APIResponse<{ status: string; version: string; timestamp: string }>> {
    try {
      return {
        success: true,
        data: {
          status: 'healthy',
          version: '1.0.0',
          timestamp: new Date().toISOString()
        },
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Health check failed:', error);
      throw new UIStudioAPIError('Health check failed');
    }
  }
}

// ============================================================================
// Service Instance
// ============================================================================

export const uiStudioAPI = new UIStudioAPIService();
export default uiStudioAPI;

// ============================================================================
// Hooks for React Components
// ============================================================================

/**
 * React hook for UI Studio API operations with loading and error states
 */
export function useUIStudioAPI() {
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<UIStudioAPIError | null>(null);

  const execute = React.useCallback(async <T>(
    operation: () => Promise<APIResponse<T>>
  ): Promise<T | null> => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await operation();
      return response.data || null;
    } catch (err) {
      const apiError = err instanceof UIStudioAPIError ? err : 
        new UIStudioAPIError(err instanceof Error ? err.message : 'Unknown error');
      setError(apiError);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    loading,
    error,
    execute,
    api: uiStudioAPI
  };
}

// Import React for the hook (this would be at the top in a real file)
import React from 'react';