import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider, useAuth } from '../../contexts/AuthContext'
import { apiService } from '../../services/api/apiService'
import { 
  getStoredTokens, 
  isTokenExpired, 
  getTimeUntilExpiry,
  clearTokens,
  storeTokens 
} from '../../utils/tokenUtils'
import type { User } from '../../services/api/types'

// Mock dependencies
vi.mock('../../services/api/apiService')
vi.mock('../../utils/tokenUtils')

const mockApiService = vi.mocked(apiService)
const mockGetStoredTokens = vi.mocked(getStoredTokens)
const mockIsTokenExpired = vi.mocked(isTokenExpired)
const mockGetTimeUntilExpiry = vi.mocked(getTimeUntilExpiry)
const mockClearTokens = vi.mocked(clearTokens)
const mockStoreTokens = vi.mocked(storeTokens)

// Test component to observe auth state
const AuthObserver = ({ onAuthChange }: { onAuthChange: (auth: any) => void }) => {
  const auth = useAuth()
  
  React.useEffect(() => {
    onAuthChange(auth)
  }, [auth, onAuthChange])

  return (
    <div>
      <div data-testid="auth-state">
        {auth.isLoading ? 'loading' : auth.isAuthenticated ? 'authenticated' : 'unauthenticated'}
      </div>
      <div data-testid="user-email">{auth.user?.email || 'no-user'}</div>
      <button onClick={() => auth.refreshAuth()} data-testid="refresh-btn">Refresh</button>
    </div>
  )
}

const renderAuthProvider = (onAuthChange?: (auth: any) => void) => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <AuthObserver onAuthChange={onAuthChange || (() => {})} />
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

describe('Authentication Session Management', () => {
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
    mockGetTimeUntilExpiry.mockReturnValue(0)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  describe('Token Refresh Scheduling', () => {
    it('should schedule token refresh before expiry', async () => {
      vi.useFakeTimers()
      
      const refreshTime = 10 * 60 * 1000 // 10 minutes
      const bufferTime = 5 * 60 * 1000   // 5 minutes buffer
      
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(refreshTime)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      // Mock successful refresh
      mockApiService.refreshToken.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresIn: 3600
        }
      })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Fast forward to refresh time (5 minutes before expiry)
      act(() => {
        vi.advanceTimersByTime(refreshTime - bufferTime)
      })

      await waitFor(() => {
        expect(mockApiService.refreshToken).toHaveBeenCalledWith('refresh-token')
      })

      vi.useRealTimers()
    })

    it('should handle failed automatic token refresh', async () => {
      vi.useFakeTimers()
      
      const refreshTime = 10 * 60 * 1000
      const bufferTime = 5 * 60 * 1000
      
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(refreshTime)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      // Mock failed refresh
      mockApiService.refreshToken.mockResolvedValue({
        error: {
          message: 'Refresh failed',
          code: 'REFRESH_FAILED'
        }
      })

      mockApiService.logout.mockResolvedValue(undefined)

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Fast forward to refresh time
      act(() => {
        vi.advanceTimersByTime(refreshTime - bufferTime)
      })

      // Should logout user when refresh fails
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
      })

      vi.useRealTimers()
    })

    it('should not schedule refresh for tokens expiring too soon', async () => {
      vi.useFakeTimers()
      
      const shortTime = 2 * 60 * 1000 // 2 minutes (less than 5 minute buffer)
      
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'soon-to-expire-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(shortTime)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Fast forward past the expiry time
      act(() => {
        vi.advanceTimersByTime(shortTime + 1000)
      })

      // Should not have attempted refresh
      expect(mockApiService.refreshToken).not.toHaveBeenCalled()

      vi.useRealTimers()
    })

    it('should cancel previous refresh timer when new one is scheduled', async () => {
      vi.useFakeTimers()
      
      let authState: any
      const onAuthChange = (auth: any) => { authState = auth }
      
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'token1',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(10 * 60 * 1000)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider(onAuthChange)

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Trigger a new login (which should reschedule refresh)
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'new-token',
          refreshToken: 'new-refresh-token',
          expiresIn: 3600
        }
      })

      mockGetTimeUntilExpiry.mockReturnValue(15 * 60 * 1000) // New longer expiry

      await act(async () => {
        await authState.login({ email: 'test@example.com', password: 'password' })
      })

      // Fast forward to original refresh time
      act(() => {
        vi.advanceTimersByTime(5 * 60 * 1000)
      })

      // Should not have triggered refresh yet (timer was cancelled and rescheduled)
      expect(mockApiService.refreshToken).not.toHaveBeenCalled()

      vi.useRealTimers()
    })
  })

  describe('Session Persistence', () => {
    it('should restore session from stored tokens on initialization', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'stored-access-token',
        refreshToken: 'stored-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
      expect(mockApiService.getCurrentUser).toHaveBeenCalled()
    })

    it('should handle corrupted stored user data gracefully', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock API timeout
      mockApiService.getCurrentUser.mockImplementation(() => 
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('API timeout')), 6000)
        )
      )

      // Mock corrupted stored data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return 'invalid-json{'
        if (key === 'jarvis_navigation') return null
        return null
      })

      renderAuthProvider()

      // Should handle gracefully and not crash
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      }, { timeout: 7000 })

      // Should have user as no-user due to corrupted data
      expect(screen.getByTestId('user-email')).toHaveTextContent('no-user')
    })

    it('should fallback to stored data when API is unavailable', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock API timeout
      mockApiService.getCurrentUser.mockRejectedValue(new Error('Network unavailable'))

      // Mock valid stored data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify([])
        return null
      })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })
  })

  describe('Token Expiry Handling', () => {
    it('should automatically refresh expired access token', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'valid-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock successful refresh
      mockApiService.refreshToken.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      expect(mockApiService.refreshToken).toHaveBeenCalledWith('valid-refresh-token')
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    it('should clear session when refresh token is invalid', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'invalid-refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock failed refresh
      mockApiService.refreshToken.mockResolvedValue({
        error: {
          message: 'Invalid refresh token',
          code: 'AUTH_INVALID_TOKEN'
        }
      })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
      })

      expect(mockClearTokens).toHaveBeenCalled()
      expect(localStorage.removeItem).toHaveBeenCalledWith('jarvis_current_user')
    })

    it('should handle refresh token network errors', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-access-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock network error during refresh
      mockApiService.refreshToken.mockRejectedValue(new Error('Network error'))

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
      })

      expect(mockClearTokens).toHaveBeenCalled()
    })
  })

  describe('API Timeout Handling', () => {
    it('should timeout API calls after 5 seconds', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock slow API response
      mockApiService.getCurrentUser.mockImplementation(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: mockUser }), 6000) // 6 seconds
        )
      )

      // Mock valid stored fallback data
      vi.mocked(localStorage.getItem).mockImplementation((key) => {
        if (key === 'jarvis_current_user') return JSON.stringify(mockUser)
        if (key === 'jarvis_navigation') return JSON.stringify([])
        return null
      })

      renderAuthProvider()

      // Should fallback to stored data after timeout
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      }, { timeout: 7000 })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    it('should timeout refresh token calls', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'expired-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(true)

      // Mock slow refresh response
      mockApiService.refreshToken.mockImplementation(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({
            data: {
              user: mockUser,
              accessToken: 'new-token',
              refreshToken: 'new-refresh',
              expiresIn: 3600
            }
          }), 6000)
        )
      )

      renderAuthProvider()

      // Should timeout and clear session
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
      }, { timeout: 7000 })

      expect(mockClearTokens).toHaveBeenCalled()
    })
  })

  describe('Manual Session Refresh', () => {
    it('should allow manual auth refresh', async () => {
      const user = userEvent.setup()

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      // Mock initial and refresh calls
      mockApiService.getCurrentUser
        .mockResolvedValueOnce({ data: mockUser })
        .mockResolvedValueOnce({ 
          data: { ...mockUser, name: 'Updated User' } 
        })

      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')

      // Trigger manual refresh
      await user.click(screen.getByTestId('refresh-btn'))

      await waitFor(() => {
        expect(mockApiService.getCurrentUser).toHaveBeenCalledTimes(2)
      })
    })

    it('should handle errors during manual refresh', async () => {
      const user = userEvent.setup()

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)

      mockApiService.getCurrentUser
        .mockResolvedValueOnce({ data: mockUser })
        .mockRejectedValueOnce(new Error('Refresh failed'))

      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Should not throw error on manual refresh failure
      await user.click(screen.getByTestId('refresh-btn'))

      // Should remain authenticated despite refresh error
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })
    })
  })

  describe('Cleanup and Memory Management', () => {
    it('should cleanup timeouts on unmount', () => {
      vi.useFakeTimers()
      
      const clearTimeoutSpy = vi.spyOn(global, 'clearTimeout')
      
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(10 * 60 * 1000)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      const { unmount } = renderAuthProvider()

      unmount()

      expect(clearTimeoutSpy).toHaveBeenCalled()

      vi.useRealTimers()
    })

    it('should cleanup on logout', async () => {
      vi.useFakeTimers()
      
      let authState: any
      const onAuthChange = (auth: any) => { authState = auth }

      mockGetStoredTokens.mockReturnValue({
        accessToken: 'valid-token',
        refreshToken: 'refresh-token'
      })
      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(10 * 60 * 1000)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })
      mockApiService.logout.mockResolvedValue(undefined)

      renderAuthProvider(onAuthChange)

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Logout should clear refresh timeout
      await act(async () => {
        await authState.logout()
      })

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('unauthenticated')
      })

      // Fast forward past original refresh time
      act(() => {
        vi.advanceTimersByTime(10 * 60 * 1000)
      })

      // Should not attempt refresh after logout
      expect(mockApiService.refreshToken).not.toHaveBeenCalled()

      vi.useRealTimers()
    })
  })

  describe('Edge Cases', () => {
    it('should handle missing refresh token during auto-refresh', async () => {
      vi.useFakeTimers()

      mockGetStoredTokens
        .mockReturnValueOnce({
          accessToken: 'valid-token',
          refreshToken: 'refresh-token'
        })
        .mockReturnValue({
          accessToken: 'valid-token',
          refreshToken: null // Refresh token missing
        })

      mockIsTokenExpired.mockReturnValue(false)
      mockGetTimeUntilExpiry.mockReturnValue(10 * 60 * 1000)

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Fast forward to refresh time
      act(() => {
        vi.advanceTimersByTime(5 * 60 * 1000)
      })

      // Should not attempt refresh without refresh token
      expect(mockApiService.refreshToken).not.toHaveBeenCalled()

      vi.useRealTimers()
    })

    it('should handle rapid token expiry during initialization', async () => {
      mockGetStoredTokens.mockReturnValue({
        accessToken: 'about-to-expire-token',
        refreshToken: 'refresh-token'
      })
      
      // Token expires during initialization
      mockIsTokenExpired
        .mockReturnValueOnce(false) // Initial check passes
        .mockReturnValue(true)      // But then it expires

      mockApiService.getCurrentUser.mockResolvedValue({ data: mockUser })
      mockApiService.refreshToken.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'new-token',
          refreshToken: 'new-refresh',
          expiresIn: 3600
        }
      })
      mockApiService.getNavigation.mockResolvedValue({ data: [] })

      renderAuthProvider()

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated')
      })

      // Should have attempted refresh
      expect(mockApiService.refreshToken).toHaveBeenCalled()
    })
  })
})