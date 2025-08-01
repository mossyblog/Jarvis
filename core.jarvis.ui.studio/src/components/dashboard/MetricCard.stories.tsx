import type { Meta, StoryObj } from '@storybook/react-vite';
import { MetricCard, MetricCardBase } from './MetricCard';
import { useState } from 'react';

const meta = {
  title: 'Dashboard/MetricCard',
  component: MetricCard,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    type: {
      options: ['database', 'auth', 'storage', 'realtime'],
      control: { type: 'select' },
    },
    loading: {
      control: { type: 'boolean' },
    },
  },
} satisfies Meta<typeof MetricCard>;

export default meta;
type Story = StoryObj<typeof meta>;

// Generate sample data
const generateData = (points: number, min: number, max: number) => {
  return Array.from({ length: points }, () => 
    Math.floor(Math.random() * (max - min + 1)) + min
  );
};

const generateTimeLabels = (hours: number) => {
  const labels = [];
  const now = new Date();
  for (let i = hours - 1; i >= 0; i--) {
    const time = new Date(now.getTime() - i * 60 * 60 * 1000);
    labels.push(time.toLocaleTimeString('en-US', { hour: 'numeric', hour12: true }));
  }
  return labels;
};

export const Default: Story = {
  args: {
    title: 'Database',
    type: 'database',
    requests: 45231,
    data: generateData(24, 100, 500),
    timeLabels: generateTimeLabels(24),
  },
};

export const Loading: Story = {
  args: {
    title: 'Database',
    type: 'database',
    requests: 0,
    data: [],
    timeLabels: [],
    loading: true,
  },
};

export const AllTypes: Story = {
  render: () => {
    const data = generateData(24, 100, 500);
    const timeLabels = generateTimeLabels(24);
    
    return (
      <div className="grid grid-cols-2 gap-4">
        <MetricCard
          title="Database"
          type="database"
          requests={45231}
          data={data}
          timeLabels={timeLabels}
        />
        <MetricCard
          title="Authentication"
          type="auth"
          requests={12543}
          data={data}
          timeLabels={timeLabels}
        />
        <MetricCard
          title="Storage"
          type="storage"
          requests={8921}
          data={data}
          timeLabels={timeLabels}
        />
        <MetricCard
          title="Realtime"
          type="realtime"
          requests={3456}
          data={data}
          timeLabels={timeLabels}
        />
      </div>
    );
  },
};

export const Interactive: Story = {
  render: () => {
    const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
    const data = generateData(24, 100, 500);
    const timeLabels = generateTimeLabels(24);
    
    return (
      <div>
        <MetricCard
          title="Interactive Chart"
          type="database"
          requests={45231}
          data={data}
          timeLabels={timeLabels}
          onChartClick={(index) => setSelectedIndex(index)}
        />
        {selectedIndex !== null && (
          <p className="mt-4 text-sm text-muted-foreground">
            Clicked bar {selectedIndex + 1}: {data[selectedIndex]} requests at {timeLabels[selectedIndex]}
          </p>
        )}
      </div>
    );
  },
};

export const CustomFormatting: Story = {
  args: {
    title: 'Custom Format',
    type: 'storage',
    requests: 1234567,
    data: generateData(24, 1000, 5000),
    timeLabels: generateTimeLabels(24),
    formatNumber: (value) => {
      if (value >= 1000000) {
        return `${(value / 1000000).toFixed(1)}M`;
      } else if (value >= 1000) {
        return `${(value / 1000).toFixed(1)}K`;
      }
      return value.toString();
    },
  },
};

export const RealTimeUpdates: Story = {
  render: () => {
    const [data, setData] = useState(generateData(24, 100, 500));
    const [requests, setRequests] = useState(45231);
    
    // Simulate real-time updates
    const updateData = () => {
      setData(prev => {
        const newData = [...prev.slice(1), Math.floor(Math.random() * 400) + 100];
        return newData;
      });
      setRequests(prev => prev + Math.floor(Math.random() * 100));
    };
    
    return (
      <div>
        <MetricCard
          title="Real-time Updates"
          type="realtime"
          requests={requests}
          data={data}
          timeLabels={generateTimeLabels(24)}
        />
        <button
          onClick={updateData}
          className="mt-4 px-4 py-2 bg-brand text-white rounded hover:opacity-90"
        >
          Update Data
        </button>
      </div>
    );
  },
};

export const PerformanceComparison: Story = {
  render: () => {
    const [count, setCount] = useState(0);
    const data = generateData(24, 100, 500);
    const timeLabels = generateTimeLabels(24);
    
    return (
      <div>
        <div className="mb-4">
          <button
            onClick={() => setCount(prev => prev + 1)}
            className="px-4 py-2 bg-brand text-white rounded hover:opacity-90"
          >
            Increment Counter: {count}
          </button>
          <p className="mt-2 text-sm text-muted-foreground">
            Click to test re-render behavior. Memoized component won't re-render.
          </p>
        </div>
        
        <div className="grid grid-cols-2 gap-4">
          <div>
            <h3 className="mb-2 font-semibold">Memoized (Optimized)</h3>
            <MetricCard
              title="With React.memo"
              type="database"
              requests={45231}
              data={data}
              timeLabels={timeLabels}
            />
          </div>
          
          <div>
            <h3 className="mb-2 font-semibold">Non-memoized</h3>
            <MetricCardBase
              title="Without React.memo"
              type="database"
              requests={45231}
              data={data}
              timeLabels={timeLabels}
            />
          </div>
        </div>
      </div>
    );
  },
};

export const EmptyState: Story = {
  args: {
    title: 'No Data',
    type: 'database',
    requests: 0,
    data: Array(24).fill(0),
    timeLabels: generateTimeLabels(24),
  },
};

export const HighVolume: Story = {
  args: {
    title: 'High Volume',
    type: 'database',
    requests: 9876543,
    data: generateData(24, 10000, 50000),
    timeLabels: generateTimeLabels(24),
    formatNumber: (value) => {
      if (value >= 1000000) {
        return `${(value / 1000000).toFixed(1)}M`;
      } else if (value >= 1000) {
        return `${(value / 1000).toFixed(0)}K`;
      }
      return value.toString();
    },
  },
};