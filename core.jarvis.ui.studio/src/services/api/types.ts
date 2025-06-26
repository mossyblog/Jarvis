export interface User {
  id: string;
  email: string;
  name: string;
  avatar?: string;
  roles: Role[];
  preferences?: UserPreferences;
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  permissions: Permission[];
}

export interface Permission {
  id: string;
  resource: string;
  actions: string[];
}

export interface NavigationItem {
  id: string;
  label: string;
  icon: string;
  href: string;
  requiredPermission?: string;
  requiredAction?: string;
  children?: NavigationItem[];
  badge?: {
    value: string | number;
    variant?: 'default' | 'success' | 'warning' | 'error';
  };
}

export interface UserPreferences {
  theme?: 'light' | 'dark' | 'system';
  sidebarBehavior?: 'expandable' | 'open' | 'closed';
  defaultProject?: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface AuthToken {
  id: string;
  ownerEntityId: string;
  accessToken: string;
  refreshToken: string;
  refreshTokenHash?: string;
  expiresAt: string;
  refreshExpiresAt: string;
  tokenType: string;
  sessionId: string;
  clientId?: string;
  ipAddress?: string;
  userAgent?: string;
  isRevoked: boolean;
  revokedAt?: string;
  issuedAt: string;
}

export interface ApiError {
  message: string;
  code?: string;
  details?: Record<string, any>;
}

export type ApiResponse<T> = {
  data: T;
  error?: never;
} | {
  data?: never;
  error: ApiError;
};