import type { 
  User, 
  LoginCredentials, 
  AuthResponse, 
  AuthToken,
  NavigationItem,
  ApiResponse
} from './types';
import { mockUsers, fullNavigationItems } from './mockData';
import { 
  storeTokens, 
  getStoredTokens, 
  clearTokens,
  ACCESS_TOKEN_KEY
} from '../../utils/tokenUtils';

const MOCK_DELAY = 500; // Simulate network delay
const TOKEN_KEY = ACCESS_TOKEN_KEY;
const USER_KEY = 'jarvis_current_user';

// Type for API responses that may use PascalCase
interface ApiAuthResponse {
  AccessToken?: string;
  accessToken?: string;
  RefreshToken?: string;
  refreshToken?: string;
  OwnerEntityId?: string;
  ownerEntityId?: string;
  Email?: string;
  email?: string;
}

export interface IApiService {
  login(credentials: LoginCredentials): Promise<ApiResponse<AuthResponse>>;
  logout(): Promise<void>;
  getCurrentUser(): Promise<ApiResponse<User | null>>;
  getNavigation(): Promise<ApiResponse<NavigationItem[]>>;
  refreshToken(token: string): Promise<ApiResponse<AuthResponse>>;
  hasPermission(user: User, resource: string, action?: string): boolean;
  getUsers(): Promise<ApiResponse<User[]>>;
}

class MockApiService implements IApiService {
  async getUsers(): Promise<ApiResponse<User[]>> {
    await this.simulateDelay();
    return { data: mockUsers };
  }
  private simulateDelay(): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, MOCK_DELAY));
  }

  private generateToken(userId: string): string {
    return btoa(JSON.stringify({ userId, timestamp: Date.now() }));
  }

  async login(credentials: LoginCredentials): Promise<ApiResponse<AuthResponse>> {
    await this.simulateDelay();

    // Find user by email
    const user = mockUsers.find(u => u.email === credentials.email);
    
    if (!user) {
      return {
        error: {
          message: 'Invalid credentials',
          code: 'AUTH_INVALID_CREDENTIALS'
        }
      };
    }

    // In mock mode, any password works
    const accessToken = this.generateToken(user.id);
    const refreshToken = this.generateToken(user.id + '-refresh');
    
    // Store auth data
    storeTokens(accessToken, refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));

    return {
      data: {
        user,
        accessToken,
        refreshToken,
        expiresIn: 3600 // 1 hour
      }
    };
  }

  async logout(): Promise<void> {
    await this.simulateDelay();
    clearTokens();
    localStorage.removeItem(USER_KEY);
  }

  async getCurrentUser(): Promise<ApiResponse<User | null>> {
    await this.simulateDelay();

    const { accessToken } = getStoredTokens();
    const userStr = localStorage.getItem(USER_KEY);

    if (!accessToken || !userStr) {
      return { data: null };
    }

    try {
      const user = JSON.parse(userStr) as User;
      return { data: user };
    } catch {
      return { data: null };
    }
  }

  async getNavigation(): Promise<ApiResponse<NavigationItem[]>> {
    await this.simulateDelay();

    const userResult = await this.getCurrentUser();
    if (!userResult.data) {
      return { data: [] };
    }

    const user = userResult.data;
    const filteredItems = fullNavigationItems.filter(item => {
      if (!item.requiredPermission) return true;
      return this.hasPermission(user, item.requiredPermission, item.requiredAction || 'read');
    });

    return { data: filteredItems };
  }

  async refreshToken(): Promise<ApiResponse<AuthResponse>> {
    // In mock mode, we don't validate the refresh token
    await this.simulateDelay();

    const userResult = await this.getCurrentUser();
    if (!userResult.data) {
      return {
        error: {
          message: 'Invalid refresh token',
          code: 'AUTH_INVALID_TOKEN'
        }
      };
    }

    const newAccessToken = this.generateToken(userResult.data.id);
    const newRefreshToken = this.generateToken(userResult.data.id + '-refresh');

    storeTokens(newAccessToken, newRefreshToken);

    return {
      data: {
        user: userResult.data,
        accessToken: newAccessToken,
        refreshToken: newRefreshToken,
        expiresIn: 3600
      }
    };
  }

  hasPermission(user: User, resource: string, action: string = 'read'): boolean {
    // Check if user has wildcard permission
    const hasWildcard = user.roles.some(role =>
      role.permissions.some(perm =>
        perm.resource === '*' && perm.actions.includes('*')
      )
    );

    if (hasWildcard) return true;

    // Check specific permission
    return user.roles.some(role =>
      role.permissions.some(perm =>
        perm.resource === resource && 
        (perm.actions.includes(action) || perm.actions.includes('*'))
      )
    );
  }
}

// Real API service implementation
class RealApiService implements IApiService {
  async getUsers(): Promise<ApiResponse<User[]>> {
    const response = await fetch(`${this.apiUrl}/users`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });
    return this.handleResponse<User[]>(response);
  }

  async getAccounts(): Promise<ApiResponse<Record<string, unknown>[]>> {
    try {
      // For now, return mock data until we have a proper accounts endpoint
      const mockAccounts = [
        {
          id: '1',
          ownerEntityId: '021536e2-035c-450b-95e0-27732100db46',
          email: 'curltest@example.com',
          authMethod: 'password',
          isActive: true,
          createdAt: new Date().toISOString(),
          lastUpdated: new Date().toISOString(),
          ipAddress: '127.0.0.1',
          userAgent: 'Mozilla/5.0',
          profile: {
            name: 'Test Admin User',
            roleIds: [],
            permissionIds: []
          }
        }
      ];
      return { data: mockAccounts };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Failed to fetch accounts',
          code: 'FETCH_ERROR'
        }
      };
    }
  }
  private apiUrl: string;

  constructor() {
    // Always use relative URL to work with Vite proxy
    this.apiUrl = '/api';
  }

  private getAuthHeaders(): HeadersInit {
    const token = localStorage.getItem(TOKEN_KEY);
    return {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };
  }

  private async handleResponse<T>(response: Response): Promise<ApiResponse<T>> {
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
      return {
        error: {
          message: errorData.message || `Error: ${response.status}`,
          code: errorData.code || `HTTP_${response.status}`
        }
      };
    }

    const data = await response.json();
    return { data };
  }

  async login(credentials: LoginCredentials): Promise<ApiResponse<AuthResponse>> {
    try {
      // The API expects lowercase property names for JSON deserialization
      const requestBody = {
        email: credentials.email,
        password: credentials.password
      };
      
      console.log('DEBUG: RealApiService.login - apiUrl:', this.apiUrl);
      console.log('DEBUG: RealApiService.login - full URL:', `${this.apiUrl}/security/auth`);
      console.log('DEBUG: RealApiService.login - requestBody:', requestBody);
      
      const response = await fetch(`${this.apiUrl}/security/auth`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody)
      });
      
      // If we get a 401, it means authentication failed
      if (response.status === 401) {
        const errorData = await response.json().catch(() => ({ message: 'Authentication failed' }));
        return { 
          error: { 
            message: errorData.message || 'Invalid email or password', 
            code: 'AUTH_FAILED' 
          } 
        };
      }

      const result = await this.handleResponse<AuthToken>(response);
      
      if (result.error) {
        return { error: result.error };
      }

      if (result.data) {
        // The API returns PascalCase properties
        const authToken = result.data as ApiAuthResponse;
        const accessToken = authToken.AccessToken || authToken.accessToken;
        const refreshToken = authToken.RefreshToken || authToken.refreshToken;
        
        if (accessToken) {
          // Store tokens
          storeTokens(accessToken, refreshToken);
          
          // Extract user info from the auth response
          const user: User = {
            id: authToken.OwnerEntityId || authToken.ownerEntityId || '',
            email: credentials.email,
            name: credentials.email.split('@')[0], // Use email prefix as name for now
            roles: []
          };
          
          localStorage.setItem(USER_KEY, JSON.stringify(user));
          
          return {
            data: {
              user: user,
              accessToken: accessToken,
              refreshToken: refreshToken || '',
              expiresIn: 3600 // Default to 1 hour
            }
          };
        }
      }

      return { error: { message: 'Authentication failed', code: 'AUTH_FAILED' } };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Network error',
          code: 'NETWORK_ERROR'
        }
      };
    }
  }

  async logout(): Promise<void> {
    const { accessToken } = getStoredTokens();
    
    if (accessToken) {
      try {
        await fetch(`${this.apiUrl}/security/deauth`, {
          method: 'POST',
          headers: this.getAuthHeaders()
        });
      } catch {
        // Ignore errors during logout
      }
    }
    
    clearTokens();
    localStorage.removeItem(USER_KEY);
  }

  async getCurrentUser(): Promise<ApiResponse<User | null>> {
    const { accessToken } = getStoredTokens();
    
    if (!accessToken) {
      return { data: null };
    }

    // Try to get user from localStorage first
    const storedUser = localStorage.getItem(USER_KEY);
    if (storedUser) {
      try {
        const user = JSON.parse(storedUser);
        return { data: user };
      } catch {
        // Invalid stored user data
        localStorage.removeItem(USER_KEY);
      }
    }

    // If no stored user, token is invalid
    clearTokens();
    localStorage.removeItem(USER_KEY);
    return { data: null };
  }

  async getNavigation(): Promise<ApiResponse<NavigationItem[]>> {
    try {
      
      const query = `
        query {
          navigation_item_componentCollection {
            edges {
              node {
                id
                menu_id
                label
                icon
                href
                sort_order
                is_active
                required_permission_id
              }
            }
          }
        }
      `;

      const response = await fetch(`${this.apiUrl}/graphql`, {
        method: 'POST',
        headers: this.getAuthHeaders(),
        body: JSON.stringify({ query })
      });

      const result = await this.handleResponse<Record<string, unknown>>(response);
      
      if (result.error) {
        // Fallback to hardcoded navigation
        return this.getFallbackNavigation();
      }

      if (result.data?.data?.navigation_item_componentCollection?.edges) {
        const navigationItems: NavigationItem[] = result.data.data.navigation_item_componentCollection.edges
          .map((edge: Record<string, unknown>) => edge.node)
          .filter((item: Record<string, unknown>) => item.is_active)
          .sort((a: Record<string, unknown>, b: Record<string, unknown>) => (a.sort_order as number) - (b.sort_order as number))
          .map((item: Record<string, unknown>) => ({
            id: item.id,
            label: item.label,
            icon: item.icon,
            href: item.href,
            requiredPermission: item.required_permission_id ? 'navigation.read' : undefined,
            requiredAction: 'read'
          }));

        return { data: navigationItems };
      }

      // Fallback if no data
      return this.getFallbackNavigation();
    } catch {
      return this.getFallbackNavigation();
    }
  }

  private getFallbackNavigation(): ApiResponse<NavigationItem[]> {
    const navigationItems: NavigationItem[] = [
      {
        id: 'dashboard',
        label: 'Dashboard',
        icon: 'LayoutDashboard',
        href: '/',
        requiredPermission: undefined,
        requiredAction: 'read'
      },
      {
        id: 'accounts',
        label: 'Accounts',
        icon: 'Users',
        href: '/accounts',
        requiredPermission: undefined,
        requiredAction: 'read'
      },
      {
        id: 'schema',
        label: 'Schema',
        icon: 'Database',
        href: '/schema',
        requiredPermission: undefined,
        requiredAction: 'read'
      }
    ];

    return { data: navigationItems };
  }

  async refreshToken(refreshToken: string): Promise<ApiResponse<AuthResponse>> {
    try {
      const response = await fetch(`${this.apiUrl}/security/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken })
      });

      const result = await this.handleResponse<AuthToken>(response);
      
      if (result.error) {
        return { error: result.error };
      }

      if (result.data) {
        // Update stored tokens
        const authToken = result.data as ApiAuthResponse;
        const newAccessToken = authToken.AccessToken || authToken.accessToken;
        const newRefreshToken = authToken.RefreshToken || authToken.refreshToken;
        
        if (newAccessToken) {
          storeTokens(newAccessToken, newRefreshToken);
        }
        
        // Get updated user info
        const userInfo = await this.getCurrentUser();
        
        if (userInfo.data) {
          return {
            data: {
              user: userInfo.data,
              accessToken: newAccessToken || '',
              refreshToken: newRefreshToken || '',
              expiresIn: 3600
            }
          };
        }
      }

      return { error: { message: 'Token refresh failed', code: 'REFRESH_FAILED' } };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Network error',
          code: 'NETWORK_ERROR'
        }
      };
    }
  }

  hasPermission(user: User, resource: string, action: string = 'read'): boolean {
    // Check if user has wildcard permission
    const hasWildcard = user.roles.some(role =>
      role.permissions.some(perm =>
        perm.resource === '*' && perm.actions.includes('*')
      )
    );

    if (hasWildcard) return true;

    // Check specific permission
    return user.roles.some(role =>
      role.permissions.some(perm =>
        perm.resource === resource && 
        (perm.actions.includes(action) || perm.actions.includes('*'))
      )
    );
  }
}

// Factory function to create the appropriate service
// Utility to get/set API mode
export function getApiMode(): 'mock' | 'real' {
  // First check environment variable
  const envMode = import.meta.env.VITE_USE_MOCK_API;
  if (envMode === 'false') return 'real';
  if (envMode === 'true') return 'mock';
  
  // Fall back to localStorage for runtime switching
  const mode = localStorage.getItem('jarvis_api_mode');
  return mode === 'real' ? 'real' : 'mock';
}

export function setApiMode(mode: 'mock' | 'real') {
  localStorage.setItem('jarvis_api_mode', mode);
  window.location.reload();
}

export function createApiService(): IApiService {
  // Check environment variable first
  const useMockApi = import.meta.env.VITE_USE_MOCK_API === 'true';
  
  console.log('DEBUG: VITE_USE_MOCK_API =', import.meta.env.VITE_USE_MOCK_API);
  console.log('DEBUG: useMockApi =', useMockApi);
  console.log('DEBUG: VITE_API_URL =', import.meta.env.VITE_API_URL);
  
  if (useMockApi) {
    console.warn('Using mock API service. Set VITE_USE_MOCK_API=false to use real API.');
    return new MockApiService();
  }
  
  // Always use real API in production or when env var is false
  console.log('DEBUG: Using RealApiService');
  return new RealApiService();
}

// Export a singleton instance
export const apiService = createApiService();