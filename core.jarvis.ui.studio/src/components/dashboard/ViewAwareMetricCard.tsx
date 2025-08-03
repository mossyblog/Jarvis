/**
 * ViewAwareMetricCard Component - MetricCard that adapts to different view modes
 * 
 * Wraps the MetricCard component to provide different layouts based on view mode:
 * - List: Horizontal layout with more details
 * - Grid: Compact square layout (default MetricCard)
 * - Card: Enhanced layout with additional information
 */

import React from 'react';
import { MetricCard } from './MetricCard';
import { cn } from '@/lib/utils';
import type { ViewMode } from '@/hooks/useViewState';

// ============================================================================
// Types
// ============================================================================

interface ViewAwareMetricCardProps {
  title: string;
  type: 'database' | 'auth' | 'storage' | 'realtime';
  requests: number;
  data: number[];
  timeLabels: string[];
  viewMode: ViewMode;
  className?: string;
}

// ============================================================================
// Component
// ============================================================================

export const ViewAwareMetricCard: React.FC<ViewAwareMetricCardProps> = ({
  title,
  type,
  requests,
  data,
  timeLabels,
  viewMode,
  className
}) => {
  // Different styling for each view mode
  const getViewModeStyles = () => {
    switch (viewMode) {
      case 'list':
        return {
          wrapper: 'flex items-center gap-4 p-4 border border-border rounded-lg bg-card',
          content: 'flex-1'
        };
      case 'card':
        return {
          wrapper: 'p-6 border border-border rounded-xl bg-card shadow-sm',
          content: 'space-y-4'
        };
      case 'grid':
      default:
        return {
          wrapper: '',
          content: ''
        };
    }
  };

  const styles = getViewModeStyles();

  // For list and card views, we create a custom layout
  if (viewMode === 'list') {
    return (
      <div className={cn(styles.wrapper, className)}>
        <div className="flex-shrink-0">
          <div className="w-12 h-12 rounded-lg bg-muted flex items-center justify-center">
            <span className="text-sm font-medium text-muted-foreground">
              {title.charAt(0)}
            </span>
          </div>
        </div>
        <div className={styles.content}>
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium text-sm">{title}</h3>
              <p className="text-2xl font-semibold">{requests}</p>
            </div>
            <div className="text-right">
              <p className="text-xs text-muted-foreground">requests/min</p>
              <p className="text-sm">Last: {timeLabels[1]}</p>
            </div>
          </div>
          <div className="text-xs text-muted-foreground">
            Status: {requests > 0 ? 'Active' : 'Inactive'}
          </div>
        </div>
      </div>
    );
  }

  if (viewMode === 'card') {
    return (
      <div className={cn(styles.wrapper, className)}>
        <div className={styles.content}>
          <div className="flex items-center justify-between">
            <h3 className="font-medium">{title}</h3>
            <div className="w-8 h-8 rounded-md bg-muted flex items-center justify-center">
              <span className="text-xs font-medium text-muted-foreground">
                {title.charAt(0)}
              </span>
            </div>
          </div>
          
          <div className="space-y-2">
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-semibold">{requests}</span>
              <span className="text-sm text-muted-foreground">req/min</span>
            </div>
            
            <div className="h-16 bg-muted rounded-md flex items-center justify-center">
              <span className="text-xs text-muted-foreground">Chart placeholder</span>
            </div>
          </div>
          
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">
              Status: {requests > 0 ? 'Active' : 'Inactive'}
            </span>
            <span className="text-muted-foreground">
              {timeLabels[1]?.split(', ')[1] || 'Now'}
            </span>
          </div>
        </div>
      </div>
    );
  }

  // Default grid view - use original MetricCard
  return (
    <MetricCard
      title={title}
      type={type}
      requests={requests}
      data={data}
      timeLabels={timeLabels}
      className={className}
    />
  );
};

export default ViewAwareMetricCard;