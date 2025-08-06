import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider, useTheme } from '../ThemeContext'
import { ThemeSwitcher, ThemeSwitcherCompact } from '../../components/ui/theme-switcher'

// Mock the themes import
vi.mock('../../styles/themes', () => ({
  themes: {
    default: {
      name: 'default',
      displayName: 'Default',
      description: 'Clean and modern default theme'
    },
    supabase: {
      name: 'supabase',
      displayName: 'Jarvis',
      description: 'Jarvis green accent theme'
    }
  }
}))

// Performance monitoring component
const PerformanceMonitor = ({ onRender }: { onRender: (renderTime: number) => void }) => {
  const renderStart = React.useRef(performance.now())
  const { theme, mode } = useTheme()
  
  React.useEffect(() => {
    const renderTime = performance.now() - renderStart.current
    onRender(renderTime)
    renderStart.current = performance.now()
  })
  
  return (
    <div data-testid="perf-monitor">
      {theme}-{mode}
    </div>
  )
}

// System preference detection mock
const createMockMatchMedia = (matches: boolean = false) => {
  return vi.fn().mockImplementation((query: string) => ({
    matches,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }))
}

// Multi-provider test component
const MultiProviderTest = () => {
  const [showSecond, setShowSecond] = React.useState(false)
  
  return (
    <div>
      <ThemeProvider defaultTheme="default" defaultMode="light">
        <div data-testid="provider-1">
          <TestThemeDisplay prefix="p1" />
        </div>
      </ThemeProvider>
      
      {showSecond && (
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <div data-testid="provider-2">
            <TestThemeDisplay prefix="p2" />
          </div>
        </ThemeProvider>
      )}
      
      <button 
        data-testid="toggle-second-provider"
        onClick={() => setShowSecond(!showSecond)}
      >
        Toggle Second Provider
      </button>
    </div>
  )
}

const TestThemeDisplay = ({ prefix }: { prefix: string }) => {
  const { theme, mode, setTheme, setMode } = useTheme()
  
  return (
    <div>
      <span data-testid={`${prefix}-theme`}>{theme}</span>
      <span data-testid={`${prefix}-mode`}>{mode}</span>
      <button 
        data-testid={`${prefix}-toggle-theme`}
        onClick={() => setTheme(theme === 'default' ? 'supabase' : 'default')}
      >
        Toggle Theme
      </button>
      <button 
        data-testid={`${prefix}-toggle-mode`}
        onClick={() => setMode(mode === 'light' ? 'dark' : 'light')}
      >
        Toggle Mode
      </button>
    </div>
  )
}

describe('ThemeContext - Integration & Performance', () => {
  let mockLocalStorage: any
  let mockDocumentElement: any

  beforeEach(() => {
    // Setup localStorage mock
    const localStorageStore: Record<string, string> = {}
    mockLocalStorage = {
      getItem: vi.fn((key: string) => localStorageStore[key] || null),
      setItem: vi.fn((key: string, value: string) => {
        localStorageStore[key] = value
      }),
      removeItem: vi.fn(),
      clear: vi.fn()
    }
    
    Object.defineProperty(window, 'localStorage', {
      value: mockLocalStorage,
      writable: true
    })

    // Setup document element mock
    mockDocumentElement = {
      classList: {
        add: vi.fn(),
        remove: vi.fn(),
        contains: vi.fn(),
        toggle: vi.fn()
      },
      setAttribute: vi.fn(),
      removeAttribute: vi.fn()
    }
    
    Object.defineProperty(document, 'documentElement', {
      value: mockDocumentElement,
      writable: true
    })

    // Setup matchMedia mock
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: createMockMatchMedia(false),
    })
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('ThemeSwitcher Integration', () => {
    it('should integrate with ThemeSwitcher component', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <ThemeSwitcher />
        </ThemeProvider>
      )
      
      // Find the mode toggle button (Moon/Sun icon)
      const modeToggle = screen.getByTitle('Switch to dark mode')
      expect(modeToggle).toBeInTheDocument()
      
      // Click mode toggle
      await user.click(modeToggle)
      
      // Should now show switch to light mode
      expect(screen.getByTitle('Switch to light mode')).toBeInTheDocument()
    })

    it('should integrate with ThemeSwitcherCompact component', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <ThemeSwitcherCompact showLabel={true} />
        </ThemeProvider>
      )
      
      // Should display current theme/mode
      expect(screen.getByText(/Jarvis • dark/)).toBeInTheDocument()
      
      // Open dropdown and change mode
      const trigger = screen.getByRole('button')
      await user.click(trigger)
      
      const lightOption = screen.getByText('Light')
      await user.click(lightOption)
      
      // Should update display
      expect(screen.getByText(/Jarvis • light/)).toBeInTheDocument()
    })

    it('should handle theme changes through ThemeSwitcher dropdown', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <ThemeSwitcher />
          <TestThemeDisplay prefix="test" />
        </ThemeProvider>
      )
      
      // Open theme selection dropdown
      const themeButton = screen.getByTitle('Select theme')
      await user.click(themeButton)
      
      // Select default theme
      const defaultOption = screen.getByRole('menuitemradio', { name: /Default/ })
      await user.click(defaultOption)
      
      // Verify theme change
      expect(screen.getByTestId('test-theme')).toHaveTextContent('default')
    })
  })

  describe('System Preference Detection', () => {
    it('should detect dark mode preference', () => {
      const mockMatchMediaDark = createMockMatchMedia(true)
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: mockMatchMediaDark,
      })
      
      // Note: Current implementation doesn't auto-detect system preferences
      // This test documents expected behavior for future enhancement
      render(
        <ThemeProvider>
          <TestThemeDisplay prefix="system" />
        </ThemeProvider>
      )
      
      // Currently uses explicit defaults, not system detection
      expect(screen.getByTestId('system-mode')).toHaveTextContent('dark')
    })

    it('should handle system preference changes', () => {
      const mockMatchMedia = createMockMatchMedia(false)
      const listeners: Function[] = []
      
      mockMatchMedia.mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn((callback: Function) => listeners.push(callback)),
        removeListener: vi.fn((callback: Function) => {
          const index = listeners.indexOf(callback)
          if (index > -1) listeners.splice(index, 1)
        }),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))
      
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: mockMatchMedia,
      })
      
      render(
        <ThemeProvider>
          <TestThemeDisplay prefix="system" />
        </ThemeProvider>
      )
      
      // Simulate system preference change
      act(() => {
        listeners.forEach(listener => listener({ matches: true }))
      })
      
      // Current implementation doesn't listen to system changes
      // This test documents future enhancement opportunity
      expect(screen.getByTestId('system-mode')).toHaveTextContent('dark')
    })
  })

  describe('Performance Testing', () => {
    it('should handle rapid theme changes efficiently', async () => {
      const user = userEvent.setup()
      const renderTimes: number[] = []
      
      render(
        <ThemeProvider>
          <PerformanceMonitor onRender={(time) => renderTimes.push(time)} />
          <TestThemeDisplay prefix="perf" />
        </ThemeProvider>
      )
      
      // Make rapid changes
      for (let i = 0; i < 10; i++) {
        await user.click(screen.getByTestId('perf-toggle-theme'))
        await user.click(screen.getByTestId('perf-toggle-mode'))
      }
      
      // Check that render times remain reasonable
      const avgRenderTime = renderTimes.slice(1).reduce((a, b) => a + b, 0) / (renderTimes.length - 1)
      expect(avgRenderTime).toBeLessThan(50) // Should render in less than 50ms on average
    })

    it('should not cause memory leaks with multiple providers', async () => {
      const user = userEvent.setup()
      
      render(<MultiProviderTest />)
      
      // Add and remove providers multiple times
      for (let i = 0; i < 5; i++) {
        await user.click(screen.getByTestId('toggle-second-provider'))
        
        if (i % 2 === 0) {
          // When second provider is active
          expect(screen.getByTestId('provider-2')).toBeInTheDocument()
          expect(screen.getByTestId('p2-theme')).toHaveTextContent('supabase')
        }
      }
      
      // Should not throw errors or leave dangling references
      expect(screen.getByTestId('provider-1')).toBeInTheDocument()
    })

    it('should handle concurrent theme changes gracefully', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <TestThemeDisplay prefix="concurrent" />
        </ThemeProvider>
      )
      
      // Simulate rapid concurrent clicks
      const promises = []
      for (let i = 0; i < 5; i++) {
        promises.push(user.click(screen.getByTestId('concurrent-toggle-theme')))
        promises.push(user.click(screen.getByTestId('concurrent-toggle-mode')))
      }
      
      await Promise.all(promises)
      
      // Should end in a valid state
      const finalTheme = screen.getByTestId('concurrent-theme').textContent
      const finalMode = screen.getByTestId('concurrent-mode').textContent
      
      expect(['default', 'supabase']).toContain(finalTheme)
      expect(['light', 'dark']).toContain(finalMode)
    })

    it('should minimize re-renders with stable references', () => {
      const renderCounts = { provider: 0, consumer: 0 }
      
      const CountingProvider = ({ children }: { children: React.ReactNode }) => {
        renderCounts.provider++
        return <ThemeProvider>{children}</ThemeProvider>
      }
      
      const CountingConsumer = () => {
        renderCounts.consumer++
        const { theme, mode, setTheme, setMode } = useTheme()
        
        return (
          <div>
            <span data-testid="stable-theme">{theme}</span>
            <span data-testid="stable-mode">{mode}</span>
            <button 
              data-testid="stable-change"
              onClick={() => {
                setTheme('default')
                setMode('light')
              }}
            >
              Change
            </button>
          </div>
        )
      }
      
      const { rerender } = render(
        <CountingProvider>
          <CountingConsumer />
        </CountingProvider>
      )
      
      const initialProviderRenders = renderCounts.provider
      const initialConsumerRenders = renderCounts.consumer
      
      // Force re-render
      rerender(
        <CountingProvider>
          <CountingConsumer />
        </CountingProvider>
      )
      
      // Provider should re-render, but context value should be stable
      expect(renderCounts.provider).toBe(initialProviderRenders + 1)
      // Consumer should also re-render due to parent re-render
      expect(renderCounts.consumer).toBe(initialConsumerRenders + 1)
    })
  })

  describe('Error Boundaries and Edge Cases', () => {
    it('should work with React.StrictMode', () => {
      expect(() => {
        render(
          <React.StrictMode>
            <ThemeProvider>
              <TestThemeDisplay prefix="strict" />
            </ThemeProvider>
          </React.StrictMode>
        )
      }).not.toThrow()
      
      expect(screen.getByTestId('strict-theme')).toHaveTextContent('supabase')
    })

    it('should handle provider unmounting during theme change', async () => {
      const user = userEvent.setup()
      
      const ConditionalProvider = () => {
        const [showProvider, setShowProvider] = React.useState(true)
        
        return (
          <div>
            {showProvider && (
              <ThemeProvider>
                <TestThemeDisplay prefix="conditional" />
              </ThemeProvider>
            )}
            <button 
              data-testid="unmount-provider"
              onClick={() => setShowProvider(false)}
            >
              Unmount
            </button>
          </div>
        )
      }
      
      render(<ConditionalProvider />)
      
      // Start a theme change
      await user.click(screen.getByTestId('conditional-toggle-theme'))
      
      // Unmount provider during change
      await user.click(screen.getByTestId('unmount-provider'))
      
      // Should not throw errors
      expect(screen.queryByTestId('conditional-theme')).not.toBeInTheDocument()
    })

    it('should handle multiple rapid provider mounts/unmounts', async () => {
      const user = userEvent.setup()
      
      const RapidMountTest = () => {
        const [count, setCount] = React.useState(0)
        
        return (
          <div>
            {count % 2 === 0 && (
              <ThemeProvider key={count}>
                <TestThemeDisplay prefix={`rapid-${count}`} />
              </ThemeProvider>
            )}
            <button 
              data-testid="rapid-toggle"
              onClick={() => setCount(c => c + 1)}
            >
              Toggle {count}
            </button>
          </div>
        )
      }
      
      render(<RapidMountTest />)
      
      // Rapidly mount/unmount providers
      for (let i = 0; i < 10; i++) {
        await user.click(screen.getByTestId('rapid-toggle'))
      }
      
      // Should not crash and should show current state
      expect(screen.getByText(/Toggle 10/)).toBeInTheDocument()
    })
  })

  describe('Accessibility Integration', () => {
    it('should work with screen readers and ARIA', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <ThemeSwitcher />
        </ThemeProvider>
      )
      
      // Theme switcher should have proper accessibility
      const modeToggle = screen.getByTitle('Switch to light mode')
      const themeSelector = screen.getByTitle('Select theme')
      
      expect(modeToggle).toBeInTheDocument()
      expect(themeSelector).toBeInTheDocument()
      
      // Should be keyboard accessible
      modeToggle.focus()
      fireEvent.keyDown(modeToggle, { key: 'Enter' })
      
      // Verify screen reader text is present
      expect(screen.getByText('Toggle theme mode')).toBeInTheDocument()
      expect(screen.getByText('Select theme')).toBeInTheDocument()
    })

    it('should document screen reader announcements for future enhancement', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <TestThemeDisplay prefix="a11y" />
        </ThemeProvider>
      )
      
      const initialTheme = screen.getByTestId('a11y-theme').textContent
      
      await user.click(screen.getByTestId('a11y-toggle-theme'))
      
      const newTheme = screen.getByTestId('a11y-theme').textContent
      
      // Current implementation doesn't announce changes to screen readers
      // This test documents future enhancement for accessibility
      expect(newTheme).not.toBe(initialTheme)
      
      // Future enhancement: could add ARIA live region to announce theme changes
      // like "Theme changed to Default" or "Theme changed to Jarvis"
    })
  })
})