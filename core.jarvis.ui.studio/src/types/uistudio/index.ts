/**
 * UIStudio API Types
 * 
 * Type definitions for the UIStudio API system that integrates with
 * the Jarvis ECS backend for page and component management.
 * 
 * @module UIStudioTypes
 */

// ============================================================================
// Core UIStudio Types (matching API responses)
// ============================================================================

/** Base interface for Jarvis ECS components */
export interface IUIStudioComponent {
  id: string;
  ownerEntityId: string;
  lastUpdated: string;
}

/** Page component from UIStudio API */
export interface UIStudioPage extends IUIStudioComponent {
  pageName: string;
  pageSlug: string;
  pageType: 'static' | 'dynamic';
  description?: string;
  isPublished: boolean;
  publishedAt?: string;
  createdAt: string;
  version?: number;
  
  // JSON serialized fields
  metadata?: Record<string, unknown>;
  tags?: string;
  
  // Audit fields
  createdByEntityId: string;
  updatedByEntityId?: string;
}

/** Layout component from UIStudio API */
export interface UIStudioLayout extends IUIStudioComponent {
  layoutType: 'grid' | 'flex' | 'masonry';
  maxColumns: number;
  maxRows?: number;
  isResponsive: boolean;
  
  // Grid configuration
  gridConfig?: {
    columns: number;
    gap: string;
    padding?: string;
    minItemWidth?: string;
    rowHeight?: string;
  };
  
  // Responsive configuration
  responsiveConfig?: {
    mobile?: { columns: number; gap: string };
    tablet?: { columns: number; gap: string };
    desktop?: { columns: number; gap: string };
  };
  
  // Breakpoint settings
  breakpointSettings?: {
    mobile: number;
    tablet: number;
    desktop: number;
  };
  
  // Audit fields
  createdByEntityId: string;
  updatedByEntityId?: string;
}

/** Component binding from UIStudio API */
export interface UIStudioComponentBinding extends IUIStudioComponent {
  pageSlug: string;
  componentType: string;
  componentInstanceId: string;
  boundComponentType: string;
  
  // Field mappings (JSON path expressions)
  fieldMappings?: Record<string, string>;
  
  // Data source configuration
  dataSourceConfig?: {
    filters?: Array<{
      field: string;
      operator: string;
      value: unknown;
    }>;
    sorting?: Array<{
      field: string;
      direction: 'asc' | 'desc';
    }>;
    pagination?: {
      pageSize: number;
      enabled: boolean;
    };
  };
  
  // Position configuration
  positionConfig?: {
    gridColumn?: string;
    gridRow?: string;
    minWidth?: string;
    minHeight?: string;
  };
  
  // Style configuration
  styleConfig?: {
    theme?: string;
    headerColor?: string;
    borderRadius?: string;
    [key: string]: unknown;
  };
  
  // Behavior configuration
  behaviorConfig?: {
    sortable?: boolean;
    filterable?: boolean;
    selectable?: 'none' | 'single' | 'multiple';
    [key: string]: unknown;
  };
  
  // Audit fields
  createdByEntityId: string;
  updatedByEntityId?: string;
}

/** Template component from UIStudio API */
export interface UIStudioTemplate extends IUIStudioComponent {
  templateName: string;
  description?: string;
  templateType: 'page' | 'layout' | 'component';
  category?: string;
  
  // Template data (JSON serialized)
  templateData: Record<string, unknown>;
  defaultValues?: Record<string, unknown>;
  
  // Sharing and usage
  isPublic: boolean;
  usageCount?: number;
  tags?: string;
  
  // Preview
  previewImage?: string;
  
  // Audit fields
  createdByEntityId: string;
  updatedByEntityId?: string;
}

/** Permission component from UIStudio API */
export interface UIStudioPermission extends IUIStudioComponent {
  resourceEntityId: string;
  resourceType: 'page' | 'layout' | 'template';
  granteeEntityId: string;
  permissionLevel: 'read' | 'write' | 'admin';
  reason?: string;
  expiresAt?: string;
  
  // Audit fields
  grantedByEntityId: string;
  revokedAt?: string;
  revokedByEntityId?: string;
}

/** Version control component from UIStudio API */
export interface UIStudioVersion extends IUIStudioComponent {
  resourceEntityId: string;
  resourceType: 'page' | 'layout' | 'template';
  versionLabel: string;
  changeDescription?: string;
  changeReason?: string;
  
  // Version data (JSON serialized snapshot)
  versionData: Record<string, unknown>;
  
  // State
  isPublished?: boolean;
  publishedAt?: string;
  publishedByEntityId?: string;
  
  // Audit fields
  createdByEntityId: string;
}

// ============================================================================
// API Request Types
// ============================================================================

/** Request to create a new page */
export interface CreatePageRequest {
  pageName: string;
  pageSlug: string;
  pageType: 'static' | 'dynamic';
  description?: string;
  createdByEntityId: string;
  metadata?: Record<string, unknown>;
  tags?: string;
}

/** Request to update an existing page */
export interface UpdatePageRequest {
  pageName?: string;
  pageSlug?: string;
  pageType?: 'static' | 'dynamic';
  description?: string;
  updatedByEntityId: string;
  metadata?: Record<string, unknown>;
  tags?: string;
}

/** Request to create a new layout */
export interface CreateLayoutRequest {
  layoutType: 'grid' | 'flex' | 'masonry';
  maxColumns: number;
  maxRows?: number;
  isResponsive: boolean;
  gridConfig?: UIStudioLayout['gridConfig'];
  responsiveConfig?: UIStudioLayout['responsiveConfig'];
  breakpointSettings?: UIStudioLayout['breakpointSettings'];
  createdByEntityId: string;
}

/** Request to update an existing layout */
export interface UpdateLayoutRequest {
  layoutType?: 'grid' | 'flex' | 'masonry';
  maxColumns?: number;
  maxRows?: number;
  isResponsive?: boolean;
  gridConfig?: UIStudioLayout['gridConfig'];
  responsiveConfig?: UIStudioLayout['responsiveConfig'];
  breakpointSettings?: UIStudioLayout['breakpointSettings'];
  updatedByEntityId: string;
}

/** Request to create a component binding */
export interface CreateBindingRequest {
  pageSlug: string;
  componentType: string;
  componentInstanceId: string;
  boundComponentType: string;
  fieldMappings?: Record<string, string>;
  dataSourceConfig?: UIStudioComponentBinding['dataSourceConfig'];
  positionConfig?: UIStudioComponentBinding['positionConfig'];
  styleConfig?: UIStudioComponentBinding['styleConfig'];
  behaviorConfig?: UIStudioComponentBinding['behaviorConfig'];
  createdByEntityId: string;
}

/** Request to update a component binding */
export interface UpdateBindingRequest {
  pageSlug?: string;
  componentType?: string;
  componentInstanceId?: string;
  boundComponentType?: string;
  fieldMappings?: Record<string, string>;
  dataSourceConfig?: UIStudioComponentBinding['dataSourceConfig'];
  positionConfig?: UIStudioComponentBinding['positionConfig'];
  styleConfig?: UIStudioComponentBinding['styleConfig'];
  behaviorConfig?: UIStudioComponentBinding['behaviorConfig'];
  updatedByEntityId: string;
}

/** Request to create a template */
export interface CreateTemplateRequest {
  templateName: string;
  description?: string;
  templateType: 'page' | 'layout' | 'component';
  category?: string;
  templateData: Record<string, unknown>;
  defaultValues?: Record<string, unknown>;
  isPublic: boolean;
  tags?: string;
  previewImage?: string;
  createdByEntityId: string;
}

/** Request to grant permission */
export interface GrantPermissionRequest {
  resourceEntityId: string;
  resourceType: 'page' | 'layout' | 'template';
  granteeEntityId: string;
  permissionLevel: 'read' | 'write' | 'admin';
  reason?: string;
  expiresAt?: string;
  grantedByEntityId: string;
}

/** Request to create version snapshot */
export interface CreateVersionRequest {
  resourceEntityId: string;
  resourceType: 'page' | 'layout' | 'template';
  versionLabel: string;
  changeDescription?: string;
  changeReason?: string;
  createdByEntityId: string;
}

/** Request to duplicate a page */
export interface DuplicatePageRequest {
  pageName: string;
  pageSlug: string;
  createdByEntityId: string;
}

/** Request to apply a template */
export interface ApplyTemplateRequest {
  pageName: string;
  pageSlug: string;
  createdByEntityId: string;
}

// ============================================================================
// API Response Types
// ============================================================================

/** Standard API response wrapper */
export type UIStudioApiResponse<T> = T[];

/** Error response from UIStudio API */
export interface UIStudioApiError {
  error: string;
  message: string;
  timestamp: string;
  details?: Record<string, unknown>;
}

/** Query parameters for getting published pages */
export interface GetPublishedPagesQuery {
  limit?: number;
  offset?: number;
  pageType?: 'static' | 'dynamic';
  search?: string;
}

/** Query parameters for getting resource permissions */
export interface GetResourcePermissionsQuery {
  resourceType: 'page' | 'layout' | 'template';
}

/** Query parameters for getting version history */
export interface GetVersionHistoryQuery {
  limit?: number;
  offset?: number;
}

// ============================================================================
// Utility Types
// ============================================================================

/** Generic ID type for UIStudio resources */
export type UIStudioEntityId = string;

/** Resource types supported by UIStudio */
export type UIStudioResourceType = 'page' | 'layout' | 'template' | 'binding' | 'permission' | 'version';

/** Component types supported for binding */
export type UIStudioComponentType = 'table' | 'card' | 'chart' | 'form' | 'list' | 'grid' | 'custom';

/** Page types supported */
export type UIStudioPageType = 'static' | 'dynamic';

/** Layout types supported */
export type UIStudioLayoutType = 'grid' | 'flex' | 'masonry';

/** Permission levels */
export type UIStudioPermissionLevel = 'read' | 'write' | 'admin';

// ============================================================================
// Type Guards
// ============================================================================

/** Type guard for UIStudioPage */
export const isUIStudioPage = (obj: unknown): obj is UIStudioPage => {
  return typeof obj === 'object' && obj !== null &&
    'id' in obj && 'pageName' in obj && 'pageSlug' in obj;
};

/** Type guard for UIStudioLayout */
export const isUIStudioLayout = (obj: unknown): obj is UIStudioLayout => {
  return typeof obj === 'object' && obj !== null &&
    'id' in obj && 'layoutType' in obj && 'maxColumns' in obj;
};

/** Type guard for UIStudioComponentBinding */
export const isUIStudioComponentBinding = (obj: unknown): obj is UIStudioComponentBinding => {
  return typeof obj === 'object' && obj !== null &&
    'id' in obj && 'componentType' in obj && 'boundComponentType' in obj;
};

/** Type guard for UIStudioTemplate */
export const isUIStudioTemplate = (obj: unknown): obj is UIStudioTemplate => {
  return typeof obj === 'object' && obj !== null &&
    'id' in obj && 'templateName' in obj && 'templateType' in obj;
};

// ============================================================================
// Constants
// ============================================================================

/** Default grid configuration */
export const DEFAULT_GRID_CONFIG = {
  columns: 12,
  gap: '16px',
  padding: '20px',
  minItemWidth: '200px'
} as const;

/** Default responsive breakpoints */
export const DEFAULT_BREAKPOINTS = {
  mobile: 768,
  tablet: 1024,
  desktop: 1440
} as const;

/** Default responsive configuration */
export const DEFAULT_RESPONSIVE_CONFIG = {
  mobile: { columns: 1, gap: '8px' },
  tablet: { columns: 2, gap: '12px' },
  desktop: { columns: 4, gap: '16px' }
} as const;

/** API endpoints */
export const UISTUDIO_ENDPOINTS = {
  PAGES: '/api/uistudio/pages',
  LAYOUTS: '/api/uistudio/layouts',
  BINDINGS: '/api/uistudio/bindings',
  TEMPLATES: '/api/uistudio/templates',
  PERMISSIONS: '/api/uistudio/permissions',
  VERSIONS: '/api/uistudio/versions'
} as const;