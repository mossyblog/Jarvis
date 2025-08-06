import type { Meta, StoryObj } from '@storybook/react-vite';
import { NetworkMonitor } from './network-monitor';
import { useState } from 'react';

const meta: Meta<typeof NetworkMonitor> = {
  title: 'UI/NetworkMonitor',
  component: NetworkMonitor,
  parameters: {
    layout: 'fullscreen',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithMockActivity: Story = {
  render: () => {
    const [triggerRequest, setTriggerRequest] = useState(0);
    
    const simulateNetworkActivity = () => {
      // Simulate multiple API calls
      fetch('https://jsonplaceholder.typicode.com/posts/1')
        .catch(() => {});
      fetch('https://jsonplaceholder.typicode.com/users/1')
        .catch(() => {});
      fetch('https://jsonplaceholder.typicode.com/albums/1')
        .catch(() => {});
      
      setTriggerRequest(prev => prev + 1);
    };

    return (
      <div className="min-h-screen bg-background relative">
        <div className="p-8">
          <div className="space-y-4">
            <h2 className="text-2xl font-bold">Network Monitor Demo</h2>
            <p className="text-muted-foreground">
              The network monitor appears in the bottom-right corner. Click the button below to simulate network activity.
            </p>
            <button
              onClick={simulateNetworkActivity}
              className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors"
            >
              Trigger Network Requests ({triggerRequest})
            </button>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mt-8">
              <div className="p-4 border rounded-lg">
                <h3 className="font-semibold mb-2">Features</h3>
                <ul className="text-sm text-muted-foreground space-y-1">
                  <li>• Real-time request monitoring</li>
                  <li>• Request/response tracking</li>
                  <li>• Network speed calculation</li>
                  <li>• Active request indicators</li>
                </ul>
              </div>
              <div className="p-4 border rounded-lg">
                <h3 className="font-semibold mb-2">States</h3>
                <ul className="text-sm text-muted-foreground space-y-1">
                  <li>• Collapsed (shows basic stats)</li>
                  <li>• Expanded (shows full details)</li>
                  <li>• Active requests (pulsing indicator)</li>
                  <li>• Recent activity log</li>
                </ul>
              </div>
              <div className="p-4 border rounded-lg">
                <h3 className="font-semibold mb-2">Usage</h3>
                <ul className="text-sm text-muted-foreground space-y-1">
                  <li>• Click to expand</li>
                  <li>• Auto-tracks fetch() calls</li>
                  <li>• Shows response times</li>
                  <li>• Fixed bottom-right position</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
        <NetworkMonitor />
      </div>
    );
  },
};