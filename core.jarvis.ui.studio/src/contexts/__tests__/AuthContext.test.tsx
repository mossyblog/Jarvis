import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider, useAuth } from '../AuthContext'
import { apiService } from '../../services/api/apiService'
import { clearTokens, storeTokens } from '../../utils/tokenUtils'
import type { User, LoginCredentials, NavigationItem } from '../../services/api/types'

// Mock dependencies
vi.mock('../../services/api/apiService')
vi.mock('../../utils/tokenUtils')

// Set environment to use mock API
vi.stubGlobal('import.meta', {
  env: {
    VITE_USE_MOCK_API: 'true'
  }
})

const mockApiService = vi.mocked(apiService)
const mockClearTokens = vi.mocked(clearTokens)
const mockStoreTokens = vi.mocked(storeTokens)

// Mock token utilities with default implementations
vi.mocked(await import('../../utils/tokenUtils')).getStoredTokens.mockReturnValue({
  accessToken: null,
  refreshToken: null
})
vi.mocked(await import('../../utils/tokenUtils')).isTokenExpired.mockReturnValue(true)
vi.mocked(await import('../../utils/tokenUtils')).getTimeUntilExpiry.mockReturnValue(0)

// Test component to use the AuthContext
const TestComponent = ({ onAuthStateChange }: { onAuthStateChange?: (auth: any) => void }) => {
  const auth = useAuth()
  
  React.useEffect(() => {
    onAuthStateChange?.(auth)
  }, [auth, onAuthStateChange])

  return (
    <div>
      <div data-testid="loading">{auth.isLoading ? 'loading' : 'not-loading'}</div>
      <div data-testid="authenticated">{auth.isAuthenticated ? 'authenticated' : 'not-authenticated'}</div>
      <div data-testid="user">{auth.user?.email || 'no-user'}</div>
      <div data-testid="navigation-count">{auth.navigation.length}</div>
      <button onClick={() => auth.login({ email: 'test@example.com', password: 'password' })}>
        Login
      </button>
      <button onClick={() => auth.logout()}>Logout</button>
      <button onClick={() => auth.refreshAuth()}>Refresh</button>
      <div data-testid="has-permission">{auth.hasPermission('test-resource', 'read') ? 'yes' : 'no'}</div>
    </div>
  )
}

const renderWithAuth = (component: React.ReactElement, onAuthStateChange?: (auth: any) => void) => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <TestComponent onAuthStateChange={onAuthStateChange} />
        {component}
      </AuthProvider>
    </BrowserRouter>
  )
}

const mockUser: User = {
  id: '1',
  email: 'test@example.com',
  name: 'Test User',
  roles: [
    {
      id: 'admin',
      name: 'Administrator',
      permissions: [
        {
          id: 'all',
          resource: '*',
          actions: ['*']
        }
      ]
    }
  ]
}

const mockNavigation: NavigationItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    icon: 'LayoutDashboard',
    href: '/'
  },
  {
    id: 'users',
    label: 'Users',
    icon: 'Users',
    href: '/users',
    requiredPermission: 'users',
    requiredAction: 'read'
  }
]

describe('AuthContext', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Setup default localStorage behavior
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

    // Mock console methods to avoid noise in tests
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Provider Initialization', () => {
    it('should provide auth context to children', () => {
      renderWithAuth(<div>test</div>)
      
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
      expect(screen.getByTestId('user')).toHaveTextContent('no-user')
    })

    it('should throw error when useAuth is used outside provider', () => {
      // Suppress console.error for this test
      const originalError = console.error
      console.error = vi.fn()

      expect(() => {
        render(<TestComponent />)
      }).toThrow('useAuth must be used within an AuthProvider')

      console.error = originalError
    })

    it('should initialize with loading state', () => {
      renderWithAuth(<div>test</div>)
      
      // Initially should be in not-loading state after initialization
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
    })
  })

  describe('Login Functionality', () => {
    it('should handle successful login', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      renderWithAuth(<div>test</div>)
      
      await user.click(screen.getByText('Login'))
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      expect(screen.getByTestId('user')).toHaveTextContent('test@example.com')
      expect(screen.getByTestId('navigation-count')).toHaveTextContent('2')
      expect(mockApiService.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password'
      })
    })

    it('should handle login failure', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        error: {
          message: 'Invalid credentials',
          code: 'AUTH_INVALID_CREDENTIALS'
        }
      })

      renderWithAuth(<div>test</div>)
      
      await expect(async () => {
        await user.click(screen.getByText('Login'))
        await waitFor(() => {
          expect(mockApiService.login).toHaveBeenCalled()
        })
      }).rejects.toThrow('Invalid credentials')
      
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    })

    it('should store user and navigation data in localStorage on successful login', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      renderWithAuth(<div>test</div>)
      
      await user.click(screen.getByText('Login'))
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      expect(localStorage.setItem).toHaveBeenCalledWith(
        'jarvis_current_user',
        JSON.stringify(mockUser)
      )
      expect(localStorage.setItem).toHaveBeenCalledWith(
        'jarvis_navigation',
        JSON.stringify(mockNavigation)
      )
    })
  })

  describe('Logout Functionality', () => {
    it('should handle logout', async () => {
      const user = userEvent.setup()
      
      // First login
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })
      
      mockApiService.logout.mockResolvedValue(undefined)

      renderWithAuth(<div>test</div>)
      
      // Login first
      await user.click(screen.getByText('Login'))
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      // Then logout
      await user.click(screen.getByText('Logout'))
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
      })
      
      expect(screen.getByTestId('user')).toHaveTextContent('no-user')
      expect(screen.getByTestId('navigation-count')).toHaveTextContent('0')
      expect(mockApiService.logout).toHaveBeenCalled()
    })

    it('should clear stored data on logout', async () => {
      const user = userEvent.setup()
      
      // Setup logged in state
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })
      
      mockApiService.logout.mockResolvedValue(undefined)

      renderWithAuth(<div>test</div>)
      
      await user.click(screen.getByText('Login'))
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      await user.click(screen.getByText('Logout'))
      
      await waitFor(() => {
        expect(localStorage.removeItem).toHaveBeenCalledWith('jarvis_current_user')
      })
      
      expect(localStorage.removeItem).toHaveBeenCalledWith('jarvis_navigation')
    })
  })

  describe('Permission System', () => {
    beforeEach(async () => {
      // Setup logged in user for permission tests
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })
      
      mockApiService.hasPermission.mockReturnValue(true)
    })

    it('should check permissions for authenticated user', async () => {
      const user = userEvent.setup()
      
      renderWithAuth(<div>test</div>)
      
      await user.click(screen.getByText('Login'))
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      expect(screen.getByTestId('has-permission')).toHaveTextContent('yes')
      expect(mockApiService.hasPermission).toHaveBeenCalledWith(
        mockUser,
        'test-resource',
        'read'
      )
    })

    it('should return false for permissions when not authenticated', () => {
      renderWithAuth(<div>test</div>)
      
      expect(screen.getByTestId('has-permission')).toHaveTextContent('no')
    })

    it('should use default action "read" when no action specified', async () => {
      const user = userEvent.setup()
      let authContext: any

      const TestPermissionComponent = () => {
        const auth = useAuth()
        authContext = auth
        return <div data-testid="permission-test">{auth.hasPermission('test-resource') ? 'yes' : 'no'}</div>
      }

      render(
        <BrowserRouter>
          <AuthProvider>
            <TestPermissionComponent />
          </AuthProvider>
        </BrowserRouter>
      )
      
      // Manually trigger login
      await act(async () => {
        await authContext.login({ email: 'test@example.com', password: 'password' })
      })
      
      await waitFor(() => {
        expect(mockApiService.hasPermission).toHaveBeenCalledWith(
          mockUser,
          'test-resource',
          'read'
        )
      })
    })
  })

  describe('Token Refresh', () => {
    it('should handle token refresh', async () => {
      (mockApiService as any).refreshAuth = vi.fn().mockResolvedValue(undefined)
      mockApiService.getCurrentUser.mockResolvedValue({
        data: mockUser
      })
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      renderWithAuth(<div>test</div>)
      
      fireEvent.click(screen.getByText('Refresh'))
      
      await waitFor(() => {
        expect(mockApiService.getCurrentUser).toHaveBeenCalled()
      })
    })
  })

  describe('Initialization with Stored Tokens', () => {
    it('should load user from stored tokens on initialization', async () => {
      // Mock stored tokens
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify(mockNavigation)
        return null
      })

      const mockGetStoredTokens = vi.fn().mockReturnValue({
        accessToken: 'stored-access-token',
        refreshToken: 'stored-refresh-token'
      })
      
      vi.doMock('../../utils/tokenUtils', () => ({
        getStoredTokens: mockGetStoredTokens,
        isTokenExpired: vi.fn().mockReturnValue(false),
        clearTokens: vi.fn(),
        storeTokens: vi.fn(),
        getTimeUntilExpiry: vi.fn().mockReturnValue(3600000)
      }))

      mockApiService.getCurrentUser.mockResolvedValue({
        data: mockUser
      })
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      renderWithAuth(<div>test</div>)
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      expect(screen.getByTestId('user')).toHaveTextContent('test@example.com')
    })

    it('should handle expired tokens by attempting refresh', async () => {
      const mockIsTokenExpired = vi.fn().mockReturnValue(true)
      const mockGetStoredTokens = vi.fn().mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'refresh-token'
      })
      
      vi.doMock('../../utils/tokenUtils', () => ({
        getStoredTokens: mockGetStoredTokens,
        isTokenExpired: mockIsTokenExpired,
        clearTokens: mockClearTokens,
        storeTokens: mockStoreTokens,
        getTimeUntilExpiry: vi.fn().mockReturnValue(0)
      }))

      mockApiService.refreshToken.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      renderWithAuth(<div>test</div>)
      
      await waitFor(() => {
        expect(mockApiService.refreshToken).toHaveBeenCalledWith('refresh-token')
      })
    })

    it('should clear auth when refresh token fails', async () => {
      const mockIsTokenExpired = vi.fn().mockReturnValue(true)
      const mockGetStoredTokens = vi.fn().mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'invalid-refresh-token'
      })
      
      vi.doMock('../../utils/tokenUtils', () => ({
        getStoredTokens: mockGetStoredTokens,
        isTokenExpired: mockIsTokenExpired,
        clearTokens: mockClearTokens,
        storeTokens: mockStoreTokens,
        getTimeUntilExpiry: vi.fn().mockReturnValue(0)
      }))

      mockApiService.refreshToken.mockResolvedValue({
        error: {
          message: 'Invalid refresh token',
          code: 'AUTH_INVALID_TOKEN'
        }
      })

      renderWithAuth(<div>test</div>)
      
      await waitFor(() => {
        expect(mockClearTokens).toHaveBeenCalled()
      })
    })
  })

  describe('Error Handling', () => {
    it('should handle network errors during login', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockRejectedValue(new Error('Network error'))

      renderWithAuth(<div>test</div>)
      
      await expect(async () => {
        await user.click(screen.getByText('Login'))
        await waitFor(() => {
          expect(mockApiService.login).toHaveBeenCalled()
        })
      }).rejects.toThrow('Network error')
    })

    it('should handle API errors gracefully during initialization', async () => {
      const mockGetStoredTokens = vi.fn().mockReturnValue({
        accessToken: 'valid-access-token',
        refreshToken: 'refresh-token'
      })
      
      vi.doMock('../../utils/tokenUtils', () => ({
        getStoredTokens: mockGetStoredTokens,
        isTokenExpired: vi.fn().mockReturnValue(false),
        clearTokens: mockClearTokens,
        storeTokens: mockStoreTokens,
        getTimeUntilExpiry: vi.fn().mockReturnValue(3600000)
      }))

      mockApiService.getCurrentUser.mockRejectedValue(new Error('API Error'))
      
      // Should fall back to stored user data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify(mockNavigation)
        return null
      })

      renderWithAuth(<div>test</div>)
      
      await waitFor(() => {
        expect(screen.getByTestId('user')).toHaveTextContent('test@example.com')
      })
    })
  })

  describe('Session Management', () => {
    it('should schedule token refresh', async () => {
      const user = userEvent.setup()
      
      // Mock timer functions
      vi.useFakeTimers()
      
      const mockGetTimeUntilExpiry = vi.fn().mockReturnValue(10 * 60 * 1000) // 10 minutes
      
      vi.doMock('../../utils/tokenUtils', () => ({
        getStoredTokens: vi.fn().mockReturnValue({
          accessToken: null,
          refreshToken: null
        }),
        isTokenExpired: vi.fn().mockReturnValue(false),
        clearTokens: mockClearTokens,
        storeTokens: mockStoreTokens,
        getTimeUntilExpiry: mockGetTimeUntilExpiry
      }))

      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })
      
      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      renderWithAuth(<div>test</div>)
      
      await user.click(screen.getByText('Login'))
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      })
      
      // Fast forward to just before refresh time (5 minutes before expiry)
      act(() => {
        vi.advanceTimersByTime(5 * 60 * 1000) // 5 minutes
      })
      
      vi.useRealTimers()
    })
  })
})