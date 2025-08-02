import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { LoginForm } from '../LoginForm'
import { AuthProvider } from '../../../contexts/AuthContext'
import { apiService } from '../../../services/api/apiService'

// Mock dependencies
vi.mock('../../../services/api/apiService')
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => vi.fn()
  }
})

const mockApiService = vi.mocked(apiService)
const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate
  }
})

const renderLoginForm = () => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <LoginForm />
      </AuthProvider>
    </BrowserRouter>
  )
}

const mockUser = {
  id: '1',
  email: 'test@example.com',
  name: 'Test User',
  roles: []
}

describe('LoginForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Mock console methods to avoid noise in tests
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Form Rendering', () => {
    it('should render login form with email and password fields', () => {
      renderLoginForm()
      
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
    })

    it('should have default values pre-filled', () => {
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement
      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement
      
      expect(emailInput.value).toBe('test@example.com')
      expect(passwordInput.value).toBe('TestPassword123!')
    })

    it('should have proper input types and attributes', () => {
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const passwordInput = screen.getByLabelText(/password/i)
      
      expect(emailInput).toHaveAttribute('type', 'email')
      expect(emailInput).toHaveAttribute('autoComplete', 'email')
      expect(passwordInput).toHaveAttribute('type', 'password')
      expect(passwordInput).toHaveAttribute('autoComplete', 'current-password')
    })
  })

  describe('Form Validation', () => {
    it('should show validation error for invalid email', async () => {
      const user = userEvent.setup()
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      // Clear email and enter invalid email
      await user.clear(emailInput)
      await user.type(emailInput, 'invalid-email')
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/invalid email address/i)).toBeInTheDocument()
      })
    })

    it('should show validation error for empty password', async () => {
      const user = userEvent.setup()
      renderLoginForm()
      
      const passwordInput = screen.getByLabelText(/password/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      // Clear password
      await user.clear(passwordInput)
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/password is required/i)).toBeInTheDocument()
      })
    })

    it('should show validation error for empty email', async () => {
      const user = userEvent.setup()
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      // Clear email
      await user.clear(emailInput)
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/invalid email address/i)).toBeInTheDocument()
      })
    })

    it('should not submit form with validation errors', async () => {
      const user = userEvent.setup()
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const passwordInput = screen.getByLabelText(/password/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      // Clear both fields
      await user.clear(emailInput)
      await user.clear(passwordInput)
      await user.click(submitButton)
      
      // Should show validation errors and not call API
      await waitFor(() => {
        expect(screen.getByText(/invalid email address/i)).toBeInTheDocument()
        expect(screen.getByText(/password is required/i)).toBeInTheDocument()
      })
      
      expect(mockApiService.login).not.toHaveBeenCalled()
    })
  })

  describe('Form Submission', () => {
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
        data: []
      })

      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const passwordInput = screen.getByLabelText(/password/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      // Use custom credentials
      await user.clear(emailInput)
      await user.clear(passwordInput)
      await user.type(emailInput, 'custom@example.com')
      await user.type(passwordInput, 'custompassword')
      
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(mockApiService.login).toHaveBeenCalledWith({
          email: 'custom@example.com',
          password: 'custompassword'
        })
      })
    })

    it('should handle login failure and show error message', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        error: {
          message: 'Invalid credentials',
          code: 'AUTH_INVALID_CREDENTIALS'
        }
      })

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
      })
      
      // Error should be displayed in destructive styling
      const errorElement = screen.getByText(/invalid credentials/i)
      expect(errorElement.closest('div')).toHaveClass('bg-destructive/10', 'text-destructive')
    })

    it('should handle network errors gracefully', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockRejectedValue(new Error('Network error'))

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/network error/i)).toBeInTheDocument()
      })
    })

    it('should handle non-Error exceptions', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockRejectedValue('String error')

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/login failed/i)).toBeInTheDocument()
      })
    })
  })

  describe('Loading States', () => {
    it('should show loading state during submission', async () => {
      const user = userEvent.setup()
      
      // Create a promise that we can control
      let resolveLogin: (value: any) => void
      const loginPromise = new Promise((resolve) => {
        resolveLogin = resolve
      })
      
      mockApiService.login.mockReturnValue(loginPromise as Promise<any>)

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      await user.click(submitButton)
      
      // Should show loading state
      expect(screen.getByRole('button', { name: /signing in/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled()
      
      // Resolve the promise
      act(() => {
        resolveLogin!({
          data: {
            user: mockUser,
            accessToken: 'token',
            refreshToken: 'refresh',
            expiresIn: 3600
          }
        })
      })
      
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
        expect(screen.getByRole('button', { name: /sign in/i })).not.toBeDisabled()
      })
    })

    it('should reset loading state after error', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        error: {
          message: 'Invalid credentials',
          code: 'AUTH_INVALID_CREDENTIALS'
        }
      })

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
      })
      
      // Loading state should be reset
      expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /sign in/i })).not.toBeDisabled()
    })
  })

  describe('Navigation', () => {
    beforeEach(() => {
      // Mock successful login for navigation tests
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: []
      })
    })

    it('should navigate to root after successful login', async () => {
      const user = userEvent.setup()
      
      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      // Wait for login to complete and navigation to occur
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/')
      }, { timeout: 1000 })
    })

    it('should add delay before navigation for state propagation', async () => {
      const user = userEvent.setup()
      
      // Use fake timers to test the delay
      vi.useFakeTimers()
      
      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      // Wait for login to complete
      await waitFor(() => {
        expect(mockApiService.login).toHaveBeenCalled()
      })
      
      // Navigation should not have happened yet
      expect(mockNavigate).not.toHaveBeenCalled()
      
      // Fast forward the timer
      act(() => {
        vi.advanceTimersByTime(100)
      })
      
      // Now navigation should have occurred
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/')
      })
      
      vi.useRealTimers()
    })
  })

  describe('Error Handling Edge Cases', () => {
    it('should clear previous error when new submission starts', async () => {
      const user = userEvent.setup()
      
      // First submission fails
      mockApiService.login.mockResolvedValueOnce({
        error: {
          message: 'First error',
          code: 'ERROR_1'
        }
      })

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      await waitFor(() => {
        expect(screen.getByText(/first error/i)).toBeInTheDocument()
      })
      
      // Second submission succeeds
      mockApiService.login.mockResolvedValueOnce({
        data: {
          user: mockUser,
          accessToken: 'token',
          refreshToken: 'refresh',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: []
      })
      
      await user.click(submitButton)
      
      // Error should be cleared
      await waitFor(() => {
        expect(screen.queryByText(/first error/i)).not.toBeInTheDocument()
      })
    })

    it('should handle empty error message', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        error: {
          message: '',
          code: 'EMPTY_MESSAGE'
        }
      })

      renderLoginForm()
      
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      await user.click(submitButton)
      
      // Should show the empty message
      await waitFor(() => {
        const errorDiv = screen.getByText(/^$/)
        expect(errorDiv).toBeInTheDocument()
      })
    })
  })

  describe('Form Accessibility', () => {
    it('should have proper ARIA labels and attributes', () => {
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const passwordInput = screen.getByLabelText(/password/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      expect(emailInput).toHaveAttribute('type', 'email')
      expect(passwordInput).toHaveAttribute('type', 'password')
      expect(submitButton).toHaveAttribute('type', 'submit')
      
      // Form should be accessible via form role
      expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
    })

    it('should show validation errors with proper ARIA attributes', async () => {
      const user = userEvent.setup()
      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      const submitButton = screen.getByRole('button', { name: /sign in/i })
      
      await user.clear(emailInput)
      await user.type(emailInput, 'invalid-email')
      await user.click(submitButton)
      
      await waitFor(() => {
        const errorMessage = screen.getByText(/invalid email address/i)
        expect(errorMessage).toBeInTheDocument()
        
        // Check that the input is marked as invalid
        expect(emailInput).toHaveAttribute('aria-invalid', 'true')
      })
    })
  })

  describe('Keyboard Interaction', () => {
    it('should submit form when Enter is pressed in password field', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'token',
          refreshToken: 'refresh',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: []
      })

      renderLoginForm()
      
      const passwordInput = screen.getByLabelText(/password/i)
      
      // Focus password field and press Enter
      await user.click(passwordInput)
      await user.keyboard('{Enter}')
      
      await waitFor(() => {
        expect(mockApiService.login).toHaveBeenCalled()
      })
    })

    it('should submit form when Enter is pressed in email field', async () => {
      const user = userEvent.setup()
      
      mockApiService.login.mockResolvedValue({
        data: {
          user: mockUser,
          accessToken: 'token',
          refreshToken: 'refresh',
          expiresIn: 3600
        }
      })

      mockApiService.getNavigation.mockResolvedValue({
        data: []
      })

      renderLoginForm()
      
      const emailInput = screen.getByLabelText(/email/i)
      
      // Focus email field and press Enter
      await user.click(emailInput)
      await user.keyboard('{Enter}')
      
      await waitFor(() => {
        expect(mockApiService.login).toHaveBeenCalled()
      })
    })
  })
})