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
  
  /** Whether to add playful rotation */
  playful?: boolean;
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
  // Calculate enhanced preview dimensions with better proportions
  const previewStyle = useMemo(() => {
    const baseWidth = 240; // Increased base width for better visibility
    const baseHeight = 180; // Increased base height for better visibility
    
    // Calculate dimensions based on component size with better scaling
    const width = Math.max(baseWidth, component.position.w * 120);
    const height = Math.max(baseHeight, component.position.h * 100);
    
    return {
      width: `${width * scale}px`,
      height: `${height * scale}px`,
      opacity,
      transform: `scale(${scale})`,
      transformOrigin: 'center center', // Better transform origin
      pointerEvents: 'none' as const,
      willChange: 'transform', // Optimize for animations
    };
  }, [component.position.w, component.position.h, scale, opacity]);

  // Render simplified preview for better performance during drag
  if (simplified) {
    return (
      <div
        className={cn(
          'drag-preview',
          'bg-gradient-to-br from-primary/20 to-brand/20 border-2 border-primary/50',
          'rounded-lg shadow-2xl',
          'p-sm backdrop-blur-sm',
          'animate-drag-preview',
          className
        )}
        style={{
          ...previewStyle,
          opacity: 0.95,
        }}
      >
        {/* Playful preview with bouncing emoji */}
        <div className="h-full w-full flex flex-col items-center justify-center gap-xs">
          <div className="text-4xl animate-bounce">
            {(() => {
              const iconMap: Record<string, string> = {
                'metric-card': '📊',
                'chart': '📈',
                'kpi': '🎯',
                'gauge': '🌡️',
                'table': '📋',
                'list': '📝',
                'grid-view': '🗋️',
                'text-block': '📄',
                'heading': '🔤',
                'card': '🎴',
                'image': '🖼️',
                'video': '🎦',
                'gallery': '🖼️',
                'button': '🔘',
                'button-group': '🎛️',
                'form': '📝'
              };
              return iconMap[component.componentType] || '📦';
            })()
          }</div>
          <div className="typography-ui-small text-center">
            {component.componentType.replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
          </div>
          <div className="typography-caption">
            {component.position.w} × {component.position.h}
          </div>
        </div>
        
        {/* Dragging indicator */}
        <div className="absolute -top-2 -right-2 w-xs h-xs bg-primary rounded-full flex items-center justify-center">
          <div className="w-xs h-xs bg-primary-foreground rounded-full animate-bounce"></div>
        </div>
      </div>
    );
  }

  // Render full component preview with enhanced styling
  return (
    <div
      className={cn(
        'drag-preview',
        'bg-card/95 border-2 border-primary/70',
        'rounded-lg shadow-2xl backdrop-blur-sm',
        'overflow-hidden',
        'ring-4 ring-primary/20',
        'animate-drag-float',
        className
      )}
      style={previewStyle}
    >
      {/* Enhanced preview badge */}
      <div className="absolute top-2 left-2 z-10 bg-primary/90 text-primary-foreground typography-button-small px-2 py-1 rounded-md backdrop-blur-sm">
        Preview
      </div>
      
      {/* Drag indicator */}
      <div className="absolute top-2 right-2 z-10 w-xs h-xs bg-primary rounded-full animate-pulse">
        <div className="absolute inset-0 bg-primary rounded-full animate-ping opacity-75"></div>
      </div>
      
      {/* Component content with overlay */}
      <div className="h-full w-full relative">
        <ComponentRenderer
          component={component}
          gridSize={{
            w: component.position.w,
            h: component.position.h,
          }}
          deviceType={deviceType}
        />
        
        {/* Enhanced interaction overlay */}
        <div className="absolute inset-0 bg-primary/5 backdrop-blur-[0.5px] pointer-events-none" />
      </div>
      
      {/* Size indicator */}
      <div className="absolute bottom-2 right-2 z-10 bg-background/90 text-foreground typography-code-small px-2 py-1 rounded-md backdrop-blur-sm">
        {component.position.w} × {component.position.h}
      </div>
    </div>
  );
};

DragPreview.displayName = 'DragPreview';