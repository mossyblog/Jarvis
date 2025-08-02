import { setupWorker } from 'msw/browser'
import { handlers } from './handlers'

// Setup MSW worker for browser environment (development/storybook)
export const worker = setupWorker(...handlers)

// Start the worker for browser environment
export const startMocking = async () => {
  if (typeof window !== 'undefined') {
    await worker.start({
      onUnhandledRequest: 'warn',
    })
  }
}