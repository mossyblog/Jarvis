/**
 * BentoGrid Basic Tests - Simple verification tests
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@/test/utils/test-utils'

import { DeviceType } from '@/types/bento'
import {
  createMockBentoGrid,
  createMockGridComponent,
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

// Create a simple test component that just shows it can import and create basic structures
const SimpleTestComponent: React.FC = () => {
  const grid = createMockBentoGrid({
    components: [
      createMockGridComponent({
        id: 'test-component',
        position: { x: 0, y: 0, w: 2, h: 2 }
      })
    ]
  })

  return (
    <MockDndProvider>
      <div data-testid="simple-test">
        <div data-testid="grid-config">
          Columns: {grid.columns}, Gap: {grid.gap}
        </div>
        <div data-testid="component-count">
          Components: {grid.components.length}
        </div>
      </div>
    </MockDndProvider>
  )
}

describe('BentoGrid Basic Setup Tests', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Test Infrastructure', () => {
    it('can create mock grid data', () => {
      const grid = createMockBentoGrid()
      
      expect(grid).toBeDefined()
      expect(grid.id).toBeDefined()
      expect(grid.columns).toBe(12)
      expect(grid.gap).toBe(16)
      expect(grid.components).toEqual([])
    })

    it('can create mock components', () => {
      const component = createMockGridComponent({
        id: 'test',
        componentType: 'metric-card'
      })
      
      expect(component).toBeDefined()
      expect(component.id).toBe('test')
      expect(component.componentType).toBe('metric-card')
      expect(component.position).toBeDefined()
    })

    it('can render simple test component', () => {
      render(<SimpleTestComponent />)
      
      expect(screen.getByTestId('simple-test')).toBeInTheDocument()
      expect(screen.getByTestId('grid-config')).toHaveTextContent('Columns: 12, Gap: 16')
      expect(screen.getByTestId('component-count')).toHaveTextContent('Components: 1')
    })

    it('test environment setup works', () => {
      expect(testEnvironment).toBeDefined()
      expect(typeof testEnvironment.enableTouchDevice).toBe('function')
      expect(typeof testEnvironment.disableTouchDevice).toBe('function')
    })

    it('can detect device capabilities', () => {
      testEnvironment.enableTouchDevice()
      expect('ontouchstart' in window).toBe(true)
      
      testEnvironment.disableTouchDevice()
      expect('ontouchstart' in window).toBe(false)
    })
  })

  describe('Type System Verification', () => {
    it('device types are properly defined', () => {
      expect(DeviceType.Desktop).toBe('desktop')
      expect(DeviceType.Tablet).toBe('tablet')
      expect(DeviceType.Mobile).toBe('mobile')
    })

    it('mock components have correct structure', () => {
      const component = createMockGridComponent()
      
      expect(component).toHaveProperty('id')
      expect(component).toHaveProperty('componentType')
      expect(component).toHaveProperty('position')
      expect(component).toHaveProperty('locked')
      expect(component).toHaveProperty('display')
      expect(component).toHaveProperty('bindings')
      expect(component).toHaveProperty('animation')
      
      expect(component.position).toHaveProperty('x')
      expect(component.position).toHaveProperty('y')
      expect(component.position).toHaveProperty('w')
      expect(component.position).toHaveProperty('h')
    })

    it('mock grids have correct structure', () => {
      const grid = createMockBentoGrid()
      
      expect(grid).toHaveProperty('id')
      expect(grid).toHaveProperty('name')
      expect(grid).toHaveProperty('device')
      expect(grid).toHaveProperty('layoutId')
      expect(grid).toHaveProperty('columns')
      expect(grid).toHaveProperty('gap')
      expect(grid).toHaveProperty('rowHeight')
      expect(grid).toHaveProperty('components')
      expect(grid).toHaveProperty('settings')
      expect(grid).toHaveProperty('zones')
    })
  })

  describe('Mock Components', () => {
    it('MockComponentRenderer displays component info', () => {
      const component = createMockGridComponent({
        id: 'test-comp',
        componentType: 'chart'
      })
      
      render(
        <MockComponentRenderer 
          component={component}
          gridSize={{ w: 3, h: 2 }}
          deviceType={DeviceType.Desktop}
        />
      )
      
      expect(screen.getByText('chart (3×2)')).toBeInTheDocument()
      expect(screen.getByTestId('component-test-comp')).toBeInTheDocument()
    })

    it('MockDndProvider wraps content correctly', () => {
      render(
        <MockDndProvider>
          <div data-testid="wrapped-content">Test Content</div>
        </MockDndProvider>
      )
      
      expect(screen.getByTestId('wrapped-content')).toBeInTheDocument()
    })
  })

  describe('Utility Functions', () => {
    it('can generate multiple components', () => {
      const components = Array.from({ length: 5 }, (_, i) => 
        createMockGridComponent({ id: `comp-${i}` })
      )
      
      expect(components).toHaveLength(5)
      expect(components[0].id).toBe('comp-0')
      expect(components[4].id).toBe('comp-4')
    })

    it('can create grids with custom properties', () => {
      const grid = createMockBentoGrid({
        columns: 8,
        gap: 24,
        rowHeight: 120
      })
      
      expect(grid.columns).toBe(8)
      expect(grid.gap).toBe(24)
      expect(grid.rowHeight).toBe(120)
    })

    it('generates unique IDs for components', () => {
      const component1 = createMockGridComponent()
      const component2 = createMockGridComponent()
      
      expect(component1.id).not.toBe(component2.id)
    })

    it('generates unique IDs for grids', () => {
      const grid1 = createMockBentoGrid()
      const grid2 = createMockBentoGrid()
      
      expect(grid1.id).not.toBe(grid2.id)
    })
  })
})