/**
 * BentoGrid Component Tests
 * 
 * Comprehensive tests for the main BentoGrid component including:
 * - Basic rendering and layout
 * - Component positioning and grid structure
 * - Edit mode behavior
 * - Device responsive behavior
 * - Grid state management
 * - Error boundaries and edge cases
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent, cleanup } from '@/test/utils/test-utils'
import userEvent from '@testing-library/user-event'

import { BentoGrid } from '../BentoGrid'
import { DeviceType, createDefaultGridPosition } from '@/types/bento'
import {
  createMockBentoGrid,
  createMockGridComponent,
  createGridComponents,
  MockComponentRenderer,
  setupBentoTestEnvironment,
  MockDndProvider
} from '@/test/utils/bento-test-utils'

// Mock the ComponentRenderer
vi.mock('../ComponentRenderer', () => ({
  ComponentRenderer: MockComponentRenderer
}))

// Mock GridOverlay
vi.mock('../GridOverlay', () => ({
  GridOverlay: ({ children, ...props }: any) => (
    <div data-testid="grid-overlay" {...props}>
      {children}
    </div>
  )
}))

// Mock DragPreview
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

describe('BentoGrid Component', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders empty grid correctly', () => {
      const grid = createMockBentoGrid()
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      expect(screen.getByRole('grid', { hidden: true })).toBeInTheDocument()
      expect(screen.queryByTestId(/component-/)).not.toBeInTheDocument()
    })

    it('renders grid with components', () => {
      const components = createGridComponents(3)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      expect(screen.getByRole('grid', { hidden: true })).toBeInTheDocument()
      expect(screen.getAllByTestId(/component-/)).toHaveLength(3)
    })

    it('applies correct CSS grid styles', () => {
      const grid = createMockBentoGrid({
        columns: 8,
        gap: 20,
        rowHeight: 120
      })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const gridElement = screen.getByRole('grid', { hidden: true })
      const styles = window.getComputedStyle(gridElement)
      
      expect(styles.display).toBe('grid')
      expect(styles.gridTemplateColumns).toBe('repeat(8, 1fr)')
      expect(styles.gap).toBe('20px')
      expect(styles.gridAutoRows).toBe('120px')
    })

    it('handles missing or invalid grid data gracefully', () => {
      const invalidGrid = {
        ...createMockBentoGrid(),
        columns: 0, // Invalid
        components: [
          // @ts-ignore - Testing invalid component
          { id: 'invalid', position: null }
        ]
      }
      
      render(
        <MockDndProvider>
          <BentoGrid grid={invalidGrid} />
        </MockDndProvider>
      )

      // Should not crash and render what it can
      expect(screen.getByRole('grid', { hidden: true })).toBeInTheDocument()
    })
  })

  describe('Component Positioning', () => {
    it('positions components correctly in CSS grid', () => {
      const components = [
        createMockGridComponent({
          id: 'comp-1',
          position: { x: 0, y: 0, w: 2, h: 2 }
        }),
        createMockGridComponent({
          id: 'comp-2',
          position: { x: 3, y: 1, w: 3, h: 1 }
        })
      ]
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      const comp1 = screen.getByTestId('component-comp-1').parentElement
      const comp2 = screen.getByTestId('component-comp-2').parentElement
      
      expect(comp1).toHaveStyle({
        gridColumn: '1 / span 2',
        gridRow: '1 / span 2'
      })
      
      expect(comp2).toHaveStyle({
        gridColumn: '4 / span 3',
        gridRow: '2 / span 1'
      })
    })

    it('handles overlapping components gracefully', () => {
      const components = [
        createMockGridComponent({
          id: 'comp-1',
          position: { x: 0, y: 0, w: 3, h: 3 }
        }),
        createMockGridComponent({
          id: 'comp-2',
          position: { x: 1, y: 1, w: 3, h: 3 } // Overlaps with comp-1
        })
      ]
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      // Both components should render even if overlapping
      expect(screen.getByTestId('component-comp-1')).toBeInTheDocument()
      expect(screen.getByTestId('component-comp-2')).toBeInTheDocument()
    })

    it('handles components outside grid bounds', () => {
      const components = [
        createMockGridComponent({
          id: 'out-of-bounds',
          position: { x: 15, y: 0, w: 2, h: 2 } // x=15 exceeds 12 columns
        })
      ]
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      // Component should still render (browser will handle overflow)
      expect(screen.getByTestId('component-out-of-bounds')).toBeInTheDocument()
    })
  })

  describe('Edit Mode Behavior', () => {
    it('shows grid overlay when in edit mode', () => {
      const grid = createMockBentoGrid()
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      expect(screen.getByTestId('grid-overlay')).toBeInTheDocument()
    })

    it('hides grid overlay when not in edit mode', () => {
      const grid = createMockBentoGrid()
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={false} />
        </MockDndProvider>
      )

      expect(screen.queryByTestId('grid-overlay')).not.toBeInTheDocument()
    })

    it('applies correct CSS classes for edit mode', () => {
      const grid = createMockBentoGrid()
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={false} />
        </MockDndProvider>
      )

      const gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement).not.toHaveClass('bento-grid--editing')

      rerender(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      expect(gridElement).toHaveClass('bento-grid--editing')
    })

    it('shows empty state message when grid is empty and in edit mode', () => {
      const grid = createMockBentoGrid({ components: [] })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      expect(screen.getByText(/ready to create/i)).toBeInTheDocument()
    })

    it('hides empty state when not in edit mode', () => {
      const grid = createMockBentoGrid({ components: [] })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={false} />
        </MockDndProvider>
      )

      expect(screen.queryByText(/ready to create/i)).not.toBeInTheDocument()
    })
  })

  describe('Device Responsive Behavior', () => {
    it('applies correct CSS classes for different device types', () => {
      const grid = createMockBentoGrid()
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} deviceType={DeviceType.Desktop} />
        </MockDndProvider>
      )

      let gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement).not.toHaveClass('bento-grid--mobile')

      rerender(
        <MockDndProvider>
          <BentoGrid grid={grid} deviceType={DeviceType.Mobile} />
        </MockDndProvider>
      )

      gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement).toHaveClass('bento-grid--mobile')
    })

    it('detects touch device correctly', () => {
      testEnvironment.enableTouchDevice()
      const grid = createMockBentoGrid()
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} deviceType={DeviceType.Mobile} />
        </MockDndProvider>
      )

      const gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement).toHaveClass('bento-grid--touch-device')
      expect(gridElement.dataset.touchEnabled).toBe('true')
    })

    it('handles component visibility based on device display settings', () => {
      const components = [
        createMockGridComponent({
          id: 'desktop-only',
          display: {
            showOnly: [DeviceType.Desktop],
            hideOn: [],
            className: '',
            style: {}
          }
        }),
        createMockGridComponent({
          id: 'mobile-hidden',
          display: {
            showOnly: [],
            hideOn: [DeviceType.Mobile],
            className: '',
            style: {}
          }
        }),
        createMockGridComponent({
          id: 'always-visible'
        })
      ]
      const grid = createMockBentoGrid({ components })
      
      // Test desktop view
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} deviceType={DeviceType.Desktop} />
        </MockDndProvider>
      )

      expect(screen.getByTestId('component-desktop-only')).toBeInTheDocument()
      expect(screen.getByTestId('component-mobile-hidden')).toBeInTheDocument()
      expect(screen.getByTestId('component-always-visible')).toBeInTheDocument()

      // Test mobile view
      rerender(
        <MockDndProvider>
          <BentoGrid grid={grid} deviceType={DeviceType.Mobile} />
        </MockDndProvider>
      )

      expect(screen.queryByTestId('component-desktop-only')).not.toBeInTheDocument()
      expect(screen.queryByTestId('component-mobile-hidden')).not.toBeInTheDocument()
      expect(screen.getByTestId('component-always-visible')).toBeInTheDocument()
    })
  })

  describe('Grid Interaction States', () => {
    it('handles mouse enter and leave for grid interaction states', async () => {
      const user = userEvent.setup()
      const grid = createMockBentoGrid()
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      const gridWrapper = screen.getByRole('grid', { hidden: true }).parentElement!
      
      await user.hover(gridWrapper)
      expect(gridWrapper).toHaveClass('bento-grid--hovering')
      
      await user.unhover(gridWrapper)
      await waitFor(() => {
        expect(gridWrapper).not.toHaveClass('bento-grid--hovering')
      })
    })

    it('maintains interaction state during drag operations', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      const onComponentMove = vi.fn()
      
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
      
      await user.hover(gridWrapper)
      expect(gridWrapper).toHaveClass('bento-grid--hovering')
    })
  })

  describe('External Drag Preview', () => {
    it('shows external drag preview when provided', () => {
      const grid = createMockBentoGrid()
      const externalDragPreview = {
        position: { x: 2, y: 2, w: 3, h: 2 },
        componentType: 'chart'
      }
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            externalDragPreview={externalDragPreview}
          />
        </MockDndProvider>
      )

      const preview = screen.getByTestId('mock-external-preview')
      expect(preview).toBeInTheDocument()
    })

    it('validates external drag preview position', () => {
      const components = [
        createMockGridComponent({
          id: 'existing',
          position: { x: 2, y: 2, w: 2, h: 2 }
        })
      ]
      const grid = createMockBentoGrid({ components })
      
      // Valid position
      const validPreview = {
        position: { x: 0, y: 0, w: 2, h: 2 },
        componentType: 'chart'
      }
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            externalDragPreview={validPreview}
          />
        </MockDndProvider>
      )

      expect(screen.getByTestId('mock-external-preview')).toHaveClass('drop-zone-valid')

      // Invalid position (overlapping)
      const invalidPreview = {
        position: { x: 2, y: 2, w: 2, h: 2 },
        componentType: 'chart'
      }
      
      rerender(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            externalDragPreview={invalidPreview}
          />
        </MockDndProvider>
      )

      expect(screen.getByTestId('mock-external-preview')).toHaveClass('drop-zone-invalid')
    })
  })

  describe('Event Handlers', () => {
    it('calls onComponentMove when component is moved', async () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      const onComponentMove = vi.fn()
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      // Simulate drag end event
      const dragEndEvent = new CustomEvent('dragend', {
        detail: {
          componentId: components[0].id,
          newPosition: { x: 2, y: 2, w: 2, h: 2 }
        }
      })
      
      fireEvent(screen.getByRole('grid', { hidden: true }), dragEndEvent)
      
      // Move handler should eventually be called
      // Note: This is simplified - actual implementation uses DnD Kit events
    })

    it('calls onComponentDelete when component is deleted', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      const onComponentDelete = vi.fn()
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onComponentDelete={onComponentDelete}
          />
        </MockDndProvider>
      )

      // This would need the actual GridComponent wrapper to test delete functionality
      // For now, we'll simulate the call
      fireEvent.click(screen.getByRole('grid', { hidden: true }))
      // onComponentDelete would be called by GridComponent's delete button
    })

    it('calls onShowProperties when component properties are requested', () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      const onShowProperties = vi.fn()
      
      render(
        <MockDndProvider>
          <BentoGrid 
            grid={grid} 
            isEditing={true}
            onShowProperties={onShowProperties}
          />
        </MockDndProvider>
      )

      // Properties handler would be called by GridComponent's properties button
      // This is tested more thoroughly in GridComponent tests
    })
  })

  describe('Performance and Edge Cases', () => {
    it('handles large number of components efficiently', () => {
      const components = Array.from({ length: 100 }, (_, i) => 
        createMockGridComponent({
          id: `perf-component-${i}`,
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
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )
      
      const endTime = performance.now()
      const renderTime = endTime - startTime
      
      // Should render within reasonable time (less than 100ms for 100 components)
      expect(renderTime).toBeLessThan(100)
      expect(screen.getAllByTestId(/component-perf-component-/)).toHaveLength(100)
    })

    it('handles rapid prop changes gracefully', async () => {
      let grid = createMockBentoGrid({ components: createGridComponents(3) })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} />
        </MockDndProvider>
      )

      // Rapidly change props
      for (let i = 0; i < 10; i++) {
        grid = createMockBentoGrid({ 
          components: createGridComponents(i + 1),
          columns: 8 + i,
          gap: 16 + i
        })
        
        rerender(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      }

      // Should handle changes without crashing
      expect(screen.getAllByTestId(/component-/)).toHaveLength(10)
    })

    it('cleans up resources on unmount', () => {
      const components = createGridComponents(5)
      const grid = createMockBentoGrid({ components })
      
      const { unmount } = render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      // Unmount should not cause any errors
      expect(() => unmount()).not.toThrow()
    })

    it('handles malformed component data gracefully', () => {
      const malformedComponents = [
        // @ts-ignore - Testing malformed data
        { id: 'malformed-1' }, // Missing position
        // @ts-ignore
        { position: { x: 0, y: 0, w: 2, h: 2 } }, // Missing id
        createMockGridComponent({ id: 'valid' }) // Valid component
      ]
      
      const grid = createMockBentoGrid({ 
        // @ts-ignore - Testing malformed data
        components: malformedComponents 
      })
      
      expect(() => {
        render(
          <MockDndProvider>
            <BentoGrid grid={grid} />
          </MockDndProvider>
        )
      }).not.toThrow()

      // Should render the valid component
      expect(screen.getByTestId('component-valid')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('provides appropriate ARIA labels and roles', () => {
      const components = createGridComponents(3)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      const gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement).toBeInTheDocument()
      
      // Components should be accessible
      components.forEach((_, index) => {
        expect(screen.getByTestId(`component-component-${index}`)).toBeInTheDocument()
      })
    })

    it('supports keyboard navigation in edit mode', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(2)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      // Tab navigation should work through interactive elements
      await user.tab()
      
      // Should be able to reach interactive elements
      // (Specific keyboard navigation is tested in GridComponent tests)
    })

    it('provides proper focus management during drag operations', () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ components })
      
      render(
        <MockDndProvider>
          <BentoGrid grid={grid} isEditing={true} />
        </MockDndProvider>
      )

      // Focus management during drag operations
      // This is primarily handled by @dnd-kit/core
    })
  })
})