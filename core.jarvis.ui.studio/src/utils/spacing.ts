/**
 * Spacing system based on 8px grid
 * All values are multiples of 8px for visual consistency
 */

export const SPACING_UNIT = 8;

// T-shirt sizes in pixels
export const SPACING = {
  xs: 4,    // 0.5 * 8
  sm: 8,    // 1 * 8
  md: 16,   // 2 * 8
  lg: 24,   // 3 * 8
  xl: 32,   // 4 * 8
  '2xl': 40, // 5 * 8
  '3xl': 48, // 6 * 8
  '4xl': 64, // 8 * 8
  '5xl': 80, // 10 * 8
  '6xl': 96, // 12 * 8
} as const;

// Spacing classes for Tailwind
export const SPACING_CLASSES = {
  padding: {
    xs: 'p-xs',
    sm: 'p-sm',
    md: 'p-md',
    lg: 'p-lg',
    xl: 'p-xl',
    '2xl': 'p-2xl',
    '3xl': 'p-3xl',
    '4xl': 'p-4xl',
  },
  margin: {
    xs: 'm-xs',
    sm: 'm-sm',
    md: 'm-md',
    lg: 'm-lg',
    xl: 'm-xl',
    '2xl': 'm-2xl',
    '3xl': 'm-3xl',
    '4xl': 'm-4xl',
  },
  gap: {
    xs: 'gap-xs',
    sm: 'gap-sm',
    md: 'gap-md',
    lg: 'gap-lg',
    xl: 'gap-xl',
    '2xl': 'gap-2xl',
    '3xl': 'gap-3xl',
    '4xl': 'gap-4xl',
  },
} as const;

// Component-specific spacing presets
export const COMPONENT_SPACING = {
  page: {
    padding: 'p-lg', // 24px
    section: 'mb-xl', // 32px bottom margin
  },
  card: {
    padding: 'p-md', // 16px
    gap: 'gap-sm', // 8px
  },
  form: {
    gap: 'gap-md', // 16px between form fields
    inputPadding: 'px-sm py-xs', // 8px horizontal, 4px vertical
  },
  button: {
    sm: 'px-sm py-xs', // Small button
    md: 'px-md py-sm', // Medium button
    lg: 'px-lg py-md', // Large button
  },
  table: {
    cellCompact: 'px-md py-xs', // Compact cells
    cellDefault: 'px-md py-sm', // Default cells
    cellRelaxed: 'px-lg py-md', // Relaxed cells
  },
} as const;

// Helper function to get pixel value from t-shirt size
export function getSpacingValue(size: keyof typeof SPACING): number {
  return SPACING[size];
}

// Helper function to get rem value from pixels
export function pxToRem(px: number): string {
  return `${px / 16}rem`;
}

// Helper function to get spacing in rem
export function getSpacingRem(size: keyof typeof SPACING): string {
  return pxToRem(SPACING[size]);
}

// Helper to ensure value is on 8px grid
export function snapToGrid(value: number): number {
  return Math.round(value / SPACING_UNIT) * SPACING_UNIT;
}

// Type for spacing sizes
export type SpacingSize = keyof typeof SPACING;
export type SpacingValue = typeof SPACING[SpacingSize];