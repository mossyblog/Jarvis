import type { Meta, StoryObj } from '@storybook/react';
import { MetricCard } from './MetricCard';

const meta: Meta<typeof MetricCard> = {
  title: 'Dashboard/MetricCard',
  component: MetricCard,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: { type: 'select' },
      options: ['database', 'auth', 'storage', 'realtime'],
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

const mockData = [12, 19, 25, 31, 28, 35, 42, 38, 45, 52, 48, 55];
const timeLabels = ['12:00', '01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00', '08:00', '09:00', '10:00', '11:00'];

export const Database: Story = {
  args: {
    title: 'Database',
    type: 'database',
    requests: 1240,
    data: mockData,
    timeLabels,
  },
};

export const Auth: Story = {
  args: {
    title: 'Authentication',
    type: 'auth',
    requests: 856,
    data: [8, 12, 15, 18, 22, 19, 25, 28, 32, 29, 35, 38],
    timeLabels,
  },
};

export const Storage: Story = {
  args: {
    title: 'Storage',
    type: 'storage',
    requests: 432,
    data: [5, 8, 12, 15, 18, 21, 17, 22, 25, 28, 24, 30],
    timeLabels,
  },
};

export const Realtime: Story = {
  args: {
    title: 'Realtime',
    type: 'realtime',
    requests: 2180,
    data: [20, 35, 42, 38, 45, 52, 48, 55, 62, 58, 65, 72],
    timeLabels,
  },
};

export const LowActivity: Story = {
  args: {
    title: 'Database',
    type: 'database',
    requests: 23,
    data: [0, 1, 0, 2, 1, 0, 3, 1, 2, 0, 1, 2],
    timeLabels,
  },
};

export const HighActivity: Story = {
  args: {
    title: 'Authentication',
    type: 'auth',
    requests: 15420,
    data: [80, 95, 88, 102, 110, 125, 118, 135, 142, 138, 155, 168],
    timeLabels,
  },
};