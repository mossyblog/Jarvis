/**
 * ViewSwitcher Storybook Stories
 * 
 * Interactive examples and documentation for the ViewSwitcher component.
 */

import type { Meta, StoryObj } from '@storybook/react';
import { useState } from 'react';
import { ViewSwitcher, ViewSwitcherCompact, ViewSwitcherWithLabels, ViewSwitcherLarge } from './view-switcher';
import { useViewState } from '@/hooks/useViewState';
import type { ViewMode } from '@/hooks/useViewState';

const meta: Meta<typeof ViewSwitcher> = {
  title: 'UI/ViewSwitcher',
  component: ViewSwitcher,
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component: 'A toggle component for switching between list, grid, and card view modes with persistence support.'
      }
    }
  },
  argTypes: {
    viewMode: {
      control: 'select',
      options: ['list', 'grid', 'card'],
      description: 'Current view mode'
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
      description: 'Size variant'
    },
    variant: {
      control: 'select',
      options: ['default', 'outline', 'ghost'],
      description: 'Visual variant'
    },
    showTooltips: {
      control: 'boolean',
      description: 'Whether to show tooltips'
    },
    showLabels: {
      control: 'boolean',
      description: 'Whether to show labels alongside icons'
    },
    disabled: {
      control: 'boolean',
      description: 'Disabled state'
    },
    isChanging: {
      control: 'boolean',
      description: 'Loading state when view is changing'
    }
  },
  tags: ['autodocs']
};

export default meta;
type Story = StoryObj<typeof ViewSwitcher>;

// ============================================================================
// Basic Stories
// ============================================================================

export const Default: Story = {
  args: {
    viewMode: 'list',
    onViewModeChange: () => {},
    size: 'md',
    variant: 'outline',
    showTooltips: true,
    showLabels: false,
    disabled: false,
    isChanging: false
  },
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>(args.viewMode);
    
    return (
      <ViewSwitcher
        {...args}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />
    );
  }
};

export const WithLabels: Story = {
  args: {
    ...Default.args,
    showLabels: true,
    showTooltips: false
  },
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>(args.viewMode);
    
    return (
      <ViewSwitcher
        {...args}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />
    );
  }
};

export const Compact: Story = {
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>('list');
    
    return (
      <ViewSwitcherCompact
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        variant={args?.variant}
        disabled={args?.disabled}
        isChanging={args?.isChanging}
      />
    );
  }
};

export const Large: Story = {
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>('grid');
    
    return (
      <ViewSwitcherLarge
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        variant={args?.variant}
        disabled={args?.disabled}
        isChanging={args?.isChanging}
      />
    );
  }
};

// ============================================================================
// State Stories
// ============================================================================

export const AllSizes: Story = {
  render: () => {
    const [viewMode, setViewMode] = useState<ViewMode>('grid');
    
    return (
      <div className="space-y-4">
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Small</h3>
          <ViewSwitcher
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            size="sm"
          />
        </div>
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Medium (default)</h3>
          <ViewSwitcher
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            size="md"
          />
        </div>
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Large</h3>
          <ViewSwitcher
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            size="lg"
          />
        </div>
      </div>
    );
  }
};

export const AllVariants: Story = {
  render: () => {
    const [viewMode, setViewMode] = useState<ViewMode>('card');
    
    return (
      <div className="space-y-4">
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Default</h3>
          <ViewSwitcher
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            variant="default"
          />
        </div>
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Outline</h3>
          <ViewSwitcher
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            variant="outline"
          />
        </div>
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Ghost</h3>
          <ViewSwitcher
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            variant="ghost"
          />
        </div>
      </div>
    );
  }
};

export const Disabled: Story = {
  args: {
    ...Default.args,
    disabled: true
  },
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>(args.viewMode);
    
    return (
      <ViewSwitcher
        {...args}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />
    );
  }
};

export const Loading: Story = {
  args: {
    ...Default.args,
    isChanging: true
  },
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>(args.viewMode);
    
    return (
      <ViewSwitcher
        {...args}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />
    );
  }
};

export const LimitedModes: Story = {
  args: {
    ...Default.args,
    availableModes: ['list', 'grid'] as ViewMode[]
  },
  render: (args) => {
    const [viewMode, setViewMode] = useState<ViewMode>(args.viewMode);
    
    return (
      <ViewSwitcher
        {...args}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />
    );
  }
};

// ============================================================================
// Integration Stories
// ============================================================================

export const WithPersistedState: Story = {
  render: () => {
    const { viewMode, setViewMode, isChanging } = useViewState({
      pageId: 'storybook-example',
      defaultView: 'grid'
    });
    
    return (
      <div className="space-y-4">
        <div className="text-sm text-muted-foreground">
          This example uses the useViewState hook with localStorage persistence.
          <br />
          Refresh the page to see the state persist!
        </div>
        <ViewSwitcher
          viewMode={viewMode}
          onViewModeChange={setViewMode}
          isChanging={isChanging}
        />
        <div className="text-xs text-muted-foreground">
          Current view: <code>{viewMode}</code>
        </div>
      </div>
    );
  }
};

export const PrebuiltVariants: Story = {
  render: () => {
    const { viewMode, setViewMode } = useViewState({
      pageId: 'prebuilt-example',
      defaultView: 'list'
    });
    
    return (
      <div className="space-y-6">
        <div className="space-y-2">
          <h3 className="text-sm font-medium">ViewSwitcherCompact</h3>
          <ViewSwitcherCompact
            viewMode={viewMode}
            onViewModeChange={setViewMode}
          />
        </div>
        
        <div className="space-y-2">
          <h3 className="text-sm font-medium">ViewSwitcherWithLabels</h3>
          <ViewSwitcherWithLabels
            viewMode={viewMode}
            onViewModeChange={setViewMode}
          />
        </div>
        
        <div className="space-y-2">
          <h3 className="text-sm font-medium">ViewSwitcherLarge</h3>
          <ViewSwitcherLarge
            viewMode={viewMode}
            onViewModeChange={setViewMode}
          />
        </div>
      </div>
    );
  }
};

// ============================================================================
// Real-world Usage Examples
// ============================================================================

export const InToolbar: Story = {
  render: () => {
    const { viewMode, setViewMode } = useViewState({
      pageId: 'toolbar-example',
      defaultView: 'grid'
    });
    
    return (
      <div className="border border-border rounded-lg p-4 bg-background">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <h2 className="text-lg font-semibold">Project Files</h2>
            <div className="text-sm text-muted-foreground">
              42 items
            </div>
          </div>
          
          <div className="flex items-center gap-2">
            <ViewSwitcherCompact
              viewMode={viewMode}
              onViewModeChange={setViewMode}
            />
          </div>
        </div>
        
        <div className="mt-4 p-8 border border-dashed border-border rounded-md text-center text-muted-foreground">
          Content would be displayed in <strong>{viewMode}</strong> view here
        </div>
      </div>
    );
  }
};