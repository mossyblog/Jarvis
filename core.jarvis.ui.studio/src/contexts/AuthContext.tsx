import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { User, LoginCredentials, NavigationItem } from '../services/api/types';
import { apiService } from '../services/api/apiService';

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

  // Load user and navigation on mount
  useEffect(() => {
    loadCurrentUser();
  }, []);

  const loadCurrentUser = async () => {
    try {
      setIsLoading(true);
      const userResult = await apiService.getCurrentUser();
      
      if (userResult.data) {
        setUser(userResult.data);
        
        // Load navigation for the user
        const navResult = await apiService.getNavigation();
        if (navResult.data) {
          setNavigation(navResult.data);
        }
      }
    } catch (error) {
      console.error('Failed to load user:', error);
    } finally {
      setIsLoading(false);
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
      
      // Load navigation for the newly logged in user
      const navResult = await apiService.getNavigation();
      if (navResult.data) {
        console.log('AuthContext: Setting navigation:', navResult.data);
        setNavigation(navResult.data);
      }
    }
  };

  const logout = async () => {
    await apiService.logout();
    setUser(null);
    setNavigation([]);
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