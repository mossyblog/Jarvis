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

      return result.account_componentCollection?.edges?.map(edge => ({
        id: edge.node.id,
        ownerEntityId: edge.node.owner_entity_id,
        email: edge.node.email,
        authMethod: edge.node.auth_method,
        isActive: edge.node.is_active,
        createdAt: edge.node.created_at,
        lastUpdated: edge.node.last_updated
      })) || [];
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
    
    return allAccounts.filter(account => 
      account.email.toLowerCase().includes(searchLower) ||
      account.ownerEntityId.toLowerCase().includes(searchLower)
    );
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
}

// Export singleton instance
// Always use relative URL for GraphQL to work with proxy
const graphqlEndpoint = '/api/graphql';

console.log('GraphQL Service: Using endpoint:', graphqlEndpoint);
export const graphqlService = new GraphQLService(graphqlEndpoint);