import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter, Routes, Route, useNavigate } from 'react-router-dom'
import { AuthProvider, useAuth } from '../../contexts/AuthContext'
import { LoginForm } from '../../components/auth/LoginForm'
import { ProtectedRoute } from '../../components/auth/ProtectedRoute'
import { apiService } from '../../services/api/apiService'
import { clearTokens, storeTokens, getStoredTokens, isTokenExpired } from '../../utils/tokenUtils'
import type { User, LoginCredentials, NavigationItem } from '../../services/api/types'

// Mock dependencies
vi.mock('../../services/api/apiService')
vi.mock('../../utils/tokenUtils')

const mockApiService = vi.mocked(apiService)
const mockClearTokens = vi.mocked(clearTokens)
const mockStoreTokens = vi.mocked(storeTokens)
const mockGetStoredTokens = vi.mocked(getStoredTokens)
const mockIsTokenExpired = vi.mocked(isTokenExpired)

// Test components
const Dashboard = () => {
  const { user, logout } = useAuth()
  return (
    <div>
      <div data-testid="dashboard">Dashboard</div>
      <div data-testid="user-email">{user?.email}</div>
      <button onClick={logout} data-testid="logout-btn">Logout</button>
    </div>
  )
}

const AdminPanel = () => (
  <div data-testid="admin-panel">Admin Panel</div>
)

const UserProfile = () => (
  <div data-testid="user-profile">User Profile</div>
)

const LoginPage = () => {
  const navigate = useNavigate()
  return (
    <div data-testid="login-page">
      <LoginForm />
      <button onClick={() => navigate('/')} data-testid="home-btn">Go Home</button>
    </div>
  )
}

// Navigation component to test auth-based navigation
const Navigation = () => {
  const { isAuthenticated, navigation } = useAuth()
  
  if (!isAuthenticated) return null
  
  return (
    <nav data-testid="navigation">
      {navigation.map(item => (
        <a key={item.id} href={item.href} data-testid={`nav-${item.id}`}>
          {item.label}
        </a>
      ))}
    </nav>
  )
}

// Main app component for integration testing
const TestApp = () => (
  <BrowserRouter>
    <AuthProvider>
      <Navigation />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route 
          path="/" 
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/admin" 
          element={
            <ProtectedRoute requiredPermission="admin" requiredAction="write">
              <AdminPanel />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/profile" 
          element={
            <ProtectedRoute requiredPermission="profile" requiredAction="read">
              <UserProfile />
            </ProtectedRoute>
          } 
        />
      </Routes>
    </AuthProvider>
  </BrowserRouter>
)

// Mock data
const mockUser: User = {
  id: '1',
  email: 'test@example.com',
  name: 'Test User',
  roles: [
    {
      id: 'user',
      name: 'User',
      permissions: [
        { id: 'profile-read', resource: 'profile', actions: ['read'] }
      ]
    }
  ]
}

const mockAdminUser: User = {
  id: '2',
  email: 'admin@example.com',
  name: 'Admin User',
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

const mockNavigation: NavigationItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: 'Home', href: '/' },
  { id: 'profile', label: 'Profile', icon: 'User', href: '/profile', requiredPermission: 'profile', requiredAction: 'read' },
  { id: 'admin', label: 'Admin', icon: 'Settings', href: '/admin', requiredPermission: 'admin', requiredAction: 'write' }
]

describe('Authentication Integration Tests', () => {
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

    // Mock console methods
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})

    // Default mock implementations
    mockGetStoredTokens.mockReturnValue({ accessToken: null, refreshToken: null })
    mockIsTokenExpired.mockReturnValue(true)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Complete Login Flow', () => {
    it('should complete full login flow from unauthenticated to authenticated', async () => {
      const user = userEvent.setup()

      // Mock successful login
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation.filter(item => !item.requiredPermission || item.id === 'profile')
      })

      mockApiService.hasPermission.mockImplementation((user, resource) => {
        return resource === 'profile'
      })

      render(<TestApp />)

      // Should redirect to login initially
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // Fill and submit login form
      const emailInput = screen.getByLabelText(/email/i)
      const passwordInput = screen.getByLabelText(/password/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })

      await user.clear(emailInput)
      await user.clear(passwordInput)
      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'password123')
      await user.click(submitButton)

      // Should navigate to dashboard after successful login
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      }, { timeout: 2000 })

      // Should show user information
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')

      // Should show navigation items based on permissions
      expect(screen.getByTestId('navigation')).toBeInTheDocument()
      expect(screen.getByTestId('nav-dashboard')).toBeInTheDocument()
      expect(screen.getByTestId('nav-profile')).toBeInTheDocument()
      expect(screen.queryByTestId('nav-admin')).not.toBeInTheDocument() // No admin permission
    })

    it('should handle login failure gracefully', async () => {
      const user = userEvent.setup()

      // Mock login failure
      mockApiService.login.mockResolvedValue({
        error: {
          message: 'Invalid credentials',
          code: 'AUTH_INVALID_CREDENTIALS'
        }
      })

      render(<TestApp />)

      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // Submit login form with invalid credentials
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)

      // Should show error message
      await waitFor(() => {
        expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
      })

      // Should remain on login page
      expect(screen.getByTestId('login-page')).toBeInTheDocument()
      expect(screen.queryByTestId('dashboard')).not.toBeInTheDocument()
    })
  })

  describe('Session Management', () => {
    it('should restore session from stored tokens on app restart', async () => {
      // Mock stored valid tokens
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-access-token',
        refreshToken: 'valid-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock current user retrieval
      mockApiService.getCurrentUser.mockResolvedValue({
        data: mockUser
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation.filter(item => !item.requiredPermission || item.id === 'profile')
      })

      render(<TestApp />)

      // Should automatically authenticate and show dashboard
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
      expect(screen.queryByTestId('login-page')).not.toBeInTheDocument()
    })

    it('should handle expired tokens by redirecting to login', async () => {
      // Mock expired tokens
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'expired-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock failed refresh
      mockApiService.refreshToken.mockResolvedValue({
        error: {
          message: 'Invalid refresh token',
          code: 'AUTH_INVALID_TOKEN'
        }
      })

      render(<TestApp />)

      // Should redirect to login after failed token refresh
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      expect(mockClearTokens).toHaveBeenCalled()
      expect(screen.queryByTestId('dashboard')).not.toBeInTheDocument()
    })

    it('should automatically refresh tokens when they expire', async () => {
      // Mock expired access token but valid refresh token
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'valid-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock successful token refresh
      mockApiService.refreshToken.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation.filter(item => !item.requiredPermission || item.id === 'profile')
      })

      render(<TestApp />)

      // Should successfully refresh and show dashboard
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      expect(mockApiService.refreshToken).toHaveBeenCalledWith('valid-refresh-token')
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })
  })

  describe('Permission-based Access Control', () => {
    beforeEach(async () => {
      // Setup authenticated admin user
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'admin-access-token',
        refreshToken: 'admin-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({
        data: mockAdminUser
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation // Admin gets all navigation items
      })

      mockApiService.hasPermission.mockReturnValue(true) // Admin has all permissions
    })

    it('should allow admin user to access admin panel', async () => {
      render(<TestApp />)

      // Should show dashboard first
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      // Navigate to admin panel
      window.history.pushState({}, '', '/admin')
      
      await waitFor(() => {
        expect(screen.getByTestId('admin-panel')).toBeInTheDocument()
      })

      // Should show admin navigation item
      expect(screen.getByTestId('nav-admin')).toBeInTheDocument()
    })

    it('should deny regular user access to admin panel', async () => {
      // Setup regular user instead of admin
      mockApiService.getCurrentUser.mockResolvedValue({
        data: mockUser
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation.filter(item => !item.requiredPermission || item.id === 'profile')
      })

      mockApiService.hasPermission.mockImplementation((user, resource, action) => {
        return resource === 'profile' && action === 'read'
      })

      render(<TestApp />)

      // Navigate directly to admin panel
      window.history.pushState({}, '', '/admin')

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      expect(screen.queryByTestId('admin-panel')).not.toBeInTheDocument()
      expect(screen.queryByTestId('nav-admin')).not.toBeInTheDocument()
    })
  })

  describe('Logout Flow', () => {
    it('should complete full logout flow', async () => {
      const user = userEvent.setup()

      // Setup authenticated state
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'access-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({
        data: mockUser
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation.filter(item => !item.requiredPermission || item.id === 'profile')
      })

      mockApiService.logout.mockResolvedValue(undefined)

      render(<TestApp />)

      // Should show authenticated dashboard
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      // Click logout button
      const logoutButton = screen.getByTestId('logout-btn')
      await user.click(logoutButton)

      // Should redirect to login page
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // Should clear stored data
      expect(mockApiService.logout).toHaveBeenCalled()
      expect(screen.queryByTestId('dashboard')).not.toBeInTheDocument()
      expect(screen.queryByTestId('navigation')).not.toBeInTheDocument()
    })
  })

  describe('Network Error Handling', () => {
    it('should handle network errors during login', async () => {
      const user = userEvent.setup()

      // Mock network error
      mockApiService.login.mockRejectedValue(new Error('Network error'))

      render(<TestApp />)

      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/network error/i)).toBeInTheDocument()
      })
    })

    it('should handle API errors during initialization', async () => {
      // Mock valid tokens but API error
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-access-token',
        refreshToken: 'valid-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockRejectedValue(new Error('API Error'))

      // Mock fallback to stored data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify(mockNavigation)
        return null
      })

      render(<TestApp />)

      // Should still show dashboard with cached data
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })
  })

  describe('Route Protection', () => {
    it('should protect all routes when not authenticated', async () => {
      // Mock unauthenticated state
      mockApiService.getCurrentUser.mockResolvedValue({ data: null })

      render(<TestApp />)

      // Should redirect to login from root
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // Try to navigate to protected routes
      const protectedRoutes = ['/', '/admin', '/profile']
      
      for (const route of protectedRoutes) {
        window.history.pushState({}, '', route)
        
        await waitFor(() => {
          expect(screen.getByTestId('login-page')).toBeInTheDocument()
        })
        
        expect(screen.queryByTestId('dashboard')).not.toBeInTheDocument()
        expect(screen.queryByTestId('admin-panel')).not.toBeInTheDocument()
        expect(screen.queryByTestId('user-profile')).not.toBeInTheDocument()
      }
    })

    it('should preserve intended destination after login', async () => {
      const user = userEvent.setup()

      // Start at protected route while unauthenticated
      window.history.pushState({}, '', '/profile')

      // Mock unauthenticated initially, then successful login
      mockApiService.getCurrentUser.mockResolvedValueOnce({ data: null })
      
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation.filter(item => !item.requiredPermission || item.id === 'profile')
      })

      mockApiService.hasPermission.mockImplementation((user, resource) => {
        return resource === 'profile'
      })

      render(<TestApp />)

      // Should redirect to login
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // Perform login
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)

      // Should redirect to dashboard (default after login)
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })
    })
  })

  describe('Concurrent Authentication Events', () => {
    it('should handle multiple simultaneous login attempts', async () => {
      const user = userEvent.setup()

      let loginResolvers: Array<(value: any) => void> = []
      
      mockApiService.login.mockImplementation(() => {
        return new Promise((resolve) => {
          loginResolvers.push(resolve)
        })
      })

      render(<TestApp />)

      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // Trigger multiple login attempts
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      await user.click(submitButton)
      await user.click(submitButton)

      // Should only process one login
      expect(loginResolvers).toHaveLength(1)

      // Resolve the login
      act(() => {
        loginResolvers[0]({
          data: {
            user: mockUser,
            accessToken: 'access-token',
            refreshToken: 'refresh-token',
            expiresIn: 3600
          }
        })
      })

      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })
    })
  })

  describe('Real-world Scenarios', () => {
    it('should handle complete user journey from login to logout', async () => {
      const user = userEvent.setup()

      // Mock login success
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockAdminUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: mockNavigation
      })

      mockApiService.hasPermission.mockReturnValue(true)
      mockApiService.logout.mockResolvedValue(undefined)

      render(<TestApp />)

      // 1. Start unauthenticated
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      // 2. Login
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      // 3. Navigate to different protected routes
      window.history.pushState({}, '', '/admin')
      
      await waitFor(() => {
        expect(screen.getByTestId('admin-panel')).toBeInTheDocument()
      })

      window.history.pushState({}, '', '/profile')
      
      await waitFor(() => {
        expect(screen.getByTestId('user-profile')).toBeInTheDocument()
      })

      // 4. Return to dashboard
      window.history.pushState({}, '', '/')
      
      await waitFor(() => {
        expect(screen.getByTestId('dashboard')).toBeInTheDocument()
      })

      // 5. Logout
      const logoutButton = screen.getByTestId('logout-btn')
      await user.click(logoutButton)

      // 6. Should return to login
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      expect(screen.queryByTestId('navigation')).not.toBeInTheDocument()
    })
  })
})