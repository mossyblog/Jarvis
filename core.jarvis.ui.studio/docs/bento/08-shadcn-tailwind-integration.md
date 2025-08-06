# shadcn/ui & Tailwind CSS Integration Guide
## Complete Reference for Bento Grid Components

## Table of Contents
1. [Overview](#overview)
2. [Component Mapping](#component-mapping)
3. [Tailwind Utility Patterns](#tailwind-utility-patterns)
4. [Theme Customization](#theme-customization)
5. [Dark Mode Implementation](#dark-mode-implementation)
6. [Responsive Design Patterns](#responsive-design-patterns)
7. [Performance Optimization](#performance-optimization)
8. [Accessibility Guidelines](#accessibility-guidelines)
9. [Code Examples](#code-examples)
10. [Common Pitfalls](#common-pitfalls)
11. [shadcn MCP Tool Reference](#shadcn-mcp-tool-reference)

## Overview

The Bento Grid System leverages shadcn/ui components and Tailwind CSS to create a cohesive, scalable design system. This guide provides the definitive reference for building dynamic forms and components within the Bento ecosystem.

### Design Principles
- **Component Reusability**: Build once, use everywhere
- **Type Safety**: Full TypeScript integration
- **Performance First**: Optimized for rapid development cycles
- **Accessible by Default**: WCAG 2.1 AA compliance
- **Mobile-First**: Touch-friendly and responsive

### Core Dependencies
```json
{
  "@radix-ui/react-*": "Latest",
  "class-variance-authority": "^0.7.0", 
  "clsx": "^2.0.0",
  "tailwind-merge": "^2.0.0",
  "tailwindcss-animate": "^1.0.7"
}
```

## Component Mapping

### ECS Field Types → shadcn Components

| ECS Field Type | shadcn Component | Use Case | Grid Size Rec. |
|---|---|---|---|
| `string` | `Input`, `Textarea` | Text input, descriptions | 1x1, 2x1 |
| `number` | `Input[type="number"]` | Numeric values | 1x1 |
| `boolean` | `Switch`, `Checkbox` | Toggle states | 1x1 |
| `date` | `Calendar`, `DatePicker` | Date selection | 2x2 |
| `enum` | `Select`, `RadioGroup` | Single choice | 1x1, 2x1 |
| `array` | `MultiSelect`, `CheckboxGroup` | Multiple choice | 2x2 |
| `object` | `Card` with nested fields | Complex data | 2x2+ |
| `file` | `FileUpload` | File handling | 2x1 |
| `color` | Custom `ColorPicker` | Color selection | 1x1 |
| `range` | `Slider` | Numeric ranges | 2x1 |
| `text` | `RichTextEditor` | Formatted content | 3x2+ |
| `json` | `JSONEditor` | Structured data | 3x3+ |

### Component Hierarchy

```typescript
// Base component interface for Bento Grid
interface BentoComponentProps {
  className?: string;
  gridSize?: { w: number; h: number };
  deviceType?: DeviceType;
  data?: unknown;
  loading?: boolean;
  error?: Error;
  onReady?: () => void;
  onError?: (error: Error) => void;
  onAction?: (action: ComponentAction) => void;
}

// ECS field-specific props
interface ECSFieldProps extends BentoComponentProps {
  fieldType: string;
  fieldName: string;
  label?: string;
  placeholder?: string;
  required?: boolean;
  validation?: ValidationRule[];
  onChange?: (value: unknown) => void;
}
```

### Component Categories

#### 1. Form Components
```typescript
// Text Input Component
const TextInputComponent: React.FC<ECSFieldProps> = ({
  fieldName,
  label,
  placeholder,
  required,
  className,
  ...props
}) => (
  <Card className={cn('h-full', className)}>
    <CardContent className="p-4">
      <div className="space-y-2">
        <Label htmlFor={fieldName} className={cn({ "required": required })}>
          {label}
        </Label>
        <Input
          id={fieldName}
          placeholder={placeholder}
          required={required}
          className="w-full"
          {...props}
        />
      </div>
    </CardContent>
  </Card>
);

// Select Component  
const SelectComponent: React.FC<ECSFieldProps & {
  options: Array<{ value: string; label: string }>;
}> = ({ fieldName, label, options, className, ...props }) => (
  <Card className={cn('h-full', className)}>
    <CardContent className="p-4">
      <div className="space-y-2">
        <Label htmlFor={fieldName}>{label}</Label>
        <Select>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select an option" />
          </SelectTrigger>
          <SelectContent>
            {options.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </CardContent>
  </Card>
);
```

#### 2. Display Components
```typescript
// Metric Display Component
const MetricComponent: React.FC<BentoComponentProps & {
  title: string;
  value: string | number;
  change?: string;
  trend?: 'up' | 'down' | 'neutral';
}> = ({ title, value, change, trend, className }) => (
  <Card className={cn('h-full', className)}>
    <CardHeader className="pb-2">
      <CardTitle className="text-sm font-medium text-muted-foreground">
        {title}
      </CardTitle>
    </CardHeader>
    <CardContent className="pt-0">
      <div className="text-3xl font-bold">{value}</div>
      {change && (
        <div className={cn(
          "text-sm font-medium flex items-center mt-2",
          trend === 'up' && "text-green-600 dark:text-green-400",
          trend === 'down' && "text-red-600 dark:text-red-400",
          trend === 'neutral' && "text-muted-foreground"
        )}>
          <TrendIcon trend={trend} className="mr-1" />
          {change}
        </div>
      )}
    </CardContent>
  </Card>
);

// Chart Container Component
const ChartComponent: React.FC<BentoComponentProps & {
  title: string;
  chartType: 'line' | 'bar' | 'pie' | 'area';
  data: unknown[];
}> = ({ title, chartType, data, className }) => (
  <Card className={cn('h-full', className)}>
    <CardHeader>
      <CardTitle className="text-base">{title}</CardTitle>
    </CardHeader>
    <CardContent className="h-[calc(100%-60px)]">
      <ChartRenderer type={chartType} data={data} />
    </CardContent>
  </Card>
);
```

#### 3. Interactive Components
```typescript
// Action Button Component
const ActionButtonComponent: React.FC<BentoComponentProps & {
  title: string;
  description?: string;
  icon?: React.ComponentType;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary';
  onAction?: () => void;
}> = ({ title, description, icon: Icon, variant = 'default', onAction, className }) => (
  <Card className={cn('h-full cursor-pointer hover:shadow-md transition-all', className)}>
    <CardContent className="flex flex-col items-center justify-center h-full p-4 text-center">
      {Icon && <Icon className="h-8 w-8 mb-2 text-primary" />}
      <h3 className="font-semibold mb-1">{title}</h3>
      {description && (
        <p className="text-sm text-muted-foreground mb-3">{description}</p>
      )}
      <Button variant={variant} onClick={onAction} className="w-full">
        Execute
      </Button>
    </CardContent>
  </Card>
);
```

## Tailwind Utility Patterns

### Grid Layout Utilities

```css
/* Core Bento Grid Classes */
.bento-grid {
  @apply grid gap-4 p-4 auto-rows-[minmax(120px,auto)];
}

.bento-grid-desktop {
  @apply grid-cols-12;
}

.bento-grid-tablet {
  @apply grid-cols-8;
}

.bento-grid-mobile {
  @apply grid-cols-4;
}

/* Component Size Classes */
.bento-1x1 { @apply col-span-1 row-span-1; }
.bento-2x1 { @apply col-span-2 row-span-1; }
.bento-1x2 { @apply col-span-1 row-span-2; }
.bento-2x2 { @apply col-span-2 row-span-2; }
.bento-3x2 { @apply col-span-3 row-span-2; }
.bento-4x2 { @apply col-span-4 row-span-2; }

/* Responsive Size Classes */
@media (min-width: 768px) {
  .bento-md-2x1 { @apply md:col-span-2 md:row-span-1; }
  .bento-md-3x2 { @apply md:col-span-3 md:row-span-2; }
}

@media (min-width: 1024px) {
  .bento-lg-4x2 { @apply lg:col-span-4 lg:row-span-2; }
  .bento-lg-6x3 { @apply lg:col-span-6 lg:row-span-3; }
}
```

### Component State Utilities

```css
/* Loading States */
.loading-shimmer {
  @apply bg-gradient-to-r from-muted via-muted/50 to-muted;
  @apply bg-[length:200%_100%] animate-[shimmer_2s_infinite];
}

@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

/* Interaction States */
.bento-component {
  @apply transition-all duration-200 ease-out;
  @apply hover:shadow-lg hover:-translate-y-1;
  @apply focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2;
}

.bento-component--dragging {
  @apply opacity-50 rotate-3 scale-105 z-50;
}

.bento-component--drop-target {
  @apply border-2 border-dashed border-primary bg-primary/10;
}

/* Form States */
.form-field--error {
  @apply border-destructive bg-destructive/10;
}

.form-field--success {
  @apply border-green-500 bg-green-50 dark:bg-green-950/20;
}

.form-field--loading {
  @apply opacity-50 pointer-events-none;
}
```

### Spacing System

Our 8px-based spacing system ensures consistent layouts:

```css
/* Custom spacing scale (extending Tailwind) */
.space-unit { @apply space-y-2; } /* 8px */
.space-compact { @apply space-y-1; } /* 4px */
.space-comfortable { @apply space-y-3; } /* 12px */
.space-relaxed { @apply space-y-4; } /* 16px */

/* Component-specific spacing */
.bento-component-padding {
  @apply p-4; /* 16px - optimal for most components */
}

.bento-component-padding-sm {
  @apply p-2; /* 8px - for compact components */
}

.bento-component-padding-lg {
  @apply p-6; /* 24px - for feature components */
}
```

## Theme Customization

### CSS Variables Integration

Our theme system uses CSS variables for maximum flexibility:

```css
/* Core theme variables in index.css */
:root {
  /* Jarvis Brand Colors */
  --brand: 210 100% 50%;
  --brand-600: 210 100% 40%;
  
  /* Extended semantic colors */
  --success: 142 76% 36%;
  --warning: 45 93% 47%;
  --info: 199 89% 48%;
  
  /* Component-specific variables */
  --bento-grid-gap: 1rem;
  --bento-border-radius: 0.5rem;
  --bento-component-min-height: 120px;
  
  /* Animation timing */
  --transition-fast: 150ms;
  --transition-normal: 200ms;
  --transition-slow: 300ms;
}

.dark {
  /* Dark mode overrides */
  --brand: 210 100% 60%;
  --success: 142 70% 45%;
  --warning: 45 90% 55%;
}
```

### Dynamic Theme Provider

```typescript
// Enhanced theme context for Bento Grid
interface BentoThemeContext {
  theme: 'light' | 'dark' | 'system';
  colors: {
    primary: string;
    secondary: string;
    accent: string;
  };
  spacing: {
    gridGap: string;
    componentPadding: string;
  };
  animations: {
    enabled: boolean;
    duration: 'fast' | 'normal' | 'slow';
  };
}

const BentoThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [theme, setTheme] = useState<BentoThemeContext>({
    theme: 'system',
    colors: {
      primary: 'hsl(var(--primary))',
      secondary: 'hsl(var(--secondary))', 
      accent: 'hsl(var(--accent))',
    },
    spacing: {
      gridGap: 'var(--bento-grid-gap)',
      componentPadding: 'var(--spacing-md)',
    },
    animations: {
      enabled: true,
      duration: 'normal',
    },
  });

  return (
    <BentoThemeContext.Provider value={theme}>
      <div 
        className={cn('min-h-screen bg-background font-sans antialiased', {
          'dark': theme.theme === 'dark',
          'animate-none': !theme.animations.enabled,
        })}
        style={{
          '--bento-grid-gap': theme.spacing.gridGap,
          '--bento-component-padding': theme.spacing.componentPadding,
        } as React.CSSProperties}
      >
        {children}
      </div>
    </BentoThemeContext.Provider>
  );
};
```

### Brand Customization

```typescript
// Brand configuration for different clients/deployments
interface BrandConfig {
  name: string;
  colors: {
    primary: [number, number, number]; // HSL values
    secondary: [number, number, number];
    accent: [number, number, number];
  };
  typography: {
    fontFamily: string;
    headingWeight: number;
    bodyWeight: number;
  };
  spacing: {
    scale: number; // Multiplier for base 8px scale
  };
  animations: {
    easing: string;
    duration: number;
  };
}

const applyBrandTheme = (brand: BrandConfig) => {
  const root = document.documentElement;
  
  // Apply color variables
  root.style.setProperty('--primary', `${brand.colors.primary.join(' ')}`);
  root.style.setProperty('--secondary', `${brand.colors.secondary.join(' ')}`);
  root.style.setProperty('--accent', `${brand.colors.accent.join(' ')}`);
  
  // Apply typography
  root.style.setProperty('--font-sans', brand.typography.fontFamily);
  root.style.setProperty('--font-weight-heading', brand.typography.headingWeight.toString());
  root.style.setProperty('--font-weight-body', brand.typography.bodyWeight.toString());
  
  // Apply spacing scale
  root.style.setProperty('--spacing-scale', brand.spacing.scale.toString());
  
  // Apply animation properties
  root.style.setProperty('--transition-timing', brand.animations.easing);
  root.style.setProperty('--transition-duration', `${brand.animations.duration}ms`);
};
```

## Dark Mode Implementation

### Automatic Theme Detection

```typescript
const useThemeDetection = () => {
  const [theme, setTheme] = useState<'light' | 'dark' | 'system'>('system');
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light');
  
  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    const updateTheme = () => {
      if (theme === 'system') {
        setResolvedTheme(mediaQuery.matches ? 'dark' : 'light');
      } else {
        setResolvedTheme(theme);
      }
    };
    
    updateTheme();
    mediaQuery.addEventListener('change', updateTheme);
    
    return () => mediaQuery.removeEventListener('change', updateTheme);
  }, [theme]);
  
  useEffect(() => {
    document.documentElement.classList.toggle('dark', resolvedTheme === 'dark');
  }, [resolvedTheme]);
  
  return { theme, setTheme, resolvedTheme };
};
```

### Component Dark Mode Patterns

```typescript
// Dark mode variants for components
const cardVariants = cva(
  "border bg-card text-card-foreground shadow-sm",
  {
    variants: {
      variant: {
        default: "border-border",
        ghost: "border-transparent shadow-none",
        elevated: "shadow-lg border-border/50",
      },
      darkMode: {
        auto: "", // Uses CSS variables automatically
        light: "bg-white border-gray-200 text-gray-900",
        dark: "bg-gray-800 border-gray-700 text-gray-100",
      }
    },
    defaultVariants: {
      variant: "default",
      darkMode: "auto",
    },
  }
);

// Usage in components
const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, variant, darkMode, ...props }, ref) => (
    <div
      ref={ref}
      className={cn(cardVariants({ variant, darkMode }), className)}
      {...props}
    />
  )
);
```

### Dark Mode Optimized Colors

```css
/* Color system optimized for both themes */
:root {
  /* Light mode (default) */
  --background: 0 0% 100%;
  --foreground: 240 10% 3.9%;
  --card: 0 0% 100%;
  --card-foreground: 240 10% 3.9%;
  --border: 240 5.9% 90%;
  --muted: 240 4.8% 95.9%;
  
  /* Bento-specific colors */
  --bento-grid-bg: var(--background);
  --bento-component-bg: var(--card);
  --bento-component-border: var(--border);
  --bento-drag-overlay: 210 100% 50% / 0.1;
}

.dark {
  /* Dark mode overrides */
  --background: 240 10% 3.9%;
  --foreground: 0 0% 98%;
  --card: 240 10% 3.9%;
  --card-foreground: 0 0% 98%;
  --border: 240 3.7% 15.9%;
  --muted: 240 3.7% 15.9%;
  
  /* Dark mode Bento colors */
  --bento-grid-bg: 240 10% 3.9%;
  --bento-component-bg: 240 6% 10%;
  --bento-component-border: 240 3.7% 15.9%;
  --bento-drag-overlay: 210 100% 70% / 0.2;
}

/* High contrast mode support */
@media (prefers-contrast: high) {
  :root {
    --border: 240 5.9% 70%;
  }
  
  .dark {
    --border: 240 3.7% 25.9%;
  }
}
```

## Responsive Design Patterns

### Breakpoint Strategy

```typescript
// Responsive breakpoints aligned with Tailwind
const breakpoints = {
  mobile: '(max-width: 767px)',
  tablet: '(min-width: 768px) and (max-width: 1023px)', 
  desktop: '(min-width: 1024px)',
  // Touch-specific
  touch: '(hover: none) and (pointer: coarse)',
  hover: '(hover: hover) and (pointer: fine)',
} as const;

// Hook for responsive behavior
const useResponsive = () => {
  const [device, setDevice] = useState<'mobile' | 'tablet' | 'desktop'>('desktop');
  const [hasTouch, setHasTouch] = useState(false);
  
  useEffect(() => {
    const queries = [
      { query: breakpoints.mobile, device: 'mobile' as const },
      { query: breakpoints.tablet, device: 'tablet' as const },
      { query: breakpoints.desktop, device: 'desktop' as const },
    ];
    
    const touchQuery = window.matchMedia(breakpoints.touch);
    
    const updateDevice = () => {
      const match = queries.find(q => window.matchMedia(q.query).matches);
      setDevice(match?.device || 'desktop');
      setHasTouch(touchQuery.matches);
    };
    
    updateDevice();
    
    const mediaQueries = queries.map(q => window.matchMedia(q.query));
    mediaQueries.forEach(mq => mq.addEventListener('change', updateDevice));
    touchQuery.addEventListener('change', updateDevice);
    
    return () => {
      mediaQueries.forEach(mq => mq.removeEventListener('change', updateDevice));
      touchQuery.removeEventListener('change', updateDevice);
    };
  }, []);
  
  return { device, hasTouch };
};
```

### Grid Layout Responsiveness

```typescript
// Responsive grid layout configuration
interface ResponsiveGridConfig {
  mobile: { columns: number; minComponentSize: number };
  tablet: { columns: number; minComponentSize: number };
  desktop: { columns: number; minComponentSize: number };
}

const defaultGridConfig: ResponsiveGridConfig = {
  mobile: { columns: 4, minComponentSize: 120 },
  tablet: { columns: 8, minComponentSize: 140 },
  desktop: { columns: 12, minComponentSize: 160 },
};

// Responsive grid component
const ResponsiveBentoGrid: React.FC<{
  children: React.ReactNode;
  config?: Partial<ResponsiveGridConfig>;
}> = ({ children, config = {} }) => {
  const { device } = useResponsive();
  const gridConfig = { ...defaultGridConfig, ...config };
  const currentConfig = gridConfig[device];
  
  return (
    <div
      className={cn(
        'bento-grid',
        `grid-cols-${currentConfig.columns}`,
        'gap-4 p-4 auto-rows-[minmax(120px,auto)]',
        device === 'mobile' && 'gap-2 p-2',
        device === 'tablet' && 'gap-3 p-3',
      )}
      style={{
        '--min-component-size': `${currentConfig.minComponentSize}px`,
        gridTemplateRows: `repeat(auto-fit, minmax(${currentConfig.minComponentSize}px, auto))`,
      } as React.CSSProperties}
    >
      {children}
    </div>
  );
};
```

### Component Responsive Behavior

```typescript
// Responsive component wrapper
const ResponsiveBentoComponent: React.FC<{
  component: GridComponent;
  mobileSize?: { w: number; h: number };
  tabletSize?: { w: number; h: number };
  desktopSize?: { w: number; h: number };
}> = ({ component, mobileSize, tabletSize, desktopSize }) => {
  const { device } = useResponsive();
  
  const getResponsiveSize = () => {
    switch (device) {
      case 'mobile': return mobileSize || { w: 4, h: 1 }; // Full width on mobile
      case 'tablet': return tabletSize || { w: 4, h: 2 };
      case 'desktop': return desktopSize || component.size;
    }
  };
  
  const size = getResponsiveSize();
  
  return (
    <div
      className={cn(
        'bento-component',
        `col-span-${size.w} row-span-${size.h}`,
        // Touch-specific classes
        device === 'mobile' && 'bento-component--mobile',
      )}
      style={{
        gridColumn: `span ${size.w}`,
        gridRow: `span ${size.h}`,
      }}
    >
      <ComponentRenderer
        component={component}
        gridSize={size}
        deviceType={device}
      />
    </div>
  );
};
```

### Touch-First Design

```css
/* Touch-optimized component styles */
.bento-component--mobile {
  /* Ensure adequate touch targets */
  min-height: var(--touch-target-min, 44px);
  min-width: var(--touch-target-min, 44px);
  
  /* Improve touch interactions */
  touch-action: manipulation;
  -webkit-tap-highlight-color: transparent;
}

/* Touch-specific interaction states */
@media (hover: none) and (pointer: coarse) {
  .bento-component:hover {
    /* Disable hover effects on touch devices */
    transform: none;
    box-shadow: inherit;
  }
  
  .bento-component:active {
    /* Provide touch feedback */
    transform: scale(0.98);
    transition: transform 100ms ease-out;
  }
  
  /* Always show interaction elements on touch */
  .bento-component__drag-handle,
  .bento-component__resize-handle {
    opacity: 1 !important;
    transform: scale(1) !important;
  }
}
```

## Performance Optimization

### Component Lazy Loading

```typescript
// Lazy loading strategy for heavy components
const LazyComponentRegistry = {
  // Light components - load immediately
  text: React.lazy(() => import('./components/TextComponent')),
  metric: React.lazy(() => import('./components/MetricComponent')),
  
  // Heavy components - lazy load
  chart: React.lazy(() => import('./components/ChartComponent')),
  table: React.lazy(() => import('./components/TableComponent')),
  map: React.lazy(() => import('./components/MapComponent')),
};

// Performance-optimized component renderer
const OptimizedComponentRenderer: React.FC<ComponentRendererProps> = ({
  component,
  ...props
}) => {
  const LazyComponent = LazyComponentRegistry[component.componentType];
  
  if (!LazyComponent) {
    return <ComponentNotFoundFallback componentType={component.componentType} />;
  }
  
  return (
    <Suspense 
      fallback={
        <ComponentLoadingFallback 
          message={`Loading ${component.componentType}...`}
        />
      }
    >
      <LazyComponent {...props} />
    </Suspense>
  );
};
```

### Virtual Grid Rendering

```typescript
// Virtual scrolling for large grids
const VirtualBentoGrid: React.FC<{
  components: GridComponent[];
  viewportHeight: number;
  rowHeight: number;
}> = ({ components, viewportHeight, rowHeight }) => {
  const [scrollTop, setScrollTop] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  
  // Calculate visible range
  const startIndex = Math.floor(scrollTop / rowHeight);
  const endIndex = Math.min(
    startIndex + Math.ceil(viewportHeight / rowHeight) + 1,
    components.length
  );
  
  const visibleComponents = components.slice(startIndex, endIndex);
  
  return (
    <div
      ref={containerRef}
      className="overflow-auto"
      style={{ height: viewportHeight }}
      onScroll={(e) => setScrollTop(e.currentTarget.scrollTop)}
    >
      <div style={{ height: components.length * rowHeight, position: 'relative' }}>
        {visibleComponents.map((component, index) => (
          <div
            key={component.id}
            style={{
              position: 'absolute',
              top: (startIndex + index) * rowHeight,
              left: 0,
              right: 0,
              height: rowHeight,
            }}
          >
            <OptimizedComponentRenderer component={component} />
          </div>
        ))}
      </div>
    </div>
  );
};
```

### Bundle Optimization

```typescript
// Tree-shakeable component exports
export { Button } from './components/Button';
export { Input } from './components/Input';
export { Card } from './components/Card';

// Avoid barrel exports for better tree-shaking
// ❌ Don't do this
// export * from './components';

// ✅ Do this instead
export { 
  Button,
  type ButtonProps,
  buttonVariants 
} from './components/Button';
```

### CSS Optimization

```css
/* Use container queries for better performance */
.bento-component {
  container-type: inline-size;
}

@container (min-width: 300px) {
  .bento-component__content {
    display: flex;
    flex-direction: row;
  }
}

@container (max-width: 299px) {
  .bento-component__content {
    display: block;
  }
}

/* Optimize animations for 60fps */
.bento-component {
  /* Use transform instead of changing layout properties */
  transition: transform 200ms ease-out, opacity 200ms ease-out;
  will-change: transform, opacity;
}

.bento-component:hover {
  transform: translateY(-2px);
}

/* Enable hardware acceleration selectively */
.bento-component--dragging {
  transform: translateZ(0); /* Force GPU layer */
}
```

## Accessibility Guidelines

### ARIA Implementation

```typescript
// Comprehensive ARIA support for Bento components
const AccessibleBentoComponent: React.FC<BentoComponentProps & {
  component: GridComponent;
  role?: string;
  ariaLabel?: string;
  ariaDescription?: string;
}> = ({ 
  component, 
  role = 'region',
  ariaLabel,
  ariaDescription,
  ...props 
}) => {
  const componentId = `bento-${component.id}`;
  const labelId = `${componentId}-label`;
  const descriptionId = `${componentId}-description`;
  
  return (
    <div
      id={componentId}
      role={role}
      aria-labelledby={ariaLabel ? labelId : undefined}
      aria-describedby={ariaDescription ? descriptionId : undefined}
      tabIndex={component.interactive ? 0 : -1}
      className={cn(
        'bento-component',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2'
      )}
    >
      {ariaLabel && (
        <div id={labelId} className="sr-only">
          {ariaLabel}
        </div>
      )}
      {ariaDescription && (
        <div id={descriptionId} className="sr-only">
          {ariaDescription}
        </div>
      )}
      <ComponentRenderer component={component} {...props} />
    </div>
  );
};
```

### Keyboard Navigation

```typescript
// Keyboard navigation for grid editing
const useKeyboardNavigation = (gridRef: React.RefObject<HTMLElement>) => {
  useEffect(() => {
    const grid = gridRef.current;
    if (!grid) return;
    
    const handleKeyDown = (event: KeyboardEvent) => {
      const { key, target } = event;
      const activeElement = target as HTMLElement;
      
      // Grid navigation
      if (key.startsWith('Arrow')) {
        event.preventDefault();
        
        const components = Array.from(
          grid.querySelectorAll('[role="region"]')
        ) as HTMLElement[];
        
        const currentIndex = components.indexOf(activeElement);
        if (currentIndex === -1) return;
        
        let newIndex: number;
        switch (key) {
          case 'ArrowRight':
            newIndex = Math.min(currentIndex + 1, components.length - 1);
            break;
          case 'ArrowLeft':
            newIndex = Math.max(currentIndex - 1, 0);
            break;
          case 'ArrowDown':
            // Calculate next row
            newIndex = Math.min(currentIndex + 4, components.length - 1); // Assuming 4 columns
            break;
          case 'ArrowUp':
            // Calculate previous row
            newIndex = Math.max(currentIndex - 4, 0);
            break;
          default:
            return;
        }
        
        components[newIndex]?.focus();
      }
      
      // Action keys
      if (key === 'Enter' || key === ' ') {
        event.preventDefault();
        // Trigger component action
        const button = activeElement.querySelector('button');
        button?.click();
      }
    };
    
    grid.addEventListener('keydown', handleKeyDown);
    return () => grid.removeEventListener('keydown', handleKeyDown);
  }, [gridRef]);
};
```

### Screen Reader Support

```typescript
// Enhanced screen reader announcements
const useScreenReaderAnnouncements = () => {
  const [announcement, setAnnouncement] = useState('');
  
  const announce = (message: string, priority: 'polite' | 'assertive' = 'polite') => {
    setAnnouncement(`${priority === 'assertive' ? '!' : ''}${message}`);
    
    // Clear announcement after it's been read
    setTimeout(() => setAnnouncement(''), 1000);
  };
  
  return { 
    announcement, 
    announce,
    AnnouncementRegion: () => (
      <div
        aria-live="polite"
        aria-atomic="true"
        className="sr-only"
      >
        {announcement}
      </div>
    )
  };
};

// Usage in grid operations
const BentoGridEditor: React.FC = () => {
  const { announce, AnnouncementRegion } = useScreenReaderAnnouncements();
  
  const handleComponentMove = (componentId: string, newPosition: Position) => {
    // Perform move operation
    moveComponent(componentId, newPosition);
    
    // Announce to screen reader
    announce(
      `Component moved to row ${newPosition.row}, column ${newPosition.col}`,
      'polite'
    );
  };
  
  return (
    <div>
      <BentoGrid onComponentMove={handleComponentMove} />
      <AnnouncementRegion />
    </div>
  );
};
```

### Color Contrast and Visual Accessibility

```css
/* Ensure WCAG AA compliance */
:root {
  --text-high-contrast: 240 10% 3.9%; /* 21:1 ratio */
  --text-medium-contrast: 240 5% 25%; /* 7:1 ratio */
  --text-low-contrast: 240 4% 46%; /* 4.5:1 ratio */
  
  --border-high-contrast: 240 5% 65%;
  --border-medium-contrast: 240 6% 80%;
  --border-low-contrast: 240 4.8% 95.9%;
}

.dark {
  --text-high-contrast: 0 0% 98%;
  --text-medium-contrast: 240 5% 84%;
  --text-low-contrast: 240 5% 64.9%;
  
  --border-high-contrast: 240 3.7% 35%;
  --border-medium-contrast: 240 3.7% 25%;
  --border-low-contrast: 240 3.7% 15.9%;
}

/* Focus indicators with high contrast */
.focus-ring {
  @apply focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2;
}

.dark .focus-ring {
  @apply focus-visible:ring-blue-400;
}

/* High contrast mode support */
@media (prefers-contrast: high) {
  :root {
    --border: var(--border-high-contrast);
    --muted-foreground: var(--text-medium-contrast);
  }
  
  .bento-component {
    border-width: 2px;
  }
}
```

## Code Examples

### Complete Form Component

```typescript
// Complete example: User Profile Form Component
interface UserProfileFormProps extends BentoComponentProps {
  initialData?: {
    name?: string;
    email?: string;
    role?: string;
    preferences?: {
      theme: 'light' | 'dark';
      notifications: boolean;
    };
  };
  onSubmit?: (data: UserProfile) => Promise<void>;
}

const UserProfileFormComponent: React.FC<UserProfileFormProps> = ({
  initialData,
  onSubmit,
  className,
  gridSize,
  loading,
  ...props
}) => {
  const form = useForm<UserProfile>({
    resolver: zodResolver(userProfileSchema),
    defaultValues: initialData,
  });
  
  const { announce } = useScreenReaderAnnouncements();
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const handleSubmit = async (data: UserProfile) => {
    if (!onSubmit) return;
    
    setIsSubmitting(true);
    try {
      await onSubmit(data);
      announce('Profile updated successfully', 'polite');
      toast.success('Profile updated!');
    } catch (error) {
      announce('Error updating profile', 'assertive');
      toast.error('Failed to update profile');
    } finally {
      setIsSubmitting(false);
    }
  };
  
  return (
    <Card className={cn('h-full', className)}>
      <CardHeader>
        <CardTitle>User Profile</CardTitle>
        <CardDescription>
          Update your profile information and preferences
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
            {/* Name Field */}
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Name</FormLabel>
                  <FormControl>
                    <Input 
                      placeholder="Enter your name" 
                      {...field}
                      aria-describedby="name-description"
                    />
                  </FormControl>
                  <FormDescription id="name-description">
                    Your full name as it appears on your profile
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
            
            {/* Email Field */}
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email</FormLabel>
                  <FormControl>
                    <Input 
                      type="email"
                      placeholder="Enter your email" 
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            
            {/* Role Selection */}
            <FormField
              control={form.control}
              name="role"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Role</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select your role" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="admin">Administrator</SelectItem>
                      <SelectItem value="user">User</SelectItem>
                      <SelectItem value="viewer">Viewer</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            
            {/* Preferences Section */}
            <div className="space-y-3">
              <h4 className="text-sm font-medium">Preferences</h4>
              
              {/* Theme Toggle */}
              <FormField
                control={form.control}
                name="preferences.theme"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                    <div className="space-y-0.5">
                      <FormLabel>Dark Mode</FormLabel>
                      <FormDescription>
                        Enable dark mode interface
                      </FormDescription>
                    </div>
                    <FormControl>
                      <Switch
                        checked={field.value === 'dark'}
                        onCheckedChange={(checked) => 
                          field.onChange(checked ? 'dark' : 'light')
                        }
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
              
              {/* Notifications Toggle */}
              <FormField
                control={form.control}
                name="preferences.notifications"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                    <div className="space-y-0.5">
                      <FormLabel>Notifications</FormLabel>
                      <FormDescription>
                        Receive email notifications
                      </FormDescription>
                    </div>
                    <FormControl>
                      <Switch
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
            </div>
            
            {/* Submit Button */}
            <div className="flex justify-end space-x-2">
              <Button 
                type="button" 
                variant="outline"
                onClick={() => form.reset()}
                disabled={isSubmitting}
              >
                Reset
              </Button>
              <Button 
                type="submit"
                disabled={isSubmitting || loading}
                className="min-w-[100px]"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  'Save Changes'
                )}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
};

// Component registration for Bento Grid
export const userProfileFormComponentConfig: ComponentConfig = {
  type: 'user-profile-form',
  name: 'User Profile Form',
  category: 'Forms',
  icon: User,
  description: 'Comprehensive user profile editing form',
  constraints: {
    minSize: { w: 2, h: 3 },
    maxSize: { w: 4, h: 4 },
    preferredSize: { w: 3, h: 4 },
  },
  props: {
    initialData: {
      type: 'object',
      description: 'Initial form data',
      required: false,
    },
    onSubmit: {
      type: 'function',
      description: 'Form submission handler',
      required: true,
    },
  },
  responsive: {
    mobile: { w: 4, h: 4 }, // Full width on mobile
    tablet: { w: 4, h: 3 },
    desktop: { w: 3, h: 4 },
  },
  accessibility: {
    role: 'form',
    ariaLabel: 'User profile form',
    keyboardNavigation: true,
    screenReaderOptimized: true,
  },
};
```

### Dynamic Data Table Component

```typescript
// Advanced data table with filtering, sorting, and pagination
interface DataTableProps<T> extends BentoComponentProps {
  data: T[];
  columns: ColumnDef<T>[];
  pageSize?: number;
  searchable?: boolean;
  filterable?: boolean;
  exportable?: boolean;
  onRowSelect?: (rows: T[]) => void;
  onRowAction?: (action: string, row: T) => void;
}

const DataTableComponent = <T,>({
  data,
  columns,
  pageSize = 10,
  searchable = true,
  filterable = true,
  exportable = false,
  onRowSelect,
  onRowAction,
  className,
  gridSize,
  ...props
}: DataTableProps<T>) => {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [globalFilter, setGlobalFilter] = useState('');
  const [rowSelection, setRowSelection] = useState({});
  
  const table = useReactTable({
    data,
    columns,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onGlobalFilterChange: setGlobalFilter,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    state: {
      sorting,
      columnFilters,
      globalFilter,
      rowSelection,
    },
    initialState: {
      pagination: { pageSize },
    },
  });
  
  // Announce changes for screen readers
  const { announce } = useScreenReaderAnnouncements();
  
  useEffect(() => {
    const selectedRows = table.getSelectedRowModel().rows;
    onRowSelect?.(selectedRows.map(row => row.original));
    
    if (selectedRows.length > 0) {
      announce(`${selectedRows.length} rows selected`, 'polite');
    }
  }, [rowSelection, onRowSelect, announce, table]);
  
  return (
    <Card className={cn('h-full flex flex-col', className)}>
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>Data Table</CardTitle>
            <CardDescription>
              {table.getFilteredRowModel().rows.length} of {data.length} rows
            </CardDescription>
          </div>
          
          <div className="flex items-center gap-2">
            {exportable && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  // Export functionality
                  const csvData = generateCSV(table.getFilteredRowModel().rows);
                  downloadCSV(csvData, 'table-export.csv');
                  announce('Table exported successfully', 'polite');
                }}
              >
                <Download className="h-4 w-4 mr-2" />
                Export
              </Button>
            )}
          </div>
        </div>
        
        {/* Search and Filters */}
        {(searchable || filterable) && (
          <div className="flex items-center gap-2">
            {searchable && (
              <div className="relative flex-1">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search all columns..."
                  value={globalFilter}
                  onChange={(e) => setGlobalFilter(e.target.value)}
                  className="pl-8"
                />
              </div>
            )}
            
            {filterable && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm">
                    <Filter className="h-4 w-4 mr-2" />
                    Filters
                    {columnFilters.length > 0 && (
                      <Badge variant="secondary" className="ml-2 h-5 w-5 p-0">
                        {columnFilters.length}
                      </Badge>
                    )}
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-[200px]">
                  {table.getAllColumns()
                    .filter(column => column.getCanFilter())
                    .map(column => (
                      <DropdownMenuItem key={column.id} asChild>
                        <div className="flex items-center space-x-2">
                          <Checkbox
                            checked={column.getIsVisible()}
                            onCheckedChange={(checked) =>
                              column.toggleVisibility(!!checked)
                            }
                          />
                          <span className="capitalize">{column.id}</span>
                        </div>
                      </DropdownMenuItem>
                    ))
                  }
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        )}
      </CardHeader>
      
      <CardContent className="flex-1 overflow-hidden">
        <div className="border rounded-md h-full flex flex-col">
          {/* Table Header */}
          <div className="border-b bg-muted/50">
            {table.getHeaderGroups().map(headerGroup => (
              <div key={headerGroup.id} className="flex">
                {headerGroup.headers.map(header => (
                  <div
                    key={header.id}
                    className={cn(
                      'px-4 py-3 text-left font-medium text-sm',
                      'border-r border-muted-foreground/10 last:border-r-0',
                      header.column.getCanSort() && 'cursor-pointer select-none hover:bg-muted/80'
                    )}
                    style={{ width: header.getSize() }}
                    onClick={header.column.getToggleSortingHandler()}
                  >
                    <div className="flex items-center space-x-2">
                      {flexRender(header.column.columnDef.header, header.getContext())}
                      {header.column.getCanSort() && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-auto p-0"
                          aria-label={`Sort by ${header.column.id}`}
                        >
                          {{
                            asc: <ChevronUp className="h-4 w-4" />,
                            desc: <ChevronDown className="h-4 w-4" />,
                          }[header.column.getIsSorted() as string] ?? (
                            <ChevronsUpDown className="h-4 w-4" />
                          )}
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            ))}
          </div>
          
          {/* Table Body */}
          <div className="flex-1 overflow-auto">
            {table.getRowModel().rows.length === 0 ? (
              <div className="flex items-center justify-center h-32 text-muted-foreground">
                <div className="text-center">
                  <FileX className="h-8 w-8 mx-auto mb-2" />
                  <p>No data available</p>
                </div>
              </div>
            ) : (
              table.getRowModel().rows.map(row => (
                <div
                  key={row.id}
                  className={cn(
                    'flex border-b border-muted-foreground/10 last:border-b-0',
                    'hover:bg-muted/50 transition-colors',
                    row.getIsSelected() && 'bg-muted'
                  )}
                >
                  {row.getVisibleCells().map(cell => (
                    <div
                      key={cell.id}
                      className="px-4 py-3 text-sm border-r border-muted-foreground/10 last:border-r-0"
                      style={{ width: cell.column.getSize() }}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </div>
                  ))}
                </div>
              ))
            )}
          </div>
          
          {/* Pagination */}
          <div className="border-t px-4 py-3 flex items-center justify-between">
            <div className="text-sm text-muted-foreground">
              {table.getFilteredSelectedRowModel().rows.length} of{' '}
              {table.getFilteredRowModel().rows.length} row(s) selected
            </div>
            
            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
              >
                <ChevronLeft className="h-4 w-4" />
                Previous
              </Button>
              
              <div className="flex items-center space-x-1">
                <span className="text-sm">Page</span>
                <strong className="text-sm">
                  {table.getState().pagination.pageIndex + 1} of{' '}
                  {table.getPageCount()}
                </strong>
              </div>
              
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
              >
                Next
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

// Component registration
export const dataTableComponentConfig: ComponentConfig = {
  type: 'data-table',
  name: 'Data Table',
  category: 'Data Display',
  icon: Table,
  description: 'Advanced data table with sorting, filtering, and pagination',
  constraints: {
    minSize: { w: 3, h: 2 },
    maxSize: { w: 12, h: 6 },
    preferredSize: { w: 6, h: 4 },
  },
  props: {
    data: {
      type: 'array',
      description: 'Table data array',
      required: true,
    },
    columns: {
      type: 'array',
      description: 'Column definitions',
      required: true,
    },
    pageSize: {
      type: 'number',
      description: 'Number of rows per page',
      default: 10,
    },
    searchable: {
      type: 'boolean',
      description: 'Enable global search',
      default: true,
    },
    filterable: {
      type: 'boolean',
      description: 'Enable column filtering',
      default: true,
    },
    exportable: {
      type: 'boolean',
      description: 'Enable data export',
      default: false,
    },
  },
  responsive: {
    mobile: { w: 4, h: 3 },
    tablet: { w: 6, h: 3 },
    desktop: { w: 6, h: 4 },
  },
  accessibility: {
    role: 'table',
    ariaLabel: 'Data table',
    keyboardNavigation: true,
    screenReaderOptimized: true,
  },
};
```

## Common Pitfalls

### 1. Styling Conflicts

**Problem**: CSS conflicts between shadcn/ui components and custom styles.

```typescript
// ❌ Don't override component styles directly
const BadComponent = () => (
  <Button className="bg-red-500 hover:bg-red-600">
    {/* This conflicts with Button's internal styles */}
    Click me
  </Button>
);

// ✅ Use proper variant system
const GoodComponent = () => (
  <Button 
    variant="destructive"
    className="custom-additional-styles"
  >
    Click me
  </Button>
);
```

**Solution**: Always use the component variant system and extend with `cn()`:

```typescript
// Proper style extension
const ExtendedButton = React.forwardRef<HTMLButtonElement, ButtonProps & {
  customVariant?: 'danger' | 'success';
}>(({ className, customVariant, ...props }, ref) => {
  return (
    <Button
      ref={ref}
      className={cn(
        // Base component styles are preserved
        customVariant === 'danger' && 'bg-red-600 hover:bg-red-700',
        customVariant === 'success' && 'bg-green-600 hover:bg-green-700',
        className
      )}
      {...props}
    />
  );
});
```

### 2. Performance Issues

**Problem**: Re-rendering all grid components when one changes.

```typescript
// ❌ This causes all components to re-render
const BadGrid = () => {
  const [components, setComponents] = useState<GridComponent[]>([]);
  
  return (
    <div>
      {components.map(component => (
        <ComponentRenderer 
          key={component.id}
          component={component}
          // This object is recreated on every render
          config={{ theme: 'dark', animations: true }}
        />
      ))}
    </div>
  );
};

// ✅ Memoize expensive operations
const GoodGrid = () => {
  const [components, setComponents] = useState<GridComponent[]>([]);
  
  // Memoize configuration
  const config = useMemo(() => ({
    theme: 'dark' as const,
    animations: true,
  }), []);
  
  // Memoize component renderer
  const MemoizedRenderer = memo(ComponentRenderer);
  
  return (
    <div>
      {components.map(component => (
        <MemoizedRenderer
          key={component.id}
          component={component}
          config={config}
        />
      ))}
    </div>
  );
};
```

### 3. Accessibility Oversights

**Problem**: Missing or incorrect ARIA attributes.

```typescript
// ❌ Poor accessibility
const BadModal = ({ isOpen, onClose, children }) => (
  <div className={isOpen ? 'block' : 'hidden'}>
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()}>
        {children}
      </div>
    </div>
  </div>
);

// ✅ Proper accessibility
const GoodModal = ({ isOpen, onClose, children, title }) => (
  <Dialog open={isOpen} onOpenChange={onClose}>
    <DialogContent>
      <DialogHeader>
        <DialogTitle>{title}</DialogTitle>
      </DialogHeader>
      {children}
    </DialogContent>
  </Dialog>
);
```

### 4. Responsive Design Issues

**Problem**: Components that don't adapt properly to different screen sizes.

```typescript
// ❌ Fixed sizing that breaks on mobile
const BadComponent = () => (
  <Card className="w-96 h-64">
    <CardContent className="grid grid-cols-3 gap-4">
      {/* This breaks on mobile */}
    </CardContent>
  </Card>
);

// ✅ Responsive sizing
const GoodComponent = () => (
  <Card className="w-full max-w-md h-auto min-h-64">
    <CardContent className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      {/* Adapts to screen size */}
    </CardContent>
  </Card>
);
```

### 5. State Management Issues

**Problem**: Complex state logic inside components.

```typescript
// ❌ Component doing too much
const OverComplexComponent = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('');
  const [sort, setSort] = useState('name');
  
  // Complex logic mixed with UI
  useEffect(() => {
    // Lots of async logic here
  }, [filter, sort]);
  
  return (/* Complex JSX */);
};

// ✅ Separate concerns with custom hooks
const WellStructuredComponent = () => {
  const {
    data,
    loading,
    error,
    filter,
    setFilter,
    sort,
    setSort,
  } = useDataTable();
  
  return (/* Clean JSX focusing on presentation */);
};
```

## shadcn MCP Tool Reference

The shadcn MCP (Model Context Protocol) tool provides automated component discovery and integration. Here's how to leverage it effectively:

### Installation and Setup

```bash
# Install the shadcn MCP tool
npm install @shadcn/mcp

# Configure in your MCP settings
{
  "mcpServers": {
    "shadcn": {
      "command": "npx",
      "args": ["@shadcn/mcp"],
      "env": {
        "SHADCN_PROJECT_PATH": "/path/to/your/project"
      }
    }
  }
}
```

### Available Commands

```typescript
// Query available components
interface ShadcnQuery {
  search?: string;
  category?: 'form' | 'data-display' | 'navigation' | 'feedback' | 'overlay';
  complexity?: 'simple' | 'intermediate' | 'advanced';
  responsive?: boolean;
  accessible?: boolean;
}

// Example queries
const queries = [
  // Find form components
  { category: 'form', accessible: true },
  
  // Search for data components
  { search: 'table chart graph', category: 'data-display' },
  
  // Find responsive navigation
  { category: 'navigation', responsive: true },
  
  // Complex interactive components
  { complexity: 'advanced', responsive: true, accessible: true },
];
```

### Component Discovery Workflow

```typescript
// 1. Discover components via MCP
const discoverComponents = async (requirements: ComponentRequirements) => {
  const query: ShadcnQuery = {
    category: requirements.category,
    responsive: true,
    accessible: true,
  };
  
  const results = await shadcnMCP.search(query);
  return results.components;
};

// 2. Analyze component compatibility
const analyzeCompatibility = async (componentName: string) => {
  const analysis = await shadcnMCP.analyze(componentName);
  
  return {
    bentoCompatible: analysis.gridCompatible,
    sizeConstraints: analysis.recommendedSizes,
    dependencies: analysis.requiredPackages,
    accessibility: analysis.a11yFeatures,
    performance: analysis.performanceScore,
  };
};

// 3. Generate integration code
const generateIntegrationCode = async (
  componentName: string,
  bentoConfig: ComponentConfig
) => {
  const code = await shadcnMCP.generateBentoWrapper({
    component: componentName,
    config: bentoConfig,
    includeTypes: true,
    includeTests: true,
    includeStories: true,
  });
  
  return {
    component: code.componentCode,
    types: code.typeDefinitions,
    tests: code.testFiles,
    stories: code.storybookStories,
    registration: code.bentoRegistration,
  };
};
```

### Best Practices for MCP Integration

```typescript
// 1. Automated component auditing
const auditBentoComponents = async () => {
  const components = await shadcnMCP.listInstalledComponents();
  
  const audit = await Promise.all(
    components.map(async (component) => {
      const analysis = await shadcnMCP.analyze(component.name);
      
      return {
        name: component.name,
        version: component.version,
        bentoCompatible: analysis.gridCompatible,
        issues: analysis.compatibilityIssues,
        recommendations: analysis.improvements,
        updateAvailable: analysis.hasUpdate,
      };
    })
  );
  
  return audit;
};

// 2. Automated updates and migrations
const updateComponents = async () => {
  const updates = await shadcnMCP.checkUpdates();
  
  for (const update of updates) {
    if (update.breakingChanges) {
      // Generate migration guide
      const migration = await shadcnMCP.generateMigration(
        update.from,
        update.to
      );
      
      console.log(`Migration required for ${update.component}:`);
      console.log(migration.steps);
    } else {
      // Safe to auto-update
      await shadcnMCP.update(update.component);
    }
  }
};

// 3. Component optimization suggestions
const optimizeForBento = async (componentName: string) => {
  const suggestions = await shadcnMCP.optimize(componentName, {
    target: 'bento-grid',
    priorities: ['performance', 'accessibility', 'responsive'],
    constraints: {
      maxBundleSize: '50kb',
      minA11yScore: 95,
      supportedDevices: ['mobile', 'tablet', 'desktop'],
    },
  });
  
  return suggestions;
};
```

### Integration Examples

```typescript
// Example: Auto-generate form components from schema
const generateFormFromSchema = async (schema: JSONSchema) => {
  // 1. Analyze schema to determine required components
  const requirements = await shadcnMCP.analyzeSchema(schema);
  
  // 2. Find best matching components
  const components = await Promise.all(
    requirements.fields.map(field => 
      shadcnMCP.findBestComponent({
        type: field.type,
        constraints: field.constraints,
        accessibility: true,
        responsive: true,
      })
    )
  );
  
  // 3. Generate complete form component
  const formCode = await shadcnMCP.generateForm({
    schema,
    components,
    bentoConfig: {
      preferredSize: { w: 3, h: 4 },
      responsive: {
        mobile: { w: 4, h: 5 },
        tablet: { w: 4, h: 4 },
        desktop: { w: 3, h: 4 },
      },
    },
  });
  
  return formCode;
};

// Example: Component library synchronization
const syncWithDesignSystem = async () => {
  const designTokens = await fetchDesignTokens();
  
  // Update component styles to match design system
  await shadcnMCP.updateTheme({
    colors: designTokens.colors,
    typography: designTokens.typography,
    spacing: designTokens.spacing,
    borderRadius: designTokens.borderRadius,
  });
  
  // Regenerate component variants
  const components = await shadcnMCP.listComponents();
  for (const component of components) {
    await shadcnMCP.regenerateVariants(component, designTokens);
  }
};
```

---

## Summary

This guide provides a complete reference for integrating shadcn/ui components with Tailwind CSS in the Bento Grid System. Key takeaways:

1. **Component Mapping**: Use the ECS field type mapping table to select appropriate shadcn components
2. **Styling Patterns**: Follow the established Tailwind utility patterns for consistency
3. **Theme System**: Leverage CSS variables for flexible theming and dark mode support
4. **Responsive Design**: Implement mobile-first, touch-friendly interfaces
5. **Performance**: Optimize with lazy loading, memoization, and efficient rendering
6. **Accessibility**: Build WCAG-compliant interfaces with proper ARIA support
7. **Code Quality**: Use TypeScript, proper error handling, and testing
8. **MCP Integration**: Leverage automation for component discovery and maintenance

For questions or updates to this guide, refer to the [project documentation](../README.md) or consult the development team.