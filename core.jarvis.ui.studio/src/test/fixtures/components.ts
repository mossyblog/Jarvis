export const mockBentoComponents = [
  {
    id: 'network-monitor',
    title: 'Network Monitor',
    description: 'Real-time network performance metrics',
    category: 'monitoring',
    icon: 'network',
    size: { width: 2, height: 2 },
    props: {
      showDetails: true,
      refreshRate: 5000,
    },
  },
  {
    id: 'user-stats',
    title: 'User Statistics',
    description: 'Active user counts and demographics',
    category: 'analytics',
    icon: 'users',
    size: { width: 1, height: 1 },
    props: {
      timeRange: '24h',
      showGrowth: true,
    },
  },
  {
    id: 'security-alerts',
    title: 'Security Alerts',
    description: 'Critical security notifications',
    category: 'security',
    icon: 'shield',
    size: { width: 2, height: 1 },
    props: {
      severity: 'high',
      autoRefresh: true,
    },
  },
  {
    id: 'performance-chart',
    title: 'Performance Chart',
    description: 'System performance over time',
    category: 'monitoring',
    icon: 'chart',
    size: { width: 3, height: 2 },
    props: {
      metric: 'cpu',
      timeRange: '1h',
    },
  },
]

export const createMockComponent = (overrides: Partial<typeof mockBentoComponents[0]> = {}) => ({
  id: Math.random().toString(36).substr(2, 9),
  title: 'Test Component',
  description: 'A test component for testing',
  category: 'test',
  icon: 'box',
  size: { width: 1, height: 1 },
  props: {},
  ...overrides,
})

export const mockGridLayout = [
  { i: 'network-monitor', x: 0, y: 0, w: 2, h: 2 },
  { i: 'user-stats', x: 2, y: 0, w: 1, h: 1 },
  { i: 'security-alerts', x: 0, y: 2, w: 2, h: 1 },
  { i: 'performance-chart', x: 2, y: 1, w: 3, h: 2 },
]