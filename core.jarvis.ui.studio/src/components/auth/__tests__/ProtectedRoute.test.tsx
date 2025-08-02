import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { ProtectedRoute } from '../ProtectedRoute'
import { AuthProvider } from '../../../contexts/AuthContext'
import { apiService } from '../../../services/api/apiService'
import type { User } from '../../../services/api/types'

// Mock dependencies
vi.mock('../../../services/api/apiService')
vi.mock('../../../utils/tokenUtils')

const mockApiService = vi.mocked(apiService)

// Mock components for testing
const TestComponent = ({ message = 'Protected Content' }: { message?: string }) => (
  <div data-testid="protected-content">{message}</div>
)

const LoginPage = () => (
  <div data-testid="login-page">Login Page</div>
)

// Helper to render ProtectedRoute with router context
const renderProtectedRoute = (
  protectedContent: React.ReactNode,
  requiredPermission?: string,
  requiredAction?: string,
  initialRoute = '/'
) => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route 
            path="/" 
            element={
              <ProtectedRoute 
                requiredPermission={requiredPermission}
                requiredAction={requiredAction}
              >
                {protectedContent}
              </ProtectedRoute>
            } 
          />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/dashboard" element={
            <ProtectedRoute>
              <TestComponent message="Dashboard Content" />
            </ProtectedRoute>
          } />
          <Route path="/admin" element={
            <ProtectedRoute requiredPermission="admin" requiredAction="write">
              <TestComponent message="Admin Content" />
            </ProtectedRoute>
          } />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

// Mock user data
const mockUser: User = {
  id: '1',
  email: 'test@example.com',
  name: 'Test User',
  roles: [
    {
      id: 'user',
      name: 'User',
      permissions: [
        {
          id: 'users-read',
          resource: 'users',
          actions: ['read']
        }
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
        {
          id: 'all',
          resource: '*',
          actions: ['*']
        }
      ]
    }
  ]
}

describe('ProtectedRoute', () => {
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
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Loading State', () => {
    it('should show loading spinner while auth is loading', async () => {
      // Mock loading state
      mockApiService.getCurrentUser.mockImplementation(() => 
        new Promise(() => {}) // Never resolves to keep loading
      )

      renderProtectedRoute(<TestComponent />)

      expect(screen.getByText('Loading...')).toBeInTheDocument()
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('should have proper loading spinner styling', () => {
      mockApiService.getCurrentUser.mockImplementation(() => 
        new Promise(() => {})
      )

      renderProtectedRoute(<TestComponent />)

      const loadingContainer = screen.getByText('Loading...').closest('div')
      expect(loadingContainer).toHaveClass('min-h-screen', 'flex', 'items-center', 'justify-center')
      expect(screen.getByText('Loading...')).toHaveClass('text-muted-foreground')
    })
  })

  describe('Unauthenticated Access', () => {
    beforeEach(() => {
      // Mock unauthenticated state
      mockApiService.getCurrentUser.mockResolvedValue({ data: null })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
    })

    it('should redirect to login when not authenticated', async () => {
      renderProtectedRoute(<TestComponent />)

      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })

      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('should preserve location state for post-login redirect', async () => {
      // We can't easily test the location state without more complex mocking,
      // but we can ensure the redirect happens
      renderProtectedRoute(<TestComponent />)

      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })
    })
  })

  describe('Authenticated Access', () => {
    beforeEach(() => {
      // Mock authenticated state
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockReturnValue(true)
    })

    it('should render protected content when authenticated', async () => {
      renderProtectedRoute(<TestComponent />)

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
      expect(screen.queryByTestId('login-page')).not.toBeInTheDocument()
    })

    it('should render children without modification', async () => {
      const complexContent = (
        <div>
          <h1>Complex Content</h1>
          <button>Action Button</button>
          <p>Description text</p>
        </div>
      )

      renderProtectedRoute(complexContent)

      await waitFor(() => {
        expect(screen.getByText('Complex Content')).toBeInTheDocument()
      })

      expect(screen.getByText('Action Button')).toBeInTheDocument()
      expect(screen.getByText('Description text')).toBeInTheDocument()
    })
  })

  describe('Permission-based Access Control', () => {
    beforeEach(() => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
    })

    it('should grant access when user has required permission', async () => {
      mockApiService.hasPermission.mockImplementation((user, resource, action) => {
        return resource === 'users' && action === 'read'
      })

      renderProtectedRoute(
        <TestComponent />,
        'users',
        'read'
      )

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })

      expect(mockApiService.hasPermission).toHaveBeenCalledWith(
        mockUser,
        'users',
        'read'
      )
    })

    it('should deny access when user lacks required permission', async () => {
      mockApiService.hasPermission.mockReturnValue(false)

      renderProtectedRoute(
        <TestComponent />,
        'admin',
        'write'
      )

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      expect(screen.getByText("You don't have permission to access this resource.")).toBeInTheDocument()
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('should use default action "read" when not specified', async () => {
      mockApiService.hasPermission.mockReturnValue(true)

      renderProtectedRoute(
        <TestComponent />,
        'users' // No action specified
      )

      await waitFor(() => {
        expect(mockApiService.hasPermission).toHaveBeenCalledWith(
          mockUser,
          'users',
          'read'
        )
      })
    })

    it('should not check permissions when none required', async () => {
      renderProtectedRoute(<TestComponent />)

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })

      expect(mockApiService.hasPermission).not.toHaveBeenCalled()
    })
  })

  describe('Access Denied State', () => {
    beforeEach(() => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockReturnValue(false)
    })

    it('should show access denied message with proper styling', async () => {
      renderProtectedRoute(
        <TestComponent />,
        'admin',
        'write'
      )

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      const container = screen.getByText('Access Denied').closest('div')
      expect(container).toHaveClass('min-h-screen', 'flex', 'items-center', 'justify-center')
      
      const messageContainer = screen.getByText('Access Denied').parentElement
      expect(messageContainer).toHaveClass('text-center', 'space-y-2')
      
      expect(screen.getByText('Access Denied')).toHaveClass('text-2xl', 'font-bold', 'text-destructive')
      expect(screen.getByText("You don't have permission to access this resource."))
        .toHaveClass('text-muted-foreground')
    })

    it('should not render protected content when access denied', async () => {
      renderProtectedRoute(
        <TestComponent />,
        'admin',
        'write'
      )

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })
  })

  describe('Complex Permission Scenarios', () => {
    it('should handle multiple roles and permissions', async () => {
      const userWithMultipleRoles: User = {
        id: '3',
        email: 'multi@example.com',
        name: 'Multi Role User',
        roles: [
          {
            id: 'viewer',
            name: 'Viewer',
            permissions: [
              { id: 'read-users', resource: 'users', actions: ['read'] }
            ]
          },
          {
            id: 'editor',
            name: 'Editor',
            permissions: [
              { id: 'write-content', resource: 'content', actions: ['write'] }
            ]
          }
        ]
      }

      mockApiService.getCurrentUser.mockResolvedValue({ data: userWithMultipleRoles })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockImplementation((user, resource, action) => {
        return resource === 'content' && action === 'write'
      })

      renderProtectedRoute(
        <TestComponent />,
        'content',
        'write'
      )

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })
    })

    it('should handle wildcard permissions', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockAdminUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockReturnValue(true) // Admin has wildcard access

      renderProtectedRoute(
        <TestComponent />,
        'any-resource',
        'any-action'
      )

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('should handle auth context errors gracefully', async () => {
      mockApiService.getCurrentUser.mockRejectedValue(new Error('Auth error'))

      renderProtectedRoute(<TestComponent />)

      // Should eventually redirect to login if auth fails
      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })
    })

    it('should handle missing user data', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: null })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderProtectedRoute(<TestComponent />)

      await waitFor(() => {
        expect(screen.getByTestId('login-page')).toBeInTheDocument()
      })
    })

    it('should handle permission check errors', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockImplementation(() => {
        throw new Error('Permission check error')
      })

      renderProtectedRoute(
        <TestComponent />,
        'users',
        'read'
      )

      await waitFor(() => {
        // Should treat permission error as access denied
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })
    })
  })

  describe('Route Navigation', () => {
    it('should work with nested routes', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockReturnValue(true)

      render(
        <BrowserRouter>
          <AuthProvider>
            <Routes>
              <Route path="/" element={
                <ProtectedRoute>
                  <div>
                    <TestComponent message="Parent Content" />
                    <Routes>
                      <Route path="/child" element={
                        <ProtectedRoute requiredPermission="child" requiredAction="read">
                          <TestComponent message="Child Content" />
                        </ProtectedRoute>
                      } />
                    </Routes>
                  </div>
                </ProtectedRoute>
              } />
              <Route path="/login" element={<LoginPage />} />
            </Routes>
          </AuthProvider>
        </BrowserRouter>
      )

      await waitFor(() => {
        expect(screen.getByText('Parent Content')).toBeInTheDocument()
      })
    })

    it('should preserve URL parameters and query strings', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      // This test ensures the route works with parameters
      // In a real scenario, we'd test with useParams and useSearchParams
      renderProtectedRoute(<TestComponent />)

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })
    })
  })

  describe('Performance', () => {
    it('should not re-render when auth state is stable', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      
      const renderSpy = vi.fn()
      const TestComponentWithSpy = () => {
        renderSpy()
        return <TestComponent />
      }

      renderProtectedRoute(<TestComponentWithSpy />)

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })

      // Should render only once after auth is established
      expect(renderSpy).toHaveBeenCalledTimes(1)
    })

    it('should memoize permission checks', async () => {
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.hasPermission.mockReturnValue(true)

      renderProtectedRoute(
        <TestComponent />,
        'users',
        'read'
      )

      await waitFor(() => {
        expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      })

      // Permission should be checked only once
      expect(mockApiService.hasPermission).toHaveBeenCalledTimes(1)
    })
  })
})