# Spacing System Guide

Our spacing system is based on an 8px grid to ensure visual consistency and clean, scalable designs.

## Core Principle

All spacing values are multiples of 8px (our base unit). This creates visual rhythm and makes it easier to maintain consistent spacing throughout the application.

## T-Shirt Sizes

We use intuitive t-shirt sizes for common spacing needs:

| Size  | Value | Use Case |
|-------|-------|----------|
| `xs`  | 4px   | Tight spacing, inline elements |
| `sm`  | 8px   | Default small spacing |
| `md`  | 16px  | Standard spacing between elements |
| `lg`  | 24px  | Section spacing |
| `xl`  | 32px  | Large section spacing |
| `2xl` | 40px  | Extra large spacing |
| `3xl` | 48px  | Hero sections |
| `4xl` | 64px  | Major section breaks |

## Usage in Tailwind

### Padding
```jsx
<div className="p-sm">     // 8px all sides
<div className="px-md">    // 16px horizontal
<div className="py-lg">    // 24px vertical
<div className="pt-xl">    // 32px top
```

### Margin
```jsx
<div className="m-sm">     // 8px all sides
<div className="mx-md">    // 16px horizontal
<div className="my-lg">    // 24px vertical
<div className="mt-xl">    // 32px top
```

### Gap (Flexbox/Grid)
```jsx
<div className="flex gap-sm">      // 8px gap
<div className="grid gap-md">      // 16px gap
```

### Width/Height
```jsx
<div className="w-64">     // 512px (64 * 8)
<div className="h-32">     // 256px (32 * 8)
<div className="min-h-16"> // 64px min height
```

## Common Patterns

### Page Layout
```jsx
<div className="p-lg">  // 24px page padding
```

### Card Component
```jsx
<div className="p-md rounded-lg">  // 16px padding, 12px radius
```

### Form Elements
```jsx
<input className="px-sm py-xs">  // 8px horizontal, 4px vertical
```

### Button Sizes
```jsx
<button className="px-md py-sm">     // Standard button
<button className="px-sm py-xs">     // Small button
<button className="px-lg py-md">     // Large button
```

### Table Cells
```jsx
<td className="px-md py-sm">  // Standard cell padding
<td className="px-md py-xs">  // Compact cell padding
```

## Numeric Values

For cases where t-shirt sizes don't fit, use numeric values:

- `p-1` = 4px (0.5 * 8)
- `p-2` = 8px (1 * 8)
- `p-3` = 12px (1.5 * 8)
- `p-4` = 16px (2 * 8)
- `p-5` = 20px (2.5 * 8)
- `p-6` = 24px (3 * 8)
- `p-8` = 32px (4 * 8)
- `p-10` = 40px (5 * 8)
- `p-12` = 48px (6 * 8)
- `p-16` = 64px (8 * 8)

## Best Practices

1. **Stick to the system**: Always use predefined spacing values
2. **Be consistent**: Use the same spacing for similar elements
3. **Think in multiples of 8**: When in doubt, round to nearest 8px
4. **Use t-shirt sizes first**: They're more semantic and easier to understand
5. **Document exceptions**: If you must break the grid, document why

## Component-Specific Guidelines

### Headers
- Main header: `h-16` (64px height)
- Subheaders: `h-12` (48px height)

### Sidebars
- Collapsed: `w-16` (64px)
- Expanded: `w-64` (256px)

### Modals
- Small: `max-w-md` (448px)
- Medium: `max-w-lg` (512px)
- Large: `max-w-2xl` (672px)

### Icons
- Small: `w-4 h-4` (16px)
- Default: `w-6 h-6` (24px)
- Large: `w-8 h-8` (32px)

## Migration Guide

When refactoring existing code:

1. Replace arbitrary values with nearest grid value
2. Use t-shirt sizes where possible
3. Test visual hierarchy after changes
4. Ensure touch targets remain accessible (min 44px)

Example migrations:
- `p-[14px]` → `p-md` (16px)
- `p-[18px]` → `p-md` (16px) or `p-5` (20px)
- `p-[30px]` → `p-xl` (32px)
- `gap-[15px]` → `gap-md` (16px)