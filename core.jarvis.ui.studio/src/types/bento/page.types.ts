/**
 * Bento Page Types
 * 
 * Type definitions for Bento pages, including configuration, bindings,
 * and security settings.
 * 
 * @module BentoPageTypes
 */

// Core types defined locally to avoid circular imports
type ID = string;
type Timestamp = string;

export enum PageStatus {
  Draft = 'draft',
  Published = 'published',
  Archived = 'archived',
  Scheduled = 'scheduled'
}
// Forward declarations needed for DataBindings
interface DataSource {
  id: string;
  name: string;
  type: string;
}

interface DataTransform {
  type: string;
  expression: string;
}

// DataBindings interface (simplified version)
export interface DataBindings {
  /** Global data sources available to all components on the page */
  sources: DataSource[];
  
  /** Global refresh configuration */
  refreshInterval?: number;
  /** Whether to refresh data when page gains focus */
  refreshOnFocus?: boolean;
  
  /** Global data transformations */
  transforms?: DataTransform[];
}

// ============================================================================
// Core Page Types
// ============================================================================

/**
 * Complete Bento page configuration
 * 
 * Represents a fully configured page with layout, security, and data bindings.
 * Pages are the top-level entities that users navigate to in the application.
 */
export interface BentoPage {
  // Identification
  /** Unique page identifier */
  id: ID;
  /** Human-readable page name displayed in navigation */
  displayName: string;
  /** Optional page description for documentation */
  description?: string;
  
  // Routing
  /** URL route path for this page (e.g., '/dashboard', '/users') */
  route: string;
  
  // Layout reference
  /** ID of the layout configuration to use for this page */
  layoutId: ID;
  
  // Configuration
  /** Page-level configuration and bindings */
  bindings: PageBindings;
  
  // Metadata
  /** Current publication status */
  status: PageStatus;
  /** Version number for optimistic concurrency control */
  version: number;
  /** ISO timestamp when page was created */
  createdAt: Timestamp;
  /** ISO timestamp when page was last modified */
  updatedAt: Timestamp;
  /** ID of user who created the page */
  createdBy: ID;
  /** ID of user who last modified the page */
  updatedBy: ID;
  
  // Additional data
  /** Custom metadata for extended functionality */
  metadata?: Record<string, unknown>;
  /** Tags for categorization and search */
  tags?: string[];
}

/**
 * Page-level configuration bindings
 * 
 * Defines how the page behaves in terms of security, visibility, and data.
 */
export interface PageBindings {
  /** Security and access control configuration */
  security: SecurityBindings;
  /** Navigation and display visibility configuration */
  visibility: VisibilityBindings;
  /** Global data configuration for the page */
  data?: DataBindings;
  /** Custom bindings for extended functionality */
  custom?: Record<string, unknown>;
}

// ============================================================================
// Security Types
// ============================================================================

/**
 * Security configuration for page access control
 * 
 * Defines who can access the page and under what conditions.
 */
export interface SecurityBindings {
  /** Whether the page is publicly accessible without authentication */
  isPublic?: boolean;
  /** List of roles required to access this page */
  requiredRoles?: string[];
  /** List of permissions required to access this page */
  requiredPermissions?: string[];
  /** Advanced custom security rules */
  customRules?: SecurityRule[];
}

/**
 * Custom security rule configuration
 * 
 * Allows for complex security logic beyond simple role/permission checks.
 */
export interface SecurityRule {
  /** Unique identifier for the rule */
  id: ID;
  /** Type of rule implementation */
  type: 'expression' | 'function';
  /** The rule definition (JavaScript expression or function name) */
  rule: string;
  /** Parameters to pass to the rule evaluation */
  parameters?: Record<string, unknown>;
}

// ============================================================================
// Visibility Types
// ============================================================================

/**
 * Visibility configuration for navigation and display
 * 
 * Controls how and when the page appears in navigation and UI.
 */
export interface VisibilityBindings {
  /** Whether the page should appear in navigation menus */
  showInNavigation: boolean;
  /** Order priority in navigation (lower numbers appear first) */
  navigationOrder?: number;
  /** Group name for organizing navigation items */
  navigationGroup?: string;
  /** Icon identifier for navigation display */
  icon?: string;
  /** Conditional visibility rules */
  conditions?: VisibilityCondition[];
}

/**
 * Conditional visibility rule
 * 
 * Defines when a page should be visible based on dynamic conditions.
 */
export interface VisibilityCondition {
  /** Field or property to evaluate */
  field: string;
  /** Comparison operator to use */
  operator: 'equals' | 'notEquals' | 'contains' | 'greater' | 'less';
  /** Value to compare against */
  value: unknown;
}

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Create default security bindings for a new page
 */
export const createDefaultSecurityBindings = (): SecurityBindings => ({
  isPublic: false,
  requiredRoles: [],
  requiredPermissions: []
});

/**
 * Create default visibility bindings for a new page
 */
export const createDefaultVisibilityBindings = (): VisibilityBindings => ({
  showInNavigation: true,
  navigationOrder: 0
});

/**
 * Create default page bindings
 */
export const createDefaultPageBindings = (): PageBindings => ({
  security: createDefaultSecurityBindings(),
  visibility: createDefaultVisibilityBindings()
});

/**
 * Create a new page with default values
 */
export const createNewPage = (
  displayName: string,
  route: string,
  layoutId: ID,
  createdBy: ID
): Omit<BentoPage, 'id' | 'createdAt' | 'updatedAt'> => ({
  displayName,
  route,
  layoutId,
  bindings: createDefaultPageBindings(),
  status: PageStatus.Draft,
  version: 1,
  createdBy,
  updatedBy: createdBy
});

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard to check if an object is a valid BentoPage
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isBentoPage = (obj: any): obj is BentoPage => {
  return obj &&
    typeof obj.id === 'string' &&
    typeof obj.displayName === 'string' &&
    typeof obj.route === 'string' &&
    typeof obj.layoutId === 'string' &&
    obj.bindings &&
    typeof obj.status === 'string' &&
    typeof obj.version === 'number' &&
    typeof obj.createdAt === 'string' &&
    typeof obj.updatedAt === 'string' &&
    typeof obj.createdBy === 'string' &&
    typeof obj.updatedBy === 'string';
};

/**
 * Type guard to check if an object has valid page bindings
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidPageBindings = (obj: any): obj is PageBindings => {
  return obj &&
    obj.security &&
    obj.visibility &&
    typeof obj.visibility.showInNavigation === 'boolean';
};

/**
 * Type guard to check if an object has valid security bindings
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidSecurityBindings = (obj: any): obj is SecurityBindings => {
  return obj &&
    (obj.isPublic === undefined || typeof obj.isPublic === 'boolean') &&
    (obj.requiredRoles === undefined || Array.isArray(obj.requiredRoles)) &&
    (obj.requiredPermissions === undefined || Array.isArray(obj.requiredPermissions));
};

/**
 * Type guard to check if an object has valid visibility bindings
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidVisibilityBindings = (obj: any): obj is VisibilityBindings => {
  return obj &&
    typeof obj.showInNavigation === 'boolean' &&
    (obj.navigationOrder === undefined || typeof obj.navigationOrder === 'number') &&
    (obj.navigationGroup === undefined || typeof obj.navigationGroup === 'string') &&
    (obj.icon === undefined || typeof obj.icon === 'string');
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Check if a page is accessible by a user based on their roles and permissions
 */
export const canUserAccessPage = (
  page: BentoPage,
  userRoles: string[],
  userPermissions: string[]
): boolean => {
  const { security } = page.bindings;
  
  // Public pages are always accessible
  if (security.isPublic) {
    return true;
  }
  
  // Check required roles
  if (security.requiredRoles && security.requiredRoles.length > 0) {
    const hasRequiredRole = security.requiredRoles.some(role => 
      userRoles.includes(role)
    );
    if (hasRequiredRole) {
      return true;
    }
  }
  
  // Check required permissions
  if (security.requiredPermissions && security.requiredPermissions.length > 0) {
    const hasRequiredPermission = security.requiredPermissions.some(permission => 
      userPermissions.includes(permission)
    );
    if (hasRequiredPermission) {
      return true;
    }
  }
  
  // If no roles or permissions are specified, deny access
  if ((!security.requiredRoles || security.requiredRoles.length === 0) &&
      (!security.requiredPermissions || security.requiredPermissions.length === 0)) {
    return false;
  }
  
  return false;
};

/**
 * Check if a page should be visible in navigation based on its visibility settings
 */
export const shouldShowInNavigation = (
  page: BentoPage,
  context?: Record<string, unknown>
): boolean => {
  const { visibility } = page.bindings;
  
  // Basic navigation visibility check
  if (!visibility.showInNavigation) {
    return false;
  }
  
  // Check visibility conditions if they exist
  if (visibility.conditions && visibility.conditions.length > 0 && context) {
    return visibility.conditions.every(condition => {
      const fieldValue = context[condition.field];
      
      switch (condition.operator) {
        case 'equals':
          return fieldValue === condition.value;
        case 'notEquals':
          return fieldValue !== condition.value;
        case 'contains':
          return Array.isArray(fieldValue) 
            ? fieldValue.includes(condition.value)
            : String(fieldValue).includes(String(condition.value));
        case 'greater':
          return Number(fieldValue) > Number(condition.value);
        case 'less':
          return Number(fieldValue) < Number(condition.value);
        default:
          return true;
      }
    });
  }
  
  return true;
};

/**
 * Sort pages by their navigation order
 */
export const sortPagesByNavigationOrder = (pages: BentoPage[]): BentoPage[] => {
  return [...pages].sort((a, b) => {
    const orderA = a.bindings.visibility.navigationOrder ?? Number.MAX_SAFE_INTEGER;
    const orderB = b.bindings.visibility.navigationOrder ?? Number.MAX_SAFE_INTEGER;
    
    if (orderA !== orderB) {
      return orderA - orderB;
    }
    
    // If orders are equal, sort by display name
    return a.displayName.localeCompare(b.displayName);
  });
};

/**
 * Group pages by their navigation group
 */
export const groupPagesByNavigationGroup = (pages: BentoPage[]): Record<string, BentoPage[]> => {
  const grouped: Record<string, BentoPage[]> = {};
  
  pages.forEach(page => {
    const group = page.bindings.visibility.navigationGroup ?? 'default';
    if (!grouped[group]) {
      grouped[group] = [];
    }
    grouped[group].push(page);
  });
  
  // Sort pages within each group
  Object.keys(grouped).forEach(group => {
    grouped[group] = sortPagesByNavigationOrder(grouped[group]);
  });
  
  return grouped;
};