import type { Meta, StoryObj } from '@storybook/react-vite';
import { Badge } from './badge';

const meta: Meta<typeof Badge> = {
  title: 'UI/Badge',
  component: Badge,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['default', 'secondary', 'destructive', 'outline'],
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children: 'Badge',
  },
};

export const Secondary: Story = {
  args: {
    variant: 'secondary',
    children: 'Secondary',
  },
};

export const Destructive: Story = {
  args: {
    variant: 'destructive',
    children: 'Destructive',
  },
};

export const Outline: Story = {
  args: {
    variant: 'outline',
    children: 'Outline',
  },
};

export const AllVariants: Story = {
  render: () => (
    <div className="flex gap-2 flex-wrap">
      <Badge>Default</Badge>
      <Badge variant="secondary">Secondary</Badge>
      <Badge variant="destructive">Destructive</Badge>
      <Badge variant="outline">Outline</Badge>
    </div>
  ),
};

export const StatusBadges: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="space-y-2">
        <h4 className="text-sm font-medium">Status Indicators</h4>
        <div className="flex gap-2 flex-wrap">
          <Badge className="bg-green-100 text-green-800 hover:bg-green-100/80">Active</Badge>
          <Badge className="bg-yellow-xl0 text-yellow-800 hover:bg-yellow-xl0/80">Pending</Badge>
          <Badge className="bg-red-100 text-red-800 hover:bg-red-100/80">Error</Badge>
          <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100/80">Processing</Badge>
        </div>
      </div>
      
      <div className="space-y-2">
        <h4 className="text-sm font-medium">Priority Levels</h4>
        <div className="flex gap-2 flex-wrap">
          <Badge variant="destructive">High</Badge>
          <Badge className="bg-orange-100 text-orange-800 hover:bg-orange-100/80">Medium</Badge>
          <Badge variant="secondary">Low</Badge>
        </div>
      </div>
      
      <div className="space-y-2">
        <h4 className="text-sm font-medium">Categories</h4>
        <div className="flex gap-2 flex-wrap">
          <Badge variant="outline">Frontend</Badge>
          <Badge variant="outline">Backend</Badge>
          <Badge variant="outline">Database</Badge>
          <Badge variant="outline">DevOps</Badge>
        </div>
      </div>
    </div>
  ),
};

export const WithIcons: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="flex gap-2 flex-wrap">
        <Badge className="gap-1">
          <div className="w-xs h-xs bg-current rounded-full" />
          Online
        </Badge>
        <Badge variant="secondary" className="gap-1">
          <div className="w-xs h-xs bg-current rounded-full" />
          Offline
        </Badge>
        <Badge variant="destructive" className="gap-1">
          ⚠️ Alert
        </Badge>
        <Badge variant="outline" className="gap-1">
          ✓ Verified
        </Badge>
      </div>
    </div>
  ),
};

export const Sizes: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Badge className="text-xs px-1.5 py-0.5">Small</Badge>
        <Badge>Default</Badge>
        <Badge className="text-sm px-3 py-1">Large</Badge>
      </div>
    </div>
  ),
};