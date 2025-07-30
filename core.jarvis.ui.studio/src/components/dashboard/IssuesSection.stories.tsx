import type { Meta, StoryObj } from '@storybook/react';
import { IssuesSection } from './IssuesSection';

const meta: Meta<typeof IssuesSection> = {
  title: 'Dashboard/IssuesSection',
  component: IssuesSection,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const InCard: Story = {
  render: () => (
    <div className="w-full max-w-2xl bg-card border rounded-lg p-6">
      <div className="mb-4">
        <h2 className="text-lg font-semibold">System Status</h2>
        <p className="text-sm text-muted-foreground">
          Current issues and warnings
        </p>
      </div>
      <IssuesSection />
    </div>
  ),
};

export const InDashboard: Story = {
  render: () => (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-6xl mx-auto space-y-6">
        <div>
          <h1 className="text-3xl font-bold">Dashboard</h1>
          <p className="text-muted-foreground">Welcome back! Here's what's happening.</p>
        </div>
        
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <div className="bg-card border rounded-lg p-6">
              <h2 className="text-lg font-semibold mb-4">Issues & Alerts</h2>
              <IssuesSection />
            </div>
          </div>
          
          <div className="space-y-4">
            <div className="bg-card border rounded-lg p-4">
              <h3 className="font-semibold text-sm mb-2">Quick Stats</h3>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Active Users</span>
                  <span className="font-mono">1,234</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">API Calls</span>
                  <span className="font-mono">5,678</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Uptime</span>
                  <span className="font-mono text-green-600">99.9%</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  ),
  parameters: {
    layout: 'fullscreen',
  },
};