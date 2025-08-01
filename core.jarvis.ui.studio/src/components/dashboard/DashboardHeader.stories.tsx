import type { Meta, StoryObj } from '@storybook/react-vite';
import { DashboardHeader } from './DashboardHeader';

const meta: Meta<typeof DashboardHeader> = {
  title: 'Dashboard/DashboardHeader',
  component: DashboardHeader,
  parameters: {
    layout: 'fullscreen',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const InDashboardLayout: Story = {
  render: () => (
    <div className="min-h-screen bg-background">
      <DashboardHeader />
      <div className="p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="p-4 bg-card border rounded-lg">
              <h3 className="font-semibold text-sm text-muted-foreground mb-2">
                Metric {i + 1}
              </h3>
              <div className="text-2xl font-bold">
                {(Math.random() * 10000).toFixed(0)}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                +{(Math.random() * 20).toFixed(1)}% from last month
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  ),
};