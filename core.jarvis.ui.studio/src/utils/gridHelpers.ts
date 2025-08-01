/**
 * Grid Helper Utilities
 * 
 * Smart interaction utilities for the Bento Grid System including:
 * - Auto-placement algorithms
 * - Magnetic snapping
 * - Drop zone validation
 * - Context-aware help
 */

import type { GridPosition, GridComponent, BentoGrid, Size } from '@/types/bento';

// ============================================================================
// Constants
// ============================================================================

/** Enhanced snapping threshold in pixels */
export const SNAP_THRESHOLD = 15;

/** Maximum attempts to find placement position */
export const MAX_PLACEMENT_ATTEMPTS = 100;

// ============================================================================
// Types
// ============================================================================

export interface DropZone {
  position: GridPosition;
  isValid: boolean;
  reason?: string;
}

export interface SnapResult {
  position: GridPosition;
  snapped: boolean;
  deltaX: number;
  deltaY: number;
  snapStrength?: number; // 0-1, for visual feedback
}

export interface PlacementResult {
  position: GridPosition | null;
  success: boolean;
  message: string;
}

export interface HelpMessage {
  type: 'success' | 'warning' | 'error' | 'info';
  message: string;
  duration?: number;
}

// ============================================================================
// Auto-placement Algorithm
// ============================================================================

/**
 * Find the best position for a new component using top-left priority
 * with gap minimization strategy
 */
export const findBestPlacement = (
  componentSize: Size,
  grid: BentoGrid,
  excludeId?: string
): PlacementResult => {
  const { w, h } = componentSize;
  const { columns, components } = grid;

  // Filter out excluded component
  const existingComponents = excludeId 
    ? components.filter(c => c.id !== excludeId)
    : components;

  // Try to find position starting from top-left
  for (let y = 0; y < MAX_PLACEMENT_ATTEMPTS; y++) {
    for (let x = 0; x <= columns - w; x++) {
      const position: GridPosition = { x, y, w, h };
      
      if (isValidPlacement(position, existingComponents, columns)) {
        return {
          position,
          success: true,
          message: `Placed at row ${y + 1}, column ${x + 1}`
        };
      }
    }
  }

  return {
    position: null,
    success: false,
    message: 'No available space found for component'
  };
};

/**
 * Find the optimal position to minimize gaps in the grid
 */
export const findOptimalPlacement = (
  componentSize: Size,
  grid: BentoGrid,
  excludeId?: string
): PlacementResult => {
  const { w, h } = componentSize;
  const { columns, components } = grid;

  const existingComponents = excludeId 
    ? components.filter(c => c.id !== excludeId)
    : components;

  let bestPosition: GridPosition | null = null;
  let minGapScore = Infinity;

  // Try all possible positions and score them
  for (let y = 0; y < MAX_PLACEMENT_ATTEMPTS; y++) {
    for (let x = 0; x <= columns - w; x++) {
      const position: GridPosition = { x, y, w, h };
      
      if (isValidPlacement(position, existingComponents, columns)) {
        const gapScore = calculateGapScore(position, existingComponents);
        
        if (gapScore < minGapScore) {
          minGapScore = gapScore;
          bestPosition = position;
        }
        
        // If we found a perfect score (no gaps), use it immediately
        if (gapScore === 0) {
          break;
        }
      }
    }
    
    if (bestPosition && minGapScore === 0) {
      break;
    }
  }

  if (bestPosition) {
    return {
      position: bestPosition,
      success: true,
      message: `Optimally placed with gap score: ${minGapScore}`
    };
  }

  return {
    position: null,
    success: false,
    message: 'No optimal placement found'
  };
};

/**
 * Calculate a score based on how much empty space surrounds a position
 * Lower scores are better (less gaps)
 */
const calculateGapScore = (
  position: GridPosition,
  existingComponents: GridComponent[]
): number => {
  let score = 0;
  
  // Penalty for being far from top-left
  score += position.x * 0.1 + position.y * 0.2;
  
  // Penalty for gaps around the component
  const adjacentPositions = [
    { x: position.x - 1, y: position.y }, // Left
    { x: position.x + position.w, y: position.y }, // Right
    { x: position.x, y: position.y - 1 }, // Above
    { x: position.x, y: position.y + position.h }, // Below
  ];
  
  adjacentPositions.forEach(adj => {
    const hasNeighbor = existingComponents.some(comp => 
      comp.position.x <= adj.x && 
      comp.position.x + comp.position.w > adj.x &&
      comp.position.y <= adj.y && 
      comp.position.y + comp.position.h > adj.y
    );
    
    if (!hasNeighbor) {
      score += 1; // Penalty for each missing neighbor
    }
  });
  
  return score;
};

// ============================================================================
// Magnetic Snapping
// ============================================================================

/**
 * Apply enhanced magnetic snapping to grid lines with smoother behavior
 */
export const applyMagneticSnapping = (
  mousePosition: { x: number; y: number },
  gridRect: DOMRect,
  grid: BentoGrid,
  componentSize: Size
): SnapResult => {
  const { columns, gap, rowHeight = 100 } = grid;
  const { w, h } = componentSize;
  
  // Calculate cell dimensions with better precision
  const cellWidth = (gridRect.width - (gap * (columns - 1))) / columns;
  const cellHeight = rowHeight;
  const cellSizeX = cellWidth + gap;
  const cellSizeY = cellHeight + gap;
  
  // Convert mouse position to relative grid coordinates
  const relativeX = mousePosition.x - gridRect.left;
  const relativeY = mousePosition.y - gridRect.top;
  
  // Calculate the exact grid position (with decimals)
  const exactX = relativeX / cellSizeX;
  const exactY = relativeY / cellSizeY;
  
  // Find nearest grid positions
  const nearestX = Math.round(exactX);
  const nearestY = Math.round(exactY);
  
  // Calculate pixel distances to nearest grid lines
  const deltaPixelsX = Math.abs(exactX - nearestX) * cellSizeX;
  const deltaPixelsY = Math.abs(exactY - nearestY) * cellSizeY;
  
  // Enhanced snapping with progressive magnetism
  const snapStrengthX = Math.max(0, 1 - (deltaPixelsX / SNAP_THRESHOLD));
  const snapStrengthY = Math.max(0, 1 - (deltaPixelsY / SNAP_THRESHOLD));
  
  const snapX = deltaPixelsX <= SNAP_THRESHOLD;
  const snapY = deltaPixelsY <= SNAP_THRESHOLD;
  
  // Apply smooth interpolation for better feel
  const finalX = snapX ? nearestX : exactX;
  const finalY = snapY ? nearestY : exactY;
  
  // Constrain to grid bounds with smooth clamping
  const constrainedX = Math.max(0, Math.min(columns - w, Math.floor(finalX)));
  const constrainedY = Math.max(0, Math.floor(finalY));
  
  return {
    position: {
      x: constrainedX,
      y: constrainedY,
      w,
      h
    },
    snapped: snapX || snapY,
    deltaX: deltaPixelsX,
    deltaY: deltaPixelsY,
    snapStrength: Math.max(snapStrengthX, snapStrengthY), // For visual feedback
  };
};

/**
 * Apply snapping to existing components (edge alignment)
 */
export const applyComponentSnapping = (
  position: GridPosition,
  existingComponents: GridComponent[],
  threshold: number = 1
): GridPosition => {
  let { x, y, w, h } = position;
  
  // Find components that could provide snap targets
  existingComponents.forEach(comp => {
    const compPos = comp.position;
    
    // Horizontal alignment opportunities
    if (Math.abs(y - compPos.y) <= threshold || 
        Math.abs(y + h - (compPos.y + compPos.h)) <= threshold) {
      
      // Snap to left edge
      if (Math.abs(x - (compPos.x + compPos.w)) <= threshold) {
        x = compPos.x + compPos.w;
      }
      // Snap to right edge  
      else if (Math.abs(x + w - compPos.x) <= threshold) {
        x = compPos.x - w;
      }
    }
    
    // Vertical alignment opportunities
    if (Math.abs(x - compPos.x) <= threshold || 
        Math.abs(x + w - (compPos.x + compPos.w)) <= threshold) {
      
      // Snap to top edge
      if (Math.abs(y - (compPos.y + compPos.h)) <= threshold) {
        y = compPos.y + compPos.h;
      }
      // Snap to bottom edge
      else if (Math.abs(y + h - compPos.y) <= threshold) {
        y = compPos.y - h;
      }
    }
  });
  
  return { x: Math.max(0, x), y: Math.max(0, y), w, h };
};

// ============================================================================
// Drop Zone Visualization
// ============================================================================

/**
 * Generate all valid drop zones for a component with overlap detection
 */
export const generateDropZones = (
  componentSize: Size,
  grid: BentoGrid,
  excludeId?: string
): DropZone[] => {
  const { w, h } = componentSize;
  const { columns, components } = grid;
  const dropZones: DropZone[] = [];
  const seenPositions = new Set<string>();
  
  const existingComponents = excludeId 
    ? components.filter(c => c.id !== excludeId)
    : components;

  // Generate zones for reasonable area (avoid too many zones)
  const maxY = Math.min(15, calculateGridHeight(existingComponents) + 3);
  
  for (let y = 0; y <= maxY; y++) {
    for (let x = 0; x <= columns - w; x++) {
      const position: GridPosition = { x, y, w, h };
      const positionKey = `${x}-${y}-${w}-${h}`;
      
      // Skip if we've already processed this exact position
      if (seenPositions.has(positionKey)) {
        continue;
      }
      seenPositions.add(positionKey);
      
      const isValid = isValidPlacement(position, existingComponents, columns);
      
      // Only add valid zones to reduce visual clutter
      if (isValid) {
        dropZones.push({
          position,
          isValid: true,
          reason: undefined
        });
      }
    }
  }
  
  return dropZones;
};

/**
 * Filter drop zones to show only the most relevant ones without overlaps
 */
export const getRelevantDropZones = (
  dropZones: DropZone[],
  maxZones: number = 12
): DropZone[] => {
  // Prioritize valid zones closer to top-left
  const validZones = dropZones
    .filter(zone => zone.isValid)
    .sort((a, b) => {
      const scoreA = a.position.y * 10 + a.position.x;
      const scoreB = b.position.y * 10 + b.position.x;
      return scoreA - scoreB;
    });
    
  // Remove overlapping zones to prevent visual conflicts
  const nonOverlappingZones: DropZone[] = [];
  
  for (const zone of validZones) {
    const hasOverlap = nonOverlappingZones.some(existing => 
      componentsOverlap(zone.position, existing.position)
    );
    
    if (!hasOverlap) {
      nonOverlappingZones.push(zone);
      
      // Stop when we have enough zones
      if (nonOverlappingZones.length >= maxZones) {
        break;
      }
    }
  }
    
  return nonOverlappingZones;
};

// ============================================================================
// Validation Helpers
// ============================================================================

/**
 * Check if a position is valid (no overlaps, within bounds)
 */
export const isValidPlacement = (
  position: GridPosition,
  existingComponents: GridComponent[],
  gridColumns: number
): boolean => {
  const { x, y, w, h } = position;
  
  // Check grid bounds
  if (x < 0 || y < 0 || x + w > gridColumns || w <= 0 || h <= 0) {
    return false;
  }
  
  // Check for overlaps
  return !existingComponents.some(comp => 
    componentsOverlap(position, comp.position)
  );
};

/**
 * Get reason why a placement is invalid
 */
export const getInvalidReason = (
  position: GridPosition,
  existingComponents: GridComponent[],
  gridColumns: number
): string => {
  const { x, y, w, h } = position;
  
  if (x < 0 || y < 0) {
    return 'Position is outside grid bounds';
  }
  
  if (x + w > gridColumns) {
    return 'Component extends beyond grid width';
  }
  
  if (w <= 0 || h <= 0) {
    return 'Invalid component dimensions';
  }
  
  const overlapping = existingComponents.find(comp => 
    componentsOverlap(position, comp.position)
  );
  
  if (overlapping) {
    return `Overlaps with existing component`;
  }
  
  return 'Unknown validation error';
};

/**
 * Check if two positions overlap
 */
export const componentsOverlap = (a: GridPosition, b: GridPosition): boolean => {
  return !(
    a.x >= b.x + b.w ||
    a.x + a.w <= b.x ||
    a.y >= b.y + b.h ||
    a.y + a.h <= b.y
  );
};

/**
 * Calculate the total height of components
 */
export const calculateGridHeight = (components: GridComponent[]): number => {
  if (components.length === 0) return 0;
  
  return Math.max(...components.map(comp => 
    comp.position.y + comp.position.h
  ));
};

// ============================================================================
// Context-aware Help
// ============================================================================

/**
 * Generate context-aware help messages based on user actions
 */
export const getContextualHelp = (
  action: string,
  context: {
    isDragging?: boolean;
    isResizing?: boolean;
    hasCollision?: boolean;
    componentCount?: number;
    gridFull?: boolean;
    snapActive?: boolean;
  }
): HelpMessage | null => {
  const { isDragging, isResizing, hasCollision, componentCount, gridFull, snapActive } = context;
  
  // Use the context parameter to suppress unused warning
  void context;
  
  if (isDragging) {
    if (hasCollision) {
      return {
        type: 'error',
        message: 'Cannot place here - position is occupied',
        duration: 1500
      };
    }
    
    if (snapActive) {
      return {
        type: 'success',
        message: 'Snapped to grid - perfect alignment!',
        duration: 1000
      };
    }
    
    return {
      type: 'info',
      message: 'Drag to reposition - green zones are valid',
      duration: 1500
    };
  }
  
  if (isResizing) {
    return {
      type: 'info',
      message: 'Drag handles to resize. Component will snap to grid boundaries.',
      duration: 2000
    };
  }
  
  if (action === 'add-component') {
    if (gridFull) {
      return {
        type: 'warning',
        message: 'Grid is full. Remove some components or resize existing ones.',
        duration: 4000
      };
    }
    
    if ((componentCount ?? 0) === 0) {
      return {
        type: 'success',
        message: 'Welcome! Drag components from the toolbar to build your layout.',
        duration: 5000
      };
    }
    
    return {
      type: 'info',
      message: 'Click or drag a component from the toolbar to add it to the grid.',
      duration: 3000
    };
  }
  
  if (action === 'edit-mode-enabled') {
    return {
      type: 'info',
      message: 'Edit mode enabled. You can now move, resize, and modify components.',
      duration: 3000
    };
  }
  
  if (action === 'edit-mode-disabled') {
    return {
      type: 'success',
      message: 'Edit mode disabled. Layout is now locked.',
      duration: 2000
    };
  }
  
  return null;
};

/**
 * Get tooltip text for various UI elements
 */
export const getTooltipText = (
  element: string,
  context?: Record<string, unknown>
): string => {
  const tooltips: Record<string, string> = {
    'drag-handle': 'Drag to move component',
    'resize-handle': 'Drag to resize component',
    'delete-button': 'Delete component',
    'properties-button': 'Edit component properties',
    'grid-toggle': 'Toggle grid overlay',
    'snap-toggle': 'Toggle magnetic snapping',
    'add-component': 'Add component to grid',
    'drop-zone-valid': 'Valid drop location',
    'drop-zone-invalid': 'Invalid drop location - overlaps existing component',
    'locked-component': 'Component is locked and cannot be modified'
  };
  
  return tooltips[element] || 'No tooltip available';
};

/**
 * Get error message for failed operations
 */
export const getErrorMessage = (
  operation: string,
  error?: string
): string => {
  const errorMessages: Record<string, string> = {
    'placement-failed': 'Could not place component - no available space',
    'resize-failed': 'Cannot resize - would cause overlap with other components',
    'move-failed': 'Cannot move component to this location',
    'bounds-exceeded': 'Component would extend outside the grid',
    'overlap-detected': 'Cannot place here - overlaps with existing component',
    'invalid-size': 'Component size is invalid',
    'grid-full': 'Grid is at capacity - remove components or resize existing ones'
  };
  
  return error || errorMessages[operation] || 'An unknown error occurred';
};

// ============================================================================
// Performance Optimization Helpers
// ============================================================================

/**
 * Debounce function for reducing frequent calculations
 */
export const debounce = <T extends (...args: unknown[]) => void>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout;
  
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
};

/**
 * Enhanced throttle function with immediate first call and trailing execution
 */
export const throttle = <T extends (...args: any[]) => void>(
  func: T,
  limit: number
): ((...args: Parameters<T>) => void) => {
  let inThrottle: boolean;
  let lastArgs: Parameters<T> | null = null;
  
  return (...args: Parameters<T>) => {
    lastArgs = args;
    
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      
      setTimeout(() => {
        inThrottle = false;
        // Execute the last call if there was one
        if (lastArgs) {
          func(...lastArgs);
          lastArgs = null;
        }
      }, limit);
    }
  };
};

/**
 * Calculate optimized set of drop zones with better performance and no overlaps
 */
export const getOptimizedDropZones = (
  componentSize: Size,
  grid: BentoGrid,
  excludeId?: string,
  maxZones: number = 8
): DropZone[] => {
  const allZones = generateDropZones(componentSize, grid, excludeId);
  return getRelevantDropZones(allZones, maxZones);
};

/**
 * Create smooth easing function for animations
 */
export const easeOutCubic = (t: number): number => {
  return 1 - Math.pow(1 - t, 3);
};

/**
 * Create smooth easing function for snap animations
 */
export const easeOutBack = (t: number): number => {
  const c1 = 1.70158;
  const c3 = c1 + 1;
  return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
};

/**
 * Calculate visual feedback intensity based on drag state
 */
export const calculateFeedbackIntensity = (
  dragDistance: number,
  snapStrength: number,
  isValid: boolean
): number => {
  const baseIntensity = Math.min(1, dragDistance / 100);
  const snapBoost = snapStrength * 0.3;
  const validityMultiplier = isValid ? 1 : 0.7;
  
  return Math.min(1, (baseIntensity + snapBoost) * validityMultiplier);
};

/**
 * Check if a drop zone would be too close to existing zones
 */
export const isDropZoneTooClose = (
  zone: DropZone,
  existingZones: DropZone[],
  minDistance: number = 1
): boolean => {
  return existingZones.some(existing => {
    const dx = Math.abs(zone.position.x - existing.position.x);
    const dy = Math.abs(zone.position.y - existing.position.y);
    return dx < minDistance && dy < minDistance;
  });
};

/**
 * Generate strategic drop zones focusing on key placement areas
 */
export const generateStrategicDropZones = (
  componentSize: Size,
  grid: BentoGrid,
  excludeId?: string,
  maxZones: number = 6
): DropZone[] => {
  const { w, h } = componentSize;
  const { columns, components } = grid;
  const strategicZones: DropZone[] = [];
  
  const existingComponents = excludeId 
    ? components.filter(c => c.id !== excludeId)
    : components;

  // Find strategic positions: top-left, adjacent to existing components, bottom areas
  const gridHeight = calculateGridHeight(existingComponents);
  const positions = [
    // Top-left priority
    { x: 0, y: 0 },
    // Adjacent to existing components
    ...existingComponents.flatMap(comp => [
      { x: comp.position.x + comp.position.w, y: comp.position.y },
      { x: comp.position.x, y: comp.position.y + comp.position.h },
      { x: Math.max(0, comp.position.x - w), y: comp.position.y },
      { x: comp.position.x, y: Math.max(0, comp.position.y - h) }
    ]),
    // Bottom row for expansion
    { x: 0, y: gridHeight },
    { x: Math.max(0, columns - w), y: gridHeight }
  ];

  const seenPositions = new Set<string>();
  
  for (const pos of positions) {
    if (pos.x + w > columns || pos.x < 0 || pos.y < 0) continue;
    
    const position: GridPosition = { x: pos.x, y: pos.y, w, h };
    const positionKey = `${pos.x}-${pos.y}`;
    
    if (seenPositions.has(positionKey)) continue;
    seenPositions.add(positionKey);
    
    if (isValidPlacement(position, existingComponents, columns)) {
      strategicZones.push({
        position,
        isValid: true,
        reason: undefined
      });
      
      if (strategicZones.length >= maxZones) break;
    }
  }
  
  return strategicZones;
};