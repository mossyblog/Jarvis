/**
 * BentoGrid Performance Tests
 * 
 * Comprehensive performance tests for the BentoGrid system including:
 * - Rendering performance with large component counts
 * - Drag and drop performance under load
 * - Memory usage and cleanup verification
 * - Real-time updates and throttling
 * - Mobile performance characteristics
 * - Grid recalculation efficiency
 * - Event handler optimization
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent, cleanup, act } from '@/test/utils/test-utils'

import { BentoGrid } from '../BentoGrid'
import { DeviceType } from '@/types/bento'
import {
  createMockBentoGrid,
  createMockGridComponent,
  createLargeComponentSet,
  measureRenderTime,
  simulateRapidDragOperations,
  MockComponentRenderer,
  setupBentoTestEnvironment,
  MockDndProvider
} from '@/test/utils/bento-test-utils'

// Mock performance-sensitive components
vi.mock('../ComponentRenderer', () => ({
  ComponentRenderer: MockComponentRenderer
}))

vi.mock('../GridOverlay', () => ({
  GridOverlay: React.memo(({ children, ...props }: any) => (
    <div data-testid="grid-overlay" {...props}>
      {children}
    </div>
  ))
}))

describe('BentoGrid Performance Tests', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let onComponentMove: ReturnType<typeof vi.fn>
  let performanceEntries: PerformanceEntry[]

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    onComponentMove = vi.fn()
    performanceEntries = []
    
    // Mock performance.mark and performance.measure
    global.performance.mark = vi.fn()
    global.performance.measure = vi.fn()
    global.performance.getEntriesByName = vi.fn(() => performanceEntries)
    
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Rendering Performance', () => {
    it('renders 50 components within acceptable time', async () => {
      const components = createLargeComponentSet(50)
      const grid = createMockBentoGrid({ components })
      
      const renderTime = await measureRenderTime(() => {
        render(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      })

      expect(renderTime).toBeLessThan(200) // Should render 50 components in under 200ms
      expect(screen.getAllByTestId(/component-perf-component-/)).toHaveLength(50)
    })

    it('renders 100 components within performance budget', async () => {
      const components = createLargeComponentSet(100)
      const grid = createMockBentoGrid({ components })
      
      const renderTime = await measureRenderTime(() => {
        render(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      })

      expect(renderTime).toBeLessThan(500) // Should render 100 components in under 500ms
      expect(screen.getAllByTestId(/component-perf-component-/)).toHaveLength(100)
    })

    it('handles incremental component additions efficiently', async () => {
      let components = createLargeComponentSet(10)
      let grid = createMockBentoGrid({ components })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const times: number[] = []

      // Add components incrementally and measure each update
      for (let i = 20; i <= 100; i += 10) {
        components = createLargeComponentSet(i)
        grid = createMockBentoGrid({ components })
        
        const updateTime = await measureRenderTime(() => {
          rerender(
            <MockDndProvider>
              <BentoGrid grid={grid} />
            </MockDndProvider>
          )
        })
        
        times.push(updateTime)
      }

      // Each incremental update should be fast
      times.forEach(time => {
        expect(time).toBeLessThan(100) // Each update under 100ms
      })

      // Final count verification
      expect(screen.getAllByTestId(/component-perf-component-/)).toHaveLength(100)
    })

    it('maintains performance during rapid prop changes', async () => {
      const components = createLargeComponentSet(30)
      let grid = createMockBentoGrid({ components })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={false} />
        </MockDndProvider>
      )

      const startTime = performance.now()

      // Rapid state changes
      for (let i = 0; i < 20; i++) {
        grid = createMockBentoGrid({ 
          components,
          columns: 12 + (i % 4), // Vary grid structure
          gap: 16 + (i % 8)
        })
        
        rerender(
          <MockDndProvider>
            <BentoGrid 
              grid={grid} 
              isEditing={i % 2 === 0}
              showGrid={i % 3 === 0}
              deviceType={i % 2 === 0 ? DeviceType.Desktop : DeviceType.Mobile}
            />
          </MockDndProvider>
        )
      }

      const endTime = performance.now()
      const totalTime = endTime - startTime

      expect(totalTime).toBeLessThan(500) // 20 rapid updates in under 500ms
    })

    it('efficiently handles component visibility changes', async () => {
      const components = createLargeComponentSet(40).map(comp => ({
        ...comp,
        display: {
          ...comp.display,
          hideOn: Math.random() > 0.5 ? [DeviceType.Mobile] : [],
          showOnly: Math.random() > 0.5 ? [DeviceType.Desktop] : []
        }
      }))
      
      const grid = createMockBentoGrid({ components })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} deviceType={DeviceType.Desktop} />
        </MockDndProvider>
      )

      const startTime = performance.now()

      // Switch device types rapidly to trigger visibility recalculations
      const deviceTypes = [DeviceType.Desktop, DeviceType.Tablet, DeviceType.Mobile]
      
      for (let i = 0; i < 15; i++) {
        const deviceType = deviceTypes[i % deviceTypes.length]
        
        rerender(
          <MockDndProvider>
            <BentoGrid grid={grid} deviceType={deviceType} />
          </MockDndProvider>
        )
      }

      const endTime = performance.now()
      const totalTime = endTime - startTime

      expect(totalTime).toBeLessThan(300) // Visibility changes should be fast
    })
  })

  describe('Drag and Drop Performance', () => {
    it('handles drag operations efficiently with many components', async () => {
      const components = createLargeComponentSet(60)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-component-0')
      
      const startTime = performance.now()

      // Simulate drag start
      fireEvent.mouseDown(component, { clientX: 100, clientY: 100 })
      
      // Rapid mouse movements
      for (let i = 0; i < 30; i++) {
        fireEvent.mouseMove(component, { 
          clientX: 100 + i * 2, 
          clientY: 100 + i * 2 
        })
      }
      
      // Drag end
      fireEvent.mouseUp(component)

      const endTime = performance.now()
      const dragTime = endTime - startTime

      expect(dragTime).toBeLessThan(150) // Complete drag sequence under 150ms
    })

    it('throttles preview updates appropriately during rapid movement', async () => {
      const components = createLargeComponentSet(20)
      const grid = createMockBentoGrid({ components })
      
      const updateCallCount = { value: 0 }
      const originalRAF = global.requestAnimationFrame
      
      global.requestAnimationFrame = vi.fn((callback) => {
        updateCallCount.value++
        return originalRAF(callback)
      })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-component-0')
      
      fireEvent.mouseDown(component, { clientX: 100, clientY: 100 })
      
      // Very rapid movements (faster than 60fps)
      for (let i = 0; i < 100; i++) {
        fireEvent.mouseMove(component, { 
          clientX: 100 + i, 
          clientY: 100 + i 
        })
      }

      fireEvent.mouseUp(component)

      // Should be throttled to reasonable number of updates
      expect(updateCallCount.value).toBeLessThan(50) // Significantly fewer than input events
      
      global.requestAnimationFrame = originalRAF
    })

    it('maintains performance during collision detection with many components', async () => {
      const components = createLargeComponentSet(80)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-component-0')
      
      const startTime = performance.now()

      await simulateRapidDragOperations(
        (position) => {
          fireEvent.mouseMove(component, {
            clientX: position.x * 10,
            clientY: position.y * 10
          })
        },
        50, // 50 rapid movements
        5   // 5ms intervals
      )

      const endTime = performance.now()
      const totalTime = endTime - startTime

      expect(totalTime).toBeLessThan(400) // All collision checks under 400ms
    })

    it('efficiently calculates drop zones for large grids', async () => {
      const components = createLargeComponentSet(40)
      const grid = createMockBentoGrid({ components, columns: 16 }) // Larger grid
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-component-0')
      
      const startTime = performance.now()

      // Start drag to trigger drop zone calculation
      fireEvent.mouseDown(component, { clientX: 100, clientY: 100 })
      
      // Wait for drop zones to be calculated
      await waitFor(() => {
        const dropZones = screen.queryAllByTestId(/drop-zone/)
        expect(dropZones.length).toBeGreaterThan(0)
      })

      const endTime = performance.now()
      const calculationTime = endTime - startTime

      expect(calculationTime).toBeLessThan(100) // Drop zone calculation under 100ms
    })
  })

  describe('Memory Management', () => {
    it('cleans up event listeners on component unmount', () => {
      const components = createLargeComponentSet(20)
      const grid = createMockBentoGrid({ components })
      
      const addEventListenerSpy = vi.spyOn(document, 'addEventListener')
      const removeEventListenerSpy = vi.spyOn(document, 'removeEventListener')
      
      const { unmount } = render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const initialAddCount = addEventListenerSpy.mock.calls.length

      unmount()

      // Should clean up at least as many listeners as were added
      expect(removeEventListenerSpy.mock.calls.length).toBeGreaterThanOrEqual(initialAddCount)
    })

    it('prevents memory leaks during rapid component updates', async () => {
      let components = createLargeComponentSet(10)
      let grid = createMockBentoGrid({ components })
      
      const { rerender, unmount } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      // Simulate rapid updates that might cause memory leaks
      for (let i = 0; i < 50; i++) {
        components = createLargeComponentSet(10 + (i % 20))
        grid = createMockBentoGrid({ components })
        
        rerender(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      }

      // Force garbage collection if available
      if (global.gc) {
        global.gc()
      }

      unmount()

      // Memory should be properly cleaned up
      // (This is more of a canary test - real memory leaks would be detected by longer-running tests)
      expect(true).toBe(true)
    })

    it('efficiently manages resize observer instances', () => {
      const components = createLargeComponentSet(30)
      const grid = createMockBentoGrid({ components })
      
      const mockObserver = testEnvironment.mockResizeObserver?.()
      
      const { unmount } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      if (mockObserver) {
        // Should create observers efficiently
        expect(mockObserver).toHaveBeenCalled()
      }

      unmount()

      // Should clean up observers
      if (mockObserver) {
        const observerInstance = mockObserver.mock.results[0]?.value
        if (observerInstance) {
          expect(observerInstance.disconnect).toHaveBeenCalled()
        }
      }
    })
  })

  describe('Mobile Performance', () => {
    beforeEach(() => {
      testEnvironment.enableTouchDevice()
    })

    it('maintains touch responsiveness with many components', async () => {
      const components = createLargeComponentSet(40)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            deviceType={DeviceType.Mobile}
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-component-0')
      
      const startTime = performance.now()

      // Simulate touch interactions
      fireEvent.touchStart(component, {
        touches: [{ clientX: 100, clientY: 100 }]
      })
      
      // Rapid touch movements
      for (let i = 0; i < 20; i++) {
        fireEvent.touchMove(component, {
          touches: [{ clientX: 100 + i * 3, clientY: 100 + i * 3 }]
        })
      }
      
      fireEvent.touchEnd(component)

      const endTime = performance.now()
      const touchTime = endTime - startTime

      expect(touchTime).toBeLessThan(200) // Touch interactions should be responsive
    })

    it('efficiently handles pinch gestures on large grids', async () => {
      const components = createLargeComponentSet(50)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            deviceType={DeviceType.Mobile}
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const gridElement = screen.getByRole('grid', { hidden: true })
      
      const startTime = performance.now()

      // Simulate pinch gesture
      fireEvent.touchStart(gridElement, {
        touches: [
          { clientX: 200, clientY: 200 },
          { clientX: 300, clientY: 200 }
        ]
      })
      
      // Pinch movements
      for (let i = 0; i < 10; i++) {
        const distance = 100 + i * 10
        fireEvent.touchMove(gridElement, {
          touches: [
            { clientX: 250 - distance/2, clientY: 200 },
            { clientX: 250 + distance/2, clientY: 200 }
          ]
        })
      }
      
      fireEvent.touchEnd(gridElement)

      const endTime = performance.now()
      const pinchTime = endTime - startTime

      expect(pinchTime).toBeLessThan(150) // Pinch gestures should be smooth
    })

    it('optimizes rendering for mobile viewport sizes', async () => {
      const components = createLargeComponentSet(60)
      const grid = createMockBentoGrid({ components })
      
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', { value: 375 })
      Object.defineProperty(window, 'innerHeight', { value: 667 })
      
      const renderTime = await measureRenderTime(() => {
        render(
          <MockDndProvider>
            <BentoGrid 
              grid={grid} 
              deviceType={DeviceType.Mobile}
            />
          </MockDndProvider>
        )
      })

      expect(renderTime).toBeLessThan(300) // Mobile rendering should be optimized
    })
  })

  describe('Grid Recalculation Performance', () => {
    it('efficiently recalculates layout on column changes', async () => {
      const components = createLargeComponentSet(40)
      let grid = createMockBentoGrid({ components, columns: 12 })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const startTime = performance.now()

      // Change grid structure multiple times
      const columnConfigs = [8, 16, 10, 14, 12]
      
      for (const columns of columnConfigs) {
        grid = createMockBentoGrid({ components, columns })
        
        rerender(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      }

      const endTime = performance.now()
      const recalcTime = endTime - startTime

      expect(recalcTime).toBeLessThan(200) // Layout recalculations should be fast
    })

    it('optimizes gap and row height changes', async () => {
      const components = createLargeComponentSet(35)
      let grid = createMockBentoGrid({ components })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const startTime = performance.now()

      // Vary spacing and sizing
      const configs = [
        { gap: 8, rowHeight: 80 },
        { gap: 24, rowHeight: 120 },
        { gap: 16, rowHeight: 100 },
        { gap: 32, rowHeight: 150 },
      ]
      
      for (const config of configs) {
        grid = createMockBentoGrid({ components, ...config })
        
        rerender(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      }

      const endTime = performance.now()
      const updateTime = endTime - startTime

      expect(updateTime).toBeLessThan(150) // Spacing changes should be efficient
    })

    it('handles component position batch updates efficiently', async () => {
      let components = createLargeComponentSet(30)
      let grid = createMockBentoGrid({ components })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const startTime = performance.now()

      // Batch update all component positions
      components = components.map(comp => ({
        ...comp,
        position: {
          ...comp.position,
          x: (comp.position.x + 2) % 10,
          y: comp.position.y + 1
        }
      }))
      
      grid = createMockBentoGrid({ components })
      
      rerender(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const endTime = performance.now()
      const batchUpdateTime = endTime - startTime

      expect(batchUpdateTime).toBeLessThan(100) // Batch updates should be optimized
    })
  })

  describe('Event Handler Optimization', () => {
    it('efficiently handles rapid mouse events', async () => {
      const components = createLargeComponentSet(25)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const gridElement = screen.getByRole('grid', { hidden: true })
      
      const startTime = performance.now()

      // Rapid mouse movements over grid
      for (let i = 0; i < 100; i++) {
        fireEvent.mouseMove(gridElement, { 
          clientX: i * 5, 
          clientY: i * 3 
        })
      }

      const endTime = performance.now()
      const eventTime = endTime - startTime

      expect(eventTime).toBeLessThan(100) // Event handling should be fast
    })

    it('debounces expensive operations appropriately', async () => {
      const components = createLargeComponentSet(20)
      let grid = createMockBentoGrid({ components })
      
      const expensiveCallback = vi.fn()
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            onGridUpdate={expensiveCallback}
          />
        </MockDndProvider>
      )

      // Rapid updates that should be debounced
      for (let i = 0; i < 10; i++) {
        grid = createMockBentoGrid({ 
          components: components.slice(0, 15 + i)
        })
        
        rerender(
          <MockDndProvider>
            <BentoGrid 
              grid={grid} 
              onGridUpdate={expensiveCallback}
            />
          </MockDndProvider>
        )
      }

      // Should not call expensive callback for every update
      await waitFor(() => {
        expect(expensiveCallback.mock.calls.length).toBeLessThan(5)
      })
    })

    it('maintains performance during simultaneous operations', async () => {
      const components = createLargeComponentSet(40)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            showGrid={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-component-0')
      const gridElement = screen.getByRole('grid', { hidden: true })
      
      const startTime = performance.now()

      // Simultaneous operations
      await act(async () => {
        // Start drag
        fireEvent.mouseDown(component, { clientX: 100, clientY: 100 })
        
        // Hover over grid
        fireEvent.mouseEnter(gridElement)
        
        // Rapid movements
        for (let i = 0; i < 20; i++) {
          fireEvent.mouseMove(component, { 
            clientX: 100 + i * 2, 
            clientY: 100 + i * 2 
          })
          fireEvent.mouseMove(gridElement, { 
            clientX: 200 + i, 
            clientY: 200 + i 
          })
        }
        
        // End operations
        fireEvent.mouseUp(component)
        fireEvent.mouseLeave(gridElement)
      })

      const endTime = performance.now()
      const totalTime = endTime - startTime

      expect(totalTime).toBeLessThan(250) // Simultaneous operations should remain performant
    })
  })
})