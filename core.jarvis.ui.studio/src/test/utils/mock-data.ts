import { vi } from 'vitest'
import { mockUsers } from '@/test/fixtures/users'
import { mockBentoComponents } from '@/test/fixtures/components'

// Helper functions to generate test data
export const generateMockUsers = (count: number) => {
  return Array.from({ length: count }, (_, index) => ({
    ...mockUsers[0],
    id: String(index + 1),
    name: `Test User ${index + 1}`,
    email: `user${index + 1}@example.com`,
  }))
}

export const generateMockComponents = (count: number) => {
  const categories = ['monitoring', 'analytics', 'security', 'performance']
  const icons = ['chart', 'users', 'shield', 'cpu']
  
  return Array.from({ length: count }, (_, index) => ({
    ...mockBentoComponents[0],
    id: `component-${index + 1}`,
    title: `Component ${index + 1}`,
    category: categories[index % categories.length],
    icon: icons[index % icons.length],
  }))
}

export const mockAuthResponse = {
  user: {
    id: '1',
    email: 'test@example.com',
    name: 'Test User',
    role: 'admin',
  },
  token: 'mock-jwt-token',
}

export const mockDashboardStats = {
  totalUsers: 1234,
  activeProjects: 56,
  pendingTasks: 78,
  completedTasks: 90,
  systemHealth: 'healthy',
  uptime: 99.9,
}

export const mockNetworkStatus = {
  status: 'healthy',
  latency: 45,
  throughput: 1250,
  errorRate: 0.02,
  uptime: 99.95,
  nodes: [
    { id: 'node-1', name: 'Main Server', status: 'online', load: 75 },
    { id: 'node-2', name: 'Database', status: 'online', load: 60 },
    { id: 'node-3', name: 'Cache', status: 'online', load: 30 },
  ],
}

export const mockNotifications = [
  {
    id: '1',
    title: 'System Update',
    message: 'System will be updated tonight at 2 AM',
    type: 'info',
    timestamp: new Date().toISOString(),
    read: false,
  },
  {
    id: '2',
    title: 'Security Alert',
    message: 'Unusual login activity detected',
    type: 'warning',
    timestamp: new Date().toISOString(),
    read: false,
  },
]

// Helper to create custom mock data with overrides
export const createMockData = <T>(template: T, overrides: Partial<T> = {}): T => ({
  ...template,
  ...overrides,
})

// Date utilities for testing
export const mockDate = (dateString: string) => {
  const mockDate = new Date(dateString)
  vi.spyOn(global, 'Date').mockImplementation(() => mockDate)
  return mockDate
}

export const restoreDate = () => {
  vi.restoreAllMocks()
}