# Bento Architecture

## System Architecture Overview

The Bento Grid System follows a layered architecture that separates concerns and enables flexibility:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                         │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────────┐  │
│  │ Grid Editor │  │ Page Builder │  │   Page Renderer       │  │
│  └─────────────┘  └──────────────┘  └───────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                         Business Layer                            │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────────┐  │
│  │Grid Manager │  │Component     │  │   Layout Engine       │  │
│  │             │  │Registry      │  │                       │  │
│  └─────────────┘  └──────────────┘  └───────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                          Data Layer                               │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────────┐  │
│  │Page Storage │  │Layout Storage│  │  Component Storage    │  │
│  │(ECS)        │  │(ECS)         │  │  (ECS)                │  │
│  └─────────────┘  └──────────────┘  └───────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Presentation Layer

#### Grid Editor (`/components/bento/GridEditor`)
Visual interface for arranging components:

```typescript
interface GridEditorProps {
  gridId: string;
  device: DeviceType;
  onSave: (grid: BentoGrid) => void;
  preview?: boolean;
}
```

Key responsibilities:
- Drag-and-drop interface
- Visual grid guidelines
- Component palette
- Properties panel
- Device preview switching

#### Page Builder (`/components/bento/PageBuilder`)
High-level page configuration interface:

```typescript
interface PageBuilderProps {
  pageId?: string; // Edit existing or create new
  onSave: (page: BentoPage) => void;
}
```

Key responsibilities:
- Page metadata editing
- Security configuration
- Layout selection
- Navigation settings

#### Page Renderer (`/components/bento/PageRenderer`)
Runtime rendering engine:

```typescript
interface PageRendererProps {
  pageId: string;
  device?: DeviceType; // Auto-detect if not provided
  dataContext?: Record<string, any>;
}
```

Key responsibilities:
- Load page configuration
- Select appropriate grid for device
- Render components with data
- Handle interactions

### 2. Business Layer

#### Grid Manager (`/services/bento/GridManager`)
Core grid logic and state management:

```typescript
class GridManager {
  // Grid manipulation
  addComponent(component: GridComponent): void;
  moveComponent(id: string, position: GridPosition): void;
  resizeComponent(id: string, size: Size): void;
  removeComponent(id: string): void;
  
  // Validation
  validatePosition(component: GridComponent): boolean;
  detectCollisions(component: GridComponent): GridComponent[];
  
  // Grid operations
  getLayout(): GridComponent[];
  clearGrid(): void;
  loadGrid(grid: BentoGrid): void;
}
```

#### Component Registry (`/services/bento/ComponentRegistry`)
Central registry for all Bento components:

```typescript
interface ComponentRegistry {
  register(key: string, config: ComponentConfig): void;
  get(key: string): ComponentConfig;
  getByCategory(category: string): ComponentConfig[];
  getAll(): Record<string, ComponentConfig>;
}

interface ComponentConfig {
  component: React.ComponentType<any>;
  displayName: string;
  category: string;
  icon: string;
  defaultProps: Record<string, any>;
  constraints: ComponentConstraints;
  dataBinding?: DataBindingConfig;
}
```

#### Layout Engine (`/services/bento/LayoutEngine`)
Responsive layout calculations:

```typescript
class LayoutEngine {
  // Layout calculations
  calculateGridLayout(components: GridComponent[], columns: number): Layout;
  
  // Responsive behavior
  adaptLayout(desktop: BentoGrid, device: DeviceType): BentoGrid;
  
  // Optimization
  packComponents(components: GridComponent[]): GridComponent[];
  optimizeSpace(grid: BentoGrid): BentoGrid;
}
```

### 3. Data Layer

#### Storage Architecture
Using Jarvis ECS pattern for all storage:

```typescript
// Page Component
interface BentoPageComponent extends IComponent {
  displayName: string;
  route: string;
  layoutId: string;
  securityBindings: string; // JSON
  visibilityBindings: string; // JSON
  metadata?: string; // JSON
}

// Layout Component  
interface BentoLayoutComponent extends IComponent {
  name: string;
  description?: string;
  desktopGridId: string;
  tabletGridId?: string;
  mobileGridId?: string;
}

// Grid Component
interface BentoGridComponent extends IComponent {
  name: string;
  device: string;
  columns: number;
  rows?: number;
  gap: number;
  components: string; // JSON array of GridComponent
}
```

## Data Flow Architecture

### 1. Edit Mode Flow

```
User Action → Grid Editor → Grid Manager → Validation → State Update
                                               ↓
Preview ← Layout Engine ← Component Registry ← ┘
```

### 2. Runtime Flow

```
Page Request → Page Renderer → Load Page Config → Load Layout
                                      ↓
Display ← Component Render ← Grid Selection ← Device Detection
```

### 3. Save Flow

```
Save Action → Validation → ECS Handlers → PostgreSQL
                 ↓
           Navigation Update → Cache Invalidation
```

## Component Lifecycle

### 1. Registration Phase
```typescript
// During app initialization
componentRegistry.register('MetricCard', {
  component: MetricCard,
  displayName: 'Metric Card',
  category: 'Analytics',
  defaultProps: { /* ... */ },
  constraints: { /* ... */ }
});
```

### 2. Design Phase
```typescript
// In Grid Editor
const component = {
  id: generateId(),
  componentType: 'MetricCard',
  position: { x: 0, y: 0, w: 3, h: 2 },
  props: { ...defaultProps, ...userProps }
};

gridManager.addComponent(component);
```

### 3. Render Phase
```typescript
// In Page Renderer
const ComponentClass = componentRegistry.get(component.componentType);
const element = (
  <ComponentClass
    {...component.props}
    className={calculateClasses(component.position)}
    style={calculateStyles(component.position)}
  />
);
```

## State Management

### Grid State Structure

```typescript
interface GridState {
  // Current grid configuration
  grid: BentoGrid;
  
  // Edit mode state
  selectedComponent?: string;
  isDragging: boolean;
  draggedComponent?: GridComponent;
  
  // Preview state
  previewDevice: DeviceType;
  showGrid: boolean;
  snapToGrid: boolean;
  
  // History for undo/redo
  history: BentoGrid[];
  historyIndex: number;
}
```

### Context Architecture

```typescript
// BentoProvider wraps the entire edit experience
<BentoProvider>
  <GridEditorProvider>
    <DragDropProvider>
      <GridEditor />
    </DragDropProvider>
  </GridEditorProvider>
</BentoProvider>
```

## Security Architecture

### 1. Page Level Security
```typescript
interface PageSecurity {
  requiredRoles?: string[];
  requiredPermissions?: string[];
  isPublic?: boolean;
}

// Checked at route level
const canAccessPage = (user: User, page: BentoPage): boolean => {
  if (page.bindings.security.isPublic) return true;
  
  const hasRole = page.bindings.security.requiredRoles?.some(
    role => user.roles.includes(role)
  );
  
  const hasPermission = page.bindings.security.requiredPermissions?.some(
    perm => user.permissions.includes(perm)
  );
  
  return hasRole || hasPermission;
};
```

### 2. Component Level Security
```typescript
interface ComponentSecurity {
  requiredPermissions?: string[];
  visibilityCondition?: string; // Expression evaluated at runtime
}

// Checked during render
const shouldRenderComponent = (
  user: User, 
  component: GridComponent
): boolean => {
  // Permission check
  if (component.bindings?.visibility?.requiredPermissions) {
    const hasPermission = /* check permissions */;
    if (!hasPermission) return false;
  }
  
  // Dynamic condition check
  if (component.bindings?.visibility?.condition) {
    return evaluateCondition(component.bindings.visibility.condition, {
      user,
      component
    });
  }
  
  return true;
};
```

## Performance Considerations

### 1. Component Optimization
- All components wrapped in `React.memo`
- Props comparison for re-render prevention
- Lazy loading for heavy components

### 2. Grid Optimization
- Virtual scrolling for large grids
- Viewport-based rendering
- Debounced resize calculations

### 3. Data Loading
- Component-level data fetching
- Parallel data loading
- Caching strategies

## Integration Points

### 1. With Existing Navigation
```typescript
// Extend NavigationItem
interface NavigationItem {
  id: string;
  label: string;
  href: string;
  pageId?: string; // Link to Bento page
  // ... existing fields
}
```

### 2. With Auth System
```typescript
// Use existing auth context
const { user, hasPermission } = useAuth();

// Apply to Bento pages
const accessiblePages = pages.filter(page => 
  canAccessPage(user, page)
);
```

### 3. With Theme System
```typescript
// Components use existing theme
const MyComponent = () => {
  const { theme } = useTheme();
  
  return (
    <div className="bg-background text-foreground">
      {/* Respects current theme */}
    </div>
  );
};
```

## Error Handling

### 1. Component Errors
```typescript
// Error boundary for each component
<ComponentErrorBoundary
  componentId={component.id}
  fallback={<ComponentErrorFallback />}
>
  <Component {...props} />
</ComponentErrorBoundary>
```

### 2. Grid Errors
```typescript
// Validation errors
class GridValidationError extends Error {
  constructor(
    public component: GridComponent,
    public reason: string
  ) {
    super(`Grid validation failed: ${reason}`);
  }
}
```

### 3. Storage Errors
```typescript
// ECS error handling
try {
  await dataContext.commit(pageComponent);
} catch (error) {
  if (error instanceof ConcurrencyError) {
    // Handle concurrent edits
  }
  throw error;
}
```

## Next Steps

1. Review [Grid System](./03-grid-system.md) for layout details
2. Explore [Data Models](./06-data-models.md) for type definitions
3. Check [Implementation Plan](./07-implementation-plan.md) for build phases
4. See [Component Registry](./04-component-registry.md) for component patterns