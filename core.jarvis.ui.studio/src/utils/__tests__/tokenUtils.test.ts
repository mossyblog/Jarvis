import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import {
  decodeToken,
  isTokenExpired,
  getTokenExpiry,
  getTimeUntilExpiry,
  storeTokens,
  getStoredTokens,
  clearTokens,
  shouldPersistTokens,
  setDevMode,
  ACCESS_TOKEN_KEY,
  REFRESH_TOKEN_KEY,
  TOKEN_REFRESH_BUFFER
} from '../tokenUtils'

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
}

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  writable: true
})

// Helper function to create a JWT token
const createJWT = (payload: Record<string, any>): string => {
  const header = { alg: 'HS256', typ: 'JWT' }
  const encodedHeader = btoa(JSON.stringify(header))
  const encodedPayload = btoa(JSON.stringify(payload))
  const signature = 'signature'
  
  return `${encodedHeader}.${encodedPayload}.${signature}`
}

describe('tokenUtils', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Reset import.meta.env mock
    vi.stubGlobal('import.meta', {
      env: {
        DEV: false
      }
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  describe('decodeToken', () => {
    it('should decode a valid JWT token', () => {
      const payload = { userId: '123', exp: 1234567890 }
      const token = createJWT(payload)
      
      const decoded = decodeToken(token)
      
      expect(decoded).toEqual(payload)
    })

    it('should return null for invalid token format', () => {
      const invalidTokens = [
        'invalid',
        'invalid.token',
        'invalid.token.signature.extra',
        ''
      ]
      
      invalidTokens.forEach(token => {
        expect(decodeToken(token)).toBeNull()
      })
    })

    it('should return null for malformed base64', () => {
      const token = 'header.invalid-base64.signature'
      
      expect(decodeToken(token)).toBeNull()
    })

    it('should return null for invalid JSON in payload', () => {
      const header = btoa(JSON.stringify({ alg: 'HS256' }))
      const payload = btoa('invalid-json{')
      const token = `${header}.${payload}.signature`
      
      expect(decodeToken(token)).toBeNull()
    })

    it('should handle special characters in base64 payload', () => {
      const payload = { test: 'value with special chars +/=' }
      const token = createJWT(payload)
      
      const decoded = decodeToken(token)
      
      expect(decoded).toEqual(payload)
    })
  })

  describe('isTokenExpired', () => {
    it('should return false for non-expired token', () => {
      const futureTime = Math.floor(Date.now() / 1000) + 3600 // 1 hour from now
      const token = createJWT({ exp: futureTime })
      
      expect(isTokenExpired(token)).toBe(false)
    })

    it('should return true for expired token', () => {
      const pastTime = Math.floor(Date.now() / 1000) - 3600 // 1 hour ago
      const token = createJWT({ exp: pastTime })
      
      expect(isTokenExpired(token)).toBe(true)
    })

    it('should return true for token expiring within buffer time', () => {
      const nearFutureTime = Math.floor(Date.now() / 1000) + 60 // 1 minute from now
      const token = createJWT({ exp: nearFutureTime })
      
      expect(isTokenExpired(token, TOKEN_REFRESH_BUFFER)).toBe(true)
    })

    it('should use custom buffer time', () => {
      const nearFutureTime = Math.floor(Date.now() / 1000) + 30 // 30 seconds from now
      const token = createJWT({ exp: nearFutureTime })
      const customBuffer = 60 * 1000 // 1 minute buffer
      
      expect(isTokenExpired(token, customBuffer)).toBe(true)
    })

    it('should return true for token without expiry', () => {
      const token = createJWT({ userId: '123' })
      
      expect(isTokenExpired(token)).toBe(true)
    })

    it('should return true for invalid token', () => {
      expect(isTokenExpired('invalid-token')).toBe(true)
    })

    it('should use default buffer when not specified', () => {
      const futureTime = Math.floor(Date.now() / 1000) + 60 // 1 minute from now
      const token = createJWT({ exp: futureTime })
      
      // Should use TOKEN_REFRESH_BUFFER (5 minutes) by default
      expect(isTokenExpired(token)).toBe(true)
    })
  })

  describe('getTokenExpiry', () => {
    it('should return expiry time in milliseconds', () => {
      const expTime = 1234567890
      const token = createJWT({ exp: expTime })
      
      expect(getTokenExpiry(token)).toBe(expTime * 1000)
    })

    it('should return null for token without expiry', () => {
      const token = createJWT({ userId: '123' })
      
      expect(getTokenExpiry(token)).toBeNull()
    })

    it('should return null for invalid token', () => {
      expect(getTokenExpiry('invalid-token')).toBeNull()
    })
  })

  describe('getTimeUntilExpiry', () => {
    it('should return time until expiry in milliseconds', () => {
      const futureTime = Math.floor(Date.now() / 1000) + 3600 // 1 hour from now
      const token = createJWT({ exp: futureTime })
      
      const timeUntilExpiry = getTimeUntilExpiry(token)
      
      // Should be approximately 1 hour (allowing for small timing differences)
      expect(timeUntilExpiry).toBeGreaterThan(3590000) // 59 minutes 50 seconds
      expect(timeUntilExpiry).toBeLessThan(3610000) // 1 hour 10 seconds
    })

    it('should return 0 for expired token', () => {
      const pastTime = Math.floor(Date.now() / 1000) - 3600 // 1 hour ago
      const token = createJWT({ exp: pastTime })
      
      expect(getTimeUntilExpiry(token)).toBe(0)
    })

    it('should return 0 for token without expiry', () => {
      const token = createJWT({ userId: '123' })
      
      expect(getTimeUntilExpiry(token)).toBe(0)
    })

    it('should return 0 for invalid token', () => {
      expect(getTimeUntilExpiry('invalid-token')).toBe(0)
    })
  })

  describe('storeTokens', () => {
    it('should store access token in localStorage', () => {
      const accessToken = 'access-token'
      
      storeTokens(accessToken)
      
      expect(localStorageMock.setItem).toHaveBeenCalledWith(ACCESS_TOKEN_KEY, accessToken)
    })

    it('should store both access and refresh tokens', () => {
      const accessToken = 'access-token'
      const refreshToken = 'refresh-token'
      
      storeTokens(accessToken, refreshToken)
      
      expect(localStorageMock.setItem).toHaveBeenCalledWith(ACCESS_TOKEN_KEY, accessToken)
      expect(localStorageMock.setItem).toHaveBeenCalledWith(REFRESH_TOKEN_KEY, refreshToken)
    })

    it('should not store refresh token if not provided', () => {
      const accessToken = 'access-token'
      
      storeTokens(accessToken)
      
      expect(localStorageMock.setItem).toHaveBeenCalledWith(ACCESS_TOKEN_KEY, accessToken)
      expect(localStorageMock.setItem).not.toHaveBeenCalledWith(REFRESH_TOKEN_KEY, expect.anything())
    })

    it('should store refresh token if provided as empty string', () => {
      const accessToken = 'access-token'
      const refreshToken = ''
      
      storeTokens(accessToken, refreshToken)
      
      expect(localStorageMock.setItem).toHaveBeenCalledWith(ACCESS_TOKEN_KEY, accessToken)
      expect(localStorageMock.setItem).not.toHaveBeenCalledWith(REFRESH_TOKEN_KEY, refreshToken)
    })
  })

  describe('getStoredTokens', () => {
    it('should retrieve stored tokens', () => {
      const accessToken = 'stored-access-token'
      const refreshToken = 'stored-refresh-token'
      
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === ACCESS_TOKEN_KEY) return accessToken
        if (key === REFRESH_TOKEN_KEY) return refreshToken
        return null
      })
      
      const tokens = getStoredTokens()
      
      expect(tokens).toEqual({
        accessToken,
        refreshToken
      })
    })

    it('should return null for missing tokens', () => {
      localStorageMock.getItem.mockReturnValue(null)
      
      const tokens = getStoredTokens()
      
      expect(tokens).toEqual({
        accessToken: null,
        refreshToken: null
      })
    })

    it('should handle partial token storage', () => {
      const accessToken = 'stored-access-token'
      
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === ACCESS_TOKEN_KEY) return accessToken
        return null
      })
      
      const tokens = getStoredTokens()
      
      expect(tokens).toEqual({
        accessToken,
        refreshToken: null
      })
    })
  })

  describe('clearTokens', () => {
    it('should remove both tokens from localStorage', () => {
      clearTokens()
      
      expect(localStorageMock.removeItem).toHaveBeenCalledWith(ACCESS_TOKEN_KEY)
      expect(localStorageMock.removeItem).toHaveBeenCalledWith(REFRESH_TOKEN_KEY)
    })
  })

  describe('shouldPersistTokens', () => {
    it('should return true in development mode', () => {
      vi.stubGlobal('import.meta', {
        env: {
          DEV: true
        }
      })
      
      expect(shouldPersistTokens()).toBe(true)
    })

    it('should return false in production mode by default', () => {
      vi.stubGlobal('import.meta', {
        env: {
          DEV: false
        }
      })
      
      localStorageMock.getItem.mockReturnValue(null)
      
      expect(shouldPersistTokens()).toBe(false)
    })

    it('should check localStorage dev mode flag in production', () => {
      vi.stubGlobal('import.meta', {
        env: {
          DEV: false
        }
      })
      
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'jarvis_dev_mode') return 'true'
        return null
      })
      
      expect(shouldPersistTokens()).toBe(true)
    })

    it('should return false for invalid dev mode flag', () => {
      vi.stubGlobal('import.meta', {
        env: {
          DEV: false
        }
      })
      
      localStorageMock.getItem.mockImplementation((key) => {
        if (key === 'jarvis_dev_mode') return 'false'
        return null
      })
      
      expect(shouldPersistTokens()).toBe(false)
    })
  })

  describe('setDevMode', () => {
    it('should enable dev mode', () => {
      setDevMode(true)
      
      expect(localStorageMock.setItem).toHaveBeenCalledWith('jarvis_dev_mode', 'true')
    })

    it('should disable dev mode', () => {
      setDevMode(false)
      
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('jarvis_dev_mode')
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('should handle localStorage errors gracefully', () => {
      // Mock localStorage to throw errors
      localStorageMock.setItem.mockImplementation(() => {
        throw new Error('Storage quota exceeded')
      })
      
      // Should not throw
      expect(() => storeTokens('token')).not.toThrow()
    })

    it('should handle malformed base64 padding', () => {
      // Create token with payload that has padding issues
      const payload = { test: 'value' }
      const encodedPayload = btoa(JSON.stringify(payload)).replace(/=/g, '') // Remove padding
      const token = `header.${encodedPayload}.signature`
      
      const decoded = decodeToken(token)
      expect(decoded).toEqual(payload)
    })

    it('should handle tokens with no payload section', () => {
      const token = 'header..signature'
      
      expect(decodeToken(token)).toBeNull()
    })

    it('should handle very large expiry times', () => {
      const largeExpiry = Math.pow(2, 31) - 1 // Max 32-bit signed integer
      const token = createJWT({ exp: largeExpiry })
      
      expect(getTokenExpiry(token)).toBe(largeExpiry * 1000)
      expect(isTokenExpired(token)).toBe(false)
    })

    it('should handle zero expiry time', () => {
      const token = createJWT({ exp: 0 })
      
      expect(getTokenExpiry(token)).toBe(0)
      expect(isTokenExpired(token)).toBe(true)
    })

    it('should handle negative expiry time', () => {
      const token = createJWT({ exp: -1 })
      
      expect(getTokenExpiry(token)).toBe(-1000)
      expect(isTokenExpired(token)).toBe(true)
    })

    it('should handle tokens with string expiry', () => {
      const token = createJWT({ exp: '1234567890' })
      
      // Should handle string expiry by converting to number
      const decoded = decodeToken(token)
      expect(decoded?.exp).toBe('1234567890')
      
      // But expiry functions should handle this gracefully
      expect(getTokenExpiry(token)).toBeNull()
    })

    it('should handle localStorage returning undefined', () => {
      localStorageMock.getItem.mockReturnValue(undefined as any)
      
      const tokens = getStoredTokens()
      
      expect(tokens).toEqual({
        accessToken: null,
        refreshToken: null
      })
    })
  })

  describe('Constants', () => {
    it('should have correct token key constants', () => {
      expect(ACCESS_TOKEN_KEY).toBe('jarvis_auth_token')
      expect(REFRESH_TOKEN_KEY).toBe('jarvis_refresh_token')
    })

    it('should have correct refresh buffer constant', () => {
      expect(TOKEN_REFRESH_BUFFER).toBe(5 * 60 * 1000) // 5 minutes in milliseconds
    })
  })

  describe('Real-world Scenarios', () => {
    it('should handle token refresh workflow', () => {
      // Store initial tokens
      storeTokens('old-access-token', 'refresh-token')
      
      // Check if refresh is needed
      const oldToken = createJWT({ exp: Math.floor(Date.now() / 1000) + 60 }) // 1 minute left
      expect(isTokenExpired(oldToken)).toBe(true) // Should refresh due to buffer
      
      // Store new tokens after refresh
      storeTokens('new-access-token', 'new-refresh-token')
      
      const tokens = getStoredTokens()
      expect(tokens.accessToken).toBe('new-access-token')
      expect(tokens.refreshToken).toBe('new-refresh-token')
    })

    it('should handle logout workflow', () => {
      // Store tokens during session
      storeTokens('access-token', 'refresh-token')
      
      // Clear on logout
      clearTokens()
      
      // Verify tokens are cleared
      const tokens = getStoredTokens()
      expect(tokens.accessToken).toBeNull()
      expect(tokens.refreshToken).toBeNull()
    })

    it('should handle session check workflow', () => {
      const validToken = createJWT({ exp: Math.floor(Date.now() / 1000) + 3600 })
      storeTokens(validToken)
      
      const tokens = getStoredTokens()
      const isValid = tokens.accessToken && !isTokenExpired(tokens.accessToken)
      
      expect(isValid).toBe(true)
    })
  })
})