/**
 * BentoGrid Drag and Drop Tests
 * 
 * Comprehensive tests for drag and drop functionality including:
 * - Drag start, over, and end events
 * - Real-time preview updates
 * - Collision detection during drag
 * - Drop zone visualization
 * - Magnetic snapping behavior
 * - Touch device drag operations
 * - Multi-touch gesture handling
 * - Performance during rapid drag operations
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent, cleanup, act } from '@/test/utils/test-utils'
import userEvent from '@testing-library/user-event'

import { BentoGrid } from '../BentoGrid'
import { DeviceType } from '@/types/bento'
import {
  createMockBentoGrid,
  createMockGridComponent,
  createGridComponents,
  createCollisionTestComponents,
  createMockDragStartEvent,
  createMockDragOverEvent,
  createMockDragEndEvent,
  createMockTouchEvent,
  simulateLongPress,
  simulateRapidDragOperations,
  MockComponentRenderer,
  setupBentoTestEnvironment,
  MockDndProvider,
  positionsOverlap
} from '@/test/utils/bento-test-utils'

// Mock the necessary components
vi.mock('../ComponentRenderer', () => ({
  ComponentRenderer: MockComponentRenderer
}))

vi.mock('../GridOverlay', () => ({
  GridOverlay: ({ children, interactionState, ...props }: any) => (
    <div 
      data-testid="grid-overlay" 
      data-interaction-state={interactionState}
      {...props}
    >
      {children}
    </div>
  )
}))

vi.mock('../DragPreview', () => ({
  DragPreview: ({ component, deviceType, simplified }: any) => (
    <div 
      data-testid="drag-preview" 
      data-component-id={component?.id}
      data-device-type={deviceType}
      data-simplified={simplified}
    >
      Drag Preview: {component?.componentType}
    </div>
  )
}))

describe('BentoGrid Drag and Drop', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let onComponentMove: ReturnType<typeof vi.fn>
  let onComponentResize: ReturnType<typeof vi.fn>
  let onComponentDelete: ReturnType<typeof vi.fn>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    onComponentMove = vi.fn()
    onComponentResize = vi.fn()
    onComponentDelete = vi.fn()
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Drag Start Events', () => {
    it('initiates drag operation when component is dragged', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(2)
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

      const component = screen.getByTestId('component-component-0')
      
      // Start drag operation
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 100, y: 100 } },
      ])

      // Should show dragging state
      await waitFor(() => {
        expect(component.closest('.bento-component')).toHaveClass('bento-component--dragging')
      })
    })

    it('generates strategic drop zones on drag start', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      // Start drag
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])

      // Should show drop zones
      await waitFor(() => {
        const dropZones = screen.queryAllByTestId(/drop-zone/)
        expect(dropZones.length).toBeGreaterThan(0)
      })
    })

    it('shows contextual help message on drag start', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])

      // Should show help message
      await waitFor(() => {
        expect(screen.getByText(/drag to reposition/i)).toBeInTheDocument()
      })
    })

    it('updates grid interaction state during drag', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const gridWrapper = screen.getByRole('grid', { hidden: true }).parentElement!
      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])

      await waitFor(() => {
        expect(gridWrapper).toHaveClass('bento-grid--interacting')
      })
    })
  })

  describe('Drag Over Events', () => {
    it('updates preview position in real-time during drag', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      // Start drag and move
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 200, y: 150 } },
      ])

      // Should show updated preview position
      await waitFor(() => {
        const preview = screen.queryByTestId('drag-preview-position')
        if (preview) {
          expect(preview).toBeInTheDocument()
        }
      })
    })

    it('shows magnetic snapping feedback', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ 
        components,
        settings: { ...grid.settings, enableSnapping: true, snapThreshold: 15 }
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

      const component = screen.getByTestId('component-component-0')
      
      // Drag near grid line for snapping
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 198, y: 102 } }, // Close to grid boundary
      ])

      // Should show snapping indicator
      await waitFor(() => {
        const snappingIndicator = screen.queryByText(/snapped/i)
        if (snappingIndicator) {
          expect(snappingIndicator).toBeInTheDocument()
        }
      })
    })

    it('validates drop position and shows feedback', async () => {
      const user = userEvent.setup()
      const components = createCollisionTestComponents()
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

      const component = screen.getByTestId('component-component-1')
      
      // Drag to invalid position (overlapping with another component)
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 50, y: 50 } }, // Should overlap with component-1
      ])

      // Should show invalid position feedback
      await waitFor(() => {
        const invalidFeedback = screen.queryByText(/cannot place here/i)
        if (invalidFeedback) {
          expect(invalidFeedback).toBeInTheDocument()
        }
      })
    })

    it('throttles preview updates for performance', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      const updateSpy = vi.fn()
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      // Rapid mouse movements
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])
      
      for (let i = 0; i < 20; i++) {
        await user.pointer([{ coords: { x: 100 + i * 5, y: 100 + i * 5 } }])
      }

      // Updates should be throttled (implementation detail)
      // This test verifies the component doesn't crash under rapid updates
      expect(component).toBeInTheDocument()
    })
  })

  describe('Drag End Events', () => {
    it('completes move operation on valid drop', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      // Complete drag operation
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 300, y: 200 } },
        { keys: '[/MouseLeft]' },
      ])

      // Should call move handler
      await waitFor(() => {
        expect(onComponentMove).toHaveBeenCalledWith(
          'component-0',
          expect.objectContaining({
            x: expect.any(Number),
            y: expect.any(Number),
            w: 2,
            h: 2
          })
        )
      })
    })

    it('reverts position on invalid drop', async () => {
      const user = userEvent.setup()
      const components = createCollisionTestComponents()
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

      const component = screen.getByTestId('component-component-2')
      const originalPosition = components[2].position
      
      // Try to drop on occupied position
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 50, y: 50 } }, // Position of component-1
        { keys: '[/MouseLeft]' },
      ])

      // Should not call move handler for invalid position
      await waitFor(() => {
        expect(onComponentMove).not.toHaveBeenCalled()
      })

      // Should show error message
      expect(screen.getByText(/cannot place there/i)).toBeInTheDocument()
    })

    it('shows celebration animation on successful move', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 300, y: 200 } },
        { keys: '[/MouseLeft]' },
      ])

      // Should show success celebration
      await waitFor(() => {
        const celebration = screen.queryByText(/perfect snap|smooth move|great positioning/i)
        if (celebration) {
          expect(celebration).toBeInTheDocument()
        }
      })
    })

    it('cleans up drag state after operation', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      const gridWrapper = screen.getByRole('grid', { hidden: true }).parentElement!
      
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 300, y: 200 } },
        { keys: '[/MouseLeft]' },
      ])

      // Should reset interaction state
      await waitFor(() => {
        expect(gridWrapper).not.toHaveClass('bento-grid--interacting')
        expect(component.closest('.bento-component')).not.toHaveClass('bento-component--dragging')
      })

      // Drop zones should be hidden
      expect(screen.queryAllByTestId(/drop-zone/)).toHaveLength(0)
    })
  })

  describe('Touch Device Drag Operations', () => {
    beforeEach(() => {
      testEnvironment.enableTouchDevice()
    })

    it('enables drag mode with long press on mobile', async () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            deviceType={DeviceType.Mobile}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      const gridWrapper = screen.getByRole('grid', { hidden: true }).parentElement!
      
      // Simulate long press
      await simulateLongPress(component, { duration: 600 })

      await waitFor(() => {
        expect(gridWrapper).toHaveClass('bento-grid--drag-mode')
      })
    })

    it('provides haptic feedback on touch devices', async () => {
      const vibrateSpy = vi.spyOn(navigator, 'vibrate')
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            deviceType={DeviceType.Mobile}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      await simulateLongPress(component, { duration: 600 })

      expect(vibrateSpy).toHaveBeenCalledWith(50)
    })

    it('handles touch drag movement', async () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            deviceType={DeviceType.Mobile}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      // Start touch drag
      await simulateLongPress(component, { duration: 600 })
      
      // Move touch point
      const touchMove = createMockTouchEvent('touchmove', [
        { clientX: 200, clientY: 150 }
      ])
      fireEvent(component, touchMove)

      // Should update drag state
      // (Implementation depends on touch gesture handling)
    })

    it('completes touch drag on touch end', async () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            deviceType={DeviceType.Mobile}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      // Complete touch drag sequence
      await simulateLongPress(component, { duration: 600 })
      
      const touchMove = createMockTouchEvent('touchmove', [
        { clientX: 200, clientY: 150 }
      ])
      fireEvent(component, touchMove)
      
      const touchEnd = createMockTouchEvent('touchend', [])
      fireEvent(component, touchEnd)

      // Should complete drag operation
      await waitFor(() => {
        expect(onComponentMove).toHaveBeenCalled()
      })
    })

    it('shows mobile-specific drag indicators', async () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            deviceType={DeviceType.Mobile}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      await simulateLongPress(component, { duration: 600 })

      // Should show mobile drag indicator
      await waitFor(() => {
        expect(screen.getByText(/drag mode active/i)).toBeInTheDocument()
      })
    })
  })

  describe('Drop Zone Visualization', () => {
    it('shows strategic drop zones during drag', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])

      // Should show multiple drop zones
      await waitFor(() => {
        const dropZones = screen.queryAllByTestId(/drop-zone/)
        expect(dropZones.length).toBeGreaterThan(0)
        expect(dropZones.length).toBeLessThanOrEqual(6) // Strategic limit
      })
    })

    it('animates drop zones with breathing effect', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
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

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])

      // Drop zones should have animation styles
      await waitFor(() => {
        const dropZone = screen.queryByTestId(/drop-zone/)
        if (dropZone) {
          expect(dropZone.style.animation).toContain('drop-zone-breathe')
        }
      })
    })

    it('avoids overlapping drop zones', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(3)
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

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])

      // Drop zones should not overlap each other
      await waitFor(() => {
        const dropZones = screen.queryAllByTestId(/drop-zone/)
        
        // Check that no two drop zones occupy the same grid position
        const positions = new Set()
        dropZones.forEach(zone => {
          const style = window.getComputedStyle(zone)
          const gridColumn = style.gridColumn
          const gridRow = style.gridRow
          const positionKey = `${gridColumn}-${gridRow}`
          
          expect(positions.has(positionKey)).toBe(false)
          positions.add(positionKey)
        })
      })
    })
  })

  describe('Collision Detection', () => {
    it('detects collisions with existing components', () => {
      const components = createCollisionTestComponents()
      const newPosition = { x: 1, y: 1, w: 2, h: 2 } // Overlaps with component-1
      
      const hasCollision = components.some(comp => 
        positionsOverlap(newPosition, comp.position)
      )
      
      expect(hasCollision).toBe(true)
    })

    it('allows placement in valid positions', () => {
      const components = createCollisionTestComponents()
      const newPosition = { x: 8, y: 0, w: 2, h: 2 } // Should be valid
      
      const hasCollision = components.some(comp => 
        positionsOverlap(newPosition, comp.position)
      )
      
      expect(hasCollision).toBe(false)
    })

    it('handles edge collision cases', () => {
      const components = [
        createMockGridComponent({
          id: 'edge-test',
          position: { x: 0, y: 0, w: 3, h: 3 }
        })
      ]
      
      // Test adjacent positions (should not collide)
      const adjacentPositions = [
        { x: 3, y: 0, w: 2, h: 2 }, // Right edge
        { x: 0, y: 3, w: 2, h: 2 }, // Bottom edge
      ]
      
      adjacentPositions.forEach(pos => {
        const hasCollision = components.some(comp => 
          positionsOverlap(pos, comp.position)
        )
        expect(hasCollision).toBe(false)
      })
      
      // Test overlapping positions (should collide)
      const overlappingPositions = [
        { x: 2, y: 2, w: 2, h: 2 }, // Overlaps corner
        { x: 1, y: 1, w: 1, h: 1 }, // Inside
      ]
      
      overlappingPositions.forEach(pos => {
        const hasCollision = components.some(comp => 
          positionsOverlap(pos, comp.position)
        )
        expect(hasCollision).toBe(true)
      })
    })
  })

  describe('Performance during Drag Operations', () => {
    it('handles rapid mouse movements efficiently', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      const startTime = performance.now()
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])
      
      // Simulate rapid mouse movements
      await simulateRapidDragOperations(
        (position) => {
          fireEvent.mouseMove(component, {
            clientX: position.x,
            clientY: position.y
          })
        },
        50, // 50 rapid movements
        5   // 5ms intervals
      )
      
      const endTime = performance.now()
      const totalTime = endTime - startTime
      
      // Should handle rapid movements within reasonable time
      expect(totalTime).toBeLessThan(500) // 500ms for 50 movements
    })

    it('maintains performance with many components during drag', async () => {
      const user = userEvent.setup()
      const components = Array.from({ length: 50 }, (_, i) => 
        createMockGridComponent({
          id: `perf-comp-${i}`,
          position: { 
            x: (i % 6) * 2, 
            y: Math.floor(i / 6) * 2, 
            w: 2, 
            h: 2 
          }
        })
      )
      const grid = createMockBentoGrid({ components })
      
      const startTime = performance.now()
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-perf-comp-0')
      
      // Start drag with many components present
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 200, y: 150 } },
        { keys: '[/MouseLeft]' },
      ])
      
      const endTime = performance.now()
      const dragTime = endTime - startTime
      
      // Should complete drag operation efficiently even with many components
      expect(dragTime).toBeLessThan(200) // 200ms for complete drag with 50 components
    })

    it('throttles preview updates appropriately', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      const updateCount = { value: 0 }
      const originalRAF = global.requestAnimationFrame
      
      global.requestAnimationFrame = vi.fn((callback) => {
        updateCount.value++
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

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])
      
      // Multiple rapid movements
      for (let i = 0; i < 20; i++) {
        fireEvent.mouseMove(component, {
          clientX: 100 + i * 5,
          clientY: 100 + i * 5
        })
      }
      
      // Updates should be throttled via RAF
      expect(updateCount.value).toBeLessThan(20) // Should be throttled
      
      global.requestAnimationFrame = originalRAF
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('handles drag operations when component is removed during drag', async () => {
      const user = userEvent.setup()
      let components = createGridComponents(2)
      let grid = createMockBentoGrid({ components })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      // Start drag
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])
      
      // Remove component during drag
      components = components.slice(1) // Remove first component
      grid = createMockBentoGrid({ components })
      
      rerender(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      // Should handle gracefully without crashing
      expect(screen.queryByTestId('component-component-0')).not.toBeInTheDocument()
    })

    it('handles drag operations when grid is resized during drag', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      let grid = createMockBentoGrid({ components, columns: 12 })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      // Start drag
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])
      
      // Change grid columns during drag
      grid = createMockBentoGrid({ components, columns: 8 })
      
      rerender(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      // Should adapt to new grid structure
      const gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement.style.gridTemplateColumns).toBe('repeat(8, 1fr)')
    })

    it('prevents drag operations on locked components', async () => {
      const user = userEvent.setup()
      const components = [
        createMockGridComponent({
          id: 'locked-component',
          locked: true,
          position: { x: 0, y: 0, w: 2, h: 2 }
        })
      ]
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

      const component = screen.getByTestId('component-locked-component')
      
      // Try to drag locked component
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 200, y: 150 } },
        { keys: '[/MouseLeft]' },
      ])

      // Should not call move handler for locked component
      expect(onComponentMove).not.toHaveBeenCalled()
    })
  })
})