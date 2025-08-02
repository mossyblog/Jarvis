# T-Shirt Sizing System - Rule of 8

## Overview

This comprehensive sizing system ensures all UI dimensions follow the "rule of 8" where every measurement is divisible by 8 pixels. This creates visual consistency, improves design implementation speed, and provides better accessibility through standardized touch targets.

## Design Principles

1. **Rule of 8**: All dimensions are multiples of 8px (8, 16, 24, 32, 40, 48, 56, 64, 72, 80...)
2. **T-shirt Sizing**: Uses xs, sm, md, lg, xl, 2xl, 3xl for intuitive scaling
3. **Touch-First**: Ensures minimum 44px touch targets for accessibility
4. **Consistent Hierarchy**: Clear visual relationships between component sizes
5. **Developer-Friendly**: Easy to remember and implement

## Size Scale

| Size | Multiplier | Pixels | Use Case |
|------|------------|--------|----------|
| xs   | 1          | 8px    | Fine details, tight spacing |
| sm   | 2          | 16px   | Small components, compact layouts |
| md   | 3          | 24px   | Default spacing, medium components |
| lg   | 4          | 32px   | Large components, section spacing |
| xl   | 5          | 40px   | Extra large components |
| 2xl  | 6          | 48px   | Hero elements, prominent features |
| 3xl  | 8          | 64px   | Major layout sections |

## CSS Custom Properties

### Spacing
```css
--spacing-xs: 8px
--spacing-sm: 16px
--spacing-md: 24px
--spacing-lg: 32px
--spacing-xl: 40px
--spacing-2xl: 48px
--spacing-3xl: 64px
```

### Component Heights
```css
--height-xs: 32px    /* Small buttons, compact inputs */
--height-sm: 40px    /* Default buttons */
--height-md: 48px    /* Large buttons, standard inputs */
--height-lg: 56px    /* Prominent actions */
--height-xl: 64px    /* Hero buttons */
--height-2xl: 80px   /* Extra large components */
--height-3xl: 96px   /* Massive components */
```

### Component Widths
```css
--width-xs: 32px     /* Icon buttons */
--width-sm: 48px     /* Small buttons */
--width-md: 64px     /* Medium buttons */
--width-lg: 80px     /* Large buttons */
--width-xl: 96px     /* Extra large */
--width-2xl: 128px   /* Very wide */
--width-3xl: 160px   /* Maximum width */
```

### Font Sizes
```css
--font-size-xs: 12px   /* Captions, fine print */
--font-size-sm: 14px   /* Secondary text */
--font-size-md: 16px   /* Body text, default */
--font-size-lg: 18px   /* Subheadings */
--font-size-xl: 20px   /* Headings */
--font-size-2xl: 24px  /* Large headings */
--font-size-3xl: 32px  /* Hero text */
```

### Touch Targets
```css
--touch-target-min: 44px   /* iOS minimum */
--touch-target-sm: 48px    /* Material minimum */
--touch-target-md: 56px    /* Comfortable */
--touch-target-lg: 64px    /* Large targets */
```

## Utility Classes

### Spacing
```css
/* Padding */
.p-xs, .p-sm, .p-md, .p-lg, .p-xl, .p-2xl, .p-3xl
.px-xs, .px-sm, .px-md, .px-lg, .px-xl, .px-2xl, .px-3xl
.py-xs, .py-sm, .py-md, .py-lg, .py-xl, .py-2xl, .py-3xl

/* Margin */
.m-xs, .m-sm, .m-md, .m-lg, .m-xl, .m-2xl, .m-3xl
.mx-xs, .mx-sm, .mx-md, .mx-lg, .mx-xl, .mx-2xl, .mx-3xl
.my-xs, .my-sm, .my-md, .my-lg, .my-xl, .my-2xl, .my-3xl

/* Gap */
.gap-xs, .gap-sm, .gap-md, .gap-lg, .gap-xl, .gap-2xl, .gap-3xl
```

### Dimensions
```css
/* Height */
.h-xs, .h-sm, .h-md, .h-lg, .h-xl, .h-2xl, .h-3xl
.min-h-xs, .min-h-sm, .min-h-md, .min-h-lg, .min-h-xl, .min-h-2xl, .min-h-3xl

/* Width */
.w-xs, .w-sm, .w-md, .w-lg, .w-xl, .w-2xl, .w-3xl
.min-w-xs, .min-w-sm, .min-w-md, .min-w-lg, .min-w-xl, .min-w-2xl, .min-w-3xl
```

### Typography
```css
.text-xs, .text-sm, .text-md, .text-lg, .text-xl, .text-2xl, .text-3xl
```

### Icons
```css
.icon-xs, .icon-sm, .icon-md, .icon-lg, .icon-xl, .icon-2xl, .icon-3xl
```

### Touch Targets
```css
.touch-target-min, .touch-target-sm, .touch-target-md, .touch-target-lg
```

## Component Presets

### Buttons
```css
.btn-xs   /* 32px height, 8px padding */
.btn-sm   /* 40px height, 16px padding */
.btn-md   /* 48px height, 24px padding */
.btn-lg   /* 56px height, 32px padding */
```

### Inputs
```css
.input-xs  /* 32px height, compact form */
.input-sm  /* 40px height, standard form */
.input-md  /* 48px height, prominent form */
.input-lg  /* 56px height, hero form */
```

### Cards
```css
.card-xs   /* Compact card with small padding */
.card-sm   /* Standard card */
.card-md   /* Prominent card */
.card-lg   /* Hero card */
```

## Usage Examples

### Button Component
```tsx
// Updated button variants using the sizing system
const buttonVariants = cva(
  "inline-flex items-center justify-center whitespace-nowrap font-medium transition-colors",
  {
    variants: {
      size: {
        xs: "btn-xs gap-xs [&_svg]:icon-xs",
        sm: "btn-sm gap-xs [&_svg]:icon-xs", 
        default: "btn-md gap-sm [&_svg]:icon-sm",
        lg: "btn-lg gap-sm [&_svg]:icon-md",
        xl: "h-xl px-2xl gap-md text-lg rounded-lg [&_svg]:icon-lg",
      },
    },
  }
)
```

### Grid Component Spacing
```tsx
// Touch-friendly sizing with proper targets
const isTouchDevice = detectTouchDevice();
const touchTargetSize = 'var(--touch-target-min)';

// Button sizing
className={cn(
  isTouchDevice ? "h-lg w-lg p-0" : "h-sm w-sm p-0",
  "touch-manipulation"
)}

// Icon sizing
<Trash2 className={isTouchDevice ? "icon-sm" : "icon-xs"} />
```

### Layout Spacing
```tsx
// Container with responsive spacing
<div className="p-md gap-lg">
  {/* Card with proper spacing */}
  <div className="card-md">
    <h2 className="text-xl mb-md">Title</h2>
    <p className="text-md">Content</p>
  </div>
</div>
```

## Migration Guide

### From Arbitrary Values
Replace hardcoded pixel values with sizing system classes:

```css
/* Before */
.component { 
  height: 44px; 
  padding: 12px 20px; 
  font-size: 15px; 
}

/* After */
.component { 
  height: var(--height-sm); 
  padding: var(--spacing-md) var(--spacing-lg); 
  font-size: var(--font-size-md); 
}

/* Or with utility classes */
.component {
  @apply h-sm px-lg py-md text-md;
}
```

### Common Conversions
| Old Value | New Class | CSS Variable |
|-----------|-----------|--------------|
| 4px       | `spacing-xs` | `var(--spacing-xs)` |
| 8px       | `spacing-xs` | `var(--spacing-xs)` |
| 12px      | `spacing-md` | `var(--spacing-md)` |
| 16px      | `spacing-sm` | `var(--spacing-sm)` |
| 20px      | `spacing-lg` | `var(--spacing-lg)` |
| 24px      | `spacing-md` | `var(--spacing-md)` |
| 32px      | `spacing-lg` | `var(--spacing-lg)` |
| 44px      | `touch-target-min` | `var(--touch-target-min)` |
| 48px      | `touch-target-sm` | `var(--touch-target-sm)` |

## Responsive Behavior

The system includes responsive adjustments for different device types:

### Mobile Adjustments
- Touch targets increase to minimum 48px
- Spacing can be adjusted with `mobile:` prefixes
- Font sizes remain consistent but line heights adjust

### Accessibility Features
- Minimum 44px touch targets on all interactive elements
- High contrast mode support
- Keyboard navigation spacing
- Screen reader friendly sizing

## Best Practices

1. **Always use the sizing system** - Avoid arbitrary pixel values
2. **Start with defaults** - Use `md` sizes as your baseline
3. **Scale consistently** - Move up/down the scale systematically
4. **Consider touch targets** - Ensure interactive elements meet minimums
5. **Test on devices** - Verify sizing works across different screen sizes
6. **Use presets** - Leverage component presets for common patterns

## Implementation Checklist

- [ ] Import `sizing-system.css` in your main stylesheet
- [ ] Update button components to use new size variants
- [ ] Replace arbitrary spacing with utility classes
- [ ] Ensure touch targets meet minimum requirements
- [ ] Update icon sizes to use the new scale
- [ ] Test responsive behavior across devices
- [ ] Validate accessibility compliance

## Files Updated

1. `/src/styles/sizing-system.css` - Main sizing system
2. `/src/index.css` - Import and mobile touch targets
3. `/src/components/ui/button.tsx` - Updated button variants
4. `/src/components/bento/GridComponent.tsx` - Standardized component sizing
5. `/src/styles/spacing.css` - Legacy compatibility layer