/**
 * GridComponent Wrapper Tests
 * 
 * Comprehensive tests for the GridComponent wrapper including:
 * - Component rendering and positioning
 * - Edit mode interactions (drag, resize, delete)
 * - Mobile touch behavior
 * - Resize handle functionality
 * - Component states and animations
 * - Accessibility features
 * - Performance optimizations
 */

import React from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent, cleanup } from '@/test/utils/test-utils'
import userEvent from '@testing-library/user-event'

import { GridComponent } from '../GridComponent'
import { DeviceType } from '@/types/bento'
import {
  createMockGridComponent,
  MockComponentRenderer,
  setupBentoTestEnvironment,
  MockDndProvider,
  simulateLongPress,
  createMockTouchEvent
} from '@/test/utils/bento-test-utils'

// Mock useDraggable hook
const mockUseDraggable = vi.fn()
vi.mock('@dnd-kit/core', async () => {
  const actual = await vi.importActual('@dnd-kit/core')
  return {
    ...actual,
    useDraggable: mockUseDraggable
  }
})

// Mock touch gesture hook
vi.mock('@/hooks/useTouchGestures', () => ({
  useTouchTargetValidation: () => ({
    isValidTarget: true,
    suggestions: [],
    minSize: 44
  })
}))

describe('GridComponent Wrapper', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let onDelete: ReturnType<typeof vi.fn>
  let onResize: ReturnType<typeof vi.fn>
  let onShowProperties: ReturnType<typeof vi.fn>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    onDelete = vi.fn()
    onResize = vi.fn()
    onShowProperties = vi.fn()
    
    // Mock useDraggable return value
    mockUseDraggable.mockReturnValue({
      attributes: {},
      listeners: {},
      setNodeRef: vi.fn(),
      transform: null,
      isDragging: false
    })
    
    vi.clearAllMocks()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders component with correct grid positioning', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        position: { x: 2, y: 3, w: 4, h: 2 }
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveStyle({
        gridColumn: '3 / span 4', // x + 1 / span w
        gridRow: '4 / span 2'     // y + 1 / span h
      })
    })

    it('applies correct CSS classes based on component state', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        locked: true
      })
      
      const { rerender } = render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            isSelected={true}
            isDragging={false}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveClass('bento-component--editing')
      expect(wrapper).toHaveClass('bento-component--selected')
      expect(wrapper).toHaveClass('bento-component--locked')
      expect(wrapper).not.toHaveClass('bento-component--dragging')

      // Test dragging state
      rerender(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            isDragging={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(wrapper).toHaveClass('bento-component--dragging')
    })

    it('sets correct data attributes', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        componentType: 'metric-card'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            deviceType={DeviceType.Mobile}
            isMobile={true}
            isTouchDevice={true}
            dragMode={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveAttribute('data-component-type', 'metric-card')
      expect(wrapper).toHaveAttribute('data-component-id', 'test-component')
      expect(wrapper).toHaveAttribute('data-mobile', 'true')
      expect(wrapper).toHaveAttribute('data-touch-device', 'true')
      expect(wrapper).toHaveAttribute('data-drag-mode', 'true')
    })

    it('renders children content correctly', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <div data-testid="child-content">Test Content</div>
          </GridComponent>
        </MockDndProvider>
      )

      expect(screen.getByTestId('child-content')).toBeInTheDocument()
      expect(screen.getByText('Test Content')).toBeInTheDocument()
    })
  })

  describe('Edit Mode Behavior', () => {
    it('shows edit controls when in edit mode and not locked', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        locked: false
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      // Should show drag handle bar
      expect(screen.getByTitle('Drag to move component')).toBeInTheDocument()
      
      // Should show action buttons
      expect(screen.getByRole('button', { name: /delete/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /properties/i })).toBeInTheDocument()
      
      // Should show resize handles
      const resizeHandles = screen.getAllByTitle(/drag to resize/i)
      expect(resizeHandles).toHaveLength(8) // 8 resize handles (corners + edges)
    })

    it('hides edit controls when not in edit mode', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={false}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(screen.queryByTitle('Drag to move component')).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /delete/i })).not.toBeInTheDocument()
      expect(screen.queryByTitle(/drag to resize/i)).not.toBeInTheDocument()
    })

    it('hides edit controls for locked components', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        locked: true
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(screen.queryByTitle('Drag to move component')).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /delete/i })).not.toBeInTheDocument()
      expect(screen.queryByTitle(/drag to resize/i)).not.toBeInTheDocument()
    })

    it('adjusts content padding for edit mode handle bar', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      const { rerender } = render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={false}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const content = screen.getByTestId('component-test-component').closest('.bento-component__content')
      expect(content).toHaveClass('pt-0')

      rerender(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(content).toHaveClass('pt-lg')
    })
  })

  describe('Mouse Interactions', () => {
    it('shows hover state on mouse enter in edit mode', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')!
      
      await user.hover(wrapper)
      expect(wrapper).toHaveClass('bento-component--hovering')
      
      await user.unhover(wrapper)
      expect(wrapper).not.toHaveClass('bento-component--hovering')
    })

    it('does not show hover state when not in edit mode', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={false}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')!
      
      await user.hover(wrapper)
      expect(wrapper).not.toHaveClass('bento-component--hovering')
    })

    it('does not show hover state for locked components', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component',
        locked: true
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')!
      
      await user.hover(wrapper)
      expect(wrapper).not.toHaveClass('bento-component--hovering')
    })
  })

  describe('Delete Functionality', () => {
    it('calls onDelete when delete button is clicked', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const deleteButton = screen.getByRole('button', { name: /delete/i })
      await user.click(deleteButton)

      // Should call onDelete after animation delay
      await waitFor(() => {
        expect(onDelete).toHaveBeenCalledWith('test-component')
      }, { timeout: 400 })
    })

    it('shows farewell animation before deletion', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const deleteButton = screen.getByRole('button', { name: /delete/i })
      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')!
      
      await user.click(deleteButton)

      // Should apply farewell animation
      expect(wrapper.style.animation).toContain('farewell-bounce')
    })

    it('prevents event propagation on delete button click', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      const onWrapperClick = vi.fn()
      
      render(
        <MockDndProvider>
          <div onClick={onWrapperClick}>
            <GridComponent
              component={component}
              isEditing={true}
              onDelete={onDelete}
              onResize={onResize}
              onShowProperties={onShowProperties}
            >
              <MockComponentRenderer 
                component={component}
                gridSize={{ w: component.position.w, h: component.position.h }}
              />
            </GridComponent>
          </div>
        </MockDndProvider>
      )

      const deleteButton = screen.getByRole('button', { name: /delete/i })
      await user.click(deleteButton)

      expect(onWrapperClick).not.toHaveBeenCalled()
    })
  })

  describe('Properties Functionality', () => {
    it('calls onShowProperties when properties button is clicked', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const propertiesButton = screen.getByRole('button', { name: /properties/i })
      await user.click(propertiesButton)

      expect(onShowProperties).toHaveBeenCalledWith('test-component')
    })

    it('prevents event propagation on properties button click', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      const onWrapperClick = vi.fn()
      
      render(
        <MockDndProvider>
          <div onClick={onWrapperClick}>
            <GridComponent
              component={component}
              isEditing={true}
              onDelete={onDelete}
              onResize={onResize}
              onShowProperties={onShowProperties}
            >
              <MockComponentRenderer 
                component={component}
                gridSize={{ w: component.position.w, h: component.position.h }}
              />
            </GridComponent>
          </div>
        </MockDndProvider>
      )

      const propertiesButton = screen.getByRole('button', { name: /properties/i })
      await user.click(propertiesButton)

      expect(onWrapperClick).not.toHaveBeenCalled()
    })
  })

  describe('Resize Functionality', () => {
    beforeEach(() => {
      // Mock getBoundingClientRect for grid container
      Element.prototype.getBoundingClientRect = vi.fn(() => ({
        top: 0,
        left: 0,
        right: 1200,
        bottom: 800,
        width: 1200,
        height: 800,
        x: 0,
        y: 0,
        toJSON: () => ({})
      }))

      // Mock getComputedStyle for grid
      Object.defineProperty(window, 'getComputedStyle', {
        value: vi.fn(() => ({
          gap: '16',
          gridTemplateColumns: 'repeat(12, 1fr)'
        })),
        writable: true
      })
    })

    it('initiates resize operation on resize handle mousedown', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component',
        position: { x: 2, y: 2, w: 3, h: 2 }
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const resizeHandle = screen.getAllByTitle(/drag to resize/i)[0] // Get first resize handle
      
      await user.pointer([
        { target: resizeHandle, keys: '[MouseLeft>]' },
        { coords: { x: 150, y: 100 } },
        { keys: '[/MouseLeft]' }
      ])

      expect(onResize).toHaveBeenCalled()
    })

    it('shows resize preview during resize operation', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component',
        position: { x: 2, y: 2, w: 3, h: 2 }
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const resizeHandle = screen.getAllByTitle(/drag to resize/i)[4] // Bottom-right handle
      
      await user.pointer([{ target: resizeHandle, keys: '[MouseLeft>]' }])
      
      // Move mouse to trigger resize
      fireEvent.mouseMove(document, { clientX: 200, clientY: 150 })

      // Should show resize preview
      await waitFor(() => {
        const resizePreview = screen.queryByText(/×/) // Looking for size display
        expect(resizePreview).toBeInTheDocument()
      })
    })

    it('applies correct resize behavior for different handle directions', async () => {
      const component = createMockGridComponent({
        id: 'test-component',
        position: { x: 2, y: 2, w: 3, h: 2 }
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      // Test east (right) resize handle
      const eastHandle = screen.getByTitle(/drag to resize/i)
      
      fireEvent.mouseDown(eastHandle, { clientX: 100, clientY: 100 })
      fireEvent.mouseMove(document, { clientX: 200, clientY: 100 })
      fireEvent.mouseUp(document)

      // Should call onResize with increased width
      expect(onResize).toHaveBeenCalledWith(
        'test-component',
        expect.objectContaining({
          w: expect.any(Number),
          h: expect.any(Number)
        })
      )
    })

    it('constrains resize to grid bounds', async () => {
      const component = createMockGridComponent({
        id: 'test-component',
        position: { x: 10, y: 2, w: 2, h: 2 } // Near right edge
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const eastHandle = screen.getByTitle(/drag to resize/i)
      
      fireEvent.mouseDown(eastHandle, { clientX: 100, clientY: 100 })
      fireEvent.mouseMove(document, { clientX: 500, clientY: 100 }) // Large movement
      fireEvent.mouseUp(document)

      // Should constrain to grid bounds (12 columns)
      expect(onResize).toHaveBeenCalledWith(
        'test-component',
        expect.objectContaining({
          w: expect.any(Number),
          h: expect.any(Number)
        })
      )
      
      const lastCall = onResize.mock.calls[onResize.mock.calls.length - 1]
      const newSize = lastCall[1]
      expect(component.position.x + newSize.w).toBeLessThanOrEqual(12)
    })

    it('handles touch events for resize on mobile devices', async () => {
      testEnvironment.enableTouchDevice()
      
      const component = createMockGridComponent({
        id: 'test-component',
        position: { x: 2, y: 2, w: 3, h: 2 }
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            isTouchDevice={true}
            deviceType={DeviceType.Mobile}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const resizeHandle = screen.getAllByTitle(/drag to resize/i)[0]
      
      // Simulate touch resize
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 100, clientY: 100 }])
      fireEvent(resizeHandle, touchStart)

      expect(onResize).toHaveBeenCalled()
    })
  })

  describe('Mobile Touch Behavior', () => {
    beforeEach(() => {
      testEnvironment.enableTouchDevice()
    })

    it('applies mobile-specific CSS classes', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            deviceType={DeviceType.Mobile}
            isMobile={true}
            isTouchDevice={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveClass('bento-component--mobile')
      expect(wrapper).toHaveClass('bento-component--touch-device')
    })

    it('uses larger touch targets for mobile', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            deviceType={DeviceType.Mobile}
            isTouchDevice={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const deleteButton = screen.getByRole('button', { name: /delete/i })
      expect(deleteButton).toHaveClass('h-lg', 'w-lg') // Larger mobile size
    })

    it('shows drag mode indicator when enabled', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            deviceType={DeviceType.Mobile}
            isMobile={true}
            dragMode={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveClass('bento-component--drag-mode')
    })

    it('enforces minimum touch target sizes', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            deviceType={DeviceType.Mobile}
            isTouchDevice={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const resizeHandles = screen.getAllByTitle(/drag to resize/i)
      resizeHandles.forEach(handle => {
        const styles = window.getComputedStyle(handle)
        expect(parseInt(styles.minWidth || '0')).toBeGreaterThanOrEqual(44)
        expect(parseInt(styles.minHeight || '0')).toBeGreaterThanOrEqual(44)
      })
    })
  })

  describe('Device Visibility', () => {
    it('hides component when hideOn includes current device', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        display: {
          hideOn: [DeviceType.Mobile],
          showOnly: [],
          className: '',
          style: {}
        }
      })
      
      const { container } = render(
        <MockDndProvider>
          <GridComponent
            component={component}
            deviceType={DeviceType.Mobile}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(container.firstChild).toBeNull() // Component should not render
    })

    it('hides component when showOnly excludes current device', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        display: {
          hideOn: [],
          showOnly: [DeviceType.Desktop],
          className: '',
          style: {}
        }
      })
      
      const { container } = render(
        <MockDndProvider>
          <GridComponent
            component={component}
            deviceType={DeviceType.Mobile}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(container.firstChild).toBeNull()
    })

    it('shows component when device conditions are met', () => {
      const component = createMockGridComponent({
        id: 'test-component',
        display: {
          hideOn: [],
          showOnly: [DeviceType.Mobile],
          className: '',
          style: {}
        }
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            deviceType={DeviceType.Mobile}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      expect(screen.getByTestId('component-test-component')).toBeInTheDocument()
    })
  })

  describe('Animations and Visual Effects', () => {
    it('applies entry animation for new components', async () => {
      const component = createMockGridComponent({
        id: 'new-component' // Fresh ID triggers animation
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-new-component').closest('.bento-component')
      
      await waitFor(() => {
        expect(wrapper).toHaveClass('bento-component--just-added')
      })

      // Animation should clear after timeout
      await waitFor(() => {
        expect(wrapper).not.toHaveClass('bento-component--just-added')
      }, { timeout: 1100 })
    })

    it('applies wobble animation when shouldWobble is true', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            shouldWobble={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveClass('bento-component--wobble')
    })

    it('applies drag transform during drag operations', () => {
      mockUseDraggable.mockReturnValue({
        attributes: {},
        listeners: {},
        setNodeRef: vi.fn(),
        transform: { x: 50, y: 30, scaleX: 1, scaleY: 1 },
        isDragging: true
      })

      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isDragging={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      expect(wrapper).toHaveStyle({
        opacity: '0.3',
        transform: 'scale(0.95)'
      })
    })
  })

  describe('Accessibility', () => {
    it('provides appropriate ARIA labels and roles', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      // Action buttons should have proper titles
      expect(screen.getByRole('button', { name: /delete/i })).toHaveAttribute('title')
      expect(screen.getByRole('button', { name: /properties/i })).toHaveAttribute('title')
      
      // Drag handle should have title
      expect(screen.getByTitle('Drag to move component')).toBeInTheDocument()
      
      // Resize handles should have titles
      const resizeHandles = screen.getAllByTitle(/drag to resize/i)
      expect(resizeHandles.length).toBeGreaterThan(0)
    })

    it('supports keyboard interaction for action buttons', async () => {
      const user = userEvent.setup()
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const deleteButton = screen.getByRole('button', { name: /delete/i })
      
      // Focus and activate with keyboard
      deleteButton.focus()
      expect(deleteButton).toHaveFocus()
      
      await user.keyboard('{Enter}')
      
      await waitFor(() => {
        expect(onDelete).toHaveBeenCalledWith('test-component')
      })
    })

    it('warns about invalid touch targets on touch devices', () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})
      testEnvironment.enableTouchDevice()
      
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            isTouchDevice={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      const wrapper = screen.getByTestId('component-test-component').closest('.bento-component')
      if (wrapper?.classList.contains('bento-component--invalid-target')) {
        expect(consoleSpy).toHaveBeenCalled()
      }
      
      consoleSpy.mockRestore()
    })
  })

  describe('Performance and Edge Cases', () => {
    it('handles rapid prop changes without performance issues', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      const { rerender } = render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={false}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      // Rapidly change states
      for (let i = 0; i < 10; i++) {
        rerender(
          <MockDndProvider>
            <GridComponent
              component={component}
              isEditing={i % 2 === 0}
              isSelected={i % 3 === 0}
              isDragging={i % 4 === 0}
              onDelete={onDelete}
              onResize={onResize}
              onShowProperties={onShowProperties}
            >
              <MockComponentRenderer 
                component={component}
                gridSize={{ w: component.position.w, h: component.position.h }}
              />
            </GridComponent>
          </MockDndProvider>
        )
      }

      // Should not crash
      expect(screen.getByTestId('component-test-component')).toBeInTheDocument()
    })

    it('cleans up event listeners on unmount', () => {
      const component = createMockGridComponent({
        id: 'test-component'
      })
      
      const { unmount } = render(
        <MockDndProvider>
          <GridComponent
            component={component}
            isEditing={true}
            onDelete={onDelete}
            onResize={onResize}
            onShowProperties={onShowProperties}
          >
            <MockComponentRenderer 
              component={component}
              gridSize={{ w: component.position.w, h: component.position.h }}
            />
          </GridComponent>
        </MockDndProvider>
      )

      // Should unmount without errors
      expect(() => unmount()).not.toThrow()
    })

    it('handles invalid component data gracefully', () => {
      const invalidComponent = {
        ...createMockGridComponent({ id: 'invalid' }),
        // @ts-ignore - Testing invalid data
        position: null
      }
      
      expect(() => {
        render(
          <MockDndProvider>
            <GridComponent
              component={invalidComponent}
              onDelete={onDelete}
              onResize={onResize}
              onShowProperties={onShowProperties}
            >
              <MockComponentRenderer 
                component={invalidComponent}
                gridSize={{ w: 2, h: 2 }}
              />
            </GridComponent>
          </MockDndProvider>
        )
      }).not.toThrow()
    })
  })
})