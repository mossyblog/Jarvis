/**
 * Bento Grid System Types
 * 
 * This module provides comprehensive TypeScript types for the Bento Grid System,
 * a flexible component-based layout system for React applications.
 * 
 * @module BentoTypes
 */

// ============================================================================
// Core Types
// ============================================================================

/** Unique identifier type used throughout the system */
export type ID = string;

/** ISO 8601 timestamp format */
export type Timestamp = string;

/** JSON string for serialized data */
export type JSONString = string;

/**
 * Device types for responsive design
 */
export enum DeviceType {
  Desktop = 'desktop',
  Tablet = 'tablet',
  Mobile = 'mobile'
}

/**
 * Component categories for organization and filtering
 */
export enum ComponentCategory {
  Analytics = 'Analytics',
  Data = 'Data',
  Status = 'Status',
  Navigation = 'Navigation',
  Forms = 'Forms',
  Layout = 'Layout',
  Media = 'Media',
  Custom = 'Custom'
}

/**
 * Page status for workflow management
 */
export enum PageStatus {
  Draft = 'draft',
  Published = 'published',
  Archived = 'archived',
  Scheduled = 'scheduled'
}

// ============================================================================
// Geometry Types
// ============================================================================

/**
 * Size definition in grid units
 */
export interface Size {
  /** Width in grid units */
  w: number;
  /** Height in grid units */
  h: number;
}

/**
 * Position in grid coordinates
 */
export interface GridPosition extends Size {
  /** X coordinate (column) */
  x: number;
  /** Y coordinate (row) */
  y: number;
}

/**
 * Pixel-based position for drag operations
 */
export interface Position {
  /** X position in pixels */
  x: number;
  /** Y position in pixels */
  y: number;
}

/**
 * Rectangle bounds for collision detection and positioning
 */
export interface Bounds {
  top: number;
  left: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
}

// ============================================================================
// Re-exports from specific modules
// ============================================================================

// Forward type exports to prevent circular dependencies
export type { BentoPage, PageBindings, SecurityBindings, SecurityRule, VisibilityBindings, VisibilityCondition, DataBindings } from './page.types';
export type { BentoLayout, LayoutSettings, SpacingConfig, BackgroundConfig, BreakpointConfig } from './layout.types';
export type { BentoGrid, GridSettings, GridZone, GridComponent, ComponentDisplay, AnimationConfig } from './grid.types';
export type { ComponentConfig, PropTypeDefinition, ComponentConstraints, ComponentVariant, ComponentExample, ResizeHandle } from './component.types';
export type { DataBindingConfig, DataField, DataFieldType, DataSourceType, DataSource, DataTransform, RefreshConfig, RefreshTrigger, CacheConfig, ComponentBindings, EventBinding, InteractionConfig, ComponentEvent, ComponentAction, PayloadDefinition, ActionParameter, StateConfig, StateField, StateUpdater } from './bindings.types';

// Component Binding Types for UIStudio API integration
export interface ComponentBinding {
  id: string;
  componentId: string;
  componentType: string;
  
  // ECS Configuration
  ecsComponent: string;
  ecsComponentConfig: ECSComponentConfig;
  
  // Field Mappings
  fieldMappings: FieldMapping[];
  
  // Read/Write Configuration
  readConfig: ReadConfig;
  writeConfig?: WriteConfig;
  
  // Deployment
  deploymentConfig: DeploymentConfig;
  
  // Metadata
  name: string;
  description?: string;
  version: string;
  status: 'draft' | 'testing' | 'deployed' | 'error';
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export interface ECSComponentConfig {
  name: string;
  displayName: string;
  description: string;
  category: string;
  version: string;
  documentation?: string;
  fields: ECSField[];
  permissions: {
    read: boolean;
    write: boolean;
    admin: boolean;
  };
}

export interface ECSField {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'date' | 'json' | 'array' | 'object';
  required: boolean;
  description?: string;
  defaultValue?: unknown;
  constraints?: {
    min?: number;
    max?: number;
    pattern?: string;
    enum?: string[];
  };
  metadata?: Record<string, unknown>;
}

export interface FieldMapping {
  ecsField: string;
  source: 'prop' | 'state' | 'computed';
  sourcePath: string;
  uiControl: UIControlType;
  transform?: string;
  validation?: FieldValidation;
  metadata?: {
    autoMapped?: boolean;
    confidence?: number;
    strategy?: string;
    [key: string]: unknown;
  };
}

export interface FieldValidation {
  required?: boolean;
  min?: number;
  max?: number;
  pattern?: string;
  custom?: string;
}

export type UIControlType = 
  | 'text' | 'number' | 'select' | 'multiselect' 
  | 'checkbox' | 'switch' | 'date' | 'datetime' 
  | 'textarea' | 'json' | 'code' | 'color' | 'file';

export interface ReadConfig {
  enabled: boolean;
  query: string;
  polling: {
    enabled: boolean;
    interval: number;
  };
  caching: {
    enabled: boolean;
    ttl: number;
  };
  errorHandling: {
    retries: number;
    fallback?: unknown;
  };
}

export interface WriteConfig {
  enabled: boolean;
  mutations: WriteMutation[];
  validation: {
    enabled: boolean;
    schema?: string;
  };
  confirmation: {
    required: boolean;
    message?: string;
  };
}

export interface WriteMutation {
  trigger: string;
  operation: 'create' | 'update' | 'delete';
  target: string;
  mapping: Record<string, string>;
}

export interface DeploymentConfig {
  environment: 'development' | 'staging' | 'production';
  autoRefresh: boolean;
  errorReporting: boolean;
  analytics: boolean;
  permissions: {
    public: boolean;
    roles: string[];
    users: string[];
  };
}

// ============================================================================
// Validation Types
// ============================================================================

/**
 * Validation rule configuration
 */
export interface ValidationRule {
  type: ValidationType;
  value?: unknown;
  message?: string;
  /** Custom validation function name or expression */
  validator?: string;
}

/**
 * Available validation types
 */
export type ValidationType =
  | 'required'
  | 'min'
  | 'max'
  | 'minLength'
  | 'maxLength'
  | 'pattern'
  | 'email'
  | 'url'
  | 'custom';

/**
 * Validation result with errors and warnings
 */
export interface ValidationResult {
  valid: boolean;
  errors: ValidationError[];
  warnings?: ValidationWarning[];
}

/**
 * Validation error details
 */
export interface ValidationError {
  field: string;
  message: string;
  code?: string;
  details?: unknown;
}

/**
 * Validation warning details
 */
export interface ValidationWarning {
  field: string;
  message: string;
  severity: 'low' | 'medium' | 'high';
}

// ============================================================================
// Export/Import Types
// ============================================================================

/**
 * Page export format for sharing and backup
 */
export interface PageExport {
  /** Export format version */
  version: string;
  timestamp: Timestamp;
  
  // Page data - using unknown for safer typing until fully imported
  page: unknown;
  layout: unknown;
  grids: unknown[];
  
  // Component information
  components: ComponentReference[];
  
  // Resources
  assets?: AssetReference[];
  
  // Metadata
  exportedBy: string;
  checksum?: string;
}

/**
 * Component reference for exports
 */
export interface ComponentReference {
  type: string;
  version?: string;
  source?: 'registry' | 'custom';
  definition?: Record<string, unknown>;
}

/**
 * Asset reference for exports
 */
export interface AssetReference {
  id: ID;
  type: 'image' | 'video' | 'font' | 'other';
  url: string;
  size: number;
  checksum: string;
}

// ============================================================================
// Storage Types (ECS Components)
// ============================================================================

/**
 * Base interface for Jarvis ECS components
 * These interfaces define how Bento data is stored in the database
 */
export interface IComponent {
  id: string;
  OwnerEntityId: string;
  LastUpdated: string;
}

/**
 * Page storage component for ECS
 */
export interface BentoPageComponent extends IComponent {
  displayName: string;
  route: string;
  layoutId: string;
  status: string;
  version: number;
  
  // JSON serialized fields
  securityBindings: string;
  visibilityBindings: string;
  dataBindings?: string;
  metadata?: string;
  tags?: string;
  
  // Audit fields
  createdBy: string;
  updatedBy: string;
}

/**
 * Layout storage component for ECS
 */
export interface BentoLayoutComponent extends IComponent {
  name: string;
  description?: string;
  category?: string;
  
  // Grid references
  desktopGridId: string;
  tabletGridId?: string;
  mobileGridId?: string;
  
  // JSON serialized fields
  settings: string;
  
  // Flags
  isDefault?: boolean;
  
  // Preview
  thumbnail?: string;
  preview?: string;
}

/**
 * Grid storage component for ECS
 */
export interface BentoGridComponent extends IComponent {
  name: string;
  device: string;
  layoutId: string;
  
  // Grid configuration
  columns: number;
  rows?: number;
  gap: number;
  rowHeight?: number;
  
  // JSON serialized fields
  components: string; // GridComponent[]
  settings: string; // GridSettings
  zones?: string; // GridZone[]
}

// ============================================================================
// Utility Types
// ============================================================================

/**
 * Deep partial type for configuration objects
 */
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

/**
 * Nullable type wrapper
 */
export type Nullable<T> = T | null;

/**
 * Make specific keys optional
 */
export type Optional<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard for GridComponent validation
 */
export const isGridComponent = (obj: unknown): obj is { id: string; componentType: string; position: GridPosition } => {
  if (!obj || typeof obj !== 'object') return false;
  
  const candidate = obj as Record<string, unknown>;
  
  return typeof candidate.id === 'string' &&
    typeof candidate.componentType === 'string' &&
    Boolean(candidate.position) &&
    typeof candidate.position === 'object' &&
    candidate.position !== null &&
    typeof (candidate.position as Record<string, unknown>).x === 'number' &&
    typeof (candidate.position as Record<string, unknown>).y === 'number' &&
    typeof (candidate.position as Record<string, unknown>).w === 'number' &&
    typeof (candidate.position as Record<string, unknown>).h === 'number';
};

/**
 * Type guard for DeviceType validation
 */
export const isValidDeviceType = (value: unknown): value is DeviceType => {
  return Object.values(DeviceType).includes(value as DeviceType);
};

/**
 * Type guard for ComponentCategory validation
 */
export const isValidComponentCategory = (value: unknown): value is ComponentCategory => {
  return Object.values(ComponentCategory).includes(value as ComponentCategory);
};

/**
 * Type guard for PageStatus validation
 */
export const isValidPageStatus = (value: unknown): value is PageStatus => {
  return Object.values(PageStatus).includes(value as PageStatus);
};

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Create a default grid position
 */
export const createDefaultGridPosition = (): GridPosition => ({
  x: 0,
  y: 0,
  w: 2,
  h: 2
});

/**
 * Create a default page configuration
 */
export const createDefaultPage = (displayName: string, route: string): {
  displayName: string;
  route: string;
  status: PageStatus;
  bindings: {
    security: { isPublic: boolean };
    visibility: { showInNavigation: boolean };
  };
} => ({
  displayName,
  route,
  status: PageStatus.Draft,
  bindings: {
    security: { isPublic: false },
    visibility: { showInNavigation: true }
  }
});

/**
 * Create a default component constraints object
 */
export const createDefaultConstraints = (): {
  minSize: { w: number; h: number };
  maxSize: { w: number; h: number };
  defaultSize: { w: number; h: number };
  resizable: {
    horizontal: boolean;
    vertical: boolean;
  };
} => ({
  minSize: { w: 1, h: 1 },
  maxSize: { w: 12, h: 12 },
  defaultSize: { w: 2, h: 2 },
  resizable: {
    horizontal: true,
    vertical: true
  }
});

// ============================================================================
// Constants
// ============================================================================

/**
 * Default grid configuration values
 */
export const GRID_DEFAULTS = {
  COLUMNS: 12,
  GAP: 16,
  ROW_HEIGHT: 100,
  MIN_COMPONENT_SIZE: { w: 1, h: 1 },
  MAX_COMPONENT_SIZE: { w: 12, h: 12 }
} as const;

/**
 * Default breakpoints for responsive design
 */
export const DEFAULT_BREAKPOINTS = {
  desktop: 1200,
  tablet: 768,
  mobile: 480
} as const;

/**
 * Component resize handles
 */
export const RESIZE_HANDLES = ['n', 'ne', 'e', 'se', 's', 'sw', 'w', 'nw'] as const;