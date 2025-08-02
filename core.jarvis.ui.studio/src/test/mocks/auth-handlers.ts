import { http, HttpResponse, delay } from 'msw'
import type { LoginCredentials, User, AuthResponse, NavigationItem } from '../../services/api/types'

// Mock users database
const mockUsers: User[] = [
  {
    id: '1',
    email: 'admin@example.com',
    name: 'Admin User',
    roles: [
      {
        id: 'admin',
        name: 'Administrator',
        permissions: [
          {
            id: 'all',
            resource: '*',
            actions: ['*']
          }
        ]
      }
    ]
  },
  {
    id: '2',
    email: 'user@example.com',
    name: 'Regular User',
    roles: [
      {
        id: 'user',
        name: 'User',
        permissions: [
          {
            id: 'users-read',
            resource: 'users',
            actions: ['read']
          },
          {
            id: 'profile-read',
            resource: 'profile',
            actions: ['read', 'write']
          }
        ]
      }
    ]
  },
  {
    id: '3',
    email: 'test@example.com',
    name: 'Test User',
    roles: [
      {
        id: 'test',
        name: 'Test Role',
        permissions: [
          {
            id: 'test-permissions',
            resource: 'test-resource',
            actions: ['read']
          }
        ]
      }
    ]
  }
]

// Mock navigation items
const mockNavigation: NavigationItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    icon: 'LayoutDashboard',
    href: '/'
  },
  {
    id: 'users',
    label: 'Users',
    icon: 'Users',
    href: '/users',
    requiredPermission: 'users',
    requiredAction: 'read'
  },
  {
    id: 'admin',
    label: 'Admin Panel',
    icon: 'Settings',
    href: '/admin',
    requiredPermission: 'admin',
    requiredAction: 'write'
  },
  {
    id: 'profile',
    label: 'Profile',
    icon: 'User',
    href: '/profile',
    requiredPermission: 'profile',
    requiredAction: 'read'
  }
]

// Session storage for active tokens
const activeSessions = new Map<string, { user: User; refreshToken: string; expiresAt: number }>()

// Helper functions
const generateToken = (userId: string, type: 'access' | 'refresh' = 'access'): string => {
  const payload = {
    userId,
    type,
    iat: Math.floor(Date.now() / 1000),
    exp: Math.floor(Date.now() / 1000) + (type === 'access' ? 3600 : 86400) // 1 hour for access, 24 hours for refresh
  }
  return btoa(JSON.stringify(payload))
}

const validateToken = (token: string): { userId: string; exp: number } | null => {
  try {
    const payload = JSON.parse(atob(token))
    if (payload.exp < Math.floor(Date.now() / 1000)) {
      return null // Token expired
    }
    return { userId: payload.userId, exp: payload.exp }
  } catch {
    return null // Invalid token
  }
}

const getUserFromToken = (token: string): User | null => {
  const tokenData = validateToken(token)
  if (!tokenData) return null
  
  return mockUsers.find(user => user.id === tokenData.userId) || null
}

const hasPermission = (user: User, resource: string, action: string = 'read'): boolean => {
  return user.roles.some(role =>
    role.permissions.some(perm =>
      (perm.resource === '*' && perm.actions.includes('*')) ||
      (perm.resource === resource && (perm.actions.includes(action) || perm.actions.includes('*')))
    )
  )
}

// Authentication handlers
export const authHandlers = [
  // Login endpoint
  http.post('/api/security/auth', async ({ request }) => {
    await delay(100) // Simulate network delay
    
    const credentials = await request.json() as LoginCredentials
    
    // Find user by email
    const user = mockUsers.find(u => u.email === credentials.email)
    
    if (!user) {
      return HttpResponse.json(
        { message: 'Invalid credentials' },
        { status: 401 }
      )
    }
    
    // In mock mode, any password works for demo purposes
    // In real scenarios, you'd validate the password here
    
    const accessToken = generateToken(user.id, 'access')
    const refreshToken = generateToken(user.id, 'refresh')
    
    // Store session
    activeSessions.set(accessToken, {
      user,
      refreshToken,
      expiresAt: Math.floor(Date.now() / 1000) + 3600
    })
    
    const response: any = {
      AccessToken: accessToken,
      RefreshToken: refreshToken,
      OwnerEntityId: user.id,
      Email: user.email
    }
    
    return HttpResponse.json(response, { status: 200 })
  }),

  // Logout endpoint
  http.post('/api/security/deauth', async ({ request }) => {
    await delay(50)
    
    const authHeader = request.headers.get('Authorization')
    if (authHeader?.startsWith('Bearer ')) {
      const token = authHeader.substring(7)
      activeSessions.delete(token)
    }
    
    return HttpResponse.json({ message: 'Logged out successfully' })
  }),

  // Token refresh endpoint
  http.post('/api/security/refresh', async ({ request }) => {
    await delay(100)
    
    const body = await request.json() as { refreshToken: string }
    const { refreshToken } = body
    
    // Find session with matching refresh token
    const session = Array.from(activeSessions.values()).find(s => s.refreshToken === refreshToken)
    
    if (!session) {
      return HttpResponse.json(
        { message: 'Invalid refresh token' },
        { status: 401 }
      )
    }
    
    // Generate new tokens
    const newAccessToken = generateToken(session.user.id, 'access')
    const newRefreshToken = generateToken(session.user.id, 'refresh')
    
    // Update session
    activeSessions.set(newAccessToken, {
      user: session.user,
      refreshToken: newRefreshToken,
      expiresAt: Math.floor(Date.now() / 1000) + 3600
    })
    
    // Remove old session
    const oldAccessToken = Array.from(activeSessions.entries())
      .find(([, s]) => s.refreshToken === refreshToken)?.[0]
    if (oldAccessToken) {
      activeSessions.delete(oldAccessToken)
    }
    
    return HttpResponse.json({
      AccessToken: newAccessToken,
      RefreshToken: newRefreshToken,
      OwnerEntityId: session.user.id
    })
  }),

  // Get current user endpoint
  http.get('/api/auth/me', async ({ request }) => {
    await delay(50)
    
    const authHeader = request.headers.get('Authorization')
    if (!authHeader?.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Unauthorized' },
        { status: 401 }
      )
    }
    
    const token = authHeader.substring(7)
    const user = getUserFromToken(token)
    
    if (!user) {
      return HttpResponse.json(
        { message: 'Invalid token' },
        { status: 401 }
      )
    }
    
    return HttpResponse.json(user)
  }),

  // Get navigation endpoint
  http.post('/api/graphql', async ({ request }) => {
    await delay(100)
    
    const body = await request.json() as { query: string }
    
    // Check if this is a navigation query
    if (body.query.includes('navigation_item_componentCollection')) {
      const authHeader = request.headers.get('Authorization')
      let userPermissions: any[] = []
      
      if (authHeader?.startsWith('Bearer ')) {
        const token = authHeader.substring(7)
        const user = getUserFromToken(token)
        if (user) {
          userPermissions = user.roles.flatMap(role => role.permissions)
        }
      }
      
      // Filter navigation based on permissions
      const allowedNavigation = mockNavigation.filter(item => {
        if (!item.requiredPermission) return true
        
        return userPermissions.some(perm =>
          (perm.resource === '*' && perm.actions.includes('*')) ||
          (perm.resource === item.requiredPermission && 
           (perm.actions.includes(item.requiredAction || 'read') || perm.actions.includes('*')))
        )
      })
      
      const graphqlResponse = {
        data: {
          navigation_item_componentCollection: {
            edges: allowedNavigation.map(item => ({
              node: {
                id: item.id,
                menu_id: 'main',
                label: item.label,
                icon: item.icon,
                href: item.href,
                sort_order: mockNavigation.indexOf(item) + 1,
                is_active: true,
                required_permission_id: item.requiredPermission
              }
            }))
          }
        }
      }
      
      return HttpResponse.json(graphqlResponse)
    }
    
    // Default GraphQL response
    return HttpResponse.json({
      data: {},
      errors: [{ message: 'Query not supported in mock' }]
    })
  }),

  // Validate token endpoint (for testing)
  http.post('/api/auth/validate', async ({ request }) => {
    await delay(50)
    
    const body = await request.json() as { token: string }
    const tokenData = validateToken(body.token)
    
    if (!tokenData) {
      return HttpResponse.json(
        { valid: false, message: 'Invalid or expired token' },
        { status: 401 }
      )
    }
    
    const user = mockUsers.find(u => u.id === tokenData.userId)
    
    return HttpResponse.json({
      valid: true,
      user,
      expiresAt: tokenData.exp
    })
  }),

  // Permissions check endpoint
  http.post('/api/auth/permissions', async ({ request }) => {
    await delay(50)
    
    const authHeader = request.headers.get('Authorization')
    if (!authHeader?.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Unauthorized' },
        { status: 401 }
      )
    }
    
    const token = authHeader.substring(7)
    const user = getUserFromToken(token)
    
    if (!user) {
      return HttpResponse.json(
        { message: 'Invalid token' },
        { status: 401 }
      )
    }
    
    const body = await request.json() as { resource: string; action?: string }
    const allowed = hasPermission(user, body.resource, body.action)
    
    return HttpResponse.json({
      resource: body.resource,
      action: body.action || 'read',
      allowed
    })
  }),

  // Simulate network errors for testing
  http.get('/api/auth/error', async () => {
    await delay(100)
    return HttpResponse.json(
      { message: 'Simulated server error' },
      { status: 500 }
    )
  }),

  // Simulate timeout for testing
  http.get('/api/auth/timeout', async () => {
    await delay(10000) // 10 second delay
    return HttpResponse.json({ message: 'This should timeout' })
  }),

  // Get user profile endpoint
  http.get('/api/users/profile', async ({ request }) => {
    await delay(100)
    
    const authHeader = request.headers.get('Authorization')
    if (!authHeader?.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Unauthorized' },
        { status: 401 }
      )
    }
    
    const token = authHeader.substring(7)
    const user = getUserFromToken(token)
    
    if (!user) {
      return HttpResponse.json(
        { message: 'Invalid token' },
        { status: 401 }
      )
    }
    
    // Return extended profile information
    return HttpResponse.json({
      ...user,
      lastLogin: new Date().toISOString(),
      preferences: {
        theme: 'light',
        notifications: true,
        language: 'en'
      }
    })
  }),

  // Update user profile endpoint
  http.put('/api/users/profile', async ({ request }) => {
    await delay(150)
    
    const authHeader = request.headers.get('Authorization')
    if (!authHeader?.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Unauthorized' },
        { status: 401 }
      )
    }
    
    const token = authHeader.substring(7)
    const user = getUserFromToken(token)
    
    if (!user) {
      return HttpResponse.json(
        { message: 'Invalid token' },
        { status: 401 }
      )
    }
    
    const updates = await request.json()
    
    // In a real app, you'd update the user in the database
    const updatedUser = { ...user, ...updates }
    
    return HttpResponse.json(updatedUser)
  }),

  // Change password endpoint
  http.post('/api/auth/change-password', async ({ request }) => {
    await delay(200)
    
    const authHeader = request.headers.get('Authorization')
    if (!authHeader?.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Unauthorized' },
        { status: 401 }
      )
    }
    
    const token = authHeader.substring(7)
    const user = getUserFromToken(token)
    
    if (!user) {
      return HttpResponse.json(
        { message: 'Invalid token' },
        { status: 401 }
      )
    }
    
    const body = await request.json() as { currentPassword: string; newPassword: string }
    
    // In mock mode, just validate that required fields are present
    if (!body.currentPassword || !body.newPassword) {
      return HttpResponse.json(
        { message: 'Current password and new password are required' },
        { status: 400 }
      )
    }
    
    if (body.newPassword.length < 8) {
      return HttpResponse.json(
        { message: 'New password must be at least 8 characters' },
        { status: 400 }
      )
    }
    
    return HttpResponse.json({ message: 'Password changed successfully' })
  }),

  // Session info endpoint
  http.get('/api/auth/session', async ({ request }) => {
    await delay(50)
    
    const authHeader = request.headers.get('Authorization')
    if (!authHeader?.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Unauthorized' },
        { status: 401 }
      )
    }
    
    const token = authHeader.substring(7)
    const session = activeSessions.get(token)
    
    if (!session) {
      return HttpResponse.json(
        { message: 'Invalid session' },
        { status: 401 }
      )
    }
    
    return HttpResponse.json({
      user: session.user,
      expiresAt: session.expiresAt,
      isActive: true
    })
  })
]

// Export for use in main handlers file
export { mockUsers, mockNavigation, activeSessions, hasPermission }