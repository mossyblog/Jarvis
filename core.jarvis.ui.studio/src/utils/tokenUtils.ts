interface TokenPayload {
  exp?: number;
  iat?: number;
  [key: string]: string | number | boolean | undefined;
}

export const REFRESH_TOKEN_KEY = 'jarvis_refresh_token';
export const ACCESS_TOKEN_KEY = 'jarvis_auth_token';
export const TOKEN_REFRESH_BUFFER = 5 * 60 * 1000; // 5 minutes before expiry

/**
 * Decode a JWT token without verifying signature
 * This is safe for client-side expiration checking
 */
export function decodeToken(token: string): TokenPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    
    const payload = parts[1];
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded);
  } catch {
    return null;
  }
}

/**
 * Check if a token is expired or about to expire
 * @param token JWT token
 * @param bufferMs Buffer time in milliseconds before actual expiry
 */
export function isTokenExpired(token: string, bufferMs: number = TOKEN_REFRESH_BUFFER): boolean {
  const payload = decodeToken(token);
  if (!payload || !payload.exp) return true;
  
  const expiryTime = payload.exp * 1000; // Convert to milliseconds
  const currentTime = Date.now();
  
  return currentTime >= (expiryTime - bufferMs);
}

/**
 * Get token expiration time in milliseconds
 */
export function getTokenExpiry(token: string): number | null {
  const payload = decodeToken(token);
  if (!payload || !payload.exp) return null;
  
  return payload.exp * 1000; // Convert to milliseconds
}

/**
 * Calculate time until token expires in milliseconds
 */
export function getTimeUntilExpiry(token: string): number {
  const expiry = getTokenExpiry(token);
  if (!expiry) return 0;
  
  const timeLeft = expiry - Date.now();
  return Math.max(0, timeLeft);
}

/**
 * Store tokens in localStorage with development mode consideration
 */
export function storeTokens(accessToken: string, refreshToken?: string): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  
  if (refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }
}

/**
 * Get stored tokens from localStorage
 */
export function getStoredTokens(): { accessToken: string | null; refreshToken: string | null } {
  return {
    accessToken: localStorage.getItem(ACCESS_TOKEN_KEY),
    refreshToken: localStorage.getItem(REFRESH_TOKEN_KEY)
  };
}

/**
 * Clear all stored tokens
 */
export function clearTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

/**
 * Check if we should persist tokens (development mode)
 */
export function shouldPersistTokens(): boolean {
  // In development, always persist
  if (import.meta.env.DEV) return true;
  
  // Check for explicit dev mode flag
  const devMode = localStorage.getItem('jarvis_dev_mode');
  return devMode === 'true';
}

/**
 * Enable/disable development mode for token persistence
 */
export function setDevMode(enabled: boolean): void {
  if (enabled) {
    localStorage.setItem('jarvis_dev_mode', 'true');
  } else {
    localStorage.removeItem('jarvis_dev_mode');
  }
}