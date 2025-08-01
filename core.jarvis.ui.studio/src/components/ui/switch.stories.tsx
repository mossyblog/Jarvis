import type { Meta, StoryObj } from '@storybook/react-vite';
import { Switch } from './switch';
import { Label } from './label';

const meta: Meta<typeof Switch> = {
  title: 'UI/Switch',
  component: Switch,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    checked: {
      control: { type: 'boolean' },
    },
    disabled: {
      control: { type: 'boolean' },
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Checked: Story = {
  args: {
    checked: true,
  },
};

export const Disabled: Story = {
  args: {
    disabled: true,
  },
};

export const DisabledChecked: Story = {
  args: {
    disabled: true,
    checked: true,
  },
};

export const WithLabel: Story = {
  render: () => (
    <div className="flex items-center space-x-2">
      <Switch id="notifications" />
      <Label htmlFor="notifications">Enable notifications</Label>
    </div>
  ),
};

export const SettingsExample: Story = {
  render: () => (
    <div className="w-full max-w-md space-y-6">
      <div>
        <h3 className="text-lg font-semibold mb-4">Privacy Settings</h3>
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label>Email Notifications</Label>
              <p className="text-sm text-muted-foreground">
                Receive email notifications for important updates
              </p>
            </div>
            <Switch />
          </div>
          
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label>Marketing Emails</Label>
              <p className="text-sm text-muted-foreground">
                Receive promotional emails and newsletters
              </p>
            </div>
            <Switch />
          </div>
          
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label>Public Profile</Label>
              <p className="text-sm text-muted-foreground">
                Make your profile visible to other users
              </p>
            </div>
            <Switch defaultChecked />
          </div>
          
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label className="text-muted-foreground">Beta Features</Label>
              <p className="text-sm text-muted-foreground">
                Access experimental features (coming soon)
              </p>
            </div>
            <Switch disabled />
          </div>
        </div>
      </div>
    </div>
  ),
};

export const AllStates: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="space-y-2">
        <h4 className="text-sm font-medium">Switch States</h4>
        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <Switch />
            <span className="text-sm">Default (unchecked)</span>
          </div>
          
          <div className="flex items-center gap-3">
            <Switch defaultChecked />
            <span className="text-sm">Checked</span>
          </div>
          
          <div className="flex items-center gap-3">
            <Switch disabled />
            <span className="text-sm text-muted-foreground">Disabled (unchecked)</span>
          </div>
          
          <div className="flex items-center gap-3">
            <Switch disabled checked />
            <span className="text-sm text-muted-foreground">Disabled (checked)</span>
          </div>
        </div>
      </div>
    </div>
  ),
};