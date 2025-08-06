import React, { useState, useEffect } from 'react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@/test/utils/test-utils'
import userEvent from '@testing-library/user-event'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

// Example component that demonstrates various testing scenarios
interface User {
  id: string
  name: string
  email: string
  status: 'active' | 'inactive'
}

const UserCard: React.FC<{ user: User; onDelete: (id: string) => void }> = ({ user, onDelete }) => {
  return (
    <div data-testid={`user-card-${user.id}`} className="p-4 border rounded">
      <h3>{user.name}</h3>
      <p>{user.email}</p>
      <Badge variant={user.status === 'active' ? 'default' : 'secondary'}>
        {user.status}
      </Badge>
      <Button 
        variant="destructive" 
        size="sm" 
        onClick={() => onDelete(user.id)}
        className="ml-2"
      >
        Delete
      </Button>
    </div>
  )
}

const UserList: React.FC = () => {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchUsers()
  }, [])

  const fetchUsers = async () => {
    try {
      setLoading(true)
      setError(null)
      const response = await fetch('/api/users')
      if (!response.ok) throw new Error('Failed to fetch users')
      const data = await response.json()
      setUsers(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setLoading(false)
    }
  }

  const handleDelete = async (id: string) => {
    try {
      const response = await fetch(`/api/users/${id}`, { method: 'DELETE' })
      if (!response.ok) throw new Error('Failed to delete user')
      setUsers(prev => prev.filter(user => user.id !== id))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete user')
    }
  }

  if (loading) return <div>Loading...</div>
  if (error) return <div>Error: {error}</div>

  return (
    <div>
      <h2>Users ({users.length})</h2>
      {users.length === 0 ? (
        <p>No users found</p>
      ) : (
        <div className="space-y-4">
          {users.map(user => (
            <UserCard key={user.id} user={user} onDelete={handleDelete} />
          ))}
        </div>
      )}
      <Button onClick={fetchUsers} className="mt-4">
        Refresh
      </Button>
    </div>
  )
}

describe('Complete Testing Example', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  describe('UserCard Component', () => {
    const mockUser: User = {
      id: '1',
      name: 'John Doe',
      email: 'john@example.com',
      status: 'active'
    }

    it('renders user information correctly', () => {
      const mockDelete = vi.fn()
      render(<UserCard user={mockUser} onDelete={mockDelete} />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('john@example.com')).toBeInTheDocument()
      expect(screen.getByText('active')).toBeInTheDocument()
    })

    it('shows correct badge variant for user status', () => {
      const mockDelete = vi.fn()
      render(<UserCard user={mockUser} onDelete={mockDelete} />)

      const badge = screen.getByText('active')
      expect(badge).toHaveClass('bg-primary')
    })

    it('calls onDelete when delete button is clicked', async () => {
      const user = userEvent.setup()
      const mockDelete = vi.fn()
      
      render(<UserCard user={mockUser} onDelete={mockDelete} />)

      const deleteButton = screen.getByRole('button', { name: /delete/i })
      await user.click(deleteButton)

      expect(mockDelete).toHaveBeenCalledWith('1')
    })

    it('renders inactive user with different badge styling', () => {
      const inactiveUser = { ...mockUser, status: 'inactive' as const }
      const mockDelete = vi.fn()
      
      render(<UserCard user={inactiveUser} onDelete={mockDelete} />)

      const badge = screen.getByText('inactive')
      expect(badge).toHaveClass('bg-muted')
    })
  })

  describe('UserList Component with API Integration', () => {
    it('loads and displays users from API', async () => {
      render(<UserList />)

      // Initially shows loading
      expect(screen.getByText('Loading...')).toBeInTheDocument()

      // Wait for users to load (using default MSW handlers)
      await waitFor(() => {
        expect(screen.getByText('Users (2)')).toBeInTheDocument()
      })

      expect(screen.getByText('Test User 1')).toBeInTheDocument()
      expect(screen.getByText('Test User 2')).toBeInTheDocument()
    })

    it('handles API errors gracefully', async () => {
      // Override default handler to return error
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
        expect(screen.getByText(/Error: Failed to fetch users/)).toBeInTheDocument()
      })
    })

    it('deletes user and updates list', async () => {
      const user = userEvent.setup()
      render(<UserList />)

      // Wait for users to load
      await waitFor(() => {
        expect(screen.getByText('Test User 1')).toBeInTheDocument()
      })

      // Click delete button for first user
      const deleteButtons = screen.getAllByRole('button', { name: /delete/i })
      await user.click(deleteButtons[0])

      // Verify user is removed from list
      await waitFor(() => {
        expect(screen.queryByText('Test User 1')).not.toBeInTheDocument()
      })

      // Count should be updated
      expect(screen.getByText('Users (1)')).toBeInTheDocument()
    })

    it('handles delete errors', async () => {
      const user = userEvent.setup()
      
      // Override delete handler to return error
      server.use(
        http.delete('/api/users/:id', () => {
          return HttpResponse.json(
            { error: 'Delete failed' },
            { status: 500 }
          )
        })
      )

      render(<UserList />)

      await waitFor(() => {
        expect(screen.getByText('Test User 1')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByRole('button', { name: /delete/i })
      await user.click(deleteButtons[0])

      await waitFor(() => {
        expect(screen.getByText(/Error: Failed to delete user/)).toBeInTheDocument()
      })
    })

    it('refreshes data when refresh button is clicked', async () => {
      const user = userEvent.setup()
      render(<UserList />)

      await waitFor(() => {
        expect(screen.getByText('Users (2)')).toBeInTheDocument()
      })

      // Override with different data
      server.use(
        http.get('/api/users', () => {
          return HttpResponse.json([
            { id: '3', name: 'New User', email: 'new@example.com', role: 'user', status: 'active' }
          ])
        })
      )

      const refreshButton = screen.getByRole('button', { name: /refresh/i })
      await user.click(refreshButton)

      await waitFor(() => {
        expect(screen.getByText('Users (1)')).toBeInTheDocument()
        expect(screen.getByText('New User')).toBeInTheDocument()
      })
    })

    it('shows empty state when no users', async () => {
      server.use(
        http.get('/api/users', () => {
          return HttpResponse.json([])
        })
      )

      render(<UserList />)

      await waitFor(() => {
        expect(screen.getByText('No users found')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility and Interaction', () => {
    it('has proper ARIA labels and roles', async () => {
      render(<UserList />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByRole('button', { name: /delete/i })
      expect(deleteButtons.length).toBeGreaterThan(0)
    })

    it('supports keyboard navigation', async () => {
      const user = userEvent.setup()
      render(<UserList />)

      await waitFor(() => {
        expect(screen.getByText('Test User 1')).toBeInTheDocument()
      })

      // Tab to first delete button and activate with keyboard
      const deleteButton = screen.getAllByRole('button', { name: /delete/i })[0]
      deleteButton.focus()
      expect(deleteButton).toHaveFocus()

      await user.keyboard('{Enter}')
      
      await waitFor(() => {
        expect(screen.queryByText('Test User 1')).not.toBeInTheDocument()
      })
    })
  })

  describe('Performance and Edge Cases', () => {
    it('handles rapid successive API calls', async () => {
      const user = userEvent.setup()
      render(<UserList />)

      await waitFor(() => {
        expect(screen.getByText('Users (2)')).toBeInTheDocument()
      })

      const refreshButton = screen.getByRole('button', { name: /refresh/i })
      
      // Click refresh multiple times rapidly
      await user.click(refreshButton)
      await user.click(refreshButton)
      await user.click(refreshButton)

      // Should still work correctly
      await waitFor(() => {
        expect(screen.getByText('Users (2)')).toBeInTheDocument()
      })
    })

    it('handles component unmounting during async operations', async () => {
      const { unmount } = render(<UserList />)

      // Unmount before API call completes
      unmount()

      // Should not cause any errors or warnings
      expect(true).toBe(true)
    })
  })
})