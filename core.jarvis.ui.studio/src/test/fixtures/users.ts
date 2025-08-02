export const mockUsers = [
  {
    id: '1',
    name: 'John Doe',
    email: 'john.doe@example.com',
    role: 'admin',
    status: 'active',
    avatar: 'https://i.pravatar.cc/150?u=1',
    createdAt: '2024-01-01T00:00:00Z',
    lastLogin: '2024-01-15T10:30:00Z',
  },
  {
    id: '2',
    name: 'Jane Smith',
    email: 'jane.smith@example.com',
    role: 'user',
    status: 'active',
    avatar: 'https://i.pravatar.cc/150?u=2',
    createdAt: '2024-01-02T00:00:00Z',
    lastLogin: '2024-01-14T14:20:00Z',
  },
  {
    id: '3',
    name: 'Bob Johnson',
    email: 'bob.johnson@example.com',
    role: 'user',
    status: 'inactive',
    avatar: 'https://i.pravatar.cc/150?u=3',
    createdAt: '2024-01-03T00:00:00Z',
    lastLogin: '2024-01-10T09:15:00Z',
  },
]

export const createMockUser = (overrides: Partial<typeof mockUsers[0]> = {}) => ({
  id: Math.random().toString(36).substr(2, 9),
  name: 'Test User',
  email: 'test@example.com',
  role: 'user' as const,
  status: 'active' as const,
  avatar: 'https://i.pravatar.cc/150?u=test',
  createdAt: new Date().toISOString(),
  lastLogin: new Date().toISOString(),
  ...overrides,
})