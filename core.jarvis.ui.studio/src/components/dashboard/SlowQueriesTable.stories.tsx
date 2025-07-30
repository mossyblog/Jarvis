import type { Meta, StoryObj } from '@storybook/react';
import { SlowQueriesTable } from './SlowQueriesTable';

const meta: Meta<typeof SlowQueriesTable> = {
  title: 'Dashboard/SlowQueriesTable',
  component: SlowQueriesTable,
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
    <div className="w-full max-w-4xl bg-card border rounded-lg p-6">
      <div className="mb-4">
        <h2 className="text-lg font-semibold">Performance Monitoring</h2>
        <p className="text-sm text-muted-foreground">
          Slow queries and performance metrics
        </p>
      </div>
      <SlowQueriesTable />
    </div>
  ),
  parameters: {
    layout: 'fullscreen',
  },
};

export const InDashboard: Story = {
  render: () => (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-6xl mx-auto space-y-6">
        <div>
          <h1 className="text-3xl font-bold">Performance Dashboard</h1>
          <p className="text-muted-foreground">Monitor your application performance and identify bottlenecks.</p>
        </div>
        
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-4 mb-6">
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Avg Response Time</div>
            <div className="text-2xl font-bold text-orange-600">245ms</div>
            <div className="text-xs text-muted-foreground">+12% from yesterday</div>
          </div>
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Slow Queries</div>
            <div className="text-2xl font-bold text-red-600">23</div>
            <div className="text-xs text-muted-foreground">-5% from yesterday</div>
          </div>
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Database Load</div>
            <div className="text-2xl font-bold text-yellow-600">68%</div>
            <div className="text-xs text-muted-foreground">+3% from yesterday</div>
          </div>
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Cache Hit Rate</div>
            <div className="text-2xl font-bold text-green-600">94%</div>
            <div className="text-xs text-muted-foreground">+1% from yesterday</div>
          </div>
        </div>
        
        <div className="bg-card border rounded-lg p-6">
          <div className="mb-4">
            <h2 className="text-lg font-semibold">Slow Query Analysis</h2>
            <p className="text-sm text-muted-foreground">
              Queries taking longer than 1 second to execute
            </p>
          </div>
          <SlowQueriesTable />
        </div>
      </div>
    </div>
  ),
  parameters: {
    layout: 'fullscreen',
  },
};