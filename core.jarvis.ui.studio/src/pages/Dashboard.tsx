import React from 'react';
import { useNavigate } from 'react-router-dom';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { DashboardHeader } from '../components/dashboard/DashboardHeader';
import { MetricCard } from '../components/dashboard/MetricCard';
import { IssuesSection } from '../components/dashboard/IssuesSection';
import { SlowQueriesTable } from '../components/dashboard/SlowQueriesTable';

// Generate mock chart data
const generateChartData = (baseValue: number, variance: number = 0.3) => {
  return Array.from({ length: 24 }, () => 
    Math.floor(baseValue * (1 + (Math.random() - 0.5) * variance))
  );
};

export function Dashboard() {
  const navigate = useNavigate();
  const [activeItem, setActiveItem] = React.useState('home');

  const handleItemClick = (itemId: string) => {
    setActiveItem(itemId);
    if (itemId === 'table-editor') {
      navigate('/editor');
    } else if (itemId === 'schema-visualizer') {
      navigate('/SchemaVisualizer');
    }
  };

  return (
    <DashboardLayout activeItem={activeItem} onItemClick={handleItemClick}>
      <div className="@container">
        {/* Header */}
        <DashboardHeader
          projectName="Nano Project"
          projectStatus="active"
          tables={5}
          functions={0}
          replicas={0}
        />

        {/* Metrics Grid */}
        <div className="px-8 py-8">
          <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <MetricCard
              title="Database"
              type="database"
              requests={12}
              data={generateChartData(12, 0.5)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
            />
            <MetricCard
              title="Auth"
              type="auth"
              requests={10}
              data={generateChartData(10, 0.4)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
            />
            <MetricCard
              title="Storage"
              type="storage"
              requests={0}
              data={generateChartData(0)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
            />
            <MetricCard
              title="Realtime"
              type="realtime"
              requests={0}
              data={generateChartData(0)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
            />
          </div>
        </div>

        {/* Issues Section */}
        <IssuesSection />

        {/* Slow Queries */}
        <SlowQueriesTable />
      </div>
    </DashboardLayout>
  );
}