import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider, useAuth } from '../AuthContext'
import type { User } from '../../services/api/types'

// Mock the API service module
vi.mock('../../services/api/apiService', () => {
  const mockService = {
    login: vi.fn(),
    logout: vi.fn(),
    getCurrentUser: vi.fn(),
    getNavigation: vi.fn(),
    refreshToken: vi.fn(),
    hasPermission: vi.fn(),
  }
  return {
    apiService: mockService,
    createApiService: () => mockService
  }
})

// Mock token utilities
vi.mock('../../utils/tokenUtils', () => ({
  getStoredTokens: vi.fn(() => ({ accessToken: null, refreshToken: null })),
  isTokenExpired: vi.fn(() => true),
  getTimeUntilExpiry: vi.fn(() => 0),
  clearTokens: vi.fn(),
  storeTokens: vi.fn(),
  ACCESS_TOKEN_KEY: 'jarvis_auth_token',
  REFRESH_TOKEN_KEY: 'jarvis_refresh_token',
  TOKEN_REFRESH_BUFFER: 5 * 60 * 1000
}))

// Import after mocks are set up
import { apiService } from '../../services/api/apiService'
import { getStoredTokens, isTokenExpired, clearTokens, storeTokens } from '../../utils/tokenUtils'

const mockApiService = vi.mocked(apiService)
const mockGetStoredTokens = vi.mocked(getStoredTokens)
const mockIsTokenExpired = vi.mocked(isTokenExpired)
const mockClearTokens = vi.mocked(clearTokens)
const mockStoreTokens = vi.mocked(storeTokens)

// Test component
const TestComponent = () => {
  const auth = useAuth()

  return (
    <div>
      <div data-testid="loading">{auth.isLoading ? 'loading' : 'not-loading'}</div>
      <div data-testid="authenticated">{auth.isAuthenticated ? 'authenticated' : 'not-authenticated'}</div>
      <div data-testid="user">{auth.user?.email || 'no-user'}</div>
      <div data-testid="navigation-count">{auth.navigation.length}</div>
      <button 
        onClick={() => auth.login({ email: 'test@example.com', password: 'password' })}
        data-testid="login-btn"
      >
        Login
      </button>
      <button onClick={() => auth.logout()} data-testid="logout-btn">Logout</button>
      <div data-testid="has-permission">
        {auth.hasPermission('test-resource', 'read') ? 'yes' : 'no'}
      </div>
    </div>
  )
}

const renderWithAuth = () => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <TestComponent />
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
        { id: 'all', resource: '*', actions: ['*'] }
      ]
    }
  ]
}

describe('AuthContext Basic Functionality', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Setup localStorage mock
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

    // Mock console methods
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})

    // Default mock implementations
    mockGetStoredTokens.mockReturnValue({ accessToken: null, refreshToken: null })
    mockIsTokenExpired.mockReturnValue(true)
    mockApiService.getCurrentUser.mockResolvedValue({ data: null })
    mockApiService.getNavigation.mockResolvedValue({ data: [] })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should provide auth context to children', async () => {
    renderWithAuth()
    
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
    })
    
    expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    expect(screen.getByTestId('user')).toHaveTextContent('no-user')
  })

  it('should throw error when useAuth is used outside provider', () => {
    const TestComponentOutside = () => {
      useAuth()
      return <div>test</div>
    }
    
    // Suppress console.error for this test
    const originalError = console.error
    console.error = vi.fn()

    expect(() => {
      render(<TestComponentOutside />)
    }).toThrow('useAuth must be used within an AuthProvider')

    console.error = originalError
  })

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
    
    mockApiService.getNavigation.mockResolvedValue({ data: [] })

    renderWithAuth()
    
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
    })
    
    await user.click(screen.getByTestId('login-btn'))
    
    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
    })
    
    expect(screen.getByTestId('user')).toHaveTextContent('test@example.com')
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

    renderWithAuth()
    
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
    })
    
    await expect(async () => {
      await user.click(screen.getByTestId('login-btn'))
      // Wait for the login promise to resolve/reject
      await new Promise(resolve => setTimeout(resolve, 100))
    }).rejects.toThrow('Invalid credentials')
    
    expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
  })

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
    
    mockApiService.getNavigation.mockResolvedValue({ data: [] })
    mockApiService.logout.mockResolvedValue(undefined)

    renderWithAuth()
    
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
    })
    
    // Login first
    await user.click(screen.getByTestId('login-btn'))
    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
    })
    
    // Then logout
    await user.click(screen.getByTestId('logout-btn'))
    
    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    })
    
    expect(screen.getByTestId('user')).toHaveTextContent('no-user')
    expect(mockApiService.logout).toHaveBeenCalled()
  })

  it('should check permissions for authenticated user', async () => {
    const user = userEvent.setup()
    
    mockApiService.login.mockResolvedValue({
      data: {
        user: mockUser,
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresIn: 3600
      }
    })
    
    mockApiService.getNavigation.mockResolvedValue({ data: [] })
    mockApiService.hasPermission.mockReturnValue(true)

    renderWithAuth()
    
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('not-loading')
    })
    
    await user.click(screen.getByTestId('login-btn'))
    
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
    renderWithAuth()
    
    expect(screen.getByTestId('has-permission')).toHaveTextContent('no')
  })

  it('should restore session from stored tokens', async () => {
    mockGetStoredTokens.mockReturnValue({
      accessToken: 'stored-access-token',
      refreshToken: 'stored-refresh-token'
    })
    mockIsTokenExpired.mockReturnValue(false)

    mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
    mockApiService.getNavigation.mockResolvedValue({ data: [] })

    renderWithAuth()

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
    })

    expect(screen.getByTestId('user')).toHaveTextContent('test@example.com')
  })

  it('should handle expired tokens by attempting refresh', async () => {
    mockGetStoredTokens.mockReturnValue({
      accessToken: 'expired-access-token',
      refreshToken: 'refresh-token'
    })
    mockIsTokenExpired.mockReturnValue(true)

    mockApiService.refreshToken.mockResolvedValue({
      data: {
        user: mockUser,
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600
      }
    })
    
    mockApiService.getNavigation.mockResolvedValue({ data: [] })

    renderWithAuth()

    await waitFor(() => {
      expect(mockApiService.refreshToken).toHaveBeenCalledWith('refresh-token')
    })
    
    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
    })
  })

  it('should clear auth when refresh token fails', async () => {
    mockGetStoredTokens.mockReturnValue({
      accessToken: 'expired-access-token',
      refreshToken: 'invalid-refresh-token'
    })
    mockIsTokenExpired.mockReturnValue(true)

    mockApiService.refreshToken.mockResolvedValue({
      error: {
        message: 'Invalid refresh token',
        code: 'AUTH_INVALID_TOKEN'
      }
    })

    renderWithAuth()

    await waitFor(() => {
      expect(mockClearTokens).toHaveBeenCalled()
    })
    
    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    })
  })

  it('should handle network errors gracefully', async () => {
    mockGetStoredTokens.mockReturnValue({
      accessToken: 'valid-access-token',
      refreshToken: 'refresh-token'
    })
    mockIsTokenExpired.mockReturnValue(false)

    mockApiService.getCurrentUser.mockRejectedValue(new Error('Network error'))
    
    // Mock stored user data for fallback
    vi.mocked(localStorage.getItem).mockImplementation((key) => {
      if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
      if (key === 'jarvis_navigation') return JSON.stringify([])
      return null
    })

    renderWithAuth()

    await waitFor(() => {
      expect(screen.getByTestId('user')).toHaveTextContent('test@example.com')
    })
  })
})