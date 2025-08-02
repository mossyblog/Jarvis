import { createContext, useContext, useState, useEffect, useRef, useCallback } from 'react';
import type { ReactNode } from 'react';
import type { User, LoginCredentials, NavigationItem } from '../services/api/types';
import { apiService } from '../services/api/apiService';
import { 
  getStoredTokens, 
  isTokenExpired, 
  getTimeUntilExpiry,
  clearTokens 
} from '../utils/tokenUtils';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  navigation: NavigationItem[];
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  hasPermission: (resource: string, action?: string) => boolean;
  refreshAuth: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [navigation, setNavigation] = useState<NavigationItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Load user and navigation on mount
  useEffect(() => {
    initializeAuth();
    
    // Cleanup on unmount
    return () => {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current);
      }
    };
  }, []); // Remove initializeAuth dependency to break circular dependency

  const logout = useCallback(async () => {
    // Clear refresh timeout
    if (refreshTimeoutRef.current) {
      clearTimeout(refreshTimeoutRef.current);
      refreshTimeoutRef.current = null;
    }
    
    await apiService.logout();
    setUser(null);
    setNavigation([]);
    
    // Clear stored data
    localStorage.removeItem('jarvis_current_user');
    localStorage.removeItem('jarvis_navigation');
  }, []);

  const scheduleTokenRefresh = useCallback((accessToken: string) => {
    // Clear any existing timeout
    if (refreshTimeoutRef.current) {
      clearTimeout(refreshTimeoutRef.current);
    }

    const timeUntilExpiry = getTimeUntilExpiry(accessToken);
    
    // Schedule refresh 5 minutes before expiry
    const refreshTime = Math.max(0, timeUntilExpiry - 5 * 60 * 1000);
    
    if (refreshTime > 0) {
      console.log(`Scheduling token refresh in ${refreshTime / 1000 / 60} minutes`);
      
      refreshTimeoutRef.current = setTimeout(async () => {
        const { refreshToken } = getStoredTokens();
        if (refreshToken) {
          console.log('Auto-refreshing token...');
          const result = await apiService.refreshToken(refreshToken);
          
          if (result.data) {
            console.log('Token refreshed successfully');
            scheduleTokenRefresh(result.data.accessToken);
          } else {
            console.error('Token refresh failed:', result.error);
            // Clear auth on refresh failure
            await logout();
          }
        }
      }, refreshTime);
    }
  }, [logout]);

  const initializeAuth = useCallback(async () => {
    try {
      setIsLoading(true);
      const { accessToken, refreshToken } = getStoredTokens();
      
      if (!accessToken) {
        // No stored tokens, user needs to login
        return;
      }

      // Create a timeout promise
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('API timeout')), 5000); // 5 second timeout
      });

      // Check if access token is expired
      if (isTokenExpired(accessToken)) {
        console.log('Access token expired, attempting refresh...');
        
        if (refreshToken) {
          try {
            const result = await Promise.race([
              apiService.refreshToken(refreshToken),
              timeoutPromise
            ]);
            
            if (result.data) {
              console.log('Token refreshed on startup');
              setUser(result.data.user);
              
              // Load navigation with timeout
              try {
                const navResult = await Promise.race([
                  apiService.getNavigation(),
                  timeoutPromise
                ]);
                if (navResult.data) {
                  setNavigation(navResult.data);
                }
              } catch (navError) {
                console.error('Failed to load navigation:', navError);
              }
              
              // Schedule next refresh
              scheduleTokenRefresh(result.data.accessToken);
              return;
            }
          } catch (refreshError) {
            console.error('Token refresh error:', refreshError);
          }
        }
        
        // Refresh failed or no refresh token
        console.log('Token refresh failed, clearing auth');
        clearTokens();
        localStorage.removeItem('jarvis_current_user');
        return;
      }

      // Token is still valid, load current user with timeout
      try {
        await Promise.race([
          loadCurrentUser(),
          timeoutPromise
        ]);
        
        // Schedule refresh
        scheduleTokenRefresh(accessToken);
      } catch (loadError) {
        console.error('Failed to load current user:', loadError);
        // If API is down, we'll still show the user as logged in
        // but with limited functionality
        const storedUser = localStorage.getItem('jarvis_current_user');
        const storedNav = localStorage.getItem('jarvis_navigation');
        
        if (storedUser) {
          try {
            setUser(JSON.parse(storedUser));
          } catch (e) {
            console.error('Failed to parse stored user:', e);
          }
        }
        
        if (storedNav) {
          try {
            setNavigation(JSON.parse(storedNav));
          } catch (e) {
            console.error('Failed to parse stored navigation:', e);
          }
        }
      }
      
    } catch (error) {
      console.error('Failed to initialize auth:', error);
    } finally {
      setIsLoading(false);
    }
  }, [scheduleTokenRefresh]);

  const loadCurrentUser = async () => {
    try {
      const userResult = await apiService.getCurrentUser();
      
      if (userResult.data) {
        setUser(userResult.data);
        // Store user data for offline access
        localStorage.setItem('jarvis_current_user', JSON.stringify(userResult.data));
        
        // Load navigation for the user
        const navResult = await apiService.getNavigation();
        if (navResult.data) {
          setNavigation(navResult.data);
          // Store navigation for offline access
          localStorage.setItem('jarvis_navigation', JSON.stringify(navResult.data));
        }
      }
    } catch (error) {
      console.error('Failed to load user:', error);
      throw error; // Re-throw to be caught by Promise.race
    }
  };

  const login = async (credentials: LoginCredentials) => {
    console.log('AuthContext: Starting login...');
    const result = await apiService.login(credentials);
    
    if (result.error) {
      console.error('AuthContext: Login error', result.error);
      throw new Error(result.error.message);
    }

    if (result.data) {
      console.log('AuthContext: Login successful, setting user:', result.data.user);
      setUser(result.data.user);
      // Store user data for offline access
      localStorage.setItem('jarvis_current_user', JSON.stringify(result.data.user));
      
      // Load navigation for the newly logged in user
      const navResult = await apiService.getNavigation();
      if (navResult.data) {
        console.log('AuthContext: Setting navigation:', navResult.data);
        setNavigation(navResult.data);
        // Store navigation for offline access
        localStorage.setItem('jarvis_navigation', JSON.stringify(navResult.data));
      }
      
      // Schedule token refresh
      scheduleTokenRefresh(result.data.accessToken);
    }
  };

  const hasPermission = (resource: string, action: string = 'read'): boolean => {
    if (!user) return false;
    return apiService.hasPermission(user, resource, action);
  };

  const refreshAuth = async () => {
    await loadCurrentUser();
  };

  const value: AuthContextType = {
    user,
    isLoading,
    isAuthenticated: !!user,
    navigation,
    login,
    logout,
    hasPermission,
    refreshAuth
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}