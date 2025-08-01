/**
 * GridOverlay - Visual grid lines for edit mode
 * 
 * This component renders visual grid lines and guides when the grid
 * is in edit mode, helping users align and position components.
 */

import React, { useMemo } from 'react';
import { cn } from '@/lib/utils';

// ============================================================================
// Types
// ============================================================================

export interface GridOverlayProps {
  /** Number of columns in the grid */
  columns: number;
  
  /** Number of rows (optional, will calculate based on content if not provided) */
  rows?: number;
  
  /** Gap between grid cells in pixels */
  gap?: number;
  
  /** Height of each row in pixels */
  rowHeight?: number;
  
  /** Color of the grid lines */
  gridColor?: string;
  
  /** Opacity of the grid lines (0-1) */
  opacity?: number;
  
  /** Additional CSS classes */
  className?: string;
  
  /** Whether to show column numbers */
  showColumnNumbers?: boolean;
  
  /** Whether to show row numbers */
  showRowNumbers?: boolean;
}

// ============================================================================
// Main Component
// ============================================================================

export const GridOverlay: React.FC<GridOverlayProps> = ({
  columns,
  rows,
  gap = 16,
  rowHeight = 100,
  gridColor = 'rgba(59, 130, 246, 0.4)', // Blue color for better contrast
  opacity = 0.8, // More visible
  className,
  showColumnNumbers = false,
  showRowNumbers = false,
}) => {
  // Calculate the number of rows to display
  const displayRows = rows || 10; // Default to 10 rows if not specified

  // Create a grid overlay that matches CSS Grid's behavior exactly
  const gridStyle = useMemo(() => ({
    display: 'grid',
    gridTemplateColumns: `repeat(${columns}, 1fr)`,
    gridTemplateRows: `repeat(${displayRows}, ${rowHeight}px)`,
    gap: `${gap}px`,
    position: 'absolute',
    inset: 0,
    pointerEvents: 'none' as const,
    zIndex: -1,
  }), [columns, displayRows, rowHeight, gap]);

  // Generate grid cells
  const gridCells = useMemo(() => {
    const cells: React.JSX.Element[] = [];
    
    for (let row = 0; row < displayRows; row++) {
      for (let col = 0; col < columns; col++) {
        const key = `cell-${row}-${col}`;
        const showNumber = (showColumnNumbers && row === 0) || (showRowNumbers && col === 0);
        
        cells.push(
          <div
            key={key}
            className="relative"
            style={{
              border: `1px dashed ${gridColor}`,
              opacity,
              gridColumn: col + 1,
              gridRow: row + 1,
              backgroundColor: 'transparent', // No background fill
              borderRadius: '4px',
            }}
          >
            {showColumnNumbers && row === 0 && (
              <div className="absolute top-1 left-1/2 transform -translate-x-1/2 text-xs font-mono text-muted-foreground">
                {col + 1}
              </div>
            )}
            {showRowNumbers && col === 0 && (
              <div className="absolute left-1 top-1/2 transform -translate-y-1/2 text-xs font-mono text-muted-foreground">
                {row + 1}
              </div>
            )}
          </div>
        );
      }
    }
    
    return cells;
  }, [columns, displayRows, gridColor, opacity, showColumnNumbers, showRowNumbers]);

  return (
    <div
      className={cn('grid-overlay', className)}
      style={{
        ...gridStyle,
        backgroundColor: 'transparent', // Transparent background
      }}
    >
      {gridCells}
    </div>
  );
};

GridOverlay.displayName = 'GridOverlay';