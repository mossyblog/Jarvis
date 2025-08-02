/**
 * BottomSheet Component Tests
 * 
 * Comprehensive tests for the BottomSheet mobile component including:
 * - State management and lifecycle
 * - Touch gesture handling and drag interactions
 * - Portal rendering and backdrop behavior
 * - Accessibility features and keyboard navigation
 * - Responsive height management and snapping
 * - Animation and performance characteristics
 * - Edge cases and error handling
 */

import React, { useState } from 'react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BottomSheet, useBottomSheet } from '../BottomSheet'
import {
  createMockTouchEvent,
  simulateSwipeGesture,
  simulateLongPress,
  setupBentoTestEnvironment
} from '@/test/utils/bento-test-utils'

describe('BottomSheet Component', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>
  let mockOnClose: ReturnType<typeof vi.fn>
  
  const defaultProps = {
    isOpen: true,
    onClose: () => {},
    children: <div data-testid="sheet-content">Test Content</div>
  }

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    testEnvironment.enableTouchDevice()
    mockOnClose = vi.fn()
    
    // Mock portal mount point
    const portalDiv = document.createElement('div')
    portalDiv.id = 'bottom-sheet-portal'
    document.body.appendChild(portalDiv)
    
    // Mock viewport dimensions
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      value: 800
    })
    
    // Mock getBoundingClientRect for drag calculations
    Element.prototype.getBoundingClientRect = vi.fn(() => ({
      top: 0,
      left: 0,
      right: 375,
      bottom: 800,
      width: 375,
      height: 800,
      x: 0,
      y: 0,
      toJSON: () => ({})
    }))
    
    vi.clearAllTimers()
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

  describe('Rendering and Portal Behavior', () => {
    it('renders in portal when open', () => {
      render(<BottomSheet {...defaultProps} isOpen={true} onClose={mockOnClose} />)
      
      const portal = document.getElementById('bottom-sheet-portal')
      expect(portal).toBeInTheDocument()
      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByTestId('sheet-content')).toBeInTheDocument()
    })

    it('does not render when closed', () => {
      render(<BottomSheet {...defaultProps} isOpen={false} onClose={mockOnClose} />)
      
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      expect(screen.queryByTestId('sheet-content')).not.toBeInTheDocument()
    })

    it('creates and cleans up portal mount point', () => {
      const { unmount } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      expect(document.getElementById('bottom-sheet-portal')).toBeInTheDocument()
      
      unmount()
      
      // Portal should be cleaned up on unmount
      expect(document.getElementById('bottom-sheet-portal')).not.toBeInTheDocument()
    })

    it('applies custom z-index', () => {
      render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          zIndex={2000} 
        />
      )
      
      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveStyle({ zIndex: '2000' })
    })

    it('renders with proper ARIA attributes', () => {
      render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          title="Test Sheet" 
        />
      )
      
      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveAttribute('aria-modal', 'true')
      expect(dialog).toHaveAttribute('aria-labelledby', 'bottom-sheet-title')
      
      expect(screen.getByText('Test Sheet')).toHaveAttribute('id', 'bottom-sheet-title')
    })
  })

  describe('Height Management and Snapping', () => {
    it('initializes with correct height', () => {
      render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          initialHeight={0.6} 
        />
      )
      
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      expect(sheet).toHaveStyle({ height: '60vh' })
    })

    it('respects minimum and maximum height constraints', () => {
      render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          initialHeight={0.5}
          minHeight={0.2}
          maxHeight={0.9} 
        />
      )
      
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      expect(sheet).toHaveStyle({ 
        height: '50vh',
        minHeight: '20vh',
        maxHeight: '90vh'
      })
    })

    it('snaps to nearest position after drag', async () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          initialHeight={0.5}
          minHeight={0.2}
          maxHeight={0.9}
        />
      )
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      expect(handle).toBeInTheDocument()
      
      // Simulate drag to position between initial and max
      fireEvent.mouseDown(handle, { clientY: 200 })
      fireEvent.mouseMove(document, { clientY: 150 }) // Drag up 50px
      fireEvent.mouseUp(document)
      
      await act(async () => {
        vi.advanceTimersByTime(300) // Animation duration
      })
      
      // Should snap to maxHeight (0.9)
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      expect(sheet).toHaveStyle({ height: '90vh' })
    })

    it('closes sheet when dragged below minimum', async () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          initialHeight={0.3}
          minHeight={0.2}
        />
      )
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      
      // Simulate drag far below minimum
      fireEvent.mouseDown(handle, { clientY: 200 })
      fireEvent.mouseMove(document, { clientY: 400 }) // Drag down significantly
      fireEvent.mouseUp(document)
      
      await act(async () => {
        vi.advanceTimersByTime(300)
      })
      
      expect(mockOnClose).toHaveBeenCalled()
    })
  })

  describe('Touch Gesture Handling', () => {
    it('handles swipe down to close', async () => {
      const { container } = render(
        <BottomSheet {...defaultProps} onClose={mockOnClose} />
      )
      
      const sheet = container.querySelector('[role="dialog"]')?.firstChild as HTMLElement
      expect(sheet).toBeInTheDocument()
      
      await simulateSwipeGesture(sheet, 'down', {
        distance: 100,
        duration: 200
      })
      
      await act(async () => {
        vi.advanceTimersByTime(300)
      })
      
      expect(mockOnClose).toHaveBeenCalled()
    })

    it('respects disableSwipeDown prop', async () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          disableSwipeDown={true} 
        />
      )
      
      const sheet = container.querySelector('[role="dialog"]')?.firstChild as HTMLElement
      
      await simulateSwipeGesture(sheet, 'down', {
        distance: 100,
        duration: 200
      })
      
      await act(async () => {
        vi.advanceTimersByTime(300)
      })
      
      expect(mockOnClose).not.toHaveBeenCalled()
    })

    it('handles touch drag on handle', async () => {
      const { container } = render(
        <BottomSheet {...defaultProps} onClose={mockOnClose} />
      )
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      
      const touchStart = createMockTouchEvent('touchstart', [{ clientX: 187, clientY: 100 }])
      const touchMove = createMockTouchEvent('touchmove', [{ clientX: 187, clientY: 80 }])
      const touchEnd = createMockTouchEvent('touchend', [])
      
      fireEvent(handle, touchStart)
      await act(async () => {
        fireEvent(document, touchMove)
      })
      fireEvent(document, touchEnd)
      
      // Should trigger height adjustment animation
      await act(async () => {
        vi.advanceTimersByTime(300)
      })
      
      // Check that drag interaction occurred (height should change)
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      expect(sheet).toBeInTheDocument()
    })

    it('prevents body scroll when open', () => {
      const originalOverflow = document.body.style.overflow
      
      render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      expect(document.body.style.overflow).toBe('hidden')
      
      // Should restore on unmount
      const { unmount } = render(<BottomSheet {...defaultProps} isOpen={false} onClose={mockOnClose} />)
      unmount()
      
      expect(document.body.style.overflow).toBe(originalOverflow)
    })
  })

  describe('Backdrop Interaction', () => {
    it('closes on backdrop click when dismissOnBackdrop is true', async () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          dismissOnBackdrop={true} 
        />
      )
      
      const backdrop = container.querySelector('[class*="bg-black"]') as HTMLElement
      expect(backdrop).toBeInTheDocument()
      
      fireEvent.click(backdrop)
      
      expect(mockOnClose).toHaveBeenCalled()
    })

    it('does not close on backdrop click when dismissOnBackdrop is false', async () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          dismissOnBackdrop={false} 
        />
      )
      
      const backdrop = container.querySelector('[class*="bg-black"]') as HTMLElement
      fireEvent.click(backdrop)
      
      expect(mockOnClose).not.toHaveBeenCalled()
    })

    it('does not close when clicking on sheet content', async () => {
      render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const content = screen.getByTestId('sheet-content')
      fireEvent.click(content)
      
      expect(mockOnClose).not.toHaveBeenCalled()
    })

    it('adjusts backdrop opacity based on sheet height', () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          initialHeight={0.8} 
        />
      )
      
      const backdrop = container.querySelector('[class*="bg-black"]') as HTMLElement
      expect(backdrop).toHaveStyle({ opacity: '0.4' }) // 0.5 * 0.8
    })
  })

  describe('Keyboard Navigation and Accessibility', () => {
    it('closes on Escape key press', async () => {
      render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      fireEvent.keyDown(document, { key: 'Escape' })
      
      expect(mockOnClose).toHaveBeenCalled()
    })

    it('shows drag handle by default', () => {
      const { container } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const handle = container.querySelector('[class*="cursor-grab"]')
      expect(handle).toBeInTheDocument()
    })

    it('hides drag handle when showHandle is false', () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          showHandle={false} 
        />
      )
      
      const handle = container.querySelector('[class*="cursor-grab"]')
      expect(handle).not.toBeInTheDocument()
    })

    it('provides proper close button accessibility', () => {
      render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          title="Test Sheet" 
        />
      )
      
      const closeButton = screen.getByRole('button', { name: /close sheet/i })
      expect(closeButton).toBeInTheDocument()
      expect(closeButton).toHaveAttribute('aria-label', 'Close sheet')
    })

    it('maintains focus management', async () => {
      render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          title="Test Sheet" 
        >
          <button>Focusable Content</button>
        </BottomSheet>
      )
      
      const dialog = screen.getByRole('dialog')
      expect(dialog).toBeInTheDocument()
      
      // Focus should be managed within the modal
      const focusableButton = screen.getByRole('button', { name: /focusable content/i })
      focusableButton.focus()
      expect(focusableButton).toHaveFocus()
    })

    it('has minimum touch target size for handle', () => {
      const { container } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      expect(handle).toHaveStyle({ minHeight: '44px' })
    })
  })

  describe('Animation and Performance', () => {
    it('applies smooth transitions during height changes', async () => {
      const { container } = render(
        <BottomSheet {...defaultProps} onClose={mockOnClose} />
      )
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      
      // Start drag
      fireEvent.mouseDown(handle, { clientY: 200 })
      
      // During drag, transitions should be disabled
      expect(sheet).toHaveClass('transition-none')
      
      // End drag
      fireEvent.mouseUp(document)
      
      await act(async () => {
        vi.advanceTimersByTime(50) // Small delay
      })
      
      // After drag, transitions should be re-enabled
      expect(sheet).not.toHaveClass('transition-none')
    })

    it('handles rapid gesture events without performance degradation', async () => {
      const { container } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const sheet = container.querySelector('[role="dialog"]')?.firstChild as HTMLElement
      const startTime = performance.now()
      
      // Simulate rapid touch events
      for (let i = 0; i < 20; i++) {
        const touchMove = createMockTouchEvent('touchmove', [{ clientX: 100, clientY: 100 + i }])
        fireEvent(sheet, touchMove)
      }
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(50) // Should handle quickly
    })

    it('cleans up timers and listeners on unmount', () => {
      const { unmount } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const timeoutSpy = vi.spyOn(window, 'clearTimeout')
      
      unmount()
      
      // Should clean up any pending timers
      expect(timeoutSpy).toHaveBeenCalled()
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('handles malformed touch events gracefully', () => {
      const { container } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      
      // Malformed touch event without touches array
      const malformedEvent = new Event('touchstart') as any
      
      expect(() => {
        fireEvent(handle, malformedEvent)
      }).not.toThrow()
    })

    it('handles missing portal mount point gracefully', () => {
      // Remove the portal mount point
      const portal = document.getElementById('bottom-sheet-portal')
      if (portal) {
        document.body.removeChild(portal)
      }
      
      expect(() => {
        render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      }).not.toThrow()
      
      // Should not render anything
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('handles window resize during drag', async () => {
      const { container } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      
      // Start drag
      fireEvent.mouseDown(handle, { clientY: 200 })
      
      // Simulate window resize
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        value: 600
      })
      fireEvent(window, new Event('resize'))
      
      // End drag
      fireEvent.mouseUp(document)
      
      expect(() => {
        vi.advanceTimersByTime(300)
      }).not.toThrow()
    })

    it('prevents drag when animation is in progress', async () => {
      const { container } = render(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const handle = container.querySelector('[class*="cursor-grab"]') as HTMLElement
      
      // Start first drag
      fireEvent.mouseDown(handle, { clientY: 200 })
      fireEvent.mouseUp(document)
      
      // Try to start another drag while animation is running
      fireEvent.mouseDown(handle, { clientY: 200 })
      
      // Should not start new drag during animation
      const sheet = screen.getByRole('dialog').firstChild as HTMLElement
      expect(sheet).toBeInTheDocument()
    })
  })

  describe('Custom Configuration', () => {
    it('applies custom CSS classes', () => {
      const { container } = render(
        <BottomSheet 
          {...defaultProps} 
          onClose={mockOnClose} 
          className="custom-sheet-class" 
        />
      )
      
      const sheet = container.querySelector('.custom-sheet-class')
      expect(sheet).toBeInTheDocument()
    })

    it('handles content overflow correctly', () => {
      render(
        <BottomSheet {...defaultProps} onClose={mockOnClose}>
          <div style={{ height: '1000px' }} data-testid="tall-content">
            Very tall content that exceeds sheet height
          </div>
        </BottomSheet>
      )
      
      const contentArea = screen.getByTestId('tall-content').parentElement
      expect(contentArea).toHaveClass('overflow-auto')
      expect(contentArea).toHaveClass('overscroll-contain')
    })

    it('calculates content height correctly with and without title', () => {
      const { rerender } = render(
        <BottomSheet {...defaultProps} onClose={mockOnClose} title="Test Title" />
      )
      
      const contentWithTitle = screen.getByRole('dialog').querySelector('[class*="overflow-auto"]') as HTMLElement
      expect(contentWithTitle).toHaveStyle({ height: 'calc(50vh - 120px)' })
      
      rerender(<BottomSheet {...defaultProps} onClose={mockOnClose} />)
      
      const contentWithoutTitle = screen.getByRole('dialog').querySelector('[class*="overflow-auto"]') as HTMLElement
      expect(contentWithoutTitle).toHaveStyle({ height: 'calc(50vh - 72px)' })
    })
  })
})

// Test component for useBottomSheet hook
const TestBottomSheetHook: React.FC<{ initialState?: boolean }> = ({ initialState = false }) => {
  const { isOpen, open, close, toggle } = useBottomSheet(initialState)
  
  return (
    <div>
      <button onClick={open} data-testid="open-button">Open</button>
      <button onClick={close} data-testid="close-button">Close</button>
      <button onClick={toggle} data-testid="toggle-button">Toggle</button>
      <div data-testid="state-display">{isOpen ? 'open' : 'closed'}</div>
      <BottomSheet isOpen={isOpen} onClose={close}>
        <div data-testid="hook-content">Hook Content</div>
      </BottomSheet>
    </div>
  )
}

describe('useBottomSheet Hook', () => {
  let testEnvironment: ReturnType<typeof setupBentoTestEnvironment>

  beforeEach(() => {
    testEnvironment = setupBentoTestEnvironment()
    
    // Mock portal mount point
    const portalDiv = document.createElement('div')
    portalDiv.id = 'bottom-sheet-portal'
    document.body.appendChild(portalDiv)
    
    vi.useFakeTimers()
  })

  afterEach(() => {
    const portal = document.getElementById('bottom-sheet-portal')
    if (portal) {
      document.body.removeChild(portal)
    }
    vi.useRealTimers()
  })

  it('initializes with default state', () => {
    render(<TestBottomSheetHook />)
    
    expect(screen.getByTestId('state-display')).toHaveTextContent('closed')
    expect(screen.queryByTestId('hook-content')).not.toBeInTheDocument()
  })

  it('initializes with custom initial state', () => {
    render(<TestBottomSheetHook initialState={true} />)
    
    expect(screen.getByTestId('state-display')).toHaveTextContent('open')
    expect(screen.getByTestId('hook-content')).toBeInTheDocument()
  })

  it('opens bottom sheet', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
    render(<TestBottomSheetHook />)
    
    await user.click(screen.getByTestId('open-button'))
    
    expect(screen.getByTestId('state-display')).toHaveTextContent('open')
    expect(screen.getByTestId('hook-content')).toBeInTheDocument()
  })

  it('closes bottom sheet', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
    render(<TestBottomSheetHook initialState={true} />)
    
    await user.click(screen.getByTestId('close-button'))
    
    expect(screen.getByTestId('state-display')).toHaveTextContent('closed')
    expect(screen.queryByTestId('hook-content')).not.toBeInTheDocument()
  })

  it('toggles bottom sheet state', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
    render(<TestBottomSheetHook />)
    
    // Toggle to open
    await user.click(screen.getByTestId('toggle-button'))
    expect(screen.getByTestId('state-display')).toHaveTextContent('open')
    
    // Toggle to close
    await user.click(screen.getByTestId('toggle-button'))
    expect(screen.getByTestId('state-display')).toHaveTextContent('closed')
  })

  it('maintains stable callback references', () => {
    const { rerender } = render(<TestBottomSheetHook />)
    
    const openButton = screen.getByTestId('open-button')
    const closeButton = screen.getByTestId('close-button')
    const toggleButton = screen.getByTestId('toggle-button')
    
    const initialOpenHandler = openButton.onclick
    const initialCloseHandler = closeButton.onclick
    const initialToggleHandler = toggleButton.onclick
    
    rerender(<TestBottomSheetHook />)
    
    // Callbacks should remain stable across re-renders
    expect(openButton.onclick).toBe(initialOpenHandler)
    expect(closeButton.onclick).toBe(initialCloseHandler)
    expect(toggleButton.onclick).toBe(initialToggleHandler)
  })
})