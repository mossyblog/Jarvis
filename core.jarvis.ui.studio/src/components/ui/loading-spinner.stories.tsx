import type { Meta, StoryObj } from '@storybook/react';
import { LoadingSpinner } from './loading-spinner';

const meta: Meta<typeof LoadingSpinner> = {
  title: 'UI/LoadingSpinner',
  component: LoadingSpinner,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithText: Story = {
  render: () => (
    <div className="flex items-center gap-3">
      <LoadingSpinner />
      <span className="text-sm text-muted-foreground">Loading...</span>
    </div>
  ),
};

export const InCard: Story = {
  render: () => (
    <div className="w-80 p-6 bg-card border rounded-lg">
      <div className="flex flex-col items-center justify-center space-y-4">
        <LoadingSpinner />
        <div className="text-center space-y-1">
          <p className="text-sm font-medium">Loading content</p>
          <p className="text-xs text-muted-foreground">Please wait while we fetch your data</p>
        </div>
      </div>
    </div>
  ),
};

export const InlineLoading: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <span className="text-sm">Saving changes</span>
        <LoadingSpinner />
      </div>
      <div className="flex items-center gap-2">
        <span className="text-sm">Uploading files</span>
        <LoadingSpinner />
      </div>
      <div className="flex items-center gap-2">
        <span className="text-sm">Processing data</span>
        <LoadingSpinner />
      </div>
    </div>
  ),
};