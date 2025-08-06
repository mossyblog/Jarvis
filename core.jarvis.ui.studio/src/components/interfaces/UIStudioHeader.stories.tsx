import type { Meta, StoryObj } from '@storybook/react';
import { UIStudioHeader } from './UIStudioHeader';
import { AuthProvider } from '../../contexts/AuthContext';
import { EditModeProvider } from '../../contexts/EditModeContext';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';

// Create a query client for Storybook
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: Infinity,
    },
  },
});

// Mock user data for stories
const mockUser = {
  id: 'user-123',
  name: 'Jane Doe',
  email: 'jane.doe@example.com',
  avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612345e?w=150&h=150&fit=crop&crop=face',
  roles: [
    { id: 'role-1', name: 'Admin' },
    { id: 'role-2', name: 'Editor' }
  ],
  permissions: ['read', 'write', 'admin']
};

// Wrapper component that provides all necessary contexts
const UIStudioHeaderWrapper = ({ children }: { children: React.ReactNode }) => {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <EditModeProvider>
            <div className="min-h-screen bg-background">
              {children}
              <div className="p-8">
                <div className="max-w-4xl mx-auto">
                  <h2 className="text-2xl font-bold mb-4">UIStudio Interface Content</h2>
                  <p className="text-muted-foreground mb-6">
                    This is placeholder content to demonstrate how the header appears in context.
                    The header component includes user context, actions, and responsive design features.
                  </p>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {Array.from({ length: 6 }).map((_, i) => (
                      <div key={i} className="p-4 border rounded-lg bg-card">
                        <h3 className="font-semibold mb-2">Sample Card {i + 1}</h3>
                        <p className="text-sm text-muted-foreground">
                          This demonstrates how content appears below the header.
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </EditModeProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
};

const meta: Meta<typeof UIStudioHeader> = {
  title: 'Components/Interfaces/UIStudioHeader',
  component: UIStudioHeader,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: {
        component: `
The UIStudioHeader component provides a comprehensive header for the UIStudio interface with:

- **User Context**: Shows authenticated user information, avatar, and role
- **Quick Actions**: Create page, templates, search, and more
- **Responsive Design**: Mobile-first approach with collapsible menus
- **Notifications**: Real-time notification system with unread indicators
- **Keyboard Navigation**: Global shortcuts for common actions
- **Accessibility**: ARIA landmarks and proper labeling

### Features

- User profile dropdown with account management
- Notification center with real-time updates
- Quick access to create pages and templates
- Search functionality (Ctrl+K)
- Help and settings access
- Mobile-responsive with hamburger menu
- Edit mode toggle integration
- Proper keyboard navigation support

### Keyboard Shortcuts

- **Ctrl+K / Cmd+K**: Open search
- **Ctrl+N / Cmd+N**: Create new page
- **?**: Show help
- **Alt+M**: Toggle mobile menu
        `,
      },
    },
  },
  argTypes: {
    userEntityId: {
      description: 'Current user entity ID',
      control: 'text',
    },
    title: {
      description: 'Header title',
      control: 'text',
    },
    subtitle: {
      description: 'Header subtitle (shown on mobile)',
      control: 'text',
    },
    showProjectSelector: {
      description: 'Show project/workspace selector',
      control: 'boolean',
    },
    showActions: {
      description: 'Configure which action buttons to show',
      control: 'object',
    },
    onOpenMobileSidebar: {
      description: 'Callback when mobile sidebar should open',
      action: 'mobile-sidebar-opened',
    },
    onCreatePage: {
      description: 'Callback when create page is triggered',
      action: 'create-page-clicked',
    },
    onOpenTemplates: {
      description: 'Callback when templates should open',
      action: 'templates-opened',
    },
    onOpenSearch: {
      description: 'Callback when search should open',
      action: 'search-opened',
    },
  },
  decorators: [
    (Story) => (
      <UIStudioHeaderWrapper>
        <Story />
      </UIStudioHeaderWrapper>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof UIStudioHeader>;

// Default authenticated user story
export const Default: Story = {
  args: {
    userEntityId: 'user-123',
    title: 'UIStudio',
    subtitle: 'Dashboard',
    showProjectSelector: false,
    showActions: {
      createPage: true,
      templates: true,
      search: true,
      notifications: true,
      settings: true,
      help: true,
    },
  },
};

// With project selector enabled
export const WithProjectSelector: Story = {
  args: {
    ...Default.args,
    showProjectSelector: true,
  },
};

// Minimal actions configuration
export const MinimalActions: Story = {
  args: {
    ...Default.args,
    showActions: {
      createPage: true,
      templates: false,
      search: false,
      notifications: false,
      settings: false,
      help: true,
    },
  },
};

// Custom title and subtitle
export const CustomTitle: Story = {
  args: {
    ...Default.args,
    title: 'Design System',
    subtitle: 'Component Library',
  },
};

// All actions enabled (kitchen sink)
export const AllActions: Story = {
  args: {
    ...Default.args,
    showProjectSelector: true,
    showActions: {
      createPage: true,
      templates: true,
      search: true,
      notifications: true,
      settings: true,
      help: true,
    },
  },
};

// Mobile viewport demonstration
export const Mobile: Story = {
  args: {
    ...Default.args,
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};

// Tablet viewport demonstration
export const Tablet: Story = {
  args: {
    ...Default.args,
    showProjectSelector: true,
  },
  parameters: {
    viewport: {
      defaultViewport: 'tablet',
    },
  },
};

// Dark mode demonstration
export const DarkMode: Story = {
  args: {
    ...Default.args,
    showProjectSelector: true,
  },
  parameters: {
    themes: {
      default: 'dark',
    },
  },
};

// No actions (minimal header)
export const NoActions: Story = {
  args: {
    ...Default.args,
    showActions: {
      createPage: false,
      templates: false,
      search: false,
      notifications: false,
      settings: false,
      help: false,
    },
  },
};