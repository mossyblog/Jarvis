# Testing Infrastructure Setup

This document outlines the comprehensive testing infrastructure set up for the React application.

## 🧪 Testing Stack

- **Test Runner**: [Vitest](https://vitest.dev/) - Fast Vite-native test runner
- **Testing Library**: [@testing-library/react](https://testing-library.com/docs/react-testing-library/intro/) - Simple and complete React testing utilities
- **DOM Environment**: [jsdom](https://github.com/jsdom/jsdom) - JavaScript implementation of WHATWG DOM and HTML standards
- **User Interactions**: [@testing-library/user-event](https://testing-library.com/docs/user-events/intro/) - Advanced user interaction simulation
- **API Mocking**: [MSW (Mock Service Worker)](https://mswjs.io/) - API mocking by intercepting requests on the network level
- **Assertions**: [@testing-library/jest-dom](https://testing-library.com/docs/ecosystem-jest-dom/) - Custom Jest matchers for DOM elements

## 📁 Directory Structure

```
src/
├── test/
│   ├── setup.ts                 # Global test setup and configuration
│   ├── utils/
│   │   ├── test-utils.tsx       # Custom render function and test utilities
│   │   └── mock-data.ts         # Mock data generators and helpers
│   ├── mocks/
│   │   ├── handlers.ts          # MSW request handlers
│   │   ├── server.ts            # MSW server setup for Node.js (tests)
│   │   └── browser.ts           # MSW worker setup for browser (dev/storybook)
│   └── fixtures/
│       ├── users.ts             # User-related test data
│       └── components.ts        # Component-related test data
├── components/
│   └── **/*.test.{ts,tsx}       # Component tests
├── pages/
│   └── **/*.test.{ts,tsx}       # Page/integration tests
└── hooks/
    └── **/*.test.{ts,tsx}       # Custom hook tests
```

## 🚀 Getting Started

### Running Tests

```bash
# Run tests in watch mode (development)
npm run test

# Run tests once
npm run test:run

# Run tests with UI (browser interface)
npm run test:ui

# Run tests with coverage report
npm run test:coverage

# Run tests in watch mode
npm run test:watch
```

### Writing Tests

#### 1. Component Tests

```tsx
import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils/test-utils'
import userEvent from '@testing-library/user-event'
import { MyComponent } from './MyComponent'

describe('MyComponent', () => {
  it('renders correctly', () => {
    render(<MyComponent title="Test" />)
    expect(screen.getByText('Test')).toBeInTheDocument()
  })

  it('handles user interactions', async () => {
    const user = userEvent.setup()
    const mockCallback = vi.fn()
    
    render(<MyComponent onClick={mockCallback} />)
    
    await user.click(screen.getByRole('button'))
    expect(mockCallback).toHaveBeenCalledTimes(1)
  })
})
```

#### 2. Integration Tests with API Mocking

```tsx
import React from 'react'
import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { UserList } from './UserList'

describe('UserList', () => {
  beforeEach(() => {
    // Reset any overridden handlers
    server.resetHandlers()
  })

  it('loads and displays users', async () => {
    render(<UserList />)
    
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })
  })

  it('handles API errors', async () => {
    // Override the default handler for this test
    server.use(
      http.get('/api/users', () => {
        return HttpResponse.json(
          { error: 'Server error' },
          { status: 500 }
        )
      })
    )

    render(<UserList />)
    
    await waitFor(() => {
      expect(screen.getByText(/error loading users/i)).toBeInTheDocument()
    })
  })
})
```

#### 3. Custom Hook Tests

```tsx
import { describe, it, expect } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useCounter } from './useCounter'

describe('useCounter', () => {
  it('increments counter', () => {
    const { result } = renderHook(() => useCounter())
    
    act(() => {
      result.current.increment()
    })
    
    expect(result.current.count).toBe(1)
  })
})
```

## 🔧 Configuration Files

### Vitest Configuration (`vitest.config.ts`)

- Extends Vite configuration for seamless integration
- Configures jsdom environment for DOM testing
- Sets up code coverage with V8 provider
- Includes custom path aliases (@/* imports)

### Test Setup (`src/test/setup.ts`)

- Imports jest-dom matchers for enhanced assertions
- Sets up MSW server for all tests
- Mocks browser APIs (IntersectionObserver, ResizeObserver, etc.)
- Configures drag and drop event mocks

### Custom Test Utils (`src/test/utils/test-utils.tsx`)

- Wraps React Testing Library's render with providers (Router, etc.)
- Provides utility functions for creating mock components
- Includes helpers for drag and drop testing

## 🌐 API Mocking with MSW

### Request Handlers (`src/test/mocks/handlers.ts`)

Defines mock responses for:
- Authentication endpoints
- User management
- Dashboard statistics
- Network monitoring
- Issue tracking

### Server Setup

- **Node.js (Tests)**: Uses `setupServer` from `msw/node`
- **Browser (Dev/Storybook)**: Uses `setupWorker` from `msw/browser`

### Custom Mock Responses

```tsx
// Override a handler for a specific test
server.use(
  http.get('/api/users', () => {
    return HttpResponse.json([
      { id: '1', name: 'Custom User' }
    ])
  })
)
```

## 📊 Coverage Configuration

Coverage is configured to:
- Use V8 provider for accurate coverage
- Generate HTML, text, and JSON reports
- Exclude test files, config files, and stories
- Set minimum thresholds (80% for all metrics)

### Coverage Reports

```bash
npm run test:coverage
```

Reports are generated in the `coverage/` directory:
- `coverage/index.html` - Visual HTML report
- `coverage/coverage-final.json` - Raw coverage data

## 🎯 Best Practices

### 1. Test Organization

- Group related tests using `describe` blocks
- Use descriptive test names that explain behavior
- Follow AAA pattern: Arrange, Act, Assert

### 2. Component Testing

- Test behavior, not implementation
- Use semantic queries (getByRole, getByLabelText)
- Mock external dependencies
- Test user interactions

### 3. Async Testing

- Use `waitFor` for asynchronous operations
- Prefer `findBy*` queries for elements that appear asynchronously
- Always clean up timers and subscriptions

### 4. Mock Management

- Use `vi.clearAllMocks()` in `beforeEach` for clean test state
- Reset MSW handlers between tests
- Mock only what's necessary for the test

### 5. Data Fixtures

- Use fixtures for consistent test data
- Create factory functions for generating test data
- Keep fixtures focused and minimal

## 🐛 Debugging Tests

### Visual Debugging

```tsx
// Add screen.debug() to see current DOM state
render(<MyComponent />)
screen.debug() // Prints DOM to console
```

### Test UI

```bash
npm run test:ui
```

Opens a browser interface for:
- Running individual tests
- Viewing test results
- Debugging failed tests

### Coverage Analysis

Check `coverage/index.html` to:
- Identify untested code paths
- View line-by-line coverage
- Analyze branch coverage

## 🔄 Continuous Integration

The test setup is CI-ready with:
- Deterministic test execution
- No external dependencies
- Comprehensive coverage reporting
- Fast execution with Vitest

### CI Commands

```yaml
# In your CI pipeline
- run: npm test:run
- run: npm test:coverage
```

## 📝 Common Patterns

### Testing Forms

```tsx
it('submits form with validation', async () => {
  const user = userEvent.setup()
  const mockSubmit = vi.fn()
  
  render(<MyForm onSubmit={mockSubmit} />)
  
  await user.type(screen.getByLabelText(/email/i), 'test@example.com')
  await user.click(screen.getByRole('button', { name: /submit/i }))
  
  expect(mockSubmit).toHaveBeenCalledWith({
    email: 'test@example.com'
  })
})
```

### Testing Navigation

```tsx
it('navigates to user detail page', async () => {
  const user = userEvent.setup()
  
  render(<UserList />)
  
  await user.click(screen.getByText('John Doe'))
  
  expect(mockNavigate).toHaveBeenCalledWith('/users/1')
})
```

### Testing Error States

```tsx
it('displays error message when API fails', async () => {
  server.use(
    http.get('/api/users', () => HttpResponse.error())
  )
  
  render(<UserList />)
  
  await waitFor(() => {
    expect(screen.getByText(/failed to load/i)).toBeInTheDocument()
  })
})
```

This testing infrastructure provides a solid foundation for maintaining high code quality and confidence in your React application.