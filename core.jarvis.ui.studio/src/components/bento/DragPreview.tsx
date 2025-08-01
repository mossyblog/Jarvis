/**
 * DragPreview - Preview component shown during drag operations
 * 
 * This component renders a preview of the component being dragged,
 * providing visual feedback during drag-and-drop operations.
 */

import React, { useMemo } from 'react';
import { cn } from '@/lib/utils';
import type { GridComponent } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { ComponentRenderer } from './ComponentRenderer';

// ============================================================================
// Types
// ============================================================================

export interface DragPreviewProps {
  /** The component being dragged */
  component: GridComponent;
  
  /** Current device type */
  deviceType?: DeviceType;
  
  /** Scale factor for the preview (0-1) */
  scale?: number;
  
  /** Opacity for the preview (0-1) */
  opacity?: number;
  
  /** Additional CSS classes */
  className?: string;
  
  /** Whether to show a simplified preview */
  simplified?: boolean;
}

// ============================================================================
// Main Component
// ============================================================================

export const DragPreview: React.FC<DragPreviewProps> = ({
  component,
  deviceType = DeviceType.Desktop,
  scale = 0.8,
  opacity = 0.8,
  className,
  simplified = false,
}) => {
  // Calculate preview dimensions
  const previewStyle = useMemo(() => {
    const baseWidth = 200; // Base width in pixels for preview
    const baseHeight = 150; // Base height in pixels for preview
    
    // Calculate dimensions based on component size
    const width = Math.max(baseWidth, component.position.w * 100);
    const height = Math.max(baseHeight, component.position.h * 80);
    
    return {
      width: `${width * scale}px`,
      height: `${height * scale}px`,
      opacity,
      transform: `scale(${scale})`,
      transformOrigin: 'top left',
      pointerEvents: 'none' as const,
    };
  }, [component.position.w, component.position.h, scale, opacity]);

  // Render simplified preview for better performance during drag
  if (simplified) {
    return (
      <div
        className={cn(
          'drag-preview',
          'bg-card/90 border border-border',
          'rounded-md shadow-lg',
          'p-4',
          className
        )}
        style={{
          ...previewStyle,
          opacity: 0.9,
        }}
      >
        {/* Just show component type name as placeholder */}
        <div className="h-full w-full flex items-center justify-center">
          <div className="text-muted-foreground text-sm font-medium">
            {component.componentType.replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
          </div>
        </div>
      </div>
    );
  }

  // Render full component preview
  return (
    <div
      className={cn(
        'drag-preview',
        'bg-card border border-primary',
        'rounded-md shadow-lg',
        'overflow-hidden',
        className
      )}
      style={previewStyle}
    >
      {/* Preview badge */}
      <div className="absolute top-1 left-1 z-10 bg-primary text-primary-foreground text-xs px-1 py-0.5 rounded">
        Preview
      </div>
      
      {/* Component content */}
      <div className="h-full w-full relative">
        <ComponentRenderer
          component={component}
          gridSize={{
            w: component.position.w,
            h: component.position.h,
          }}
          deviceType={deviceType}
        />
        
        {/* Overlay to prevent interactions */}
        <div className="absolute inset-0 bg-transparent pointer-events-none" />
      </div>
    </div>
  );
};

DragPreview.displayName = 'DragPreview';