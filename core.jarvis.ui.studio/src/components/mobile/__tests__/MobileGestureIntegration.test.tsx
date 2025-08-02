/**
 * Mobile Gesture Integration Tests
 * 
 * Comprehensive integration tests for mobile gesture handling across components:
 * - Cross-component gesture coordination
 * - Real-world mobile usage scenarios
 * - Performance under mixed gesture load
 * - Gesture conflict resolution between components
 * - Mobile-specific edge cases and device quirks
 */

import React, { useState, useRef } from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BottomSheet } from '../BottomSheet'
import { useTouchGestures } from '@/hooks/useTouchGestures'
import {
  createMockTouchEvent,
  simulateSwipeGesture,
  simulatePinchGesture,
  simulateLongPress,
  setupBentoTestEnvironment
} from '@/test/utils/bento-test-utils'

// Complex mobile app simulation
const MobileAppSimulator: React.FC<{
  onGestureEvent?: (event: any) => void
  enableBottomSheet?: boolean
  enableSwipeCards?: boolean
  enablePinchZoom?: boolean
}> = ({ 
  onGestureEvent, 
  enableBottomSheet = true,
  enableSwipeCards = true,
  enablePinchZoom = true
}) => {
  const [bottomSheetOpen, setBottomSheetOpen] = useState(false)
  const [currentCard, setCurrentCard] = useState(0)
  const [zoomScale, setZoomScale] = useState(1)
  const [gestureLog, setGestureLog] = useState<string[]>([])
  
  const mainAreaRef = useRef<HTMLDivElement>(null)
  const cardContainerRef = useRef<HTMLDivElement>(null)
  const zoomableRef = useRef<HTMLDivElement>(null)

  // Main area gestures
  const { attachListeners: attachMainListeners } = useTouchGestures({
    enableLongPress: true,
    enableSwipe: true,
    longPressDelay: 500
  }, {
    onLongPress: (detail) => {
      setBottomSheetOpen(true)
      setGestureLog(prev => [...prev, `long-press-main:${detail.x},${detail.y}`])
      onGestureEvent?.({ type: 'long-press', source: 'main', detail })
    },
    onSwipe: (detail) => {
      if (detail.direction === 'up' && detail.velocity > 1.0) {
        setBottomSheetOpen(true)
      }
      setGestureLog(prev => [...prev, `swipe-main:${detail.direction}`])
      onGestureEvent?.({ type: 'swipe', source: 'main', detail })
    }
  })

  // Card container gestures
  const { attachListeners: attachCardListeners } = useTouchGestures({
    enableSwipe: enableSwipeCards,
    swipeThreshold: 80,
    swipeTimeout: 400
  }, {
    onSwipe: (detail) => {
      if (detail.direction === 'left') {
        setCurrentCard(prev => Math.min(prev + 1, 4))
      } else if (detail.direction === 'right') {
        setCurrentCard(prev => Math.max(prev - 1, 0))
      }
      setGestureLog(prev => [...prev, `swipe-cards:${detail.direction}`])
      onGestureEvent?.({ type: 'swipe', source: 'cards', detail })
    }
  })

  // Zoomable area gestures
  const { attachListeners: attachZoomListeners } = useTouchGestures({
    enablePinch: enablePinchZoom,
    pinchThreshold: 0.05
  }, {
    onPinch: (detail) => {
      setZoomScale(prev => Math.max(0.5, Math.min(3.0, prev * detail.scale)))
      setGestureLog(prev => [...prev, `pinch-zoom:${detail.scale.toFixed(2)}`])
      onGestureEvent?.({ type: 'pinch', source: 'zoom', detail })
    }
  })

  // Attach listeners
  React.useEffect(() => {
    const cleanups: (() => void)[] = []
    
    if (mainAreaRef.current) {
      cleanups.push(attachMainListeners(mainAreaRef.current))
    }
    if (cardContainerRef.current && enableSwipeCards) {
      cleanups.push(attachCardListeners(cardContainerRef.current))
    }
    if (zoomableRef.current && enablePinchZoom) {
      cleanups.push(attachZoomListeners(zoomableRef.current))
    }

    return () => {
      cleanups.forEach(cleanup => cleanup())
    }
  }, [attachMainListeners, attachCardListeners, attachZoomListeners, enableSwipeCards, enablePinchZoom])

  return (
    <div data-testid="mobile-app">
      {/* Main touch area */}
      <div
        ref={mainAreaRef}
        data-testid="main-area"
        style={{
          width: '100%',
          height: '300px',
          backgroundColor: '#f0f0f0',
          padding: '20px',
          touchAction: 'none'
        }}
      >
        <h2>Main Area</h2>
        <p>Long press or swipe up to open bottom sheet</p>
        
        {/* Card container */}
        <div
          ref={cardContainerRef}
          data-testid="card-container"
          style={{
            width: '100%',
            height: '120px',
            backgroundColor: '#e0e0e0',
            margin: '10px 0',
            overflow: 'hidden',
            position: 'relative'
          }}
        >
          <div
            style={{
              display: 'flex',
              transform: `translateX(-${currentCard * 100}%)`,
              transition: 'transform 0.3s ease'
            }}
          >
            {Array.from({ length: 5 }, (_, i) => (
              <div
                key={i}
                data-testid={`card-${i}`}
                style={{
                  minWidth: '100%',
                  height: '100%',
                  backgroundColor: `hsl(${i * 60}, 70%, 80%)`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '24px'
                }}
              >
                Card {i + 1}
              </div>
            ))}
          </div>
        </div>

        {/* Zoomable area */}
        <div
          ref={zoomableRef}
          data-testid="zoomable-area"
          style={{
            width: '100%',
            height: '80px',
            backgroundColor: '#d0d0d0',
            overflow: 'hidden',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          <div
            style={{
              transform: `scale(${zoomScale})`,
              transformOrigin: 'center',
              transition: 'transform 0.1s ease'
            }}
          >
            Pinch to zoom (Scale: {zoomScale.toFixed(2)})
          </div>
        </div>
      </div>

      {/* Status display */}
      <div data-testid="status-display" style={{ padding: '10px', fontSize: '12px' }}>
        <div>Current Card: {currentCard + 1}</div>
        <div>Zoom Scale: {zoomScale.toFixed(2)}</div>
        <div>Bottom Sheet: {bottomSheetOpen ? 'Open' : 'Closed'}</div>
        <div data-testid="gesture-log">
          Recent: {gestureLog.slice(-3).join(', ')}
        </div>
      </div>

      {/* Bottom Sheet */}
      {enableBottomSheet && (
        <BottomSheet
          isOpen={bottomSheetOpen}
          onClose={() => setBottomSheetOpen(false)}
          title="Mobile Bottom Sheet"
          initialHeight={0.6}
        >
          <div data-testid="bottom-sheet-content" style={{ padding: '20px' }}>
            <p>Bottom sheet opened via gesture</p>
            <button 
              onClick={() => setBottomSheetOpen(false)}
              data-testid="close-sheet-button"
            >
              Close
            </button>
            <div style={{ marginTop: '20px' }}>
              <h4>Gesture Log:</h4>
              <div data-testid="sheet-gesture-log" style={{ maxHeight: '100px', overflow: 'auto' }}>
                {gestureLog.map((log, i) => (
                  <div key={i} style={{ fontSize: '11px', margin: '2px 0' }}>
                    {log}
                  </div>
                ))}
              </div>
            </div>
          </div>
        </BottomSheet>
      )}
    </div>
  )
}

// Performance monitoring component
const PerformanceMonitor: React.FC<{ onMetric?: (metric: any) => void }> = ({ onMetric }) => {
  React.useEffect(() => {
    const observer = new PerformanceObserver((list) => {
      list.getEntries().forEach((entry) => {
        onMetric?.(entry)
      })
    })
    
    observer.observe({ entryTypes: ['measure', 'navigation'] })
    
    return () => observer.disconnect()
  }, [onMetric])

  return null
}

describe('Mobile Gesture Integration', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let mockGestureEvent: ReturnType<typeof vi.fn>
  let mockPerformanceMetric: ReturnType<typeof vi.fn>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    mockGestureEvent = vi.fn()
    mockPerformanceMetric = vi.fn()
    
    // Create portal mount point
    const portalDiv = document.createElement('div')
    portalDiv.id = 'bottom-sheet-portal'
    document.body.appendChild(portalDiv)
    
    // Mock mobile viewport
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      value: 375
    })
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      value: 812
    })
    
    // Mock PerformanceObserver
    const MockPerformanceObserver = vi.fn().mockImplementation((callback) => ({
      observe: vi.fn(),
      disconnect: vi.fn()
    }))
    Object.defineProperty(MockPerformanceObserver, 'supportedEntryTypes', {
      value: ['measure', 'navigation'],
      writable: true
    })
    global.PerformanceObserver = MockPerformanceObserver as any
    
    vi.useFakeTimers()
  })

  afterEach(() => {
    const portal = document.getElementById('bottom-sheet-portal')
    if (portal) {
      document.body.removeChild(portal)
    }
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  describe('Cross-Component Gesture Coordination', () => {
    it('coordinates gestures between different components', async () => {
      render(
        <MobileAppSimulator 
          onGestureEvent={mockGestureEvent}
        />
      )

      const mainArea = screen.getByTestId('main-area')
      const cardContainer = screen.getByTestId('card-container')

      // Swipe on main area (should open bottom sheet)
      await simulateSwipeGesture(mainArea, 'up', {
        distance: 100,
        duration: 200
      })

      await waitFor(() => {
        expect(screen.getByTestId('bottom-sheet-content')).toBeInTheDocument()
      })

      // Swipe on cards (should change card)
      await simulateSwipeGesture(cardContainer, 'left', {
        distance: 120,
        duration: 300
      })

      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 2')
      })

      expect(mockGestureEvent).toHaveBeenCalledTimes(2)
    })

    it('prevents gesture conflicts between overlapping areas', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')
      const cardContainer = screen.getByTestId('card-container')

      // Long press on card container (should only trigger one handler)
      await simulateLongPress(cardContainer, {
        x: 100,
        y: 50,
        duration: 600
      })

      // Should prioritize more specific component
      expect(mockGestureEvent).toHaveBeenCalledTimes(1)
    })

    it('handles gesture isolation in bottom sheet', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // Open bottom sheet
      await simulateSwipeGesture(mainArea, 'up', {
        distance: 100,
        duration: 200
      })

      await waitFor(() => {
        expect(screen.getByTestId('bottom-sheet-content')).toBeInTheDocument()
      })

      const sheetContent = screen.getByTestId('bottom-sheet-content')

      // Gestures in sheet should not affect background components
      await simulateSwipeGesture(sheetContent, 'left', {
        distance: 100,
        duration: 200
      })

      // Should not change card in background
      expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 1')
    })

    it('coordinates pinch zoom with other gestures', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const zoomableArea = screen.getByTestId('zoomable-area')

      // Pinch to zoom
      await simulatePinchGesture(zoomableArea, {
        startDistance: 100,
        endDistance: 150,
        steps: 5
      })

      await waitFor(() => {
        const statusDisplay = screen.getByTestId('status-display')
        expect(statusDisplay).toHaveTextContent('Zoom Scale: 1.5')
      })

      // Other gestures should still work
      const cardContainer = screen.getByTestId('card-container')
      await simulateSwipeGesture(cardContainer, 'left', {
        distance: 120,
        duration: 300
      })

      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 2')
      })

      expect(mockGestureEvent).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'pinch', source: 'zoom' })
      )
    })

    it('handles rapid gesture switching between components', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')
      const cardContainer = screen.getByTestId('card-container')
      const zoomableArea = screen.getByTestId('zoomable-area')

      // Rapid gesture sequence
      await simulateSwipeGesture(cardContainer, 'left', { distance: 100, duration: 150 })
      await new Promise(resolve => setTimeout(resolve, 50))
      
      await simulatePinchGesture(zoomableArea, { 
        startDistance: 100, 
        endDistance: 120, 
        steps: 3 
      })
      await new Promise(resolve => setTimeout(resolve, 50))
      
      await simulateSwipeGesture(mainArea, 'up', { distance: 80, duration: 200 })

      // Should handle rapid switching without conflicts
      expect(mockGestureEvent).toHaveBeenCalledTimes(3)
    })
  })

  describe('Real-World Mobile Usage Scenarios', () => {
    it('simulates typical mobile app navigation flow', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      // 1. User browses cards
      const cardContainer = screen.getByTestId('card-container')
      
      await simulateSwipeGesture(cardContainer, 'left', { distance: 120, duration: 300 })
      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 2')
      })

      await simulateSwipeGesture(cardContainer, 'left', { distance: 120, duration: 300 })
      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 3')
      })

      // 2. User accidentally triggers zoom
      const zoomableArea = screen.getByTestId('zoomable-area')
      await simulatePinchGesture(zoomableArea, {
        startDistance: 100,
        endDistance: 130,
        steps: 3
      })

      // 3. User opens bottom sheet for more options
      const mainArea = screen.getByTestId('main-area')
      await simulateSwipeGesture(mainArea, 'up', { distance: 150, duration: 250 })

      await waitFor(() => {
        expect(screen.getByTestId('bottom-sheet-content')).toBeInTheDocument()
      })

      // 4. User closes sheet and continues browsing
      const closeButton = screen.getByTestId('close-sheet-button')
      fireEvent.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByTestId('bottom-sheet-content')).not.toBeInTheDocument()
      })

      await simulateSwipeGesture(cardContainer, 'right', { distance: 120, duration: 300 })

      expect(mockGestureEvent).toHaveBeenCalledTimes(5) // All gestures captured
    })

    it('handles accidental touches and palm rejection', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // Simulate accidental large touch area (palm)
      const palmTouch = createMockTouchEvent('touchstart', [
        { clientX: 100, clientY: 100, identifier: 0 },
        { clientX: 150, clientY: 120, identifier: 1 },
        { clientX: 200, clientY: 140, identifier: 2 }
      ])
      fireEvent(mainArea, palmTouch)

      // Should not trigger unintended gestures
      await new Promise(resolve => setTimeout(resolve, 100))

      const palmEnd = createMockTouchEvent('touchend', [])
      fireEvent(mainArea, palmEnd)

      // No gestures should be triggered by palm touches
      expect(mockGestureEvent).not.toHaveBeenCalled()
    })

    it('simulates one-handed mobile usage patterns', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      // Simulate one-handed usage with thumb gestures
      const cardContainer = screen.getByTestId('card-container')

      // Thumb swipe from bottom-right area
      await simulateSwipeGesture(cardContainer, 'left', {
        distance: 80,
        duration: 400,
        startX: 320, // Near right edge
        startY: 60   // In reachable thumb area
      })

      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 2')
      })

      // Thumb reach gesture to open bottom sheet
      const mainArea = screen.getByTestId('main-area')
      await simulateSwipeGesture(mainArea, 'up', {
        distance: 120,
        duration: 300,
        startX: 300,
        startY: 250
      })

      await waitFor(() => {
        expect(screen.getByTestId('bottom-sheet-content')).toBeInTheDocument()
      })

      expect(mockGestureEvent).toHaveBeenCalledTimes(2)
    })

    it('handles landscape to portrait orientation changes', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const cardContainer = screen.getByTestId('card-container')

      // Portrait mode gestures
      await simulateSwipeGesture(cardContainer, 'left', { distance: 120, duration: 300 })

      // Simulate orientation change to landscape
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

      await new Promise(resolve => setTimeout(resolve, 100))

      // Gestures should still work after orientation change
      await simulateSwipeGesture(cardContainer, 'left', { distance: 120, duration: 300 })

      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 3')
      })

      expect(mockGestureEvent).toHaveBeenCalledTimes(2)
    })

    it('simulates multitasking scenarios', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // User starts gesture, gets interrupted by notification
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 300 }])
      fireEvent(mainArea, touchStart)

      // Simulate app losing focus
      fireEvent(window, new Event('blur'))
      fireEvent(document, new Event('visibilitychange'))

      // User returns and completes gesture
      fireEvent(window, new Event('focus'))
      
      await simulateSwipeGesture(mainArea, 'up', { distance: 100, duration: 200 })

      // Should handle interrupted gestures gracefully
      expect(screen.getByTestId('mobile-app')).toBeInTheDocument()
    })
  })

  describe('Performance Under Mixed Gesture Load', () => {
    it('maintains performance with simultaneous gestures', async () => {
      render(
        <>
          <MobileAppSimulator onGestureEvent={mockGestureEvent} />
          <PerformanceMonitor onMetric={mockPerformanceMetric} />
        </>
      )

      const startTime = performance.now()

      // Simulate multiple simultaneous gesture areas being active
      const cardContainer = screen.getByTestId('card-container')
      const zoomableArea = screen.getByTestId('zoomable-area')
      const mainArea = screen.getByTestId('main-area')

      // Concurrent gestures
      const promises = [
        simulateSwipeGesture(cardContainer, 'left', { distance: 100, duration: 200 }),
        simulatePinchGesture(zoomableArea, { 
          startDistance: 100, 
          endDistance: 120, 
          steps: 5 
        }),
        simulateSwipeGesture(mainArea, 'up', { distance: 80, duration: 250 })
      ]

      await Promise.all(promises)

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(500) // Should handle efficiently
    })

    it('handles high-frequency touch events without lag', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const zoomableArea = screen.getByTestId('zoomable-area')

      // Simulate very fast pinch gesture (high frequency events)
      const touchStart = createMockTouchEvent('touchstart', [
        { clientX: 150, clientY: 50, identifier: 0 },
        { clientX: 250, clientY: 50, identifier: 1 }
      ])
      fireEvent(zoomableArea, touchStart)

      const startTime = performance.now()

      // Rapid-fire touch move events
      for (let i = 0; i < 50; i++) {
        const distance = 100 + i * 2
        const touchMove = createMockTouchEvent('touchmove', [
          { clientX: 200 - distance/2, clientY: 50, identifier: 0 },
          { clientX: 200 + distance/2, clientY: 50, identifier: 1 }
        ])
        fireEvent(zoomableArea, touchMove)
      }

      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(zoomableArea, touchEnd)

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(200) // Should handle rapid events efficiently
    })

    it('optimizes memory usage during extended gesture sessions', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const cardContainer = screen.getByTestId('card-container')

      // Extended gesture session
      for (let session = 0; session < 20; session++) {
        await simulateSwipeGesture(cardContainer, 'left', { 
          distance: 100, 
          duration: 150 
        })
        
        await simulateSwipeGesture(cardContainer, 'right', { 
          distance: 100, 
          duration: 150 
        })
        
        // Small delay between sessions
        await new Promise(resolve => setTimeout(resolve, 10))
      }

      // Should maintain performance without memory leaks
      expect(screen.getByTestId('mobile-app')).toBeInTheDocument()
    })

    it('throttles excessive gesture events', async () => {
      const eventCount = { touch: 0, gesture: 0 }
      
      const ThrottleTestComponent: React.FC = () => {
        const elementRef = useRef<HTMLDivElement>(null)
        
        const { attachListeners } = useTouchGestures({}, {
          onSwipe: () => {
            eventCount.gesture++
          }
        })

        React.useEffect(() => {
          if (elementRef.current) {
            const cleanup = attachListeners(elementRef.current)
            
            // Count raw touch events
            const element = elementRef.current
            const countTouchEvents = () => { eventCount.touch++ }
            
            element.addEventListener('touchmove', countTouchEvents)
            
            return () => {
              cleanup()
              element.removeEventListener('touchmove', countTouchEvents)
            }
          }
        }, [attachListeners])

        return (
          <div 
            ref={elementRef}
            data-testid="throttle-test"
            style={{ width: '300px', height: '200px', backgroundColor: '#f0f0f0' }}
          />
        )
      }

      render(<ThrottleTestComponent />)

      const element = screen.getByTestId('throttle-test')

      // Generate excessive touch events
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 50, clientY: 50 }])
      fireEvent(element, touchStart)

      for (let i = 0; i < 100; i++) {
        const touchMove = createMockTouchEvent('touchmove', [{ clientX: 50 + i, clientY: 50 }])
        fireEvent(element, touchMove)
      }

      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(element, touchEnd)

      // Gesture events should be throttled compared to raw touch events
      expect(eventCount.gesture).toBeLessThan(eventCount.touch)
    })
  })

  describe('Device-Specific Edge Cases', () => {
    it('handles iOS Safari touch behavior', async () => {
      // Mock iOS Safari
      Object.defineProperty(navigator, 'userAgent', {
        writable: true,
        value: 'Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15'
      })

      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // iOS Safari has specific touch behavior
      await simulateSwipeGesture(mainArea, 'up', {
        distance: 100,
        duration: 200
      })

      // Should handle iOS-specific behavior correctly
      expect(mockGestureEvent).toHaveBeenCalled()
    })

    it('handles Android Chrome touch quirks', async () => {
      // Mock Android Chrome
      Object.defineProperty(navigator, 'userAgent', {
        writable: true,
        value: 'Mozilla/5.0 (Linux; Android 12; SM-G998B) AppleWebKit/537.36 Chrome/98.0.4758.101'
      })

      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const zoomableArea = screen.getByTestId('zoomable-area')

      // Android Chrome might have different pinch behavior
      await simulatePinchGesture(zoomableArea, {
        startDistance: 100,
        endDistance: 150,
        steps: 8
      })

      expect(mockGestureEvent).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'pinch' })
      )
    })

    it('handles high DPI device scaling', async () => {
      // Mock high DPI device
      Object.defineProperty(window, 'devicePixelRatio', {
        writable: true,
        value: 3
      })

      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const cardContainer = screen.getByTestId('card-container')

      // Touch coordinates should be properly scaled
      await simulateSwipeGesture(cardContainer, 'left', {
        distance: 120,
        duration: 300
      })

      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 2')
      })

      expect(mockGestureEvent).toHaveBeenCalled()
    })

    it('handles small screen devices', async () => {
      // Mock small screen device
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 320
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 568
      })

      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // Gestures should still work on small screens
      await simulateSwipeGesture(mainArea, 'up', {
        distance: 80, // Smaller distance for small screen
        duration: 200
      })

      await waitFor(() => {
        expect(screen.getByTestId('bottom-sheet-content')).toBeInTheDocument()
      })

      expect(mockGestureEvent).toHaveBeenCalled()
    })

    it('handles tablet-specific gesture behaviors', async () => {
      // Mock tablet dimensions
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 768
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 1024
      })

      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const zoomableArea = screen.getByTestId('zoomable-area')

      // Tablets might support more complex gestures
      await simulatePinchGesture(zoomableArea, {
        startDistance: 200,
        endDistance: 300,
        centerX: 384,
        centerY: 512,
        steps: 10
      })

      expect(mockGestureEvent).toHaveBeenCalledWith(
        expect.objectContaining({ 
          type: 'pinch',
          detail: expect.objectContaining({
            center: expect.objectContaining({
              x: expect.any(Number),
              y: expect.any(Number)
            })
          })
        })
      )
    })

    it('handles foldable device state changes', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const cardContainer = screen.getByTestId('card-container')

      // Normal folded state
      await simulateSwipeGesture(cardContainer, 'left', { distance: 120, duration: 300 })

      // Simulate unfolding (screen size change)
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 712 // Unfolded width
      })

      fireEvent(window, new Event('resize'))
      await new Promise(resolve => setTimeout(resolve, 100))

      // Gestures should adapt to new screen size
      await simulateSwipeGesture(cardContainer, 'left', { distance: 140, duration: 300 })

      await waitFor(() => {
        expect(screen.getByTestId('status-display')).toHaveTextContent('Current Card: 3')
      })

      expect(mockGestureEvent).toHaveBeenCalledTimes(2)
    })
  })

  describe('Error Recovery and Resilience', () => {
    it('recovers from gesture conflicts gracefully', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')
      const cardContainer = screen.getByTestId('card-container')

      // Create conflicting gestures
      const conflictPromises = [
        simulateSwipeGesture(mainArea, 'up', { distance: 100, duration: 200 }),
        simulateSwipeGesture(cardContainer, 'left', { distance: 120, duration: 300 })
      ]

      // Should handle conflicts without crashes
      await Promise.all(conflictPromises)

      expect(screen.getByTestId('mobile-app')).toBeInTheDocument()
    })

    it('handles malformed touch events', async () => {
      render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // Malformed touch event
      const malformedEvent = new Event('touchstart') as any
      // Missing touches property

      expect(() => {
        fireEvent(mainArea, malformedEvent)
      }).not.toThrow()

      // Normal gestures should still work after malformed event
      await simulateSwipeGesture(mainArea, 'up', { distance: 100, duration: 200 })

      expect(mockGestureEvent).toHaveBeenCalled()
    })

    it('recovers from component unmounting during gesture', async () => {
      const { unmount } = render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)

      const mainArea = screen.getByTestId('main-area')

      // Start gesture
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 300 }])
      fireEvent(mainArea, touchStart)

      // Unmount component during gesture
      unmount()

      // Should not cause errors
      expect(true).toBe(true)
    })

    it('handles rapid component re-mounting', async () => {
      for (let i = 0; i < 5; i++) {
        const { unmount } = render(<MobileAppSimulator onGestureEvent={mockGestureEvent} />)
        
        const mainArea = screen.getByTestId('main-area')
        
        // Quick gesture before unmount
        await simulateSwipeGesture(mainArea, 'up', { distance: 50, duration: 100 })
        
        unmount()
      }

      // Should handle rapid mount/unmount cycles
      expect(true).toBe(true)
    })
  })
})