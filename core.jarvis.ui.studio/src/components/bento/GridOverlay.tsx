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
  className,
}) => {
  // Simple grid pattern - just dots at intersections
  return (
    <div
      className={cn('grid-overlay', className)}
      style={{
        position: 'absolute',
        inset: 0,
        pointerEvents: 'none',
        zIndex: 0,
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
              cx="1"
              cy="1"
              r="1"
              fill="rgba(148, 163, 184, 0.3)"
            />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#grid-pattern)" />
      </svg>
    </div>
  );
};

GridOverlay.displayName = 'GridOverlay';