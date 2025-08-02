import '@testing-library/jest-dom'
import { cleanup } from '@testing-library/react'
import { afterEach, beforeAll, afterAll } from 'vitest'
import { server } from './mocks/server'

// Setup MSW server
beforeAll(() => {
  server.listen({ onUnhandledRequest: 'warn' })
})

// Cleanup after each test case (e.g. clearing jsdom)
afterEach(() => {
  cleanup()
  server.resetHandlers()
})

// Clean up MSW server after all tests
afterAll(() => {
  server.close()
})

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
}

// Mock ResizeObserver  
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
}

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => {},
  }),
})

// Mock scrollTo
Object.defineProperty(window, 'scrollTo', {
  value: () => {},
  writable: true,
})

// Mock HTMLElement.scrollIntoView
Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
  value: () => {},
  writable: true,
})

// Mock PointerEvent for drag and drop tests
global.PointerEvent = class PointerEvent extends Event {
  pointerId: number
  width: number
  height: number
  pressure: number
  tangentialPressure: number
  tiltX: number
  tiltY: number
  twist: number
  pointerType: string
  isPrimary: boolean

  constructor(type: string, eventInitDict: any = {}) {
    super(type, eventInitDict)
    this.pointerId = eventInitDict.pointerId || 0
    this.width = eventInitDict.width || 0
    this.height = eventInitDict.height || 0
    this.pressure = eventInitDict.pressure || 0
    this.tangentialPressure = eventInitDict.tangentialPressure || 0
    this.tiltX = eventInitDict.tiltX || 0
    this.tiltY = eventInitDict.tiltY || 0
    this.twist = eventInitDict.twist || 0
    this.pointerType = eventInitDict.pointerType || ''
    this.isPrimary = eventInitDict.isPrimary || false
  }
}

// Mock DragEvent for drag and drop tests
global.DragEvent = class DragEvent extends Event {
  dataTransfer: DataTransfer | null

  constructor(type: string, eventInitDict: any = {}) {
    super(type, eventInitDict)
    this.dataTransfer = eventInitDict.dataTransfer || null
  }
}

// Mock DataTransfer for drag and drop tests
global.DataTransfer = class DataTransfer {
  dropEffect: string = 'none'
  effectAllowed: string = 'uninitialized'
  files: FileList = new FileList()
  items: DataTransferItemList = {} as DataTransferItemList
  types: string[] = []

  clearData() {}
  getData() { return '' }
  setData() {}
  setDragImage() {}
}

// Set environment variables for testing
process.env.NODE_ENV = 'test'