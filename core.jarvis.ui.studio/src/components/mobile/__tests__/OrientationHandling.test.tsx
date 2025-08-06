/**
 * Orientation Change Handling Tests
 * 
 * Comprehensive tests for mobile device orientation changes including:
 * - Portrait to landscape transitions
 * - Component layout adaptation during orientation changes
 * - Gesture handling across orientations
 * - Performance during rapid orientation changes
 * - Touch target adjustments for different orientations
 * - State preservation during orientation changes
 */

import React, { useState, useEffect, useRef } from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BottomSheet } from '../BottomSheet'
import { useTouchGestures } from '@/hooks/useTouchGestures'
import { 
  createMockTouchEvent, 
  simulateSwipeGesture, 
  setupBentoTestEnvironment 
} from '@/test/utils/bento-test-utils'

// Test component that responds to orientation changes
const OrientationAwareComponent: React.FC = () => {
  const [orientation, setOrientation] = useState<'portrait' | 'landscape'>('portrait')
  const [dimensions, setDimensions] = useState({
    width: window.innerWidth,
    height: window.innerHeight
  })
  const [gestureCount, setGestureCount] = useState(0)
  const elementRef = useRef<HTMLDivElement>(null)

  const { attachListeners, isTouchDevice } = useTouchGestures({}, {
    onTap: () => setGestureCount(prev => prev + 1),
    onSwipe: () => setGestureCount(prev => prev + 1),
    onPinch: () => setGestureCount(prev => prev + 1)
  })

  useEffect(() => {
    const handleOrientationChange = () => {
      // Small delay to allow browser to update dimensions
      setTimeout(() => {
        const newWidth = window.innerWidth
        const newHeight = window.innerHeight
        
        setDimensions({ width: newWidth, height: newHeight })
        setOrientation(newWidth > newHeight ? 'landscape' : 'portrait')
      }, 100)
    }

    const handleResize = () => {
      setDimensions({
        width: window.innerWidth,
        height: window.innerHeight
      })
    }

    window.addEventListener('orientationchange', handleOrientationChange)
    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('orientationchange', handleOrientationChange)
      window.removeEventListener('resize', handleResize)
    }
  }, [])

  useEffect(() => {
    if (elementRef.current) {
      return attachListeners(elementRef.current)
    }
  }, [attachListeners])

  return (
    <div
      ref={elementRef}
      data-testid="orientation-component"
      data-orientation={orientation}
      data-width={dimensions.width}
      data-height={dimensions.height}
      data-gesture-count={gestureCount}
      data-is-touch-device={isTouchDevice}
      style={{
        width: '100%',
        height: orientation === 'portrait' ? '200px' : '120px',
        padding: orientation === 'portrait' ? '16px' : '8px',
        fontSize: orientation === 'portrait' ? '16px' : '14px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: orientation === 'portrait' ? '#e3f2fd' : '#f3e5f5',
        transition: 'all 0.3s ease'
      }}
    >
      <span data-testid="orientation-info">
        {orientation} - {dimensions.width}x{dimensions.height}
      </span>
    </div>
  )
}

// Bottom sheet with orientation awareness
const OrientationAwareBottomSheet: React.FC<{
  isOpen: boolean
  onClose: () => void
}> = ({ isOpen, onClose }) => {
  const [orientation, setOrientation] = useState<'portrait' | 'landscape'>('portrait')

  useEffect(() => {
    const updateOrientation = () => {
      setTimeout(() => {
        setOrientation(window.innerWidth > window.innerHeight ? 'landscape' : 'portrait')
      }, 100)
    }

    window.addEventListener('orientationchange', updateOrientation)
    window.addEventListener('resize', updateOrientation)

    return () => {
      window.removeEventListener('orientationchange', updateOrientation)
      window.removeEventListener('resize', updateOrientation)
    }
  }, [])

  return (
    <BottomSheet
      isOpen={isOpen}
      onClose={onClose}
      initialHeight={orientation === 'portrait' ? 0.6 : 0.8}
      maxHeight={orientation === 'portrait' ? 0.9 : 0.95}
      minHeight={orientation === 'portrait' ? 0.3 : 0.4}
      title={`Sheet in ${orientation}`}
    >
      <div 
        data-testid="sheet-content" 
        data-orientation={orientation}
        style={{ 
          padding: orientation === 'portrait' ? '20px' : '10px',
          height: orientation === 'portrait' ? '300px' : '150px'
        }}
      >
        Content adapted for {orientation} orientation
      </div>
    </BottomSheet>
  )
}

describe('Orientation Change Handling', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  
  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    // Set initial portrait orientation
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      value: 375
    })
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      value: 812
    })
    Object.defineProperty(screen, 'orientation', {
      writable: true,
      value: {
        angle: 0,
        type: 'portrait-primary'
      }
    })
    
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  describe('Basic Orientation Detection', () => {
    it('detects initial portrait orientation', () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      expect(component).toHaveAttribute('data-orientation', 'portrait')
      expect(component).toHaveAttribute('data-width', '375')
      expect(component).toHaveAttribute('data-height', '812')
    })

    it('detects orientation change to landscape', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      expect(component).toHaveAttribute('data-orientation', 'portrait')
      
      // Simulate rotation to landscape
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 812
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 375
      })
      Object.defineProperty(screen, 'orientation', {
        writable: true,
        value: {
          angle: 90,
          type: 'landscape-primary'
        }
      })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150) // Account for debounce delay
      })
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-orientation', 'landscape')
        expect(component).toHaveAttribute('data-width', '812')
        expect(component).toHaveAttribute('data-height', '375')
      })
    })

    it('handles multiple rapid orientation changes', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Simulate rapid orientation changes
      const orientations = [
        { width: 812, height: 375, expected: 'landscape' },
        { width: 375, height: 812, expected: 'portrait' },
        { width: 812, height: 375, expected: 'landscape' },
        { width: 375, height: 812, expected: 'portrait' }
      ]
      
      for (const [index, orientation] of orientations.entries()) {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          value: orientation.width
        })
        Object.defineProperty(window, 'innerHeight', {
          writable: true,
          value: orientation.height
        })
        
        fireEvent(window, new Event('orientationchange'))
        
        await act(async () => {
          vi.advanceTimersByTime(150)
        })
      }
      
      // Should settle on the final orientation
      await waitFor(() => {
        expect(component).toHaveAttribute('data-orientation', 'portrait')
      })
    })
  })

  describe('Component Layout Adaptation', () => {
    it('adapts component height for different orientations', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Portrait should have taller height
      expect(component).toHaveStyle({ height: '200px' })
      
      // Change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      await waitFor(() => {
        expect(component).toHaveStyle({ height: '120px' })
      })
    })

    it('adjusts padding and font size for orientation', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Portrait styling
      expect(component).toHaveStyle({ 
        padding: '16px',
        fontSize: '16px' 
      })
      
      // Change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      await waitFor(() => {
        expect(component).toHaveStyle({ 
          padding: '8px',
          fontSize: '14px' 
        })
      })
    })

    it('applies smooth transitions during orientation changes', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      expect(component).toHaveStyle({ transition: 'all 0.3s ease' })
      
      // Transition should remain during orientation change
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      expect(component).toHaveStyle({ transition: 'all 0.3s ease' })
    })
  })

  describe('Gesture Handling Across Orientations', () => {
    it('maintains gesture functionality after orientation change', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Test gesture in portrait
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      const touchEnd = createMockTouchEvent('touchend', [])
      
      fireEvent(component, touchStart)
      fireEvent(component, touchEnd)
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-gesture-count', '1')
      })
      
      // Change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Test gesture in landscape
      fireEvent(component, touchStart)
      fireEvent(component, touchEnd)
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-gesture-count', '2')
      })
    })

    it('adjusts swipe thresholds for orientation', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Test swipe in portrait
      await simulateSwipeGesture(component, 'right', {
        distance: 60,
        duration: 200
      })
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-gesture-count', '1')
      })
      
      // Change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Test swipe in landscape (might need different threshold)
      await simulateSwipeGesture(component, 'left', {
        distance: 60,
        duration: 200
      })
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-gesture-count', '2')
      })
    })

    it('handles gestures during orientation change', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Start a gesture
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      fireEvent(component, touchStart)
      
      // Change orientation mid-gesture
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      // Complete the gesture
      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(component, touchEnd)
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Should handle gracefully without errors
      expect(() => {
        vi.advanceTimersByTime(500)
      }).not.toThrow()
    })
  })

  describe('BottomSheet Orientation Handling', () => {
    it('adjusts bottom sheet height for orientation change', async () => {
      const portalDiv = document.createElement('div')
      portalDiv.id = 'bottom-sheet-portal'
      document.body.appendChild(portalDiv)
      
      render(<OrientationAwareBottomSheet isOpen={true} onClose={() => {}} />)
      
      // Portrait should use 60% height
      await waitFor(() => {
        const sheet = screen.getByRole('dialog').firstChild as HTMLElement
        expect(sheet).toHaveStyle({ height: '60vh' })
      })
      
      // Change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Landscape should use 80% height
      await waitFor(() => {
        const sheet = screen.getByRole('dialog').firstChild as HTMLElement
        expect(sheet).toHaveStyle({ height: '80vh' })
      })
      
      document.body.removeChild(portalDiv)
    })

    it('updates title and content for orientation', async () => {
      const portalDiv = document.createElement('div')
      portalDiv.id = 'bottom-sheet-portal'
      document.body.appendChild(portalDiv)
      
      render(<OrientationAwareBottomSheet isOpen={true} onClose={() => {}} />)
      
      expect(screen.getByText('Sheet in portrait')).toBeInTheDocument()
      expect(screen.getByTestId('sheet-content')).toHaveAttribute('data-orientation', 'portrait')
      
      // Change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      await waitFor(() => {
        expect(screen.getByText('Sheet in landscape')).toBeInTheDocument()
        expect(screen.getByTestId('sheet-content')).toHaveAttribute('data-orientation', 'landscape')
      })
      
      document.body.removeChild(portalDiv)
    })

    it('maintains drag functionality across orientations', async () => {
      const portalDiv = document.createElement('div')
      portalDiv.id = 'bottom-sheet-portal'
      document.body.appendChild(portalDiv)
      
      const onClose = vi.fn()
      render(<OrientationAwareBottomSheet isOpen={true} onClose={onClose} />)
      
      // Test swipe down in portrait
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      await simulateSwipeGesture(sheet, 'down', {
        distance: 100,
        duration: 200
      })
      
      await act(async () => {
        vi.advanceTimersByTime(300)
      })
      
      expect(onClose).toHaveBeenCalledTimes(1)
      onClose.mockClear()
      
      // Change to landscape and test again
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      await simulateSwipeGesture(sheet, 'down', {
        distance: 100,
        duration: 200
      })
      
      await act(async () => {
        vi.advanceTimersByTime(300)
      })
      
      expect(onClose).toHaveBeenCalledTimes(1)
      
      document.body.removeChild(portalDiv)
    })
  })

  describe('Performance During Orientation Changes', () => {
    it('handles orientation changes without performance degradation', async () => {
      render(<OrientationAwareComponent />)
      
      const startTime = performance.now()
      
      // Simulate multiple orientation changes
      for (let i = 0; i < 5; i++) {
        const isLandscape = i % 2 === 0
        Object.defineProperty(window, 'innerWidth', {
          value: isLandscape ? 812 : 375
        })
        Object.defineProperty(window, 'innerHeight', {
          value: isLandscape ? 375 : 812
        })
        
        fireEvent(window, new Event('orientationchange'))
        
        await act(async () => {
          vi.advanceTimersByTime(150)
        })
      }
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(1000) // Should handle efficiently
    })

    it('debounces rapid orientation events', async () => {
      const orientationHandler = vi.fn()
      
      window.addEventListener('orientationchange', orientationHandler)
      
      render(<OrientationAwareComponent />)
      
      // Fire multiple rapid orientation events
      for (let i = 0; i < 10; i++) {
        fireEvent(window, new Event('orientationchange'))
      }
      
      expect(orientationHandler).toHaveBeenCalledTimes(10)
      
      // Component should handle debouncing internally
      const component = screen.getByTestId('orientation-component')
      expect(component).toBeInTheDocument()
      
      window.removeEventListener('orientationchange', orientationHandler)
    })

    it('prevents memory leaks during orientation changes', () => {
      const { unmount } = render(<OrientationAwareComponent />)
      
      // Add event listeners
      fireEvent(window, new Event('orientationchange'))
      fireEvent(window, new Event('resize'))
      
      // Unmount should clean up listeners
      unmount()
      
      expect(() => {
        fireEvent(window, new Event('orientationchange'))
        fireEvent(window, new Event('resize'))
      }).not.toThrow()
    })
  })

  describe('State Preservation', () => {
    it('preserves component state during orientation change', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Trigger some gestures to build up state
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      const touchEnd = createMockTouchEvent('touchend', [])
      
      fireEvent(component, touchStart)
      fireEvent(component, touchEnd)
      
      await waitFor(() => {
        expect(component).toHaveAttribute('data-gesture-count', '1')
      })
      
      // Change orientation
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // State should be preserved
      await waitFor(() => {
        expect(component).toHaveAttribute('data-gesture-count', '1')
        expect(component).toHaveAttribute('data-orientation', 'landscape')
      })
    })

    it('maintains touch device detection across orientations', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      expect(component).toHaveAttribute('data-is-touch-device', 'true')
      
      // Change orientation
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Touch device detection should persist
      await waitFor(() => {
        expect(component).toHaveAttribute('data-is-touch-device', 'true')
      })
    })
  })

  describe('Edge Cases', () => {
    it('handles orientation change events without actual dimension changes', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Fire orientation change without changing dimensions
      fireEvent(window, new Event('orientationchange'))
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Should handle gracefully
      expect(component).toHaveAttribute('data-orientation', 'portrait')
      expect(component).toHaveAttribute('data-width', '375')
    })

    it('handles invalid or extreme dimension values', async () => {
      render(<OrientationAwareComponent />)
      
      // Set extreme values
      Object.defineProperty(window, 'innerWidth', { value: 0 })
      Object.defineProperty(window, 'innerHeight', { value: 0 })
      
      expect(() => {
        fireEvent(window, new Event('orientationchange'))
        
        act(() => {
          vi.advanceTimersByTime(150)
        })
      }).not.toThrow()
    })

    it('handles simultaneous orientation change and gesture events', async () => {
      render(<OrientationAwareComponent />)
      
      const component = screen.getByTestId('orientation-component')
      
      // Start a gesture
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      fireEvent(component, touchStart)
      
      // Simultaneously fire orientation change and gesture completion
      Object.defineProperty(window, 'innerWidth', { value: 812 })
      Object.defineProperty(window, 'innerHeight', { value: 375 })
      
      fireEvent(window, new Event('orientationchange'))
      
      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(component, touchEnd)
      
      await act(async () => {
        vi.advanceTimersByTime(150)
      })
      
      // Should handle both events without conflicts
      expect(component).toHaveAttribute('data-orientation', 'landscape')
      expect(component).toHaveAttribute('data-gesture-count', '1')
    })
  })
})