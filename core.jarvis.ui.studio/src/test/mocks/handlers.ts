import { http, HttpResponse } from 'msw'
import { authHandlers } from './auth-handlers'

// Mock API handlers for testing
export const handlers = [
  // Include comprehensive auth handlers
  ...authHandlers,

  // Dashboard endpoints
  http.get('/api/dashboard/stats', () => {
    return HttpResponse.json({
      totalUsers: 1234,
      activeProjects: 56,
      pendingTasks: 78,
      completedTasks: 90,
    })
  }),

  // User management endpoints
  http.get('/api/users', () => {
    return HttpResponse.json([
      {
        id: '1',
        name: 'Test User 1',
        email: 'user1@example.com',
        role: 'admin',
        status: 'active',
      },
      {
        id: '2',
        name: 'Test User 2',
        email: 'user2@example.com',
        role: 'user',
        status: 'active',
      },
    ])
  }),

  http.post('/api/users', async ({ request }) => {
    const newUser = await request.json() as Record<string, any>
    return HttpResponse.json({
      id: '3',
      ...newUser,
      status: 'active',
    }, { status: 201 })
  }),

  http.put('/api/users/:id', async ({ request, params }) => {
    const updatedUser = await request.json() as Record<string, any>
    return HttpResponse.json({
      id: params.id,
      ...updatedUser,
    })
  }),

  http.delete('/api/users/:id', ({ params }) => {
    return HttpResponse.json({ message: `User ${params.id} deleted successfully` })
  }),

  // Issues/notifications endpoints
  http.get('/api/issues', () => {
    return HttpResponse.json([
      {
        id: '1',
        title: 'Critical Security Alert',
        description: 'Unauthorized access attempt detected',
        severity: 'high',
        type: 'security',
        timestamp: new Date().toISOString(),
        status: 'open',
      },
      {
        id: '2',
        title: 'System Performance Warning',
        description: 'CPU usage above 80% for 10 minutes',
        severity: 'medium',
        type: 'performance',
        timestamp: new Date().toISOString(),
        status: 'acknowledged',
      },
    ])
  }),

  // Network monitoring endpoints
  http.get('/api/network/status', () => {
    return HttpResponse.json({
      status: 'healthy',
      latency: 45,
      throughput: 1250,
      errorRate: 0.02,
      uptime: 99.95,
    })
  }),

  // Error handler for unhandled requests
  http.all('*', ({ request }) => {
    console.warn(`Unhandled ${request.method} request to ${request.url}`)
    return HttpResponse.json(
      { error: 'Not found' },
      { status: 404 }
    )
  }),
]