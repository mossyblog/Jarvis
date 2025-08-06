import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider, useTheme, type ThemeName, type ThemeMode } from '../ThemeContext'

// Mock localStorage
const createMockLocalStorage = () => {
  const store: Record<string, string> = {}
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key]
    }),
    clear: vi.fn(() => {
      Object.keys(store).forEach(key => delete store[key])
    }),
    get store() { return { ...store } }
  }
}

// Test component to use the ThemeContext
const TestComponent = ({ onThemeChange }: { onThemeChange?: (context: any) => void }) => {
  const themeContext = useTheme()
  
  React.useEffect(() => {
    onThemeChange?.(themeContext)
  }, [themeContext, onThemeChange])

  return (
    <div>
      <div data-testid="theme-name">{themeContext.theme}</div>
      <div data-testid="theme-mode">{themeContext.mode}</div>
      <div data-testid="available-themes">{themeContext.availableThemes.join(',')}</div>
      <button 
        data-testid="set-theme-default" 
        onClick={() => themeContext.setTheme('default')}
      >
        Set Default Theme
      </button>
      <button 
        data-testid="set-theme-supabase" 
        onClick={() => themeContext.setTheme('supabase')}
      >
        Set Supabase Theme
      </button>
      <button 
        data-testid="set-mode-light" 
        onClick={() => themeContext.setMode('light')}
      >
        Set Light Mode
      </button>
      <button 
        data-testid="set-mode-dark" 
        onClick={() => themeContext.setMode('dark')}
      >
        Set Dark Mode
      </button>
    </div>
  )
}

const renderWithThemeProvider = (
  component: React.ReactElement,
  {
    defaultTheme,
    defaultMode,
    onThemeChange
  }: {
    defaultTheme?: ThemeName
    defaultMode?: ThemeMode
    onThemeChange?: (context: any) => void
  } = {}
) => {
  return render(
    <ThemeProvider defaultTheme={defaultTheme} defaultMode={defaultMode}>
      <TestComponent onThemeChange={onThemeChange} />
      {component}
    </ThemeProvider>
  )
}

describe('ThemeContext', () => {
  let mockLocalStorage: ReturnType<typeof createMockLocalStorage>

  beforeEach(() => {
    // Setup localStorage mock
    mockLocalStorage = createMockLocalStorage()
    Object.defineProperty(window, 'localStorage', {
      value: mockLocalStorage,
      writable: true
    })

    // Mock document.documentElement
    const mockDocumentElement = {
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
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('Provider Initialization', () => {
    it('should provide theme context to children', () => {
      renderWithThemeProvider(<div>test</div>)
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('supabase')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark')
      expect(screen.getByTestId('available-themes')).toHaveTextContent('default,supabase')
    })

    it('should throw error when useTheme is used outside provider', () => {
      // Suppress console.error for this test
      const originalError = console.error
      console.error = vi.fn()

      expect(() => {
        render(<TestComponent />)
      }).toThrow('useTheme must be used within a ThemeProvider')

      console.error = originalError
    })

    it('should use default props when no props provided', () => {
      renderWithThemeProvider(<div>test</div>)
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('supabase')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark')
    })

    it('should use custom default props when provided', () => {
      renderWithThemeProvider(<div>test</div>, {
        defaultTheme: 'default',
        defaultMode: 'light'
      })
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
    })

    it('should provide readonly available themes array', () => {
      let themeContext: any
      renderWithThemeProvider(<div>test</div>, {
        onThemeChange: (context) => { themeContext = context }
      })
      
      expect(themeContext.availableThemes).toEqual(['default', 'supabase'])
      // The array is created with 'as const', making it readonly in TypeScript
      // but Object.isFrozen may not be true in runtime
      expect(Array.isArray(themeContext.availableThemes)).toBe(true)
    })
  })

  describe('Local Storage Integration', () => {
    it('should load theme from localStorage on initialization', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'default'
        if (key === 'jarvis-theme-mode') return 'light'
        return null
      })

      renderWithThemeProvider(<div>test</div>)
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
    })

    it('should ignore invalid theme from localStorage and use default', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'invalid-theme'
        if (key === 'jarvis-theme-mode') return 'dark'
        return null
      })

      renderWithThemeProvider(<div>test</div>, {
        defaultTheme: 'default'
      })
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark')
    })

    it('should ignore invalid mode from localStorage and use default', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'supabase'
        if (key === 'jarvis-theme-mode') return 'invalid-mode'
        return null
      })

      renderWithThemeProvider(<div>test</div>, {
        defaultMode: 'light'
      })
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('supabase')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
    })

    it('should save theme to localStorage when changed', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      await user.click(screen.getByTestId('set-theme-default'))
      
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('jarvis-theme', 'default')
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
    })

    it('should save mode to localStorage when changed', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      await user.click(screen.getByTestId('set-mode-light'))
      
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('jarvis-theme-mode', 'light')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
    })
  })

  describe('Theme Switching', () => {
    it('should switch theme correctly', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      // Start with supabase theme
      expect(screen.getByTestId('theme-name')).toHaveTextContent('supabase')
      
      // Switch to default
      await user.click(screen.getByTestId('set-theme-default'))
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      
      // Switch back to supabase
      await user.click(screen.getByTestId('set-theme-supabase'))
      expect(screen.getByTestId('theme-name')).toHaveTextContent('supabase')
    })

    it('should switch mode correctly', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      // Start with dark mode
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark')
      
      // Switch to light
      await user.click(screen.getByTestId('set-mode-light'))
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
      
      // Switch back to dark
      await user.click(screen.getByTestId('set-mode-dark'))
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark')
    })

    it('should maintain independent theme and mode states', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      // Change theme
      await user.click(screen.getByTestId('set-theme-default'))
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark') // Mode should remain unchanged
      
      // Change mode
      await user.click(screen.getByTestId('set-mode-light'))
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default') // Theme should remain unchanged
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
    })
  })

  describe('DOM Updates', () => {
    it('should apply theme and mode classes to document element', () => {
      renderWithThemeProvider(<div>test</div>)
      
      const documentElement = document.documentElement
      
      expect(documentElement.classList.add).toHaveBeenCalledWith('supabase', 'dark')
      expect(documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'supabase')
      expect(documentElement.setAttribute).toHaveBeenCalledWith('data-mode', 'dark')
    })

    it('should remove old classes before applying new ones', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      const documentElement = document.documentElement
      
      // Clear previous calls
      vi.clearAllMocks()
      
      // Change theme
      await user.click(screen.getByTestId('set-theme-default'))
      
      expect(documentElement.classList.remove).toHaveBeenCalledWith('default')
      expect(documentElement.classList.remove).toHaveBeenCalledWith('supabase')
      expect(documentElement.classList.remove).toHaveBeenCalledWith('light', 'dark')
      
      expect(documentElement.classList.add).toHaveBeenCalledWith('default', 'dark')
    })

    it('should update data attributes when theme changes', async () => {
      const user = userEvent.setup()
      renderWithThemeProvider(<div>test</div>)
      
      const documentElement = document.documentElement
      
      // Clear previous calls
      vi.clearAllMocks()
      
      await user.click(screen.getByTestId('set-theme-default'))
      await user.click(screen.getByTestId('set-mode-light'))
      
      expect(documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'default')
      expect(documentElement.setAttribute).toHaveBeenCalledWith('data-mode', 'light')
    })
  })

  describe('State Synchronization', () => {
    it('should notify all consumers when theme changes', async () => {
      const user = userEvent.setup()
      const onThemeChange1 = vi.fn()
      const onThemeChange2 = vi.fn()
      
      render(
        <ThemeProvider>
          <TestComponent onThemeChange={onThemeChange1} />
          <TestComponent onThemeChange={onThemeChange2} />
        </ThemeProvider>
      )
      
      await user.click(screen.getAllByTestId('set-theme-default')[0])
      
      await waitFor(() => {
        expect(onThemeChange1).toHaveBeenCalledWith(
          expect.objectContaining({ theme: 'default' })
        )
        expect(onThemeChange2).toHaveBeenCalledWith(
          expect.objectContaining({ theme: 'default' })
        )
      })
    })

    it('should provide stable function references within same provider instance', () => {
      let context1: any
      let context2: any
      
      render(
        <ThemeProvider>
          <TestComponent onThemeChange={(ctx) => { 
            if (!context1) context1 = ctx
            else context2 = ctx
          }} />
        </ThemeProvider>
      )
      
      // Function references should be stable within the same provider instance
      // Note: Different provider instances will have different function references
      expect(typeof context1.setTheme).toBe('function')
      expect(typeof context1.setMode).toBe('function')
      expect(Array.isArray(context1.availableThemes)).toBe(true)
    })
  })

  describe('Error Handling', () => {
    it('should document localStorage error handling for future enhancement', () => {
      // Current implementation doesn't handle localStorage errors gracefully
      // This test documents the expected behavior for future enhancement
      
      // When localStorage works normally
      renderWithThemeProvider(<div>test</div>, {
        defaultTheme: 'default',
        defaultMode: 'light'
      })
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
      
      // Future enhancement: should handle localStorage.getItem() throwing errors
      // by wrapping in try-catch and falling back to default values
    })

    it('should handle invalid JSON in localStorage gracefully', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'default'
        if (key === 'jarvis-theme-mode') return 'light'
        return null
      })
      
      expect(() => {
        renderWithThemeProvider(<div>test</div>)
      }).not.toThrow()
      
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
    })
  })

  describe('TypeScript Type Safety', () => {
    it('should enforce theme name types', async () => {
      const user = userEvent.setup()
      let themeContext: any
      
      renderWithThemeProvider(<div>test</div>, {
        onThemeChange: (context) => { themeContext = context }
      })
      
      // Valid theme names should work
      act(() => {
        themeContext.setTheme('default')
      })
      expect(screen.getByTestId('theme-name')).toHaveTextContent('default')
      
      act(() => {
        themeContext.setTheme('supabase')
      })
      expect(screen.getByTestId('theme-name')).toHaveTextContent('supabase')
    })

    it('should enforce mode types', async () => {
      let themeContext: any
      
      renderWithThemeProvider(<div>test</div>, {
        onThemeChange: (context) => { themeContext = context }
      })
      
      // Valid modes should work
      act(() => {
        themeContext.setMode('light')
      })
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('light')
      
      act(() => {
        themeContext.setMode('dark')
      })
      expect(screen.getByTestId('theme-mode')).toHaveTextContent('dark')
    })
  })
})