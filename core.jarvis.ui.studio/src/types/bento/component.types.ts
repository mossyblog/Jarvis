/**
 * Bento Component Types
 * 
 * Type definitions for component registry, configuration, constraints,
 * and metadata used by the Bento Grid System.
 * 
 * @module BentoComponentTypes
 */

// Core types defined locally to avoid circular imports
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

enum DeviceType {
  Desktop = 'desktop',
  Tablet = 'tablet',
  Mobile = 'mobile'
}

interface Size {
  w: number;
  h: number;
}
import type { DataBindingConfig, InteractionConfig } from './bindings.types';
import * as React from 'react';

// ============================================================================
// Core Component Configuration Types
// ============================================================================

/**
 * Complete component configuration for the registry
 * 
 * Defines everything needed to register and use a component
 * within the Bento Grid System.
 */
export interface ComponentConfig {
  // React component reference
  /** The actual React component to render */
  component: React.ComponentType<Record<string, unknown>>;
  
  // Metadata
  /** Display name shown in the component palette */
  displayName: string;
  /** Optional description of the component's purpose */
  description?: string;
  /** Category for organizing components in the palette */
  category: ComponentCategory;
  /** Icon identifier or React component for visual representation */
  icon: string | React.ComponentType;
  /** Tags for search and filtering */
  tags?: string[];
  
  // Configuration
  /** Default properties applied when component is added to grid */
  defaultProps?: Record<string, unknown>;
  /** Property type definitions for validation and editing */
  propTypes?: PropTypeDefinition[];
  
  // Constraints
  /** Size and behavior constraints for the component */
  constraints: ComponentConstraints;
  
  // Advanced features
  /** Data binding configuration if component supports dynamic data */
  dataBinding?: DataBindingConfig;
  /** Interaction configuration for events and actions */
  interactions?: InteractionConfig;
  /** Available component variants */
  variants?: ComponentVariant[];
  
  // Documentation
  /** Usage examples for the component */
  examples?: ComponentExample[];
  /** Preview component or thumbnail for the palette */
  preview?: string | React.ComponentType;
}

// ============================================================================
// Property Type Definitions
// ============================================================================

/**
 * Property type definition for component props
 * 
 * Used for validation, editing UI generation, and documentation.
 */
export interface PropTypeDefinition {
  /** Property name */
  name: string;
  /** TypeScript type of the property */
  type: 'string' | 'number' | 'boolean' | 'object' | 'array' | 'function';
  /** Whether the property is required */
  required?: boolean;
  /** Default value if not provided */
  defaultValue?: unknown;
  /** Human-readable description */
  description?: string;
  /** Available options for enum-like properties */
  options?: unknown[];
  /** Validation rules for the property */
  validation?: PropertyValidation;
  /** UI configuration for property editing */
  editor?: PropertyEditor;
}

/**
 * Property validation configuration
 */
export interface PropertyValidation {
  /** Minimum value for numbers */
  min?: number;
  /** Maximum value for numbers */
  max?: number;
  /** Minimum length for strings/arrays */
  minLength?: number;
  /** Maximum length for strings/arrays */
  maxLength?: number;
  /** Regular expression pattern for strings */
  pattern?: string;
  /** Custom validation function */
  validator?: (value: unknown) => boolean | string;
}

/**
 * Property editor configuration for UI generation
 */
export interface PropertyEditor {
  /** Type of editor to use */
  type: 'text' | 'number' | 'select' | 'multiselect' | 'boolean' | 'color' | 'date' | 'textarea' | 'code';
  /** Additional editor-specific options */
  options?: Record<string, unknown>;
  /** Whether the editor should be inline or in a modal */
  inline?: boolean;
  /** Placeholder text */
  placeholder?: string;
  /** Help text */
  helpText?: string;
}

// ============================================================================
// Component Constraints
// ============================================================================

/**
 * Size and behavior constraints for components
 * 
 * Defines how components can be sized, positioned, and behave
 * within the grid system.
 */
export interface ComponentConstraints {
  // Size constraints
  /** Minimum size the component can be */
  minSize: Size;
  /** Maximum size the component can be */
  maxSize: Size;
  /** Default size when component is first added */
  defaultSize?: Size;
  
  // Aspect ratio
  /** Fixed aspect ratio (width/height) */
  aspectRatio?: number;
  /** Whether to maintain aspect ratio during resize */
  maintainAspectRatio?: boolean;
  
  // Resize behavior
  /** Resize configuration */
  resizable?: {
    /** Can be resized horizontally */
    horizontal: boolean;
    /** Can be resized vertically */
    vertical: boolean;
    /** Which resize handles to show */
    handles?: ResizeHandle[];
  };
  
  // Position constraints
  /** Whether component can be moved */
  movable?: boolean;
  /** Grid zones where component can be placed */
  allowedZones?: string[];
  /** Components that this component cannot overlap with */
  avoidOverlap?: string[];
  
  // Responsive constraints
  /** Device-specific constraint overrides */
  responsive?: {
    [K in DeviceType]?: Partial<ComponentConstraints>;
  };
}

/**
 * Available resize handles for components
 */
export type ResizeHandle = 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w' | 'nw';

// ============================================================================
// Component Variants
// ============================================================================

/**
 * Component variant definition
 * 
 * Allows a single component to have multiple pre-configured versions
 * with different default props or constraints.
 */
export interface ComponentVariant {
  /** Variant identifier */
  name: string;
  /** Display name for the variant */
  displayName: string;
  /** Optional description of the variant */
  description?: string;
  /** Icon for the variant (inherits from component if not specified) */
  icon?: string;
  
  // Override configuration
  /** Constraint overrides for this variant */
  constraints?: Partial<ComponentConstraints>;
  /** Default props overrides for this variant */
  defaultProps?: Record<string, unknown>;
  /** Property type overrides for this variant */
  propTypes?: PropTypeDefinition[];
  
  // Custom render (optional)
  /** Custom render function for this variant */
  render?: (props: Record<string, unknown>) => React.ReactElement;
  
  // Conditions
  /** When this variant should be suggested or auto-selected */
  conditions?: VariantCondition[];
}

/**
 * Condition for when a variant should be used
 */
export interface VariantCondition {
  /** Type of condition */
  type: 'device' | 'zone' | 'prop' | 'context';
  /** Condition expression or value */
  value: unknown;
  /** Operator for comparison */
  operator?: 'equals' | 'contains' | 'greaterThan' | 'lessThan';
}

// ============================================================================
// Component Examples
// ============================================================================

/**
 * Component usage example
 * 
 * Provides documentation and examples for how to use components.
 */
export interface ComponentExample {
  /** Example title */
  title: string;
  /** Optional description */
  description?: string;
  /** Props configuration for the example */
  props: Record<string, unknown>;
  /** Source code for the example */
  code?: string;
  /** Screenshot or visual representation */
  screenshot?: string;
  /** Category or group for organizing examples */
  category?: string;
}

// ============================================================================
// Registry Types
// ============================================================================

/**
 * Component registry interface
 * 
 * Central registry for managing all available components.
 */
export interface ComponentRegistry {
  /** Register a new component */
  register(key: string, config: ComponentConfig): void;
  /** Get a component configuration by key */
  get(key: string): ComponentConfig | undefined;
  /** Get all components in a category */
  getByCategory(category: ComponentCategory): Record<string, ComponentConfig>;
  /** Get all registered components */
  getAll(): Record<string, ComponentConfig>;
  /** Check if a component is registered */
  has(key: string): boolean;
  /** Unregister a component */
  unregister(key: string): boolean;
  /** Get component keys matching search criteria */
  search(query: string, categories?: ComponentCategory[]): string[];
}

/**
 * Component registry entry with metadata
 */
export interface RegistryEntry {
  /** Component key */
  key: string;
  /** Component configuration */
  config: ComponentConfig;
  /** Registration timestamp */
  registeredAt: Date;
  /** Usage statistics */
  usage?: ComponentUsage;
}

/**
 * Component usage statistics
 */
export interface ComponentUsage {
  /** Total number of times component has been used */
  totalUsage: number;
  /** Last time component was used */
  lastUsed?: Date;
  /** Most common props configurations */
  commonProps?: Array<{
    props: Record<string, unknown>;
    count: number;
  }>;
}

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Create default component constraints
 */
export const createDefaultConstraints = (): ComponentConstraints => ({
  minSize: { w: 1, h: 1 },
  maxSize: { w: 12, h: 12 },
  defaultSize: { w: 2, h: 2 },
  resizable: {
    horizontal: true,
    vertical: true,
    handles: ['se'] // Only southeast handle by default
  },
  movable: true
});

/**
 * Create a new component configuration
 */
export const createComponentConfig = (
  component: React.ComponentType<Record<string, unknown>>,
  displayName: string,
  category: ComponentCategory,
  icon: string | React.ComponentType,
  overrides: Partial<ComponentConfig> = {}
): ComponentConfig => ({
  component,
  displayName,
  category,
  icon,
  constraints: createDefaultConstraints(),
  ...overrides
});

/**
 * Create a property type definition
 */
export const createPropType = (
  name: string,
  type: PropTypeDefinition['type'],
  overrides: Partial<PropTypeDefinition> = {}
): PropTypeDefinition => ({
  name,
  type,
  required: false,
  ...overrides
});

/**
 * Create a component variant
 */
export const createVariant = (
  name: string,
  displayName: string,
  overrides: Partial<ComponentVariant> = {}
): ComponentVariant => ({
  name,
  displayName,
  ...overrides
});

/**
 * Create a component example
 */
export const createExample = (
  title: string,
  props: Record<string, unknown>,
  overrides: Partial<ComponentExample> = {}
): ComponentExample => ({
  title,
  props,
  ...overrides
});

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard to check if an object is a valid ComponentConfig
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isComponentConfig = (obj: any): obj is ComponentConfig => {
  return obj &&
    typeof obj.component === 'function' &&
    typeof obj.displayName === 'string' &&
    typeof obj.category === 'string' &&
    (typeof obj.icon === 'string' || typeof obj.icon === 'function') &&
    obj.constraints &&
    typeof obj.constraints === 'object';
};

/**
 * Type guard to check if an object has valid component constraints
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidConstraints = (obj: any): obj is ComponentConstraints => {
  return obj &&
    obj.minSize &&
    typeof obj.minSize.w === 'number' &&
    typeof obj.minSize.h === 'number' &&
    obj.maxSize &&
    typeof obj.maxSize.w === 'number' &&
    typeof obj.maxSize.h === 'number';
};

/**
 * Type guard to check if an object is a valid PropTypeDefinition
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isPropTypeDefinition = (obj: any): obj is PropTypeDefinition => {
  return obj &&
    typeof obj.name === 'string' &&
    ['string', 'number', 'boolean', 'object', 'array', 'function'].includes(obj.type);
};

/**
 * Type guard to check if an object is a valid ComponentVariant
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isComponentVariant = (obj: any): obj is ComponentVariant => {
  return obj &&
    typeof obj.name === 'string' &&
    typeof obj.displayName === 'string';
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Check if a component can be resized in a given direction
 */
export const canResize = (
  constraints: ComponentConstraints,
  direction: 'horizontal' | 'vertical'
): boolean => {
  if (!constraints.resizable) {
    return false;
  }
  
  return direction === 'horizontal' 
    ? constraints.resizable.horizontal 
    : constraints.resizable.vertical;
};

/**
 * Check if a component size is within constraints
 */
export const isValidSize = (size: Size, constraints: ComponentConstraints): boolean => {
  return (
    size.w >= constraints.minSize.w &&
    size.w <= constraints.maxSize.w &&
    size.h >= constraints.minSize.h &&
    size.h <= constraints.maxSize.h
  );
};

/**
 * Clamp a size to fit within constraints
 */
export const clampSize = (size: Size, constraints: ComponentConstraints): Size => {
  let { w, h } = size;
  
  // Clamp to min/max bounds
  w = Math.max(constraints.minSize.w, Math.min(constraints.maxSize.w, w));
  h = Math.max(constraints.minSize.h, Math.min(constraints.maxSize.h, h));
  
  // Apply aspect ratio if specified
  if (constraints.aspectRatio && constraints.maintainAspectRatio) {
    const currentRatio = w / h;
    if (Math.abs(currentRatio - constraints.aspectRatio) > 0.01) {
      // Adjust height to maintain aspect ratio
      h = Math.round(w / constraints.aspectRatio);
      
      // Ensure height is still within bounds
      if (h < constraints.minSize.h) {
        h = constraints.minSize.h;
        w = Math.round(h * constraints.aspectRatio);
      } else if (h > constraints.maxSize.h) {
        h = constraints.maxSize.h;
        w = Math.round(h * constraints.aspectRatio);
      }
    }
  }
  
  return { w, h };
};

/**
 * Get constraints for a specific device type
 */
export const getConstraintsForDevice = (
  constraints: ComponentConstraints,
  device: DeviceType
): ComponentConstraints => {
  const deviceConstraints = constraints.responsive?.[device];
  
  if (!deviceConstraints) {
    return constraints;
  }
  
  return {
    ...constraints,
    ...deviceConstraints,
    minSize: { ...constraints.minSize, ...deviceConstraints.minSize },
    maxSize: { ...constraints.maxSize, ...deviceConstraints.maxSize },
    defaultSize: deviceConstraints.defaultSize || constraints.defaultSize,
    resizable: deviceConstraints.resizable ? {
      ...constraints.resizable,
      ...deviceConstraints.resizable
    } : constraints.resizable
  };
};

/**
 * Merge two component configurations
 */
export const mergeComponentConfigs = (
  base: ComponentConfig,
  override: Partial<ComponentConfig>
): ComponentConfig => {
  return {
    ...base,
    ...override,
    defaultProps: {
      ...base.defaultProps,
      ...override.defaultProps
    },
    constraints: {
      ...base.constraints,
      ...override.constraints,
      minSize: { ...base.constraints.minSize, ...override.constraints?.minSize },
      maxSize: { ...base.constraints.maxSize, ...override.constraints?.maxSize },
      resizable: override.constraints?.resizable ? {
        ...base.constraints.resizable,
        ...override.constraints.resizable
      } : base.constraints.resizable
    },
    propTypes: override.propTypes || base.propTypes,
    tags: override.tags || base.tags,
    variants: override.variants || base.variants,
    examples: override.examples || base.examples
  };
};

/**
 * Filter components by search query
 */
export const filterComponents = (
  components: Record<string, ComponentConfig>,
  query: string,
  categories?: ComponentCategory[]
): Record<string, ComponentConfig> => {
  const filtered: Record<string, ComponentConfig> = {};
  const lowercaseQuery = query.toLowerCase();
  
  Object.entries(components).forEach(([key, config]) => {
    // Category filter
    if (categories && categories.length > 0 && !categories.includes(config.category)) {
      return;
    }
    
    // Search query filter
    if (query) {
      const matchesName = config.displayName.toLowerCase().includes(lowercaseQuery);
      const matchesDescription = config.description?.toLowerCase().includes(lowercaseQuery);
      const matchesTags = config.tags?.some(tag => tag.toLowerCase().includes(lowercaseQuery));
      const matchesKey = key.toLowerCase().includes(lowercaseQuery);
      
      if (!matchesName && !matchesDescription && !matchesTags && !matchesKey) {
        return;
      }
    }
    
    filtered[key] = config;
  });
  
  return filtered;
};