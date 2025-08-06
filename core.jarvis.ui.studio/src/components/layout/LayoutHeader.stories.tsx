import type { Meta, StoryObj } from '@storybook/react'
import LayoutHeader from './LayoutHeader'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../../contexts/AuthContext'
import { EditModeProvider } from '../../contexts/EditModeContext'
import { Button } from '../ui/button'
import { Badge } from '../ui/badge'
import { Plus, Search, Filter, Download } from 'lucide-react'
import { Input } from '../ui/input'

const meta: Meta<typeof LayoutHeader> = {
  title: 'Layout/LayoutHeader',
  component: LayoutHeader,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: {
        component: `
The main layout header component with comprehensive user context and actions.

## Features
- **Responsive Design**: Adapts to mobile, tablet, and desktop screens
- **User Context**: Shows user profile, authentication status, and permissions
- **Notifications**: Real-time notification system with read/unread states
- **Quick Actions**: Fast access to common operations and keyboard shortcuts
- **Settings**: User preferences and account management
- **Edit Mode**: Toggle for content editing with unsaved changes indicator
- **Connection Status**: Network connectivity indicator
- **Help & Support**: Documentation, support, and community links
- **Accessibility**: Full ARIA labels, keyboard navigation, and screen reader support
        `
      }
    }
  },
  decorators: [
    (Story) => (
      <BrowserRouter>
        <AuthProvider>
          <EditModeProvider>
            <div className="min-h-screen bg-background">
              <Story />
              <div className="p-8 space-y-6">
                <div>
                  <h1 className="text-3xl font-bold mb-2">Dashboard</h1>
                  <p className="text-muted-foreground">Welcome to your Jarvis UI Studio dashboard.</p>
                </div>
                
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  <div className="bg-card p-6 rounded-lg border">
                    <h3 className="font-semibold mb-2">Pages</h3>
                    <p className="text-2xl font-bold">12</p>
                    <p className="text-sm text-muted-foreground">Total pages created</p>
                  </div>
                  
                  <div className="bg-card p-6 rounded-lg border">
                    <h3 className="font-semibold mb-2">Components</h3>
                    <p className="text-2xl font-bold">48</p>
                    <p className="text-sm text-muted-foreground">Available components</p>
                  </div>
                  
                  <div className="bg-card p-6 rounded-lg border">
                    <h3 className="font-semibold mb-2">Templates</h3>
                    <p className="text-2xl font-bold">8</p>
                    <p className="text-sm text-muted-foreground">Ready-to-use templates</p>
                  </div>
                </div>
                
                <div className="bg-card p-6 rounded-lg border">
                  <h3 className="font-semibold mb-4">Recent Activity</h3>
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="font-medium">Dashboard page updated</p>
                        <p className="text-sm text-muted-foreground">2 minutes ago</p>
                      </div>
                      <Badge variant="secondary">Edit</Badge>
                    </div>
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="font-medium">New component added</p>
                        <p className="text-sm text-muted-foreground">1 hour ago</p>
                      </div>
                      <Badge variant="outline">Create</Badge>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </EditModeProvider>
        </AuthProvider>
      </BrowserRouter>
    )
  ]
}

export default meta
type Story = StoryObj<typeof LayoutHeader>

export const Default: Story = {
  name: 'Default Header',
  args: {
    showProductMenu: true
  }
}

export const WithSearchBar: Story = {
  name: 'With Search Bar',
  args: {
    showProductMenu: true,
    customHeaderComponents: (
      <div className="flex items-center gap-2 flex-1 max-w-md">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-xs w-xs text-muted-foreground" />
          <Input 
            placeholder="Search pages, components..."
            className="pl-10 h-lg"
          />
        </div>
      </div>
    )
  }
}

export const WithActionButtons: Story = {
  name: 'With Action Buttons',
  args: {
    showProductMenu: true,
    customHeaderComponents: (
      <div className="flex items-center gap-2">
        <Button size="sm" className="h-lg">
          <Plus className="w-xs h-xs mr-1" />
          New Page
        </Button>
        <Button size="sm" variant="outline" className="h-lg">
          <Filter className="w-xs h-xs mr-1" />
          Filter
        </Button>
        <Button size="sm" variant="outline" className="h-lg">
          <Download className="w-xs h-xs mr-1" />
          Export
        </Button>
      </div>
    )
  }
}

export const NoProductMenu: Story = {
  name: 'No Product Menu',
  args: {
    showProductMenu: false
  }
}

export const CompactMode: Story = {
  name: 'Compact Mode',
  args: {
    showProductMenu: true,
    className: 'h-xl' // Smaller height
  },
  parameters: {
    docs: {
      description: {
        story: 'A more compact version of the header for space-constrained layouts.'
      }
    }
  }
}

export const MobileView: Story = {
  name: 'Mobile View',
  parameters: {
    viewport: {
      defaultViewport: 'mobile1'
    },
    docs: {
      description: {
        story: 'How the header appears on mobile devices with collapsible menu.'
      }
    }
  },
  args: {
    showProductMenu: true
  }
}

export const TabletView: Story = {
  name: 'Tablet View',
  parameters: {
    viewport: {
      defaultViewport: 'tablet'
    },
    docs: {
      description: {
        story: 'How the header appears on tablet devices with responsive layout.'
      }
    }
  },
  args: {
    showProductMenu: true
  }
}

// Interactive story for testing header functionality
export const Interactive: Story = {
  name: 'Interactive Demo',
  args: {
    showProductMenu: true,
    customHeaderComponents: (
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2">
          <Badge variant="outline">Live Demo</Badge>
          <span className="text-sm text-muted-foreground">
            Try all header features
          </span>
        </div>
      </div>
    )
  },
  parameters: {
    docs: {
      description: {
        story: `
This interactive demo lets you test all header functionality:

- Click the **notifications** bell to see the notification panel
- Try the **quick actions** (lightning bolt) for shortcuts and help
- Access **settings** to see user preferences
- Toggle **edit mode** to see the editing interface
- Click **help** for support options
- View **user menu** for profile and logout options
- Check **connection status** (WiFi icon) for network state

The header is fully responsive and will adapt to different screen sizes.
        `
      }
    }
  }
}