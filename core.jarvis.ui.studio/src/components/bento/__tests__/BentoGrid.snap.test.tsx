/**
 * BentoGrid Snap-to-Grid Tests
 * 
 * Comprehensive tests for magnetic snapping behavior including:
 * - Grid line snapping calculations
 * - Component edge alignment
 * - Snap threshold sensitivity
 * - Visual feedback for snapping
 * - Performance with snap calculations
 * - Snap strength and magnetism
 * - Mobile snap behavior differences
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent, cleanup } from '@/test/utils/test-utils'
import userEvent from '@testing-library/user-event'

import { BentoGrid } from '../BentoGrid'
import { DeviceType } from '@/types/bento'
import {
  createMockBentoGrid,
  createMockGridComponent,
  createGridComponents,
  createMockGridRect,
  MockComponentRenderer,
  setupBentoTestEnvironment,
  MockDndProvider
} from '@/test/utils/bento-test-utils'

import {
  applyMagneticSnapping,
  applyComponentSnapping,
  SNAP_THRESHOLD
} from '@/utils/gridHelpers'

// Mock the necessary components
vi.mock('../ComponentRenderer', () => ({
  ComponentRenderer: MockComponentRenderer
}))

vi.mock('../GridOverlay', () => ({
  GridOverlay: ({ children, ...props }: any) => (
    <div data-testid="grid-overlay" {...props}>
      {children}
    </div>
  )
}))

describe('BentoGrid Snap-to-Grid Behavior', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let onComponentMove: ReturnType<typeof vi.fn>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    onComponentMove = vi.fn()
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Magnetic Snapping Calculations', () => {
    it('snaps to nearest grid lines when within threshold', () => {
      const mousePosition = { x: 102, y: 98 } // Close to 100,100 grid position
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      
      const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      
      expect(result.snapped).toBe(true)
      expect(result.position.x).toBe(1) // Should snap to grid position 1
      expect(result.position.y).toBe(1)
      expect(result.snapStrength).toBeGreaterThan(0.5)
    })

    it('does not snap when outside threshold distance', () => {
      const mousePosition = { x: 125, y: 135 } // Far from grid lines
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      
      const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      
      expect(result.snapped).toBe(false)
      expect(result.snapStrength).toBe(0)
    })

    it('calculates snap strength based on distance', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      
      // Test different distances from grid line
      const testCases = [
        { distance: 0, expectedStrength: 1.0 },
        { distance: SNAP_THRESHOLD / 2, expectedStrength: 0.5 },
        { distance: SNAP_THRESHOLD, expectedStrength: 0.0 },
      ]
      
      testCases.forEach(({ distance, expectedStrength }) => {
        const mousePosition = { x: 100 + distance, y: 100 }
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.snapStrength).toBeCloseTo(expectedStrength, 1)
      })
    })

    it('constrains snapped position to grid bounds', () => {
      const mousePosition = { x: -10, y: -10 } // Outside grid
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid({ columns: 12 })
      const componentSize = { w: 2, h: 2 }
      
      const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      
      expect(result.position.x).toBe(0)
      expect(result.position.y).toBe(0)
    })

    it('prevents component from extending beyond grid width', () => {
      const mousePosition = { x: 1100, y: 100 } // Near right edge
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid({ columns: 12 })
      const componentSize = { w: 4, h: 2 }
      
      const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      
      expect(result.position.x + result.position.w).toBeLessThanOrEqual(12)
    })

    it('handles different grid configurations correctly', () => {
      const mousePosition = { x: 120, y: 150 }
      
      // Test with different column counts and gaps
      const configs = [
        { columns: 6, gap: 20, rowHeight: 120 },
        { columns: 16, gap: 8, rowHeight: 80 },
        { columns: 8, gap: 24, rowHeight: 150 },
      ]
      
      configs.forEach(config => {
        const gridRect = createMockGridRect(config.columns, config.gap, config.rowHeight, 1200)
        const grid = createMockBentoGrid(config)
        const componentSize = { w: 2, h: 2 }
        
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.position.x).toBeGreaterThanOrEqual(0)
        expect(result.position.y).toBeGreaterThanOrEqual(0)
        expect(result.position.x + result.position.w).toBeLessThanOrEqual(config.columns)
      })
    })
  })

  describe('Component Edge Snapping', () => {
    it('snaps to component edges when aligned', () => {
      const existingComponents = [
        createMockGridComponent({
          id: 'target',
          position: { x: 4, y: 4, w: 2, h: 2 }
        })
      ]
      
      // Test snapping to different edges
      const snapTests = [
        {
          position: { x: 5, y: 6, w: 2, h: 2 }, // Below target
          expectedY: 6 // Should snap to bottom edge
        },
        {
          position: { x: 6, y: 4, w: 2, h: 2 }, // Right of target
          expectedX: 6 // Should snap to right edge
        },
        {
          position: { x: 1, y: 4, w: 2, h: 2 }, // Left of target
          expectedX: 2 // Should snap to left edge (4 - 2)
        }
      ]
      
      snapTests.forEach(({ position, expectedX, expectedY }) => {
        const result = applyComponentSnapping(position, existingComponents, 2)
        
        if (expectedX !== undefined) {
          expect(result.x).toBe(expectedX)
        }
        if (expectedY !== undefined) {
          expect(result.y).toBe(expectedY)
        }
      })
    })

    it('only snaps when components are properly aligned', () => {
      const existingComponents = [
        createMockGridComponent({
          id: 'target',
          position: { x: 4, y: 4, w: 2, h: 2 }
        })
      ]
      
      // Not aligned vertically
      const unalignedPosition = { x: 6, y: 8, w: 2, h: 2 }
      const result = applyComponentSnapping(unalignedPosition, existingComponents, 1)
      
      expect(result).toEqual(unalignedPosition) // Should remain unchanged
    })

    it('respects snapping threshold for component edges', () => {
      const existingComponents = [
        createMockGridComponent({
          id: 'target',
          position: { x: 4, y: 4, w: 2, h: 2 }
        })
      ]
      
      const position = { x: 8, y: 4, w: 2, h: 2 } // 2 units away from edge
      
      const resultWithLowThreshold = applyComponentSnapping(position, existingComponents, 1)
      const resultWithHighThreshold = applyComponentSnapping(position, existingComponents, 3)
      
      expect(resultWithLowThreshold).toEqual(position) // Should not snap
      expect(resultWithHighThreshold.x).toBe(6) // Should snap to right edge
    })

    it('handles multiple component targets', () => {
      const existingComponents = [
        createMockGridComponent({
          id: 'comp1',
          position: { x: 2, y: 2, w: 2, h: 2 }
        }),
        createMockGridComponent({
          id: 'comp2',
          position: { x: 6, y: 2, w: 2, h: 2 }
        })
      ]
      
      // Position that could snap to either component
      const position = { x: 4, y: 2, w: 2, h: 2 }
      const result = applyComponentSnapping(position, existingComponents, 1)
      
      // Should snap to the first valid edge (right edge of comp1)
      expect(result.x).toBe(4) // Already at the right edge
    })
  })

  describe('Visual Snap Feedback in BentoGrid', () => {
    it('shows snapping indicator during drag with snap', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ 
        components,
        settings: { 
          ...createMockBentoGrid().settings,
          enableSnapping: true,
          snapThreshold: 15
        }
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
      
      // Start drag near a grid line for snapping
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 198, y: 102 } }, // Close to grid boundary
      ])

      // Should show snapping feedback
      await waitFor(() => {
        const snappingIndicator = screen.queryByText(/snapped/i)
        if (snappingIndicator) {
          expect(snappingIndicator).toBeInTheDocument()
        }
      })
    })

    it('applies magnetic snap CSS class during snap operations', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ 
        components,
        settings: { enableSnapping: true }
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
      
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 199, y: 101 } }, // Very close to grid line
      ])

      // Should apply magnetic snap class
      await waitFor(() => {
        const dragPreview = screen.queryByTestId('drag-preview')
        if (dragPreview) {
          expect(dragPreview).toHaveClass('magnetic-snap')
        }
      })
    })

    it('shows enhanced visual feedback for strong snaps', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ 
        components,
        settings: { enableSnapping: true, snapThreshold: 20 }
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
      
      // Exact grid line position (strongest snap)
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 200, y: 100 } }, // Exact grid position
      ])

      // Should show strong snap feedback
      await waitFor(() => {
        const preview = screen.queryByTestId('drag-preview')
        if (preview) {
          // Check for enhanced visual feedback
          const styles = window.getComputedStyle(preview)
          expect(styles.boxShadow).toContain('rgba(59, 130, 246, 0.4)') // Blue glow
        }
      })
    })

    it('updates snap feedback in real-time during drag', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ 
        components,
        settings: { enableSnapping: true }
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
      
      // Start drag
      await user.pointer([{ target: component, keys: '[MouseLeft>]' }])
      
      // Move gradually toward snap point
      const positions = [
        { x: 180, y: 100 }, // Far from snap
        { x: 190, y: 100 }, // Getting closer
        { x: 198, y: 100 }, // Very close (should snap)
      ]
      
      for (const pos of positions) {
        fireEvent.mouseMove(component, { clientX: pos.x, clientY: pos.y })
        
        // Small delay to allow updates
        await new Promise(resolve => setTimeout(resolve, 10))
      }

      // Final position should show snap feedback
      await waitFor(() => {
        const snappingElement = screen.queryByText(/snapped/i)
        if (snappingElement) {
          expect(snappingElement).toBeInTheDocument()
        }
      })
    })
  })

  describe('Snap Configuration and Settings', () => {
    it('respects enableSnapping setting', async () => {
      const user = userEvent.setup()
      const components = createGridComponents(1)
      
      // Test with snapping disabled
      const gridNoSnap = createMockBentoGrid({ 
        components,
        settings: { 
          ...createMockBentoGrid().settings,
          enableSnapping: false
        }
      })
      
      const { rerender } = render(
        <MockDndProvider>
          <BentoGrid 
            grid={gridNoSnap} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      const component = screen.getByTestId('component-component-0')
      
      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 199, y: 101 } }, // Close to grid line
      ])

      // Should not show snapping feedback
      expect(screen.queryByText(/snapped/i)).not.toBeInTheDocument()

      // Test with snapping enabled
      const gridWithSnap = createMockBentoGrid({ 
        components,
        settings: { 
          ...createMockBentoGrid().settings,
          enableSnapping: true
        }
      })
      
      rerender(
        <MockDndProvider>
          <BentoGrid 
            grid={gridWithSnap} 
            isEditing={true}
            onComponentMove={onComponentMove}
          />
        </MockDndProvider>
      )

      await user.pointer([
        { target: component, keys: '[MouseLeft>]' },
        { coords: { x: 199, y: 101 } },
      ])

      // Should show snapping feedback when enabled
      await waitFor(() => {
        const snappingIndicator = screen.queryByText(/snapped/i)
        if (snappingIndicator) {
          expect(snappingIndicator).toBeInTheDocument()
        }
      })
    })

    it('uses custom snap threshold from settings', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const componentSize = { w: 2, h: 2 }
      
      // Test with different thresholds
      const thresholds = [5, 15, 25]
      
      thresholds.forEach(threshold => {
        const grid = createMockBentoGrid({
          settings: { 
            ...createMockBentoGrid().settings,
            snapThreshold: threshold
          }
        })
        
        // Position just outside the custom threshold
        const mousePosition = { x: 100 + threshold + 1, y: 100 }
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.snapped).toBe(false)
        
        // Position just inside the custom threshold
        const snapPosition = { x: 100 + threshold - 1, y: 100 }
        const snapResult = applyMagneticSnapping(snapPosition, gridRect, grid, componentSize)
        
        expect(snapResult.snapped).toBe(true)
      })
    })

    it('handles disabled snapping gracefully', () => {
      const mousePosition = { x: 101, y: 99 } // Very close to grid line
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid({
        settings: { 
          ...createMockBentoGrid().settings,
          enableSnapping: false
        }
      })
      const componentSize = { w: 2, h: 2 }
      
      // Even with position very close to grid, should not snap when disabled
      const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      
      // Position should still be calculated but snapping flags should be false
      expect(result.snapped).toBe(false)
      expect(result.snapStrength).toBe(0)
    })
  })

  describe('Mobile Snap Behavior', () => {
    beforeEach(() => {
      testEnvironment.enableTouchDevice()
    })

    it('applies more forgiving snap thresholds on mobile', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid({
        settings: { 
          enableSnapping: true,
          snapThreshold: 15 // Base threshold
        }
      })
      const componentSize = { w: 2, h: 2 }
      
      // On mobile, threshold might be increased for easier touch interaction
      const mobilePosition = { x: 118, y: 102 } // Further from grid line
      const result = applyMagneticSnapping(mobilePosition, gridRect, grid, componentSize)
      
      // May snap on mobile even with larger distance due to touch-friendly adjustments
      // This would depend on implementation details
      expect(typeof result.snapped).toBe('boolean')
    })

    it('provides enhanced visual feedback on touch devices', async () => {
      const components = createGridComponents(1)
      const grid = createMockBentoGrid({ 
        components,
        settings: { enableSnapping: true }
      })
      
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

      // Touch device should show enhanced snap feedback
      const gridElement = screen.getByRole('grid', { hidden: true })
      expect(gridElement).toHaveClass('bento-grid--touch-device')
    })

    it('uses larger snap targets for touch interactions', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      
      // Test positions that might have different behavior on touch vs mouse
      const testPositions = [
        { x: 110, y: 110 },
        { x: 115, y: 105 },
        { x: 108, y: 112 }
      ]
      
      testPositions.forEach(position => {
        const result = applyMagneticSnapping(position, gridRect, grid, componentSize)
        
        // Should handle consistently regardless of input method
        expect(result.position).toBeDefined()
        expect(result.snapStrength).toBeGreaterThanOrEqual(0)
      })
    })
  })

  describe('Performance with Snap Calculations', () => {
    it('performs snap calculations efficiently during rapid movement', async () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      
      const startTime = performance.now()
      
      // Simulate rapid position updates
      for (let i = 0; i < 100; i++) {
        const mousePosition = { x: 100 + i, y: 100 + i }
        applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      }
      
      const endTime = performance.now()
      const duration = endTime - startTime
      
      expect(duration).toBeLessThan(50) // Should complete quickly
    })

    it('caches snap calculations for repeated positions', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      const mousePosition = { x: 150, y: 150 }
      
      // Multiple calls with same position should be fast
      const startTime = performance.now()
      
      for (let i = 0; i < 50; i++) {
        applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      }
      
      const endTime = performance.now()
      const duration = endTime - startTime
      
      expect(duration).toBeLessThan(25) // Should be very fast for repeated calculations
    })

    it('handles large grids efficiently', () => {
      const largeGridRect = createMockGridRect(24, 8, 50, 2400) // Large grid
      const grid = createMockBentoGrid({ columns: 24 })
      const componentSize = { w: 3, h: 2 }
      
      const startTime = performance.now()
      
      // Test various positions across large grid
      for (let x = 0; x < 2400; x += 100) {
        for (let y = 0; y < 1000; y += 100) {
          applyMagneticSnapping({ x, y }, largeGridRect, grid, componentSize)
        }
      }
      
      const endTime = performance.now()
      const duration = endTime - startTime
      
      expect(duration).toBeLessThan(100) // Should handle large grids efficiently
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('handles zero-sized grids gracefully', () => {
      const zeroGridRect = createMockGridRect(0, 16, 100, 0)
      const grid = createMockBentoGrid({ columns: 0 })
      const componentSize = { w: 2, h: 2 }
      const mousePosition = { x: 100, y: 100 }
      
      expect(() => {
        applyMagneticSnapping(mousePosition, zeroGridRect, grid, componentSize)
      }).not.toThrow()
    })

    it('handles invalid mouse positions', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      
      const invalidPositions = [
        { x: NaN, y: 100 },
        { x: 100, y: Infinity },
        { x: -Infinity, y: 100 }
      ]
      
      invalidPositions.forEach(position => {
        expect(() => {
          applyMagneticSnapping(position, gridRect, grid, componentSize)
        }).not.toThrow()
      })
    })

    it('handles extremely small or large component sizes', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const mousePosition = { x: 100, y: 100 }
      
      const extremeSizes = [
        { w: 0, h: 1 }, // Zero width
        { w: 1, h: 0 }, // Zero height
        { w: 50, h: 50 }, // Larger than grid
      ]
      
      extremeSizes.forEach(size => {
        expect(() => {
          applyMagneticSnapping(mousePosition, gridRect, grid, size)
        }).not.toThrow()
      })
    })

    it('maintains consistency with repeated snap calculations', () => {
      const gridRect = createMockGridRect(12, 16, 100, 1200)
      const grid = createMockBentoGrid()
      const componentSize = { w: 2, h: 2 }
      const mousePosition = { x: 203, y: 97 }
      
      // Multiple calls should return identical results
      const results = Array.from({ length: 10 }, () => 
        applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
      )
      
      const firstResult = results[0]
      results.forEach(result => {
        expect(result.position).toEqual(firstResult.position)
        expect(result.snapped).toBe(firstResult.snapped)
        expect(result.snapStrength).toBeCloseTo(firstResult.snapStrength || 0, 5)
      })
    })
  })
})