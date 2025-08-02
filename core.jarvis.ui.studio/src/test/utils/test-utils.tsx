import React, { ReactElement } from 'react'
import { render, RenderOptions, beforeEach, afterEach } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { vi } from 'vitest'

// Custom render function that includes providers
const AllTheProviders = ({ children }: { children: React.ReactNode }) => {
  return (
    <BrowserRouter>
      {children}
    </BrowserRouter>
  )
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllTheProviders, ...options })

export * from '@testing-library/react'
export { customRender as render }

// Common test utilities
export const createMockComponent = (name: string) => {
  const MockComponent = ({ children, ...props }: any) => (
    <div data-testid={`mock-${name.toLowerCase()}`} {...props}>
      {children}
    </div>
  )
  MockComponent.displayName = `Mock${name}`
  return MockComponent
}

export const mockConsole = () => {
  const originalConsole = { ...console }
  beforeEach(() => {
    console.error = vi.fn()
    console.warn = vi.fn()
    console.log = vi.fn()
  })
  afterEach(() => {
    Object.assign(console, originalConsole)
  })
}

export const sleep = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

// Mock drag and drop events
export const createDragEvent = (type: string, dataTransfer?: Partial<DataTransfer>) => {
  const event = new DragEvent(type, {
    bubbles: true,
    cancelable: true,
    dataTransfer: {
      dropEffect: 'move',
      effectAllowed: 'all',
      files: new FileList(),
      items: {} as DataTransferItemList,
      types: [],
      clearData: vi.fn(),
      getData: vi.fn(() => ''),
      setData: vi.fn(),
      setDragImage: vi.fn(),
      ...dataTransfer,
    },
  })
  return event
}

export const createPointerEvent = (type: string, options: Partial<PointerEvent> = {}) => {
  return new PointerEvent(type, {
    bubbles: true,
    cancelable: true,
    pointerId: 1,
    ...options,
  })
}