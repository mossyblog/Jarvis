# Data Models

## Overview

This document defines all TypeScript interfaces and data structures used in the Bento Grid System. These models ensure type safety and provide a clear contract for all system components.

## Core Types

### Basic Types

```typescript
// Primitive types used throughout the system
type ID = string;
type Timestamp = string; // ISO 8601 format
type JSONString = string;

// Device types for responsive design
enum DeviceType {
  Desktop = 'desktop',
  Tablet = 'tablet',
  Mobile = 'mobile'
}

// Component categories for organization
enum ComponentCategory {
  Analytics = 'Analytics',
  Data = 'Data',
  Status = 'Status',
  Navigation = 'Navigation',
  Forms = 'Forms',
  Layout = 'Layout',
  Media = 'Media',
  Custom = 'Custom'
}

// Page status for workflow management
enum PageStatus {
  Draft = 'draft',
  Published = 'published',
  Archived = 'archived',
  Scheduled = 'scheduled'
}
```

### Geometry Types

```typescript
// Size definition
interface Size {
  w: number; // Width in grid units
  h: number; // Height in grid units
}

// Position in grid
interface GridPosition extends Size {
  x: number; // X coordinate (column)
  y: number; // Y coordinate (row)
}

// Pixel-based position (for drag operations)
interface Position {
  x: number; // X in pixels
  y: number; // Y in pixels
}

// Rectangle bounds
interface Bounds {
  top: number;
  left: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
}
```

## Page Models

### BentoPage

```typescript
interface BentoPage {
  // Identification
  id: ID;
  displayName: string;
  description?: string;
  
  // Routing
  route: string;
  
  // Layout reference
  layoutId: ID;
  
  // Configuration
  bindings: PageBindings;
  
  // Metadata
  status: PageStatus;
  version: number;
  createdAt: Timestamp;
  updatedAt: Timestamp;
  createdBy: ID;
  updatedBy: ID;
  
  // Additional data
  metadata?: Record<string, any>;
  tags?: string[];
}

interface PageBindings {
  // Security configuration
  security: SecurityBindings;
  
  // Display configuration
  visibility: VisibilityBindings;
  
  // Data configuration
  data?: DataBindings;
  
  // Custom bindings
  custom?: Record<string, any>;
}

interface SecurityBindings {
  // Access control
  isPublic?: boolean;
  requiredRoles?: string[];
  requiredPermissions?: string[];
  
  // Advanced rules
  customRules?: SecurityRule[];
}

interface SecurityRule {
  id: ID;
  type: 'expression' | 'function';
  rule: string; // Expression or function name
  parameters?: Record<string, any>;
}

interface VisibilityBindings {
  // Navigation display
  showInNavigation: boolean;
  navigationOrder?: number;
  navigationGroup?: string;
  icon?: string;
  
  // Display conditions
  conditions?: VisibilityCondition[];
}

interface VisibilityCondition {
  field: string;
  operator: 'equals' | 'notEquals' | 'contains' | 'greater' | 'less';
  value: any;
}

interface DataBindings {
  // Global data sources for the page
  sources: DataSource[];
  
  // Refresh configuration
  refreshInterval?: number;
  refreshOnFocus?: boolean;
  
  // Data transformations
  transforms?: DataTransform[];
}
```

## Layout Models

### BentoLayout

```typescript
interface BentoLayout {
  // Identification
  id: ID;
  name: string;
  description?: string;
  category?: 'standard' | 'custom' | 'template';
  
  // Grid configurations per device
  grids: {
    desktop: ID;     // Required
    tablet?: ID;     // Optional, falls back to desktop
    mobile?: ID;     // Optional, falls back to tablet or desktop
  };
  
  // Layout settings
  settings: LayoutSettings;
  
  // Metadata
  isDefault?: boolean;
  createdAt: Timestamp;
  updatedAt: Timestamp;
  
  // Preview
  thumbnail?: string;
  preview?: string;
}

interface LayoutSettings {
  // Grid behavior
  containerWidth?: 'fixed' | 'fluid' | 'full';
  maxWidth?: number;
  
  // Spacing
  padding?: SpacingConfig;
  margin?: SpacingConfig;
  
  // Background
  background?: BackgroundConfig;
  
  // Responsive behavior
  breakpoints?: BreakpointConfig;
}

interface SpacingConfig {
  top?: number;
  right?: number;
  bottom?: number;
  left?: number;
  unit?: 'px' | 'rem' | 'em' | '%';
}

interface BackgroundConfig {
  color?: string;
  image?: string;
  repeat?: 'repeat' | 'no-repeat' | 'repeat-x' | 'repeat-y';
  position?: string;
  size?: 'cover' | 'contain' | 'auto';
}

interface BreakpointConfig {
  desktop?: number;
  tablet?: number;
  mobile?: number;
  custom?: Record<string, number>;
}
```

## Grid Models

### BentoGrid

```typescript
interface BentoGrid {
  // Identification
  id: ID;
  name: string;
  device: DeviceType;
  
  // Grid configuration
  columns: number;
  rows?: number; // Optional, auto-expand if not set
  gap: number; // Gap between components in pixels
  rowHeight?: number; // Height of each row in pixels
  
  // Components in the grid
  components: GridComponent[];
  
  // Grid settings
  settings: GridSettings;
  
  // Metadata
  createdAt: Timestamp;
  updatedAt: Timestamp;
}

interface GridSettings {
  // Visual settings
  showGrid?: boolean;
  snapToGrid?: boolean;
  gridColor?: string;
  
  // Behavior
  allowOverlap?: boolean; // Default: false
  compactMode?: 'none' | 'vertical' | 'horizontal';
  
  // Constraints
  minColumns?: number;
  maxColumns?: number;
  
  // Zones (optional)
  zones?: GridZone[];
}

interface GridZone {
  id: ID;
  name: string;
  bounds: GridPosition;
  type: 'header' | 'sidebar' | 'content' | 'footer' | 'custom';
  
  // Zone constraints
  locked?: boolean; // Prevent modifications
  accepts?: string[]; // Component types allowed
  maxComponents?: number;
  
  // Styling
  className?: string;
  style?: React.CSSProperties;
}
```

## Component Models

### GridComponent

```typescript
interface GridComponent {
  // Identification
  id: ID;
  componentType: string; // Registry key
  
  // Position and size
  position: GridPosition;
  
  // Component configuration
  props?: Record<string, any>;
  
  // Bindings
  bindings?: ComponentBindings;
  
  // Display settings
  display?: ComponentDisplay;
  
  // Metadata
  locked?: boolean; // Prevent modifications
  hidden?: boolean; // Conditionally hidden
}

interface ComponentBindings {
  // Data binding
  dataSource?: string;
  dataPath?: string;
  dataTransform?: string;
  
  // Event bindings
  events?: EventBinding[];
  
  // Visibility rules
  visibility?: {
    condition?: string; // Expression
    requiredPermissions?: string[];
    requiredRoles?: string[];
  };
  
  // Update configuration
  refreshInterval?: number;
  refreshTriggers?: string[];
}

interface EventBinding {
  event: string; // Component event name
  action: string; // Action to perform
  target?: string; // Target component ID
  parameters?: Record<string, any>;
}

interface ComponentDisplay {
  // Custom styling
  className?: string;
  style?: React.CSSProperties;
  
  // Responsive visibility
  hideOn?: DeviceType[];
  showOnly?: DeviceType[];
  
  // Animation
  animation?: AnimationConfig;
}

interface AnimationConfig {
  enter?: string; // CSS class or animation name
  exit?: string;
  duration?: number;
  delay?: number;
}
```

### Component Registry Models

```typescript
interface ComponentConfig {
  // React component reference
  component: React.ComponentType<any>;
  
  // Metadata
  displayName: string;
  description?: string;
  category: ComponentCategory;
  icon: string | React.ComponentType;
  tags?: string[];
  
  // Configuration
  defaultProps?: Record<string, any>;
  propTypes?: PropTypeDefinition[];
  
  // Constraints
  constraints: ComponentConstraints;
  
  // Advanced features
  dataBinding?: DataBindingConfig;
  interactions?: InteractionConfig;
  variants?: ComponentVariant[];
  
  // Documentation
  examples?: ComponentExample[];
  preview?: string | React.ComponentType;
}

interface PropTypeDefinition {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'object' | 'array' | 'function';
  required?: boolean;
  defaultValue?: any;
  description?: string;
  options?: any[]; // For enum-like props
}

interface ComponentConstraints {
  // Size constraints
  minSize: Size;
  maxSize: Size;
  defaultSize?: Size;
  
  // Aspect ratio
  aspectRatio?: number;
  maintainAspectRatio?: boolean;
  
  // Resize behavior
  resizable?: {
    horizontal: boolean;
    vertical: boolean;
    handles?: ResizeHandle[];
  };
  
  // Responsive constraints
  responsive?: {
    [K in DeviceType]?: Partial<ComponentConstraints>;
  };
}

type ResizeHandle = 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w' | 'nw';

interface ComponentVariant {
  name: string;
  displayName: string;
  description?: string;
  icon?: string;
  
  // Override configuration
  constraints?: Partial<ComponentConstraints>;
  defaultProps?: Record<string, any>;
  
  // Custom render (optional)
  render?: (props: any) => React.ReactElement;
}

interface ComponentExample {
  title: string;
  description?: string;
  props: Record<string, any>;
  code?: string;
}
```

## Data Binding Models

```typescript
interface DataBindingConfig {
  // Available fields for binding
  fields: DataField[];
  
  // Refresh configuration
  refresh?: RefreshConfig;
  
  // Dependencies
  dependencies?: string[];
}

interface DataField {
  name: string; // Prop name
  type: DataFieldType;
  required?: boolean;
  
  // Data source
  source: DataSourceType;
  
  // Source-specific configuration
  path?: string; // For API/GraphQL
  query?: string; // For GraphQL/SQL
  compute?: string; // Expression for computed values
  
  // Transformation
  transform?: DataTransform;
  
  // Validation
  validation?: ValidationRule[];
}

type DataFieldType = 
  | 'string' 
  | 'number' 
  | 'boolean' 
  | 'date' 
  | 'array' 
  | 'object' 
  | 'any';

type DataSourceType = 
  | 'static' 
  | 'api' 
  | 'graphql' 
  | 'database' 
  | 'computed' 
  | 'context';

interface DataSource {
  id: ID;
  name: string;
  type: DataSourceType;
  
  // Connection details
  endpoint?: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  headers?: Record<string, string>;
  
  // Query/Parameters
  query?: string;
  parameters?: Record<string, any>;
  
  // Caching
  cache?: CacheConfig;
}

interface DataTransform {
  type: 'map' | 'filter' | 'reduce' | 'custom';
  expression: string; // JavaScript expression
  parameters?: Record<string, any>;
}

interface RefreshConfig {
  // Automatic refresh
  interval?: number; // milliseconds
  
  // Conditional refresh
  triggers?: RefreshTrigger[];
  
  // Refresh behavior
  debounce?: number;
  throttle?: number;
}

interface RefreshTrigger {
  type: 'event' | 'dependency' | 'visibility' | 'custom';
  source: string;
  condition?: string;
}

interface CacheConfig {
  enabled: boolean;
  ttl?: number; // Time to live in seconds
  strategy?: 'memory' | 'localStorage' | 'sessionStorage';
  key?: string;
}
```

## Interaction Models

```typescript
interface InteractionConfig {
  // Supported events
  events: ComponentEvent[];
  
  // Available actions
  actions: ComponentAction[];
  
  // State management
  state?: StateConfig;
}

interface ComponentEvent {
  name: string;
  description?: string;
  payload?: PayloadDefinition;
  
  // Event metadata
  bubbles?: boolean;
  cancelable?: boolean;
}

interface ComponentAction {
  name: string;
  description?: string;
  
  // Parameters
  parameters?: ActionParameter[];
  
  // Handler
  handler: string; // Function name or inline function
  
  // Validation
  validate?: string; // Validation expression
}

interface PayloadDefinition {
  [key: string]: {
    type: DataFieldType;
    required?: boolean;
    description?: string;
  };
}

interface ActionParameter {
  name: string;
  type: DataFieldType;
  required?: boolean;
  defaultValue?: any;
  description?: string;
}

interface StateConfig {
  // Local component state
  fields: StateField[];
  
  // State persistence
  persist?: {
    enabled: boolean;
    key: string;
    storage: 'memory' | 'localStorage' | 'sessionStorage';
  };
}

interface StateField {
  name: string;
  type: DataFieldType;
  defaultValue?: any;
  
  // State updates
  updaters?: StateUpdater[];
}

interface StateUpdater {
  name: string;
  type: 'set' | 'toggle' | 'increment' | 'custom';
  handler?: string; // For custom updaters
}
```

## Storage Models (ECS Components)

```typescript
// Jarvis ECS component interfaces
interface BentoPageComponent extends IComponent {
  displayName: string;
  route: string;
  layoutId: string;
  status: string;
  version: number;
  
  // JSON fields
  securityBindings: string;
  visibilityBindings: string;
  dataBindings?: string;
  metadata?: string;
  tags?: string;
  
  // Audit fields
  createdBy: string;
  updatedBy: string;
}

interface BentoLayoutComponent extends IComponent {
  name: string;
  description?: string;
  category?: string;
  
  // Grid references
  desktopGridId: string;
  tabletGridId?: string;
  mobileGridId?: string;
  
  // JSON fields
  settings: string;
  
  // Flags
  isDefault?: boolean;
  
  // Preview
  thumbnail?: string;
  preview?: string;
}

interface BentoGridComponent extends IComponent {
  name: string;
  device: string;
  layoutId: string;
  
  // Grid configuration
  columns: number;
  rows?: number;
  gap: number;
  rowHeight?: number;
  
  // JSON fields
  components: string; // GridComponent[]
  settings: string; // GridSettings
  zones?: string; // GridZone[]
}
```

## Validation Models

```typescript
interface ValidationRule {
  type: ValidationType;
  value?: any;
  message?: string;
  
  // Custom validation
  validator?: string; // Function name or expression
}

type ValidationType =
  | 'required'
  | 'min'
  | 'max'
  | 'minLength'
  | 'maxLength'
  | 'pattern'
  | 'email'
  | 'url'
  | 'custom';

interface ValidationResult {
  valid: boolean;
  errors: ValidationError[];
  warnings?: ValidationWarning[];
}

interface ValidationError {
  field: string;
  message: string;
  code?: string;
  details?: any;
}

interface ValidationWarning {
  field: string;
  message: string;
  severity: 'low' | 'medium' | 'high';
}
```

## Export/Import Models

```typescript
interface PageExport {
  version: string; // Export format version
  timestamp: Timestamp;
  
  // Page data
  page: BentoPage;
  layout: BentoLayout;
  grids: BentoGrid[];
  
  // Component information
  components: ComponentReference[];
  
  // Resources
  assets?: AssetReference[];
  
  // Metadata
  exportedBy: string;
  checksum?: string;
}

interface ComponentReference {
  type: string;
  version?: string;
  source?: 'registry' | 'custom';
  definition?: ComponentConfig; // For custom components
}

interface AssetReference {
  id: ID;
  type: 'image' | 'video' | 'font' | 'other';
  url: string;
  size: number;
  checksum: string;
}
```

## Type Guards and Utilities

```typescript
// Type guards
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

export const isValidDeviceType = (value: any): value is DeviceType => {
  return Object.values(DeviceType).includes(value);
};

// Utility types
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

export type Nullable<T> = T | null;

export type Optional<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

// Factory functions
export const createDefaultGridPosition = (): GridPosition => ({
  x: 0,
  y: 0,
  w: 2,
  h: 2
});

export const createDefaultPage = (displayName: string, route: string): Partial<BentoPage> => ({
  displayName,
  route,
  status: PageStatus.Draft,
  bindings: {
    security: { isPublic: false },
    visibility: { showInNavigation: true }
  }
});
```

## Next Steps

1. Review [Architecture](./02-architecture.md) for system design
2. Check [Implementation Plan](./07-implementation-plan.md) for development phases
3. See [Component API](./09-component-api.md) for API reference
4. Explore [Testing Strategy](./08-testing-strategy.md) for testing approaches