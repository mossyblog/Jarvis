# Bento Grid System

## Overview

The Bento Grid System is a flexible, responsive layout engine that provides precise control over component placement while maintaining ease of use.

## Grid Fundamentals

### Grid Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                          Grid Container                           │
│                                                                   │
│  Columns: 1    2    3    4    5    6    7    8    9   10   11  12│
│         ┌────┬────┬────┬────┬────┬────┬────┬────┬────┬────┬────┐│
│  Row 1  │ A  │ A  │ A  │ B  │ B  │ B  │ C  │ C  │ C  │ D  │ D  ││
│         ├────┴────┴────┼────┴────┴────┼────┴────┴────┼────┴────┤│
│  Row 2  │       E       │      F       │      G       │    H    ││
│         ├───────────────┴──────────────┴──────────────┴─────────┤│
│  Row 3  │                          I                             ││
│         ├────────────────────────────────────────────────────────┤│
│  Row 4  │              J              │           K              ││
│         └────────────────────────────┴──────────────────────────┘│
│                                                                   │
│  Gap: 8px (configurable)                                          │
│  Padding: 16px (configurable)                                     │
└─────────────────────────────────────────────────────────────────┘
```

### Grid Units

Components are positioned and sized using grid units:

```typescript
interface GridPosition {
  x: number; // Column start (0-based)
  y: number; // Row start (0-based)
  w: number; // Width in columns
  h: number; // Height in rows
}

// Example: Component spans columns 0-2 (3 columns wide)
// and rows 0-1 (2 rows tall)
const position: GridPosition = {
  x: 0,
  y: 0,
  w: 3,
  h: 2
};
```

## Responsive Grid Configurations

### Device Breakpoints

```typescript
enum DeviceType {
  Desktop = 'desktop',  // >= 1024px
  Tablet = 'tablet',    // >= 768px && < 1024px
  Mobile = 'mobile'     // < 768px
}

interface GridConfig {
  desktop: { columns: 12, gap: 8, padding: 16 };
  tablet:  { columns: 8,  gap: 6, padding: 12 };
  mobile:  { columns: 4,  gap: 4, padding: 8 };
}
```

### Responsive Behavior

```
Desktop (12 columns)          Tablet (8 columns)         Mobile (4 columns)
┌────┬────┬────┬────┐        ┌──────┬──────┐            ┌────────┐
│ A  │ B  │ C  │ D  │   →    │  A   │  B   │      →     │   A    │
├────┴────┴────┴────┤        ├──────┴──────┤            ├────────┤
│         E         │        │      C      │            │   B    │
└───────────────────┘        ├─────────────┤            ├────────┤
                             │      D      │            │   C    │
                             ├─────────────┤            ├────────┤
                             │      E      │            │   D    │
                             └─────────────┘            ├────────┤
                                                        │   E    │
                                                        └────────┘
```

## Grid Rules and Constraints

### 1. No Overlap Rule

Components cannot occupy the same grid cells:

```typescript
// Collision detection algorithm
function detectCollision(
  component: GridComponent,
  others: GridComponent[]
): boolean {
  return others.some(other => {
    if (component.id === other.id) return false;
    
    const horizontalOverlap = 
      component.position.x < other.position.x + other.position.w &&
      component.position.x + component.position.w > other.position.x;
      
    const verticalOverlap = 
      component.position.y < other.position.y + other.position.h &&
      component.position.y + component.position.h > other.position.y;
      
    return horizontalOverlap && verticalOverlap;
  });
}
```

### 2. Boundary Constraints

Components must stay within grid bounds:

```typescript
function validateBounds(
  position: GridPosition,
  gridColumns: number
): boolean {
  return (
    position.x >= 0 &&
    position.y >= 0 &&
    position.x + position.w <= gridColumns &&
    position.w > 0 &&
    position.h > 0
  );
}
```

### 3. Size Constraints

Components have min/max size limits:

```typescript
interface ComponentConstraints {
  minSize: { w: number; h: number };
  maxSize: { w: number; h: number };
  aspectRatio?: number; // Optional fixed aspect ratio
}

// Validation
function validateSize(
  size: Size,
  constraints: ComponentConstraints
): boolean {
  return (
    size.w >= constraints.minSize.w &&
    size.h >= constraints.minSize.h &&
    size.w <= constraints.maxSize.w &&
    size.h <= constraints.maxSize.h
  );
}
```

## Grid Operations

### 1. Adding Components

```
Initial Grid                  After Adding Component C
┌────┬────┬────┬────┐        ┌────┬────┬────┬────┐
│ A  │ A  │ B  │ B  │        │ A  │ A  │ B  │ B  │
├────┴────┼────┴────┤   →    ├────┴────┼────┴────┤
│ Empty   │ Empty   │        │ C  │ C  │ Empty   │
└─────────┴─────────┘        └────┴────┴─────────┘
```

### 2. Moving Components

```typescript
// Drag and drop movement
function moveComponent(
  component: GridComponent,
  newPosition: GridPosition,
  grid: GridComponent[]
): GridComponent[] {
  // Check if new position is valid
  if (!validateBounds(newPosition, GRID_COLUMNS)) {
    return grid; // Invalid move, return unchanged
  }
  
  // Check for collisions
  const testComponent = { ...component, position: newPosition };
  const others = grid.filter(c => c.id !== component.id);
  
  if (detectCollision(testComponent, others)) {
    // Try to push other components
    return attemptPush(testComponent, others);
  }
  
  // Update position
  return grid.map(c => 
    c.id === component.id 
      ? { ...c, position: newPosition }
      : c
  );
}
```

### 3. Resizing Components

```
Before Resize                 After Resize (Expand Right)
┌────┬────┬────┬────┐        ┌────┬────┬────┬────┐
│ A  │ A  │ B  │ B  │        │ A  │ A  │ A  │ B  │
├────┴────┼────┴────┤   →    ├────┴────┴────┼────┤
│ C  │ C  │ D  │ D  │        │ C  │ C  │ D  │ D  │
└────┴────┴────┴────┘        └────┴────┴────┴────┘
```

### 4. Auto-Layout (Packing)

```typescript
// Automatically arrange components to minimize empty space
function packComponents(components: GridComponent[]): GridComponent[] {
  // Sort by size (larger first) and position
  const sorted = [...components].sort((a, b) => {
    const areaA = a.position.w * a.position.h;
    const areaB = b.position.w * b.position.h;
    return areaB - areaA;
  });
  
  const packed: GridComponent[] = [];
  
  sorted.forEach(component => {
    const position = findBestPosition(component, packed);
    packed.push({ ...component, position });
  });
  
  return packed;
}
```

## Grid Rendering

### CSS Grid Implementation

```css
.bento-grid {
  display: grid;
  grid-template-columns: repeat(var(--grid-columns), 1fr);
  grid-auto-rows: var(--grid-row-height, 100px);
  gap: var(--grid-gap);
  padding: var(--grid-padding);
}

.bento-component {
  grid-column: span var(--component-width);
  grid-row: span var(--component-height);
}
```

### React Implementation

```typescript
const BentoGrid: React.FC<BentoGridProps> = ({ 
  grid, 
  columns, 
  gap, 
  padding 
}) => {
  return (
    <div 
      className="bento-grid"
      style={{
        '--grid-columns': columns,
        '--grid-gap': `${gap}px`,
        '--grid-padding': `${padding}px`
      }}
    >
      {grid.components.map(component => (
        <div
          key={component.id}
          className="bento-component"
          style={{
            gridColumn: `${component.position.x + 1} / span ${component.position.w}`,
            gridRow: `${component.position.y + 1} / span ${component.position.h}`
          }}
        >
          <ComponentRenderer component={component} />
        </div>
      ))}
    </div>
  );
};
```

## Drag and Drop Mechanics

### Drag States

```typescript
enum DragState {
  Idle = 'idle',
  Dragging = 'dragging',
  Resizing = 'resizing',
  Invalid = 'invalid' // Position would cause collision
}

interface DragContext {
  state: DragState;
  component?: GridComponent;
  startPosition?: Position;
  currentPosition?: Position;
  previewPosition?: GridPosition;
}
```

### Visual Feedback

```
During Drag                   During Resize
┌────┬────┬────┬────┐        ┌────┬────┬────┬────┐
│ A  │ A  │░░░░│░░░░│        │ A  │ A  │ B  │ B ══
├────┴────┼────┴────┤        ├────┴────┼────┴────┤
│ C  │ C  │ D  │ D  │        │ C  │ C  │ D  │ D  │
└────┴────┴────┴────┘        └────┴────┴────┴────┘
 ░░░ = Ghost preview           ══ = Resize handle
```

### Snap to Grid

```typescript
function snapToGrid(
  position: Position,
  gridSize: number
): Position {
  return {
    x: Math.round(position.x / gridSize) * gridSize,
    y: Math.round(position.y / gridSize) * gridSize
  };
}

// Convert pixel position to grid position
function pixelToGrid(
  pixel: Position,
  cellSize: Size
): GridPosition {
  return {
    x: Math.floor(pixel.x / cellSize.width),
    y: Math.floor(pixel.y / cellSize.height),
    w: 1, // Default size
    h: 1
  };
}
```

## Advanced Grid Features

### 1. Grid Templates

Pre-defined layouts for common use cases:

```typescript
const templates = {
  dashboard: {
    name: 'Standard Dashboard',
    components: [
      { type: 'Header', position: { x: 0, y: 0, w: 12, h: 1 } },
      { type: 'Metric', position: { x: 0, y: 1, w: 3, h: 2 } },
      { type: 'Metric', position: { x: 3, y: 1, w: 3, h: 2 } },
      { type: 'Metric', position: { x: 6, y: 1, w: 3, h: 2 } },
      { type: 'Metric', position: { x: 9, y: 1, w: 3, h: 2 } },
      { type: 'Chart', position: { x: 0, y: 3, w: 8, h: 4 } },
      { type: 'List', position: { x: 8, y: 3, w: 4, h: 4 } }
    ]
  }
};
```

### 2. Grid Zones

Define areas with special behaviors:

```typescript
interface GridZone {
  id: string;
  bounds: GridPosition;
  type: 'header' | 'sidebar' | 'content' | 'footer';
  locked?: boolean; // Prevent modifications
  accepts?: string[]; // Component types allowed
}
```

### 3. Responsive Adaptation

Automatic layout adjustment for different devices:

```typescript
function adaptToDevice(
  desktopGrid: BentoGrid,
  targetDevice: DeviceType
): BentoGrid {
  const ratio = getColumnRatio(targetDevice);
  
  return {
    ...desktopGrid,
    components: desktopGrid.components.map(component => {
      const newWidth = Math.max(
        1,
        Math.round(component.position.w * ratio)
      );
      
      return {
        ...component,
        position: {
          ...component.position,
          w: newWidth,
          // Reflow vertically as needed
          y: calculateNewY(component, newWidth)
        }
      };
    })
  };
}
```

## Grid Performance

### Optimization Strategies

1. **Virtual Rendering**: Only render visible components
2. **Memoization**: Cache layout calculations
3. **Debouncing**: Delay resize recalculations
4. **Web Workers**: Offload complex calculations

```typescript
// Example: Virtualized grid
const VirtualGrid = ({ grid, viewportHeight }) => {
  const visibleComponents = useMemo(() => {
    const rowHeight = 100; // px
    const startRow = Math.floor(scrollTop / rowHeight);
    const endRow = Math.ceil((scrollTop + viewportHeight) / rowHeight);
    
    return grid.components.filter(component => {
      const componentStart = component.position.y;
      const componentEnd = component.position.y + component.position.h;
      return componentEnd >= startRow && componentStart <= endRow;
    });
  }, [grid, scrollTop, viewportHeight]);
  
  return <BentoGrid components={visibleComponents} />;
};
```

## Next Steps

1. Learn about [Component Registry](./04-component-registry.md) for component management
2. Explore [Page Builder](./05-page-builder.md) for using the grid system
3. Review [Implementation Plan](./07-implementation-plan.md) for development phases
4. Check [Testing Strategy](./08-testing-strategy.md) for grid testing approaches