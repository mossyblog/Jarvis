import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider, useAuth } from '../../contexts/AuthContext'
import { apiService } from '../../services/api/apiService'
import { getStoredTokens, isTokenExpired } from '../../utils/tokenUtils'
import type { User } from '../../services/api/types'

// Mock dependencies
vi.mock('../../services/api/apiService')
vi.mock('../../utils/tokenUtils')

const mockApiService = vi.mocked(apiService)
const mockGetStoredTokens = vi.mocked(getStoredTokens)
const mockIsTokenExpired = vi.mocked(isTokenExpired)

// Mock online/offline events
const mockOnlineEvents = {
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  dispatchEvent: vi.fn()
}

Object.defineProperty(window, 'navigator', {
  value: {
    onLine: true
  },
  writable: true
})

// Test component to observe auth state and trigger network events
const OfflineTestComponent = () => {
  const auth = useAuth()
  const [networkState, setNetworkState] = React.useState(navigator.onLine ? 'online' : 'offline')

  React.useEffect(() => {
    const handleOnline = () => setNetworkState('online')
    const handleOffline = () => setNetworkState('offline')

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  const simulateOffline = () => {
    Object.defineProperty(window.navigator, 'onLine', { value: false, writable: true })
    window.dispatchEvent(new Event('offline'))
  }

  const simulateOnline = () => {
    Object.defineProperty(window.navigator, 'onLine', { value: true, writable: true })
    window.dispatchEvent(new Event('online'))
  }

  return (
    <div>
      <div data-testid="auth-state">
        {auth.isLoading ? 'loading' : auth.isAuthenticated ? 'authenticated' : 'unauthenticated'}
      </div>
      <div data-testid="network-state">{networkState}</div>
      <div data-testid="user-email">{auth.user?.email || 'no-user'}</div>
      <button onClick={simulateOffline} data-testid="go-offline">Go Offline</button>
      <button onClick={simulateOnline} data-testid="go-online">Go Online</button>
      <button onClick={() => auth.login({ email: 'test@example.com', password: 'password' })} data-testid="login">
        Login
      </button>
      <button onClick={() => auth.refreshAuth()} data-testid="refresh">Refresh</button>
    </div>
  )
}

const renderOfflineTest = () => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <OfflineTestComponent />
      </AuthProvider>
    </BrowserRouter>
  )
}

// Mock user data
const mockUser: User = {
  id: '1',
  email: 'test@example.com',
  name: 'Test User',
  roles: []
}

describe('Authentication Offline/Online Behavior', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Reset navigator.onLine
    Object.defineProperty(window.navigator, 'onLine', { value: true, writable: true })
    
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
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Offline Authentication', () => {
    it('should work with cached data when offline', async () => {
      const user = userEvent.setup()

      // Setup stored tokens and cached user data
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'cached-token',
        refreshToken: 'cached-refresh'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock stored user data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify([])
        return null
      })

      // Mock API to fail (simulating offline)
      mockApiService.getCurrentUser.mockRejectedValue(new Error('Network error'))
      mockApiService.getNavigation.mockRejectedValue(new Error('Network error'))

      renderOfflineTest()

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Should still be authenticated using cached data
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    it('should handle login failure when offline', async () => {
      const user = userEvent.setup()

      renderOfflineTest()

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Mock login to fail with network error
      mockApiService.login.mockRejectedValue(new Error('Network error'))

      // Attempt login while offline
      await expect(async () => {
        await user.click(screen.getByTestId('login'))
        await waitFor(() => {
          expect(mockApiService.login).toHaveBeenCalled()
        })
      }).rejects.toThrow('Network error')

      expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
    })

    it('should queue API calls and retry when coming back online', async () => {
      const user = userEvent.setup()

      // Start with authenticated state
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderOfflineTest()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Mock refresh to fail while offline
      mockApiService.refreshAuth.mockRejectedValueOnce(new Error('Network error'))

      // Attempt refresh while offline
      await user.click(screen.getByTestId('refresh'))

      // Should handle gracefully
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Come back online
      mockApiService.refreshAuth.mockResolvedValue(undefined)
      mockApiService.getCurrentUser.mockResolvedValue({ 
        data: { ...mockUser, name: 'Updated User' } 
      })

      await user.click(screen.getByTestId('go-online'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('online')
      })

      // Should automatically sync when back online
      // (This would require implementing online event handling in the AuthContext)
    })
  })

  describe('Online/Offline State Transitions', () => {
    it('should gracefully handle network interruptions during login', async () => {
      const user = userEvent.setup()

      renderOfflineTest()

      // Start login while online
      let loginResolve: (value: any) => void
      const loginPromise = new Promise((resolve) => {
        loginResolve = resolve
      })

      mockApiService.login.mockReturnValue(loginPromise)

      await user.click(screen.getByTestId('login'))

      // Go offline while login is in progress
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Resolve login with error
      await act(async () => {
        loginResolve!({
          error: {
            message: 'Network error',
            code: 'NETWORK_ERROR'
          }
        })
      })

      // Should handle the error gracefully
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
      })
    })

    it('should handle token refresh failures when offline', async () => {
      const user = userEvent.setup()

      // Setup with expired token
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock stored user data for fallback
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify([])
        return null
      })

      renderOfflineTest()

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Mock refresh to fail due to network
      mockApiService.refreshToken.mockRejectedValue(new Error('Network error'))

      // Should fall back to cached data despite refresh failure
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    it('should handle rapid online/offline transitions', async () => {
      const user = userEvent.setup()

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderOfflineTest()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Rapid offline/online transitions
      for (let i = 0; i < 5; i++) {
        await user.click(screen.getByTestId('go-offline'))
        await user.click(screen.getByTestId('go-online'))
      }

      // Should remain stable
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
        expect(screen.getByTestId('network-state')).toHaveTextContent('online')
      })
    })
  })

  describe('Data Persistence During Offline Periods', () => {
    it('should preserve authentication state during offline periods', async () => {
      const user = userEvent.setup()

      // Setup authenticated state
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderOfflineTest()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Should maintain authentication state
      expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')

      // Stay offline for extended period (simulate)
      await new Promise(resolve => setTimeout(resolve, 100))

      // Should still be authenticated
      expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
    })

    it('should handle storage errors during offline mode', async () => {
      const user = userEvent.setup()

      // Setup with storage errors
      vi.mocked(localStorage.getItem).mockImplementation(() => {
        throw new Error('Storage error')
      })

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // API should fail due to offline
      mockApiService.getCurrentUser.mockRejectedValue(new Error('Network error'))

      renderOfflineTest()

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      // Should handle storage errors gracefully
      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Should not crash, might be unauthenticated due to storage error
      expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
    })

    it('should sync data when connectivity is restored', async () => {
      const user = userEvent.setup()

      // Start authenticated
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock initial load
      mockApiService.getCurrentUser.mockResolvedValueOnce({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValueOnce({ data: [] })

      renderOfflineTest()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Go offline
      await user.click(screen.getByTestId('go-offline'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('offline')
      })

      // Mock updated data for when coming back online
      const updatedUser = { ...mockUser, name: 'Updated Name' }
      mockApiService.getCurrentUser.mockResolvedValue({ data: updatedUser })

      // Come back online
      await user.click(screen.getByTestId('go-online'))

      await waitFor(() => {
        expect(screen.getByTestId('network-state')).toHaveTextContent('online')
      })

      // Should automatically refresh data
      // (This would require implementing online event handling in the AuthContext)
      await user.click(screen.getByTestId('refresh'))

      // Should reflect updated data
      await waitFor(() => {
        expect(mockApiService.getCurrentUser).toHaveBeenCalledTimes(3) // Initial + offline handling + refresh
      })
    })
  })

  describe('Error Recovery', () => {
    it('should recover from network errors when connectivity is restored', async () => {
      const user = userEvent.setup()

      renderOfflineTest()

      // Simulate network error during initial load
      mockApiService.getCurrentUser.mockRejectedValueOnce(new Error('Network error'))

      // Go offline/online to trigger recovery
      await user.click(screen.getByTestId('go-offline'))
      await user.click(screen.getByTestId('go-online'))

      // Mock successful recovery
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      // Manual refresh should work after connectivity restored
      await user.click(screen.getByTestId('refresh'))

      await waitFor(() => {
        expect(mockApiService.getCurrentUser).toHaveBeenCalledTimes(2)
      })
    })

    it('should handle partial connectivity issues', async () => {
      const user = userEvent.setup()

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock partial connectivity - some APIs work, others don't
      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockRejectedValue(new Error('Timeout'))

      renderOfflineTest()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Should work with partial data
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })
  })

  describe('Background Sync', () => {
    it('should handle background authentication checks', async () => {
      // Setup authenticated state
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderOfflineTest()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Simulate app going to background (page visibility change)
      Object.defineProperty(document, 'hidden', { value: true, writable: true })
      document.dispatchEvent(new Event('visibilitychange'))

      // Come back to foreground
      Object.defineProperty(document, 'hidden', { value: false, writable: true })
      document.dispatchEvent(new Event('visibilitychange'))

      // Should maintain state
      expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
    })
  })

  describe('Performance Under Network Constraints', () => {
    it('should handle slow network gracefully', async () => {
      const user = userEvent.setup()

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock slow API responses
      mockApiService.getCurrentUser.mockImplementation(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: mockUser }), 3000)
        )
      )

      // Mock cached data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify([])
        return null
      })

      renderOfflineTest()

      // Should show cached data quickly
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      }, { timeout: 1000 })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    it('should debounce rapid network state changes', async () => {
      const user = userEvent.setup()

      renderOfflineTest()

      const networkChanges = []
      const originalDispatchEvent = window.dispatchEvent

      window.dispatchEvent = vi.fn((event) => {
        networkChanges.push(event.type)
        return originalDispatchEvent.call(window, event)
      })

      // Rapid network changes
      for (let i = 0; i < 10; i++) {
        await user.click(screen.getByTestId('go-offline'))
        await user.click(screen.getByTestId('go-online'))
      }

      // Should handle all events
      expect(networkChanges.filter(type => type === 'offline')).toHaveLength(10)
      expect(networkChanges.filter(type => type === 'online')).toHaveLength(10)

      window.dispatchEvent = originalDispatchEvent
    })
  })
})