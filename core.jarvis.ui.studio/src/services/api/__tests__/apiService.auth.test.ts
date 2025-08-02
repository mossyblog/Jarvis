import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { createApiService, MockApiService, RealApiService } from '../apiService'
import { mockUsers } from '../mockData'
import type { LoginCredentials, User, NavigationItem } from '../types'

// Mock dependencies
vi.mock('../../../utils/tokenUtils')

const mockTokenUtils = await import('../../../utils/tokenUtils')

// Mock fetch for RealApiService tests
const mockFetch = vi.fn()
global.fetch = mockFetch

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

describe('ApiService Authentication', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Reset environment variables
    vi.stubGlobal('import.meta', {
      env: {
        VITE_USE_MOCK_API: 'true',
        VITE_API_URL: undefined
      }
    })

    // Mock console methods
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  describe('MockApiService', () => {
    let mockService: MockApiService

    beforeEach(() => {
      mockService = new MockApiService()
    })

    describe('login', () => {
      it('should authenticate user with valid credentials', async () => {
        const credentials: LoginCredentials = {
          email: 'admin@example.com',
          password: 'any-password'
        }

        const result = await mockService.login(credentials)

        expect(result.data).toBeDefined()
        expect(result.data?.user.email).toBe(credentials.email)
        expect(result.data?.accessToken).toBeDefined()
        expect(result.data?.refreshToken).toBeDefined()
        expect(result.data?.expiresIn).toBe(3600)
      })

      it('should reject invalid email', async () => {
        const credentials: LoginCredentials = {
          email: 'nonexistent@example.com',
          password: 'password'
        }

        const result = await mockService.login(credentials)

        expect(result.error).toBeDefined()
        expect(result.error?.message).toBe('Invalid credentials')
        expect(result.error?.code).toBe('AUTH_INVALID_CREDENTIALS')
      })

      it('should store tokens after successful login', async () => {
        const credentials: LoginCredentials = {
          email: 'admin@example.com',
          password: 'password'
        }

        await mockService.login(credentials)

        expect(mockTokenUtils.storeTokens).toHaveBeenCalled()
        expect(localStorageMock.setItem).toHaveBeenCalledWith(
          'jarvis_current_user',
          expect.stringContaining('admin@example.com')
        )
      })

      it('should simulate network delay', async () => {
        const startTime = Date.now()
        
        await mockService.login({
          email: 'admin@example.com',
          password: 'password'
        })
        
        const endTime = Date.now()
        
        // Should take at least 500ms (MOCK_DELAY)
        expect(endTime - startTime).toBeGreaterThanOrEqual(500)
      })

      it('should generate unique tokens for each login', async () => {
        const credentials: LoginCredentials = {
          email: 'admin@example.com',
          password: 'password'
        }

        const result1 = await mockService.login(credentials)
        const result2 = await mockService.login(credentials)

        expect(result1.data?.accessToken).not.toBe(result2.data?.accessToken)
        expect(result1.data?.refreshToken).not.toBe(result2.data?.refreshToken)
      })
    })

    describe('logout', () => {
      it('should clear stored tokens', async () => {
        await mockService.logout()

        expect(mockTokenUtils.clearTokens).toHaveBeenCalled()
        expect(localStorageMock.removeItem).toHaveBeenCalledWith('jarvis_current_user')
      })

      it('should simulate network delay', async () => {
        const startTime = Date.now()
        
        await mockService.logout()
        
        const endTime = Date.now()
        
        expect(endTime - startTime).toBeGreaterThanOrEqual(500)
      })
    })

    describe('getCurrentUser', () => {
      it('should return user when tokens exist', async () => {
        // Mock stored tokens and user
        const mockUser = mockUsers[0]
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'valid-token',
          refreshToken: 'refresh-token'
        })
        localStorageMock.getItem.mockReturnValue(JSON.stringify(mockUser))

        const result = await mockService.getCurrentUser()

        expect(result.data).toEqual(mockUser)
      })

      it('should return null when no tokens exist', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: null,
          refreshToken: null
        })

        const result = await mockService.getCurrentUser()

        expect(result.data).toBeNull()
      })

      it('should return null when user data is corrupted', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'valid-token',
          refreshToken: 'refresh-token'
        })
        localStorageMock.getItem.mockReturnValue('invalid-json{')

        const result = await mockService.getCurrentUser()

        expect(result.data).toBeNull()
      })
    })

    describe('refreshToken', () => {
      it('should refresh tokens for valid user', async () => {
        // Setup existing user
        const mockUser = mockUsers[0]
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'old-token',
          refreshToken: 'refresh-token'
        })
        localStorageMock.getItem.mockReturnValue(JSON.stringify(mockUser))

        const result = await mockService.refreshToken('refresh-token')

        expect(result.data).toBeDefined()
        expect(result.data?.user).toEqual(mockUser)
        expect(result.data?.accessToken).toBeDefined()
        expect(result.data?.refreshToken).toBeDefined()
        expect(mockTokenUtils.storeTokens).toHaveBeenCalled()
      })

      it('should fail when no current user exists', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: null,
          refreshToken: null
        })
        localStorageMock.getItem.mockReturnValue(null)

        const result = await mockService.refreshToken('invalid-token')

        expect(result.error).toBeDefined()
        expect(result.error?.message).toBe('Invalid refresh token')
        expect(result.error?.code).toBe('AUTH_INVALID_TOKEN')
      })
    })

    describe('getNavigation', () => {
      it('should return filtered navigation for user', async () => {
        // Setup admin user
        const adminUser = mockUsers.find(u => u.email === 'admin@example.com')!
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'valid-token',
          refreshToken: 'refresh-token'
        })
        localStorageMock.getItem.mockReturnValue(JSON.stringify(adminUser))

        const result = await mockService.getNavigation()

        expect(result.data).toBeDefined()
        expect(Array.isArray(result.data)).toBe(true)
        expect(result.data!.length).toBeGreaterThan(0)
      })

      it('should return empty navigation for unauthenticated user', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: null,
          refreshToken: null
        })

        const result = await mockService.getNavigation()

        expect(result.data).toEqual([])
      })
    })

    describe('hasPermission', () => {
      const adminUser = mockUsers.find(u => u.email === 'admin@example.com')!
      const regularUser = mockUsers.find(u => u.email === 'user@example.com')!

      it('should grant access for admin with wildcard permissions', () => {
        const hasAccess = mockService.hasPermission(adminUser, 'any-resource', 'any-action')
        expect(hasAccess).toBe(true)
      })

      it('should check specific permissions', () => {
        const hasAccess = mockService.hasPermission(regularUser, 'users', 'read')
        expect(hasAccess).toBe(true)
      })

      it('should deny access for insufficient permissions', () => {
        const hasAccess = mockService.hasPermission(regularUser, 'admin', 'write')
        expect(hasAccess).toBe(false)
      })

      it('should use default action "read"', () => {
        const hasAccess = mockService.hasPermission(regularUser, 'users')
        expect(hasAccess).toBe(true)
      })
    })
  })

  describe('RealApiService', () => {
    let realService: RealApiService

    beforeEach(() => {
      vi.stubGlobal('import.meta', {
        env: {
          VITE_USE_MOCK_API: 'false',
          VITE_API_URL: 'http://test-api.com'
        }
      })
      realService = new RealApiService()
    })

    describe('login', () => {
      it('should handle successful login with PascalCase response', async () => {
        const mockResponse = {
          AccessToken: 'access-token',
          RefreshToken: 'refresh-token',
          OwnerEntityId: 'user-id'
        }

        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve(mockResponse)
        })

        const credentials: LoginCredentials = {
          email: 'test@example.com',
          password: 'password'
        }

        const result = await realService.login(credentials)

        expect(result.data).toBeDefined()
        expect(result.data?.user.email).toBe(credentials.email)
        expect(result.data?.accessToken).toBe('access-token')
        expect(result.data?.refreshToken).toBe('refresh-token')
        expect(mockFetch).toHaveBeenCalledWith('/api/security/auth', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(credentials)
        })
      })

      it('should handle successful login with camelCase response', async () => {
        const mockResponse = {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          ownerEntityId: 'user-id'
        }

        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve(mockResponse)
        })

        const result = await realService.login({
          email: 'test@example.com',
          password: 'password'
        })

        expect(result.data?.accessToken).toBe('access-token')
        expect(result.data?.refreshToken).toBe('refresh-token')
      })

      it('should handle 401 authentication failure', async () => {
        mockFetch.mockResolvedValue({
          status: 401,
          ok: false,
          json: () => Promise.resolve({ message: 'Invalid credentials' })
        })

        const result = await realService.login({
          email: 'wrong@example.com',
          password: 'wrongpassword'
        })

        expect(result.error).toBeDefined()
        expect(result.error?.message).toBe('Invalid credentials')
        expect(result.error?.code).toBe('AUTH_FAILED')
      })

      it('should handle network errors', async () => {
        mockFetch.mockRejectedValue(new Error('Network error'))

        const result = await realService.login({
          email: 'test@example.com',
          password: 'password'
        })

        expect(result.error).toBeDefined()
        expect(result.error?.message).toBe('Network error')
        expect(result.error?.code).toBe('NETWORK_ERROR')
      })

      it('should handle malformed response', async () => {
        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve({}) // Empty response
        })

        const result = await realService.login({
          email: 'test@example.com',
          password: 'password'
        })

        expect(result.error).toBeDefined()
        expect(result.error?.message).toBe('Authentication failed')
      })

      it('should store tokens and user data on success', async () => {
        const mockResponse = {
          AccessToken: 'access-token',
          RefreshToken: 'refresh-token',
          OwnerEntityId: 'user-id'
        }

        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve(mockResponse)
        })

        await realService.login({
          email: 'test@example.com',
          password: 'password'
        })

        expect(mockTokenUtils.storeTokens).toHaveBeenCalledWith('access-token', 'refresh-token')
        expect(localStorageMock.setItem).toHaveBeenCalledWith(
          'jarvis_current_user',
          expect.stringContaining('test@example.com')
        )
      })
    })

    describe('logout', () => {
      it('should call logout endpoint with auth headers', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'access-token',
          refreshToken: 'refresh-token'
        })

        mockFetch.mockResolvedValue({ ok: true })

        await realService.logout()

        expect(mockFetch).toHaveBeenCalledWith('/api/security/deauth', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer access-token'
          }
        })

        expect(mockTokenUtils.clearTokens).toHaveBeenCalled()
        expect(localStorageMock.removeItem).toHaveBeenCalledWith('jarvis_current_user')
      })

      it('should clear tokens even if API call fails', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'access-token',
          refreshToken: 'refresh-token'
        })

        mockFetch.mockRejectedValue(new Error('Network error'))

        await realService.logout()

        expect(mockTokenUtils.clearTokens).toHaveBeenCalled()
        expect(localStorageMock.removeItem).toHaveBeenCalledWith('jarvis_current_user')
      })

      it('should handle logout without tokens', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: null,
          refreshToken: null
        })

        await realService.logout()

        expect(mockFetch).not.toHaveBeenCalled()
        expect(mockTokenUtils.clearTokens).toHaveBeenCalled()
      })
    })

    describe('getCurrentUser', () => {
      it('should return user from localStorage when available', async () => {
        const mockUser = { id: '1', email: 'test@example.com', name: 'Test User', roles: [] }
        
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'access-token',
          refreshToken: 'refresh-token'
        })
        localStorageMock.getItem.mockReturnValue(JSON.stringify(mockUser))

        const result = await realService.getCurrentUser()

        expect(result.data).toEqual(mockUser)
      })

      it('should return null when no tokens exist', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: null,
          refreshToken: null
        })

        const result = await realService.getCurrentUser()

        expect(result.data).toBeNull()
      })

      it('should clear invalid stored user data', async () => {
        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'access-token',
          refreshToken: 'refresh-token'
        })
        localStorageMock.getItem.mockReturnValue('invalid-json')

        const result = await realService.getCurrentUser()

        expect(result.data).toBeNull()
        expect(localStorageMock.removeItem).toHaveBeenCalledWith('jarvis_current_user')
        expect(mockTokenUtils.clearTokens).toHaveBeenCalled()
      })
    })

    describe('refreshToken', () => {
      it('should refresh tokens successfully', async () => {
        const mockResponse = {
          AccessToken: 'new-access-token',
          RefreshToken: 'new-refresh-token'
        }

        const mockUser = { id: '1', email: 'test@example.com', name: 'Test', roles: [] }

        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve(mockResponse)
        })

        mockTokenUtils.getStoredTokens.mockReturnValue({
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token'
        })
        localStorageMock.getItem.mockReturnValue(JSON.stringify(mockUser))

        const result = await realService.refreshToken('old-refresh-token')

        expect(result.data).toBeDefined()
        expect(result.data?.accessToken).toBe('new-access-token')
        expect(result.data?.refreshToken).toBe('new-refresh-token')
        expect(result.data?.user).toEqual(mockUser)

        expect(mockFetch).toHaveBeenCalledWith('/api/security/refresh', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: 'old-refresh-token' })
        })
      })

      it('should handle refresh token failure', async () => {
        mockFetch.mockResolvedValue({
          ok: false,
          status: 401,
          json: () => Promise.resolve({ message: 'Invalid refresh token' })
        })

        const result = await realService.refreshToken('invalid-token')

        expect(result.error).toBeDefined()
        expect(result.error?.message).toBe('Invalid refresh token')
      })
    })

    describe('getNavigation', () => {
      it('should fetch navigation via GraphQL', async () => {
        const mockGraphQLResponse = {
          data: {
            navigation_item_componentCollection: {
              edges: [
                {
                  node: {
                    id: '1',
                    label: 'Dashboard',
                    icon: 'LayoutDashboard',
                    href: '/',
                    is_active: true,
                    sort_order: 1
                  }
                }
              ]
            }
          }
        }

        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve(mockGraphQLResponse)
        })

        const result = await realService.getNavigation()

        expect(result.data).toBeDefined()
        expect(result.data!.length).toBe(1)
        expect(result.data![0].label).toBe('Dashboard')

        expect(mockFetch).toHaveBeenCalledWith('/api/graphql', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: expect.stringContaining('navigation_item_componentCollection')
        })
      })

      it('should return fallback navigation on GraphQL failure', async () => {
        mockFetch.mockResolvedValue({
          ok: false,
          status: 500,
          json: () => Promise.resolve({ error: 'Server error' })
        })

        const result = await realService.getNavigation()

        expect(result.data).toBeDefined()
        expect(result.data!.length).toBeGreaterThan(0)
        // Should return fallback navigation
        expect(result.data!.some(item => item.label === 'Dashboard')).toBe(true)
      })

      it('should filter inactive navigation items', async () => {
        const mockGraphQLResponse = {
          data: {
            navigation_item_componentCollection: {
              edges: [
                {
                  node: {
                    id: '1',
                    label: 'Active Item',
                    href: '/active',
                    is_active: true,
                    sort_order: 1
                  }
                },
                {
                  node: {
                    id: '2',
                    label: 'Inactive Item',
                    href: '/inactive',
                    is_active: false,
                    sort_order: 2
                  }
                }
              ]
            }
          }
        }

        mockFetch.mockResolvedValue({
          ok: true,
          json: () => Promise.resolve(mockGraphQLResponse)
        })

        const result = await realService.getNavigation()

        expect(result.data!.length).toBe(1)
        expect(result.data![0].label).toBe('Active Item')
      })
    })

    describe('hasPermission', () => {
      const adminUser: User = {
        id: '1',
        email: 'admin@example.com',
        name: 'Admin',
        roles: [{
          id: 'admin',
          name: 'Administrator',
          permissions: [{
            id: 'all',
            resource: '*',
            actions: ['*']
          }]
        }]
      }

      const regularUser: User = {
        id: '2',
        email: 'user@example.com',
        name: 'User',
        roles: [{
          id: 'user',
          name: 'User',
          permissions: [{
            id: 'read-users',
            resource: 'users',
            actions: ['read']
          }]
        }]
      }

      it('should grant wildcard permissions', () => {
        expect(realService.hasPermission(adminUser, 'any-resource', 'write')).toBe(true)
      })

      it('should check specific permissions', () => {
        expect(realService.hasPermission(regularUser, 'users', 'read')).toBe(true)
        expect(realService.hasPermission(regularUser, 'users', 'write')).toBe(false)
      })

      it('should use default action', () => {
        expect(realService.hasPermission(regularUser, 'users')).toBe(true)
      })
    })
  })

  describe('Factory Function', () => {
    it('should create MockApiService when VITE_USE_MOCK_API is true', () => {
      vi.stubGlobal('import.meta', {
        env: {
          VITE_USE_MOCK_API: 'true'
        }
      })

      const service = createApiService()
      expect(service).toBeInstanceOf(MockApiService)
    })

    it('should create RealApiService when VITE_USE_MOCK_API is false', () => {
      vi.stubGlobal('import.meta', {
        env: {
          VITE_USE_MOCK_API: 'false'
        }
      })

      const service = createApiService()
      expect(service).toBeInstanceOf(RealApiService)
    })

    it('should default to MockApiService when env var is undefined', () => {
      vi.stubGlobal('import.meta', {
        env: {}
      })

      const service = createApiService()
      expect(service).toBeInstanceOf(MockApiService)
    })
  })
})