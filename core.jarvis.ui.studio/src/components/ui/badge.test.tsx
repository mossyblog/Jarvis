import React from 'react'
import { describe, it, expect } from 'vitest'
import { render, screen } from '@/test/utils/test-utils'
import { Badge } from './badge'

describe('Badge', () => {
  it('renders with correct text', () => {
    render(<Badge>Test Badge</Badge>)
    expect(screen.getByText('Test Badge')).toBeInTheDocument()
  })

  it('applies variant classes correctly', () => {
    render(<Badge variant="destructive">Error Badge</Badge>)
    const badge = screen.getByText('Error Badge')
    expect(badge).toHaveClass('bg-destructive')
  })

  it('applies default variant when no variant specified', () => {
    render(<Badge>Default Badge</Badge>)
    const badge = screen.getByText('Default Badge')
    expect(badge).toHaveClass('bg-primary')
  })

  it('applies secondary variant correctly', () => {
    render(<Badge variant="secondary">Secondary Badge</Badge>)
    const badge = screen.getByText('Secondary Badge')
    expect(badge).toHaveClass('bg-muted')
  })

  it('applies outline variant correctly', () => {
    render(<Badge variant="outline">Outline Badge</Badge>)
    const badge = screen.getByText('Outline Badge')
    expect(badge).toHaveClass('border-border')
  })

  it('forwards additional props', () => {
    render(<Badge data-testid="custom-badge" title="Custom Badge">Test</Badge>)
    const badge = screen.getByTestId('custom-badge')
    expect(badge).toHaveAttribute('title', 'Custom Badge')
  })

  it('renders with custom className', () => {
    render(<Badge className="custom-class">Custom Badge</Badge>)
    const badge = screen.getByText('Custom Badge')
    expect(badge).toHaveClass('custom-class')
  })
})