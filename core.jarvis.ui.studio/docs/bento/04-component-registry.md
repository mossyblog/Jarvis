# Component Registry

## Overview

The Component Registry is the central catalog of all components available in the Bento Grid System. It manages component metadata, constraints, and provides a standardized way to make any React component "Bento-ready".

## Registry Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Component Registry                           │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Analytics     │  │      Data       │  │     Status      │ │
│  │                 │  │                 │  │                 │ │
│  │ • MetricCard    │  │ • DataTable     │  │ • AlertCard     │ │
│  │ • ChartWidget   │  │ • DataGrid      │  │ • StatusBadge   │ │
│  │ • KPIDisplay    │  │ • ListView      │  │ • ProgressBar   │ │
│  │ • Sparkline     │  │ • DetailView    │  │ • Notification  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Navigation    │  │     Forms       │  │    Layout       │ │
│  │                 │  │                 │  │                 │ │
│  │ • Breadcrumb    │  │ • InputField    │  │ • Container     │ │
│  │ • TabBar        │  │ • SelectBox     │  │ • Divider       │ │
│  │ • MenuList      │  │ • DatePicker    │  │ • Spacer        │ │
│  │ • ActionBar     │  │ • FileUpload    │  │ • Section       │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Component Configuration

### Basic Component Structure

```typescript
interface ComponentConfig {
  // React component
  component: React.ComponentType<any>;
  
  // Display metadata
  displayName: string;
  description?: string;
  category: ComponentCategory;
  icon: string; // Emoji or icon component
  tags?: string[]; // For search/filtering
  
  // Component behavior
  defaultProps: Record<string, any>;
  constraints: ComponentConstraints;
  
  // Advanced features
  dataBinding?: DataBindingConfig;
  interactions?: InteractionConfig;
  variants?: ComponentVariant[];
}
```

### Component Categories

```typescript
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
```

## Creating Bento-Ready Components

### Step 1: Component Implementation

```typescript
// src/components/dashboard/SalesMetric.tsx
import { memo } from 'react';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardHeader, CardContent } from '@/components/ui/card';
import { BentoComponentProps } from '@/types/bento';

interface SalesMetricProps extends BentoComponentProps {
  title: string;
  value: number;
  previousValue?: number;
  format?: 'currency' | 'number' | 'percentage';
  currency?: string;
}

export const SalesMetric = memo<SalesMetricProps>(({
  title,
  value,
  previousValue,
  format = 'number',
  currency = 'USD',
  className,
  style
}) => {
  // Calculate trend
  const trend = previousValue 
    ? ((value - previousValue) / previousValue) * 100 
    : 0;
    
  const TrendIcon = trend > 0 
    ? TrendingUp 
    : trend < 0 
      ? TrendingDown 
      : Minus;
      
  // Format value
  const formattedValue = formatValue(value, format, currency);
  
  return (
    <Card className={className} style={style}>
      <CardHeader>
        <h3 className="text-sm font-medium text-muted-foreground">
          {title}
        </h3>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-between">
          <span className="text-2xl font-bold">
            {formattedValue}
          </span>
          {previousValue && (
            <div className="flex items-center gap-1">
              <TrendIcon 
                size={16} 
                className={trend > 0 ? 'text-green-500' : 'text-red-500'} 
              />
              <span className="text-sm">
                {Math.abs(trend).toFixed(1)}%
              </span>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
});

SalesMetric.displayName = 'SalesMetric';
```

### Step 2: Component Metadata

```typescript
// src/components/dashboard/SalesMetric.registry.ts
import { SalesMetric } from './SalesMetric';
import { ComponentConfig } from '@/types/bento';

export const SalesMetricConfig: ComponentConfig = {
  component: SalesMetric,
  
  // Display metadata
  displayName: 'Sales Metric',
  description: 'Display sales KPIs with trend indicators',
  category: 'Analytics',
  icon: '💰',
  tags: ['metric', 'kpi', 'sales', 'trend'],
  
  // Default configuration
  defaultProps: {
    title: 'Total Sales',
    value: 0,
    format: 'currency',
    currency: 'USD'
  },
  
  // Size constraints
  constraints: {
    minSize: { w: 2, h: 2 },
    maxSize: { w: 4, h: 3 },
    resizable: {
      horizontal: true,
      vertical: false
    }
  },
  
  // Data binding configuration
  dataBinding: {
    fields: [
      {
        name: 'value',
        type: 'number',
        source: 'api',
        path: 'sales.total'
      },
      {
        name: 'previousValue',
        type: 'number',
        source: 'api',
        path: 'sales.previous'
      }
    ]
  },
  
  // Component variants
  variants: [
    {
      name: 'compact',
      displayName: 'Compact View',
      constraints: {
        minSize: { w: 1, h: 1 },
        maxSize: { w: 2, h: 1 }
      }
    }
  ]
};
```

### Step 3: Registry Registration

```typescript
// src/components/bento/registry.ts
import { ComponentRegistry } from '@/services/bento/ComponentRegistry';
import { MetricCardConfig } from '../dashboard/MetricCard.registry';
import { SalesMetricConfig } from '../dashboard/SalesMetric.registry';
import { DataTableConfig } from '../data/DataTable.registry';
// ... other imports

// Create registry instance
const registry = new ComponentRegistry();

// Register components
registry.register('MetricCard', MetricCardConfig);
registry.register('SalesMetric', SalesMetricConfig);
registry.register('DataTable', DataTableConfig);
// ... other registrations

// Export for use
export const componentRegistry = registry;
```

## Component Constraints

### Size Constraints

```typescript
interface ComponentConstraints {
  minSize: Size;
  maxSize: Size;
  defaultSize?: Size;
  aspectRatio?: number;
  resizable?: {
    horizontal: boolean;
    vertical: boolean;
  };
}

// Examples
const constraints = {
  // Fixed size component
  fixed: {
    minSize: { w: 3, h: 2 },
    maxSize: { w: 3, h: 2 },
    resizable: {
      horizontal: false,
      vertical: false
    }
  },
  
  // Flexible component
  flexible: {
    minSize: { w: 2, h: 2 },
    maxSize: { w: 12, h: 8 },
    defaultSize: { w: 4, h: 3 },
    resizable: {
      horizontal: true,
      vertical: true
    }
  },
  
  // Aspect ratio locked
  aspectLocked: {
    minSize: { w: 2, h: 1 },
    maxSize: { w: 8, h: 4 },
    aspectRatio: 2, // 2:1 ratio
    resizable: {
      horizontal: true,
      vertical: true
    }
  }
};
```

### Responsive Constraints

```typescript
interface ResponsiveConstraints {
  desktop: ComponentConstraints;
  tablet?: ComponentConstraints;
  mobile?: ComponentConstraints;
}

// Example: Different constraints per device
const responsiveConstraints: ResponsiveConstraints = {
  desktop: {
    minSize: { w: 3, h: 3 },
    maxSize: { w: 6, h: 6 }
  },
  tablet: {
    minSize: { w: 4, h: 3 },
    maxSize: { w: 8, h: 6 }
  },
  mobile: {
    minSize: { w: 4, h: 2 },
    maxSize: { w: 4, h: 4 }
  }
};
```

## Data Binding

### Configuration

```typescript
interface DataBindingConfig {
  fields: DataField[];
  refresh?: {
    interval?: number; // milliseconds
    event?: string; // Custom event name
  };
}

interface DataField {
  name: string; // Prop name
  type: 'string' | 'number' | 'boolean' | 'array' | 'object';
  source: 'api' | 'query' | 'static' | 'computed';
  path?: string; // Data path for api/query
  query?: string; // GraphQL or SQL query
  compute?: (context: DataContext) => any; // Computed value
  transform?: (value: any) => any; // Value transformation
}
```

### Examples

```typescript
// API data binding
const apiBinding: DataBindingConfig = {
  fields: [
    {
      name: 'users',
      type: 'array',
      source: 'api',
      path: '/api/users',
      transform: (data) => data.slice(0, 10) // Top 10
    }
  ],
  refresh: {
    interval: 60000 // Refresh every minute
  }
};

// Computed data binding
const computedBinding: DataBindingConfig = {
  fields: [
    {
      name: 'trend',
      type: 'string',
      source: 'computed',
      compute: (context) => {
        const current = context.data.currentValue;
        const previous = context.data.previousValue;
        return current > previous ? 'up' : 'down';
      }
    }
  ]
};
```

## Component Interactions

### Interaction Configuration

```typescript
interface InteractionConfig {
  events: ComponentEvent[];
  actions: ComponentAction[];
}

interface ComponentEvent {
  name: string;
  description: string;
  payload?: Record<string, any>;
}

interface ComponentAction {
  name: string;
  description: string;
  handler: (params: any) => void;
}
```

### Example: Interactive Chart

```typescript
const chartInteractions: InteractionConfig = {
  events: [
    {
      name: 'onPointClick',
      description: 'Fired when user clicks a data point',
      payload: {
        value: 'number',
        label: 'string',
        index: 'number'
      }
    }
  ],
  actions: [
    {
      name: 'refresh',
      description: 'Refresh chart data',
      handler: async (params) => {
        await fetchNewData();
      }
    },
    {
      name: 'exportData',
      description: 'Export chart data as CSV',
      handler: (params) => {
        exportToCSV(params.data);
      }
    }
  ]
};
```

## Component Variants

### Defining Variants

```typescript
interface ComponentVariant {
  name: string;
  displayName: string;
  description?: string;
  constraints?: Partial<ComponentConstraints>;
  defaultProps?: Record<string, any>;
  render?: (props: any) => React.ReactElement;
}

// Example: Table variants
const tableVariants: ComponentVariant[] = [
  {
    name: 'compact',
    displayName: 'Compact Table',
    description: 'Dense table for more data',
    defaultProps: {
      size: 'sm',
      striped: false
    },
    constraints: {
      minSize: { w: 3, h: 2 }
    }
  },
  {
    name: 'detailed',
    displayName: 'Detailed Table',
    description: 'Expanded view with more columns',
    defaultProps: {
      size: 'lg',
      showActions: true,
      expandable: true
    },
    constraints: {
      minSize: { w: 6, h: 4 }
    }
  }
];
```

## Registry API

### Core Methods

```typescript
class ComponentRegistry {
  // Registration
  register(key: string, config: ComponentConfig): void;
  unregister(key: string): void;
  
  // Retrieval
  get(key: string): ComponentConfig | undefined;
  getByCategory(category: string): ComponentConfig[];
  search(query: string): ComponentConfig[];
  getAll(): Record<string, ComponentConfig>;
  
  // Validation
  validate(key: string, props: any): ValidationResult;
  validateConstraints(key: string, size: Size): boolean;
  
  // Utilities
  getCategories(): string[];
  getTags(): string[];
  export(): RegistryExport;
  import(data: RegistryExport): void;
}
```

### Usage Examples

```typescript
// Get component by key
const metricCard = registry.get('MetricCard');

// Get all analytics components
const analyticsComponents = registry.getByCategory('Analytics');

// Search components
const chartComponents = registry.search('chart');

// Validate component props
const validation = registry.validate('MetricCard', {
  title: 'Revenue',
  value: 'invalid' // Should be number
});

if (!validation.valid) {
  console.error(validation.errors);
}
```

## Component Palette UI

### Visual Organization

```
┌─────────────────────────────────────────────────────────────────┐
│  Component Palette                                     [Search...] │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  📊 Analytics (12)                                              ▼ │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐               │
│  │ Metric  │ │  Chart  │ │   KPI   │ │  Gauge  │               │
│  │  Card   │ │ Widget  │ │ Display │ │  Chart  │               │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘               │
│                                                                   │
│  📋 Data (8)                                                    ▼ │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐               │
│  │  Data   │ │  Data   │ │  List   │ │ Detail  │               │
│  │  Table  │ │  Grid   │ │  View   │ │  View   │               │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘               │
│                                                                   │
│  [+ Add Custom Component]                                         │
└─────────────────────────────────────────────────────────────────┘
```

### Drag Preview

```typescript
const DragPreview = ({ component, size }) => {
  const config = registry.get(component.type);
  
  return (
    <div 
      className="drag-preview"
      style={{
        width: size.w * GRID_UNIT,
        height: size.h * GRID_UNIT,
        opacity: 0.8
      }}
    >
      <div className="drag-preview-header">
        <span>{config.icon}</span>
        <span>{config.displayName}</span>
      </div>
      <div className="drag-preview-body">
        {/* Simplified component preview */}
      </div>
    </div>
  );
};
```

## Best Practices

### 1. Component Independence
- Components should not depend on specific page context
- Use props for all configuration
- Avoid global state dependencies

### 2. Performance Optimization
- Always use React.memo for components
- Implement proper prop comparison
- Lazy load heavy components

### 3. Accessibility
- Include proper ARIA labels
- Support keyboard navigation
- Provide focus indicators

### 4. Documentation
- Document all props with TypeScript
- Include usage examples
- Provide Storybook stories

## Next Steps

1. Review [Page Builder](./05-page-builder.md) for using registered components
2. Explore [Data Models](./06-data-models.md) for type definitions
3. Check [Component API](./09-component-api.md) for detailed API reference
4. See [Testing Strategy](./08-testing-strategy.md) for testing components