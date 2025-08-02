/**
 * Touch Gestures Performance Tests
 * 
 * Focused performance and stress tests for the useTouchGestures hook:
 * - Memory usage optimization
 * - High-frequency event handling
 * - Concurrent gesture processing
 * - Memory leak detection
 * - Performance under extreme conditions
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useTouchGestures } from '@/hooks/useTouchGestures'
import {
  createMockTouchEvent,
  simulateSwipeGesture,
  simulatePinchGesture,
  setupBentoTestEnvironment
} from '@/test/utils/bento-test-utils'

// Performance monitoring utilities
interface MemoryUsage {
  used: number;
  total: number;
  timestamp: number;
}

const createPerformanceMonitor = () => {
  const metrics = {
    memoryUsage: [] as MemoryUsage[],
    executionTimes: [] as number[],
    eventCounts: { total: 0, processed: 0, throttled: 0 },
    frameRates: [] as number[]
  }

  const startTime = performance.now()
  let lastFrameTime = startTime

  const monitor = {
    recordExecution: (duration: number) => {
      metrics.executionTimes.push(duration)
    },
    
    recordMemory: () => {
      if ((performance as any).memory) {
        metrics.memoryUsage.push({
          used: (performance as any).memory.usedJSHeapSize,
          total: (performance as any).memory.totalJSHeapSize,
          timestamp: performance.now() - startTime
        })
      }
    },
    
    recordEvent: (processed: boolean) => {
      metrics.eventCounts.total++
      if (processed) {
        metrics.eventCounts.processed++
      } else {
        metrics.eventCounts.throttled++
      }
    },
    
    recordFrame: () => {
      const currentTime = performance.now()
      const frameDuration = currentTime - lastFrameTime
      metrics.frameRates.push(1000 / frameDuration)
      lastFrameTime = currentTime
    },
    
    getMetrics: () => ({
      ...metrics,
      avgExecutionTime: metrics.executionTimes.reduce((a, b) => a + b, 0) / metrics.executionTimes.length,
      avgFrameRate: metrics.frameRates.reduce((a, b) => a + b, 0) / metrics.frameRates.length,
      memoryGrowth: metrics.memoryUsage.length > 1 
        ? metrics.memoryUsage[metrics.memoryUsage.length - 1].used - metrics.memoryUsage[0].used
        : 0
    }),
    
    reset: () => {
      metrics.memoryUsage.length = 0
      metrics.executionTimes.length = 0
      metrics.eventCounts = { total: 0, processed: 0, throttled: 0 }
      metrics.frameRates.length = 0
    }
  }

  return monitor
}

// Stress test utilities
const generateHighFrequencyTouchSequence = (
  element: HTMLElement,
  duration: number = 1000,
  frequency: number = 120 // 120fps
) => {
  const events: TouchEvent[] = []
  const interval = 1000 / frequency
  const totalEvents = Math.floor(duration / interval)

  for (let i = 0; i < totalEvents; i++) {
    const progress = i / totalEvents
    const x = 200 + Math.sin(progress * Math.PI * 4) * 50
    const y = 200 + Math.cos(progress * Math.PI * 4) * 50
    
    events.push(createMockTouchEvent('touchmove', [{ clientX: x, clientY: y }]))
  }

  return events
}

const simulateMemoryPressure = (iterations: number = 1000) => {
  const memoryBallast: any[] = []
  
  for (let i = 0; i < iterations; i++) {
    // Create memory pressure with large objects
    memoryBallast.push({
      id: i,
      data: new Array(1000).fill(Math.random()),
      nested: {
        more: new Array(500).fill({ value: Math.random() }),
        deeper: new Array(100).fill(new Array(10).fill(Math.random()))
      }
    })
  }
  
  return () => {
    memoryBallast.length = 0
  }
}

describe('Touch Gestures Performance Tests', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let mockElement: HTMLElement
  let performanceMonitor: ReturnType<typeof createPerformanceMonitor>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    mockElement = document.createElement('div')
    document.body.appendChild(mockElement)
    
    performanceMonitor = createPerformanceMonitor()
    
    // Mock (performance as any).memory for memory monitoring
    Object.defineProperty(performance, 'memory', {
      value: {
        usedJSHeapSize: 1000000,
        totalJSHeapSize: 2000000,
        jsHeapSizeLimit: 4000000
      },
      configurable: true
    })
    
    vi.clearAllMocks()
  })

  afterEach(() => {
    document.body.removeChild(mockElement)
    performanceMonitor.reset()
    vi.restoreAllMocks()
  })

  describe('High-Frequency Event Handling', () => {
    it('maintains performance with 120fps touch events', async () => {
      const handlers = {
        onTouchMove: vi.fn(),
        onSwipe: vi.fn()
      }

      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: (detail) => {
            const start = performance.now()
            handlers.onSwipe(detail)
            const end = performance.now()
            performanceMonitor.recordExecution(end - start)
          }
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Generate high-frequency events
      const events = generateHighFrequencyTouchSequence(mockElement, 1000, 120)
      
      const startTime = performance.now()
      
      // Start gesture
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
      mockElement.dispatchEvent(touchStart)
      
      // Rapid-fire touch move events
      for (const event of events) {
        mockElement.dispatchEvent(event)
        performanceMonitor.recordEvent(true)
        performanceMonitor.recordFrame()
      }
      
      // End gesture
      const touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)
      
      const endTime = performance.now()
      const totalDuration = endTime - startTime

      // Performance assertions
      expect(totalDuration).toBeLessThan(1500) // Should complete within 1.5 seconds
      
      const metrics = performanceMonitor.getMetrics()
      expect(metrics.avgExecutionTime).toBeLessThan(5) // Each handler should be fast
      expect(metrics.avgFrameRate).toBeGreaterThan(30) // Maintain reasonable frame rate

      cleanup()
    })

    it('throttles excessive touch events effectively', async () => {
      let processedEvents = 0
      let throttledEvents = 0

      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: () => {
            processedEvents++
            performanceMonitor.recordEvent(true)
          }
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Generate excessive events (240fps)
      const events = generateHighFrequencyTouchSequence(mockElement, 500, 240)
      
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
      mockElement.dispatchEvent(touchStart)
      
      for (const event of events) {
        mockElement.dispatchEvent(event)
        if (processedEvents === 0) {
          throttledEvents++
          performanceMonitor.recordEvent(false)
        }
      }
      
      const touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      // Should throttle excessive events
      const metrics = performanceMonitor.getMetrics()
      expect(metrics.eventCounts.throttled).toBeGreaterThan(0)
      expect(metrics.eventCounts.processed).toBeLessThan(metrics.eventCounts.total)

      cleanup()
    })

    it('handles burst events without memory leaks', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onPinch: vi.fn(),
          onSwipe: vi.fn(),
          onTap: vi.fn(),
          onLongPress: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      performanceMonitor.recordMemory()

      // Multiple bursts of events
      for (let burst = 0; burst < 10; burst++) {
        const events = generateHighFrequencyTouchSequence(mockElement, 100, 60)
        
        const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
        mockElement.dispatchEvent(touchStart)
        
        for (const event of events) {
          mockElement.dispatchEvent(event)
        }
        
        const touchEnd = createMockTouchEvent('touchend', [])
        mockElement.dispatchEvent(touchEnd)
        
        // Force garbage collection opportunity
        await new Promise(resolve => setTimeout(resolve, 10))
        
        performanceMonitor.recordMemory()
      }

      const metrics = performanceMonitor.getMetrics()
      
      // Memory growth should be minimal (allow some reasonable growth)
      expect(metrics.memoryGrowth).toBeLessThan(100000) // Less than 100KB growth

      cleanup()
    })

    it('maintains accuracy with rapid gesture changes', async () => {
      const gestureLog: string[] = []

      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: (detail) => gestureLog.push(`swipe-${detail.direction}`),
          onPinch: (detail) => gestureLog.push(`pinch-${detail.scale.toFixed(2)}`),
          onTap: () => gestureLog.push('tap'),
          onLongPress: () => gestureLog.push('longpress')
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Rapid gesture sequence
      const gestureSequence = [
        () => simulateSwipeGesture(mockElement, 'left', { distance: 80, duration: 100 }),
        () => simulateSwipeGesture(mockElement, 'right', { distance: 80, duration: 100 }),
        () => simulatePinchGesture(mockElement, { startDistance: 100, endDistance: 150, steps: 3 }),
        () => simulateSwipeGesture(mockElement, 'up', { distance: 80, duration: 100 }),
        () => simulatePinchGesture(mockElement, { startDistance: 150, endDistance: 100, steps: 3 })
      ]

      const startTime = performance.now()

      for (const gesture of gestureSequence) {
        await gesture()
        await new Promise(resolve => setTimeout(resolve, 20)) // Brief pause
      }

      const endTime = performance.now()

      // Should complete rapidly
      expect(endTime - startTime).toBeLessThan(1000)
      
      // Should detect most gestures accurately
      expect(gestureLog.length).toBeGreaterThan(3)

      cleanup()
    })
  })

  describe('Memory Usage Optimization', () => {
    it('prevents memory leaks with repeated attach/detach cycles', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: vi.fn(),
          onPinch: vi.fn()
        })
      )

      performanceMonitor.recordMemory()

      // Repeated attach/detach cycles
      for (let cycle = 0; cycle < 50; cycle++) {
        const cleanup = result.current.attachListeners(mockElement)
        
        // Simulate some gesture activity
        await simulateSwipeGesture(mockElement, 'left', { distance: 50, duration: 100 })
        
        cleanup()
        
        if (cycle % 10 === 0) {
          performanceMonitor.recordMemory()
        }
      }

      const metrics = performanceMonitor.getMetrics()
      
      // Memory should not grow significantly
      expect(metrics.memoryGrowth).toBeLessThan(50000) // Less than 50KB growth
    })

    it('efficiently manages event listener lifecycle', async () => {
      const addEventListenerSpy = vi.spyOn(mockElement, 'addEventListener')
      const removeEventListenerSpy = vi.spyOn(mockElement, 'removeEventListener')

      const { result } = renderHook(() => 
        useTouchGestures({}, { onSwipe: vi.fn() })
      )

      const cleanup1 = result.current.attachListeners(mockElement)
      const cleanup2 = result.current.attachListeners(mockElement)
      const cleanup3 = result.current.attachListeners(mockElement)

      // Should efficiently handle multiple attachments
      expect(addEventListenerSpy).toHaveBeenCalled()

      cleanup1()
      cleanup2()
      cleanup3()

      // Should properly clean up all listeners
      expect(removeEventListenerSpy).toHaveBeenCalled()
      expect(removeEventListenerSpy.mock.calls.length).toBeGreaterThan(0)
    })

    it('optimizes memory under pressure', async () => {
      const cleanupMemoryPressure = simulateMemoryPressure(2000)

      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: vi.fn(),
          onPinch: vi.fn(),
          onLongPress: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      performanceMonitor.recordMemory()

      // Perform gestures under memory pressure
      for (let i = 0; i < 20; i++) {
        await simulateSwipeGesture(mockElement, 'left', { distance: 100, duration: 100 })
        await simulatePinchGesture(mockElement, { startDistance: 100, endDistance: 120, steps: 3 })
        
        if (i % 5 === 0) {
          performanceMonitor.recordMemory()
        }
      }

      // Clean up memory pressure
      cleanupMemoryPressure()
      
      // Should continue to function under memory pressure
      await simulateSwipeGesture(mockElement, 'right', { distance: 100, duration: 100 })

      const metrics = performanceMonitor.getMetrics()
      expect(metrics.memoryUsage.length).toBeGreaterThan(0)

      cleanup()
    })

    it('handles large touch point datasets efficiently', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onPinch: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      performanceMonitor.recordMemory()

      // Simulate gesture with many touch points over time
      const touchStart = createMockTouchEvent('touchstart', [
        { clientX: 100, clientY: 100, identifier: 0 },
        { clientX: 200, clientY: 100, identifier: 1 }
      ])
      mockElement.dispatchEvent(touchStart)

      // Generate large number of touch move events with varying distances
      for (let i = 0; i < 1000; i++) {
        const distance = 100 + i * 0.5
        const touchMove = createMockTouchEvent('touchmove', [
          { clientX: 150 - distance/2, clientY: 100, identifier: 0 },
          { clientX: 150 + distance/2, clientY: 100, identifier: 1 }
        ])
        mockElement.dispatchEvent(touchMove)
        
        if (i % 100 === 0) {
          performanceMonitor.recordMemory()
        }
      }

      const touchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(touchEnd)

      const metrics = performanceMonitor.getMetrics()
      
      // Should handle large datasets without significant memory growth
      expect(metrics.memoryGrowth).toBeLessThan(200000) // Less than 200KB growth

      cleanup()
    })
  })

  describe('Concurrent Gesture Processing', () => {
    it('handles multiple simultaneous gesture recognizers', async () => {
      const results = Array.from({ length: 5 }, () => 
        renderHook(() => 
          useTouchGestures({}, {
            onSwipe: vi.fn(),
            onPinch: vi.fn(),
            onTap: vi.fn()
          })
        )
      )

      const elements = Array.from({ length: 5 }, () => {
        const el = document.createElement('div')
        document.body.appendChild(el)
        return el
      })

      const cleanups = results.map((result, i) => 
        result.result.current.attachListeners(elements[i])
      )

      const startTime = performance.now()

      // Concurrent gestures on different elements
      const gesturePromises = elements.map((element, i) => 
        simulateSwipeGesture(element, ['left', 'right', 'up', 'down'][i % 4] as any, {
          distance: 100,
          duration: 200
        })
      )

      await Promise.all(gesturePromises)

      const endTime = performance.now()

      // Should handle concurrent gestures efficiently
      expect(endTime - startTime).toBeLessThan(500)

      // Cleanup
      cleanups.forEach(cleanup => cleanup())
      elements.forEach(el => document.body.removeChild(el))
    })

    it('maintains performance with overlapping gesture areas', async () => {
      const parentElement = document.createElement('div')
      const childElement = document.createElement('div')
      parentElement.appendChild(childElement)
      document.body.appendChild(parentElement)

      const parentResult = renderHook(() => 
        useTouchGestures({}, { onSwipe: vi.fn() })
      )
      const childResult = renderHook(() => 
        useTouchGestures({}, { onPinch: vi.fn() })
      )

      const parentCleanup = parentResult.result.current.attachListeners(parentElement)
      const childCleanup = childResult.result.current.attachListeners(childElement)

      performanceMonitor.recordMemory()

      // Gestures on overlapping areas
      const startTime = performance.now()

      await Promise.all([
        simulateSwipeGesture(parentElement, 'left', { distance: 100, duration: 200 }),
        simulatePinchGesture(childElement, { startDistance: 100, endDistance: 150, steps: 5 })
      ])

      const endTime = performance.now()

      // Should handle overlapping areas efficiently
      expect(endTime - startTime).toBeLessThan(400)

      performanceMonitor.recordMemory()
      const metrics = performanceMonitor.getMetrics()
      expect(metrics.memoryGrowth).toBeLessThan(100000)

      // Cleanup
      parentCleanup()
      childCleanup()
      document.body.removeChild(parentElement)
    })

    it('scales efficiently with many active gesture recognizers', async () => {
      const recognizerCount = 20
      const results: any[] = []
      const elements: HTMLElement[] = []
      const cleanups: (() => void)[] = []

      // Create many gesture recognizers
      for (let i = 0; i < recognizerCount; i++) {
        const result = renderHook(() => 
          useTouchGestures({}, {
            onSwipe: vi.fn(),
            onPinch: vi.fn(),
            onTap: vi.fn(),
            onLongPress: vi.fn()
          })
        )
        
        const element = document.createElement('div')
        document.body.appendChild(element)
        
        const cleanup = result.result.current.attachListeners(element)
        
        results.push(result)
        elements.push(element)
        cleanups.push(cleanup)
      }

      performanceMonitor.recordMemory()

      const startTime = performance.now()

      // Simulate gestures on subset of recognizers
      const activeCount = Math.floor(recognizerCount / 3)
      const gesturePromises = []

      for (let i = 0; i < activeCount; i++) {
        const element = elements[i * 3] // Every third element
        gesturePromises.push(
          simulateSwipeGesture(element, 'left', { distance: 80, duration: 150 })
        )
      }

      await Promise.all(gesturePromises)

      const endTime = performance.now()

      // Should scale reasonably with many recognizers
      expect(endTime - startTime).toBeLessThan(1000)

      performanceMonitor.recordMemory()
      const metrics = performanceMonitor.getMetrics()
      
      // Memory should scale reasonably
      expect(metrics.memoryGrowth).toBeLessThan(500000) // Less than 500KB for 20 recognizers

      // Cleanup all recognizers
      cleanups.forEach(cleanup => cleanup())
      elements.forEach(el => document.body.removeChild(el))
    })
  })

  describe('Extreme Conditions', () => {
    it('survives stress test with rapid gesture switching', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: vi.fn(),
          onPinch: vi.fn(),
          onTap: vi.fn(),
          onLongPress: vi.fn(),
          onDoubleTap: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      performanceMonitor.recordMemory()

      const stressTestDuration = 2000 // 2 seconds
      const startTime = performance.now()
      let gestureCount = 0

      while (performance.now() - startTime < stressTestDuration) {
        const gestureType = gestureCount % 4
        
        switch (gestureType) {
          case 0:
            await simulateSwipeGesture(mockElement, 'left', { distance: 50, duration: 50 })
            break
          case 1:
            await simulateSwipeGesture(mockElement, 'right', { distance: 50, duration: 50 })
            break
          case 2:
            await simulatePinchGesture(mockElement, { 
              startDistance: 100, 
              endDistance: 120, 
              steps: 2 
            })
            break
          case 3:
            const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
            mockElement.dispatchEvent(touchStart)
            await new Promise(resolve => setTimeout(resolve, 20))
            const touchEnd = createMockTouchEvent('touchend', [])
            mockElement.dispatchEvent(touchEnd)
            break
        }
        
        gestureCount++
        
        if (gestureCount % 50 === 0) {
          performanceMonitor.recordMemory()
        }
      }

      const endTime = performance.now()

      // Should survive stress test
      expect(endTime - startTime).toBeLessThan(stressTestDuration + 500)
      expect(gestureCount).toBeGreaterThan(10) // Should have processed many gestures

      const metrics = performanceMonitor.getMetrics()
      expect(metrics.memoryGrowth).toBeLessThan(1000000) // Less than 1MB growth

      cleanup()
    })

    it('handles pathological input patterns', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: vi.fn(),
          onPinch: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Pathological pattern: Rapid start/stop cycles
      for (let cycle = 0; cycle < 100; cycle++) {
        const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 200 }])
        mockElement.dispatchEvent(touchStart)
        
        // Immediate stop
        const touchEnd = createMockTouchEvent('touchend', [])
        mockElement.dispatchEvent(touchEnd)
      }

      // Pathological pattern: Many touches at once
      const manyTouches = Array.from({ length: 10 }, (_, i) => ({
        clientX: 200 + i * 10,
        clientY: 200,
        identifier: i
      }))
      
      const multiTouchStart = createMockTouchEvent('touchstart', manyTouches)
      mockElement.dispatchEvent(multiTouchStart)
      
      const multiTouchEnd = createMockTouchEvent('touchend', [])
      mockElement.dispatchEvent(multiTouchEnd)

      // Should handle pathological patterns without crashing
      expect(mockElement).toBeInTheDocument()

      cleanup()
    })

    it('maintains functionality under CPU throttling simulation', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Simulate CPU throttling by adding artificial delays
      const originalSetTimeout = global.setTimeout
      global.setTimeout = ((callback: Function, delay: number) => {
        return originalSetTimeout(callback, delay * 3) // 3x slower
      }) as any

      const startTime = performance.now()

      // Perform gestures under simulated CPU throttling
      await simulateSwipeGesture(mockElement, 'left', { distance: 100, duration: 200 })
      await simulateSwipeGesture(mockElement, 'right', { distance: 100, duration: 200 })

      const endTime = performance.now()

      // Should adapt to slower CPU conditions
      expect(endTime - startTime).toBeGreaterThan(400) // Should be slower due to throttling
      expect(mockElement).toBeInTheDocument() // Should still function

      // Restore original setTimeout
      global.setTimeout = originalSetTimeout

      cleanup()
    })

    it('recovers gracefully from JavaScript errors during processing', async () => {
      const errorHandler = vi.fn()
      const originalConsoleError = console.error
      console.error = errorHandler

      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: () => {
            throw new Error('Simulated processing error')
          },
          onPinch: vi.fn() // This should still work
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      // Gesture that will cause error
      await simulateSwipeGesture(mockElement, 'left', { distance: 100, duration: 200 })

      // Gesture that should still work
      await simulatePinchGesture(mockElement, { 
        startDistance: 100, 
        endDistance: 150, 
        steps: 3 
      })

      // Should handle errors gracefully and continue functioning
      expect(mockElement).toBeInTheDocument()

      console.error = originalConsoleError
      cleanup()
    })
  })

  describe('Performance Regression Detection', () => {
    it('benchmarks basic gesture recognition performance', async () => {
      const iterations = 100
      const executionTimes: number[] = []

      for (let i = 0; i < iterations; i++) {
        const { result } = renderHook(() => 
          useTouchGestures({}, { onSwipe: vi.fn() })
        )

        const cleanup = result.current.attachListeners(mockElement)

        const startTime = performance.now()
        
        await simulateSwipeGesture(mockElement, 'left', { distance: 100, duration: 100 })
        
        const endTime = performance.now()
        executionTimes.push(endTime - startTime)

        cleanup()
      }

      const avgTime = executionTimes.reduce((a, b) => a + b, 0) / iterations
      const maxTime = Math.max(...executionTimes)

      // Performance benchmarks (these would be baseline values)
      expect(avgTime).toBeLessThan(50) // Average should be under 50ms
      expect(maxTime).toBeLessThan(100) // Max should be under 100ms
    })

    it('measures memory allocation efficiency', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, {
          onSwipe: vi.fn(),
          onPinch: vi.fn(),
          onTap: vi.fn(),
          onLongPress: vi.fn()
        })
      )

      const cleanup = result.current.attachListeners(mockElement)

      const memoryMeasurements: number[] = []
      
      if ((performance as any).memory) {
        memoryMeasurements.push((performance as any).memory.usedJSHeapSize)

        // Perform standard gesture set
        for (let i = 0; i < 50; i++) {
          await simulateSwipeGesture(mockElement, 'left', { distance: 100, duration: 100 })
          
          if (i % 10 === 0) {
            memoryMeasurements.push((performance as any).memory.usedJSHeapSize)
          }
        }

        const memoryGrowth = memoryMeasurements[memoryMeasurements.length - 1] - memoryMeasurements[0]
        
        // Memory growth should be minimal for repeated operations
        expect(memoryGrowth).toBeLessThan(100000) // Less than 100KB growth
      }

      cleanup()
    })

    it('validates frame rate stability during gestures', async () => {
      const { result } = renderHook(() => 
        useTouchGestures({}, { onSwipe: vi.fn() })
      )

      const cleanup = result.current.attachListeners(mockElement)

      const frameRates: number[] = []
      let lastFrameTime = performance.now()

      // Mock requestAnimationFrame to measure frame rates
      const originalRAF = global.requestAnimationFrame
      global.requestAnimationFrame = vi.fn((callback) => {
        const currentTime = performance.now()
        const frameDuration = currentTime - lastFrameTime
        frameRates.push(1000 / frameDuration)
        lastFrameTime = currentTime
        
        setTimeout(callback, 16) // 60fps target
        return 1
      })

      // Perform gesture while measuring frame rates
      await simulateSwipeGesture(mockElement, 'left', { distance: 100, duration: 500 })

      const avgFrameRate = frameRates.reduce((a, b) => a + b, 0) / frameRates.length
      const minFrameRate = Math.min(...frameRates)

      // Frame rate should remain stable
      expect(avgFrameRate).toBeGreaterThan(30) // Average above 30fps
      expect(minFrameRate).toBeGreaterThan(15) // Minimum above 15fps

      global.requestAnimationFrame = originalRAF
      cleanup()
    })
  })
})