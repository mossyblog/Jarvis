import React from 'react'
import { describe, it, expect } from 'vitest'
import { render, screen } from '@/test/utils/test-utils'

// Simple component for testing
const TestComponent = ({ children }: { children: React.ReactNode }) => (
  <div data-testid="test-component">{children}</div>
)

describe('Basic Test Infrastructure', () => {
  it('renders a simple component', () => {
    render(<TestComponent>Hello Test</TestComponent>)
    expect(screen.getByTestId('test-component')).toBeInTheDocument()
    expect(screen.getByText('Hello Test')).toBeInTheDocument()
  })

  it('can use custom test utilities', () => {
    render(
      <div>
        <h1>Test Page</h1>
        <p>This is a test</p>
      </div>
    )
    
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Test Page')
    expect(screen.getByText('This is a test')).toBeInTheDocument()
  })

  it('supports jsdom environment', () => {
    expect(typeof window).toBe('object')
    expect(typeof document).toBe('object')
    expect(document.createElement('div')).toBeInstanceOf(HTMLDivElement)
  })

  it('has jest-dom matchers available', () => {
    const element = document.createElement('div')
    element.style.display = 'none'
    document.body.appendChild(element)
    
    expect(element).not.toBeVisible()
    
    element.style.display = 'block'
    expect(element).toBeVisible()
    
    document.body.removeChild(element)
  })
})