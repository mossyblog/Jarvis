/**
 * QuickActionsPanel Storybook Stories
 * 
 * Stories for the QuickActionsPanel component showcasing different variants,
 * states, and interactions.
 */

import type { Meta, StoryObj } from '@storybook/react';
import { action } from '@storybook/addon-actions';
import { QuickActionsPanel } from './QuickActionsPanel';
import { KeyboardNavigationProvider } from '../keyboard/KeyboardNavigationProvider';

const meta: Meta<typeof QuickActionsPanel> = {
  title: 'Components/Panels/QuickActionsPanel',
  component: QuickActionsPanel,
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component: 'A comprehensive quick actions panel with keyboard shortcuts, import/export functionality, and template browsing.',
      },
    },
  },
  decorators: [
    (Story) => (
      <KeyboardNavigationProvider>
        <div className="max-w-7xl mx-auto p-6">
          <Story />
        </div>
      </KeyboardNavigationProvider>
    ),
  ],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['horizontal', 'vertical', 'grid'],
      description: 'Panel layout variant',
    },
    size: {
      control: { type: 'select' },
      options: ['compact', 'normal', 'expanded'],
      description: 'Panel size',
    },
    showShortcuts: {
      control: { type: 'boolean' },
      description: 'Show keyboard shortcuts hints',
    },
  },
  args: {
    userEntityId: 'user-123',
    onCreatePage: action('onCreatePage'),
    onOpenTemplates: action('onOpenTemplates'),
    onImport: action('onImport'),
    onExport: action('onExport'),
  },
};

export default meta;
type Story = StoryObj<typeof QuickActionsPanel>;

// Default story showing normal grid layout
export const Default: Story = {
  args: {
    variant: 'grid',
    size: 'normal',
    showShortcuts: true,
    loading: {},
    errors: {},
  },
};

// Compact variant for smaller spaces
export const Compact: Story = {
  args: {
    variant: 'grid',
    size: 'compact',
    showShortcuts: true,
    loading: {},
    errors: {},
  },
};

// Expanded variant with larger cards
export const Expanded: Story = {
  args: {
    variant: 'grid',
    size: 'expanded',
    showShortcuts: true,
    loading: {},
    errors: {},
  },
};

// Horizontal layout variant
export const Horizontal: Story = {
  args: {
    variant: 'horizontal',
    size: 'normal',
    showShortcuts: true,
    loading: {},
    errors: {},
  },
};

// Vertical layout variant
export const Vertical: Story = {
  args: {
    variant: 'vertical',
    size: 'normal',
    showShortcuts: true,
    loading: {},
    errors: {},
  },
};

// Loading states
export const WithLoading: Story = {
  args: {
    variant: 'grid',
    size: 'normal',
    showShortcuts: true,
    loading: {
      creating: true,
      importing: false,
      exporting: false,
    },
    errors: {},
  },
};

// Error states
export const WithErrors: Story = {
  args: {
    variant: 'grid',
    size: 'normal',
    showShortcuts: true,
    loading: {},
    errors: {
      import: 'Failed to import file. Please check the file format.',
      export: 'Export failed due to insufficient permissions.',
    },
  },
};

// Without keyboard shortcuts
export const NoShortcuts: Story = {
  args: {
    variant: 'grid',
    size: 'normal',
    showShortcuts: false,
    loading: {},
    errors: {},
  },
};

// Mobile responsive preview
export const Mobile: Story = {
  args: {
    variant: 'grid',
    size: 'compact',
    showShortcuts: false,
    loading: {},
    errors: {},
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};

// Tablet responsive preview
export const Tablet: Story = {
  args: {
    variant: 'grid',
    size: 'normal',
    showShortcuts: true,
    loading: {},
    errors: {},
  },
  parameters: {
    viewport: {
      defaultViewport: 'tablet',
    },
  },
};

// Interactive demo with realistic actions
export const Interactive: Story = {
  args: {
    variant: 'grid',
    size: 'normal',
    showShortcuts: true,
    loading: {},
    errors: {},
    onCreatePage: () => {
      action('onCreatePage')();
      alert('Create Page dialog would open here');
    },
    onOpenTemplates: () => {
      action('onOpenTemplates')();
      alert('Template gallery would open here');
    },
    onImport: async (file: File) => {
      action('onImport')(file);
      await new Promise(resolve => setTimeout(resolve, 1000));
      alert(`Import completed for: ${file.name}`);
    },
    onExport: async (format: 'json' | 'zip') => {
      action('onExport')(format);
      await new Promise(resolve => setTimeout(resolve, 1000));
      alert(`Export completed in ${format.toUpperCase()} format`);
    },
  },
};