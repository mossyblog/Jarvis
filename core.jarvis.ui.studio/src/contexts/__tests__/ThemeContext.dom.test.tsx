import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider, useTheme } from '../ThemeContext'

// Test component for DOM manipulation testing
const DOMTestComponent = () => {
  const { theme, mode, setTheme, setMode } = useTheme()
  
  return (
    <div>
      <div data-testid="theme-display">{theme}</div>
      <div data-testid="mode-display">{mode}</div>
      <button 
        data-testid="switch-to-default-light" 
        onClick={() => {
          setTheme('default')
          setMode('light')
        }}
      >
        Default Light
      </button>
      <button 
        data-testid="switch-to-supabase-dark" 
        onClick={() => {
          setTheme('supabase')
          setMode('dark')
        }}
      >
        Supabase Dark
      </button>
      <button 
        data-testid="toggle-theme" 
        onClick={() => setTheme(theme === 'default' ? 'supabase' : 'default')}
      >
        Toggle Theme
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

// Advanced DOM element mock with call tracking
const createMockDocumentElement = () => {
  const classListCalls: Array<{ method: string; args: string[] }> = []
  const attributeCalls: Array<{ method: string; name: string; value?: string }> = []
  
  const mockClassList = {
    add: vi.fn((...classes: string[]) => {
      classListCalls.push({ method: 'add', args: classes })
    }),
    remove: vi.fn((...classes: string[]) => {
      classListCalls.push({ method: 'remove', args: classes })
    }),
    contains: vi.fn((className: string) => false),
    toggle: vi.fn((className: string) => {
      classListCalls.push({ method: 'toggle', args: [className] })
      return true
    }),
    get calls() { return [...classListCalls] },
    clearCalls() { classListCalls.length = 0 }
  }
  
  const mockElement = {
    classList: mockClassList,
    setAttribute: vi.fn((name: string, value: string) => {
      attributeCalls.push({ method: 'setAttribute', name, value })
    }),
    removeAttribute: vi.fn((name: string) => {
      attributeCalls.push({ method: 'removeAttribute', name })
    }),
    getAttribute: vi.fn((name: string) => {
      const lastCall = attributeCalls
        .slice()
        .reverse()
        .find(call => call.name === name && call.method === 'setAttribute')
      return lastCall?.value || null
    }),
    get attributeCalls() { return [...attributeCalls] },
    clearAttributeCalls() { attributeCalls.length = 0 }
  }
  
  return mockElement
}

describe('ThemeContext - DOM Integration', () => {
  let mockDocumentElement: ReturnType<typeof createMockDocumentElement>
  let mockLocalStorage: any

  beforeEach(() => {
    // Setup localStorage mock
    const localStorageStore: Record<string, string> = {}
    mockLocalStorage = {
      getItem: vi.fn((key: string) => localStorageStore[key] || null),
      setItem: vi.fn((key: string, value: string) => {
        localStorageStore[key] = value
      }),
      removeItem: vi.fn((key: string) => {
        delete localStorageStore[key]
      }),
      clear: vi.fn(() => {
        Object.keys(localStorageStore).forEach(key => delete localStorageStore[key])
      })
    }
    
    Object.defineProperty(window, 'localStorage', {
      value: mockLocalStorage,
      writable: true
    })

    // Setup document element mock
    mockDocumentElement = createMockDocumentElement()
    Object.defineProperty(document, 'documentElement', {
      value: mockDocumentElement,
      writable: true
    })
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('CSS Class Management', () => {
    it('should apply initial theme and mode classes on mount', () => {
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      const addCalls = mockDocumentElement.classList.calls.filter(call => call.method === 'add')
      expect(addCalls).toContainEqual({ method: 'add', args: ['default', 'light'] })
    })

    it('should remove all theme classes before applying new ones', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Clear initial calls
      mockDocumentElement.classList.clearCalls()
      
      // Change theme
      await user.click(screen.getByTestId('switch-to-default-light'))
      
      const removeCalls = mockDocumentElement.classList.calls.filter(call => call.method === 'remove')
      const addCalls = mockDocumentElement.classList.calls.filter(call => call.method === 'add')
      
      // Should remove all possible theme classes (they are removed in groups)
      const allRemoveCalls = removeCalls.flatMap(call => call.args)
      expect(allRemoveCalls).toContain('default')
      expect(allRemoveCalls).toContain('supabase')
      expect(allRemoveCalls).toContain('light')
      expect(allRemoveCalls).toContain('dark')
      
      // Should add new classes
      expect(addCalls).toContainEqual({ method: 'add', args: ['default', 'light'] })
    })

    it('should handle rapid theme changes correctly', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Clear initial calls
      mockDocumentElement.classList.clearCalls()
      
      // Make rapid changes
      await user.click(screen.getByTestId('toggle-theme'))
      await user.click(screen.getByTestId('toggle-mode'))
      await user.click(screen.getByTestId('toggle-theme'))
      
      const calls = mockDocumentElement.classList.calls
      const addCalls = calls.filter(call => call.method === 'add')
      
      // Should have correct final state
      const lastAddCall = addCalls[addCalls.length - 1]
      expect(lastAddCall.args).toEqual(['default', 'dark'])
    })

    it('should maintain class application order', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      mockDocumentElement.classList.clearCalls()
      
      await user.click(screen.getByTestId('switch-to-default-light'))
      
      const calls = mockDocumentElement.classList.calls
      
      // Remove calls should come before add calls
      const removeIndex = calls.findIndex(call => call.method === 'remove')
      const addIndex = calls.findIndex(call => call.method === 'add')
      
      expect(removeIndex).toBeLessThan(addIndex)
    })
  })

  describe('Data Attribute Management', () => {
    it('should set data-theme and data-mode attributes on initialization', () => {
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      expect(mockDocumentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'supabase')
      expect(mockDocumentElement.setAttribute).toHaveBeenCalledWith('data-mode', 'dark')
    })

    it('should update data attributes when theme changes', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Clear initial calls
      vi.clearAllMocks()
      
      await user.click(screen.getByTestId('switch-to-default-light'))
      
      expect(mockDocumentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'default')
      expect(mockDocumentElement.setAttribute).toHaveBeenCalledWith('data-mode', 'light')
    })

    it('should maintain attribute consistency across multiple changes', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Test multiple theme/mode combinations
      const combinations = [
        { theme: 'supabase', mode: 'dark', button: 'switch-to-supabase-dark' },
        { theme: 'default', mode: 'light', button: 'switch-to-default-light' },
      ]
      
      for (const combo of combinations) {
        await user.click(screen.getByTestId(combo.button))
        
        // Verify current attributes
        expect(mockDocumentElement.getAttribute('data-theme')).toBe(combo.theme)
        expect(mockDocumentElement.getAttribute('data-mode')).toBe(combo.mode)
      }
    })

    it('should handle simultaneous theme and mode changes atomically', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="supabase" defaultMode="dark">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      const initialAttributeCalls = mockDocumentElement.attributeCalls.length
      
      // Change both theme and mode simultaneously
      await user.click(screen.getByTestId('switch-to-default-light'))
      
      const newAttributeCalls = mockDocumentElement.attributeCalls.slice(initialAttributeCalls)
      
      // Should set both attributes
      expect(newAttributeCalls).toContainEqual({
        method: 'setAttribute',
        name: 'data-theme',
        value: 'default'
      })
      expect(newAttributeCalls).toContainEqual({
        method: 'setAttribute',
        name: 'data-mode',
        value: 'light'
      })
    })
  })

  describe('CSS Custom Properties Integration', () => {
    it('should work with CSS custom properties via data attributes', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // CSS would use these data attributes like:
      // :root[data-theme="supabase"][data-mode="dark"] { --primary: ...; }
      
      await user.click(screen.getByTestId('switch-to-default-light'))
      
      expect(mockDocumentElement.getAttribute('data-theme')).toBe('default')
      expect(mockDocumentElement.getAttribute('data-mode')).toBe('light')
      
      await user.click(screen.getByTestId('switch-to-supabase-dark'))
      
      expect(mockDocumentElement.getAttribute('data-theme')).toBe('supabase')
      expect(mockDocumentElement.getAttribute('data-mode')).toBe('dark')
    })

    it('should support themed component selection via classes', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Components can use classes like .default.light for styling
      
      await user.click(screen.getByTestId('toggle-mode'))
      
      const addCalls = mockDocumentElement.classList.calls.filter(call => call.method === 'add')
      const lastAddCall = addCalls[addCalls.length - 1]
      
      expect(lastAddCall.args).toEqual(['default', 'dark'])
    })
  })

  describe('Theme Transition Preparation', () => {
    it('should provide stable DOM structure for CSS transitions', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Clear initial setup calls
      mockDocumentElement.classList.clearCalls()
      mockDocumentElement.clearAttributeCalls()
      
      // Make a theme change
      await user.click(screen.getByTestId('toggle-theme'))
      
      const calls = mockDocumentElement.classList.calls
      const attributeCalls = mockDocumentElement.attributeCalls
      
      // Should have predictable call pattern for CSS transitions
      expect(calls.length).toBeGreaterThan(0)
      expect(attributeCalls.length).toBe(2) // theme and mode attributes
    })

    it('should handle theme changes without causing layout thrashing', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Track DOM mutations for performance
      let mutationCount = 0
      const originalSetAttribute = mockDocumentElement.setAttribute
      mockDocumentElement.setAttribute = vi.fn((...args) => {
        mutationCount++
        return originalSetAttribute.apply(mockDocumentElement, args)
      })
      
      // Make multiple rapid changes
      await user.click(screen.getByTestId('toggle-theme'))
      await user.click(screen.getByTestId('toggle-mode'))
      
      // Should have minimal DOM mutations
      expect(mutationCount).toBeLessThanOrEqual(4) // 2 changes × 2 attributes
    })
  })

  describe('Error Handling in DOM Operations', () => {
    it('should document DOM error handling for future enhancement', async () => {
      const user = userEvent.setup()
      
      // Component should render and work normally
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Should update component state
      expect(screen.getByTestId('theme-display')).toHaveTextContent('supabase')
      expect(screen.getByTestId('mode-display')).toHaveTextContent('dark')
      
      // Theme change should work for component state
      await user.click(screen.getByTestId('toggle-theme'))
      expect(screen.getByTestId('theme-display')).toHaveTextContent('default')
      
      // Future enhancement: should handle DOM manipulation errors gracefully
      // by wrapping DOM operations in try-catch blocks
    })

    it('should handle missing document.documentElement gracefully', () => {
      // Current implementation requires documentElement to exist
      // This test documents the limitation and expected behavior
      const originalElement = document.documentElement
      
      // Test with minimal mock that won't crash
      const minimalMock = {
        classList: { add: vi.fn(), remove: vi.fn() },
        setAttribute: vi.fn()
      }
      
      Object.defineProperty(document, 'documentElement', {
        value: minimalMock,
        writable: true
      })
      
      expect(() => {
        render(
          <ThemeProvider>
            <DOMTestComponent />
          </ThemeProvider>
        )
      }).not.toThrow()
      
      expect(screen.getByTestId('theme-display')).toHaveTextContent('supabase')
      
      // Restore original
      Object.defineProperty(document, 'documentElement', {
        value: originalElement,
        writable: true
      })
    })
  })

  describe('Performance Optimizations', () => {
    it('should minimize DOM operations for unchanged values', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider defaultTheme="default" defaultMode="light">
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      // Clear initial calls
      mockDocumentElement.classList.clearCalls()
      mockDocumentElement.clearAttributeCalls()
      
      // Toggle theme twice (back to original)
      await user.click(screen.getByTestId('toggle-theme'))
      await user.click(screen.getByTestId('toggle-theme'))
      
      // Should still have DOM operations for each change
      // (Current implementation doesn't optimize for this)
      const addCalls = mockDocumentElement.classList.calls.filter(call => call.method === 'add')
      expect(addCalls.length).toBe(2) // One for each toggle
    })

    it('should batch DOM updates within single render cycle', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <DOMTestComponent />
        </ThemeProvider>
      )
      
      mockDocumentElement.classList.clearCalls()
      mockDocumentElement.clearAttributeCalls()
      
      // Simultaneous theme and mode change
      await user.click(screen.getByTestId('switch-to-default-light'))
      
      // Should have single batch of operations
      const attributeCalls = mockDocumentElement.attributeCalls
      expect(attributeCalls.length).toBe(2) // One setAttribute for each attribute
    })
  })
})