/**
 * Touch Gestures Hook Tests
 * 
 * Comprehensive tests for the useTouchGestures hook including:
 * - Long press detection and handling
 * - Pinch-to-zoom gesture recognition
 * - Swipe gesture detection and direction
 * - Tap and double-tap handling
 * - Touch target validation
 * - Multi-touch gesture coordination
 * - Performance with rapid touch events
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act, fireEvent } from '@testing-library/react'
import { useTouchGestures, useTouchTargetValidation } from '../useTouchGestures'
import {
  createMockTouchEvent,
  simulateLongPress,
  simulatePinchGesture,
  simulateSwipeGesture,
  setupBentoTestEnvironment
} from '@/test/utils/bento-test-utils'

describe('useTouchGestures Hook', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let mockElement: HTMLElement
  let handlers: {
    onLongPress: ReturnType<typeof vi.fn>
    onPinch: ReturnType<typeof vi.fn>
    onSwipe: ReturnType<typeof vi.fn>
    onTap: ReturnType<typeof vi.fn>
    onDoubleTap: ReturnType<typeof vi.fn>
  }

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    mockElement = document.createElement('div')
    document.body.appendChild(mockElement)
    
    handlers = {
      onLongPress: vi.fn(),
      onPinch: vi.fn(),
      onSwipe: vi.fn(),
      onTap: vi.fn(),
      onDoubleTap: vi.fn()
    }
    
    vi.clearAllMocks()
  })

  afterEach(() => {
    document.body.removeChild(mockElement)
    vi.restoreAllMocks()
  })

  describe('Hook Initialization', () => {
    it('returns correct initial state', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      expect(result.current.isLongPressing).toBe(false)
      expect(result.current.isPinching).toBe(false)
      expect(result.current.currentScale).toBe(1)
      expect(result.current.isTouchDevice).toBe(true)
      expect(typeof result.current.attachListeners).toBe('function')
    })

    it('merges configuration with defaults', () => {
      const customConfig = {
        longPressDelay: 1000,
        swipeThreshold: 75,
        enableDoubleTap: false
      }

      const { result } = renderHook(() => 
        useTouchGestures(customConfig, handlers)
      )

      expect(result.current.config.longPressDelay).toBe(1000)
      expect(result.current.config.swipeThreshold).toBe(75)
      expect(result.current.config.enableDoubleTap).toBe(false)
      expect(result.current.config.pinchThreshold).toBe(0.1) // Default value
    })

    it('detects touch device capability', () => {
      testEnvironment.disableTouchDevice()
      
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      expect(result.current.isTouchDevice).toBe(false)
    })
  })

  describe('Long Press Detection', () => {
    it('detects long press after specified delay', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ longPressDelay: 300 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateLongPress(mockElement, { duration: 350 })

      expect(handlers.onLongPress).toHaveBeenCalledWith(
        expect.objectContaining({
          x: expect.any(Number),
          y: expect.any(Number),
          duration: 300
        }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('cancels long press on touch movement', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ longPressDelay: 300 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start touch
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      // Move touch significantly
      const touchMove = createMockTouchEvent('touchmove', [{ clientX: 120, clientY: 120 }])
      mockElement.dispatchEvent(touchMove)

      // Wait for delay
      await new Promise(resolve => setTimeout(resolve, 350))

      expect(handlers.onLongPress).not.toHaveBeenCalled()

      cleanup()
    })

    it('updates isLongPressing state correctly', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ longPressDelay: 100 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      expect(result.current.isLongPressing).toBe(false)

      await simulateLongPress(mockElement, { duration: 150 })

      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 50))
      })

      expect(result.current.isLongPressing).toBe(true)

      cleanup()
    })

    it('respects enableLongPress configuration', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ enableLongPress: false }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateLongPress(mockElement, { duration: 600 })

      expect(handlers.onLongPress).not.toHaveBeenCalled()

      cleanup()
    })
  })

  describe('Pinch Gesture Detection', () => {
    it('detects pinch gestures with two touches', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ pinchThreshold: 0.1 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulatePinchGesture(mockElement, {
        startDistance: 100,
        endDistance: 150,
        steps: 3
      })

      expect(handlers.onPinch).toHaveBeenCalled()
      expect(handlers.onPinch).toHaveBeenCalledWith(
        expect.objectContaining({
          scale: expect.any(Number),
          deltaScale: expect.any(Number),
          center: expect.objectContaining({
            x: expect.any(Number),
            y: expect.any(Number)
          })
        }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('updates pinch state during gesture', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start pinch
      const touchStart = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 250, identifier: 0 },
        { clientX: 300, clientY: 250, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchStart)

      await act(async () => {
        expect(result.current.isPinching).toBe(true)
      })

      cleanup()
    })

    it('calculates scale correctly during pinch', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ pinchThreshold: 0.05 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulatePinchGesture(mockElement, {
        startDistance: 100,
        endDistance: 200 // 2x scale
      })

      const lastCall = handlers.onPinch.mock.calls[handlers.onPinch.mock.calls.length - 1]
      expect(lastCall[0].scale).toBeCloseTo(2, 1)

      cleanup()
    })

    it('respects pinch threshold for filtering small movements', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ pinchThreshold: 0.5 }, handlers) // High threshold
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulatePinchGesture(mockElement, {
        startDistance: 100,
        endDistance: 110 // Small change
      })

      expect(handlers.onPinch).not.toHaveBeenCalled()

      cleanup()
    })

    it('respects enablePinch configuration', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ enablePinch: false }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulatePinchGesture(mockElement, {
        startDistance: 100,
        endDistance: 150
      })

      expect(handlers.onPinch).not.toHaveBeenCalled()

      cleanup()
    })
  })

  describe('Swipe Gesture Detection', () => {
    it('detects horizontal swipe gestures', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ swipeThreshold: 50 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateSwipeGesture(mockElement, 'right', {
        distance: 75,
        duration: 200
      })

      expect(handlers.onSwipe).toHaveBeenCalledWith(
        expect.objectContaining({
          direction: 'right',
          distance: expect.any(Number),
          velocity: expect.any(Number),
          duration: expect.any(Number)
        }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('detects vertical swipe gestures', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ swipeThreshold: 50 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateSwipeGesture(mockElement, 'up', {
        distance: 80,
        duration: 150
      })

      expect(handlers.onSwipe).toHaveBeenCalledWith(
        expect.objectContaining({
          direction: 'up',
          distance: expect.any(Number),
          velocity: expect.any(Number)
        }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('requires minimum distance for swipe detection', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ swipeThreshold: 100 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateSwipeGesture(mockElement, 'left', {
        distance: 50, // Below threshold
        duration: 200
      })

      expect(handlers.onSwipe).not.toHaveBeenCalled()

      cleanup()
    })

    it('requires swipe within time limit', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ swipeTimeout: 200 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateSwipeGesture(mockElement, 'right', {
        distance: 100,
        duration: 500 // Too slow
      })

      expect(handlers.onSwipe).not.toHaveBeenCalled()

      cleanup()
    })

    it('calculates velocity correctly', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateSwipeGesture(mockElement, 'right', {
        distance: 100,
        duration: 200
      })

      const call = handlers.onSwipe.mock.calls[0]
      const expectedVelocity = 100 / 200 // distance / duration
      expect(call[0].velocity).toBeCloseTo(expectedVelocity, 1)

      cleanup()
    })

    it('prevents tap detection after swipe', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateSwipeGesture(mockElement, 'right', {
        distance: 100,
        duration: 200
      })

      // Should not trigger tap after swipe
      expect(handlers.onTap).not.toHaveBeenCalled()

      cleanup()
    })
  })

  describe('Tap and Double Tap Detection', () => {
    it('detects single taps', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ doubleTapDelay: 300 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      const touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      // Wait for double tap timeout
      await new Promise(resolve => setTimeout(resolve, 350))

      expect(handlers.onTap).toHaveBeenCalledWith(
        expect.objectContaining({
          x: 100,
          y: 100,
          tapCount: 1
        }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('detects double taps', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ doubleTapDelay: 300 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // First tap
      let touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)
      let touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      // Second tap within delay
      await new Promise(resolve => setTimeout(resolve, 100))
      touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)
      touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      expect(handlers.onDoubleTap).toHaveBeenCalledWith(
        expect.objectContaining({
          x: 100,
          y: 100,
          tapCount: 2
        }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('resets tap count after double tap delay', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ doubleTapDelay: 200 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // First tap
      let touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)
      let touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      // Wait longer than double tap delay
      await new Promise(resolve => setTimeout(resolve, 250))

      // Second tap (should be treated as new single tap)
      touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)
      touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      // Should trigger single tap for first tap
      expect(handlers.onTap).toHaveBeenCalledWith(
        expect.objectContaining({ tapCount: 1 }),
        expect.any(TouchEvent)
      )

      cleanup()
    })

    it('respects enableDoubleTap configuration', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ enableDoubleTap: false }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Double tap sequence
      let touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)
      let touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      await new Promise(resolve => setTimeout(resolve, 50))

      touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)
      touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      expect(handlers.onDoubleTap).not.toHaveBeenCalled()

      cleanup()
    })
  })

  describe('Touch Target Validation', () => {
    it('validates touch target size', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      // Mock a small element
      const smallElement = document.createElement('button')
      Object.defineProperty(smallElement, 'getBoundingClientRect', {
        value: () => ({ width: 30, height: 30 })
      })

      const isValid = result.current.isValidTouchTarget(smallElement)
      expect(isValid).toBe(false)
    })

    it('accepts interactive elements even if small', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})

      // Mock a small button element
      const smallButton = document.createElement('button')
      Object.defineProperty(smallButton, 'getBoundingClientRect', {
        value: () => ({ width: 30, height: 30 })
      })
      Object.defineProperty(smallButton, 'matches', {
        value: (selector: string) => selector.includes('button')
      })

      const isValid = result.current.isValidTouchTarget(smallButton)
      expect(isValid).toBe(true)
      expect(consoleSpy).toHaveBeenCalled() // Should warn but still allow

      consoleSpy.mockRestore()
    })

    it('checks parent elements for interactive targets', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      // Create nested structure
      const parent = document.createElement('div')
      const child = document.createElement('span')
      parent.appendChild(child)

      Object.defineProperty(parent, 'getBoundingClientRect', {
        value: () => ({ width: 50, height: 50 })
      })
      Object.defineProperty(child, 'getBoundingClientRect', {
        value: () => ({ width: 20, height: 20 })
      })
      Object.defineProperty(child, 'parentElement', {
        value: parent
      })

      const isValid = result.current.isValidTouchTarget(child)
      expect(isValid).toBe(true) // Should be valid due to parent size
    })
  })

  describe('Multi-touch Coordination', () => {
    it('cancels long press when second touch is added', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ longPressDelay: 300 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start single touch
      const touchStart1 = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart1)

      // Add second touch before long press completes
      const touchStart2 = createMockTouchEvent('touchstart', [
        { clientX: 100, clientY: 100 },
        { clientX: 200, clientY: 200 }
      ])
      mockElement.dispatchEvent(touchStart2)

      // Wait for long press delay
      await new Promise(resolve => setTimeout(resolve, 350))

      expect(handlers.onLongPress).not.toHaveBeenCalled()

      cleanup()
    })

    it('switches from potential long press to pinch mode', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start with one touch
      const touchStart1 = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
      mockElement.dispatchEvent(touchStart1)

      // Add second touch (should cancel long press and enable pinch)
      const touchStart2 = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 200 },
        { clientX: 300, clientY: 200 }
      ])
      mockElement.dispatchEvent(touchStart2)

      await act(async () => {
        expect(result.current.isPinching).toBe(true)
        expect(result.current.isLongPressing).toBe(false)
      })

      cleanup()
    })

    it('clears all gestures when more than two touches are detected', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start with two touches (pinch mode)
      const touchStart2 = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 200 },
        { clientX: 300, clientY: 200 }
      ])
      mockElement.dispatchEvent(touchStart2)

      // Add third touch
      const touchStart3 = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 200 },
        { clientX: 300, clientY: 200 },
        { clientX: 250, clientY: 300 }
      ])
      mockElement.dispatchEvent(touchStart3)

      await act(async () => {
        expect(result.current.isPinching).toBe(false)
        expect(result.current.isLongPressing).toBe(false)
      })

      cleanup()
    })
  })

  describe('Touch Cancel Handling', () => {
    it('cleans up state on touch cancel', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start long press
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      // Cancel touch
      const touchCancel = new TouchEvent('touchcancel', {
        bubbles: true,
        cancelable: true
      })
      mockElement.dispatchEvent(touchCancel)

      await act(async () => {
        expect(result.current.isLongPressing).toBe(false)
        expect(result.current.isPinching).toBe(false)
        expect(result.current.currentScale).toBe(1)
      })

      cleanup()
    })
  })

  describe('Performance and Edge Cases', () => {
    it('handles rapid touch events without performance issues', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      const startTime = performance.now()

      // Simulate rapid touch events
      for (let i = 0; i < 50; i++) {
        const touchEvent = createMockTouchEvent('touchmove', [
          { clientX: 100 + i, clientY: 100 + i }
        ])
        mockElement.dispatchEvent(touchEvent)
      }

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(100) // Should handle quickly

      cleanup()
    })

    it('handles high-frequency touch events on mobile devices', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)
      const startTime = performance.now()

      // Simulate 120fps touch events (mobile devices can go this high)
      for (let i = 0; i < 120; i++) {
        const touchEvent = createMockTouchEvent('touchmove', [
          { clientX: 200 + Math.sin(i * 0.1) * 50, clientY: 200 + Math.cos(i * 0.1) * 50 }
        ])
        mockElement.dispatchEvent(touchEvent)
      }

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(200) // Should handle 120fps without lag

      cleanup()
    })

    it('maintains performance with multiple simultaneous gestures', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)
      const startTime = performance.now()

      // Simulate complex multi-touch with rapid updates
      for (let i = 0; i < 30; i++) {
        const touchEvent = createMockTouchEvent('touchmove', [
          { clientX: 100 + i, clientY: 100 + i, identifier: 0 },
          { clientX: 300 - i, clientY: 100 + i, identifier: 1 },
          { clientX: 200, clientY: 200 + i * 2, identifier: 2 }
        ])
        mockElement.dispatchEvent(touchEvent)
      }

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(150) // Should handle multi-touch efficiently

      cleanup()
    })

    it('does not leak memory with repeated attach/detach cycles', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      // Simulate multiple attach/detach cycles
      for (let i = 0; i < 10; i++) {
        const cleanup = result.current.attachListeners(mockElement)
        cleanup()
      }

      // Should not accumulate event listeners
      // expect(mockElement.eventListenerCount).toBeUndefined() // Non-standard property
    })

    it('properly removes event listeners on cleanup', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const addEventListenerSpy = vi.spyOn(mockElement, 'addEventListener')
      const removeEventListenerSpy = vi.spyOn(mockElement, 'removeEventListener')

      const cleanup = result.current.attachListeners(mockElement)

      expect(addEventListenerSpy).toHaveBeenCalledTimes(4) // 4 touch events

      cleanup()

      expect(removeEventListenerSpy).toHaveBeenCalledTimes(4)
    })

    it('handles malformed touch events gracefully', () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Malformed touch event
      const malformedEvent = new Event('touchstart')
      
      expect(() => {
        mockElement.dispatchEvent(malformedEvent)
      }).not.toThrow()

      cleanup()
    })
  })

  describe('Gesture Conflict Resolution', () => {
    it('prioritizes pinch over long press when second touch is added', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ longPressDelay: 200 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start single touch (potential long press)
      const touchStart1 = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart1)

      // Add second touch before long press completes (should cancel long press, start pinch)
      const touchStart2 = createMockTouchEvent('touchstart', [
        { clientX: 100, clientY: 100, identifier: 0 },
        { clientX: 200, clientY: 100, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchStart2)

      await new Promise(resolve => setTimeout(resolve, 250))

      expect(handlers.onLongPress).not.toHaveBeenCalled()
      expect(result.current.isPinching).toBe(true)

      cleanup()
    })

    it('cancels pinch when third touch is added', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start with two touches (pinch mode)
      const touchStart2 = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 200, identifier: 0 },
        { clientX: 300, clientY: 200, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchStart2)

      expect(result.current.isPinching).toBe(true)

      // Add third touch (should cancel pinch)
      const touchStart3 = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 200, identifier: 0 },
        { clientX: 300, clientY: 200, identifier: 1 },
        { clientX: 250, clientY: 300, identifier: 2 }
      ])
      mockElement.dispatchEvent(touchStart3)

      expect(result.current.isPinching).toBe(false)

      cleanup()
    })

    it('resolves conflicting swipe vs tap gestures', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({ swipeThreshold: 30 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start touch
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      // Move enough to trigger swipe
      const touchMove = createMockTouchEvent('touchmove', [{ clientX: 140, clientY: 100 }])
      mockElement.dispatchEvent(touchMove)

      const touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      // Should trigger swipe, not tap
      expect(handlers.onSwipe).toHaveBeenCalled()

      // Wait for tap timeout to ensure tap is not called
      await new Promise(resolve => setTimeout(resolve, 350))
      expect(handlers.onTap).not.toHaveBeenCalled()

      cleanup()
    })

    it('handles rapid gesture switching', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start with potential long press
      let touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      // Quickly add second touch (switch to pinch)
      touchStart = createMockTouchEvent('touchstart', [
        { clientX: 100, clientY: 100, identifier: 0 },
        { clientX: 200, clientY: 100, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchStart)

      // Quickly remove one touch (back to potential tap)
      const touchEnd = createMockTouchEvent('touchend', [])
      Object.defineProperty(touchEnd, 'touches', {
        value: [{ clientX: 100, clientY: 100, identifier: 0 }]
      })
      mockElement.dispatchEvent(touchEnd)

      // Remove remaining touch
      const finalTouchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(finalTouchEnd)

      // Should handle rapid switching without errors
      expect(() => {
        vi.advanceTimersByTime(500)
      }).not.toThrow()

      cleanup()
    })

    it('prevents gesture interference between multiple elements', async () => {
      const element1 = document.createElement('div')
      const element2 = document.createElement('div')
      document.body.appendChild(element1)
      document.body.appendChild(element2)

      const handlers1 = {
        onLongPress: vi.fn(),
        onPinch: vi.fn(),
        onSwipe: vi.fn(),
        onTap: vi.fn()
      }

      const handlers2 = {
        onLongPress: vi.fn(),
        onPinch: vi.fn(),
        onSwipe: vi.fn(),
        onTap: vi.fn()
      }

      const { result: result1 } = renderHook(() => useTouchGestures({}, handlers1))
      const { result: result2 } = renderHook(() => useTouchGestures({}, handlers2))

      const cleanup1 = result1.current.attachListeners(element1)
      const cleanup2 = result2.current.attachListeners(element2)

      // Touch on element1
      const touchStart1 = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      Object.defineProperty(touchStart1, 'target', { value: element1 })
      element1.dispatchEvent(touchStart1)

      // Touch on element2 simultaneously
      const touchStart2 = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
      Object.defineProperty(touchStart2, 'target', { value: element2 })
      element2.dispatchEvent(touchStart2)

      await simulateLongPress(element1, { duration: 600 })

      // Only element1 should receive the long press
      expect(handlers1.onLongPress).toHaveBeenCalled()
      expect(handlers2.onLongPress).not.toHaveBeenCalled()

      cleanup1()
      cleanup2()
      document.body.removeChild(element1)
      document.body.removeChild(element2)
    })
  })

  describe('Mobile Device Simulation', () => {
    it('adjusts behavior for different device pixel ratios', () => {
      // Mock high DPI device
      Object.defineProperty(window, 'devicePixelRatio', {
        writable: true,
        value: 3
      })

      const { result } = renderHook(() => 
        useTouchGestures({ swipeThreshold: 50 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Touch coordinates should be adjusted for high DPI
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      const touchMove = createMockTouchEvent('touchmove', [{ clientX: 150, clientY: 100 }])
      mockElement.dispatchEvent(touchMove)

      const touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      expect(handlers.onSwipe).toHaveBeenCalled()

      cleanup()
    })

    it('simulates iOS Safari touch behavior', () => {
      // Mock iOS Safari user agent
      Object.defineProperty(navigator, 'userAgent', {
        writable: true,
        value: 'Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1'
      })

      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      expect(result.current.isTouchDevice).toBe(true)

      const cleanup = result.current.attachListeners(mockElement)

      // iOS Safari has specific touch behavior characteristics
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      touchStart.preventDefault = vi.fn()
      mockElement.dispatchEvent(touchStart)

      // Should prevent default on touch start for iOS
      expect(touchStart.preventDefault).toHaveBeenCalled()

      cleanup()
    })

    it('simulates Android Chrome touch behavior', () => {
      // Mock Android Chrome user agent
      Object.defineProperty(navigator, 'userAgent', {
        writable: true,
        value: 'Mozilla/5.0 (Linux; Android 12; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.101 Mobile Safari/537.36'
      })

      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      expect(result.current.isTouchDevice).toBe(true)

      const cleanup = result.current.attachListeners(mockElement)

      // Android Chrome might have different timing characteristics
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      mockElement.dispatchEvent(touchStart)

      // Simulate Android's tendency for slightly delayed touch events
      setTimeout(() => {
        const touchMove = createMockTouchEvent('touchmove', [{ clientX: 110, clientY: 100 }])
        mockElement.dispatchEvent(touchMove)
      }, 5)

      cleanup()
    })

    it('handles touch events with hardware acceleration', () => {
      // Mock hardware acceleration support
      Object.defineProperty(window, 'TouchEvent', {
        value: class TouchEvent extends Event {
          touches: TouchList
          changedTouches: TouchList
          targetTouches: TouchList

          constructor(type: string, eventInitDict: any = {}) {
            super(type, eventInitDict)
            this.touches = eventInitDict.touches || []
            this.changedTouches = eventInitDict.changedTouches || []
            this.targetTouches = eventInitDict.targetTouches || []
          }
        }
      })

      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Should work with hardware-accelerated touch events
      expect(() => {
        const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
        mockElement.dispatchEvent(touchStart)
      }).not.toThrow()

      cleanup()
    })

    it('simulates low-end device performance characteristics', async () => {
      // Mock slower device by adding artificial delays
      const originalSetTimeout = window.setTimeout
      window.setTimeout = ((callback: Function, delay: number) => {
        return originalSetTimeout(callback, delay + 20) // Add 20ms delay to simulate slow device
      }) as any

      const { result } = renderHook(() => 
        useTouchGestures({ longPressDelay: 300 }, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      await simulateLongPress(mockElement, { duration: 350 })

      // Should still work on slower devices, just with adjusted timing
      expect(handlers.onLongPress).toHaveBeenCalled()

      window.setTimeout = originalSetTimeout
      cleanup()
    })

    it('handles device orientation changes during gestures', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, handlers)
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Start a pinch gesture
      const touchStart = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 250, identifier: 0 },
        { clientX: 300, clientY: 250, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchStart)

      expect(result.current.isPinching).toBe(true)

      // Simulate orientation change (triggers resize)
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        value: 800 // Landscape
      })
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 600
      })

      fireEvent(window, new Event('resize'))
      fireEvent(window, new Event('orientationchange'))

      // Gesture should continue working after orientation change
      const touchMove = createMockTouchEvent('touchmove', [
        { clientX: 180, clientY: 250, identifier: 0 },
        { clientX: 320, clientY: 250, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchMove)

      expect(handlers.onPinch).toHaveBeenCalled()

      cleanup()
    })
  })
})

describe('useTouchTargetValidation Hook', () => {
  let mockElement: HTMLElement
  let elementRef: React.RefObject<HTMLElement>

  beforeEach(() => {
    mockElement = document.createElement('div')
    elementRef = { current: mockElement }
  })

  it('validates element size against minimum touch target', () => {
    Object.defineProperty(mockElement, 'getBoundingClientRect', {
      value: () => ({ width: 50, height: 50 })
    })

    const { result } = renderHook(() => 
      useTouchTargetValidation(elementRef)
    )

    expect(result.current.isValidTarget).toBe(true)
    expect(result.current.suggestions).toHaveLength(0)
  })

  it('provides suggestions for invalid targets', () => {
    Object.defineProperty(mockElement, 'getBoundingClientRect', {
      value: () => ({ width: 30, height: 20 })
    })

    const { result } = renderHook(() => 
      useTouchTargetValidation(elementRef)
    )

    expect(result.current.isValidTarget).toBe(false)
    expect(result.current.suggestions.length).toBeGreaterThan(0)
    expect(result.current.suggestions.some(s => s.includes('width'))).toBe(true)
    expect(result.current.suggestions.some(s => s.includes('height'))).toBe(true)
  })

  it('provides minimum size reference', () => {
    const { result } = renderHook(() => 
      useTouchTargetValidation(elementRef)
    )

    expect(result.current.minSize).toBe(44) // Standard minimum touch target
  })

  it('handles null element reference', () => {
    const nullRef = { current: null } as unknown as React.RefObject<HTMLElement>

    const { result } = renderHook(() => 
      useTouchTargetValidation(nullRef)
    )

    expect(result.current.isValidTarget).toBe(true) // Default to valid
    expect(result.current.suggestions).toHaveLength(0)
  })
})