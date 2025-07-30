import type { Meta, StoryObj } from '@storybook/react';
import { ContentHeader } from './ContentHeader';
import { Button } from '../ui/button';
import { Plus, Download, Settings } from 'lucide-react';

const meta: Meta<typeof ContentHeader> = {
  title: 'Layout/ContentHeader',
  component: ContentHeader,
  parameters: {
    layout: 'fullscreen',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: 'Dashboard',
    description: 'Welcome back! Here\'s what\'s happening with your projects.',
  },
};

export const WithBreadcrumbs: Story = {
  args: {
    title: 'User Management',
    description: 'Manage user accounts, roles, and permissions.',
    breadcrumbs: (
      <div className="flex items-center gap-2">
        <a href="/" className="hover:text-foreground">Dashboard</a>
        <span>/</span>
        <a href="/settings" className="hover:text-foreground">Settings</a>
        <span>/</span>
        <span className="text-foreground">Users</span>
      </div>
    ),
  },
};

export const WithActions: Story = {
  args: {
    title: 'Projects',
    description: 'View and manage all your projects in one place.',
    actions: (
      <div className="flex gap-2">
        <Button variant="outline" size="sm">
          <Download className="h-4 w-4 mr-2" />
          Export
        </Button>
        <Button size="sm">
          <Plus className="h-4 w-4 mr-2" />
          New Project
        </Button>
      </div>
    ),
  },
};

export const WithBreadcrumbsAndActions: Story = {
  args: {
    title: 'Account Settings',
    description: 'Manage your account preferences and security settings.',
    breadcrumbs: (
      <div className="flex items-center gap-2">
        <a href="/" className="hover:text-foreground">Dashboard</a>
        <span>/</span>
        <a href="/settings" className="hover:text-foreground">Settings</a>
        <span>/</span>
        <span className="text-foreground">Account</span>
      </div>
    ),
    actions: (
      <Button variant="outline" size="sm">
        <Settings className="h-4 w-4 mr-2" />
        Advanced
      </Button>
    ),
  },
};

export const InPageLayout: Story = {
  render: () => (
    <div className="min-h-screen bg-background">
      <ContentHeader
        title="Analytics Dashboard"
        description="Monitor your application performance and user engagement metrics."
        breadcrumbs={
          <div className="flex items-center gap-2">
            <a href="/" className="hover:text-foreground">Home</a>
            <span>/</span>
            <span className="text-foreground">Analytics</span>
          </div>
        }
        actions={
          <div className="flex gap-2">
            <Button variant="outline" size="sm">
              <Download className="h-4 w-4 mr-2" />
              Export Data
            </Button>
            <Button size="sm">
              <Settings className="h-4 w-4 mr-2" />
              Configure
            </Button>
          </div>
        }
      />
      
      <div className="p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="p-4 bg-card border rounded-lg">
              <h3 className="font-semibold text-sm text-muted-foreground mb-2">
                Metric {i + 1}
              </h3>
              <div className="text-2xl font-bold">
                {(Math.random() * 10000).toFixed(0)}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                +{(Math.random() * 20).toFixed(1)}% from last week
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  ),
};

export const LongTitle: Story = {
  args: {
    title: 'This is a Very Long Title That Might Need to Wrap on Smaller Screens',
    description: 'This is also a longer description that provides more context about what this page contains and what users can expect to find here. It demonstrates how the component handles longer content.',
    breadcrumbs: (
      <div className="flex items-center gap-2">
        <a href="/" className="hover:text-foreground">Home</a>
        <span>/</span>
        <a href="/category" className="hover:text-foreground">Very Long Category Name</a>
        <span>/</span>
        <a href="/category/sub" className="hover:text-foreground">Subcategory</a>
        <span>/</span>
        <span className="text-foreground">Current Page with Long Name</span>
      </div>
    ),
    actions: (
      <div className="flex gap-2">
        <Button variant="outline" size="sm">Cancel</Button>
        <Button size="sm">Save Changes</Button>
      </div>
    ),
  },
};