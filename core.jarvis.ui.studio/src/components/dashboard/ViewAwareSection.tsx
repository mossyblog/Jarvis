/**
 * ViewAwareSection Component - Wrapper that adapts content layout based on view mode
 * 
 * Provides responsive layout for dashboard sections that need to adapt to different view modes.
 */

import React from 'react';
import { cn } from '@/lib/utils';
import type { ViewMode } from '@/hooks/useViewState';

// ============================================================================
// Types
// ============================================================================

interface ViewAwareSectionProps {
  viewMode: ViewMode;
  title?: string;
  description?: string;
  children: React.ReactNode;
  className?: string;
  id?: string;
  'aria-labelledby'?: string;
}

// ============================================================================
// Component
// ============================================================================

export const ViewAwareSection: React.FC<ViewAwareSectionProps> = ({
  viewMode,
  title,
  description,
  children,
  className,
  id,
  'aria-labelledby': ariaLabelledBy,
  ...props
}) => {
  // Get layout styles based on view mode
  const getLayoutStyles = () => {
    switch (viewMode) {
      case 'list':
        return 'space-y-3';
      case 'card':
        return 'space-y-6';
      case 'grid':
      default:
        return 'space-y-4';
    }
  };

  const getSectionPadding = () => {
    switch (viewMode) {
      case 'list':
        return 'py-4';
      case 'card':
        return 'py-6';
      case 'grid':
      default:
        return 'py-4';
    }
  };

  return (
    <section
      className={cn(
        'space-y-4',
        getSectionPadding(),
        className
      )}
      role="region"
      aria-labelledby={ariaLabelledBy}
      id={id}
      {...props}
    >
      {title && (
        <div className="space-y-2">
          <h2 
            className={cn(
              'font-semibold',
              viewMode === 'card' ? 'text-lg' : 'text-base'
            )}
            id={ariaLabelledBy}
          >
            {title}
          </h2>
          {description && (
            <p className="text-sm text-muted-foreground">
              {description}
            </p>
          )}
        </div>
      )}
      
      <div className={getLayoutStyles()}>
        {children}
      </div>
    </section>
  );
};

export default ViewAwareSection;