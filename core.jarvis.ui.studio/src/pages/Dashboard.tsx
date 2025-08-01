import React from 'react';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { ContentHeader } from '../components/layout/ContentHeader';
import { EditableContent } from '../components/layout/EditableContent';
import { MetricCard } from '../components/dashboard/MetricCard';
import { IssuesSection } from '../components/dashboard/IssuesSection';
import { SlowQueriesTable } from '../components/dashboard/SlowQueriesTable';
import { useNavigation } from '../hooks/useNavigation';
import { Circle } from 'lucide-react';
import { cn } from '../lib/utils';

// Generate mock chart data
const generateChartData = (baseValue: number, variance: number = 0.3) => {
  return Array.from({ length: 24 }, () => 
    Math.floor(baseValue * (1 + (Math.random() - 0.5) * variance))
  );
};

export function Dashboard() {
  const { navigateToItem, navigation } = useNavigation();
  const [activeItem, setActiveItem] = React.useState('home');

  const handleItemClick = (itemId: string) => {
    setActiveItem(itemId);
    const item = navigation.find(nav => nav.id === itemId);
    if (item) {
      navigateToItem(item);
    }
  };

  const projectStatus = 'active' as const;
  const statusColors = {
    active: 'text-brand',
    paused: 'text-yellow-500',
    inactive: 'text-gray-500'
  };

  return (
    <DashboardLayout activeItem={activeItem} onItemClick={handleItemClick}>
      <EditableContent pageId="dashboard" className="@container">
        {/* Content Header */}
        <ContentHeader
          title="Dashboard"
          description="Monitor your project's performance, database activity, and system health."
          actions={
            <div className="flex items-center gap-2">
              <Circle 
                size={8} 
                className={cn('fill-current', statusColors[projectStatus])}
              />
              <span className="text-sm font-medium">Project Status</span>
            </div>
          }
        />

        {/* Content Body */}
        <div className="px-lg py-lg space-y-lg">
          {/* Project Stats */}
          <div className="space-y-6">
            <div className="flex flex-wrap gap-8 text-sm">
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">Tables</span>
                <span className="text-2xl font-light">5</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">Functions</span>
                <span className="text-2xl font-light">0</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">Replicas</span>
                <span className="text-2xl font-light">0</span>
              </div>
            </div>

            <div className="flex items-center gap-2 text-sm">
              <span className="text-muted-foreground">Last 60 minutes</span>
              <button className="text-muted-foreground hover:text-foreground">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M4 6L8 10L12 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                </svg>
              </button>
              <span className="text-muted-foreground ml-4">
                Statistics for last 60 minutes
              </span>
            </div>
          </div>

          {/* Metrics Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
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

          {/* Issues Section */}
          <IssuesSection />

          {/* Slow Queries */}
          <SlowQueriesTable />
        </div>
      </EditableContent>
    </DashboardLayout>
  );
}