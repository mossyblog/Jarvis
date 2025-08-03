import type { NavigationItem } from '../api/types';
import { ACCESS_TOKEN_KEY } from '../../utils/tokenUtils';

/**
 * GraphQL service for querying Jarvis data through Azure Functions
 * Uses the GraphQL endpoint in the API
 */
export class GraphQLService {
  private readonly endpoint: string;

  constructor(endpoint: string = '/api/graphql') {
    // Azure Functions GraphQL endpoint
    this.endpoint = endpoint;
  }

  /**
   * Execute a GraphQL query through Azure Functions
   */
  private async executeQuery<T>(query: string, variables?: Record<string, unknown>): Promise<T> {
    const response = await fetch(this.endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getJWTToken()}`,
      },
      body: JSON.stringify({
        query,
        variables,
      }),
    });

    if (!response.ok) {
      throw new Error(`GraphQL query failed: ${response.status} ${response.statusText}`);
    }

    const result = await response.json();
    
    if (result.errors) {
      throw new Error(`GraphQL errors: ${JSON.stringify(result.errors)}`);
    }

    return result.data;
  }

  /**
   * Get JWT token from localStorage
   */
  private getJWTToken(): string {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (!token) {
      throw new Error('No authentication token available');
    }
    return token;
  }

  /**
   * Extract user ID from JWT token
   */
  private getCurrentUserId(): string {
    const token = this.getJWTToken();
    try {
      // Simple JWT decode (just for payload, no verification needed here)
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.nameid || payload.sub;
    } catch {
      throw new Error('Invalid JWT token format');
    }
  }

  /**
   * Get navigation items filtered by user permissions
   * Uses direct GraphQL query to PostgreSQL with permission filtering
   */
  async getNavigationItems(): Promise<NavigationItem[]> {
    const userId = this.getCurrentUserId();
    
    const query = `
      query GetUserNavigation($userId: UUID!) {
        navigationItemCollection(
          filter: { 
            is_active: { eq: true },
            or: [
              { required_permission_id: { is: null } },
              { 
                required_permission_id: { 
                  in: {
                    select: { 
                      permission_ids: true 
                    },
                    from: "securityProfileCollection",
                    filter: { 
                      owner_entity_id: { eq: $userId } 
                    }
                  }
                }
              }
            ]
          },
          orderBy: [{ sort_order: AscNullsLast }]
        ) {
          edges {
            node {
              id
              menuId: menu_id
              label
              icon
              href
              sortOrder: sort_order
              requiredPermissionId: required_permission_id
              isActive: is_active
              badgeConfig: badge_config
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        navigationItemCollection: {
          edges: Array<{
            node: NavigationItem;
          }>;
        };
      }>(query, { userId });

      return result.navigationItemCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch navigation items via GraphQL:', error);
      // Fallback to empty array for now
      return [];
    }
  }

  /**
   * Get all navigation items (admin function)
   */
  async getAllNavigationItems(): Promise<NavigationItem[]> {
    const query = `
      query GetAllNavigation {
        navigationItemCollection(
          orderBy: [{ sort_order: AscNullsLast }]
        ) {
          edges {
            node {
              id
              menuId: menu_id
              label
              icon
              href
              sortOrder: sort_order
              requiredPermissionId: required_permission_id
              isActive: is_active
              badgeConfig: badge_config
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        navigationItemCollection: {
          edges: Array<{
            node: NavigationItem;
          }>;
        };
      }>(query);

      return result.navigationItemCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch all navigation items via GraphQL:', error);
      return [];
    }
  }

  /**
   * Check if user can access a specific navigation item
   */
  async canAccessNavigation(menuId: string): Promise<boolean> {
    const userId = this.getCurrentUserId();
    
    const query = `
      query CanAccessNavigation($menuId: String!, $userId: UUID!) {
        navigationItemCollection(
          filter: {
            menu_id: { eq: $menuId },
            is_active: { eq: true },
            or: [
              { required_permission_id: { is: null } },
              { 
                required_permission_id: { 
                  in: {
                    select: { 
                      permission_ids: true 
                    },
                    from: "securityProfileCollection",
                    filter: { 
                      owner_entity_id: { eq: $userId } 
                    }
                  }
                }
              }
            ]
          }
        ) {
          edges {
            node {
              id
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        navigationItemCollection: {
          edges: unknown[];
        };
      }>(query, { menuId, userId });

      return result.navigationItemCollection.edges.length > 0;
    } catch (error) {
      console.error('Failed to check navigation access via GraphQL:', error);
      return false;
    }
  }

  /**
   * Get all accounts with their security profiles
   */
  async getAccounts(): Promise<unknown[]> {
    const query = `
      query GetAccounts {
        account_componentCollection {
          edges {
            node {
              id
              owner_entity_id
              email
              auth_method
              is_active
              created_at
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        account_componentCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query);

      return result.account_componentCollection?.edges?.map((edge: { node: unknown }) => {
        const node = edge.node as Record<string, unknown>;
        return {
          id: node.id,
          ownerEntityId: node.owner_entity_id,
          email: node.email,
          authMethod: node.auth_method,
          isActive: node.is_active,
          createdAt: node.created_at,
          lastUpdated: node.last_updated
        };
      }) || [];
    } catch (error) {
      console.error('Failed to fetch accounts via GraphQL:', error);
      return [];
    }
  }

  /**
   * Search accounts by email, phone, or entity ID
   */
  async searchAccounts(searchTerm: string): Promise<unknown[]> {
    // For now, let's use client-side filtering until we understand the GraphQL filter syntax
    const allAccounts = await this.getAccounts();
    
    if (!searchTerm.trim()) {
      return allAccounts;
    }
    
    const searchLower = searchTerm.toLowerCase();
    
    return allAccounts
      .filter((account): account is Record<string, unknown> => 
        typeof account === 'object' && account !== null
      )
      .filter((account) => {
      const email = account.email as string;
      const ownerEntityId = account.ownerEntityId as string;
      return email?.toLowerCase().includes(searchLower) ||
        ownerEntityId?.toLowerCase().includes(searchLower);
    }
    );
  }

  /**
   * Get UIStudio published pages with GraphQL
   */
  async getPublishedPages(options?: {
    limit?: number;
    offset?: number;
    pageType?: string;
    search?: string;
  }): Promise<unknown[]> {
    const query = `
      query GetPublishedPages($limit: Int, $offset: Int, $pageType: String, $search: String) {
        ui_studio_pageCollection(
          filter: {
            is_published: { eq: true },
            ${options?.pageType ? 'page_type: { eq: $pageType },' : ''}
            ${options?.search ? 'or: [{ page_name: { ilike: $search } }, { description: { ilike: $search } }]' : ''}
          },
          ${options?.limit ? 'first: $limit,' : ''}
          ${options?.offset ? 'offset: $offset,' : ''}
          orderBy: [{ published_at: DescNullsLast }]
        ) {
          edges {
            node {
              id
              owner_entity_id
              page_name
              page_slug
              page_type
              description
              is_published
              published_at
              created_at
              version
              metadata
              tags
              created_by_entity_id
              updated_by_entity_id
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_pageCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, options);

      return result.ui_studio_pageCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch published pages via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio pages by owner with GraphQL
   */
  async getPagesByOwner(ownerEntityId: string): Promise<unknown[]> {
    const query = `
      query GetPagesByOwner($ownerId: UUID!) {
        ui_studio_pageCollection(
          filter: { owner_entity_id: { eq: $ownerId } },
          orderBy: [{ last_updated: DescNullsLast }]
        ) {
          edges {
            node {
              id
              owner_entity_id
              page_name
              page_slug
              page_type
              description
              is_published
              published_at
              created_at
              version
              metadata
              tags
              created_by_entity_id
              updated_by_entity_id
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_pageCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { ownerId: ownerEntityId });

      return result.ui_studio_pageCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch pages by owner via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio page metadata with GraphQL
   */
  async getPageMetadata(pageId: string): Promise<unknown | null> {
    const query = `
      query GetPageMetadata($pageId: UUID!) {
        ui_studio_pageCollection(
          filter: { id: { eq: $pageId } }
        ) {
          edges {
            node {
              id
              page_name
              page_slug
              page_type
              description
              is_published
              published_at
              created_at
              version
              metadata
              tags
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_pageCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { pageId });

      const edges = result.ui_studio_pageCollection.edges;
      return edges.length > 0 ? edges[0].node : null;
    } catch (error) {
      console.error('Failed to fetch page metadata via GraphQL:', error);
      return null;
    }
  }

  /**
   * Get UIStudio templates with GraphQL
   */
  async getTemplates(): Promise<unknown[]> {
    const query = `
      query GetTemplates {
        ui_studio_templateCollection(
          orderBy: [{ created_at: DescNullsLast }]
        ) {
          edges {
            node {
              id
              owner_entity_id
              template_name
              description
              category
              is_public
              thumbnail_url
              preview_url
              layout_config
              component_bindings
              created_by_entity_id
              created_at
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_templateCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query);

      return result.ui_studio_templateCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch templates via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio template metadata with GraphQL
   */
  async getTemplateMetadata(templateId: string): Promise<unknown | null> {
    const query = `
      query GetTemplateMetadata($templateId: UUID!) {
        ui_studio_templateCollection(
          filter: { id: { eq: $templateId } }
        ) {
          edges {
            node {
              id
              template_name
              description
              category
              is_public
              thumbnail_url
              preview_url
              layout_config
              component_bindings
              created_at
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_templateCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { templateId });

      const edges = result.ui_studio_templateCollection.edges;
      return edges.length > 0 ? edges[0].node : null;
    } catch (error) {
      console.error('Failed to fetch template metadata via GraphQL:', error);
      return null;
    }
  }

  /**
   * Search UIStudio templates with GraphQL
   */
  async searchTemplates(searchTerm: string): Promise<unknown[]> {
    const query = `
      query SearchTemplates($search: String!) {
        ui_studio_templateCollection(
          filter: {
            or: [
              { template_name: { ilike: $search } },
              { description: { ilike: $search } },
              { category: { ilike: $search } }
            ]
          },
          orderBy: [{ created_at: DescNullsLast }]
        ) {
          edges {
            node {
              id
              template_name
              description
              category
              is_public
              thumbnail_url
              preview_url
              created_at
              last_updated
            }
          }
        }
      }
    `;

    try {
      const searchPattern = `%${searchTerm}%`;
      const result = await this.executeQuery<{
        ui_studio_templateCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { search: searchPattern });

      return result.ui_studio_templateCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to search templates via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio component registry with GraphQL - Enhanced for Component Palette
   */
  async getComponentRegistry(options?: {
    category?: string;
    device?: string;
    search?: string;
  }): Promise<unknown[]> {
    // Use REST API call to the new component registry endpoint for better structure
    try {
      const searchParams = new URLSearchParams();
      if (options?.category) searchParams.append('category', options.category);
      if (options?.device) searchParams.append('device', options.device);
      if (options?.search) searchParams.append('search', options.search);

      const endpoint = `/api/uistudio/components/registry${searchParams.toString() ? `?${searchParams.toString()}` : ''}`;
      
      const response = await fetch(endpoint, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getJWTToken()}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Component registry API failed: ${response.status} ${response.statusText}`);
      }

      const result = await response.json();
      return result.components || [];
    } catch (error) {
      console.error('Failed to fetch component registry via API:', error);
      
      // Fallback to GraphQL-based component bindings query
      const query = `
        query GetComponentRegistry {
          ui_studio_component_bindingCollection {
            edges {
              node {
                component_type
                bound_component_type
                field_mappings
                data_source_config
              }
            }
          }
        }
      `;

      try {
        const result = await this.executeQuery<{
          ui_studio_component_bindingCollection: {
            edges: Array<{
              node: unknown;
            }>;
          };
        }>(query);

        // Group by component type to create registry
        const components = new Map();
        result.ui_studio_component_bindingCollection.edges.forEach(edge => {
          const node = edge.node as Record<string, unknown>;
          if (!components.has(node.component_type)) {
            components.set(node.component_type, {
              type: node.component_type,
              bindings: []
            });
          }
          components.get(node.component_type).bindings.push(node);
        });

        return Array.from(components.values());
      } catch (fallbackError) {
        console.error('Failed to fetch component registry via GraphQL fallback:', fallbackError);
        return [];
      }
    }
  }

  /**
   * Search components in the registry with advanced filtering
   */
  async searchComponentRegistry(searchQuery: string, options?: {
    category?: string;
    tags?: string[];
    limit?: number;
  }): Promise<unknown[]> {
    try {
      const searchParams = new URLSearchParams({
        q: searchQuery
      });
      
      if (options?.category) searchParams.append('category', options.category);
      if (options?.tags?.length) searchParams.append('tags', options.tags.join(','));
      if (options?.limit) searchParams.append('limit', options.limit.toString());

      const endpoint = `/api/uistudio/components/search?${searchParams.toString()}`;
      
      const response = await fetch(endpoint, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getJWTToken()}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Component search API failed: ${response.status} ${response.statusText}`);
      }

      const result = await response.json();
      return result.results || [];
    } catch (error) {
      console.error('Failed to search component registry:', error);
      return [];
    }
  }

  /**
   * Get detailed metadata for a specific component type
   */
  async getComponentMetadata(componentType: string): Promise<unknown | null> {
    try {
      const endpoint = `/api/uistudio/components/${encodeURIComponent(componentType)}`;
      
      const response = await fetch(endpoint, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getJWTToken()}`,
        },
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`Component metadata API failed: ${response.status} ${response.statusText}`);
      }

      const result = await response.json();
      return result.component || null;
    } catch (error) {
      console.error('Failed to get component metadata:', error);
      return null;
    }
  }

  /**
   * Legacy method - Search UIStudio components with GraphQL (maintained for compatibility)
   */
  async searchComponents(searchTerm: string): Promise<unknown[]> {
    // Delegate to the new enhanced search method
    return this.searchComponentRegistry(searchTerm);
  }

  /**
   * Get navigation statistics (for admin dashboard)
   */
  async getNavigationStats(): Promise<{
    totalItems: number;
    activeItems: number;
    permissionProtectedItems: number;
  }> {
    const query = `
      query GetNavigationStats {
        total: navigationItemCollection {
          edges {
            node {
              id
            }
          }
        }
        active: navigationItemCollection(
          filter: { is_active: { eq: true } }
        ) {
          edges {
            node {
              id
            }
          }
        }
        protected: navigationItemCollection(
          filter: { required_permission_id: { is: { null: false } } }
        ) {
          edges {
            node {
              id
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        total: { edges: unknown[] };
        active: { edges: unknown[] };
        protected: { edges: unknown[] };
      }>(query);

      return {
        totalItems: result.total.edges.length,
        activeItems: result.active.edges.length,
        permissionProtectedItems: result.protected.edges.length,
      };
    } catch (error) {
      console.error('Failed to fetch navigation stats via GraphQL:', error);
      return {
        totalItems: 0,
        activeItems: 0,
        permissionProtectedItems: 0,
      };
    }
  }

  /**
   * Get UIStudio page bindings with GraphQL
   */
  async getPageBindings(pageSlug: string): Promise<unknown[]> {
    const query = `
      query GetPageBindings($pageSlug: String!) {
        ui_studio_component_bindingCollection(
          filter: { page_slug: { eq: $pageSlug } }
        ) {
          edges {
            node {
              id
              owner_entity_id
              page_slug
              component_type
              component_instance_id
              bound_component_type
              field_mappings
              data_source_config
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_component_bindingCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { pageSlug });

      return result.ui_studio_component_bindingCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch page bindings via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio version history with GraphQL
   */
  async getVersionHistory(resourceId: string, options?: {
    limit?: number;
    offset?: number;
  }): Promise<unknown[]> {
    const query = `
      query GetVersionHistory($resourceId: UUID!, $limit: Int, $offset: Int) {
        ui_studio_versionCollection(
          filter: { resource_entity_id: { eq: $resourceId } },
          ${options?.limit ? 'first: $limit,' : ''}
          ${options?.offset ? 'offset: $offset,' : ''}
          orderBy: [{ created_at: DescNullsLast }]
        ) {
          edges {
            node {
              id
              owner_entity_id
              resource_entity_id
              resource_type
              version_number
              snapshot_data
              change_summary
              is_published
              created_by_entity_id
              created_at
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_versionCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { resourceId, ...options });

      return result.ui_studio_versionCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch version history via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio layouts with GraphQL
   */
  async getUIStudioLayouts(): Promise<unknown[]> {
    const query = `
      query GetUIStudioLayouts {
        ui_studio_layoutCollection(
          orderBy: [{ last_updated: DescNullsLast }]
        ) {
          edges {
            node {
              id
              owner_entity_id
              layout_type
              max_columns
              max_rows
              is_responsive
              grid_config
              responsive_config
              breakpoint_settings
              created_by_entity_id
              updated_by_entity_id
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_layoutCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query);

      return result.ui_studio_layoutCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch UIStudio layouts via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio recent pages with GraphQL
   */
  async getUIStudioRecentPages(userId?: string, limit: number = 10): Promise<unknown[]> {
    const currentUserId = userId || this.getCurrentUserId();
    
    const query = `
      query GetRecentPages($userId: UUID!, $limit: Int!) {
        ui_studio_pageCollection(
          filter: { 
            or: [
              { owner_entity_id: { eq: $userId } },
              { updated_by_entity_id: { eq: $userId } }
            ]
          },
          first: $limit,
          orderBy: [{ last_updated: DescNullsLast }]
        ) {
          edges {
            node {
              id
              page_name
              page_slug
              page_type
              description
              is_published
              last_updated
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        ui_studio_pageCollection: {
          edges: Array<{
            node: unknown;
          }>;
        };
      }>(query, { userId: currentUserId, limit });

      return result.ui_studio_pageCollection.edges.map(edge => edge.node);
    } catch (error) {
      console.error('Failed to fetch recent pages via GraphQL:', error);
      return [];
    }
  }

  /**
   * Get UIStudio page statistics with GraphQL
   */
  async getPageStatistics(): Promise<{
    totalPages: number;
    publishedPages: number;
    draftPages: number;
  }> {
    const query = `
      query GetPageStatistics {
        total: ui_studio_pageCollection {
          edges {
            node {
              id
            }
          }
        }
        published: ui_studio_pageCollection(
          filter: { is_published: { eq: true } }
        ) {
          edges {
            node {
              id
            }
          }
        }
        draft: ui_studio_pageCollection(
          filter: { is_published: { eq: false } }
        ) {
          edges {
            node {
              id
            }
          }
        }
      }
    `;

    try {
      const result = await this.executeQuery<{
        total: { edges: unknown[] };
        published: { edges: unknown[] };
        draft: { edges: unknown[] };
      }>(query);

      return {
        totalPages: result.total.edges.length,
        publishedPages: result.published.edges.length,
        draftPages: result.draft.edges.length,
      };
    } catch (error) {
      console.error('Failed to fetch page statistics via GraphQL:', error);
      return {
        totalPages: 0,
        publishedPages: 0,
        draftPages: 0,
      };
    }
  }
}

// Export singleton instance
// Always use relative URL for GraphQL to work with proxy
const graphqlEndpoint = '/api/graphql';

console.log('GraphQL Service: Using endpoint:', graphqlEndpoint);
export const graphqlService = new GraphQLService(graphqlEndpoint);