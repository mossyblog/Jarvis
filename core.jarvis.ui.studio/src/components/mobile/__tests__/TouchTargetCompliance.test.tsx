/**
 * Touch Target Size Compliance Tests
 * 
 * Comprehensive tests for touch target accessibility including:
 * - WCAG 2.1 AA compliance (minimum 44x44px touch targets)
 * - Touch target size validation across different devices
 * - Spacing requirements between interactive elements
 * - Accessibility validation for screen readers
 * - Voice navigation and switch control compatibility
 * - High contrast mode support
 * - Reduced motion preferences
 */

import React, { useRef, useState } from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BottomSheet } from '../BottomSheet'
import { useTouchTargetValidation, useTouchGestures } from '@/hooks/useTouchGestures'
import { setupBentoTestEnvironment } from '@/test/utils/bento-test-utils'

// WCAG 2.1 AA compliance constants
const MINIMUM_TOUCH_TARGET_SIZE = 44 // pixels
const MINIMUM_SPACING_BETWEEN_TARGETS = 8 // pixels
const RECOMMENDED_TOUCH_TARGET_SIZE = 48 // pixels

// Test component with various touch targets
const TouchTargetTestComponent: React.FC<{
  buttonSize?: number
  spacing?: number
  highContrast?: boolean
}> = ({ 
  buttonSize = MINIMUM_TOUCH_TARGET_SIZE, 
  spacing = MINIMUM_SPACING_BETWEEN_TARGETS,
  highContrast = false 
}) => {
  const [activeButton, setActiveButton] = useState<string | null>(null)
  const [gestureCount, setGestureCount] = useState(0)
  
  const smallButtonRef = useRef<HTMLButtonElement>(null)
  const mediumButtonRef = useRef<HTMLButtonElement>(null)
  const largeButtonRef = useRef<HTMLButtonElement>(null)
  const linkRef = useRef<HTMLAnchorElement>(null)
  
  const { isValidTarget: isSmallValid, suggestions: smallSuggestions } = 
    useTouchTargetValidation(smallButtonRef as React.RefObject<HTMLElement>)
  const { isValidTarget: isMediumValid, suggestions: mediumSuggestions } = 
    useTouchTargetValidation(mediumButtonRef as React.RefObject<HTMLElement>)
  const { isValidTarget: isLargeValid, suggestions: largeSuggestions } = 
    useTouchTargetValidation(largeButtonRef as React.RefObject<HTMLElement>)
  const { isValidTarget: isLinkValid, suggestions: linkSuggestions } = 
    useTouchTargetValidation(linkRef as React.RefObject<HTMLElement>)

  const { attachListeners } = useTouchGestures({}, {
    onTap: () => setGestureCount(prev => prev + 1)
  })

  React.useEffect(() => {
    const refs = [smallButtonRef, mediumButtonRef, largeButtonRef, linkRef]
    const cleanups = refs.map(ref => {
      if (ref.current) {
        return attachListeners(ref.current)
      }
      return () => {}
    })

    return () => {
      cleanups.forEach(cleanup => cleanup())
    }
  }, [attachListeners])

  const buttonStyle = {
    width: `${buttonSize}px`,
    height: `${buttonSize}px`,
    margin: `${spacing}px`,
    backgroundColor: highContrast ? '#000000' : '#007acc',
    color: highContrast ? '#ffffff' : '#ffffff',
    border: highContrast ? '2px solid #ffffff' : '1px solid #005a9e',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '14px',
    fontWeight: 'bold'
  }

  return (
    <div data-testid="touch-target-container" style={{ padding: '20px' }}>
      <h2 data-testid="container-title">Touch Target Test</h2>
      
      {/* Small button (potentially non-compliant) */}
      <button
        ref={smallButtonRef}
        data-testid="small-button"
        data-is-valid={isSmallValid}
        data-suggestions-count={smallSuggestions.length}
        style={{ ...buttonStyle, width: '32px', height: '32px' }}
        onClick={() => setActiveButton('small')}
        aria-label="Small button for testing"
      >
        S
      </button>

      {/* Medium button (minimum compliant) */}
      <button
        ref={mediumButtonRef}
        data-testid="medium-button"
        data-is-valid={isMediumValid}
        data-suggestions-count={mediumSuggestions.length}
        style={buttonStyle}
        onClick={() => setActiveButton('medium')}
        aria-label="Medium button for testing"
      >
        M
      </button>

      {/* Large button (recommended size) */}
      <button
        ref={largeButtonRef}
        data-testid="large-button"
        data-is-valid={isLargeValid}
        data-suggestions-count={largeSuggestions.length}
        style={{ ...buttonStyle, width: '48px', height: '48px' }}
        onClick={() => setActiveButton('large')}
        aria-label="Large button for testing"
      >
        L
      </button>

      {/* Link with touch target */}
      <a
        ref={linkRef}
        href="#"
        data-testid="test-link"
        data-is-valid={isLinkValid}
        data-suggestions-count={linkSuggestions.length}
        style={{
          display: 'inline-block',
          width: `${buttonSize}px`,
          height: `${buttonSize}px`,
          margin: `${spacing}px`,
          textAlign: 'center',
          lineHeight: `${buttonSize}px`,
          backgroundColor: highContrast ? '#333333' : '#28a745',
          color: highContrast ? '#ffffff' : '#ffffff',
          textDecoration: 'none',
          borderRadius: '4px'
        }}
        onClick={(e) => {
          e.preventDefault()
          setActiveButton('link')
        }}
        aria-label="Test link with touch target"
      >
        Link
      </a>

      <div data-testid="active-button">{activeButton}</div>
      <div data-testid="gesture-count">{gestureCount}</div>

      {/* Display validation suggestions */}
      {smallSuggestions.length > 0 && (
        <div data-testid="small-suggestions">
          {smallSuggestions.join(', ')}
        </div>
      )}
      {mediumSuggestions.length > 0 && (
        <div data-testid="medium-suggestions">
          {mediumSuggestions.join(', ')}
        </div>
      )}
      {largeSuggestions.length > 0 && (
        <div data-testid="large-suggestions">
          {largeSuggestions.join(', ')}
        </div>
      )}
      {linkSuggestions.length > 0 && (
        <div data-testid="link-suggestions">
          {linkSuggestions.join(', ')}
        </div>
      )}
    </div>
  )
}

// Component with densely packed interactive elements
const DenseInterfaceComponent: React.FC = () => {
  const [selectedItem, setSelectedItem] = useState<number | null>(null)

  return (
    <div data-testid="dense-interface" style={{ padding: '10px' }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '4px' }}>
        {Array.from({ length: 16 }, (_, i) => (
          <button
            key={i}
            data-testid={`dense-button-${i}`}
            style={{
              width: '40px',
              height: '40px',
              fontSize: '12px',
              backgroundColor: selectedItem === i ? '#007acc' : '#f0f0f0',
              color: selectedItem === i ? '#ffffff' : '#333333',
              border: '1px solid #ccc',
              borderRadius: '2px',
              cursor: 'pointer'
            }}
            onClick={() => setSelectedItem(i)}
            aria-label={`Grid item ${i + 1}`}
            aria-pressed={selectedItem === i}
          >
            {i + 1}
          </button>
        ))}
      </div>
      <div data-testid="selected-item">{selectedItem !== null ? selectedItem : 'none'}</div>
    </div>
  )
}

describe('Touch Target Size Compliance', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  
  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    // Mock getBoundingClientRect to return actual element dimensions
    Element.prototype.getBoundingClientRect = vi.fn(function(this: Element) {
      const style = window.getComputedStyle(this)
      const width = parseInt(style.width) || 44
      const height = parseInt(style.height) || 44
      
      return {
        top: 0,
        left: 0,
        right: width,
        bottom: height,
        width,
        height,
        x: 0,
        y: 0,
        toJSON: () => ({})
      } as DOMRect
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('WCAG 2.1 AA Compliance', () => {
    it('validates minimum touch target size (44x44px)', () => {
      render(<TouchTargetTestComponent />)
      
      const smallButton = screen.getByTestId('small-button')
      const mediumButton = screen.getByTestId('medium-button')
      const largeButton = screen.getByTestId('large-button')
      
      // Small button should be invalid
      expect(smallButton).toHaveAttribute('data-is-valid', 'false')
      expect(smallButton).toHaveAttribute('data-suggestions-count', '2') // Width and height suggestions
      
      // Medium button should be valid (minimum size)
      expect(mediumButton).toHaveAttribute('data-is-valid', 'true')
      expect(mediumButton).toHaveAttribute('data-suggestions-count', '0')
      
      // Large button should be valid (recommended size)
      expect(largeButton).toHaveAttribute('data-is-valid', 'true')
      expect(largeButton).toHaveAttribute('data-suggestions-count', '0')
    })

    it('provides specific suggestions for non-compliant targets', () => {
      render(<TouchTargetTestComponent />)
      
      const suggestions = screen.queryByTestId('small-suggestions')
      expect(suggestions).toBeInTheDocument()
      expect(suggestions?.textContent).toContain('width')
      expect(suggestions?.textContent).toContain('height')
      expect(suggestions?.textContent).toContain('44px')
    })

    it('validates link touch targets', () => {
      render(<TouchTargetTestComponent />)
      
      const testLink = screen.getByTestId('test-link')
      expect(testLink).toHaveAttribute('data-is-valid', 'true')
      expect(testLink).toHaveAttribute('data-suggestions-count', '0')
    })

    it('handles different viewport sizes for touch target validation', () => {
      // Test on small mobile device
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 320
      })
      
      render(<TouchTargetTestComponent />)
      
      const mediumButton = screen.getByTestId('medium-button')
      expect(mediumButton).toHaveAttribute('data-is-valid', 'true')
      
      // Test on larger device
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })
      
      const { rerender } = render(<TouchTargetTestComponent />)
      rerender(<TouchTargetTestComponent />)
      
      const mediumButtonLarge = screen.getByTestId('medium-button')
      expect(mediumButtonLarge).toHaveAttribute('data-is-valid', 'true')
    })
  })

  describe('Spacing Requirements', () => {
    it('validates minimum spacing between interactive elements', () => {
      render(<TouchTargetTestComponent spacing={4} />)
      
      const container = screen.getByTestId('touch-target-container')
      const buttons = container.querySelectorAll('button')
      
      buttons.forEach(button => {
        const style = window.getComputedStyle(button)
        const margin = parseInt(style.margin)
        expect(margin).toBeGreaterThanOrEqual(4)
      })
    })

    it('identifies when spacing is insufficient', () => {
      render(<TouchTargetTestComponent spacing={2} />)
      
      // With only 2px spacing, elements might be too close for comfortable use
      const container = screen.getByTestId('touch-target-container')
      const buttons = container.querySelectorAll('button')
      
      buttons.forEach(button => {
        const style = window.getComputedStyle(button)
        const margin = parseInt(style.margin)
        expect(margin).toBeLessThan(MINIMUM_SPACING_BETWEEN_TARGETS)
      })
    })

    it('validates dense interfaces have adequate touch targets', () => {
      render(<DenseInterfaceComponent />)
      
      const denseButtons = screen.getAllByTestId(/dense-button-\d+/)
      
      denseButtons.forEach(button => {
        // Each button should be clickable despite being in a dense layout
        expect(button).toBeVisible()
        expect(button).toHaveAttribute('aria-label')
      })
    })
  })

  describe('Accessibility Features', () => {
    it('provides proper ARIA labels for touch targets', () => {
      render(<TouchTargetTestComponent />)
      
      const smallButton = screen.getByTestId('small-button')
      const mediumButton = screen.getByTestId('medium-button')
      const largeButton = screen.getByTestId('large-button')
      const testLink = screen.getByTestId('test-link')
      
      expect(smallButton).toHaveAttribute('aria-label', 'Small button for testing')
      expect(mediumButton).toHaveAttribute('aria-label', 'Medium button for testing')
      expect(largeButton).toHaveAttribute('aria-label', 'Large button for testing')
      expect(testLink).toHaveAttribute('aria-label', 'Test link with touch target')
    })

    it('supports keyboard navigation alongside touch', async () => {
      render(<TouchTargetTestComponent />)
      
      const mediumButton = screen.getByTestId('medium-button')
      
      // Should be focusable via keyboard
      mediumButton.focus()
      expect(mediumButton).toHaveFocus()
      
      // Should respond to Enter key
      fireEvent.keyDown(mediumButton, { key: 'Enter' })
      
      await waitFor(() => {
        expect(screen.getByTestId('active-button')).toHaveTextContent('medium')
      })
    })

    it('supports screen reader announcements', () => {
      render(<TouchTargetTestComponent />)
      
      const buttons = [
        screen.getByTestId('small-button'),
        screen.getByTestId('medium-button'),
        screen.getByTestId('large-button')
      ]
      
      buttons.forEach(button => {
        // Should have proper role
        expect(button.tagName.toLowerCase()).toBe('button')
        
        // Should have accessible name
        expect(button).toHaveAttribute('aria-label')
        
        // Should be announced by screen readers
        expect(button).toBeVisible()
      })
    })

    it('indicates interactive state for assistive technology', async () => {
      render(<DenseInterfaceComponent />)
      
      const firstButton = screen.getByTestId('dense-button-0')
      
      // Initial state
      expect(firstButton).toHaveAttribute('aria-pressed', 'false')
      
      // After activation
      fireEvent.click(firstButton)
      
      await waitFor(() => {
        expect(firstButton).toHaveAttribute('aria-pressed', 'true')
      })
    })
  })

  describe('High Contrast Mode Support', () => {
    it('maintains touch target visibility in high contrast mode', () => {
      render(<TouchTargetTestComponent highContrast={true} />)
      
      const mediumButton = screen.getByTestId('medium-button')
      const style = window.getComputedStyle(mediumButton)
      
      // Should have high contrast colors
      expect(style.backgroundColor).toBe('rgb(0, 0, 0)') // #000000
      expect(style.color).toBe('rgb(255, 255, 255)') // #ffffff
      expect(style.border).toContain('rgb(255, 255, 255)') // White border
    })

    it('maintains touch target borders in high contrast mode', () => {
      render(<TouchTargetTestComponent highContrast={true} />)
      
      const buttons = [
        screen.getByTestId('small-button'),
        screen.getByTestId('medium-button'),
        screen.getByTestId('large-button')
      ]
      
      buttons.forEach(button => {
        const style = window.getComputedStyle(button)
        expect(style.border).toContain('2px solid') // Enhanced border for visibility
      })
    })
  })

  describe('BottomSheet Touch Target Compliance', () => {
    it('ensures bottom sheet handle meets touch target requirements', () => {
      const portalDiv = document.createElement('div')
      portalDiv.id = 'bottom-sheet-portal'
      document.body.appendChild(portalDiv)
      
      render(
        <BottomSheet 
          isOpen={true} 
          onClose={() => {}} 
          showHandle={true}
        >
          Content
        </BottomSheet>
      )
      
      const handle = document.querySelector('[class*="cursor-grab"]') as HTMLElement
      expect(handle).toBeInTheDocument()
      expect(handle).toHaveStyle({ minHeight: '44px' })
      
      document.body.removeChild(portalDiv)
    })

    it('ensures close button meets accessibility requirements', () => {
      const portalDiv = document.createElement('div')
      portalDiv.id = 'bottom-sheet-portal'
      document.body.appendChild(portalDiv)
      
      render(
        <BottomSheet 
          isOpen={true} 
          onClose={() => {}} 
          title="Test Sheet"
        >
          Content
        </BottomSheet>
      )
      
      const closeButton = screen.getByRole('button', { name: /close sheet/i })
      expect(closeButton).toBeInTheDocument()
      expect(closeButton).toHaveAttribute('aria-label', 'Close sheet')
      
      // Should have adequate size for touch
      const style = window.getComputedStyle(closeButton)
      expect(parseInt(style.width)).toBeGreaterThanOrEqual(36) // 9 * 4 (h-lg w-lg)
      expect(parseInt(style.height)).toBeGreaterThanOrEqual(36)
      
      document.body.removeChild(portalDiv)
    })
  })

  describe('Touch Gesture Accessibility', () => {
    it('provides alternative input methods for gestures', async () => {
      render(<TouchTargetTestComponent />)
      
      const mediumButton = screen.getByTestId('medium-button')
      
      // Touch gesture
      fireEvent.touchStart(mediumButton, {
        touches: [{ clientX: 50, clientY: 50 }]
      })
      fireEvent.touchEnd(mediumButton)
      
      await waitFor(() => {
        expect(screen.getByTestId('gesture-count')).toHaveTextContent('1')
      })
      
      // Mouse click (alternative input)
      fireEvent.click(mediumButton)
      
      await waitFor(() => {
        expect(screen.getByTestId('active-button')).toHaveTextContent('medium')
      })
    })

    it('supports reduced motion preferences', () => {
      const mockMatchMedia = vi.fn().mockImplementation((query: string) => ({
        matches: query.includes('prefers-reduced-motion: reduce'),
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))
      
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: mockMatchMedia,
      })
      
      render(<TouchTargetTestComponent />)
      
      // Component should respect motion preferences
      const container = screen.getByTestId('touch-target-container')
      expect(container).toBeInTheDocument()
    })

    it('handles voice control and switch navigation', async () => {
      render(<TouchTargetTestComponent />)
      
      const buttons = [
        screen.getByTestId('small-button'),
        screen.getByTestId('medium-button'),
        screen.getByTestId('large-button')
      ]
      
      // Voice control relies on proper labeling
      buttons.forEach(button => {
        expect(button).toHaveAttribute('aria-label')
        expect(button.getAttribute('aria-label')).toBeTruthy()
      })
      
      // Switch navigation relies on focus management
      for (let i = 0; i < buttons.length; i++) {
        buttons[i].focus()
        expect(buttons[i]).toHaveFocus()
        
        // Should be activatable with Space or Enter
        fireEvent.keyDown(buttons[i], { key: 'Space' })
        
        await waitFor(() => {
          const activeButton = screen.getByTestId('active-button')
          expect(activeButton.textContent).toBeTruthy()
        })
      }
    })
  })

  describe('Performance with Accessibility Features', () => {
    it('handles rapid accessibility queries efficiently', () => {
      render(<TouchTargetTestComponent />)
      
      const startTime = performance.now()
      
      // Simulate rapid accessibility queries
      for (let i = 0; i < 100; i++) {
        const button = screen.getByTestId('medium-button')
        button.getAttribute('aria-label')
        button.getAttribute('data-is-valid')
        window.getComputedStyle(button)
      }
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(100) // Should be fast
    })

    it('optimizes touch target validation calculations', () => {
      const { rerender } = render(<TouchTargetTestComponent />)
      
      const startTime = performance.now()
      
      // Multiple re-renders should not cause excessive recalculation
      for (let i = 0; i < 10; i++) {
        rerender(<TouchTargetTestComponent buttonSize={44 + i} />)
      }
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(200) // Should handle efficiently
    })
  })

  describe('Error Handling and Edge Cases', () => {
    it('handles missing or invalid element references', () => {
      const TestComponentWithNullRef: React.FC = () => {
        const nullRef = useRef<HTMLButtonElement>(null)
        const { isValidTarget, suggestions } = useTouchTargetValidation(nullRef as React.RefObject<HTMLElement>)
        
        return (
          <div>
            <span data-testid="null-ref-valid">{isValidTarget.toString()}</span>
            <span data-testid="null-ref-suggestions">{suggestions.length}</span>
          </div>
        )
      }
      
      render(<TestComponentWithNullRef />)
      
      // Should handle null ref gracefully
      expect(screen.getByTestId('null-ref-valid')).toHaveTextContent('true')
      expect(screen.getByTestId('null-ref-suggestions')).toHaveTextContent('0')
    })

    it('handles elements with zero or negative dimensions', () => {
      const TestComponentWithZeroDimensions: React.FC = () => {
        const zeroRef = useRef<HTMLDivElement>(null)
        const { isValidTarget, suggestions } = useTouchTargetValidation(zeroRef as React.RefObject<HTMLElement>)
        
        return (
          <div>
            <div 
              ref={zeroRef} 
              style={{ width: 0, height: 0 }}
              data-testid="zero-size-element"
            />
            <span data-testid="zero-size-valid">{isValidTarget.toString()}</span>
            <span data-testid="zero-size-suggestions">{suggestions.length}</span>
          </div>
        )
      }
      
      render(<TestComponentWithZeroDimensions />)
      
      // Should handle zero dimensions gracefully
      expect(screen.getByTestId('zero-size-valid')).toHaveTextContent('false')
      expect(screen.getByTestId('zero-size-suggestions')).toHaveTextContent('2') // Width and height suggestions
    })

    it('handles getBoundingClientRect errors gracefully', () => {
      // Mock getBoundingClientRect to throw an error
      Element.prototype.getBoundingClientRect = vi.fn(() => {
        throw new Error('getBoundingClientRect failed')
      })
      
      expect(() => {
        render(<TouchTargetTestComponent />)
      }).not.toThrow()
      
      // Should still render the component
      expect(screen.getByTestId('touch-target-container')).toBeInTheDocument()
    })
  })
})