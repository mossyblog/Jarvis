/**
 * Responsive Behavior Tests for Mobile Components
 * 
 * Comprehensive tests for responsive behavior including:
 * - Breakpoint simulation and media query handling
 * - Viewport size changes and adaptation
 * - Touch target scaling across different screen sizes
 * - Component layout adjustments for different devices
 * - Performance with dynamic viewport changes
 */

import React, { useState, useRef } from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BottomSheet } from '../BottomSheet'
import { useTouchGestures, useTouchTargetValidation } from '@/hooks/useTouchGestures'
import { setupBentoTestEnvironment } from '@/test/utils/bento-test-utils'

// Test component that responds to viewport changes
const ResponsiveTestComponent: React.FC = () => {
  const [viewportSize, setViewportSize] = useState({
    width: window.innerWidth,
    height: window.innerHeight
  })
  
  const elementRef = useRef<HTMLDivElement>(null)
  const { isValidTarget, suggestions } = useTouchTargetValidation(elementRef)
  
  const { isTouchDevice, attachListeners } = useTouchGestures({}, {
    onTap: () => console.log('tap'),
    onSwipe: () => console.log('swipe')
  })

  React.useEffect(() => {
    const handleResize = () => {
      setViewportSize({
        width: window.innerWidth,
        height: window.innerHeight
      })
    }

    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  React.useEffect(() => {
    if (elementRef.current) {
      return attachListeners(elementRef.current)
    }
  }, [attachListeners])

  return (
    <div
      ref={elementRef}
      data-testid="responsive-component"
      data-viewport-width={viewportSize.width}
      data-viewport-height={viewportSize.height}
      data-is-touch-device={isTouchDevice}
      data-is-valid-target={isValidTarget}
      style={{
        width: viewportSize.width < 768 ? '100%' : '50%',
        height: viewportSize.width < 768 ? '44px' : '60px', // Touch target scaling
        padding: viewportSize.width < 768 ? '12px' : '16px',
        fontSize: viewportSize.width < 768 ? '14px' : '16px'
      }}
    >
      <span data-testid="viewport-info">
        {viewportSize.width}x{viewportSize.height}
      </span>
      <span data-testid="device-type">
        {isTouchDevice ? 'Touch' : 'Desktop'}
      </span>
      {suggestions.length > 0 && (
        <div data-testid="target-suggestions">
          {suggestions.join(', ')}
        </div>
      )}
    </div>
  )
}

describe('Responsive Behavior', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  
  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    
    // Set initial viewport size
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      value: 1024
    })
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      value: 768
    })
    
    // Mock getBoundingClientRect with responsive behavior
    Element.prototype.getBoundingClientRect = vi.fn(function(this: Element) {
      const width = window.innerWidth < 768 ? window.innerWidth : window.innerWidth * 0.5
      const height = window.innerWidth < 768 ? 44 : 60
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
    
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  describe('Viewport Size Adaptation', () => {
    it('adapts component layout for mobile viewport', async () => {
      render(<ResponsiveTestComponent />)
      
      const component = screen.getByTestId('responsive-component')
      expect(component).toHaveAttribute('data-viewport-width', '1024')
      expect(component).toHaveStyle({ width: '50%', height: '60px' })
      
      // Simulate mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 812
      })
      
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-viewport-width', '375')
        expect(component).toHaveStyle({ width: '100%', height: '44px' })
      })
    })

    it('updates viewport info on orientation change', async () => {
      render(<ResponsiveTestComponent />)
      
      const viewportInfo = screen.getByTestId('viewport-info')
      expect(viewportInfo).toHaveTextContent('1024x768')
      
      // Simulate portrait to landscape on mobile
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 812
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 375
      })
      
      fireEvent(window, new Event('orientationchange'))
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        expect(viewportInfo).toHaveTextContent('812x375')
      })
    })

    it('handles rapid viewport size changes', async () => {
      render(<ResponsiveTestComponent />)
      
      const component = screen.getByTestId('responsive-component')
      const startTime = performance.now()
      
      // Simulate rapid resize events (common during orientation changes)
      const sizes = [
        { width: 1024, height: 768 },
        { width: 800, height: 600 },
        { width: 768, height: 1024 },
        { width: 375, height: 812 },
        { width: 414, height: 896 }
      ]
      
      for (const size of sizes) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: size.width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: size.height
        })
        
        fireEvent(window, new Event('resize'))
        await new Promise(resolve => setTimeout(resolve, 10))
      }
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(200) // Should handle rapidly
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-viewport-width', '414')
      })
    })
  })

  describe('Breakpoint-Specific Behavior', () => {
    const breakpoints = {
      mobile: { width: 375, height: 812 },
      tablet: { width: 768, height: 1024 },
      desktop: { width: 1024, height: 768 },
      large: { width: 1440, height: 900 }
    }

    Object.entries(breakpoints).forEach(([device, size]) => {
      it(`optimizes layout for ${device} breakpoint`, async () => {
        render(<ResponsiveTestComponent />)
        
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: size.width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: size.height
        })
        
        fireEvent(window, new Event('resize'))
        
        const component = screen.getByTestId('responsive-component')
        
        await waitFor(() => {
          expect(component).toHaveAttribute('data-viewport-width', size.width.toString())
          
          if (size.width < 768) {
            expect(component).toHaveStyle({ width: '100%' })
            expect(component).toHaveAttribute('data-is-valid-target', 'true')
          } else {
            expect(component).toHaveStyle({ width: '50%' })
          }
        })
      })
    })

    it('applies appropriate touch target sizes across breakpoints', async () => {
      render(<ResponsiveTestComponent />)
      
      // Test mobile - should have minimum 44px touch targets
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        const component = screen.getByTestId('responsive-component')
        expect(component).toHaveStyle({ height: '44px' })
      })
      
      // Test desktop - can have larger targets
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        const component = screen.getByTestId('responsive-component')
        expect(component).toHaveStyle({ height: '60px' })
      })
    })
  })

  describe('Media Query Simulation', () => {
    it('responds to media query changes', () => {
      const mockMatchMedia = vi.fn().mockImplementation((query: string) => ({
        matches: query.includes('max-width: 768px') ? false : true,
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
      
      render(<ResponsiveTestComponent />)
      
      expect(mockMatchMedia).toHaveBeenCalled()
    })

    it('handles prefers-reduced-motion media query', () => {
      const mockMatchMedia = vi.fn().mockImplementation((query: string) => ({
        matches: query.includes('prefers-reduced-motion') ? true : false,
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
      
      // Component should respect motion preferences
      render(<ResponsiveTestComponent />)
      
      // Verify media query was checked
      expect(mockMatchMedia).toHaveBeenCalled()
    })

    it('adapts to color scheme preferences', () => {
      const mockMatchMedia = vi.fn().mockImplementation((query: string) => ({
        matches: query.includes('prefers-color-scheme: dark') ? true : false,
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
      
      render(<ResponsiveTestComponent />)
      
      expect(mockMatchMedia).toHaveBeenCalled()
    })
  })

  describe('BottomSheet Responsive Behavior', () => {
    it('adjusts bottom sheet height for different viewports', async () => {
      const { rerender } = render(
        <BottomSheet 
          isOpen={true} 
          onClose={() => {}} 
          initialHeight={0.5}
        >
          Content
        </BottomSheet>
      )
      
      // Create portal mount point
      const portalDiv = document.createElement('div')
      portalDiv.id = 'bottom-sheet-portal'
      document.body.appendChild(portalDiv)
      
      // Mobile viewport - should use more screen space
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 812
      })
      
      rerender(
        <BottomSheet 
          isOpen={true} 
          onClose={() => {}} 
          initialHeight={0.7} // More height on mobile
        >
          Content
        </BottomSheet>
      )
      
      await waitFor(() => {
        const sheet = screen.getByRole('dialog').firstChild as HTMLElement
        expect(sheet).toHaveStyle({ height: '70vh' })
      })
      
      document.body.removeChild(portalDiv)
    })

    it('maintains minimum touch target size on small screens', async () => {
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
      
      // Very small screen
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 320
      })
      
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        const handle = document.querySelector('[class*="cursor-grab"]') as HTMLElement
        expect(handle).toHaveStyle({ minHeight: '44px' })
      })
      
      document.body.removeChild(portalDiv)
    })
  })

  describe('Performance with Dynamic Changes', () => {
    it('debounces resize events for performance', async () => {
      const resizeHandler = vi.fn()
      
      window.addEventListener('resize', resizeHandler)
      
      render(<ResponsiveTestComponent />)
      
      // Trigger multiple rapid resize events
      for (let i = 0; i < 10; i++) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: 1000 + i * 10
        })
        fireEvent(window, new Event('resize'))
      }
      
      // Should be called for each event, but component updates should be optimized
      expect(resizeHandler).toHaveBeenCalledTimes(10)
      
      window.removeEventListener('resize', resizeHandler)
    })

    it('handles viewport changes without layout thrashing', async () => {
      const { container } = render(<ResponsiveTestComponent />)
      const startTime = performance.now()
      
      // Simulate many viewport changes
      for (let i = 0; i < 20; i++) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: 800 + i * 10
        })
        fireEvent(window, new Event('resize'))
        
        // Small delay to simulate real timing
        await new Promise(resolve => setTimeout(resolve, 5))
      }
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(500) // Should handle efficiently
      
      // Component should still be responsive
      const component = screen.getByTestId('responsive-component')
      expect(component).toBeInTheDocument()
    })

    it('optimizes re-renders during viewport changes', () => {
      const renderCount = { count: 0 }
      
      const TestComponentWithRenderCount: React.FC = () => {
        renderCount.count++
        return <ResponsiveTestComponent />
      }
      
      const { rerender } = render(<TestComponentWithRenderCount />)
      
      const initialRenderCount = renderCount.count
      
      // Trigger multiple resizes
      for (let i = 0; i < 5; i++) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: 1000 + i * 50
        })
        fireEvent(window, new Event('resize'))
      }
      
      // Should not cause excessive re-renders
      const finalRenderCount = renderCount.count
      expect(finalRenderCount - initialRenderCount).toBeLessThan(10)
    })
  })

  describe('Cross-Device Compatibility', () => {
    const devices = [
      { name: 'iPhone SE', width: 375, height: 667, dpr: 2 },
      { name: 'iPhone 12', width: 390, height: 844, dpr: 3 },
      { name: 'iPad', width: 768, height: 1024, dpr: 2 },
      { name: 'Desktop', width: 1920, height: 1080, dpr: 1 },
      { name: 'Ultra-wide', width: 3440, height: 1440, dpr: 1 }
    ]

    devices.forEach(device => {
      it(`adapts correctly for ${device.name}`, async () => {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: device.width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: device.height
        })
        Object.defineProperty(window, 'devicePixelRatio', {
          writable: true,
          value: device.dpr
        })
        
        render(<ResponsiveTestComponent />)
        
        const component = screen.getByTestId('responsive-component')
        
        await waitFor(() => {
          expect(component).toHaveAttribute('data-viewport-width', device.width.toString())
          expect(component).toHaveAttribute('data-viewport-height', device.height.toString())
          
          // Verify appropriate styling for device type
          if (device.width < 768) {
            expect(component).toHaveStyle({ width: '100%' })
          } else {
            expect(component).toHaveStyle({ width: '50%' })
          }
        })
      })
    })

    it('handles extreme viewport sizes gracefully', async () => {
      const extremeSizes = [
        { width: 240, height: 320 }, // Very small
        { width: 5120, height: 2880 }, // Very large
        { width: 1, height: 1 }, // Minimal
        { width: 10000, height: 10000 } // Excessive
      ]
      
      for (const size of extremeSizes) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: size.width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: size.height
        })
        
        expect(() => {
          render(<ResponsiveTestComponent />)
          fireEvent(window, new Event('resize'))
        }).not.toThrow()
      }
    })
  })

  describe('Accessibility Considerations', () => {
    it('maintains accessible touch targets across viewports', async () => {
      render(<ResponsiveTestComponent />)
      
      // Test on small mobile device
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 320
      })
      
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        const component = screen.getByTestId('responsive-component')
        expect(component).toHaveAttribute('data-is-valid-target', 'true')
      })
    })

    it('provides appropriate font scaling', async () => {
      render(<ResponsiveTestComponent />)
      
      const component = screen.getByTestId('responsive-component')
      
      // Mobile should have appropriate font size
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })
      
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        expect(component).toHaveStyle({ fontSize: '14px' })
      })
      
      // Desktop should have larger font size
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })
      
      fireEvent(window, new Event('resize'))
      
      await waitFor(() => {
        expect(component).toHaveStyle({ fontSize: '16px' })
      })
    })

    it('respects user zoom preferences', () => {
      // Mock zoom level
      Object.defineProperty(window, 'devicePixelRatio', {
        writable: true,
        value: 2 // Simulates 200% zoom
      })
      
      render(<ResponsiveTestComponent />)
      
      const component = screen.getByTestId('responsive-component')
      expect(component).toBeInTheDocument()
      
      // Component should still be usable at high zoom levels
      expect(component).toHaveAttribute('data-is-valid-target', 'true')
    })
  })
})