/**
 * BentoGrid Test Utilities
 * 
 * Comprehensive testing utilities for the Bento Grid System including:
 * - Grid component factories
 * - Mock data generators
 * - Drag and drop test helpers
 * - Touch gesture simulation
 * - Collision detection test helpers
 * - Performance test utilities
 */

import React from 'react'
import { vi } from 'vitest'
import { DndContext, DragOverlay, closestCenter } from '@dnd-kit/core'
import type { 
  BentoGrid, 
  GridComponent, 
  GridPosition, 
  Size 
} from '@/types/bento'
import {
  DeviceType,
  ComponentCategory,
  createDefaultGridPosition,
  GRID_DEFAULTS 
} from '@/types/bento'

// ============================================================================
// Mock Data Factories
// ============================================================================

/**
 * Create a mock grid component with sensible defaults
 */
export const createMockGridComponent = (
  overrides: Partial<GridComponent> = {}
): GridComponent => ({
  id: `component-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  componentType: 'metric-card',
  position: createDefaultGridPosition(),
  locked: false,
  display: {
    className: '',
    style: {},
    hideOn: [],
    showOnly: []
  },
  bindings: {
    data: {},
    events: [] as any[],
    state: {}
  },
  ...overrides
})

/**
 * Create a mock bento grid with sensible defaults
 */
export const createMockBentoGrid = (
  overrides: Partial<BentoGrid> = {}
): BentoGrid => ({
  id: `grid-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  name: 'Test Grid',
  device: DeviceType.Desktop,
  columns: GRID_DEFAULTS.COLUMNS,
  gap: GRID_DEFAULTS.GAP,
  rowHeight: GRID_DEFAULTS.ROW_HEIGHT,
  components: [],
  settings: {
    enableSnapping: true,
    snapThreshold: 15,
    showGridLines: false,
    allowOverflow: false,
    maxHeight: undefined,
    autoResize: true,
    showGrid: false,
    snapToGrid: true,
    gridColor: '#e5e7eb',
    allowOverlap: false,
    compactMode: 'vertical' as const,
    minColumns: 1,
    maxColumns: 24
  },
  zones: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides
})

/**
 * Create multiple grid components with non-overlapping positions
 */
export const createGridComponents = (
  count: number,
  gridColumns: number = 12
): GridComponent[] => {
  const components: GridComponent[] = []
  let x = 0
  let y = 0
  
  for (let i = 0; i < count; i++) {
    const component = createMockGridComponent({
      id: `component-${i}`,
      componentType: ['metric-card', 'chart', 'kpi', 'table'][i % 4],
      position: { x, y, w: 2, h: 2 }
    })
    
    components.push(component)
    
    // Move to next position
    x += 2
    if (x >= gridColumns) {
      x = 0
      y += 2
    }
  }
  
  return components
}

/**
 * Create components with specific positions for collision testing
 */
export const createCollisionTestComponents = (): GridComponent[] => [
  createMockGridComponent({
    id: 'component-1',
    position: { x: 0, y: 0, w: 3, h: 2 }
  }),
  createMockGridComponent({
    id: 'component-2', 
    position: { x: 4, y: 0, w: 2, h: 3 }
  }),
  createMockGridComponent({
    id: 'component-3',
    position: { x: 0, y: 3, w: 4, h: 1 }
  })
]

// ============================================================================
// Drag and Drop Test Helpers
// ============================================================================

/**
 * Mock DND Context provider for testing
 */
export const MockDndProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <DndContext collisionDetection={closestCenter}>
    {children}
    <DragOverlay />
  </DndContext>
)

/**
 * Create a mock drag start event
 */
export const createMockDragStartEvent = (componentId: string) => ({
  active: {
    id: componentId,
    data: {
      current: {
        type: 'grid-component',
        component: createMockGridComponent({ id: componentId })
      }
    },
    rect: {
      current: {
        initial: { top: 0, left: 0, right: 100, bottom: 100 },
        translated: { top: 0, left: 0, right: 100, bottom: 100 }
      }
    }
  }
})

/**
 * Create a mock drag over event
 */
export const createMockDragOverEvent = (componentId: string, delta = { x: 50, y: 50 }) => ({
  active: {
    id: componentId,
    rect: {
      current: {
        initial: { top: 0, left: 0, right: 100, bottom: 100 },
        translated: { top: delta.y, left: delta.x, right: 100 + delta.x, bottom: 100 + delta.y }
      }
    }
  },
  over: null,
  delta,
  collisions: []
})

/**
 * Create a mock drag end event
 */
export const createMockDragEndEvent = (componentId: string, delta = { x: 50, y: 50 }) => ({
  active: {
    id: componentId,
    rect: {
      current: {
        initial: { top: 0, left: 0, right: 100, bottom: 100 },
        translated: { top: delta.y, left: delta.x, right: 100 + delta.x, bottom: 100 + delta.y }
      }
    }
  },
  over: null,
  delta
})

// ============================================================================
// Touch Gesture Test Helpers
// ============================================================================

/**
 * Create a mock touch event
 */
interface MockTouch {
  clientX: number;
  clientY: number;
  identifier?: number;
  pageX?: number;
  pageY?: number;
  screenX?: number;
  screenY?: number;
}

export const createMockTouchEvent = (
  type: string, 
  touches: MockTouch[] = []
): TouchEvent => {
  const touchList = touches.map((touch, index) => ({
    ...touch,
    identifier: touch.identifier ?? index,
    clientX: touch.clientX,
    clientY: touch.clientY,
    pageX: touch.pageX ?? touch.clientX,
    pageY: touch.pageY ?? touch.clientY,
    screenX: touch.screenX ?? touch.clientX,
    screenY: touch.screenY ?? touch.clientY,
    radiusX: 20,
    radiusY: 20,
    rotationAngle: 0,
    force: 1,
    target: document.body
  })) as Touch[]

  const mockTouchList = {
    length: touchList.length,
    item: (index: number) => touchList[index] || null,
    [Symbol.iterator]: function* () {
      for (const touch of touchList) {
        yield touch
      }
    }
  } as TouchList

  return new TouchEvent(type, {
    bubbles: true,
    cancelable: true,
    touches: mockTouchList as any,
    changedTouches: mockTouchList as any,
    targetTouches: mockTouchList as any
  })
}

/**
 * Simulate a long press gesture
 */
export const simulateLongPress = async (
  element: HTMLElement, 
  options: { x?: number; y?: number; duration?: number } = {}
) => {
  const { x = 50, y = 50, duration = 500 } = options
  
  const touchStart = createMockTouchEvent('touchstart', [{ clientX: x, clientY: y }])
  element.dispatchEvent(touchStart)
  
  await new Promise(resolve => setTimeout(resolve, duration + 50))
  
  const touchEnd = createMockTouchEvent('touchend', [])
  element.dispatchEvent(touchEnd)
}

/**
 * Simulate a pinch gesture
 */
export const simulatePinchGesture = async (
  element: HTMLElement,
  options: { 
    startDistance?: number; 
    endDistance?: number; 
    centerX?: number; 
    centerY?: number;
    steps?: number;
  } = {}
) => {
  const { 
    startDistance = 100, 
    endDistance = 200, 
    centerX = 250, 
    centerY = 250,
    steps = 5
  } = options

  // Start with two touches
  const touch1Start = { 
    clientX: centerX - startDistance / 2, 
    clientY: centerY,
    identifier: 0
  }
  const touch2Start = { 
    clientX: centerX + startDistance / 2, 
    clientY: centerY,
    identifier: 1
  }

  const touchStart = createMockTouchEvent('touchstart', [touch1Start, touch2Start])
  element.dispatchEvent(touchStart)

  // Simulate pinch movement
  for (let i = 1; i <= steps; i++) {
    const progress = i / steps
    const currentDistance = startDistance + (endDistance - startDistance) * progress
    
    const touch1 = { 
      clientX: centerX - currentDistance / 2, 
      clientY: centerY,
      identifier: 0
    }
    const touch2 = { 
      clientX: centerX + currentDistance / 2, 
      clientY: centerY,
      identifier: 1
    }

    const touchMove = createMockTouchEvent('touchmove', [touch1, touch2])
    element.dispatchEvent(touchMove)
    
    await new Promise(resolve => setTimeout(resolve, 10))
  }

  // End touches
  const touchEnd = createMockTouchEvent('touchend', [])
  element.dispatchEvent(touchEnd)
}

/**
 * Simulate a swipe gesture
 */
export const simulateSwipeGesture = async (
  element: HTMLElement,
  direction: 'up' | 'down' | 'left' | 'right',
  options: { distance?: number; duration?: number; startX?: number; startY?: number } = {}
) => {
  const { distance = 100, duration = 300, startX = 250, startY = 250 } = options
  
  let endX = startX
  let endY = startY
  
  switch (direction) {
    case 'up':
      endY -= distance
      break
    case 'down':
      endY += distance
      break
    case 'left':
      endX -= distance
      break
    case 'right':
      endX += distance
      break
  }
  
  const touchStart = createMockTouchEvent('touchstart', [{ clientX: startX, clientY: startY }])
  element.dispatchEvent(touchStart)
  
  await new Promise(resolve => setTimeout(resolve, 50))
  
  const touchMove = createMockTouchEvent('touchmove', [{ clientX: endX, clientY: endY }])
  element.dispatchEvent(touchMove)
  
  await new Promise(resolve => setTimeout(resolve, duration))
  
  const touchEnd = createMockTouchEvent('touchend', [])
  element.dispatchEvent(touchEnd)
}

// ============================================================================
// Position and Collision Test Helpers
// ============================================================================

/**
 * Check if two positions overlap
 */
export const positionsOverlap = (pos1: GridPosition, pos2: GridPosition): boolean => {
  return !(
    pos1.x >= pos2.x + pos2.w ||
    pos1.x + pos1.w <= pos2.x ||
    pos1.y >= pos2.y + pos2.h ||
    pos1.y + pos1.h <= pos2.y
  )
}

/**
 * Generate all valid positions for a component size within a grid
 */
export const generateValidPositions = (
  componentSize: Size,
  gridColumns: number,
  existingComponents: GridComponent[] = [],
  maxRows = 10
): GridPosition[] => {
  const validPositions: GridPosition[] = []
  
  for (let y = 0; y < maxRows; y++) {
    for (let x = 0; x <= gridColumns - componentSize.w; x++) {
      const position: GridPosition = { x, y, ...componentSize }
      
      const hasCollision = existingComponents.some(comp => 
        positionsOverlap(position, comp.position)
      )
      
      if (!hasCollision) {
        validPositions.push(position)
      }
    }
  }
  
  return validPositions
}

/**
 * Create a grid rect for testing magnetic snapping
 */
export const createMockGridRect = (
  columns = 12,
  gap = 16,
  rowHeight = 100,
  containerWidth = 1200
): DOMRect => {
  const cellWidth = (containerWidth - (gap * (columns - 1))) / columns
  
  return {
    top: 0,
    left: 0,
    right: containerWidth,
    bottom: rowHeight * 10, // Assume 10 rows for testing
    width: containerWidth,
    height: rowHeight * 10,
    x: 0,
    y: 0,
    toJSON: () => ({})
  } as DOMRect
}

// ============================================================================
// Performance Test Helpers
// ============================================================================

/**
 * Create a large number of components for performance testing
 */
export const createLargeComponentSet = (count: number): GridComponent[] => {
  const components: GridComponent[] = []
  const columns = 12
  
  for (let i = 0; i < count; i++) {
    const x = (i * 2) % columns
    const y = Math.floor((i * 2) / columns) * 2
    
    components.push(createMockGridComponent({
      id: `perf-component-${i}`,
      position: { x, y, w: 2, h: 2 }
    }))
  }
  
  return components
}

/**
 * Measure rendering performance
 */
export const measureRenderTime = async (renderFn: () => void): Promise<number> => {
  const start = performance.now()
  await renderFn()
  const end = performance.now()
  return end - start
}

/**
 * Simulate rapid drag operations for performance testing
 */
export const simulateRapidDragOperations = async (
  onDrag: (position: { x: number; y: number }) => void,
  count = 100,
  interval = 10
) => {
  for (let i = 0; i < count; i++) {
    onDrag({ x: i, y: i })
    await new Promise(resolve => setTimeout(resolve, interval))
  }
}

// ============================================================================
// Mock Component Renderer
// ============================================================================

/**
 * Simple mock component renderer for testing
 */
export const MockComponentRenderer: React.FC<{ 
  component: GridComponent;
  gridSize: Size;
  deviceType?: DeviceType;
}> = ({ component, gridSize, deviceType = DeviceType.Desktop }) => (
  <div 
    data-testid={`component-${component.id}`}
    data-component-type={component.componentType}
    data-device-type={deviceType}
    className="mock-component-renderer"
    style={{
      width: '100%',
      height: '100%',
      backgroundColor: '#f0f0f0',
      border: '1px solid #ccc',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontSize: '12px',
      color: '#666'
    }}
  >
    {component.componentType} ({gridSize.w}×{gridSize.h})
  </div>
)

// ============================================================================
// Mock Implementations
// ============================================================================

/**
 * Mock ResizeObserver for testing
 */
export const mockResizeObserver = () => {
  const mockObserver = vi.fn(() => ({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
  }))
  
  // @ts-ignore
  global.ResizeObserver = mockObserver
  
  return mockObserver
}

/**
 * Mock IntersectionObserver for testing
 */
export const mockIntersectionObserver = () => {
  const mockObserver = vi.fn(() => ({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
  }))
  
  // @ts-ignore
  global.IntersectionObserver = mockObserver
  
  return mockObserver
}

/**
 * Mock requestAnimationFrame for testing
 */
export const mockRequestAnimationFrame = () => {
  const mockRAF = vi.fn((callback: FrameRequestCallback) => {
    setTimeout(callback, 16) // Simulate ~60fps
    return 1
  })
  
  global.requestAnimationFrame = mockRAF
  global.cancelAnimationFrame = vi.fn()
  
  return mockRAF
}

/**
 * Mock getBoundingClientRect for DOM elements
 */
export const mockGetBoundingClientRect = (rect: Partial<DOMRect>) => {
  const mockRect = {
    top: 0,
    left: 0,
    right: 100,
    bottom: 100,
    width: 100,
    height: 100,
    x: 0,
    y: 0,
    toJSON: () => ({}),
    ...rect
  } as DOMRect
  
  Element.prototype.getBoundingClientRect = vi.fn(() => mockRect)
  
  return mockRect
}

// ============================================================================
// Test Environment Setup
// ============================================================================

/**
 * Setup complete test environment for BentoGrid testing
 */
export const setupBentoTestEnvironment = () => {
  // Mock DOM APIs
  mockResizeObserver()
  mockIntersectionObserver()
  mockRequestAnimationFrame()
  mockGetBoundingClientRect({})
  
  // Mock touch support
  Object.defineProperty(window, 'ontouchstart', {
    value: undefined,
    writable: true
  })
  
  Object.defineProperty(navigator, 'maxTouchPoints', {
    value: 0,
    writable: true
  })
  
  // Mock vibration API
  Object.defineProperty(navigator, 'vibrate', {
    value: vi.fn(),
    writable: true
  })
  
  // Mock CSS grid support
  Object.defineProperty(document.documentElement.style, 'gridTemplateColumns', {
    value: '',
    writable: true
  })
  
  return {
    enableTouchDevice: () => {
      Object.defineProperty(window, 'ontouchstart', { value: {}, writable: true })
      Object.defineProperty(navigator, 'maxTouchPoints', { value: 1, writable: true })
    },
    
    disableTouchDevice: () => {
      Object.defineProperty(window, 'ontouchstart', { value: undefined, writable: true })
      Object.defineProperty(navigator, 'maxTouchPoints', { value: 0, writable: true })
    },
    
    mockResizeObserver: mockResizeObserver
  }
}

// ============================================================================
// Export All Utilities
// ============================================================================

export default {
  // Factories
  createMockGridComponent,
  createMockBentoGrid,
  createGridComponents,
  createCollisionTestComponents,
  
  // Drag and Drop
  MockDndProvider,
  createMockDragStartEvent,
  createMockDragOverEvent,
  createMockDragEndEvent,
  
  // Touch Gestures
  createMockTouchEvent,
  simulateLongPress,
  simulatePinchGesture,
  simulateSwipeGesture,
  
  // Position and Collision
  positionsOverlap,
  generateValidPositions,
  createMockGridRect,
  
  // Performance
  createLargeComponentSet,
  measureRenderTime,
  simulateRapidDragOperations,
  
  // Components
  MockComponentRenderer,
  
  // Environment
  setupBentoTestEnvironment
}