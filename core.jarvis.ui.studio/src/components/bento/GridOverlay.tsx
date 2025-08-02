/**
 * GridOverlay - Simple grid visualization
 * 
 * This component renders a subtle grid pattern to help users
 * align components. Keep it simple and functional.
 */

import React from 'react';
import { cn } from '@/lib/utils';

// ============================================================================
// Types
// ============================================================================

export interface GridOverlayProps {
  /** Number of columns in the grid */
  columns: number;
  
  
  /** Gap between grid cells in pixels */
  gap?: number;
  
  /** Height of each row in pixels */
  rowHeight?: number;
  
  /** Current interaction state for progressive visibility */
  interactionState?: 'idle' | 'hovering' | 'interacting';
  
  /** Force grid to be visible regardless of interaction state */
  forceVisible?: boolean;
  
  /** Additional CSS classes */
  className?: string;
}

// ============================================================================
// Main Component
// ============================================================================

export const GridOverlay: React.FC<GridOverlayProps> = ({
  columns,
  gap = 16,
  rowHeight = 100,
  interactionState = 'idle',
  forceVisible = false,
  className,
}) => {
  // Calculate progressive opacity based on interaction state
  const getGridOpacity = () => {
    if (forceVisible) return 0.6;
    
    switch (interactionState) {
      case 'idle': return 0;
      case 'hovering': return 0.15;
      case 'interacting': return 0.4;
      default: return 0;
    }
  };
  
  const gridOpacity = getGridOpacity();
  // Simple grid pattern - just dots at intersections
  return (
    <div
      className={cn(
        'grid-overlay',
        'transition-opacity duration-300 ease-out',
        {
          'grid-overlay--visible': gridOpacity > 0,
          'grid-overlay--hovering': interactionState === 'hovering',
          'grid-overlay--interacting': interactionState === 'interacting',
        },
        className
      )}
      style={{
        position: 'absolute',
        inset: 0,
        pointerEvents: 'none',
        zIndex: 0,
        opacity: gridOpacity,
      }}
    >
      {/* Create a simple dot pattern */}
      <svg width="100%" height="100%" style={{ position: 'absolute', top: 0, left: 0 }}>
        <defs>
          <pattern
            id="grid-pattern"
            x="0"
            y="0"
            width={`${100 / columns}%`}
            height={`${rowHeight + gap}px`}
            patternUnits="userSpaceOnUse"
          >
            <circle
              cx="2"
              cy="2"
              r="1.5"
              fill="currentColor"
              className="text-primary/60"
            />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#grid-pattern)" />
      </svg>
    </div>
  );
};

GridOverlay.displayName = 'GridOverlay';