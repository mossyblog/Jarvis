/**
 * ComponentTile - Visual component tile for ribbon-style toolbar
 * 
 * Displays a draggable component tile with icon, name, and hover effects
 * similar to Office ribbon interface.
 */

import React from 'react';
import { cn } from '@/lib/utils';

// Extend Window interface for drag data
declare global {
  interface Window {
    __bentoExternalDrag?: {
      type: string;
      componentType: string;
      defaultSize: { w: number; h: number };
    };
  }
}

// ============================================================================
// Types
// ============================================================================

export interface ComponentTileProps {
  /** Unique component ID */
  id: string;
  /** Display name */
  name: string;
  /** Component description */
  description: string;
  /** Icon element or component */
  icon: React.ReactNode;
  /** Default size when placed on grid */
  defaultSize: { w: number; h: number };
  /** Whether the tile is selected */
  selected?: boolean;
  /** Additional CSS classes */
  className?: string;
  /** Drag start handler */
  onDragStart?: (event: React.DragEvent) => void;
  /** Click handler */
  onClick?: () => void;
}

// ============================================================================
// Main Component
// ============================================================================

export const ComponentTile: React.FC<ComponentTileProps> = ({
  id,
  name,
  description,
  icon,
  defaultSize,
  selected = false,
  className,
  onDragStart,
  onClick
}) => {
  const handleDragStart = (event: React.DragEvent) => {
    const dragData = {
      type: 'external-component',
      componentType: id,
      defaultSize
    };
    
    event.dataTransfer.setData('application/json', JSON.stringify(dragData));
    event.dataTransfer.effectAllowed = 'copy';
    
    // Store drag data globally for access during dragOver
    window.__bentoExternalDrag = dragData;
    
    // Notify any listening grid components about external drag start
    window.dispatchEvent(new CustomEvent('bento-external-drag-start', { 
      detail: dragData 
    }));
    
    // Create a compact drag image
    const dragImage = document.createElement('div');
    dragImage.style.position = 'absolute';
    dragImage.style.top = '-1000px';
    dragImage.style.left = '-1000px';
    dragImage.style.padding = '8px';
    dragImage.style.backgroundColor = 'hsl(var(--card))';
    dragImage.style.border = '1px solid hsl(var(--border))';
    dragImage.style.borderRadius = '8px';
    dragImage.style.opacity = '0.9';
    dragImage.style.transform = 'rotate(-2deg)';
    dragImage.style.boxShadow = '0 4px 6px -1px rgba(0, 0, 0, 0.1)';
    dragImage.style.fontSize = '24px';
    dragImage.style.width = '48px';
    dragImage.style.height = '48px';
    dragImage.style.display = 'flex';
    dragImage.style.alignItems = 'center';
    dragImage.style.justifyContent = 'center';
    
    // Add just the icon to the drag image
    if (typeof icon === 'string') {
      dragImage.textContent = icon;
    } else {
      dragImage.innerHTML = '<div style="width: 24px; height: 24px; display: flex; align-items: center; justify-content: center;">📦</div>';
    }
    
    document.body.appendChild(dragImage);
    event.dataTransfer.setDragImage(dragImage, 24, 24);
    
    setTimeout(() => {
      document.body.removeChild(dragImage);
    }, 0);
    
    onDragStart?.(event);
  };

  return (
    <div
      draggable
      onDragStart={handleDragStart}
      onDragEnd={(event) => {
        // Notify any listening grid components about external drag end
        window.dispatchEvent(new CustomEvent('bento-external-drag-end', { 
          detail: { componentType: id, defaultSize } 
        }));
        
        // Clean up global drag data
        delete window.__bentoExternalDrag;
      }}
      onClick={onClick}
      className={cn(
        // Base styles
        "component-tile group relative flex flex-col items-center justify-start",
        "p-2 rounded-lg cursor-grab",
        "transition-all duration-200",
        
        // Background and border
        "bg-card border border-border",
        "hover:bg-accent/10 hover:border-accent",
        
        // Selected state
        selected && "bg-accent/20 border-accent",
        
        // Hover effects
        "hover:scale-105 hover:shadow-md",
        "active:scale-95 active:cursor-grabbing",
        
        className
      )}
      title={description}
    >
      {/* Icon */}
      <div className="component-tile__icon transition-transform group-hover:scale-110">
        {typeof icon === 'string' ? (
          <div className="w-lg h-lg flex items-center justify-center text-base">
            {icon}
          </div>
        ) : (
          <div className="w-lg h-lg flex items-center justify-center">
            {icon}
          </div>
        )}
      </div>
      
      {/* Name */}
      <span className="component-tile__name text-micro font-medium text-center line-clamp-1">
        {name}
      </span>
      
      {/* Size indicator on hover */}
      <div className={cn(
        "absolute -bottom-6 left-1/2 transform -translate-x-1/2",
        "text-tiny typography-caption bg-background px-1.5 py-0.5 rounded",
        "opacity-0 group-hover:opacity-100 transition-opacity",
        "pointer-events-none whitespace-nowrap"
      )}>
        {defaultSize.w}×{defaultSize.h}
      </div>
    </div>
  );
};

export default ComponentTile;