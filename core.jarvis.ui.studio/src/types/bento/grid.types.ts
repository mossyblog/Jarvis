/**
 * Bento Grid Types
 * 
 * Type definitions for Bento grids and grid components, including
 * positioning, sizing, display settings, and zones.
 * 
 * @module BentoGridTypes
 */

import type { ID, Timestamp, DeviceType, GridPosition } from './index';
import type { ComponentBindings } from './bindings.types';

// ============================================================================
// Core Grid Types
// ============================================================================

/**
 * Complete Bento grid configuration
 * 
 * Represents a grid layout for a specific device type, containing
 * components arranged in a grid system with configurable settings.
 */
export interface BentoGrid {
  // Identification
  /** Unique grid identifier */
  id: ID;
  /** Human-readable grid name */
  name: string;
  /** Target device type for this grid */
  device: DeviceType;
  
  // Grid configuration
  /** Number of columns in the grid */
  columns: number;
  /** Number of rows (optional, auto-expand if not set) */
  rows?: number;
  /** Gap between components in pixels */
  gap: number;
  /** Height of each row in pixels */
  rowHeight?: number;
  
  // Components in the grid
  /** Array of components positioned within this grid */
  components: GridComponent[];
  
  // Grid settings
  /** Additional grid configuration and behavior */
  settings: GridSettings;
  
  // Metadata
  /** ISO timestamp when grid was created */
  createdAt: Timestamp;
  /** ISO timestamp when grid was last modified */
  updatedAt: Timestamp;
}

/**
 * Grid-level settings that control behavior and appearance
 */
export interface GridSettings {
  // Visual settings
  /** Whether to show grid lines in edit mode */
  showGrid?: boolean;
  /** Whether components snap to grid positions */
  snapToGrid?: boolean;
  /** Color of grid lines (CSS color value) */
  gridColor?: string;
  
  // Behavior
  /** Whether components can overlap each other */
  allowOverlap?: boolean;
  /** How to handle automatic compacting of components */
  compactMode?: 'none' | 'vertical' | 'horizontal';
  
  // Constraints
  /** Minimum number of columns allowed */
  minColumns?: number;
  /** Maximum number of columns allowed */
  maxColumns?: number;
  
  // Zones (optional predefined areas)
  /** Predefined zones within the grid for organized layout */
  zones?: GridZone[];
}

/**
 * Predefined zone within a grid for organized component placement
 */
export interface GridZone {
  /** Unique zone identifier */
  id: ID;
  /** Human-readable zone name */
  name: string;
  /** Position and size of the zone */
  bounds: GridPosition;
  /** Type of zone for semantic meaning */
  type: 'header' | 'sidebar' | 'content' | 'footer' | 'custom';
  
  // Zone constraints
  /** Whether the zone can be modified */
  locked?: boolean;
  /** Component types allowed in this zone */
  accepts?: string[];
  /** Maximum number of components allowed in zone */
  maxComponents?: number;
  
  // Styling
  /** CSS class name for styling the zone */
  className?: string;
  /** Inline styles for the zone */
  style?: React.CSSProperties;
}

// ============================================================================
// Grid Component Types
// ============================================================================

/**
 * A component positioned within a grid
 * 
 * Represents an instance of a component with specific positioning,
 * configuration, and behavior within a grid layout.
 */
export interface GridComponent {
  // Identification
  /** Unique component instance identifier */
  id: ID;
  /** Component type key from the component registry */
  componentType: string;
  
  // Position and size
  /** Position and dimensions within the grid */
  position: GridPosition;
  
  // Component configuration
  /** Properties passed to the component */
  props?: Record<string, unknown>;
  
  // Bindings
  /** Data binding and interaction configuration */
  bindings?: ComponentBindings;
  
  // Display settings
  /** Display and styling configuration */
  display?: ComponentDisplay;
  
  // Metadata
  /** Whether the component is locked from modifications */
  locked?: boolean;
  /** Whether the component is conditionally hidden */
  hidden?: boolean;
}

/**
 * Display and styling configuration for grid components
 */
export interface ComponentDisplay {
  // Custom styling
  /** Additional CSS class names */
  className?: string;
  /** Inline styles for the component */
  style?: React.CSSProperties;
  
  // Responsive visibility
  /** Device types where component should be hidden */
  hideOn?: DeviceType[];
  /** Device types where component should be shown (exclusive) */
  showOnly?: DeviceType[];
  
  // Animation
  /** Animation configuration for component transitions */
  animation?: AnimationConfig;
}

/**
 * Animation configuration for component transitions
 */
export interface AnimationConfig {
  /** CSS class or animation name for enter transition */
  enter?: string;
  /** CSS class or animation name for exit transition */
  exit?: string;
  /** Animation duration in milliseconds */
  duration?: number;
  /** Animation delay in milliseconds */
  delay?: number;
}

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Create default grid settings
 */
export const createDefaultGridSettings = (): GridSettings => ({
  showGrid: false,
  snapToGrid: true,
  gridColor: '#e5e7eb',
  allowOverlap: false,
  compactMode: 'vertical',
  minColumns: 1,
  maxColumns: 24
});

/**
 * Create a new grid with default values
 */
export const createNewGrid = (
  name: string,
  device: DeviceType,
  columns: number = 12
): Omit<BentoGrid, 'id' | 'createdAt' | 'updatedAt'> => ({
  name,
  device,
  columns,
  gap: 16,
  rowHeight: 100,
  components: [],
  settings: createDefaultGridSettings()
});

/**
 * Create a new grid component
 */
export const createNewGridComponent = (
  componentType: string,
  position: GridPosition,
  props: Record<string, unknown> = {}
): Omit<GridComponent, 'id'> => ({
  componentType,
  position,
  props,
  locked: false,
  hidden: false
});

/**
 * Create default component display settings
 */
export const createDefaultComponentDisplay = (): ComponentDisplay => ({
  className: '',
  style: {}
});

/**
 * Create a grid zone
 */
export const createGridZone = (
  name: string,
  bounds: GridPosition,
  type: GridZone['type'] = 'custom'
): Omit<GridZone, 'id'> => ({
  name,
  bounds,
  type,
  locked: false,
  accepts: []
});

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard to check if an object is a valid BentoGrid
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isBentoGrid = (obj: any): obj is BentoGrid => {
  return obj &&
    typeof obj.id === 'string' &&
    typeof obj.name === 'string' &&
    typeof obj.device === 'string' &&
    typeof obj.columns === 'number' &&
    typeof obj.gap === 'number' &&
    Array.isArray(obj.components) &&
    obj.settings &&
    typeof obj.createdAt === 'string' &&
    typeof obj.updatedAt === 'string';
};

/**
 * Type guard to check if an object is a valid GridComponent
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isGridComponent = (obj: any): obj is GridComponent => {
  return obj &&
    typeof obj.id === 'string' &&
    typeof obj.componentType === 'string' &&
    obj.position &&
    typeof obj.position.x === 'number' &&
    typeof obj.position.y === 'number' &&
    typeof obj.position.w === 'number' &&
    typeof obj.position.h === 'number';
};

/**
 * Type guard to check if an object has valid grid settings
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidGridSettings = (obj: any): obj is GridSettings => {
  return obj &&
    (obj.showGrid === undefined || typeof obj.showGrid === 'boolean') &&
    (obj.snapToGrid === undefined || typeof obj.snapToGrid === 'boolean') &&
    (obj.gridColor === undefined || typeof obj.gridColor === 'string') &&
    (obj.allowOverlap === undefined || typeof obj.allowOverlap === 'boolean') &&
    (obj.compactMode === undefined || 
     ['none', 'vertical', 'horizontal'].includes(obj.compactMode));
};

/**
 * Type guard to check if an object is a valid GridZone
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidGridZone = (obj: any): obj is GridZone => {
  return obj &&
    typeof obj.id === 'string' &&
    typeof obj.name === 'string' &&
    obj.bounds &&
    typeof obj.bounds.x === 'number' &&
    typeof obj.bounds.y === 'number' &&
    typeof obj.bounds.w === 'number' &&
    typeof obj.bounds.h === 'number' &&
    ['header', 'sidebar', 'content', 'footer', 'custom'].includes(obj.type);
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Check if two grid components overlap
 */
export const componentsOverlap = (a: GridComponent, b: GridComponent): boolean => {
  const aRight = a.position.x + a.position.w;
  const aBottom = a.position.y + a.position.h;
  const bRight = b.position.x + b.position.w;
  const bBottom = b.position.y + b.position.h;
  
  return !(
    aRight <= b.position.x ||
    bRight <= a.position.x ||
    aBottom <= b.position.y ||
    bBottom <= a.position.y
  );
};

/**
 * Find all components that overlap with a given component
 */
export const findOverlappingComponents = (
  component: GridComponent,
  allComponents: GridComponent[]
): GridComponent[] => {
  return allComponents.filter(other => 
    other.id !== component.id && componentsOverlap(component, other)
  );
};

/**
 * Check if a component position is valid within grid bounds
 */
export const isValidPosition = (
  component: GridComponent,
  grid: BentoGrid
): boolean => {
  const { position } = component;
  
  // Check if component fits within grid columns
  if (position.x < 0 || position.x + position.w > grid.columns) {
    return false;
  }
  
  // Check if component has valid dimensions
  if (position.w <= 0 || position.h <= 0) {
    return false;
  }
  
  // Check if component is within row bounds (if rows are specified)
  if (grid.rows && (position.y < 0 || position.y + position.h > grid.rows)) {
    return false;
  }
  
  return true;
};

/**
 * Find the next available position for a component in the grid
 */
export const findNextAvailablePosition = (
  componentSize: { w: number; h: number },
  grid: BentoGrid,
  startRow: number = 0
): GridPosition | null => {
  const { columns, components } = grid;
  const { w, h } = componentSize;
  
  // Try to find a position starting from the specified row
  let currentRow = startRow;
  const maxAttempts = 100; // Prevent infinite loops
  let attempts = 0;
  
  while (attempts < maxAttempts) {
    // Try each column in the current row
    for (let x = 0; x <= columns - w; x++) {
      const position: GridPosition = { x, y: currentRow, w, h };
      const testComponent: GridComponent = {
        id: 'test',
        componentType: 'test',
        position
      };
      
      // Check if this position overlaps with any existing components
      const overlaps = findOverlappingComponents(testComponent, components);
      
      if (overlaps.length === 0) {
        return position;
      }
    }
    
    currentRow++;
    attempts++;
  }
  
  return null; // No available position found
};

/**
 * Compact grid by moving components up to fill empty spaces
 */
export const compactGrid = (
  components: GridComponent[],
  direction: 'vertical' | 'horizontal' = 'vertical'
): GridComponent[] => {
  if (direction === 'vertical') {
    return compactVertically(components);
  } else {
    return compactHorizontally(components);
  }
};

/**
 * Compact components vertically (move up to fill gaps)
 */
const compactVertically = (components: GridComponent[]): GridComponent[] => {
  // Sort by y position, then by x position
  const sorted = [...components].sort((a, b) => {
    if (a.position.y !== b.position.y) {
      return a.position.y - b.position.y;
    }
    return a.position.x - b.position.x;
  });
  
  const compacted: GridComponent[] = [];
  
  for (const component of sorted) {
    let newY = 0;
    
    // Find the lowest position where this component can fit
    while (true) {
      const testPosition: GridPosition = {
        ...component.position,
        y: newY
      };
      
      const testComponent: GridComponent = {
        ...component,
        position: testPosition
      };
      
      const overlaps = findOverlappingComponents(testComponent, compacted);
      
      if (overlaps.length === 0) {
        compacted.push({
          ...component,
          position: testPosition
        });
        break;
      }
      
      newY++;
    }
  }
  
  return compacted;
};

/**
 * Compact components horizontally (move left to fill gaps)
 */
const compactHorizontally = (components: GridComponent[]): GridComponent[] => {
  // Sort by x position, then by y position
  const sorted = [...components].sort((a, b) => {
    if (a.position.x !== b.position.x) {
      return a.position.x - b.position.x;
    }
    return a.position.y - b.position.y;
  });
  
  const compacted: GridComponent[] = [];
  
  for (const component of sorted) {
    let newX = 0;
    
    // Find the leftmost position where this component can fit
    while (true) {
      const testPosition: GridPosition = {
        ...component.position,
        x: newX
      };
      
      const testComponent: GridComponent = {
        ...component,
        position: testPosition
      };
      
      const overlaps = findOverlappingComponents(testComponent, compacted);
      
      if (overlaps.length === 0) {
        compacted.push({
          ...component,
          position: testPosition
        });
        break;
      }
      
      newX++;
    }
  }
  
  return compacted;
};

/**
 * Calculate the total height of the grid based on component positions
 */
export const calculateGridHeight = (components: GridComponent[]): number => {
  if (components.length === 0) {
    return 0;
  }
  
  return Math.max(...components.map(component => 
    component.position.y + component.position.h
  ));
};

/**
 * Check if a component is within a specific zone
 */
export const isComponentInZone = (component: GridComponent, zone: GridZone): boolean => {
  const { position } = component;
  const { bounds } = zone;
  
  return (
    position.x >= bounds.x &&
    position.y >= bounds.y &&
    position.x + position.w <= bounds.x + bounds.w &&
    position.y + position.h <= bounds.y + bounds.h
  );
};

/**
 * Get all components within a specific zone
 */
export const getComponentsInZone = (
  components: GridComponent[],
  zone: GridZone
): GridComponent[] => {
  return components.filter(component => isComponentInZone(component, zone));
};

/**
 * Check if a component type is allowed in a zone
 */
export const isComponentAllowedInZone = (
  componentType: string,
  zone: GridZone
): boolean => {
  if (!zone.accepts || zone.accepts.length === 0) {
    return true; // No restrictions
  }
  
  return zone.accepts.includes(componentType);
};

/**
 * Clone a grid component with modifications
 */
export const cloneGridComponent = (
  component: GridComponent,
  modifications: Partial<GridComponent>
): GridComponent => {
  return {
    ...component,
    ...modifications,
    position: {
      ...component.position,
      ...modifications.position
    },
    props: {
      ...component.props,
      ...modifications.props
    },
    display: {
      ...component.display,
      ...modifications.display
    }
  };
};