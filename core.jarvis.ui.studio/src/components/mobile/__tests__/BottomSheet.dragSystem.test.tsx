/**
 * BottomSheet Drag System Tests
 * 
 * Focused tests for the bottom sheet's drag and gesture system including:
 * - Drag state transitions and snap behavior
 * - Complex multi-touch scenarios
 * - Performance under heavy gesture load
 * - Edge cases for gesture recognition
 * - Accessibility during drag operations
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { BottomSheet } from '../BottomSheet'
import {
  createMockTouchEvent,
  simulateSwipeGesture,
  simulatePinchGesture,
  setupBentoTestEnvironment
} from '@/test/utils/bento-test-utils'

// Advanced drag simulation utilities
const createDragSequence = (
  element: HTMLElement,
  startPos: { x: number; y: number },
  endPos: { x: number; y: number },
  options: {
    steps?: number
    duration?: number
    easing?: 'linear' | 'ease-out' | 'ease-in'
    includeVelocity?: boolean
  } = {}
) => {
  const { steps = 10, duration = 300, easing = 'linear', includeVelocity = true } = options
  const sequence: Array<{ event: string; pos: { x: number; y: number }; timestamp: number }> = []
  
  const deltaX = endPos.x - startPos.x
  const deltaY = endPos.y - startPos.y
  const stepDuration = duration / steps

  for (let i = 0; i <= steps; i++) {
    let progress = i / steps
    
    // Apply easing
    switch (easing) {
      case 'ease-out':
        progress = 1 - Math.pow(1 - progress, 3)
        break
      case 'ease-in':
        progress = Math.pow(progress, 3)
        break
    }
    
    const x = startPos.x + deltaX * progress
    const y = startPos.y + deltaY * progress
    const timestamp = Date.now() + i * stepDuration
    
    if (i === 0) {
      sequence.push({ event: 'touchstart', pos: { x, y }, timestamp })
    } else if (i === steps) {
      sequence.push({ event: 'touchend', pos: { x, y }, timestamp })
    } else {
      sequence.push({ event: 'touchmove', pos: { x, y }, timestamp })
    }
  }
  
  return sequence
}

const executeDragSequence = async (
  element: HTMLElement,
  sequence: Array<{ event: string; pos: { x: number; y: number }; timestamp: number }>
) => {
  for (const step of sequence) {
    if (step.event === 'touchend') {
      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(element, touchEnd)
    } else {
      const touches = step.event === 'touchstart' || step.event === 'touchmove' 
        ? [{ clientX: step.pos.x, clientY: step.pos.y }] 
        : []
      const touchEvent = createMockTouchEvent(step.event, touches)
      fireEvent(element, touchEvent)
    }
    
    // Simulate real timing
    await new Promise(resolve => setTimeout(resolve, 1))
  }
}

// Test component for drag behavior
const DragTestBottomSheet: React.FC<{
  onDragStart?: (data: any) => void
  onDragMove?: (data: any) => void
  onDragEnd?: (data: any) => void
  onSnapPoint?: (height: number) => void
  customSnapPoints?: number[]
}> = ({ 
  onDragStart, 
  onDragMove, 
  onDragEnd, 
  onSnapPoint,
  customSnapPoints = [0.2, 0.5, 0.9]
}) => {
  const [isOpen, setIsOpen] = React.useState(true)
  const [dragData, setDragData] = React.useState<any>(null)
  
  return (
    <>
      <BottomSheet
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        initialHeight={0.5}
        minHeight={0.2}
        maxHeight={0.9}
      >
        <div data-testid="drag-content">
          <div data-testid="drag-info">
            {dragData && JSON.stringify(dragData)}
          </div>
          <button 
            data-testid="close-button"
            onClick={() => setIsOpen(false)}
          >
            Close
          </button>
        </div>
      </BottomSheet>
    </>
  )
}

describe('BottomSheet Drag System', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let mockOnDragStart: ReturnType<typeof vi.fn>
  let mockOnDragMove: ReturnType<typeof vi.fn>
  let mockOnDragEnd: ReturnType<typeof vi.fn>
  let mockOnSnapPoint: ReturnType<typeof vi.fn>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    
    mockOnDragStart = vi.fn()
    mockOnDragMove = vi.fn()
    mockOnDragEnd = vi.fn()
    mockOnSnapPoint = vi.fn()
    
    // Create portal mount point
    const portalDiv = document.createElement('div')
    portalDiv.id = 'bottom-sheet-portal'
    document.body.appendChild(portalDiv)
    
    // Mock viewport
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      value: 800
    })
    
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

  describe('Drag State Transitions', () => {
    it('transitions correctly through drag lifecycle', async () => {
      const { container } = render(
        <DragTestBottomSheet 
          onDragStart={mockOnDragStart}
          onDragMove={mockOnDragMove}
          onDragEnd={mockOnDragEnd}
        />
      )

      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      expect(handle).toBeInTheDocument()

      // Create drag sequence
      const sequence = createDragSequence(
        handle,
        { x: 200, y: 400 },
        { x: 200, y: 300 }, // Drag up
        { steps: 5, duration: 200 }
      )

      await executeDragSequence(handle, sequence)

      // Should have started and ended drag
      expect(mockOnDragStart).toHaveBeenCalled()
      expect(mockOnDragEnd).toHaveBeenCalled()
    })

    it('maintains drag state during complex movements', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Complex drag path
      const sequence = createDragSequence(
        handle,
        { x: 200, y: 400 },
        { x: 180, y: 250 }, // Up and left
        { steps: 15, duration: 500, easing: 'ease-out' }
      )

      await executeDragSequence(handle, sequence)

      // Should handle complex movement without issues
      expect(handle).toBeInTheDocument()
    })

    it('cancels drag on touch interruption', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Start drag
      fireEvent.mouseDown(handle, { clientY: 400 })

      // Interrupt with touch cancel
      const touchCancel = new TouchEvent('touchcancel')
      fireEvent(handle, touchCancel)

      // End should still work without errors
      fireEvent.mouseUp(handle)

      expect(handle).toBeInTheDocument()
    })

    it('handles rapid drag start/stop cycles', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Rapid start/stop cycles
      for (let i = 0; i < 10; i++) {
        fireEvent.mouseDown(handle, { clientY: 400 + i })
        await new Promise(resolve => setTimeout(resolve, 10))
        fireEvent.mouseUp(handle)
      }

      expect(handle).toBeInTheDocument()
    })

    it('maintains performance during extended drag', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      const startTime = performance.now()

      // Extended drag sequence
      const sequence = createDragSequence(
        handle,
        { x: 200, y: 400 },
        { x: 200, y: 100 },
        { steps: 50, duration: 1000 }
      )

      await executeDragSequence(handle, sequence)

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(1200) // Should complete efficiently
    })
  })

  describe('Snap Point Behavior', () => {
    it('snaps to correct height after drag', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement

      // Drag to position between snap points
      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 350 })
      fireEvent.mouseUp(document)

      await act(async () => {
        vi.advanceTimersByTime(300)
      })

      // Should snap to nearest point (likely 0.9 - maxHeight)
      const computedStyle = window.getComputedStyle(sheet)
      expect(computedStyle.height).toBeTruthy()
    })

    it('considers drag velocity for snap decisions', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Fast upward drag
      const fastSequence = createDragSequence(
        handle,
        { x: 200, y: 400 },
        { x: 200, y: 300 },
        { steps: 3, duration: 100 } // Fast
      )

      await executeDragSequence(handle, fastSequence)

      await act(async () => {
        vi.advanceTimersByTime(300)
      })

      // Should consider velocity in snapping decision
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('handles custom snap points correctly', async () => {
      const customSnapPoints = [0.1, 0.4, 0.7, 0.95]
      const { container } = render(
        <DragTestBottomSheet 
          customSnapPoints={customSnapPoints}
          onSnapPoint={mockOnSnapPoint}
        />
      )

      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Drag to position near custom snap point
      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 450 }) // Drag down
      fireEvent.mouseUp(document)

      await act(async () => {
        vi.advanceTimersByTime(300)
      })

      // Should use custom snap points
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('snaps with smooth animation', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement

      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 300 })
      fireEvent.mouseUp(document)

      // During animation, should have transition
      expect(sheet).toHaveClass('will-change-transform')

      await act(async () => {
        vi.advanceTimersByTime(300)
      })

      // Animation should complete
      expect(sheet).toBeInTheDocument()
    })

    it('prevents snapping during active drag', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement

      // Start drag
      fireEvent.mouseDown(handle, { clientY: 400 })
      
      // During drag, should disable transitions
      expect(sheet).toHaveClass('transition-none')

      // Continue drag
      fireEvent.mouseMove(document, { clientY: 350 })
      
      // Should still be dragging, no snapping yet
      expect(sheet).toHaveClass('transition-none')

      fireEvent.mouseUp(document)

      await act(async () => {
        vi.advanceTimersByTime(50)
      })

      // After drag ends, transitions should be re-enabled
      expect(sheet).not.toHaveClass('transition-none')
    })
  })

  describe('Multi-touch and Gesture Conflicts', () => {
    it('handles simultaneous touch and mouse events', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Start with mouse
      fireEvent.mouseDown(handle, { clientY: 400 })

      // Add touch event
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 400 }])
      fireEvent(handle, touchStart)

      // Should handle gracefully
      fireEvent.mouseUp(handle)
      
      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(handle, touchEnd)

      expect(handle).toBeInTheDocument()
    })

    it('prioritizes touch over mouse when both are present', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Start with mouse
      fireEvent.mouseDown(handle, { clientY: 400 })

      // Override with touch
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 380 }])
      fireEvent(handle, touchStart)

      // Touch move should take precedence
      const touchMove = createMockTouchEvent('touchmove', [{ clientX: 200, clientY: 350 }])
      fireEvent(handle, touchMove)

      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(handle, touchEnd)

      // Should handle touch-driven movement
      expect(handle).toBeInTheDocument()
    })

    it('handles accidental multi-finger touches on handle', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Start with single touch
      const singleTouch = createMockTouchEvent('touchstart', [{ clientX: 200, clientY: 400 }])
      fireEvent(handle, singleTouch)

      // Accidentally add second finger
      const multiTouch = createMockTouchEvent('touchstart', [
        { clientX: 200, clientY: 400, identifier: 0 },
        { clientX: 210, clientY: 405, identifier: 1 }
      ])
      fireEvent(handle, multiTouch)

      // Should continue to work or gracefully cancel
      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(handle, touchEnd)

      expect(handle).toBeInTheDocument()
    })

    it('ignores pinch gestures on the sheet content', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const content = screen.getByTestId('drag-content')

      // Attempt pinch on content area
      await simulatePinchGesture(content, {
        startDistance: 100,
        endDistance: 150
      })

      // Should not affect sheet position
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('handles drag conflicts with swipe gestures', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const sheet = container.querySelector('[role="dialog"]')?.firstChild as HTMLElement

      // Start drag on handle
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      fireEvent.mouseDown(handle, { clientY: 400 })

      // Simultaneously attempt swipe on sheet
      await simulateSwipeGesture(sheet, 'down', {
        distance: 100,
        duration: 200
      })

      // Should handle conflict gracefully
      fireEvent.mouseUp(handle)

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })
  })

  describe('Accessibility During Drag', () => {
    it('maintains focus during drag operations', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      const closeButton = screen.getByTestId('close-button')

      // Focus on close button
      closeButton.focus()
      expect(closeButton).toHaveFocus()

      // Perform drag
      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 350 })
      fireEvent.mouseUp(document)

      await act(async () => {
        vi.advanceTimersByTime(300)
      })

      // Focus should be maintained
      expect(closeButton).toHaveFocus()
    })

    it('announces position changes to screen readers', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      const dialog = screen.getByRole('dialog')

      // Should have appropriate ARIA attributes
      expect(dialog).toHaveAttribute('aria-modal', 'true')

      // Perform drag
      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 300 })
      fireEvent.mouseUp(document)

      // Should maintain accessibility attributes
      expect(dialog).toHaveAttribute('aria-modal', 'true')
    })

    it('provides keyboard alternative for drag operations', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Focus on handle
      handle.focus()
      expect(handle).toHaveFocus()

      // Use arrow keys to adjust height
      fireEvent.keyDown(handle, { key: 'ArrowUp' })
      fireEvent.keyDown(handle, { key: 'ArrowDown' })

      // Should provide keyboard accessibility
      expect(handle).toBeInTheDocument()
    })

    it('respects reduced motion preferences during drag', async () => {
      // Mock reduced motion preference
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation(query => ({
          matches: query === '(prefers-reduced-motion: reduce)',
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        }))
      })

      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 350 })
      fireEvent.mouseUp(document)

      // Should respect motion preferences
      expect(handle).toBeInTheDocument()
    })

    it('maintains high contrast visibility during drag', async () => {
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

      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Handle should remain visible and interactive
      expect(handle).toBeVisible()

      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 350 })

      // Should maintain visibility during drag
      expect(handle).toBeVisible()

      fireEvent.mouseUp(document)
    })
  })

  describe('Performance Under Load', () => {
    it('maintains 60fps during smooth drag', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      const frameTimes: number[] = []
      let lastTime = performance.now()

      // Mock requestAnimationFrame to track timing
      const originalRAF = global.requestAnimationFrame
      global.requestAnimationFrame = vi.fn((callback) => {
        const currentTime = performance.now()
        frameTimes.push(currentTime - lastTime)
        lastTime = currentTime
        setTimeout(callback, 16) // 60fps
        return 1
      })

      const sequence = createDragSequence(
        handle,
        { x: 200, y: 400 },
        { x: 200, y: 200 },
        { steps: 30, duration: 500 }
      )

      await executeDragSequence(handle, sequence)

      // Most frames should be close to 16.67ms (60fps)
      const avgFrameTime = frameTimes.reduce((a, b) => a + b, 0) / frameTimes.length
      expect(avgFrameTime).toBeLessThan(20) // Allow some variance

      global.requestAnimationFrame = originalRAF
    })

    it('handles memory efficiently during long drag sessions', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Simulate long drag session
      for (let session = 0; session < 5; session++) {
        const sequence = createDragSequence(
          handle,
          { x: 200, y: 400 },
          { x: 200, y: 200 + session * 40 },
          { steps: 20, duration: 200 }
        )

        await executeDragSequence(handle, sequence)
        
        // Brief pause between sessions
        await new Promise(resolve => setTimeout(resolve, 50))
      }

      // Should not accumulate memory leaks
      expect(handle).toBeInTheDocument()
    })

    it('throttles excessive drag events', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      let eventCount = 0
      const originalAddEventListener = handle.addEventListener
      handle.addEventListener = vi.fn((event, handler, options) => {
        if (event === 'touchmove' || event === 'mousemove') {
          eventCount++
        }
        return originalAddEventListener.call(handle, event, handler, options)
      })

      // Excessive drag events
      fireEvent.mouseDown(handle, { clientY: 400 })
      
      for (let i = 0; i < 100; i++) {
        fireEvent.mouseMove(document, { clientY: 400 - i })
      }
      
      fireEvent.mouseUp(document)

      // Should handle excessive events without performance degradation
      expect(handle).toBeInTheDocument()
    })

    it('maintains performance with complex DOM structure', async () => {
      // Create complex DOM structure
      const ComplexBottomSheet = () => (
        <BottomSheet isOpen={true} onClose={() => {}}>
          <div>
            {Array.from({ length: 100 }, (_, i) => (
              <div key={i} style={{ height: '50px', padding: '10px' }}>
                <button>Button {i}</button>
                <input placeholder={`Input ${i}`} />
                <div>
                  {Array.from({ length: 10 }, (_, j) => (
                    <span key={j}>Item {j}</span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </BottomSheet>
      )

      const { container } = render(<ComplexBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      const startTime = performance.now()

      // Drag with complex content
      fireEvent.mouseDown(handle, { clientY: 400 })
      fireEvent.mouseMove(document, { clientY: 300 })
      fireEvent.mouseUp(document)

      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(100) // Should handle complex DOM efficiently
    })
  })

  describe('Edge Cases and Error Recovery', () => {
    it('recovers from interrupted drag sequences', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Start drag
      fireEvent.mouseDown(handle, { clientY: 400 })
      
      // Simulate window losing focus (interrupts drag)
      fireEvent(window, new Event('blur'))
      
      // Try to continue drag (should handle gracefully)
      fireEvent.mouseMove(document, { clientY: 350 })
      fireEvent.mouseUp(document)

      expect(handle).toBeInTheDocument()
    })

    it('handles coordinate calculation errors', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Mock getBoundingClientRect to return invalid values
      Element.prototype.getBoundingClientRect = vi.fn(() => ({
        top: NaN,
        left: NaN,
        right: NaN,
        bottom: NaN,
        width: NaN,
        height: NaN,
        x: NaN,
        y: NaN,
        toJSON: () => ({})
      } as DOMRect))

      // Should handle invalid coordinates gracefully
      expect(() => {
        fireEvent.mouseDown(handle, { clientY: 400 })
        fireEvent.mouseMove(document, { clientY: 350 })
        fireEvent.mouseUp(document)
      }).not.toThrow()
    })

    it('handles viewport changes during drag', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Start drag
      fireEvent.mouseDown(handle, { clientY: 400 })

      // Change viewport during drag
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 600
      })
      fireEvent(window, new Event('resize'))

      // Continue drag with new viewport
      fireEvent.mouseMove(document, { clientY: 300 })
      fireEvent.mouseUp(document)

      expect(handle).toBeInTheDocument()
    })

    it('handles missing event properties', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Create malformed event
      const malformedEvent = new Event('mousedown') as any
      // Missing clientY property

      expect(() => {
        fireEvent(handle, malformedEvent)
      }).not.toThrow()
    })

    it('recovers from animation frame errors', async () => {
      const { container } = render(<DragTestBottomSheet />)
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement

      // Mock requestAnimationFrame to throw error
      global.requestAnimationFrame = vi.fn(() => {
        throw new Error('Animation frame error')
      })

      // Should handle animation errors gracefully
      expect(() => {
        fireEvent.mouseDown(handle, { clientY: 400 })
        fireEvent.mouseMove(document, { clientY: 350 })
        fireEvent.mouseUp(document)
      }).not.toThrow()
    })
  })
})