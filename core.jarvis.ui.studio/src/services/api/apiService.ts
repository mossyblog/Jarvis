import type { 
  User, 
  LoginCredentials, 
  AuthResponse, 
  AuthToken,
  NavigationItem,
  ApiResponse
} from './types';
import { mockUsers, fullNavigationItems } from './mockData';

const MOCK_DELAY = 500; // Simulate network delay
const TOKEN_KEY = 'jarvis_auth_token';
const USER_KEY = 'jarvis_current_user';

export interface IApiService {
  login(credentials: LoginCredentials): Promise<ApiResponse<AuthResponse>>;
  logout(): Promise<void>;
  getCurrentUser(): Promise<ApiResponse<User | null>>;
  getNavigation(): Promise<ApiResponse<NavigationItem[]>>;
  refreshToken(token: string): Promise<ApiResponse<AuthResponse>>;
  hasPermission(user: User, resource: string, action?: string): boolean;
}

class MockApiService implements IApiService {
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
    localStorage.setItem(TOKEN_KEY, accessToken);
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
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  async getCurrentUser(): Promise<ApiResponse<User | null>> {
    await this.simulateDelay();

    const token = localStorage.getItem(TOKEN_KEY);
    const userStr = localStorage.getItem(USER_KEY);

    if (!token || !userStr) {
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

  async refreshToken(_token: string): Promise<ApiResponse<AuthResponse>> {
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

    localStorage.setItem(TOKEN_KEY, newAccessToken);

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
  private apiUrl: string;

  constructor(apiUrl: string = import.meta.env.VITE_API_URL || 'http://localhost:7071/api') {
    this.apiUrl = apiUrl;
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
      // The API expects a User object but only cares about Email and Password for auth
      const requestBody = {
        Email: credentials.email,
        Password: credentials.password
      };
      
      console.log('Sending login request:', requestBody);
      
      const response = await fetch(`${this.apiUrl}/security/auth`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody)
      });

      const result = await this.handleResponse<AuthToken>(response);
      
      if (result.error) {
        return { error: result.error };
      }

      if (result.data) {
        // The API returns PascalCase properties
        const authToken = result.data as any;
        const accessToken = authToken.AccessToken || authToken.accessToken;
        const refreshToken = authToken.RefreshToken || authToken.refreshToken;
        
        if (accessToken) {
          // Store tokens
          localStorage.setItem(TOKEN_KEY, accessToken);
          
          // Get user info from the token
          const userInfo = await this.getCurrentUser();
          
          if (userInfo.data) {
            localStorage.setItem(USER_KEY, JSON.stringify(userInfo.data));
            
            return {
              data: {
                user: userInfo.data,
                accessToken: accessToken,
                refreshToken: refreshToken,
                expiresIn: 3600 // Default to 1 hour
              }
            };
          }
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
    const token = localStorage.getItem(TOKEN_KEY);
    
    if (token) {
      try {
        await fetch(`${this.apiUrl}/security/deauth`, {
          method: 'POST',
          headers: this.getAuthHeaders()
        });
      } catch {
        // Ignore errors during logout
      }
    }
    
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  async getCurrentUser(): Promise<ApiResponse<User | null>> {
    const token = localStorage.getItem(TOKEN_KEY);
    
    if (!token) {
      return { data: null };
    }

    try {
      const response = await fetch(`${this.apiUrl}/security/user`, {
        headers: this.getAuthHeaders()
      });

      if (!response.ok) {
        if (response.status === 401) {
          // Token expired or invalid
          localStorage.removeItem(TOKEN_KEY);
          localStorage.removeItem(USER_KEY);
        }
        return { data: null };
      }

      const result = await this.handleResponse<User>(response);
      return result.data ? result : { data: null };
    } catch {
      return { data: null };
    }
  }

  async getNavigation(): Promise<ApiResponse<NavigationItem[]>> {
    try {
      const response = await fetch(`${this.apiUrl}/security/navigation`, {
        headers: this.getAuthHeaders()
      });

      return await this.handleResponse<NavigationItem[]>(response);
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Failed to load navigation',
          code: 'NAVIGATION_ERROR'
        }
      };
    }
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
        localStorage.setItem(TOKEN_KEY, result.data.accessToken);
        
        // Get updated user info
        const userInfo = await this.getCurrentUser();
        
        if (userInfo.data) {
          return {
            data: {
              user: userInfo.data,
              accessToken: result.data.accessToken,
              refreshToken: result.data.refreshToken,
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
export function createApiService(): IApiService {
  const useMockApi = import.meta.env.VITE_USE_MOCK_API === 'true';
  
  if (useMockApi) {
    console.log('Using mock API service');
    return new MockApiService();
  }
  
  console.log('Using real API service at:', import.meta.env.VITE_API_URL || 'http://localhost:7071/api');
  return new RealApiService();
}

// Export a singleton instance
export const apiService = createApiService();