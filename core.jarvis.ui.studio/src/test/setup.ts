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
class MockIntersectionObserver {
  root: Element | null = null
  rootMargin: string = '0px'
  thresholds: ReadonlyArray<number> = []
  
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
  takeRecords(): IntersectionObserverEntry[] { return [] }
}
(global as any).IntersectionObserver = MockIntersectionObserver

// Mock ResizeObserver  
class MockResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
}
(global as any).ResizeObserver = MockResizeObserver

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
if (typeof HTMLElement !== 'undefined') {
  Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
    value: () => {},
    writable: true,
  })
}

// Mock PointerEvent for drag and drop tests
class MockPointerEvent extends Event {
  pointerId: number = 0
  width: number = 0
  height: number = 0
  pressure: number = 0
  tangentialPressure: number = 0
  tiltX: number = 0
  tiltY: number = 0
  twist: number = 0
  pointerType: string = 'mouse'
  isPrimary: boolean = true
  altitudeAngle?: number
  azimuthAngle?: number
  clientX: number = 0
  clientY: number = 0
  pageX: number = 0
  pageY: number = 0
  screenX: number = 0
  screenY: number = 0
  movementX: number = 0
  movementY: number = 0
  offsetX: number = 0
  offsetY: number = 0
  x: number = 0
  y: number = 0
  button: number = 0
  buttons: number = 0
  ctrlKey: boolean = false
  shiftKey: boolean = false
  altKey: boolean = false
  metaKey: boolean = false
  relatedTarget: EventTarget | null = null

  constructor(type: string, eventInitDict: any = {}) {
    super(type, eventInitDict)
    Object.assign(this, eventInitDict)
  }

  getCoalescedEvents() { return [] }
  getPredictedEvents() { return [] }
}
(global as any).PointerEvent = MockPointerEvent

// Mock DragEvent for drag and drop tests
class MockDragEvent extends Event {
  dataTransfer: DataTransfer | null = null
  altKey: boolean = false
  button: number = 0
  buttons: number = 0
  clientX: number = 0
  clientY: number = 0
  ctrlKey: boolean = false
  metaKey: boolean = false
  pageX: number = 0
  pageY: number = 0
  screenX: number = 0
  screenY: number = 0
  shiftKey: boolean = false
  x: number = 0
  y: number = 0
  offsetX: number = 0
  offsetY: number = 0
  movementX: number = 0
  movementY: number = 0
  relatedTarget: EventTarget | null = null

  constructor(type: string, eventInitDict: any = {}) {
    super(type, eventInitDict)
    Object.assign(this, eventInitDict)
  }

  getModifierState() { return false }
  initMouseEvent() {}
}
(global as any).DragEvent = MockDragEvent

// Mock DataTransfer for drag and drop tests
class MockDataTransfer {
  dropEffect: 'none' | 'copy' | 'move' | 'link' = 'none'
  effectAllowed: string = 'uninitialized'
  files: FileList = Object.assign([], { item: () => null, length: 0 }) as FileList
  items: DataTransferItemList = Object.assign([], { 
    add: () => null, 
    clear: () => {}, 
    remove: () => {},
    length: 0
  }) as DataTransferItemList
  types: string[] = []

  clearData() {}
  getData() { return '' }
  setData() {}
  setDragImage() {}
}
(global as any).DataTransfer = MockDataTransfer

// Set environment variables for testing
process.env.NODE_ENV = 'test'