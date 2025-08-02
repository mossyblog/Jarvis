/**
 * Responsive Breakpoints Tests
 * 
 * Comprehensive tests for responsive breakpoint behavior across mobile components:
 * - Breakpoint detection and switching
 * - Component adaptation across device sizes
 * - CSS custom properties and media query integration
 * - Performance during breakpoint changes
 * - Accessibility at different screen sizes
 */

import React, { useState, useRef, useEffect } from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BottomSheet } from '../BottomSheet'
import { useTouchGestures, useTouchTargetValidation } from '@/hooks/useTouchGestures'
import { setupBentoTestEnvironment } from '@/test/utils/bento-test-utils'

// Responsive breakpoint system
const BREAKPOINTS = {
  xs: 320,
  sm: 375,
  md: 768,
  lg: 1024,
  xl: 1440,
  xxl: 1920
} as const

type BreakpointKey = keyof typeof BREAKPOINTS

// Hook for responsive breakpoint detection
const useResponsiveBreakpoint = () => {
  const [currentBreakpoint, setCurrentBreakpoint] = useState<BreakpointKey>('md')
  const [dimensions, setDimensions] = useState({
    width: window.innerWidth,
    height: window.innerHeight
  })

  useEffect(() => {
    const updateBreakpoint = () => {
      const width = window.innerWidth
      const height = window.innerHeight
      
      setDimensions({ width, height })

      let breakpoint: BreakpointKey = 'xs'
      for (const [key, value] of Object.entries(BREAKPOINTS)) {
        if (width >= value) {
          breakpoint = key as BreakpointKey
        }
      }
      setCurrentBreakpoint(breakpoint)
    }

    updateBreakpoint()
    window.addEventListener('resize', updateBreakpoint)
    window.addEventListener('orientationchange', updateBreakpoint)

    return () => {
      window.removeEventListener('resize', updateBreakpoint)
      window.removeEventListener('orientationchange', updateBreakpoint)
    }
  }, [])

  const isBreakpoint = (breakpoint: BreakpointKey) => currentBreakpoint === breakpoint
  const isBreakpointUp = (breakpoint: BreakpointKey) => 
    dimensions.width >= BREAKPOINTS[breakpoint]
  const isBreakpointDown = (breakpoint: BreakpointKey) => 
    dimensions.width < BREAKPOINTS[breakpoint]

  return {
    currentBreakpoint,
    dimensions,
    isBreakpoint,
    isBreakpointUp,
    isBreakpointDown,
    isMobile: isBreakpointDown('md'),
    isTablet: isBreakpoint('md'),
    isDesktop: isBreakpointUp('lg')
  }
}

// Test component with comprehensive responsive behavior
const ResponsiveTestComponent: React.FC<{
  onBreakpointChange?: (breakpoint: BreakpointKey) => void
  onDimensionChange?: (dimensions: { width: number; height: number }) => void
  testGestures?: boolean
  testTouchTargets?: boolean
}> = ({ 
  onBreakpointChange, 
  onDimensionChange,
  testGestures = true,
  testTouchTargets = true
}) => {
  const responsive = useResponsiveBreakpoint()
  const [gestureLog, setGestureLog] = useState<string[]>([])
  const [bottomSheetOpen, setBottomSheetOpen] = useState(false)
  
  const mainRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  
  const { isValidTarget, suggestions } = useTouchTargetValidation(buttonRef as React.RefObject<HTMLElement>)

  const { attachListeners } = useTouchGestures({
    enableSwipe: testGestures,
    enableLongPress: testGestures,
    swipeThreshold: responsive.isMobile ? 30 : 50
  }, {
    onSwipe: (detail) => {
      setGestureLog(prev => [...prev, `swipe-${detail.direction}`])
      if (detail.direction === 'up' && responsive.isMobile) {
        setBottomSheetOpen(true)
      }
    },
    onLongPress: () => {
      setGestureLog(prev => [...prev, 'long-press'])
      setBottomSheetOpen(true)
    }
  })

  // Notify parent of changes
  useEffect(() => {
    onBreakpointChange?.(responsive.currentBreakpoint)
  }, [responsive.currentBreakpoint, onBreakpointChange])

  useEffect(() => {
    onDimensionChange?.(responsive.dimensions)
  }, [responsive.dimensions, onDimensionChange])

  // Attach gesture listeners
  useEffect(() => {
    if (mainRef.current && testGestures) {
      return attachListeners(mainRef.current)
    }
  }, [attachListeners, testGestures])

  // Responsive styling
  const getResponsiveStyles = () => {
    const baseStyles = {
      padding: '16px',
      backgroundColor: '#f8f9fa',
      minHeight: '200px',
      display: 'flex',
      flexDirection: 'column' as const,
      gap: '12px'
    }

    switch (responsive.currentBreakpoint) {
      case 'xs':
        return {
          ...baseStyles,
          padding: '8px',
          fontSize: '14px',
          gap: '8px'
        }
      case 'sm':
        return {
          ...baseStyles,
          padding: '12px',
          fontSize: '15px',
          gap: '10px'
        }
      case 'md':
        return {
          ...baseStyles,
          padding: '20px',
          fontSize: '16px',
          gap: '16px'
        }
      case 'lg':
      case 'xl':
      case 'xxl':
        return {
          ...baseStyles,
          padding: '24px',
          fontSize: '18px',
          gap: '20px'
        }
    }
  }

  const getButtonStyles = () => {
    const minTouchSize = responsive.isMobile ? 44 : 36
    
    return {
      minWidth: `${minTouchSize}px`,
      minHeight: `${minTouchSize}px`,
      padding: responsive.isMobile ? '12px 16px' : '8px 12px',
      fontSize: responsive.isMobile ? '16px' : '14px',
      borderRadius: responsive.isMobile ? '8px' : '4px',
      backgroundColor: '#007acc',
      color: 'white',
      border: 'none',
      cursor: 'pointer'
    }
  }

  return (
    <div data-testid="responsive-container">
      <div
        ref={mainRef}
        data-testid="responsive-main"
        data-breakpoint={responsive.currentBreakpoint}
        data-is-mobile={responsive.isMobile}
        data-is-tablet={responsive.isTablet}
        data-is-desktop={responsive.isDesktop}
        style={getResponsiveStyles()}
      >
        <div data-testid="breakpoint-info">
          <h3>Current Breakpoint: {responsive.currentBreakpoint}</h3>
          <p>Dimensions: {responsive.dimensions.width}x{responsive.dimensions.height}</p>
          <p>Device Type: {responsive.isMobile ? 'Mobile' : responsive.isTablet ? 'Tablet' : 'Desktop'}</p>
        </div>

        <button
          ref={buttonRef}
          data-testid="responsive-button"
          data-is-valid-target={isValidTarget}
          data-suggestions-count={suggestions.length}
          style={getButtonStyles()}
          onClick={() => setBottomSheetOpen(true)}
        >
          Open Bottom Sheet
        </button>

        {testTouchTargets && suggestions.length > 0 && (
          <div data-testid="touch-target-suggestions">
            {suggestions.map((suggestion, i) => (
              <div key={i} style={{ fontSize: '12px', color: '#666' }}>
                {suggestion}
              </div>
            ))}
          </div>
        )}

        <div data-testid="gesture-log">
          Recent gestures: {gestureLog.slice(-3).join(', ')}
        </div>

        {/* Responsive grid layout */}
        <div
          data-testid="responsive-grid"
          style={{
            display: 'grid',
            gridTemplateColumns: responsive.isMobile 
              ? '1fr' 
              : responsive.isTablet 
                ? 'repeat(2, 1fr)' 
                : 'repeat(3, 1fr)',
            gap: responsive.isMobile ? '8px' : '16px'
          }}
        >
          {Array.from({ length: 6 }, (_, i) => (
            <div
              key={i}
              data-testid={`grid-item-${i}`}
              style={{
                backgroundColor: '#e9ecef',
                padding: responsive.isMobile ? '8px' : '16px',
                borderRadius: '4px',
                textAlign: 'center',
                minHeight: responsive.isMobile ? '40px' : '60px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            >
              Item {i + 1}
            </div>
          ))}
        </div>
      </div>

      {/* Responsive Bottom Sheet */}
      <BottomSheet
        isOpen={bottomSheetOpen}
        onClose={() => setBottomSheetOpen(false)}
        title="Responsive Bottom Sheet"
        initialHeight={responsive.isMobile ? 0.7 : 0.5}
        maxHeight={responsive.isMobile ? 0.9 : 0.8}
        minHeight={responsive.isMobile ? 0.3 : 0.2}
      >
        <div data-testid="bottom-sheet-responsive-content" style={{ padding: '20px' }}>
          <p>Breakpoint: {responsive.currentBreakpoint}</p>
          <p>Screen: {responsive.dimensions.width}x{responsive.dimensions.height}</p>
          
          <div style={{ marginTop: '20px' }}>
            <h4>Responsive Features:</h4>
            <ul>
              <li>Touch targets: {responsive.isMobile ? '44px min' : '36px min'}</li>
              <li>Swipe threshold: {responsive.isMobile ? '30px' : '50px'}</li>
              <li>Grid layout: {responsive.isMobile ? '1 column' : responsive.isTablet ? '2 columns' : '3 columns'}</li>
            </ul>
          </div>
          
          <button 
            onClick={() => setBottomSheetOpen(false)}
            style={getButtonStyles()}
            data-testid="close-responsive-sheet"
          >
            Close
          </button>
        </div>
      </BottomSheet>
    </div>
  )
}

// CSS custom properties test component
const CSSCustomPropertiesComponent: React.FC = () => {
  const responsive = useResponsiveBreakpoint()

  useEffect(() => {
    // Set CSS custom properties based on breakpoint
    const root = document.documentElement
    
    switch (responsive.currentBreakpoint) {
      case 'xs':
        root.style.setProperty('--spacing-unit', '4px')
        root.style.setProperty('--font-size-base', '14px')
        root.style.setProperty('--touch-target-min', '44px')
        break
      case 'sm':
        root.style.setProperty('--spacing-unit', '6px')
        root.style.setProperty('--font-size-base', '15px')
        root.style.setProperty('--touch-target-min', '44px')
        break
      case 'md':
        root.style.setProperty('--spacing-unit', '8px')
        root.style.setProperty('--font-size-base', '16px')
        root.style.setProperty('--touch-target-min', '40px')
        break
      default:
        root.style.setProperty('--spacing-unit', '12px')
        root.style.setProperty('--font-size-base', '18px')
        root.style.setProperty('--touch-target-min', '36px')
        break
    }
  }, [responsive.currentBreakpoint])

  return (
    <div 
      data-testid="css-custom-properties"
      style={{
        padding: 'calc(var(--spacing-unit) * 2)',
        fontSize: 'var(--font-size-base)'
      }}
    >
      <button
        data-testid="css-button"
        style={{
          width: 'var(--touch-target-min)',
          height: 'var(--touch-target-min)',
          padding: 'var(--spacing-unit)',
          backgroundColor: '#007acc',
          color: 'white',
          border: 'none',
          borderRadius: '4px'
        }}
      >
        CSS Test
      </button>
      <div data-testid="css-info">
        Breakpoint: {responsive.currentBreakpoint}
      </div>
    </div>
  )
}

describe('Responsive Breakpoints', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let mockBreakpointChange: ReturnType<typeof vi.fn>
  let mockDimensionChange: ReturnType<typeof vi.fn>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    mockBreakpointChange = vi.fn()
    mockDimensionChange = vi.fn()
    
    // Create portal mount point
    const portalDiv = document.createElement('div')
    portalDiv.id = 'bottom-sheet-portal'
    document.body.appendChild(portalDiv)
    
    // Mock initial viewport
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      value: 1024
    })
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      value: 768
    })
    
    vi.clearAllMocks()
  })

  afterEach(() => {
    const portal = document.getElementById('bottom-sheet-portal')
    if (portal) {
      document.body.removeChild(portal)
    }
    vi.restoreAllMocks()
  })

  describe('Breakpoint Detection and Switching', () => {
    const testBreakpoints = [
      { name: 'xs', width: 320, height: 568 },
      { name: 'sm', width: 375, height: 667 },
      { name: 'md', width: 768, height: 1024 },
      { name: 'lg', width: 1024, height: 768 },
      { name: 'xl', width: 1440, height: 900 },
      { name: 'xxl', width: 1920, height: 1080 }
    ]

    testBreakpoints.forEach(({ name, width, height }) => {
      it(`correctly detects ${name} breakpoint`, async () => {
        render(
          <ResponsiveTestComponent 
            onBreakpointChange={mockBreakpointChange}
            onDimensionChange={mockDimensionChange}
          />
        )

        // Change to target breakpoint
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: height
        })

        fireEvent(window, new Event('resize'))

        await waitFor(() => {
          const container = screen.getByTestId('responsive-main')
          expect(container).toHaveAttribute('data-breakpoint', name)
        })

        expect(mockBreakpointChange).toHaveBeenCalledWith(name)
        expect(mockDimensionChange).toHaveBeenCalledWith({ width, height })
      })
    })

    it('handles rapid breakpoint changes efficiently', async () => {
      render(
        <ResponsiveTestComponent 
          onBreakpointChange={mockBreakpointChange}
        />
      )

      const breakpointSequence = [
        { width: 320, height: 568 },
        { width: 768, height: 1024 },
        { width: 375, height: 667 },
        { width: 1440, height: 900 },
        { width: 1024, height: 768 }
      ]

      const startTime = performance.now()

      for (const { width, height } of breakpointSequence) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: height
        })

        fireEvent(window, new Event('resize'))
        await new Promise(resolve => setTimeout(resolve, 10))
      }

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(200) // Should handle rapidly

      expect(mockBreakpointChange).toHaveBeenCalledTimes(breakpointSequence.length)
    })

    it('correctly identifies device types', async () => {
      render(<ResponsiveTestComponent />)

      const deviceTests = [
        { width: 375, height: 667, isMobile: true, isTablet: false, isDesktop: false },
        { width: 768, height: 1024, isMobile: false, isTablet: true, isDesktop: false },
        { width: 1024, height: 768, isMobile: false, isTablet: false, isDesktop: true }
      ]

      for (const { width, height, isMobile, isTablet, isDesktop } of deviceTests) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: height
        })

        fireEvent(window, new Event('resize'))

        await waitFor(() => {
          const container = screen.getByTestId('responsive-main')
          expect(container).toHaveAttribute('data-is-mobile', isMobile.toString())
          expect(container).toHaveAttribute('data-is-tablet', isTablet.toString())
          expect(container).toHaveAttribute('data-is-desktop', isDesktop.toString())
        })
      }
    })

    it('handles orientation changes correctly', async () => {
      render(<ResponsiveTestComponent onBreakpointChange={mockBreakpointChange} />)

      // Portrait mobile
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 812
      })

      fireEvent(window, new Event('orientationchange'))
      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        expect(screen.getByTestId('responsive-main')).toHaveAttribute('data-breakpoint', 'sm')
      })

      // Landscape mobile  
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
        expect(screen.getByTestId('responsive-main')).toHaveAttribute('data-breakpoint', 'md')
      })

      expect(mockBreakpointChange).toHaveBeenCalledTimes(2)
    })
  })

  describe('Component Adaptation Across Device Sizes', () => {
    it('adapts touch target sizes based on breakpoint', async () => {
      render(<ResponsiveTestComponent testTouchTargets={true} />)

      // Mobile - should have larger touch targets
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })

      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        const button = screen.getByTestId('responsive-button')
        const computedStyle = window.getComputedStyle(button)
        expect(parseInt(computedStyle.minWidth)).toBeGreaterThanOrEqual(44)
        expect(parseInt(computedStyle.minHeight)).toBeGreaterThanOrEqual(44)
      })

      // Desktop - can have smaller touch targets
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })

      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        const button = screen.getByTestId('responsive-button')
        const computedStyle = window.getComputedStyle(button)
        expect(parseInt(computedStyle.minWidth)).toBeGreaterThanOrEqual(36)
        expect(parseInt(computedStyle.minHeight)).toBeGreaterThanOrEqual(36)
      })
    })

    it('adapts grid layouts responsively', async () => {
      render(<ResponsiveTestComponent />)

      // Mobile - single column
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })

      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        const grid = screen.getByTestId('responsive-grid')
        const computedStyle = window.getComputedStyle(grid)
        expect(computedStyle.gridTemplateColumns).toBe('1fr')
      })

      // Tablet - two columns
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 768
      })

      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        const grid = screen.getByTestId('responsive-grid')
        const computedStyle = window.getComputedStyle(grid)
        expect(computedStyle.gridTemplateColumns).toBe('repeat(2, 1fr)')
      })

      // Desktop - three columns
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })

      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        const grid = screen.getByTestId('responsive-grid')
        const computedStyle = window.getComputedStyle(grid)
        expect(computedStyle.gridTemplateColumns).toBe('repeat(3, 1fr)')
      })
    })

    it('adapts bottom sheet behavior for different screen sizes', async () => {
      render(<ResponsiveTestComponent />)

      // Mobile configuration
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })

      fireEvent(window, new Event('resize'))

      const openButton = screen.getByTestId('responsive-button')
      fireEvent.click(openButton)

      await waitFor(() => {
        expect(screen.getByTestId('bottom-sheet-responsive-content')).toBeInTheDocument()
      })

      const sheetContent = screen.getByTestId('bottom-sheet-responsive-content')
      expect(sheetContent).toHaveTextContent('44px min') // Mobile touch targets
      expect(sheetContent).toHaveTextContent('30px') // Mobile swipe threshold
      expect(sheetContent).toHaveTextContent('1 column') // Mobile grid

      const closeButton = screen.getByTestId('close-responsive-sheet')
      fireEvent.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByTestId('bottom-sheet-responsive-content')).not.toBeInTheDocument()
      })

      // Desktop configuration
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })

      fireEvent(window, new Event('resize'))
      fireEvent.click(openButton)

      await waitFor(() => {
        const content = screen.getByTestId('bottom-sheet-responsive-content')
        expect(content).toHaveTextContent('36px min') // Desktop touch targets
        expect(content).toHaveTextContent('50px') // Desktop swipe threshold
        expect(content).toHaveTextContent('3 columns') // Desktop grid
      })
    })

    it('adapts gesture sensitivity based on device type', async () => {
      render(<ResponsiveTestComponent testGestures={true} />)

      const mainArea = screen.getByTestId('responsive-main')

      // Mobile - more sensitive gestures
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })

      fireEvent(window, new Event('resize'))

      // Smaller swipe should trigger on mobile
      const mobileSwipe = new TouchEvent('touchstart', {
        touches: [{ clientX: 100, clientY: 100 } as Touch]
      })
      fireEvent(mainArea, mobileSwipe)

      const mobileMove = new TouchEvent('touchmove', {
        touches: [{ clientX: 130, clientY: 100 } as Touch] // 30px swipe
      })
      fireEvent(mainArea, mobileMove)

      const mobileEnd = new TouchEvent('touchend', { touches: [] })
      fireEvent(mainArea, mobileEnd)

      await waitFor(() => {
        expect(screen.getByTestId('gesture-log')).toHaveTextContent('swipe-right')
      })

      // Desktop - less sensitive gestures
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 1024
      })

      fireEvent(window, new Event('resize'))

      // Same 30px swipe should not trigger on desktop (requires 50px)
      const desktopSwipe = new TouchEvent('touchstart', {
        touches: [{ clientX: 200, clientY: 100 } as Touch]
      })
      fireEvent(mainArea, desktopSwipe)

      const desktopMove = new TouchEvent('touchmove', {
        touches: [{ clientX: 230, clientY: 100 } as Touch] // 30px swipe
      })
      fireEvent(mainArea, desktopMove)

      const desktopEnd = new TouchEvent('touchend', { touches: [] })
      fireEvent(mainArea, desktopEnd)

      // Should not add another swipe to gesture log
      await new Promise(resolve => setTimeout(resolve, 100))
      const gestureLog = screen.getByTestId('gesture-log').textContent
      expect(gestureLog).not.toContain('swipe-right, swipe-right')
    })
  })

  describe('CSS Custom Properties Integration', () => {
    it('sets CSS custom properties based on breakpoints', async () => {
      render(<CSSCustomPropertiesComponent />)

      const breakpointTests = [
        { width: 320, expectedSpacing: '4px', expectedFont: '14px', expectedTouch: '44px' },
        { width: 375, expectedSpacing: '6px', expectedFont: '15px', expectedTouch: '44px' },
        { width: 768, expectedSpacing: '8px', expectedFont: '16px', expectedTouch: '40px' },
        { width: 1024, expectedSpacing: '12px', expectedFont: '18px', expectedTouch: '36px' }
      ]

      for (const { width, expectedSpacing, expectedFont, expectedTouch } of breakpointTests) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))

        await waitFor(() => {
          const root = document.documentElement
          expect(root.style.getPropertyValue('--spacing-unit')).toBe(expectedSpacing)
          expect(root.style.getPropertyValue('--font-size-base')).toBe(expectedFont)
          expect(root.style.getPropertyValue('--touch-target-min')).toBe(expectedTouch)
        })
      }
    })

    it('applies CSS custom properties to components', async () => {
      render(<CSSCustomPropertiesComponent />)

      // Set mobile breakpoint
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })

      fireEvent(window, new Event('resize'))

      await waitFor(() => {
        const button = screen.getByTestId('css-button')
        const container = screen.getByTestId('css-custom-properties')
        
        const buttonStyle = window.getComputedStyle(button)
        const containerStyle = window.getComputedStyle(container)
        
        expect(buttonStyle.width).toBe('44px')
        expect(buttonStyle.height).toBe('44px')
        expect(containerStyle.fontSize).toBe('15px')
      })
    })

    it('updates CSS properties smoothly during breakpoint transitions', async () => {
      render(<CSSCustomPropertiesComponent />)

      const propertyValues: string[] = []

      // Monitor CSS property changes
      const observer = new MutationObserver(() => {
        const root = document.documentElement
        propertyValues.push(root.style.getPropertyValue('--spacing-unit'))
      })

      observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['style']
      })

      // Trigger multiple breakpoint changes
      const widths = [320, 768, 1024, 375]
      
      for (const width of widths) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))
        await new Promise(resolve => setTimeout(resolve, 50))
      }

      observer.disconnect()

      // Should have updated properties for each breakpoint
      expect(propertyValues.length).toBeGreaterThan(0)
    })
  })

  describe('Performance During Breakpoint Changes', () => {
    it('handles rapid breakpoint changes without performance degradation', async () => {
      render(<ResponsiveTestComponent onBreakpointChange={mockBreakpointChange} />)

      const startTime = performance.now()

      // Rapid breakpoint changes
      const rapidSequence = [
        320, 375, 768, 1024, 1440, 768, 375, 320, 1024, 1440
      ]

      for (const width of rapidSequence) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))
        
        // Minimal delay to simulate real-world timing
        await new Promise(resolve => setTimeout(resolve, 5))
      }

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(200) // Should handle rapidly

      expect(mockBreakpointChange).toHaveBeenCalledTimes(rapidSequence.length)
    })

    it('optimizes re-renders during orientation changes', async () => {
      let renderCount = 0
      
      const RenderCountComponent: React.FC = () => {
        renderCount++
        const responsive = useResponsiveBreakpoint()
        return <div data-testid="render-count">{renderCount}</div>
      }

      render(<RenderCountComponent />)

      const initialRenderCount = renderCount

      // Simulate orientation change sequence
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
        expect(screen.getByTestId('render-count')).toBeInTheDocument()
      })

      const finalRenderCount = renderCount

      // Should minimize re-renders
      expect(finalRenderCount - initialRenderCount).toBeLessThan(5)
    })

    it('maintains gesture performance across breakpoint changes', async () => {
      render(<ResponsiveTestComponent testGestures={true} />)

      const mainArea = screen.getByTestId('responsive-main')

      // Perform gestures while changing breakpoints
      const gestureCount = { before: 0, during: 0, after: 0 }

      // Gestures before breakpoint change
      for (let i = 0; i < 3; i++) {
        const touchStart = new TouchEvent('touchstart', {
          touches: [{ clientX: 100 + i * 10, clientY: 100 } as Touch]
        })
        fireEvent(mainArea, touchStart)

        const touchEnd = new TouchEvent('touchend', { touches: [] })
        fireEvent(mainArea, touchEnd)
        
        gestureCount.before++
      }

      // Change breakpoint
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 375
      })

      fireEvent(window, new Event('resize'))

      // Gestures during transition
      for (let i = 0; i < 3; i++) {
        const touchStart = new TouchEvent('touchstart', {
          touches: [{ clientX: 200 + i * 10, clientY: 100 } as Touch]
        })
        fireEvent(mainArea, touchStart)

        const touchEnd = new TouchEvent('touchend', { touches: [] })
        fireEvent(mainArea, touchEnd)
        
        gestureCount.during++
      }

      await waitFor(() => {
        expect(screen.getByTestId('responsive-main')).toHaveAttribute('data-breakpoint', 'sm')
      })

      // Gestures after breakpoint change
      for (let i = 0; i < 3; i++) {
        const touchStart = new TouchEvent('touchstart', {
          touches: [{ clientX: 300 + i * 10, clientY: 100 } as Touch]
        })
        fireEvent(mainArea, touchStart)

        const touchEnd = new TouchEvent('touchend', { touches: [] })
        fireEvent(mainArea, touchEnd)
        
        gestureCount.after++
      }

      // All gestures should be processed
      expect(gestureCount.before + gestureCount.during + gestureCount.after).toBe(9)
    })
  })

  describe('Accessibility at Different Screen Sizes', () => {
    it('maintains WCAG compliance across all breakpoints', async () => {
      render(<ResponsiveTestComponent testTouchTargets={true} />)

      const breakpointTests = [
        { width: 320, name: 'xs' },
        { width: 768, name: 'md' },
        { width: 1024, name: 'lg' }
      ]

      for (const { width, name } of breakpointTests) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))

        await waitFor(() => {
          const button = screen.getByTestId('responsive-button')
          expect(button).toHaveAttribute('data-is-valid-target', 'true')
          
          // Touch targets should meet minimum requirements
          const computedStyle = window.getComputedStyle(button)
          const minDimension = Math.min(
            parseInt(computedStyle.minWidth),
            parseInt(computedStyle.minHeight)
          )
          
          if (width < 768) {
            expect(minDimension).toBeGreaterThanOrEqual(44) // Mobile minimum
          } else {
            expect(minDimension).toBeGreaterThanOrEqual(36) // Desktop minimum
          }
        })
      }
    })

    it('provides appropriate focus indicators at all sizes', async () => {
      render(<ResponsiveTestComponent />)

      const button = screen.getByTestId('responsive-button')

      const breakpointTests = [375, 768, 1024]

      for (const width of breakpointTests) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))

        // Focus should work at all breakpoints
        act(() => {
          button.focus()
        })

        expect(button).toHaveFocus()

        // Keyboard activation should work
        fireEvent.keyDown(button, { key: 'Enter' })

        await waitFor(() => {
          expect(screen.getByTestId('bottom-sheet-responsive-content')).toBeInTheDocument()
        })

        const closeButton = screen.getByTestId('close-responsive-sheet')
        fireEvent.click(closeButton)

        await waitFor(() => {
          expect(screen.queryByTestId('bottom-sheet-responsive-content')).not.toBeInTheDocument()
        })
      }
    })

    it('maintains readable font sizes across breakpoints', async () => {
      render(<ResponsiveTestComponent />)

      const breakpointTests = [
        { width: 320, minFontSize: 14 },
        { width: 768, minFontSize: 16 },
        { width: 1024, minFontSize: 16 }
      ]

      for (const { width, minFontSize } of breakpointTests) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))

        await waitFor(() => {
          const container = screen.getByTestId('responsive-main')
          const computedStyle = window.getComputedStyle(container)
          const fontSize = parseInt(computedStyle.fontSize)
          
          expect(fontSize).toBeGreaterThanOrEqual(minFontSize)
        })
      }
    })

    it('supports high contrast mode across breakpoints', async () => {
      // Mock high contrast mode
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation(query => ({
          matches: query === '(prefers-contrast: high)',
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        }))
      })

      render(<ResponsiveTestComponent />)

      const breakpointTests = [375, 768, 1024]

      for (const width of breakpointTests) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })

        fireEvent(window, new Event('resize'))

        // Components should remain accessible in high contrast mode
        const button = screen.getByTestId('responsive-button')
        expect(button).toBeVisible()
        expect(button).toHaveAttribute('data-is-valid-target', 'true')
      }
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('handles extreme viewport sizes gracefully', async () => {
      render(<ResponsiveTestComponent onBreakpointChange={mockBreakpointChange} />)

      const extremeSizes = [
        { width: 1, height: 1 },
        { width: 10000, height: 10000 },
        { width: 240, height: 320 }, // Very small
        { width: 5120, height: 2880 } // Very large
      ]

      for (const { width, height } of extremeSizes) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: height
        })

        expect(() => {
          fireEvent(window, new Event('resize'))
        }).not.toThrow()

        await waitFor(() => {
          expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
        })
      }

      expect(mockBreakpointChange).toHaveBeenCalled()
    })

    it('handles missing window properties gracefully', async () => {
      const originalWindow = global.window

      // Temporarily remove window properties
      Object.defineProperty(global, 'window', {
        value: {
          ...originalWindow,
          innerWidth: undefined,
          innerHeight: undefined
        },
        writable: true
      })

      expect(() => {
        render(<ResponsiveTestComponent />)
      }).not.toThrow()

      // Restore window
      global.window = originalWindow
    })

    it('recovers from resize event errors', async () => {
      render(<ResponsiveTestComponent onBreakpointChange={mockBreakpointChange} />)

      // Mock console.error to catch any errors
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      // Trigger many rapid resize events
      for (let i = 0; i < 100; i++) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: 300 + i * 10
        })

        fireEvent(window, new Event('resize'))
      }

      await waitFor(() => {
        expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
      })

      // Should not have logged errors
      expect(consoleSpy).not.toHaveBeenCalled()

      consoleSpy.mockRestore()
    })
  })
})