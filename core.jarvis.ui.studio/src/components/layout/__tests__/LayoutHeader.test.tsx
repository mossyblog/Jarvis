/**
 * @vitest-environment jsdom
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import LayoutHeader from '../LayoutHeader'
import { AuthProvider } from '../../../contexts/AuthContext'
import { EditModeProvider } from '../../../contexts/EditModeContext'

// Mock the contexts and services
vi.mock('../../../contexts/AuthContext', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  useAuth: () => ({
    user: {
      id: '123',
      name: 'Test User',
      email: 'test@example.com',
      roles: [{ name: 'Admin' }]
    },
    isAuthenticated: true,
    logout: vi.fn()
  })
}))

vi.mock('../../../contexts/EditModeContext', () => ({
  EditModeProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  useEditMode: () => ({
    isEditMode: false,
    hasUnsavedChanges: false,
    toggleEditMode: vi.fn()
  })
}))

// Mock the dropdown components
vi.mock('../dropdowns/OrganizationDropdown', () => ({
  OrganizationDropdown: () => <div data-testid="organization-dropdown">Organization</div>
}))

vi.mock('../UserMenu', () => ({
  UserMenu: () => <div data-testid="user-menu">User Menu</div>
}))

vi.mock('../DeviceSelector', () => ({
  DeviceSelector: () => <div data-testid="device-selector">Device Selector</div>
}))

vi.mock('../../ui/theme-switcher', () => ({
  ThemeSwitcher: () => <div data-testid="theme-switcher">Theme</div>
}))

const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <BrowserRouter>
    <AuthProvider>
      <EditModeProvider>
        {children}
      </EditModeProvider>
    </AuthProvider>
  </BrowserRouter>
)

describe('LayoutHeader', () => {
  beforeEach(() => {
    // Mock navigator.onLine
    Object.defineProperty(navigator, 'onLine', {
      writable: true,
      value: true
    })
  })

  it('renders the header with all main elements', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    expect(screen.getByRole('banner')).toBeInTheDocument()
    expect(screen.getByTestId('organization-dropdown')).toBeInTheDocument()
    expect(screen.getByTestId('user-menu')).toBeInTheDocument()
    expect(screen.getByTestId('theme-switcher')).toBeInTheDocument()
  })

  it('shows mobile menu button when showProductMenu is true', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const mobileMenuButton = screen.getByLabelText('Open mobile menu')
    expect(mobileMenuButton).toBeInTheDocument()
  })

  it('hides mobile menu button when showProductMenu is false', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={false} />
      </TestWrapper>
    )

    const mobileMenuButton = screen.queryByLabelText('Open mobile menu')
    expect(mobileMenuButton).not.toBeInTheDocument()
  })

  it('renders custom header components when provided', () => {
    const customComponent = <div data-testid="custom-component">Custom Content</div>
    
    render(
      <TestWrapper>
        <LayoutHeader 
          showProductMenu={true} 
          customHeaderComponents={customComponent}
        />
      </TestWrapper>
    )

    expect(screen.getByTestId('custom-component')).toBeInTheDocument()
  })

  it('shows notifications button for authenticated users', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const notificationsButton = screen.getByLabelText(/Notifications/)
    expect(notificationsButton).toBeInTheDocument()
  })

  it('shows settings button for authenticated users', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const settingsButton = screen.getByLabelText('Settings and preferences')
    expect(settingsButton).toBeInTheDocument()
  })

  it('shows help button', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const helpButton = screen.getByLabelText('Help and support')
    expect(helpButton).toBeInTheDocument()
  })

  it('shows quick actions button', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const quickActionsButton = screen.getByLabelText('Quick actions and help')
    expect(quickActionsButton).toBeInTheDocument()
  })

  it('shows connection status indicator', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const connectionStatus = screen.getByLabelText(/Network status:/)
    expect(connectionStatus).toBeInTheDocument()
  })

  it('opens mobile menu when mobile menu button is clicked', async () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const mobileMenuButton = screen.getByLabelText('Open mobile menu')
    fireEvent.click(mobileMenuButton)

    await waitFor(() => {
      expect(screen.getByText('Menu')).toBeInTheDocument()
    })
  })

  it('mobile menu functionality works correctly', async () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    // Initially mobile menu should not be visible
    expect(screen.queryByText('Menu')).not.toBeInTheDocument()

    // Open menu by clicking mobile menu button
    const mobileMenuButton = screen.getByLabelText('Open mobile menu')
    fireEvent.click(mobileMenuButton)

    // Menu should now be visible
    await waitFor(() => {
      expect(screen.getByText('Menu')).toBeInTheDocument()
    })

    // Verify mobile menu content is present
    expect(screen.getAllByTestId('device-selector').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Edit').length).toBeGreaterThan(0)
  })

  it('applies custom className when provided', () => {
    const { container } = render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} className="custom-header-class" />
      </TestWrapper>
    )

    const header = container.querySelector('header')
    expect(header).toHaveClass('custom-header-class')
  })

  it('has proper accessibility attributes', () => {
    render(
      <TestWrapper>
        <LayoutHeader showProductMenu={true} />
      </TestWrapper>
    )

    const header = screen.getByRole('banner')
    expect(header).toBeInTheDocument()

    // Check that interactive elements have proper labels
    const buttons = screen.getAllByRole('button')
    buttons.forEach(button => {
      expect(button).toHaveAttribute('aria-label')
    })
  })
})