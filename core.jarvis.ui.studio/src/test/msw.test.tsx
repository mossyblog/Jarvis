import { describe, it, expect, beforeEach } from 'vitest'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

describe('MSW API Mocking', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  it('can mock API responses', async () => {
    const response = await fetch('/api/auth/me')
    const data = await response.json()
    
    expect(data).toEqual({
      id: '1',
      email: 'test@example.com',
      name: 'Test User',
    })
  })

  it('can override handlers for specific tests', async () => {
    server.use(
      http.get('/api/users', () => {
        return HttpResponse.json([
          { id: '1', name: 'Custom User', email: 'custom@example.com' }
        ])
      })
    )

    const response = await fetch('/api/users')
    const data = await response.json()
    
    expect(data).toEqual([
      { id: '1', name: 'Custom User', email: 'custom@example.com' }
    ])
  })

  it('can simulate API errors', async () => {
    server.use(
      http.get('/api/users', () => {
        return HttpResponse.json(
          { error: 'Internal server error' },
          { status: 500 }
        )
      })
    )

    const response = await fetch('/api/users')
    
    expect(response.status).toBe(500)
    
    const data = await response.json()
    expect(data).toEqual({ error: 'Internal server error' })
  })

  it('handles POST requests with request data', async () => {
    const response = await fetch('/api/users', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: 'New User',
        email: 'new@example.com',
        role: 'user'
      })
    })

    const data = await response.json()
    
    expect(response.status).toBe(201)
    expect(data).toMatchObject({
      name: 'New User',
      email: 'new@example.com',
      role: 'user',
      status: 'active'
    })
    expect(data.id).toBe('3') // From our mock handler
  })
})