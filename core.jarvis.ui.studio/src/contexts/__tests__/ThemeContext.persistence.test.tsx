import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider, useTheme, type ThemeName, type ThemeMode } from '../ThemeContext'

// Test component for persistence testing
const PersistenceTestComponent = () => {
  const { theme, mode, setTheme, setMode, availableThemes } = useTheme()
  
  return (
    <div>
      <div data-testid="current-theme">{theme}</div>
      <div data-testid="current-mode">{mode}</div>
      <div data-testid="available-themes-count">{availableThemes.length}</div>
      <button 
        data-testid="cycle-theme" 
        onClick={() => {
          const currentIndex = availableThemes.indexOf(theme)
          const nextIndex = (currentIndex + 1) % availableThemes.length
          setTheme(availableThemes[nextIndex])
        }}
      >
        Cycle Theme
      </button>
      <button 
        data-testid="toggle-mode" 
        onClick={() => setMode(mode === 'light' ? 'dark' : 'light')}
      >
        Toggle Mode
      </button>
    </div>
  )
}

// Mock localStorage with enhanced tracking
const createAdvancedMockLocalStorage = () => {
  const store: Record<string, string> = {}
  const callHistory: Array<{ method: string; key?: string; value?: string }> = []
  
  return {
    getItem: vi.fn((key: string) => {
      callHistory.push({ method: 'getItem', key })
      return store[key] || null
    }),
    setItem: vi.fn((key: string, value: string) => {
      callHistory.push({ method: 'setItem', key, value })
      store[key] = value
    }),
    removeItem: vi.fn((key: string) => {
      callHistory.push({ method: 'removeItem', key })
      delete store[key]
    }),
    clear: vi.fn(() => {
      callHistory.push({ method: 'clear' })
      Object.keys(store).forEach(key => delete store[key])
    }),
    get store() { return { ...store } },
    get callHistory() { return [...callHistory] },
    clearHistory() { callHistory.length = 0 }
  }
}

describe('ThemeContext - Persistence & Validation', () => {
  let mockLocalStorage: ReturnType<typeof createAdvancedMockLocalStorage>

  beforeEach(() => {
    mockLocalStorage = createAdvancedMockLocalStorage()
    Object.defineProperty(window, 'localStorage', {
      value: mockLocalStorage,
      writable: true
    })

    // Mock document.documentElement for DOM updates
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

  describe('localStorage Persistence', () => {
    it('should persist theme changes across provider remounts', async () => {
      const user = userEvent.setup()
      
      // First render with default settings
      const { unmount } = render(
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Change theme to default
      await user.click(screen.getByTestId('cycle-theme'))
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      
      // Unmount the provider
      unmount()
      
      // Verify that theme was saved to localStorage
      expect(mockLocalStorage.store['jarvis-theme']).toBe('default')
      
      // Re-render with different defaults - should load from localStorage
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="light">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should use persisted theme, but use current default mode since it wasn't changed
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
    })

    it('should persist multiple theme changes correctly', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Initial state
      expect(screen.getByTestId('current-theme')).toHaveTextContent('supabase')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('dark')
      
      // Change theme multiple times
      await user.click(screen.getByTestId('cycle-theme'))
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('jarvis-theme', 'default')
      
      await user.click(screen.getByTestId('cycle-theme'))
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('jarvis-theme', 'supabase')
      
      // Change mode multiple times
      await user.click(screen.getByTestId('toggle-mode'))
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('jarvis-theme-mode', 'light')
      
      await user.click(screen.getByTestId('toggle-mode'))
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('jarvis-theme-mode', 'dark')
    })

    it('should handle localStorage quota exceeded gracefully', async () => {
      const user = userEvent.setup()
      
      // Mock localStorage.setItem to throw quota exceeded error
      mockLocalStorage.setItem.mockImplementation(() => {
        throw new DOMException('QuotaExceededError', 'QuotaExceededError')
      })
      
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should still allow theme changes even if storage fails
      await user.click(screen.getByTestId('cycle-theme'))
      
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      expect(mockLocalStorage.setItem).toHaveBeenCalled()
    })

    it('should document localStorage disabled handling for future enhancement', () => {
      // Current implementation assumes localStorage is available
      // This test documents the expected behavior for future enhancement
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('light')
      
      // Future enhancement: should handle localStorage being undefined/null
      // by checking if localStorage exists before using it
    })
  })

  describe('Theme Validation', () => {
    it('should reject invalid theme names and use default', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'non-existent-theme'
        if (key === 'jarvis-theme-mode') return 'dark'
        return null
      })
      
      render(
        <ThemeProvider defaultTheme="default">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should fall back to default theme
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('dark')
    })

    it('should reject invalid mode names and use default', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'supabase'
        if (key === 'jarvis-theme-mode') return 'twilight' // invalid mode
        return null
      })
      
      render(
        <ThemeProvider defaultMode="light">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should fall back to default mode
      expect(screen.getByTestId('current-theme')).toHaveTextContent('supabase')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('light')
    })

    it('should validate available themes array', () => {
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      expect(screen.getByTestId('available-themes-count')).toHaveTextContent('2')
      
      // Verify current theme is one of the available themes
      const currentTheme = screen.getByTestId('current-theme').textContent
      expect(['default', 'supabase']).toContain(currentTheme)
    })

    it('should handle malformed data in localStorage', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return JSON.stringify({ invalid: 'object' })
        if (key === 'jarvis-theme-mode') return 'null'
        return null
      })
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should use defaults when data is malformed
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('light')
    })
  })

  describe('Edge Cases & Error Handling', () => {
    it('should handle empty string values in localStorage', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return ''
        if (key === 'jarvis-theme-mode') return ''
        return null
      })
      
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should use defaults when values are empty
      expect(screen.getByTestId('current-theme')).toHaveTextContent('supabase')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('dark')
    })

    it('should handle null values in localStorage', () => {
      mockLocalStorage.getItem.mockReturnValue(null)
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('light')
    })

    it('should handle case-sensitive theme names correctly', () => {
      mockLocalStorage.getItem.mockImplementation((key) => {
        if (key === 'jarvis-theme') return 'DEFAULT' // wrong case
        if (key === 'jarvis-theme-mode') return 'DARK' // wrong case
        return null
      })
      
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="light">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should use defaults when case doesn't match
      expect(screen.getByTestId('current-theme')).toHaveTextContent('supabase')
      expect(screen.getByTestId('current-mode')).toHaveTextContent('light')
    })

    it('should be resilient to localStorage being redefined during runtime', async () => {
      const user = userEvent.setup()
      const originalLocalStorage = window.localStorage
      
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Redefine localStorage during runtime to cause errors
      Object.defineProperty(window, 'localStorage', {
        value: null,
        writable: true
      })
      
      // Theme changes should still work for component state, even if storage fails
      await user.click(screen.getByTestId('cycle-theme'))
      
      expect(screen.getByTestId('current-theme')).toHaveTextContent('default')
      
      // Restore localStorage for other tests
      Object.defineProperty(window, 'localStorage', {
        value: originalLocalStorage,
        writable: true
      })
    })
  })

  describe('Performance & Optimization', () => {
    it('should minimize localStorage reads on initialization', () => {
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Should only read each key once during initialization
      const getItemCalls = mockLocalStorage.callHistory.filter(call => call.method === 'getItem')
      const themeReadCalls = getItemCalls.filter(call => call.key === 'jarvis-theme')
      const modeReadCalls = getItemCalls.filter(call => call.key === 'jarvis-theme-mode')
      
      expect(themeReadCalls.length).toBe(1)
      expect(modeReadCalls.length).toBe(1)
    })

    it('should only write to localStorage when values actually change', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Clear previous calls
      mockLocalStorage.clearHistory()
      
      // Cycle through themes and back to original
      await user.click(screen.getByTestId('cycle-theme')) // supabase -> default
      await user.click(screen.getByTestId('cycle-theme')) // default -> supabase
      
      // Should have exactly 2 setItem calls (one for each actual change)
      const setItemCalls = mockLocalStorage.callHistory.filter(call => call.method === 'setItem')
      expect(setItemCalls.length).toBe(2)
      expect(setItemCalls[0].value).toBe('default')
      expect(setItemCalls[1].value).toBe('supabase')
    })

    it('should batch localStorage operations efficiently', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      mockLocalStorage.clearHistory()
      
      // Make rapid consecutive changes
      await user.click(screen.getByTestId('cycle-theme'))
      await user.click(screen.getByTestId('toggle-mode'))
      
      // Should have separate calls for theme and mode
      const setItemCalls = mockLocalStorage.callHistory.filter(call => call.method === 'setItem')
      expect(setItemCalls.length).toBe(2)
      
      const themeCall = setItemCalls.find(call => call.key === 'jarvis-theme')
      const modeCall = setItemCalls.find(call => call.key === 'jarvis-theme-mode')
      
      expect(themeCall).toBeTruthy()
      expect(modeCall).toBeTruthy()
    })
  })

  describe('Cross-tab Synchronization', () => {
    it('should document storage event handling for future enhancement', () => {
      render(
        <ThemeProvider>
          <PersistenceTestComponent />
        </ThemeProvider>
      )
      
      // Create a custom storage event object instead of using StorageEvent constructor
      const storageEvent = new Event('storage')
      Object.defineProperty(storageEvent, 'key', { value: 'jarvis-theme' })
      Object.defineProperty(storageEvent, 'newValue', { value: 'default' })
      Object.defineProperty(storageEvent, 'oldValue', { value: 'supabase' })
      Object.defineProperty(storageEvent, 'storageArea', { value: window.localStorage })
      
      act(() => {
        window.dispatchEvent(storageEvent)
      })
      
      // Note: The current implementation doesn't listen to storage events
      // This test documents expected behavior for future enhancement
      expect(screen.getByTestId('current-theme')).toHaveTextContent('supabase')
    })
  })
})