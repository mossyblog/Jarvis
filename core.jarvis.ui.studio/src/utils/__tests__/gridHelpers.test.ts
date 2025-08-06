/**
 * Grid Helper Functions Tests
 * 
 * Comprehensive tests for grid helper utilities including:
 * - Auto-placement algorithms
 * - Magnetic snapping calculations
 * - Collision detection
 * - Drop zone generation
 * - Position validation
 * - Performance optimization functions
 * - Context-aware help generation
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import {
  findBestPlacement,
  findOptimalPlacement,
  applyMagneticSnapping,
  applyComponentSnapping,
  generateDropZones,
  getRelevantDropZones,
  isValidPlacement,
  getInvalidReason,
  componentsOverlap,
  calculateGridHeight,
  getContextualHelp,
  getTooltipText,
  getErrorMessage,
  debounce,
  throttle,
  getOptimizedDropZones,
  generateStrategicDropZones,
  easeOutCubic,
  easeOutBack,
  calculateFeedbackIntensity,
  isDropZoneTooClose,
  SNAP_THRESHOLD,
  MAX_PLACEMENT_ATTEMPTS
} from '../gridHelpers'

import {
  createMockBentoGrid,
  createMockGridComponent,
  createGridComponents,
  createCollisionTestComponents,
  createMockGridRect
} from '@/test/utils/bento-test-utils'

describe('Grid Helper Functions', () => {
  describe('Auto-placement Algorithms', () => {
    describe('findBestPlacement', () => {
      it('finds placement in top-left corner when grid is empty', () => {
        const grid = createMockBentoGrid({ components: [] })
        const componentSize = { w: 2, h: 2 }
        
        const result = findBestPlacement(componentSize, grid)
        
        expect(result.success).toBe(true)
        expect(result.position).toEqual({ x: 0, y: 0, w: 2, h: 2 })
        expect(result.message).toContain('row 1, column 1')
      })

      it('finds next available position when top-left is occupied', () => {
        const components = [
          createMockGridComponent({
            id: 'existing',
            position: { x: 0, y: 0, w: 3, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const result = findBestPlacement(componentSize, grid)
        
        expect(result.success).toBe(true)
        expect(result.position?.x).toBeGreaterThanOrEqual(3) // Should be to the right
        expect(result.position?.y).toBe(0) // Should still be in first row
      })

      it('moves to next row when current row is full', () => {
        const components = Array.from({ length: 6 }, (_, i) => 
          createMockGridComponent({
            id: `comp-${i}`,
            position: { x: i * 2, y: 0, w: 2, h: 2 }
          })
        )
        const grid = createMockBentoGrid({ components, columns: 12 })
        const componentSize = { w: 2, h: 2 }
        
        const result = findBestPlacement(componentSize, grid)
        
        expect(result.success).toBe(true)
        expect(result.position?.y).toBe(2) // Should be in next available row
      })

      it('excludes specified component from collision detection', () => {
        const components = [
          createMockGridComponent({
            id: 'exclude-me',
            position: { x: 0, y: 0, w: 2, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const result = findBestPlacement(componentSize, grid, 'exclude-me')
        
        expect(result.success).toBe(true)
        expect(result.position).toEqual({ x: 0, y: 0, w: 2, h: 2 })
      })

      it('returns failure when no space is available', () => {
        // Fill grid completely
        const components = Array.from({ length: MAX_PLACEMENT_ATTEMPTS }, (_, i) => 
          createMockGridComponent({
            id: `comp-${i}`,
            position: { 
              x: (i % 6) * 2, 
              y: Math.floor(i / 6) * 2, 
              w: 2, 
              h: 2 
            }
          })
        )
        const grid = createMockBentoGrid({ components, columns: 12 })
        const componentSize = { w: 2, h: 2 }
        
        const result = findBestPlacement(componentSize, grid)
        
        expect(result.success).toBe(false)
        expect(result.position).toBeNull()
        expect(result.message).toContain('No available space')
      })

      it('handles edge case where component is larger than grid', () => {
        const grid = createMockBentoGrid({ columns: 8 })
        const componentSize = { w: 10, h: 2 } // Wider than grid
        
        const result = findBestPlacement(componentSize, grid)
        
        expect(result.success).toBe(false)
        expect(result.position).toBeNull()
      })
    })

    describe('findOptimalPlacement', () => {
      it('prefers positions with lower gap scores', () => {
        const components = [
          createMockGridComponent({
            id: 'anchor',
            position: { x: 2, y: 2, w: 2, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const result = findOptimalPlacement(componentSize, grid)
        
        expect(result.success).toBe(true)
        // Should prefer position adjacent to existing component or top-left
        const pos = result.position!
        expect(pos.x <= 4 && pos.y <= 4).toBe(true)
      })

      it('returns immediately when perfect score is found', () => {
        const components = [
          createMockGridComponent({
            id: 'existing',
            position: { x: 0, y: 0, w: 2, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const result = findOptimalPlacement(componentSize, grid)
        
        expect(result.success).toBe(true)
        expect(result.message).toContain('gap score: 0')
      })

      it('handles empty grid correctly', () => {
        const grid = createMockBentoGrid({ components: [] })
        const componentSize = { w: 2, h: 2 }
        
        const result = findOptimalPlacement(componentSize, grid)
        
        expect(result.success).toBe(true)
        expect(result.position).toEqual({ x: 0, y: 0, w: 2, h: 2 })
      })
    })
  })

  describe('Magnetic Snapping', () => {
    describe('applyMagneticSnapping', () => {
      it('snaps to nearest grid position when within threshold', () => {
        const mousePosition = { x: 103, y: 97 } // Close to 100,100 grid position
        const gridRect = createMockGridRect(12, 16, 100, 1200)
        const grid = createMockBentoGrid()
        const componentSize = { w: 2, h: 2 }
        
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.snapped).toBe(true)
        expect(result.position.x).toBe(1) // Should snap to grid position 1
        expect(result.position.y).toBe(1) // Should snap to grid position 1
        expect(result.snapStrength).toBeGreaterThan(0)
      })

      it('does not snap when outside threshold', () => {
        const mousePosition = { x: 150, y: 150 } // Far from grid lines
        const gridRect = createMockGridRect(12, 16, 100, 1200)
        const grid = createMockBentoGrid()
        const componentSize = { w: 2, h: 2 }
        
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.snapped).toBe(false)
        expect(result.snapStrength).toBe(0)
      })

      it('constrains position to grid bounds', () => {
        const mousePosition = { x: -50, y: -50 } // Outside grid
        const gridRect = createMockGridRect(12, 16, 100, 1200)
        const grid = createMockBentoGrid({ columns: 12 })
        const componentSize = { w: 2, h: 2 }
        
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.position.x).toBe(0)
        expect(result.position.y).toBe(0)
      })

      it('prevents component from extending beyond grid width', () => {
        const mousePosition = { x: 1150, y: 100 } // Near right edge
        const gridRect = createMockGridRect(12, 16, 100, 1200)
        const grid = createMockBentoGrid({ columns: 12 })
        const componentSize = { w: 4, h: 2 } // 4 units wide
        
        const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
        
        expect(result.position.x + result.position.w).toBeLessThanOrEqual(12)
      })

      it('calculates snap strength correctly', () => {
        const gridRect = createMockGridRect(12, 16, 100, 1200)
        const grid = createMockBentoGrid()
        const componentSize = { w: 2, h: 2 }
        
        // Test various distances from grid line
        const testCases = [
          { distance: 0, expectedStrength: 1 },
          { distance: SNAP_THRESHOLD / 2, expectedStrength: 0.5 },
          { distance: SNAP_THRESHOLD, expectedStrength: 0 },
        ]
        
        testCases.forEach(({ distance, expectedStrength }) => {
          const mousePosition = { x: 100 + distance, y: 100 }
          const result = applyMagneticSnapping(mousePosition, gridRect, grid, componentSize)
          
          expect(result.snapStrength).toBeCloseTo(expectedStrength, 1)
        })
      })
    })

    describe('applyComponentSnapping', () => {
      it('snaps to component edges when aligned', () => {
        const existingComponents = [
          createMockGridComponent({
            id: 'target',
            position: { x: 4, y: 4, w: 2, h: 2 }
          })
        ]
        const position = { x: 5, y: 6, w: 2, h: 2 } // Close to bottom edge
        
        const result = applyComponentSnapping(position, existingComponents, 2)
        
        expect(result.y).toBe(6) // Should snap to bottom of target component
      })

      it('does not snap when not aligned', () => {
        const existingComponents = [
          createMockGridComponent({
            id: 'target',
            position: { x: 4, y: 4, w: 2, h: 2 }
          })
        ]
        const position = { x: 8, y: 8, w: 2, h: 2 } // Not aligned
        
        const result = applyComponentSnapping(position, existingComponents, 1)
        
        expect(result).toEqual(position) // Should remain unchanged
      })

      it('respects snapping threshold', () => {
        const existingComponents = [
          createMockGridComponent({
            id: 'target',
            position: { x: 4, y: 4, w: 2, h: 2 }
          })
        ]
        const position = { x: 3, y: 6, w: 2, h: 2 } // 3 units away
        
        const resultWithThreshold1 = applyComponentSnapping(position, existingComponents, 1)
        const resultWithThreshold5 = applyComponentSnapping(position, existingComponents, 5)
        
        expect(resultWithThreshold1).toEqual(position) // Should not snap
        expect(resultWithThreshold5.x).toBe(2) // Should snap to left edge
      })
    })
  })

  describe('Drop Zone Generation', () => {
    describe('generateDropZones', () => {
      it('generates zones for empty grid', () => {
        const grid = createMockBentoGrid({ components: [] })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateDropZones(componentSize, grid)
        
        expect(zones.length).toBeGreaterThan(0)
        expect(zones[0].position).toEqual({ x: 0, y: 0, w: 2, h: 2 })
        expect(zones[0].isValid).toBe(true)
      })

      it('avoids positions that would cause collisions', () => {
        const components = createCollisionTestComponents()
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateDropZones(componentSize, grid)
        
        // All zones should be valid (non-colliding)
        zones.forEach(zone => {
          expect(zone.isValid).toBe(true)
          
          const hasCollision = components.some(comp => 
            componentsOverlap(zone.position, comp.position)
          )
          expect(hasCollision).toBe(false)
        })
      })

      it('excludes specified component from collision detection', () => {
        const components = [
          createMockGridComponent({
            id: 'exclude-me',
            position: { x: 0, y: 0, w: 2, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateDropZones(componentSize, grid, 'exclude-me')
        
        // Should include position of excluded component
        const topLeftZone = zones.find(zone => 
          zone.position.x === 0 && zone.position.y === 0
        )
        expect(topLeftZone).toBeDefined()
        expect(topLeftZone?.isValid).toBe(true)
      })

      it('limits search area to reasonable bounds', () => {
        const grid = createMockBentoGrid({ components: [] })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateDropZones(componentSize, grid)
        
        // Should not generate excessive zones
        const maxExpectedZones = 15 * (12 - 2 + 1) // maxY * valid x positions
        expect(zones.length).toBeLessThanOrEqual(maxExpectedZones)
      })
    })

    describe('getRelevantDropZones', () => {
      it('limits number of zones to specified maximum', () => {
        const zones = Array.from({ length: 20 }, (_, i) => ({
          position: { x: i % 10, y: Math.floor(i / 10), w: 2, h: 2 },
          isValid: true
        }))
        
        const relevant = getRelevantDropZones(zones, 8)
        
        expect(relevant.length).toBeLessThanOrEqual(8)
      })

      it('prioritizes zones closer to top-left', () => {
        const zones = [
          { position: { x: 5, y: 5, w: 2, h: 2 }, isValid: true },
          { position: { x: 0, y: 0, w: 2, h: 2 }, isValid: true },
          { position: { x: 2, y: 1, w: 2, h: 2 }, isValid: true },
        ]
        
        const relevant = getRelevantDropZones(zones, 3)
        
        expect(relevant[0].position).toEqual({ x: 0, y: 0, w: 2, h: 2 })
        expect(relevant[1].position).toEqual({ x: 2, y: 1, w: 2, h: 2 })
        expect(relevant[2].position).toEqual({ x: 5, y: 5, w: 2, h: 2 })
      })

      it('removes overlapping zones', () => {
        const overlappingZones = [
          { position: { x: 0, y: 0, w: 3, h: 3 }, isValid: true },
          { position: { x: 1, y: 1, w: 2, h: 2 }, isValid: true }, // Overlaps
          { position: { x: 4, y: 0, w: 2, h: 2 }, isValid: true },
        ]
        
        const relevant = getRelevantDropZones(overlappingZones, 10)
        
        expect(relevant.length).toBe(2) // Should remove overlapping zone
        expect(relevant.some(z => z.position.x === 1 && z.position.y === 1)).toBe(false)
      })

      it('filters out invalid zones', () => {
        const zones = [
          { position: { x: 0, y: 0, w: 2, h: 2 }, isValid: true },
          { position: { x: 2, y: 0, w: 2, h: 2 }, isValid: false },
          { position: { x: 4, y: 0, w: 2, h: 2 }, isValid: true },
        ]
        
        const relevant = getRelevantDropZones(zones, 10)
        
        expect(relevant.length).toBe(2)
        expect(relevant.every(z => z.isValid)).toBe(true)
      })
    })

    describe('generateStrategicDropZones', () => {
      it('includes top-left position when available', () => {
        const grid = createMockBentoGrid({ components: [] })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateStrategicDropZones(componentSize, grid)
        
        const topLeft = zones.find(z => z.position.x === 0 && z.position.y === 0)
        expect(topLeft).toBeDefined()
      })

      it('includes positions adjacent to existing components', () => {
        const components = [
          createMockGridComponent({
            id: 'existing',
            position: { x: 2, y: 2, w: 2, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateStrategicDropZones(componentSize, grid)
        
        // Should include adjacent positions
        const adjacentPositions = [
          { x: 4, y: 2 }, // Right
          { x: 2, y: 4 }, // Bottom
        ]
        
        adjacentPositions.forEach(pos => {
          const adjacentZone = zones.find(z => 
            z.position.x === pos.x && z.position.y === pos.y
          )
          expect(adjacentZone).toBeDefined()
        })
      })

      it('limits zones to specified maximum', () => {
        const components = createGridComponents(10)
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateStrategicDropZones(componentSize, grid, undefined, 4)
        
        expect(zones.length).toBeLessThanOrEqual(4)
      })

      it('avoids duplicate positions', () => {
        const components = [
          createMockGridComponent({
            id: 'comp1',
            position: { x: 2, y: 2, w: 2, h: 2 }
          }),
          createMockGridComponent({
            id: 'comp2',
            position: { x: 2, y: 4, w: 2, h: 2 }
          })
        ]
        const grid = createMockBentoGrid({ components })
        const componentSize = { w: 2, h: 2 }
        
        const zones = generateStrategicDropZones(componentSize, grid)
        
        // Check for duplicate positions
        const positions = new Set()
        zones.forEach(zone => {
          const key = `${zone.position.x}-${zone.position.y}`
          expect(positions.has(key)).toBe(false)
          positions.add(key)
        })
      })
    })
  })

  describe('Validation Functions', () => {
    describe('isValidPlacement', () => {
      it('validates position within grid bounds', () => {
        const components: any[] = []
        
        const validPosition = { x: 0, y: 0, w: 2, h: 2 }
        const invalidPositions = [
          { x: -1, y: 0, w: 2, h: 2 }, // Negative x
          { x: 0, y: -1, w: 2, h: 2 }, // Negative y
          { x: 11, y: 0, w: 2, h: 2 }, // Extends beyond width (12 columns)
          { x: 0, y: 0, w: 0, h: 2 },  // Zero width
          { x: 0, y: 0, w: 2, h: 0 },  // Zero height
        ]
        
        expect(isValidPlacement(validPosition, components, 12)).toBe(true)
        
        invalidPositions.forEach(pos => {
          expect(isValidPlacement(pos, components, 12)).toBe(false)
        })
      })

      it('detects collisions with existing components', () => {
        const components = createCollisionTestComponents()
        
        const collidingPosition = { x: 1, y: 1, w: 2, h: 2 } // Overlaps with existing
        const validPosition = { x: 8, y: 0, w: 2, h: 2 } // Clear area
        
        expect(isValidPlacement(collidingPosition, components, 12)).toBe(false)
        expect(isValidPlacement(validPosition, components, 12)).toBe(true)
      })

      it('handles edge cases for component boundaries', () => {
        const components = [
          createMockGridComponent({
            id: 'existing',
            position: { x: 2, y: 2, w: 2, h: 2 }
          })
        ]
        
        // Adjacent positions should be valid
        const adjacentPositions = [
          { x: 0, y: 2, w: 2, h: 2 }, // Left edge
          { x: 4, y: 2, w: 2, h: 2 }, // Right edge
          { x: 2, y: 0, w: 2, h: 2 }, // Top edge
          { x: 2, y: 4, w: 2, h: 2 }, // Bottom edge
        ]
        
        adjacentPositions.forEach(pos => {
          expect(isValidPlacement(pos, components, 12)).toBe(true)
        })
      })
    })

    describe('getInvalidReason', () => {
      it('identifies bounds violations', () => {
        const components: any[] = []
        
        expect(getInvalidReason({ x: -1, y: 0, w: 2, h: 2 }, components, 12))
          .toContain('outside grid bounds')
        
        expect(getInvalidReason({ x: 11, y: 0, w: 2, h: 2 }, components, 12))
          .toContain('beyond grid width')
        
        expect(getInvalidReason({ x: 0, y: 0, w: 0, h: 2 }, components, 12))
          .toContain('Invalid component dimensions')
      })

      it('identifies collisions', () => {
        const components = [
          createMockGridComponent({
            id: 'existing',
            position: { x: 2, y: 2, w: 2, h: 2 }
          })
        ]
        
        const reason = getInvalidReason({ x: 2, y: 2, w: 2, h: 2 }, components, 12)
        expect(reason).toContain('Overlaps with existing component')
      })
    })

    describe('componentsOverlap', () => {
      it('detects overlapping components', () => {
        const comp1 = { x: 0, y: 0, w: 3, h: 3 }
        const comp2 = { x: 2, y: 2, w: 3, h: 3 }
        
        expect(componentsOverlap(comp1, comp2)).toBe(true)
      })

      it('identifies non-overlapping components', () => {
        const comp1 = { x: 0, y: 0, w: 2, h: 2 }
        const comp2 = { x: 3, y: 0, w: 2, h: 2 }
        
        expect(componentsOverlap(comp1, comp2)).toBe(false)
      })

      it('handles edge-touching components', () => {
        const comp1 = { x: 0, y: 0, w: 2, h: 2 }
        const comp2 = { x: 2, y: 0, w: 2, h: 2 } // Touches right edge
        
        expect(componentsOverlap(comp1, comp2)).toBe(false)
      })

      it('handles one component inside another', () => {
        const outer = { x: 0, y: 0, w: 5, h: 5 }
        const inner = { x: 1, y: 1, w: 2, h: 2 }
        
        expect(componentsOverlap(outer, inner)).toBe(true)
        expect(componentsOverlap(inner, outer)).toBe(true)
      })
    })

    describe('calculateGridHeight', () => {
      it('returns 0 for empty components array', () => {
        expect(calculateGridHeight([])).toBe(0)
      })

      it('calculates maximum bottom position', () => {
        const components = [
          createMockGridComponent({
            id: 'comp1',
            position: { x: 0, y: 0, w: 2, h: 3 } // Bottom at y=3
          }),
          createMockGridComponent({
            id: 'comp2',
            position: { x: 0, y: 2, w: 2, h: 4 } // Bottom at y=6
          }),
          createMockGridComponent({
            id: 'comp3',
            position: { x: 0, y: 1, w: 2, h: 2 } // Bottom at y=3
          }),
        ]
        
        expect(calculateGridHeight(components)).toBe(6)
      })
    })
  })

  describe('Context-aware Help', () => {
    describe('getContextualHelp', () => {
      it('provides help for drag operations', () => {
        const help = getContextualHelp('drag-start', { isDragging: true })
        
        expect(help).toBeDefined()
        expect(help?.type).toBe('info')
        expect(help?.message).toContain('drag')
      })

      it('provides collision feedback', () => {
        const help = getContextualHelp('drag-start', { 
          isDragging: true,
          hasCollision: true 
        })
        
        expect(help).toBeDefined()
        expect(help?.type).toBe('error')
        expect(help?.message).toContain('Cannot place here')
      })

      it('provides snapping feedback', () => {
        const help = getContextualHelp('drag-start', { 
          isDragging: true,
          snapActive: true 
        })
        
        expect(help).toBeDefined()
        expect(help?.type).toBe('success')
        expect(help?.message).toContain('Snapped')
      })

      it('provides welcome message for empty grid', () => {
        const help = getContextualHelp('add-component', { componentCount: 0 })
        
        expect(help).toBeDefined()
        expect(help?.type).toBe('success')
        expect(help?.message).toContain('Welcome')
      })

      it('warns when grid is full', () => {
        const help = getContextualHelp('add-component', { gridFull: true })
        
        expect(help).toBeDefined()
        expect(help?.type).toBe('warning')
        expect(help?.message).toContain('Grid is full')
      })
    })

    describe('getTooltipText', () => {
      it('provides tooltip for common elements', () => {
        expect(getTooltipText('drag-handle')).toContain('Drag to move')
        expect(getTooltipText('resize-handle')).toContain('Drag to resize')
        expect(getTooltipText('delete-button')).toContain('Delete')
        expect(getTooltipText('properties-button')).toContain('Edit')
      })

      it('provides fallback for unknown elements', () => {
        expect(getTooltipText('unknown-element')).toContain('No tooltip available')
      })
    })

    describe('getErrorMessage', () => {
      it('provides error messages for common operations', () => {
        expect(getErrorMessage('placement-failed')).toContain('Could not place')
        expect(getErrorMessage('resize-failed')).toContain('Cannot resize')
        expect(getErrorMessage('overlap-detected')).toContain('overlaps')
      })

      it('uses custom error message when provided', () => {
        expect(getErrorMessage('unknown', 'Custom error')).toBe('Custom error')
      })

      it('provides fallback for unknown operations', () => {
        expect(getErrorMessage('unknown-operation')).toContain('unknown error')
      })
    })
  })

  describe('Performance Utilities', () => {
    describe('debounce', () => {
      it('delays function execution', async () => {
        const fn = vi.fn()
        const debouncedFn = debounce(fn, 50)
        
        debouncedFn()
        debouncedFn()
        debouncedFn()
        
        expect(fn).not.toHaveBeenCalled()
        
        await new Promise(resolve => setTimeout(resolve, 60))
        expect(fn).toHaveBeenCalledTimes(1)
      })

      it('resets delay on subsequent calls', async () => {
        const fn = vi.fn()
        const debouncedFn = debounce(fn, 50)
        
        debouncedFn()
        
        setTimeout(() => debouncedFn(), 25) // Before first delay expires
        
        await new Promise(resolve => setTimeout(resolve, 60))
        expect(fn).not.toHaveBeenCalled()
        
        await new Promise(resolve => setTimeout(resolve, 30))
        expect(fn).toHaveBeenCalledTimes(1)
      })
    })

    describe('throttle', () => {
      it('limits function execution rate', async () => {
        const fn = vi.fn()
        const throttledFn = throttle(fn, 50)
        
        throttledFn()
        throttledFn()
        throttledFn()
        
        expect(fn).toHaveBeenCalledTimes(1) // Immediate first call
        
        await new Promise(resolve => setTimeout(resolve, 60))
        expect(fn).toHaveBeenCalledTimes(2) // Trailing call
      })

      it('executes immediately on first call', () => {
        const fn = vi.fn()
        const throttledFn = throttle(fn, 50)
        
        throttledFn()
        expect(fn).toHaveBeenCalledTimes(1)
      })
    })

    describe('easing functions', () => {
      describe('easeOutCubic', () => {
        it('returns correct values for ease out cubic', () => {
          expect(easeOutCubic(0)).toBe(0)
          expect(easeOutCubic(1)).toBe(1)
          expect(easeOutCubic(0.5)).toBeCloseTo(0.875, 2)
        })
      })

      describe('easeOutBack', () => {
        it('returns correct values for ease out back', () => {
          expect(easeOutBack(0)).toBeCloseTo(-0.73, 1) // Overshoot
          expect(easeOutBack(1)).toBe(1)
        })

        it('creates overshoot effect', () => {
          const value = easeOutBack(0.8)
          expect(value).toBeGreaterThan(1) // Should overshoot
        })
      })
    })

    describe('calculateFeedbackIntensity', () => {
      it('calculates intensity based on drag distance', () => {
        expect(calculateFeedbackIntensity(0, 0, true)).toBe(0)
        expect(calculateFeedbackIntensity(100, 0, true)).toBe(1)
        expect(calculateFeedbackIntensity(200, 0, true)).toBe(1) // Capped at 1
      })

      it('increases intensity with snap strength', () => {
        const withSnap = calculateFeedbackIntensity(50, 0.5, true)
        const withoutSnap = calculateFeedbackIntensity(50, 0, true)
        
        expect(withSnap).toBeGreaterThan(withoutSnap)
      })

      it('reduces intensity for invalid positions', () => {
        const valid = calculateFeedbackIntensity(50, 0, true)
        const invalid = calculateFeedbackIntensity(50, 0, false)
        
        expect(invalid).toBeLessThan(valid)
      })
    })

    describe('isDropZoneTooClose', () => {
      it('detects zones that are too close', () => {
        const existingZones = [
          { position: { x: 2, y: 2, w: 2, h: 2 }, isValid: true }
        ]
        const newZone = { position: { x: 2, y: 3, w: 2, h: 2 }, isValid: true }
        
        expect(isDropZoneTooClose(newZone, existingZones, 2)).toBe(true)
      })

      it('allows zones with sufficient distance', () => {
        const existingZones = [
          { position: { x: 2, y: 2, w: 2, h: 2 }, isValid: true }
        ]
        const newZone = { position: { x: 5, y: 5, w: 2, h: 2 }, isValid: true }
        
        expect(isDropZoneTooClose(newZone, existingZones, 2)).toBe(false)
      })
    })
  })

  describe('Integration Tests', () => {
    it('works with realistic grid scenarios', () => {
      const components = createGridComponents(8)
      const grid = createMockBentoGrid({ components })
      
      // Test placement
      const placement = findBestPlacement({ w: 2, h: 2 }, grid)
      expect(placement.success).toBe(true)
      
      // Test drop zones
      const zones = generateStrategicDropZones({ w: 2, h: 2 }, grid)
      expect(zones.length).toBeGreaterThan(0)
      
      // Test validation
      if (placement.position) {
        expect(isValidPlacement(placement.position, components, 12)).toBe(true)
      }
    })

    it('handles performance with large grids', () => {
      const components = Array.from({ length: 50 }, (_, i) => 
        createMockGridComponent({
          id: `perf-${i}`,
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
      
      // Test multiple operations
      const placement = findOptimalPlacement({ w: 2, h: 2 }, grid)
      const zones = getOptimizedDropZones({ w: 2, h: 2 }, grid)
      const height = calculateGridHeight(components)
      
      const endTime = performance.now()
      const duration = endTime - startTime
      
      expect(duration).toBeLessThan(50) // Should complete quickly
      expect(placement.success).toBeDefined()
      expect(zones.length).toBeGreaterThan(0)
      expect(height).toBeGreaterThan(0)
    })
  })
})