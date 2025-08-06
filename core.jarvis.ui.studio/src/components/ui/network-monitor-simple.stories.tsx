import type { Meta, StoryObj } from '@storybook/react-vite';
import { NetworkMonitor } from './network-monitor-simple';

const meta: Meta<typeof NetworkMonitor> = {
  title: 'UI/NetworkMonitorSimple',
  component: NetworkMonitor,
  parameters: {
    layout: 'fullscreen',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <div className="min-h-screen bg-background relative">
      <div className="p-8">
        <div className="space-y-4">
          <h2 className="text-2xl font-bold">Simple Network Monitor</h2>
          <p className="text-muted-foreground">
            A simplified version of the network monitor that appears in the bottom-right corner.
          </p>
          <div className="p-4 border rounded-lg">
            <h3 className="font-semibold mb-2">Features</h3>
            <ul className="text-sm text-muted-foreground space-y-1">
              <li>• Minimal design</li>
              <li>• Request/response counters</li>
              <li>• Fixed positioning</li>
              <li>• Automatic fetch() interception</li>
            </ul>
          </div>
        </div>
      </div>
      <NetworkMonitor />
    </div>
  ),
};