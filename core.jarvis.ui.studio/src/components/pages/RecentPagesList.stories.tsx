/**
 * Recent Pages List Stories
 * 
 * Storybook stories for the RecentPagesList component demonstrating
 * various states, configurations, and interactive features.
 */

import type { Meta, StoryObj } from '@storybook/react';
import { action } from '@storybook/addon-actions';
import { within, expect, userEvent } from '@storybook/test';
import { RecentPagesList } from './RecentPagesList';
import { getRecentPagesManager, type RecentPageMetadata } from '@/utils/recentPagesManager';

// Mock data for stories
const mockPages: RecentPageMetadata[] = [
  {
    id: '1',
    displayName: 'Dashboard Overview',
    route: '/dashboard',
    pageSlug: 'dashboard',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 30).toISOString(), // 30 minutes ago
    accessCount: 15,
    description: 'Main dashboard with key metrics and overview charts',
    tags: ['dashboard', 'analytics', 'overview'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7).toISOString(), // 1 week ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(), // 2 days ago
    createdBy: 'user-1'
  },
  {
    id: '2',
    displayName: 'User Management',
    route: '/users',
    pageSlug: 'users',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(), // 2 hours ago
    accessCount: 8,
    description: 'Manage users, roles, and permissions',
    tags: ['users', 'admin', 'security'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 5).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(),
    createdBy: 'user-1'
  },
  {
    id: '3',
    displayName: 'Sales Report',
    route: '/reports/sales',
    pageSlug: 'reports-sales',
    status: 'draft',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 6).toISOString(), // 6 hours ago
    accessCount: 3,
    description: 'Comprehensive sales analytics and reporting dashboard',
    tags: ['reports', 'sales', 'analytics'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 3).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 12).toISOString(),
    createdBy: 'user-2'
  },
  {
    id: '4',
    displayName: 'Product Catalog',
    route: '/products',
    pageSlug: 'products',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(), // 1 day ago
    accessCount: 22,
    description: 'Browse and manage product inventory',
    tags: ['products', 'inventory', 'catalog'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 10).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 3).toISOString(),
    createdBy: 'user-1'
  },
  {
    id: '5',
    displayName: 'Settings & Configuration',
    route: '/settings',
    pageSlug: 'settings',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 48).toISOString(), // 2 days ago
    accessCount: 5,
    description: 'System settings and configuration options',
    tags: ['settings', 'config', 'admin'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 14).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 4).toISOString(),
    createdBy: 'user-1'
  },
  {
    id: '6',
    displayName: 'Archive: Old Landing Page',
    route: '/old-landing',
    pageSlug: 'old-landing',
    status: 'archived',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7).toISOString(), // 1 week ago
    accessCount: 1,
    description: 'Previous version of the landing page (archived)',
    tags: ['archive', 'landing', 'old'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 20).toISOString(),
    createdBy: 'user-3'
  }
];

// Setup mock recent pages manager
const setupMockData = () => {
  const manager = getRecentPagesManager();
  manager.clearAll();
  mockPages.forEach(page => manager.addPage(page));
};

const meta: Meta<typeof RecentPagesList> = {
  title: 'Pages/RecentPagesList',
  component: RecentPagesList,
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component: `
The RecentPagesList component displays user's recently accessed pages with thumbnails, 
metadata, and quick access actions. It features:

- Grid and list view modes
- Search and filtering capabilities
- Quick actions (edit, duplicate, delete)
- Mobile-first responsive design
- Integration with localStorage for tracking access
- Loading and error states
- Accessibility compliance

The component automatically tracks page access and provides a convenient way for users
to return to their recent work.
        `
      }
    }
  },
  argTypes: {
    limit: {
      control: { type: 'number', min: 1, max: 50 },
      description: 'Maximum number of pages to display'
    },
    showControls: {
      control: 'boolean',
      description: 'Whether to show search and filter controls'
    },
    defaultViewMode: {
      control: { type: 'select' },
      options: ['grid', 'list'],
      description: 'Default view mode'
    },
    autoRefresh: {
      control: 'boolean',
      description: 'Whether to auto-refresh on storage changes'
    },
    compact: {
      control: 'boolean',
      description: 'Compact mode for smaller spaces'
    },
    onEdit: { action: 'edit' },
    onDuplicate: { action: 'duplicate' },
    onDelete: { action: 'delete' },
    onPreview: { action: 'preview' }
  },
  beforeEach: () => {
    setupMockData();
  }
};

export default meta;
type Story = StoryObj<typeof RecentPagesList>;

// Default story with full functionality
export const Default: Story = {
  args: {
    onEdit: action('edit'),
    onDuplicate: action('duplicate'),
    onDelete: action('delete'),
    onPreview: action('preview')
  }
};

// Grid view mode
export const GridView: Story = {
  args: {
    ...Default.args,
    defaultViewMode: 'grid'
  },
  parameters: {
    docs: {
      description: {
        story: 'Grid view displays pages as cards with thumbnails, ideal for visual browsing.'
      }
    }
  }
};

// List view mode
export const ListView: Story = {
  args: {
    ...Default.args,
    defaultViewMode: 'list'
  },
  parameters: {
    docs: {
      description: {
        story: 'List view displays pages in a compact list format, showing more metadata.'
      }
    }
  }
};

// Compact mode
export const CompactMode: Story = {
  args: {
    ...Default.args,
    compact: true,
    limit: 6
  },
  parameters: {
    docs: {
      description: {
        story: 'Compact mode reduces spacing and hides some metadata for smaller spaces.'
      }
    }
  }
};

// Limited pages
export const LimitedPages: Story = {
  args: {
    ...Default.args,
    limit: 3
  },
  parameters: {
    docs: {
      description: {
        story: 'Limit the number of pages displayed for specific use cases.'
      }
    }
  }
};

// No controls
export const NoControls: Story = {
  args: {
    ...Default.args,
    showControls: false,
    limit: 6
  },
  parameters: {
    docs: {
      description: {
        story: 'Hide search and filter controls for embedded usage.'
      }
    }
  }
};

// Empty state
export const EmptyState: Story = {
  args: {
    ...Default.args
  },
  beforeEach: () => {
    // Clear all recent pages to show empty state
    const manager = getRecentPagesManager();
    manager.clearAll();
  },
  parameters: {
    docs: {
      description: {
        story: 'Empty state when no recent pages are available.'
      }
    }
  }
};

// Loading state
export const LoadingState: Story = {
  args: {
    ...Default.args
  },
  parameters: {
    docs: {
      description: {
        story: 'Loading state with skeleton placeholders.'
      }
    }
  },
  // Override the hook to simulate loading
  decorators: [
    (Story) => {
      // This would typically be handled by mocking the hook
      return <Story />;
    }
  ]
};

// Mobile responsive
export const MobileView: Story = {
  args: {
    ...Default.args
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile1'
    },
    docs: {
      description: {
        story: 'Mobile-responsive design optimized for touch interactions.'
      }
    }
  }
};

// Tablet responsive
export const TabletView: Story = {
  args: {
    ...Default.args
  },
  parameters: {
    viewport: {
      defaultViewport: 'tablet'
    },
    docs: {
      description: {
        story: 'Tablet view with optimized grid layout.'
      }
    }
  }
};

// Dark mode
export const DarkMode: Story = {
  args: {
    ...Default.args
  },
  parameters: {
    backgrounds: { default: 'dark' },
    docs: {
      description: {
        story: 'Dark mode theme support.'
      }
    }
  },
  decorators: [
    (Story) => (
      <div className="dark">
        <div className="bg-background text-foreground p-4 rounded-lg">
          <Story />
        </div>
      </div>
    )
  ]
};

// Interactive testing
export const InteractiveTest: Story = {
  args: {
    ...Default.args
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const user = userEvent.setup();

    // Test search functionality
    const searchInput = canvas.getByPlaceholderText('Search recent pages...');
    await user.type(searchInput, 'dashboard');

    // Verify search results
    await expect(canvas.getByText('Dashboard Overview')).toBeInTheDocument();

    // Clear search
    await user.clear(searchInput);

    // Test view mode toggle
    const listViewButton = canvas.getByRole('button', { name: /list/i });
    await user.click(listViewButton);

    // Test filter
    const filterSelect = canvas.getByRole('combobox', { name: /filter/i });
    await user.click(filterSelect);

    // Test sorting
    const sortSelect = canvas.getByRole('combobox', { name: /sort/i });
    await user.click(sortSelect);
  },
  parameters: {
    docs: {
      description: {
        story: 'Interactive test story demonstrating search, filtering, and view mode changes.'
      }
    }
  }
};

// Performance test with many pages
export const ManyPages: Story = {
  args: {
    ...Default.args,
    limit: 50
  },
  beforeEach: () => {
    // Generate many mock pages
    const manager = getRecentPagesManager();
    manager.clearAll();
    
    for (let i = 0; i < 50; i++) {
      const page: RecentPageMetadata = {
        id: `page-${i}`,
        displayName: `Page ${i + 1}`,
        route: `/page-${i}`,
        pageSlug: `page-${i}`,
        status: ['draft', 'published', 'archived'][i % 3] as any,
        lastAccessed: new Date(Date.now() - i * 1000 * 60 * 60).toISOString(),
        accessCount: Math.floor(Math.random() * 20) + 1,
        description: `Description for page ${i + 1}`,
        tags: [`tag-${i % 5}`, `category-${i % 3}`],
        createdAt: new Date(Date.now() - i * 1000 * 60 * 60 * 24).toISOString(),
        updatedAt: new Date(Date.now() - i * 1000 * 60 * 60 * 12).toISOString(),
        createdBy: `user-${i % 3}`
      };
      manager.addPage(page);
    }
  },
  parameters: {
    docs: {
      description: {
        story: 'Performance test with many pages to verify rendering efficiency.'
      }
    }
  }
};