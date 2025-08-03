import React from 'react';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { ContentHeader } from '../components/layout/ContentHeader';
import { EditableContent } from '../components/layout/EditableContent';
import { ViewAwareMetricCard } from '../components/dashboard/ViewAwareMetricCard';
import { ViewAwareSection } from '../components/dashboard/ViewAwareSection';
import { IssuesSection } from '../components/dashboard/IssuesSection';
import { SlowQueriesTable } from '../components/dashboard/SlowQueriesTable';
import { RecentPagesList } from '../components/pages/RecentPagesList';
import { useNavigation } from '../hooks/useNavigation';
import { useViewState } from '../hooks/useViewState';
import { ViewSwitcher } from '../components/ui/view-switcher';
import { Circle, ChevronDown } from 'lucide-react';
import { cn } from '../lib/utils';
import { addToRecentPages } from '../utils/recentPagesManager';
import type { RecentPageMetadata } from '../utils/recentPagesManager';
import { populateMockRecentPages } from '../utils/mockRecentPages';
import { ErrorBoundary } from '../components/ui/error-boundary';

// Generate mock chart data
const generateChartData = (baseValue: number, variance: number = 0.3) => {
  return Array.from({ length: 24 }, () => 
    Math.floor(baseValue * (1 + (Math.random() - 0.5) * variance))
  );
};

export function Dashboard() {
  const { navigateToItem, navigation } = useNavigation();
  const [activeItem, setActiveItem] = React.useState('home');
  
  // Initialize mock data for development
  React.useEffect(() => {
    // Only populate mock data in development mode
    if (import.meta.env.DEV) {
      populateMockRecentPages();
    }
  }, []);
  
  // View state management
  const { viewMode, setViewMode, isChanging } = useViewState({
    pageId: 'dashboard',
    defaultView: 'grid'
  });

  const handleItemClick = (itemId: string) => {
    setActiveItem(itemId);
    const item = navigation.find(nav => nav.id === itemId);
    if (item) {
      navigateToItem(item);
    }
  };

  // Handle recent page actions
  const handleEditPage = (page: RecentPageMetadata) => {
    console.log('Edit page:', page.displayName);
    // TODO: Navigate to page builder
    // navigateToItem({ id: 'edit-page', path: `/studio/edit/${page.id}` });
  };

  const handleDuplicatePage = (page: RecentPageMetadata) => {
    console.log('Duplicate page:', page.displayName);
    // TODO: Implement page duplication logic
  };

  const handleDeletePage = (page: RecentPageMetadata) => {
    console.log('Delete page from recent:', page.displayName);
    // This is handled by the RecentPagesList component
  };

  const handlePreviewPage = (page: RecentPageMetadata) => {
    console.log('Preview page:', page.displayName);
    // TODO: Open page preview
    // window.open(page.route, '_blank');
  };

  const projectStatus = 'active' as const;
  const statusColors = {
    active: 'text-brand',
    paused: 'text-warning',
    inactive: 'text-muted-foreground'
  };

  return (
    <DashboardLayout activeItem={activeItem} onItemClick={handleItemClick}>
      <EditableContent pageId="dashboard" className="@container">
        {/* Content Header */}
        <ContentHeader
          title="Dashboard"
          description="Monitor your project's performance, database activity, and system health."
          actions={
            <div className="flex items-center gap-4">
              <ViewSwitcher
                viewMode={viewMode}
                onViewModeChange={setViewMode}
                isChanging={isChanging}
                size="md"
                variant="outline"
                availableModes={['list', 'grid', 'card']}
              />
              <div className="flex items-center gap-2">
                <Circle 
                  size={8} 
                  className={cn('fill-current', statusColors[projectStatus])}
                />
                <span className="text-sm font-medium">Project Status</span>
              </div>
            </div>
          }
        />

        {/* Content Body */}
        <div className="px-lg py-lg space-y-lg" role="main" aria-label="Dashboard content">
          {/* Project Stats */}
          <section className="space-y-6" role="region" aria-labelledby="project-stats-heading">
            <h2 id="project-stats-heading" className="sr-only">Project Statistics</h2>
            <div className="flex flex-wrap gap-8 text-sm">
              <div className="flex flex-col gap-1">
                <span className="typography-caption">Tables</span>
                <span className="text-2xl font-light font-custom">5</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="typography-caption">Functions</span>
                <span className="text-2xl font-light font-custom">0</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="typography-caption">Replicas</span>
                <span className="text-2xl font-light font-custom">0</span>
              </div>
            </div>

            <div className="flex items-center gap-2 text-sm">
              <span className="typography-caption">Last 60 minutes</span>
              <button className="text-muted-foreground hover:text-foreground">
                <ChevronDown size={16} strokeWidth={1.5} />
              </button>
              <span className="typography-caption ml-4">
                Statistics for last 60 minutes
              </span>
            </div>
          </section>

          {/* Metrics Grid */}
          <section 
            className={cn(
              "gap-4", 
              viewMode === 'list' && "space-y-4",
              viewMode === 'grid' && "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4",
              viewMode === 'card' && "grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6"
            )} 
            role="region" 
            aria-labelledby="metrics-heading"
          >
            <h2 id="metrics-heading" className="sr-only">Service Metrics</h2>
            <ViewAwareMetricCard
              title="Database"
              type="database"
              requests={12}
              data={generateChartData(12, 0.5)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
              viewMode={viewMode}
            />
            <ViewAwareMetricCard
              title="Auth"
              type="auth"
              requests={10}
              data={generateChartData(10, 0.4)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
              viewMode={viewMode}
            />
            <ViewAwareMetricCard
              title="Storage"
              type="storage"
              requests={0}
              data={generateChartData(0)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
              viewMode={viewMode}
            />
            <ViewAwareMetricCard
              title="Realtime"
              type="realtime"
              requests={0}
              data={generateChartData(0)}
              timeLabels={['Jun 1, 3:07pm', 'Jun 1, 3:31pm']}
              viewMode={viewMode}
            />
          </section>

          {/* Issues Section */}
          <ViewAwareSection
            viewMode={viewMode}
            title="System Issues"
            description="Monitor system health and resolve issues"
            aria-labelledby="issues-heading"
          >
            <IssuesSection />
          </ViewAwareSection>

          {/* Recent Pages */}
          <ViewAwareSection
            viewMode={viewMode}
            title="Recent Pages"
            description="Quick access to your recently edited pages"
            aria-labelledby="recent-pages-heading"
          >
            <RecentPagesList
              limit={8}
              compact={viewMode === 'list'}
              showControls={false}
              defaultViewMode={viewMode === 'list' ? 'list' : 'grid'}
              onEdit={handleEditPage}
              onDuplicate={handleDuplicatePage}
              onDelete={handleDeletePage}
              onPreview={handlePreviewPage}
              autoRefresh={true}
            />
          </ViewAwareSection>

          {/* Slow Queries */}
          <ViewAwareSection
            viewMode={viewMode}
            title="Database Performance"
            description="Track slow queries and optimize performance"
            aria-labelledby="slow-queries-heading"
          >
            <SlowQueriesTable />
          </ViewAwareSection>
        </div>
      </EditableContent>
    </DashboardLayout>
  );
}