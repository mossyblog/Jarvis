import type { Meta, StoryObj } from '@storybook/react';
import { NotificationCard } from './notification-card';
import { AlertCircle, CheckCircle, Info, AlertTriangle, X } from 'lucide-react';

const meta: Meta<typeof NotificationCard> = {
  title: 'UI/NotificationCard',
  component: NotificationCard,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['default', 'success', 'warning', 'error'],
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: 'Default Notification',
    description: 'This is a default notification card with some helpful information.',
  },
};

export const Success: Story = {
  args: {
    variant: 'success',
    icon: CheckCircle,
    title: 'Success!',
    description: 'Your changes have been saved successfully.',
  },
};

export const Warning: Story = {
  args: {
    variant: 'warning',
    icon: AlertTriangle,
    title: 'Warning',
    description: 'This action may have unintended consequences. Please review before proceeding.',
  },
};

export const Error: Story = {
  args: {
    variant: 'error',
    icon: AlertCircle,
    title: 'Error Occurred',
    description: 'Something went wrong while processing your request. Please try again.',
  },
};

export const WithChildren: Story = {
  render: () => (
    <NotificationCard
      variant="warning"
      icon={Info}
      title="Development Mode"
      description="Additional development tools are available."
    >
      <div className="mt-3 space-y-2">
        <button className="px-3 py-1 text-xs bg-primary text-primary-foreground rounded hover:bg-primary/90">
          Enable Debug Mode
        </button>
        <p className="text-xs text-muted-foreground">
          Debug mode will show additional logging information in the console.
        </p>
      </div>
    </NotificationCard>
  ),
};

export const AllVariants: Story = {
  render: () => (
    <div className="space-y-4 w-full max-w-md">
      <NotificationCard
        title="Default"
        description="This is a default notification."
        icon={Info}
      />
      
      <NotificationCard
        variant="success"
        title="Success"
        description="Operation completed successfully."
        icon={CheckCircle}
      />
      
      <NotificationCard
        variant="warning"
        title="Warning"
        description="Please review this important information."
        icon={AlertTriangle}
      />
      
      <NotificationCard
        variant="error"
        title="Error"
        description="An error occurred during processing."
        icon={AlertCircle}
      />
    </div>
  ),
  parameters: {
    layout: 'fullscreen',
  },
};

export const Dismissible: Story = {
  render: () => (
    <NotificationCard
      variant="success"
      icon={CheckCircle}
      title="Dismissible Notification"
      description="This notification can be dismissed."
    >
      <button className="absolute top-2 right-2 p-1 hover:bg-background/80 rounded">
        <X size={16} />
      </button>
    </NotificationCard>
  ),
};