import type { Meta, StoryObj } from '@storybook/react';
import { StatusFooter } from './StatusFooter';
import { ApiStatusProvider } from '../../contexts/ApiStatusContext';

const meta: Meta<typeof StatusFooter> = {
  title: 'Layout/StatusFooter',
  component: StatusFooter,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: {
        component: 'A status footer that displays build information, network statistics, and connection status.',
      },
    },
  },
  decorators: [
    (Story) => (
      <ApiStatusProvider>
        <div className="h-screen bg-background">
          <div className="p-8">
            <h1 className="text-2xl font-bold">Sample Page Content</h1>
            <p className="text-muted-foreground mt-2">
              The status footer is fixed at the bottom of the screen.
            </p>
          </div>
          <Story />
        </div>
      </ApiStatusProvider>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof StatusFooter>;

export const Default: Story = {};

export const WithMockData: Story = {
  decorators: [
    (Story) => {
      // Mock some network activity
      setTimeout(() => {
        fetch('/api/test').catch(() => {});
        fetch('/api/test2').catch(() => {});
      }, 100);
      
      return (
        <ApiStatusProvider>
          <div className="h-screen bg-background">
            <div className="p-8">
              <h1 className="text-2xl font-bold">Sample Page Content</h1>
              <p className="text-muted-foreground mt-2">
                The status footer shows network activity and connection status.
              </p>
              <div className="mt-4 space-y-2">
                <button 
                  className="bg-primary text-primary-foreground px-4 py-2 rounded"
                  onClick={() => fetch('/api/test').catch(() => {})}
                >
                  Make API Request
                </button>
                <button 
                  className="bg-destructive text-destructive-foreground px-4 py-2 rounded ml-2"
                  onClick={() => fetch('/api/error').catch(() => {})}
                >
                  Make Failing Request
                </button>
              </div>
            </div>
            <Story />
          </div>
        </ApiStatusProvider>
      );
    },
  ],
};

export const MobileView: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};

export const TabletView: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'tablet',
    },
  },
};