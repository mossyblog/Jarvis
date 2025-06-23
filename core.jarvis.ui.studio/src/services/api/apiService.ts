import type { 
  User, 
  LoginCredentials, 
  AuthResponse, 
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

// Real API service implementation (to be implemented later)
class RealApiService implements IApiService {
  // private apiUrl: string;

  constructor(_apiUrl: string = import.meta.env.VITE_API_URL || '/api') {
    // this.apiUrl = apiUrl;
    // TODO: Store API URL when implementing real API
  }

  async login(_credentials: LoginCredentials): Promise<ApiResponse<AuthResponse>> {
    // TODO: Implement real API call
    throw new Error('Real API not implemented yet');
  }

  async logout(): Promise<void> {
    // TODO: Implement real API call
    throw new Error('Real API not implemented yet');
  }

  async getCurrentUser(): Promise<ApiResponse<User | null>> {
    // TODO: Implement real API call
    throw new Error('Real API not implemented yet');
  }

  async getNavigation(): Promise<ApiResponse<NavigationItem[]>> {
    // TODO: Implement real API call
    throw new Error('Real API not implemented yet');
  }

  async refreshToken(_token: string): Promise<ApiResponse<AuthResponse>> {
    // TODO: Implement real API call
    throw new Error('Real API not implemented yet');
  }

  hasPermission(_user: User, _resource: string, _action?: string): boolean {
    // TODO: Implement permission check
    throw new Error('Real API not implemented yet');
  }
}

// Factory function to create the appropriate service
export function createApiService(): IApiService {
  const useMockApi = import.meta.env.VITE_USE_MOCK_API !== 'false';
  
  if (useMockApi) {
    return new MockApiService();
  }
  
  return new RealApiService();
}

// Export a singleton instance
export const apiService = createApiService();