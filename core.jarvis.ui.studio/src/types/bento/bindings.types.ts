/**
 * Bento Bindings Types
 * 
 * Type definitions for data binding, event handling, interactions,
 * and state management within the Bento Grid System.
 * 
 * @module BentoBindingsTypes
 */

import type { ID } from './index';

// ============================================================================
// Data Binding Types
// ============================================================================

/**
 * Data binding configuration for components
 * 
 * Defines how components can connect to data sources and update dynamically.
 */
export interface DataBindingConfig {
  /** Available fields that can be bound to data */
  fields: DataField[];
  /** Refresh configuration for data updates */
  refresh?: RefreshConfig;
  /** Dependencies that trigger data refreshes */
  dependencies?: string[];
}

/**
 * Data field definition for binding configuration
 */
export interface DataField {
  /** Name of the component prop this field binds to */
  name: string;
  /** Type of data expected */
  type: DataFieldType;
  /** Whether this field is required for the component to function */
  required?: boolean;
  
  // Data source configuration
  /** Type of data source for this field */
  source: DataSourceType;
  
  // Source-specific configuration
  /** Path within the data source (for API/GraphQL) */
  path?: string;
  /** Query string (for GraphQL/SQL) */
  query?: string;
  /** Expression for computed values */
  compute?: string;
  
  // Transformation
  /** Data transformation configuration */
  transform?: DataTransform;
  
  // Validation
  /** Validation rules for the data */
  validation?: ValidationRule[];
}

/**
 * Supported data field types
 */
export type DataFieldType = 
  | 'string' 
  | 'number' 
  | 'boolean' 
  | 'date' 
  | 'array' 
  | 'object' 
  | 'any';

/**
 * Available data source types
 */
export type DataSourceType = 
  | 'static'     // Static value
  | 'api'        // REST API
  | 'graphql'    // GraphQL endpoint
  | 'database'   // Direct database query
  | 'computed'   // Computed from other values
  | 'context';   // Application context

/**
 * Data source configuration
 */
export interface DataSource {
  /** Unique identifier for the data source */
  id: ID;
  /** Human-readable name */
  name: string;
  /** Type of data source */
  type: DataSourceType;
  
  // Connection details
  /** API endpoint URL */
  endpoint?: string;
  /** HTTP method for API calls */
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  /** HTTP headers to include */
  headers?: Record<string, string>;
  
  // Query/Parameters
  /** Query string or GraphQL query */
  query?: string;
  /** Parameters to pass to the data source */
  parameters?: Record<string, unknown>;
  
  // Authentication
  /** Authentication configuration */
  auth?: AuthConfig;
  
  // Caching
  /** Cache configuration */
  cache?: CacheConfig;
}

/**
 * Authentication configuration for data sources
 */
export interface AuthConfig {
  /** Type of authentication */
  type: 'none' | 'bearer' | 'basic' | 'apikey' | 'oauth';
  /** Token or key value */
  token?: string;
  /** Username for basic auth */
  username?: string;
  /** Password for basic auth */
  password?: string;
  /** Header name for API key auth */
  headerName?: string;
}

/**
 * Data transformation configuration
 */
export interface DataTransform {
  /** Type of transformation */
  type: 'map' | 'filter' | 'reduce' | 'custom';
  /** JavaScript expression for the transformation */
  expression: string;
  /** Parameters for the transformation */
  parameters?: Record<string, unknown>;
}

/**
 * Data refresh configuration
 */
export interface RefreshConfig {
  /** Automatic refresh interval in milliseconds */
  interval?: number;
  
  /** Conditional refresh triggers */
  triggers?: RefreshTrigger[];
  
  // Refresh behavior
  /** Debounce delay in milliseconds */
  debounce?: number;
  /** Throttle limit in milliseconds */
  throttle?: number;
  
  /** Whether to refresh on component mount */
  refreshOnMount?: boolean;
  /** Whether to refresh when component becomes visible */
  refreshOnVisible?: boolean;
}

/**
 * Refresh trigger configuration
 */
export interface RefreshTrigger {
  /** Type of trigger */
  type: 'event' | 'dependency' | 'visibility' | 'custom';
  /** Source of the trigger (event name, dependency path, etc.) */
  source: string;
  /** Condition that must be met for refresh */
  condition?: string;
}

/**
 * Cache configuration for data sources
 */
export interface CacheConfig {
  /** Whether caching is enabled */
  enabled: boolean;
  /** Time to live in seconds */
  ttl?: number;
  /** Cache storage strategy */
  strategy?: 'memory' | 'localStorage' | 'sessionStorage';
  /** Cache key (auto-generated if not provided) */
  key?: string;
}

// ============================================================================
// Component Binding Types
// ============================================================================

/**
 * Component-level data and event bindings
 */
export interface ComponentBindings {
  // Data binding
  /** Primary data source identifier */
  dataSource?: string;
  /** Path within the data source */
  dataPath?: string;
  /** Data transformation expression */
  dataTransform?: string;
  
  // Event bindings
  /** Event handler configurations */
  events?: EventBinding[];
  
  // Visibility rules
  /** Visibility and security configuration */
  visibility?: {
    /** Visibility condition expression */
    condition?: string;
    /** Required permissions to view component */
    requiredPermissions?: string[];
    /** Required roles to view component */
    requiredRoles?: string[];
  };
  
  // Update configuration
  /** Auto-refresh interval in milliseconds */
  refreshInterval?: number;
  /** Events that trigger data refresh */
  refreshTriggers?: string[];
}

/**
 * Event binding configuration
 */
export interface EventBinding {
  /** Component event name (e.g., 'onClick', 'onSubmit') */
  event: string;
  /** Action to perform when event is triggered */
  action: string;
  /** Target component ID (if action affects another component) */
  target?: string;
  /** Parameters to pass to the action */
  parameters?: Record<string, unknown>;
  /** Condition that must be met for action to execute */
  condition?: string;
}

// ============================================================================
// Page-Level Data Binding Types
// ============================================================================

/**
 * Page-level data binding configuration
 */
export interface DataBindings {
  /** Global data sources available to all components on the page */
  sources: DataSource[];
  
  /** Global refresh configuration */
  refreshInterval?: number;
  /** Whether to refresh data when page gains focus */
  refreshOnFocus?: boolean;
  
  /** Global data transformations */
  transforms?: DataTransform[];
  
  /** Global error handling */
  errorHandling?: ErrorHandlingConfig;
}

/**
 * Error handling configuration for data operations
 */
export interface ErrorHandlingConfig {
  /** How to handle data loading errors */
  onError: 'throw' | 'ignore' | 'fallback' | 'retry';
  /** Fallback data to use on error */
  fallbackData?: unknown;
  /** Retry configuration */
  retry?: {
    /** Maximum number of retry attempts */
    maxAttempts: number;
    /** Delay between retries in milliseconds */
    delay: number;
    /** Whether to use exponential backoff */
    exponentialBackoff?: boolean;
  };
  /** Custom error handler function name */
  customHandler?: string;
}

// ============================================================================
// Interaction Types
// ============================================================================

/**
 * Component interaction configuration
 */
export interface InteractionConfig {
  /** Supported events that the component can emit */
  events: ComponentEvent[];
  /** Available actions that can be performed on the component */
  actions: ComponentAction[];
  /** State management configuration */
  state?: StateConfig;
}

/**
 * Component event definition
 */
export interface ComponentEvent {
  /** Event name */
  name: string;
  /** Optional event description */
  description?: string;
  /** Expected payload structure */
  payload?: PayloadDefinition;
  
  // Event metadata
  /** Whether the event bubbles up the component tree */
  bubbles?: boolean;
  /** Whether the event can be cancelled */
  cancelable?: boolean;
}

/**
 * Component action definition
 */
export interface ComponentAction {
  /** Action name */
  name: string;
  /** Optional action description */
  description?: string;
  
  /** Action parameters */
  parameters?: ActionParameter[];
  
  /** Handler function name or inline function */
  handler: string;
  
  /** Validation expression for action execution */
  validate?: string;
}

/**
 * Event payload definition structure
 */
export interface PayloadDefinition {
  [key: string]: {
    /** Type of the payload field */
    type: DataFieldType;
    /** Whether the field is required */
    required?: boolean;
    /** Description of the field */
    description?: string;
  };
}

/**
 * Action parameter definition
 */
export interface ActionParameter {
  /** Parameter name */
  name: string;
  /** Parameter type */
  type: DataFieldType;
  /** Whether the parameter is required */
  required?: boolean;
  /** Default value if not provided */
  defaultValue?: unknown;
  /** Parameter description */
  description?: string;
}

// ============================================================================
// State Management Types
// ============================================================================

/**
 * Component state management configuration
 */
export interface StateConfig {
  /** State fields managed by the component */
  fields: StateField[];
  
  /** State persistence configuration */
  persist?: {
    /** Whether to persist state */
    enabled: boolean;
    /** Persistence key */
    key: string;
    /** Storage mechanism */
    storage: 'memory' | 'localStorage' | 'sessionStorage';
  };
}

/**
 * State field definition
 */
export interface StateField {
  /** Field name */
  name: string;
  /** Field type */
  type: DataFieldType;
  /** Default value */
  defaultValue?: unknown;
  
  /** State update functions */
  updaters?: StateUpdater[];
}

/**
 * State updater function definition
 */
export interface StateUpdater {
  /** Updater name */
  name: string;
  /** Type of update operation */
  type: 'set' | 'toggle' | 'increment' | 'decrement' | 'append' | 'remove' | 'custom';
  /** Custom handler for complex updates */
  handler?: string;
}

// ============================================================================
// Validation Types
// ============================================================================

/**
 * Validation rule for data fields
 */
export interface ValidationRule {
  /** Type of validation */
  type: ValidationType;
  /** Value to validate against */
  value?: unknown;
  /** Error message if validation fails */
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

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Create a default data source configuration
 */
export const createDefaultDataSource = (
  name: string,
  type: DataSourceType
): Omit<DataSource, 'id'> => ({
  name,
  type,
  cache: {
    enabled: true,
    ttl: 300, // 5 minutes default
    strategy: 'memory'
  }
});

/**
 * Create a data field configuration
 */
export const createDataField = (
  name: string,
  type: DataFieldType,
  source: DataSourceType,
  overrides: Partial<DataField> = {}
): DataField => ({
  name,
  type,
  source,
  required: false,
  ...overrides
});

/**
 * Create an event binding
 */
export const createEventBinding = (
  event: string,
  action: string,
  overrides: Partial<EventBinding> = {}
): EventBinding => ({
  event,
  action,
  ...overrides
});

/**
 * Create a component event definition
 */
export const createComponentEvent = (
  name: string,
  overrides: Partial<ComponentEvent> = {}
): ComponentEvent => ({
  name,
  bubbles: true,
  cancelable: true,
  ...overrides
});

/**
 * Create a component action definition
 */
export const createComponentAction = (
  name: string,
  handler: string,
  overrides: Partial<ComponentAction> = {}
): ComponentAction => ({
  name,
  handler,
  ...overrides
});

/**
 * Create default refresh configuration
 */
export const createDefaultRefreshConfig = (): RefreshConfig => ({
  refreshOnMount: true,
  refreshOnVisible: true,
  debounce: 300,
  throttle: 1000
});

/**
 * Create default cache configuration
 */
export const createDefaultCacheConfig = (): CacheConfig => ({
  enabled: true,
  ttl: 300,
  strategy: 'memory'
});

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard for DataSource validation
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isDataSource = (obj: any): obj is DataSource => {
  return obj &&
    typeof obj.id === 'string' &&
    typeof obj.name === 'string' &&
    ['static', 'api', 'graphql', 'database', 'computed', 'context'].includes(obj.type);
};

/**
 * Type guard for DataField validation
 */
 
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isDataField = (obj: any): obj is DataField => {
  return obj &&
    typeof obj.name === 'string' &&
    ['string', 'number', 'boolean', 'date', 'array', 'object', 'any'].includes(obj.type) &&
    ['static', 'api', 'graphql', 'database', 'computed', 'context'].includes(obj.source);
};

/**
 * Type guard for ComponentBindings validation
 */
 
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isComponentBindings = (obj: any): obj is ComponentBindings => {
  return obj &&
    (obj.dataSource === undefined || typeof obj.dataSource === 'string') &&
    (obj.events === undefined || Array.isArray(obj.events));
};

/**
 * Type guard for EventBinding validation
 */
 
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isEventBinding = (obj: any): obj is EventBinding => {
  return obj &&
    typeof obj.event === 'string' &&
    typeof obj.action === 'string';
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Merge data bindings configurations
 */
export const mergeDataBindings = (
  base: DataBindings,
  override: Partial<DataBindings>
): DataBindings => {
  return {
    ...base,
    ...override,
    sources: [...base.sources, ...(override.sources || [])],
    transforms: [...(base.transforms || []), ...(override.transforms || [])]
  };
};

/**
 * Validate data against field constraints
 */
export const validateFieldData = (
  data: unknown,
  field: DataField
): { valid: boolean; errors: string[] } => {
  const errors: string[] = [];
  
  // Required validation
  if (field.required && (data === null || data === undefined || data === '')) {
    errors.push(`Field '${field.name}' is required`);
  }
  
  // Type validation
  if (data !== null && data !== undefined) {
    const actualType = Array.isArray(data) ? 'array' : typeof data;
    if (field.type !== 'any' && actualType !== field.type) {
      errors.push(`Field '${field.name}' expected ${field.type} but got ${actualType}`);
    }
  }
  
  // Custom validation rules
  if (field.validation) {
    for (const rule of field.validation) {
      const result = validateRule(data, rule);
      if (!result.valid && result.message) {
        errors.push(result.message);
      }
    }
  }
  
  return {
    valid: errors.length === 0,
    errors
  };
};

/**
 * Validate data against a single validation rule
 */
export const validateRule = (
  data: unknown,
  rule: ValidationRule
): { valid: boolean; message?: string } => {
  switch (rule.type) {
    case 'required':
      return {
        valid: data !== null && data !== undefined && data !== '',
        message: rule.message || 'This field is required'
      };
      
    case 'min':
      return {
        valid: typeof data === 'number' && typeof rule.value === 'number' && data >= rule.value,
        message: rule.message || `Value must be at least ${rule.value}`
      };
      
    case 'max':
      return {
        valid: typeof data === 'number' && typeof rule.value === 'number' && data <= rule.value,
        message: rule.message || `Value must be at most ${rule.value}`
      };
      
    case 'minLength': {
      const minLength = typeof rule.value === 'number' ? rule.value : 0;
      const hasMinLength = (
        (typeof data === 'string' && data.length >= minLength) ||
        (Array.isArray(data) && data.length >= minLength)
      );
      return {
        valid: hasMinLength,
        message: rule.message || `Must be at least ${minLength} characters/items`
      };
    }
      
    case 'maxLength': {
      const maxLength = typeof rule.value === 'number' ? rule.value : 0;
      const hasMaxLength = (
        (typeof data === 'string' && data.length <= maxLength) ||
        (Array.isArray(data) && data.length <= maxLength)
      );
      return {
        valid: hasMaxLength,
        message: rule.message || `Must be at most ${maxLength} characters/items`
      };
    }
      
    case 'pattern': {
      const patternStr = typeof rule.value === 'string' ? rule.value : '';
      const pattern = new RegExp(patternStr);
      return {
        valid: typeof data === 'string' && pattern.test(data),
        message: rule.message || 'Invalid format'
      };
    }
      
    case 'email': {
      const pattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return {
        valid: typeof data === 'string' && pattern.test(data),
        message: rule.message || 'Invalid email address'
      };
    }
      
    case 'url':
      try {
        if (typeof data === 'string') {
          new URL(data);
          return { valid: true };
        }
        return { valid: false, message: rule.message || 'Invalid URL' };
      } catch {
        return {
          valid: false,
          message: rule.message || 'Invalid URL'
        };
      }
      
    case 'custom':
      // Custom validation would need to be handled by the application
      return { valid: true };
      
    default:
      return { valid: true };
  }
};

/**
 * Build cache key for a data source
 */
export const buildCacheKey = (
  source: DataSource,
  parameters?: Record<string, unknown>
): string => {
  if (source.cache?.key) {
    return source.cache.key;
  }
  
  const baseKey = `${source.type}:${source.id}`;
  
  if (parameters && Object.keys(parameters).length > 0) {
    const paramString = JSON.stringify(parameters);
    return `${baseKey}:${btoa(paramString)}`;
  }
  
  return baseKey;
};

/**
 * Check if data source needs authentication
 */
export const requiresAuthentication = (source: DataSource): boolean => {
  return source.auth?.type !== 'none' && source.auth?.type !== undefined;
};

/**
 * Apply data transformation
 */
export const applyTransform = (
  data: unknown,
  transform: DataTransform
): unknown => {
  // This would need to be implemented by the application
  // with a proper expression evaluator
  switch (transform.type) {
    case 'map':
    case 'filter':
    case 'reduce':
    case 'custom':
    default:
      return data; // Placeholder implementation
  }
};