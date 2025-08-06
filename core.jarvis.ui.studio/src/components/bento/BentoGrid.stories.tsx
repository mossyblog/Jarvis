/**
 * BentoGrid Storybook Stories
 * 
 * Stories demonstrating the BentoGrid component in various configurations.
 */

import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { BentoGrid } from './BentoGrid';
import type { BentoGrid as BentoGridType, GridPosition } from '@/types/bento';
import { DeviceType } from '@/types/bento';

// Sample grid data
const sampleGrid: BentoGridType = {
  id: 'sample-grid',
  name: 'Sample Grid',
  device: DeviceType.Desktop,
  columns: 12,
  gap: 16,
  rowHeight: 100,
  components: [
    {
      id: 'metric1',
      componentType: 'metric',
      position: { x: 0, y: 0, w: 3, h: 2 },
      props: {
        title: 'Active Users',
        value: '2,534',
        change: '+12%'
      }
    },
    {
      id: 'metric2',
      componentType: 'metric',
      position: { x: 3, y: 0, w: 3, h: 2 },
      props: {
        title: 'Revenue',
        value: '$15,234',
        change: '+8%'
      }
    },
    {
      id: 'chart1',
      componentType: 'chart',
      position: { x: 6, y: 0, w: 6, h: 4 },
      props: {
        title: 'Performance Overview'
      }
    },
    {
      id: 'text1',
      componentType: 'text',
      position: { x: 0, y: 2, w: 6, h: 2 },
      props: {
        text: 'Welcome to the Bento Grid System demonstration.'
      }
    }
  ],
  settings: {
    snapToGrid: true,
    gridColor: '#e5e7eb',
    allowOverlap: false,
    compactMode: 'vertical'
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString()
};

const meta: Meta<typeof BentoGrid> = {
  title: 'Bento/BentoGrid',
  component: BentoGrid,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: {
        component: `
The BentoGrid component is the main container for the Bento Grid System. 
It provides a flexible, responsive layout engine with drag-and-drop functionality.

## Features
- Responsive grid layout with configurable columns and rows
- Drag and drop component repositioning
- Component resizing with handles
- Visual grid overlay for editing mode
- Support for different device types (desktop, tablet, mobile)
- Component locking and selection states
        `
      }
    }
  },
  argTypes: {
    grid: {
      description: 'Grid configuration with components and settings',
      control: { type: 'object' }
    },
    deviceType: {
      description: 'Target device type for responsive behavior',
      control: { type: 'select' },
      options: [DeviceType.Desktop, DeviceType.Tablet, DeviceType.Mobile]
    },
    isEditing: {
      description: 'Whether the grid is in edit mode (enables drag/drop)',
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof BentoGrid>;

// Default story
export const Default: Story = {
  args: {
    grid: sampleGrid,
    deviceType: DeviceType.Desktop,
    isEditing: false,
  }
};

// Edit mode story
export const EditMode: Story = {
  args: {
    grid: sampleGrid,
    deviceType: DeviceType.Desktop,
    isEditing: true,
  }
};

// Tablet view story
export const TabletView: Story = {
  args: {
    grid: {
      ...sampleGrid,
      columns: 8,
      device: DeviceType.Tablet
    },
    deviceType: DeviceType.Tablet,
    isEditing: false,
  }
};

// Mobile view story
export const MobileView: Story = {
  args: {
    grid: {
      ...sampleGrid,
      columns: 4,
      device: DeviceType.Mobile,
      components: sampleGrid.components.map(component => ({
        ...component,
        position: {
          ...component.position,
          w: Math.min(component.position.w, 4),
          x: component.position.x % 4
        }
      }))
    },
    deviceType: DeviceType.Mobile,
    isEditing: false,
  }
};

// Empty grid story
export const EmptyGrid: Story = {
  args: {
    grid: {
      ...sampleGrid,
      components: []
    },
    deviceType: DeviceType.Desktop,
    isEditing: true,
  }
};

// Dense layout story
export const DenseLayout: Story = {
  args: {
    grid: {
      ...sampleGrid,
      gap: 8,
      components: [
        { id: 'item1', componentType: 'placeholder', position: { x: 0, y: 0, w: 2, h: 1 } },
        { id: 'item2', componentType: 'placeholder', position: { x: 2, y: 0, w: 2, h: 1 } },
        { id: 'item3', componentType: 'placeholder', position: { x: 4, y: 0, w: 2, h: 1 } },
        { id: 'item4', componentType: 'placeholder', position: { x: 6, y: 0, w: 2, h: 1 } },
        { id: 'item5', componentType: 'placeholder', position: { x: 8, y: 0, w: 2, h: 1 } },
        { id: 'item6', componentType: 'placeholder', position: { x: 10, y: 0, w: 2, h: 1 } },
        { id: 'item7', componentType: 'placeholder', position: { x: 0, y: 1, w: 3, h: 2 } },
        { id: 'item8', componentType: 'placeholder', position: { x: 3, y: 1, w: 3, h: 2 } },
        { id: 'item9', componentType: 'placeholder', position: { x: 6, y: 1, w: 3, h: 2 } },
        { id: 'item10', componentType: 'placeholder', position: { x: 9, y: 1, w: 3, h: 2 } }
      ].map(comp => ({ ...comp, props: {} }))
    },
    deviceType: DeviceType.Desktop,
    isEditing: true,
  }
};

// Interactive drag and drop test story
export const DragAndDropTest: Story = {
  render: (args) => {
    const [grid, setGrid] = useState<BentoGridType>({
      ...args.grid!,
      components: [
        {
          id: 'drag1',
          componentType: 'metric',
          position: { x: 0, y: 0, w: 3, h: 2 },
          props: {
            title: 'Drag Me!',
            value: '100',
            change: '+5%'
          }
        },
        {
          id: 'drag2',
          componentType: 'metric',
          position: { x: 4, y: 0, w: 3, h: 2 },
          props: {
            title: 'Or Me!',
            value: '200',
            change: '+10%'
          }
        },
        {
          id: 'drag3',
          componentType: 'chart',
          position: { x: 8, y: 0, w: 4, h: 3 },
          props: {
            title: 'Chart Component'
          }
        }
      ]
    });

    const handleComponentMove = (componentId: string, newPosition: GridPosition) => {
      console.log(`Moving component ${componentId} to:`, newPosition);
      setGrid(prevGrid => ({
        ...prevGrid,
        components: prevGrid.components.map(comp =>
          comp.id === componentId
            ? { ...comp, position: newPosition }
            : comp
        )
      }));
    };

    return (
      <div style={{ padding: '20px', height: '600px' }}>
        <div style={{ marginBottom: '20px', fontSize: '14px', color: '#666' }}>
          <strong>Instructions:</strong> Drag the components around to test the positioning. 
          Check the browser console for move events.
        </div>
        <BentoGrid
          {...args}
          grid={grid}
          onComponentMove={handleComponentMove}
        />
      </div>
    );
  },
  args: {
    grid: sampleGrid,
    deviceType: DeviceType.Desktop,
    isEditing: true,
  }
};