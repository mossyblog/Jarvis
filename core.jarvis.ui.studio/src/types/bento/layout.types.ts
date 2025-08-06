/**
 * Bento Layout Types
 * 
 * Type definitions for Bento layouts, which define the overall structure
 * and responsive behavior of pages across different devices.
 * 
 * @module BentoLayoutTypes
 */

// Core types defined locally to avoid circular imports
type ID = string;
type Timestamp = string;

// ============================================================================
// Core Layout Types
// ============================================================================

/**
 * Complete Bento layout configuration
 * 
 * Layouts define the structure and responsive behavior of pages.
 * Each layout can have different grid configurations for different devices.
 */
export interface BentoLayout {
  // Identification
  /** Unique layout identifier */
  id: ID;
  /** Human-readable layout name */
  name: string;
  /** Optional description of the layout's purpose */
  description?: string;
  /** Layout category for organization */
  category?: 'standard' | 'custom' | 'template';
  
  // Grid configurations per device
  /** Grid configurations for different screen sizes */
  grids: {
    /** Required desktop grid configuration */
    desktop: ID;
    /** Optional tablet grid (falls back to desktop if not specified) */
    tablet?: ID;
    /** Optional mobile grid (falls back to tablet or desktop if not specified) */
    mobile?: ID;
  };
  
  // Layout settings
  /** Global layout configuration and styling */
  settings: LayoutSettings;
  
  // Metadata
  /** Whether this is the default layout for new pages */
  isDefault?: boolean;
  /** ISO timestamp when layout was created */
  createdAt: Timestamp;
  /** ISO timestamp when layout was last modified */
  updatedAt: Timestamp;
  
  // Preview
  /** URL to layout thumbnail image */
  thumbnail?: string;
  /** URL to layout preview image or description */
  preview?: string;
}

/**
 * Global layout settings that affect the entire page structure
 */
export interface LayoutSettings {
  // Container behavior
  /** How the main container should behave */
  containerWidth?: 'fixed' | 'fluid' | 'full';
  /** Maximum width in pixels for fixed and fluid containers */
  maxWidth?: number;
  
  // Spacing configuration
  /** Padding around the main container */
  padding?: SpacingConfig;
  /** Margin around the main container */
  margin?: SpacingConfig;
  
  // Background styling
  /** Background configuration for the layout */
  background?: BackgroundConfig;
  
  // Responsive behavior
  /** Custom breakpoints for this layout */
  breakpoints?: BreakpointConfig;
}

// ============================================================================
// Spacing Types
// ============================================================================

/**
 * Spacing configuration for margins and padding
 */
export interface SpacingConfig {
  /** Top spacing */
  top?: number;
  /** Right spacing */
  right?: number;
  /** Bottom spacing */
  bottom?: number;
  /** Left spacing */
  left?: number;
  /** Unit of measurement for spacing values */
  unit?: 'px' | 'rem' | 'em' | '%';
}

// ============================================================================
// Background Types
// ============================================================================

/**
 * Background styling configuration
 */
export interface BackgroundConfig {
  /** Background color (CSS color value) */
  color?: string;
  /** Background image URL */
  image?: string;
  /** Background repeat behavior */
  repeat?: 'repeat' | 'no-repeat' | 'repeat-x' | 'repeat-y';
  /** Background position */
  position?: string;
  /** Background size behavior */
  size?: 'cover' | 'contain' | 'auto';
}

// ============================================================================
// Responsive Types
// ============================================================================

/**
 * Breakpoint configuration for responsive design
 */
export interface BreakpointConfig {
  /** Desktop breakpoint in pixels */
  desktop?: number;
  /** Tablet breakpoint in pixels */
  tablet?: number;
  /** Mobile breakpoint in pixels */
  mobile?: number;
  /** Custom named breakpoints */
  custom?: Record<string, number>;
}

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Create default spacing configuration
 */
export const createDefaultSpacing = (): SpacingConfig => ({
  top: 16,
  right: 16,
  bottom: 16,
  left: 16,
  unit: 'px'
});

/**
 * Create uniform spacing configuration
 */
export const createUniformSpacing = (value: number, unit: SpacingConfig['unit'] = 'px'): SpacingConfig => ({
  top: value,
  right: value,
  bottom: value,
  left: value,
  unit
});

/**
 * Create default background configuration
 */
export const createDefaultBackground = (): BackgroundConfig => ({
  color: 'transparent',
  repeat: 'no-repeat',
  position: 'center center',
  size: 'cover'
});

/**
 * Create default breakpoint configuration
 */
export const createDefaultBreakpoints = (): BreakpointConfig => ({
  desktop: 1200,
  tablet: 768,
  mobile: 480
});

/**
 * Create default layout settings
 */
export const createDefaultLayoutSettings = (): LayoutSettings => ({
  containerWidth: 'fluid',
  maxWidth: 1200,
  padding: createDefaultSpacing(),
  margin: createUniformSpacing(0),
  background: createDefaultBackground(),
  breakpoints: createDefaultBreakpoints()
});

/**
 * Create a new layout with default values
 */
export const createNewLayout = (
  name: string,
  desktopGridId: ID
): Omit<BentoLayout, 'id' | 'createdAt' | 'updatedAt'> => ({
  name,
  category: 'custom',
  grids: {
    desktop: desktopGridId
  },
  settings: createDefaultLayoutSettings(),
  isDefault: false
});

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard to check if an object is a valid BentoLayout
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isBentoLayout = (obj: any): obj is BentoLayout => {
  return obj &&
    typeof obj.id === 'string' &&
    typeof obj.name === 'string' &&
    obj.grids &&
    typeof obj.grids.desktop === 'string' &&
    obj.settings &&
    typeof obj.createdAt === 'string' &&
    typeof obj.updatedAt === 'string';
};

/**
 * Type guard to check if an object has valid layout settings
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidLayoutSettings = (obj: any): obj is LayoutSettings => {
  return obj &&
    (obj.containerWidth === undefined || 
     ['fixed', 'fluid', 'full'].includes(obj.containerWidth)) &&
    (obj.maxWidth === undefined || typeof obj.maxWidth === 'number');
};

/**
 * Type guard to check if an object has valid spacing configuration
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidSpacingConfig = (obj: any): obj is SpacingConfig => {
  return obj &&
    (obj.top === undefined || typeof obj.top === 'number') &&
    (obj.right === undefined || typeof obj.right === 'number') &&
    (obj.bottom === undefined || typeof obj.bottom === 'number') &&
    (obj.left === undefined || typeof obj.left === 'number') &&
    (obj.unit === undefined || ['px', 'rem', 'em', '%'].includes(obj.unit));
};

/**
 * Type guard to check if an object has valid background configuration
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidBackgroundConfig = (obj: any): obj is BackgroundConfig => {
  return obj &&
    (obj.color === undefined || typeof obj.color === 'string') &&
    (obj.image === undefined || typeof obj.image === 'string') &&
    (obj.repeat === undefined || 
     ['repeat', 'no-repeat', 'repeat-x', 'repeat-y'].includes(obj.repeat)) &&
    (obj.position === undefined || typeof obj.position === 'string') &&
    (obj.size === undefined || ['cover', 'contain', 'auto'].includes(obj.size));
};

/**
 * Type guard to check if an object has valid breakpoint configuration
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidBreakpointConfig = (obj: any): obj is BreakpointConfig => {
  return obj &&
    (obj.desktop === undefined || typeof obj.desktop === 'number') &&
    (obj.tablet === undefined || typeof obj.tablet === 'number') &&
    (obj.mobile === undefined || typeof obj.mobile === 'number') &&
    (obj.custom === undefined || typeof obj.custom === 'object');
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Get the appropriate grid ID for a given device type and layout
 */
export const getGridIdForDevice = (
  layout: BentoLayout,
  device: 'desktop' | 'tablet' | 'mobile'
): ID => {
  switch (device) {
    case 'mobile':
      return layout.grids.mobile ?? layout.grids.tablet ?? layout.grids.desktop;
    case 'tablet':
      return layout.grids.tablet ?? layout.grids.desktop;
    case 'desktop':
    default:
      return layout.grids.desktop;
  }
};

/**
 * Convert spacing configuration to CSS properties
 */
export const spacingToCss = (spacing: SpacingConfig, property: 'padding' | 'margin'): Record<string, string> => {
  const unit = spacing.unit ?? 'px';
  const top = spacing.top !== undefined ? `${spacing.top}${unit}` : '0';
  const right = spacing.right !== undefined ? `${spacing.right}${unit}` : '0';
  const bottom = spacing.bottom !== undefined ? `${spacing.bottom}${unit}` : '0';
  const left = spacing.left !== undefined ? `${spacing.left}${unit}` : '0';
  
  return {
    [`${property}Top`]: top,
    [`${property}Right`]: right,
    [`${property}Bottom`]: bottom,
    [`${property}Left`]: left
  };
};

/**
 * Convert background configuration to CSS properties
 */
export const backgroundToCss = (background: BackgroundConfig): Record<string, string> => {
  const css: Record<string, string> = {};
  
  if (background.color) {
    css.backgroundColor = background.color;
  }
  
  if (background.image) {
    css.backgroundImage = `url(${background.image})`;
    
    if (background.repeat) {
      css.backgroundRepeat = background.repeat;
    }
    
    if (background.position) {
      css.backgroundPosition = background.position;
    }
    
    if (background.size) {
      css.backgroundSize = background.size;
    }
  }
  
  return css;
};

/**
 * Get the current breakpoint based on viewport width
 */
export const getCurrentBreakpoint = (
  viewportWidth: number,
  breakpoints: BreakpointConfig
): 'mobile' | 'tablet' | 'desktop' => {
  const desktop = breakpoints.desktop ?? 1200;
  const tablet = breakpoints.tablet ?? 768;
  
  if (viewportWidth >= desktop) {
    return 'desktop';
  } else if (viewportWidth >= tablet) {
    return 'tablet';
  } else {
    return 'mobile';
  }
};

/**
 * Check if a layout has responsive grid configurations
 */
export const isResponsiveLayout = (layout: BentoLayout): boolean => {
  return !!(layout.grids.tablet || layout.grids.mobile);
};

/**
 * Clone a layout with modifications
 */
export const cloneLayout = (
  layout: BentoLayout,
  modifications: Partial<BentoLayout>
): BentoLayout => {
  return {
    ...layout,
    ...modifications,
    grids: {
      ...layout.grids,
      ...modifications.grids
    },
    settings: {
      ...layout.settings,
      ...modifications.settings
    }
  };
};

/**
 * Merge layout settings with another settings object
 */
export const mergeLayoutSettings = (
  base: LayoutSettings,
  override: Partial<LayoutSettings>
): LayoutSettings => {
  return {
    ...base,
    ...override,
    padding: override.padding ? { ...base.padding, ...override.padding } : base.padding,
    margin: override.margin ? { ...base.margin, ...override.margin } : base.margin,
    background: override.background ? { ...base.background, ...override.background } : base.background,
    breakpoints: override.breakpoints ? { ...base.breakpoints, ...override.breakpoints } : base.breakpoints
  };
};