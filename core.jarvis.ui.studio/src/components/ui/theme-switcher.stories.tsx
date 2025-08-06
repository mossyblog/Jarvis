import type { Meta, StoryObj } from '@storybook/react-vite';
import { ThemeSwitcher, ThemeSwitcherCompact } from './theme-switcher';

const meta: Meta<typeof ThemeSwitcher> = {
  title: 'UI/ThemeSwitcher',
  component: ThemeSwitcher,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Compact: Story = {
  render: () => <ThemeSwitcherCompact />,
};

export const CompactWithLabel: Story = {
  render: () => <ThemeSwitcherCompact showLabel={true} />,
};

export const InLayout: Story = {
  render: () => (
    <div className="w-full max-w-4xl mx-auto">
      <div className="bg-card border rounded-lg p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Settings</h2>
            <p className="text-muted-foreground">Customize your experience</p>
          </div>
          <ThemeSwitcher />
        </div>
        
        <div className="space-y-6">
          <div className="space-y-2">
            <h3 className="text-lg font-semibold">Appearance</h3>
            <p className="text-sm text-muted-foreground">
              Choose how the interface looks and feels. Use the theme switcher in the top-right to change themes and toggle between light and dark modes.
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-4 border rounded-lg">
              <h4 className="font-medium mb-2">Theme Options</h4>
              <ul className="text-sm text-muted-foreground space-y-1">
                <li>• Default theme</li>
                <li>• Supabase theme</li>
                <li>• Auto theme detection</li>
              </ul>
            </div>
            <div className="p-4 border rounded-lg">
              <h4 className="font-medium mb-2">Mode Options</h4>
              <ul className="text-sm text-muted-foreground space-y-1">
                <li>• Light mode</li>
                <li>• Dark mode</li>
                <li>• Instant switching</li>
              </ul>
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

export const AllVariants: Story = {
  render: () => (
    <div className="space-y-8 p-6">
      <div>
        <h3 className="text-lg font-semibold mb-4">Theme Switcher Variants</h3>
        <div className="space-y-6">
          <div className="flex items-center gap-4">
            <span className="w-mdxl text-sm text-muted-foreground">Default:</span>
            <ThemeSwitcher />
          </div>
          
          <div className="flex items-center gap-4">
            <span className="w-mdxl text-sm text-muted-foreground">Compact:</span>
            <ThemeSwitcherCompact />
          </div>
          
          <div className="flex items-center gap-4">
            <span className="w-mdxl text-sm text-muted-foreground">With Label:</span>
            <ThemeSwitcherCompact showLabel={true} />
          </div>
        </div>
      </div>
      
      <div className="border-t pt-6">
        <h4 className="font-medium mb-2">Usage Examples</h4>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="p-4 border rounded-lg">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium">Header</span>
              <ThemeSwitcher />
            </div>
            <p className="text-xs text-muted-foreground">
              Perfect for app headers and navigation bars
            </p>
          </div>
          
          <div className="p-4 border rounded-lg">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium">Settings Panel</span>
              <ThemeSwitcherCompact showLabel={true} />
            </div>
            <p className="text-xs text-muted-foreground">
              Great for settings pages and user preferences
            </p>
          </div>
        </div>
      </div>
    </div>
  ),
  parameters: {
    layout: 'fullscreen',
  },
};