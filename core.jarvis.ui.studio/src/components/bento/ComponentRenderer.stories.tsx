/**
 * ComponentRenderer Storybook Stories
 * 
 * Stories demonstrating the ComponentRenderer with different component types.
 */

import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { ComponentRenderer } from './ComponentRenderer';
import type { GridComponent } from '@/types/bento';
import { DeviceType } from '@/types/bento';

const meta: Meta<typeof ComponentRenderer> = {
  title: 'Bento/ComponentRenderer',
  component: ComponentRenderer,
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component: `
The ComponentRenderer acts as a factory that renders React components 
based on their type configuration. It handles component loading, error states, 
and provides fallbacks for unknown component types.

## Supported Component Types
- **placeholder**: Basic placeholder component
- **metric**: KPI/metric display component
- **chart**: Chart/graph placeholder
- **text**: Simple text display component
        `
      }
    }
  },
  argTypes: {
    component: {
      description: 'Grid component configuration',
      control: { type: 'object' }
    },
    deviceType: {
      description: 'Target device type',
      control: { type: 'select' },
      options: [DeviceType.Desktop, DeviceType.Tablet, DeviceType.Mobile]
    },
    loading: {
      description: 'Whether component is in loading state',
      control: { type: 'boolean' }
    }
  },
  decorators: [
    (Story) => (
      <div style={{ width: '300px', height: '200px' }}>
        <Story />
      </div>
    )
  ]
};

export default meta;
type Story = StoryObj<typeof ComponentRenderer>;

// Placeholder component story
export const Placeholder: Story = {
  args: {
    component: {
      id: 'placeholder-1',
      componentType: 'placeholder',
      position: { x: 0, y: 0, w: 2, h: 2 },
      props: {}
    } as GridComponent,
    gridSize: { w: 2, h: 2 },
    deviceType: DeviceType.Desktop
  }
};

// Metric component story
export const Metric: Story = {
  args: {
    component: {
      id: 'metric-1',
      componentType: 'metric',
      position: { x: 0, y: 0, w: 3, h: 2 },
      props: {
        title: 'Active Users',
        value: '1,234',
        change: '+12%'
      }
    } as GridComponent,
    gridSize: { w: 3, h: 2 },
    deviceType: DeviceType.Desktop
  }
};

// Chart component story
export const Chart: Story = {
  args: {
    component: {
      id: 'chart-1',
      componentType: 'chart',
      position: { x: 0, y: 0, w: 4, h: 3 },
      props: {
        title: 'Performance Chart'
      }
    } as GridComponent,
    gridSize: { w: 4, h: 3 },
    deviceType: DeviceType.Desktop
  }
};

// Text component story
export const Text: Story = {
  args: {
    component: {
      id: 'text-1',
      componentType: 'text',
      position: { x: 0, y: 0, w: 4, h: 2 },
      props: {
        text: 'This is a sample text component that can display any text content.'
      }
    } as GridComponent,
    gridSize: { w: 4, h: 2 },
    deviceType: DeviceType.Desktop
  }
};

// Loading state story
export const Loading: Story = {
  args: {
    component: {
      id: 'loading-1',
      componentType: 'metric',
      position: { x: 0, y: 0, w: 3, h: 2 },
      props: {
        title: 'Loading Metric',
        value: '0',
        change: '0%'
      }
    } as GridComponent,
    gridSize: { w: 3, h: 2 },
    deviceType: DeviceType.Desktop,
    loading: true
  }
};

// Error state story
export const ErrorState: Story = {
  args: {
    component: {
      id: 'error-1',
      componentType: 'metric',
      position: { x: 0, y: 0, w: 3, h: 2 },
      props: {
        title: 'Error Metric',
        value: '0',
        change: '0%'
      }
    } as GridComponent,
    gridSize: { w: 3, h: 2 },
    deviceType: DeviceType.Desktop,
    error: new Error('Failed to load component data')
  }
};

// Unknown component type story
export const UnknownType: Story = {
  args: {
    component: {
      id: 'unknown-1',
      componentType: 'unknown-component-type',
      position: { x: 0, y: 0, w: 2, h: 2 },
      props: {}
    } as GridComponent,
    gridSize: { w: 2, h: 2 },
    deviceType: DeviceType.Desktop
  }
};