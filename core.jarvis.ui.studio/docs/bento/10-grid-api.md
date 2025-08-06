# Grid API Reference

## Overview

The Grid API provides programmatic control over the Bento grid system, including component management, layout operations, and grid manipulation.

## Core Grid API

### GridManager Class

```typescript
class GridManager {
  constructor(config: GridConfig);
  
  // Component operations
  addComponent(component: GridComponent): Promise<void>;
  updateComponent(id: string, updates: Partial<GridComponent>): Promise<void>;
  removeComponent(id: string): Promise<void>;
  getComponent(id: string): GridComponent | undefined;
  
  // Position operations
  moveComponent(id: string, position: GridPosition): Promise<void>;
  resizeComponent(id: string, size: Size): Promise<void>;
  swapComponents(id1: string, id2: string): Promise<void>;
  
  // Grid operations
  getLayout(): GridComponent[];
  clearGrid(): Promise<void>;
  loadGrid(grid: BentoGrid): Promise<void>;
  saveGrid(): Promise<BentoGrid>;
  
  // Validation
  validatePosition(component: GridComponent): ValidationResult;
  detectCollisions(component: GridComponent): GridComponent[];
  findAvailableSpace(size: Size): GridPosition | null;
  
  // Utilities
  packComponents(direction?: 'vertical' | 'horizontal'): Promise<void>;
  getOccupiedCells(): Set<string>;
  getGridBounds(): Bounds;
}
```

### Usage Example

```typescript
import { GridManager } from '@/services/bento/GridManager';

// Initialize grid manager
const gridManager = new GridManager({
  columns: 12,
  gap: 8,
  rowHeight: 100
});

// Add a component
await gridManager.addComponent({
  id: 'metric-1',
  componentType: 'MetricCard',
  position: { x: 0, y: 0, w: 3, h: 2 },
  props: {
    title: 'Revenue',
    value: 125000
  }
});

// Move component
await gridManager.moveComponent('metric-1', { 
  x: 3, 
  y: 0, 
  w: 3, 
  h: 2 
});

// Check for collisions
const collisions = gridManager.detectCollisions({
  id: 'new-component',
  position: { x: 2, y: 0, w: 4, h: 2 }
});

if (collisions.length > 0) {
  console.log('Would collide with:', collisions);
}
```

## Grid Configuration

### GridConfig Interface

```typescript
interface GridConfig {
  // Grid dimensions
  columns: number;
  rows?: number; // Optional, auto-expand if not set
  
  // Spacing
  gap: number; // Gap between components in pixels
  padding?: SpacingConfig; // Grid padding
  
  // Component sizing
  rowHeight?: number; // Height of each row in pixels
  columnWidth?: number; // Calculated if not provided
  
  // Behavior
  compactMode?: 'none' | 'vertical' | 'horizontal';
  allowOverlap?: boolean; // Default: false
  snapToGrid?: boolean; // Default: true
  
  // Constraints
  maxRows?: number;
  minColumns?: number;
  maxColumns?: number;
  
  // Callbacks
  onChange?: (grid: GridComponent[]) => void;
  onCollision?: (component: GridComponent, collisions: GridComponent[]) => void;
}
```

### Dynamic Grid Configuration

```typescript
// Create responsive grid configuration
const createResponsiveGrid = (deviceType: DeviceType): GridConfig => {
  switch (deviceType) {
    case DeviceType.Desktop:
      return {
        columns: 12,
        gap: 8,
        rowHeight: 100,
        padding: { top: 16, right: 16, bottom: 16, left: 16 }
      };
      
    case DeviceType.Tablet:
      return {
        columns: 8,
        gap: 6,
        rowHeight: 90,
        padding: { top: 12, right: 12, bottom: 12, left: 12 }
      };
      
    case DeviceType.Mobile:
      return {
        columns: 4,
        gap: 4,
        rowHeight: 80,
        padding: { top: 8, right: 8, bottom: 8, left: 8 },
        compactMode: 'vertical'
      };
  }
};
```

## Component Management

### Adding Components

```typescript
// Add with validation
const addComponentSafely = async (
  gridManager: GridManager,
  component: GridComponent
): Promise<boolean> => {
  // Validate position
  const validation = gridManager.validatePosition(component);
  if (!validation.valid) {
    console.error('Invalid position:', validation.errors);
    return false;
  }
  
  // Check for collisions
  const collisions = gridManager.detectCollisions(component);
  if (collisions.length > 0) {
    // Try to find available space
    const newPosition = gridManager.findAvailableSpace({
      w: component.position.w,
      h: component.position.h
    });
    
    if (newPosition) {
      component.position = newPosition;
    } else {
      console.error('No available space');
      return false;
    }
  }
  
  // Add component
  await gridManager.addComponent(component);
  return true;
};
```

### Batch Operations

```typescript
// Add multiple components efficiently
const batchAddComponents = async (
  gridManager: GridManager,
  components: GridComponent[]
): Promise<void> => {
  // Sort by size (larger first) for better packing
  const sorted = [...components].sort((a, b) => {
    const areaA = a.position.w * a.position.h;
    const areaB = b.position.w * b.position.h;
    return areaB - areaA;
  });
  
  // Add components with auto-positioning
  for (const component of sorted) {
    const position = gridManager.findAvailableSpace({
      w: component.position.w,
      h: component.position.h
    });
    
    if (position) {
      await gridManager.addComponent({
        ...component,
        position
      });
    }
  }
  
  // Pack components to minimize gaps
  await gridManager.packComponents('vertical');
};
```

## Position and Layout

### Position Utilities

```typescript
// Grid position helpers
class GridPositionUtils {
  // Convert pixel coordinates to grid position
  static pixelToGrid(
    pixel: Position,
    gridConfig: GridConfig
  ): GridPosition {
    const cellWidth = gridConfig.columnWidth || 
      (containerWidth - padding) / gridConfig.columns;
    const cellHeight = gridConfig.rowHeight || 100;
    
    return {
      x: Math.floor(pixel.x / cellWidth),
      y: Math.floor(pixel.y / cellHeight),
      w: 1,
      h: 1
    };
  }
  
  // Convert grid position to pixel coordinates
  static gridToPixel(
    gridPos: GridPosition,
    gridConfig: GridConfig
  ): Bounds {
    const cellWidth = gridConfig.columnWidth || 
      (containerWidth - padding) / gridConfig.columns;
    const cellHeight = gridConfig.rowHeight || 100;
    
    return {
      left: gridPos.x * cellWidth + (gridPos.x * gridConfig.gap),
      top: gridPos.y * cellHeight + (gridPos.y * gridConfig.gap),
      width: gridPos.w * cellWidth + ((gridPos.w - 1) * gridConfig.gap),
      height: gridPos.h * cellHeight + ((gridPos.h - 1) * gridConfig.gap),
      right: 0, // Calculate based on left + width
      bottom: 0 // Calculate based on top + height
    };
  }
  
  // Check if positions overlap
  static doPositionsOverlap(
    pos1: GridPosition,
    pos2: GridPosition
  ): boolean {
    return !(
      pos1.x + pos1.w <= pos2.x ||
      pos2.x + pos2.w <= pos1.x ||
      pos1.y + pos1.h <= pos2.y ||
      pos2.y + pos2.h <= pos1.y
    );
  }
}
```

### Layout Algorithms

```typescript
// Auto-layout algorithm
class GridLayoutEngine {
  // Pack components to minimize empty space
  static packVertical(components: GridComponent[]): GridComponent[] {
    const packed: GridComponent[] = [];
    const occupiedCells = new Set<string>();
    
    // Helper to check if position is free
    const isFree = (pos: GridPosition): boolean => {
      for (let x = pos.x; x < pos.x + pos.w; x++) {
        for (let y = pos.y; y < pos.y + pos.h; y++) {
          if (occupiedCells.has(`${x},${y}`)) {
            return false;
          }
        }
      }
      return true;
    };
    
    // Helper to mark cells as occupied
    const markOccupied = (pos: GridPosition) => {
      for (let x = pos.x; x < pos.x + pos.w; x++) {
        for (let y = pos.y; y < pos.y + pos.h; y++) {
          occupiedCells.add(`${x},${y}`);
        }
      }
    };
    
    // Pack each component
    for (const component of components) {
      let placed = false;
      
      // Try to place at current position
      if (isFree(component.position)) {
        markOccupied(component.position);
        packed.push(component);
        placed = true;
      } else {
        // Find first available position
        for (let y = 0; y < 100; y++) { // Max 100 rows
          for (let x = 0; x <= 12 - component.position.w; x++) {
            const testPos = {
              ...component.position,
              x,
              y
            };
            
            if (isFree(testPos)) {
              markOccupied(testPos);
              packed.push({
                ...component,
                position: testPos
              });
              placed = true;
              break;
            }
          }
          if (placed) break;
        }
      }
    }
    
    return packed;
  }
}
```

## Grid Events

### Event Types

```typescript
enum GridEvent {
  ComponentAdded = 'grid:component:added',
  ComponentRemoved = 'grid:component:removed',
  ComponentMoved = 'grid:component:moved',
  ComponentResized = 'grid:component:resized',
  ComponentUpdated = 'grid:component:updated',
  GridCleared = 'grid:cleared',
  GridLoaded = 'grid:loaded',
  LayoutChanged = 'grid:layout:changed'
}

interface GridEventData {
  type: GridEvent;
  component?: GridComponent;
  previousState?: Partial<GridComponent>;
  grid?: BentoGrid;
  timestamp: number;
}
```

### Event Handling

```typescript
// Grid event emitter
class GridEventEmitter extends EventEmitter {
  emit(event: GridEvent, data: Omit<GridEventData, 'type' | 'timestamp'>) {
    super.emit(event, {
      type: event,
      timestamp: Date.now(),
      ...data
    });
  }
}

// Usage
gridManager.on(GridEvent.ComponentMoved, (event: GridEventData) => {
  console.log('Component moved:', event.component?.id);
  console.log('From:', event.previousState?.position);
  console.log('To:', event.component?.position);
});

gridManager.on(GridEvent.LayoutChanged, (event: GridEventData) => {
  // Save layout to backend
  saveLayout(event.grid);
});
```

## Drag and Drop API

### Drag Operations

```typescript
interface DragAPI {
  // Start drag operation
  startDrag(componentId: string, position: Position): void;
  
  // Update drag position
  updateDrag(position: Position): void;
  
  // End drag operation
  endDrag(position: Position): Promise<void>;
  
  // Cancel drag
  cancelDrag(): void;
  
  // Get drag state
  getDragState(): DragState;
  
  // Preview
  getPreviewPosition(): GridPosition | null;
  setPreviewPosition(position: GridPosition): void;
}

interface DragState {
  isDragging: boolean;
  draggedComponent?: GridComponent;
  startPosition?: Position;
  currentPosition?: Position;
  previewPosition?: GridPosition;
  validDrop: boolean;
}
```

### Drag Implementation

```typescript
class GridDragHandler implements DragAPI {
  private gridManager: GridManager;
  private state: DragState;
  
  startDrag(componentId: string, position: Position) {
    const component = this.gridManager.getComponent(componentId);
    if (!component) return;
    
    this.state = {
      isDragging: true,
      draggedComponent: component,
      startPosition: position,
      currentPosition: position,
      validDrop: true
    };
  }
  
  updateDrag(position: Position) {
    if (!this.state.isDragging) return;
    
    // Convert to grid position
    const gridPos = GridPositionUtils.pixelToGrid(
      position,
      this.gridManager.config
    );
    
    // Update preview with dragged component size
    const previewPos = {
      ...gridPos,
      w: this.state.draggedComponent!.position.w,
      h: this.state.draggedComponent!.position.h
    };
    
    // Check validity
    const validation = this.gridManager.validatePosition({
      ...this.state.draggedComponent!,
      position: previewPos
    });
    
    this.state = {
      ...this.state,
      currentPosition: position,
      previewPosition: previewPos,
      validDrop: validation.valid && 
        this.gridManager.detectCollisions({
          ...this.state.draggedComponent!,
          position: previewPos
        }).length === 0
    };
  }
  
  async endDrag(position: Position) {
    if (!this.state.isDragging || !this.state.validDrop) {
      this.cancelDrag();
      return;
    }
    
    const finalPosition = this.state.previewPosition!;
    await this.gridManager.moveComponent(
      this.state.draggedComponent!.id,
      finalPosition
    );
    
    this.state = { isDragging: false };
  }
}
```

## Grid Persistence

### Save and Load

```typescript
interface GridPersistence {
  // Save grid state
  saveGrid(grid: BentoGrid): Promise<void>;
  
  // Load grid state
  loadGrid(gridId: string): Promise<BentoGrid>;
  
  // Auto-save
  enableAutoSave(interval: number): void;
  disableAutoSave(): void;
  
  // Versioning
  saveVersion(grid: BentoGrid, message?: string): Promise<string>;
  loadVersion(versionId: string): Promise<BentoGrid>;
  listVersions(gridId: string): Promise<GridVersion[]>;
}

// Implementation
class GridPersistenceService implements GridPersistence {
  private autoSaveInterval?: NodeJS.Timeout;
  
  async saveGrid(grid: BentoGrid): Promise<void> {
    await apiService.put(`/api/grids/${grid.id}`, grid);
  }
  
  async loadGrid(gridId: string): Promise<BentoGrid> {
    return await apiService.get(`/api/grids/${gridId}`);
  }
  
  enableAutoSave(interval: number = 30000) {
    this.autoSaveInterval = setInterval(async () => {
      const currentGrid = await gridManager.saveGrid();
      await this.saveGrid(currentGrid);
    }, interval);
  }
  
  disableAutoSave() {
    if (this.autoSaveInterval) {
      clearInterval(this.autoSaveInterval);
    }
  }
}
```

## Grid Hooks

### React Hooks

```typescript
// useGrid hook
export const useGrid = (gridId: string) => {
  const [grid, setGrid] = useState<BentoGrid | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  
  const gridManager = useRef<GridManager | null>(null);
  
  useEffect(() => {
    const loadGrid = async () => {
      try {
        setLoading(true);
        const loadedGrid = await gridPersistence.loadGrid(gridId);
        
        gridManager.current = new GridManager(loadedGrid.settings);
        await gridManager.current.loadGrid(loadedGrid);
        
        setGrid(loadedGrid);
      } catch (err) {
        setError(err as Error);
      } finally {
        setLoading(false);
      }
    };
    
    loadGrid();
  }, [gridId]);
  
  return {
    grid,
    loading,
    error,
    gridManager: gridManager.current,
    refresh: () => loadGrid()
  };
};

// useGridComponent hook
export const useGridComponent = (componentId: string) => {
  const { gridManager } = useGrid();
  const [component, setComponent] = useState<GridComponent | null>(null);
  
  useEffect(() => {
    if (!gridManager) return;
    
    const updateComponent = () => {
      setComponent(gridManager.getComponent(componentId) || null);
    };
    
    updateComponent();
    
    gridManager.on(GridEvent.ComponentUpdated, (event) => {
      if (event.component?.id === componentId) {
        updateComponent();
      }
    });
    
    return () => {
      gridManager.removeAllListeners(GridEvent.ComponentUpdated);
    };
  }, [gridManager, componentId]);
  
  return component;
};
```

## Performance Optimization

### Virtual Grid

```typescript
// Virtual grid for large grids
class VirtualGrid {
  private gridManager: GridManager;
  private viewport: Bounds;
  private visibleComponents: Set<string>;
  
  constructor(gridManager: GridManager) {
    this.gridManager = gridManager;
    this.visibleComponents = new Set();
  }
  
  updateViewport(viewport: Bounds) {
    this.viewport = viewport;
    this.updateVisibleComponents();
  }
  
  private updateVisibleComponents() {
    const allComponents = this.gridManager.getLayout();
    const newVisible = new Set<string>();
    
    for (const component of allComponents) {
      const bounds = GridPositionUtils.gridToPixel(
        component.position,
        this.gridManager.config
      );
      
      if (this.isInViewport(bounds)) {
        newVisible.add(component.id);
      }
    }
    
    this.visibleComponents = newVisible;
  }
  
  private isInViewport(bounds: Bounds): boolean {
    return !(
      bounds.right < this.viewport.left ||
      bounds.left > this.viewport.right ||
      bounds.bottom < this.viewport.top ||
      bounds.top > this.viewport.bottom
    );
  }
  
  getVisibleComponents(): GridComponent[] {
    return this.gridManager.getLayout().filter(
      c => this.visibleComponents.has(c.id)
    );
  }
}
```

## Next Steps

1. Review [Component API](./09-component-api.md) for component integration
2. Check [Storage API](./11-storage-api.md) for persistence details
3. See [Testing Strategy](./08-testing-strategy.md) for testing grid operations
4. Explore [Performance Guide](./13-performance-guide.md) for optimization