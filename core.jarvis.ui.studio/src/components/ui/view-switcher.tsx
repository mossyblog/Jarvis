/**
 * ViewSwitcher Component - Toggle between list, grid, and card views
 * 
 * Provides a set of toggle buttons for switching between different view modes.
 * Integrates with useViewState hook for state management.
 * 
 * @module ViewSwitcher
 */

import React from 'react';
import { List, Grid3X3, LayoutGrid } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from './button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from './tooltip';
import type { ViewMode } from '@/hooks/useViewState';

// ============================================================================
// Types
// ============================================================================

export interface ViewSwitcherProps {
  /** Current view mode */
  viewMode: ViewMode;
  /** Callback when view mode changes */
  onViewModeChange: (mode: ViewMode) => void;
  /** Whether view is currently changing (for loading states) */
  isChanging?: boolean;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Visual variant */
  variant?: 'default' | 'outline' | 'ghost';
  /** Additional CSS classes */
  className?: string;
  /** Whether to show tooltips */
  showTooltips?: boolean;
  /** Whether to show labels alongside icons */
  showLabels?: boolean;
  /** Disabled state */
  disabled?: boolean;
  /** Available view modes (defaults to all) */
  availableModes?: ViewMode[];
}

// ============================================================================
// Configuration
// ============================================================================

const VIEW_CONFIG = {
  list: {
    icon: List,
    label: 'List View',
    description: 'Display items in a vertical list format'
  },
  grid: {
    icon: Grid3X3,
    label: 'Grid View', 
    description: 'Display items in a grid layout'
  },
  card: {
    icon: LayoutGrid,
    label: 'Card View',
    description: 'Display items as cards with rich content'
  }
} as const;

const SIZE_STYLES = {
  sm: 'h-8 w-8 text-sm',
  md: 'h-9 w-9 text-base',
  lg: 'h-10 w-10 text-lg'
} as const;

// ============================================================================
// Component
// ============================================================================

export const ViewSwitcher: React.FC<ViewSwitcherProps> = ({
  viewMode,
  onViewModeChange,
  isChanging = false,
  size = 'md',
  variant = 'outline',
  className,
  showTooltips = true,
  showLabels = false,
  disabled = false,
  availableModes = ['list', 'grid', 'card']
}) => {
  // ============================================================================
  // Render Helpers
  // ============================================================================

  const renderViewButton = (mode: ViewMode) => {
    const config = VIEW_CONFIG[mode];
    const Icon = config.icon;
    const isActive = viewMode === mode;
    const isDisabled = disabled || isChanging;

    const button = (
      <Button
        variant={isActive ? 'default' : variant}
        size="sm"
        onClick={() => !isDisabled && onViewModeChange(mode)}
        disabled={isDisabled}
        className={cn(
          SIZE_STYLES[size],
          'flex-shrink-0 transition-all duration-200',
          {
            'opacity-50 cursor-not-allowed': isDisabled,
            'opacity-60': isChanging && !isActive,
          },
          showLabels && 'px-3 gap-2'
        )}
        aria-label={config.label}
        aria-pressed={isActive}
      >
        <Icon 
          className={cn(
            'flex-shrink-0',
            size === 'sm' && 'h-3.5 w-3.5',
            size === 'md' && 'h-4 w-4', 
            size === 'lg' && 'h-5 w-5'
          )} 
        />
        {showLabels && (
          <span className="font-medium">
            {config.label.replace(' View', '')}
          </span>
        )}
      </Button>
    );

    // Wrap with tooltip if enabled
    if (showTooltips && !showLabels) {
      return (
        <Tooltip key={mode}>
          <TooltipTrigger asChild>
            {button}
          </TooltipTrigger>
          <TooltipContent side="bottom">
            <div className="text-center">
              <div className="font-medium">{config.label}</div>
              <div className="text-xs text-muted-foreground mt-1">
                {config.description}
              </div>
            </div>
          </TooltipContent>
        </Tooltip>
      );
    }

    return <React.Fragment key={mode}>{button}</React.Fragment>;
  };

  // Remove renderTooltipContent as it's no longer needed

  // ============================================================================
  // Render
  // ============================================================================

  const content = (
    <div 
      className={cn(
        'inline-flex items-center gap-1 p-1 bg-background border border-border rounded-md',
        className
      )}
      role="radiogroup"
      aria-label="View mode selection"
    >
      {availableModes.map(renderViewButton)}
    </div>
  );

  // Wrap with tooltip provider if tooltips are enabled
  if (showTooltips && !showLabels) {
    return (
      <TooltipProvider delayDuration={300}>
        {content}
      </TooltipProvider>
    );
  }

  return content;
};

// ============================================================================
// Enhanced Variants
// ============================================================================

/**
 * Compact ViewSwitcher for tight spaces
 */
export const ViewSwitcherCompact: React.FC<Omit<ViewSwitcherProps, 'size' | 'showLabels'>> = (props) => (
  <ViewSwitcher {...props} size="sm" showLabels={false} />
);

/**
 * ViewSwitcher with labels for better accessibility
 */
export const ViewSwitcherWithLabels: React.FC<Omit<ViewSwitcherProps, 'showLabels' | 'showTooltips'>> = (props) => (
  <ViewSwitcher {...props} showLabels={true} showTooltips={false} />
);

/**
 * Large ViewSwitcher for primary placement
 */
export const ViewSwitcherLarge: React.FC<Omit<ViewSwitcherProps, 'size'>> = (props) => (
  <ViewSwitcher {...props} size="lg" />
);

export default ViewSwitcher;