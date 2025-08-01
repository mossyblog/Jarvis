# Component API Reference

## Overview

This document provides a comprehensive API reference for creating Bento-compatible components.

## Base Component Interface

### BentoComponentProps

All Bento components must accept these base props:

```typescript
interface BentoComponentProps {
  // Positioning - Injected by grid system
  className?: string;
  style?: React.CSSProperties;
  
  // Grid information - Available in render
  gridSize?: Size;
  deviceType?: DeviceType;
  
  // Data binding - Injected by data layer
  data?: any;
  loading?: boolean;
  error?: Error;
  
  // Interaction callbacks
  onReady?: () => void;
  onError?: (error: Error) => void;
  onAction?: (action: ComponentAction) => void;
}
```

## Creating a Bento Component

### Basic Structure

```typescript
import { memo } from 'react';
import { BentoComponentProps } from '@/types/bento';

interface MyComponentProps extends BentoComponentProps {
  // Component-specific props
  title: string;
  value: number;
  showTrend?: boolean;
}

export const MyComponent = memo<MyComponentProps>(({
  // Destructure Bento props
  className,
  style,
  gridSize,
  data,
  loading,
  error,
  
  // Component props
  title,
  value,
  showTrend = true,
  
  // Callbacks
  onReady,
  onError,
  onAction
}) => {
  // Component implementation
  useEffect(() => {
    onReady?.();
  }, [onReady]);
  
  if (loading) return <LoadingState />;
  if (error) return <ErrorState error={error} />;
  
  return (
    <div className={className} style={style}>
      {/* Component content */}
    </div>
  );
});

MyComponent.displayName = 'MyComponent';
```

### Component Registration

```typescript
import { ComponentConfig } from '@/types/bento';

export const MyComponentConfig: ComponentConfig = {
  // Component reference
  component: MyComponent,
  
  // Display metadata
  displayName: 'My Component',
  description: 'A sample Bento component',
  category: 'Custom',
  icon: '🔧',
  tags: ['sample', 'custom'],
  
  // Default props
  defaultProps: {
    title: 'Default Title',
    value: 0,
    showTrend: true
  },
  
  // Size constraints
  constraints: {
    minSize: { w: 2, h: 2 },
    maxSize: { w: 6, h: 4 },
    defaultSize: { w: 3, h: 2 },
    resizable: {
      horizontal: true,
      vertical: true
    }
  }
};
```

## Advanced Features

### Data Binding

Configure automatic data binding for your component:

```typescript
const dataBinding: DataBindingConfig = {
  fields: [
    {
      name: 'value',
      type: 'number',
      source: 'api',
      path: '/api/metrics/current',
      transform: {
        type: 'map',
        expression: 'data.metrics.value'
      }
    },
    {
      name: 'previousValue',
      type: 'number',
      source: 'api',
      path: '/api/metrics/previous'
    }
  ],
  refresh: {
    interval: 60000, // 1 minute
    triggers: [
      {
        type: 'event',
        source: 'window',
        condition: 'document.visibilityState === "visible"'
      }
    ]
  }
};
```

### Component Interactions

Define events and actions:

```typescript
const interactions: InteractionConfig = {
  events: [
    {
      name: 'onClick',
      description: 'Fired when component is clicked',
      payload: {
        componentId: { type: 'string', required: true },
        value: { type: 'number', required: true }
      }
    },
    {
      name: 'onValueChange',
      description: 'Fired when value changes',
      payload: {
        oldValue: { type: 'number', required: true },
        newValue: { type: 'number', required: true }
      }
    }
  ],
  actions: [
    {
      name: 'refresh',
      description: 'Refresh component data',
      handler: 'handleRefresh'
    },
    {
      name: 'reset',
      description: 'Reset to default state',
      parameters: [
        {
          name: 'keepData',
          type: 'boolean',
          defaultValue: false
        }
      ],
      handler: 'handleReset'
    }
  ]
};
```

### Responsive Behavior

Handle different grid sizes:

```typescript
export const ResponsiveComponent = memo<ComponentProps>(({ 
  gridSize,
  ...props 
}) => {
  // Determine layout based on size
  const layout = useMemo(() => {
    if (!gridSize) return 'default';
    
    if (gridSize.w <= 2) return 'compact';
    if (gridSize.w <= 4) return 'medium';
    return 'large';
  }, [gridSize]);
  
  // Render different layouts
  switch (layout) {
    case 'compact':
      return <CompactLayout {...props} />;
    case 'medium':
      return <MediumLayout {...props} />;
    case 'large':
      return <LargeLayout {...props} />;
    default:
      return <DefaultLayout {...props} />;
  }
});
```

### Component Variants

Define multiple variants of a component:

```typescript
const variants: ComponentVariant[] = [
  {
    name: 'default',
    displayName: 'Default View',
    constraints: {
      minSize: { w: 3, h: 2 },
      maxSize: { w: 6, h: 4 }
    }
  },
  {
    name: 'compact',
    displayName: 'Compact View',
    constraints: {
      minSize: { w: 2, h: 1 },
      maxSize: { w: 4, h: 2 }
    },
    defaultProps: {
      showTrend: false,
      compact: true
    }
  },
  {
    name: 'detailed',
    displayName: 'Detailed View',
    constraints: {
      minSize: { w: 4, h: 3 },
      maxSize: { w: 8, h: 6 }
    },
    defaultProps: {
      showDetails: true,
      showHistory: true
    }
  }
];
```

## Component Lifecycle

### Initialization

```typescript
export const LifecycleComponent = memo(() => {
  // Called when component is mounted in grid
  useEffect(() => {
    console.log('Component mounted in grid');
    
    return () => {
      console.log('Component removed from grid');
    };
  }, []);
  
  // Called when component is ready
  const handleReady = useCallback(() => {
    console.log('Component ready for interaction');
  }, []);
  
  // Called on data load
  const handleDataLoad = useCallback((data: any) => {
    console.log('Data loaded:', data);
  }, []);
  
  return <div>...</div>;
});
```

### Error Handling

```typescript
export const ErrorBoundaryComponent = memo<ComponentProps>(({ 
  onError 
}) => {
  const [hasError, setHasError] = useState(false);
  
  // Catch errors in component
  useEffect(() => {
    const handleError = (error: Error) => {
      setHasError(true);
      onError?.(error);
    };
    
    window.addEventListener('error', handleError);
    return () => window.removeEventListener('error', handleError);
  }, [onError]);
  
  if (hasError) {
    return (
      <div className="error-state">
        <p>Component error occurred</p>
        <button onClick={() => setHasError(false)}>
          Retry
        </button>
      </div>
    );
  }
  
  return <div>Normal content</div>;
});
```

## Hooks and Utilities

### useBentoContext

Access Bento system context:

```typescript
import { useBentoContext } from '@/hooks/bento';

export const ContextAwareComponent = () => {
  const {
    gridConfig,
    deviceType,
    isEditing,
    theme,
    dataContext
  } = useBentoContext();
  
  return (
    <div>
      <p>Device: {deviceType}</p>
      <p>Columns: {gridConfig.columns}</p>
      <p>Editing: {isEditing ? 'Yes' : 'No'}</p>
    </div>
  );
};
```

### useComponentData

Handle component data binding:

```typescript
import { useComponentData } from '@/hooks/bento';

export const DataBoundComponent = ({ dataSource }) => {
  const { 
    data, 
    loading, 
    error, 
    refresh 
  } = useComponentData(dataSource);
  
  if (loading) return <Spinner />;
  if (error) return <Error error={error} />;
  
  return (
    <div>
      <div>{data.value}</div>
      <button onClick={refresh}>Refresh</button>
    </div>
  );
};
```

### useComponentSize

React to size changes:

```typescript
import { useComponentSize } from '@/hooks/bento';

export const SizeAwareComponent = () => {
  const { width, height, gridSize } = useComponentSize();
  
  const fontSize = useMemo(() => {
    if (width < 200) return '12px';
    if (width < 400) return '16px';
    return '20px';
  }, [width]);
  
  return (
    <div style={{ fontSize }}>
      Size: {gridSize.w}x{gridSize.h}
    </div>
  );
};
```

## Best Practices

### 1. Performance Optimization

```typescript
// Always memoize components
export const OptimizedComponent = memo(({ prop1, prop2 }) => {
  // Memoize expensive calculations
  const expensiveValue = useMemo(() => {
    return calculateExpensive(prop1, prop2);
  }, [prop1, prop2]);
  
  // Memoize callbacks
  const handleClick = useCallback(() => {
    doSomething(prop1);
  }, [prop1]);
  
  return <div onClick={handleClick}>{expensiveValue}</div>;
});
```

### 2. Type Safety

```typescript
// Define strict prop types
interface StrictComponentProps extends BentoComponentProps {
  value: number;
  onChange: (value: number) => void;
  options?: {
    min?: number;
    max?: number;
    step?: number;
  };
}

// Use generic constraints
export const TypedComponent = <T extends Record<string, any>>({
  data,
  formatter
}: {
  data: T;
  formatter: (item: T) => string;
}) => {
  return <div>{formatter(data)}</div>;
};
```

### 3. Accessibility

```typescript
export const AccessibleComponent = memo<ComponentProps>(({ 
  title, 
  value,
  onAction 
}) => {
  return (
    <div 
      role="region"
      aria-label={title}
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter') {
          onAction?.({ action: 'activate' });
        }
      }}
    >
      <h3 id="title">{title}</h3>
      <div aria-labelledby="title" aria-live="polite">
        {value}
      </div>
    </div>
  );
});
```

### 4. Error Boundaries

```typescript
export const SafeComponent = memo(() => {
  return (
    <ErrorBoundary
      fallback={<div>Component failed to load</div>}
      onError={(error, errorInfo) => {
        console.error('Component error:', error, errorInfo);
      }}
    >
      <ActualComponent />
    </ErrorBoundary>
  );
});
```

## Testing Components

### Unit Testing

```typescript
import { render, screen } from '@testing-library/react';
import { MyComponent } from './MyComponent';

describe('MyComponent', () => {
  const defaultProps = {
    title: 'Test',
    value: 42
  };
  
  it('renders with props', () => {
    render(<MyComponent {...defaultProps} />);
    
    expect(screen.getByText('Test')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();
  });
  
  it('handles loading state', () => {
    render(<MyComponent {...defaultProps} loading />);
    
    expect(screen.getByTestId('loading')).toBeInTheDocument();
  });
  
  it('calls onReady when mounted', () => {
    const onReady = jest.fn();
    render(<MyComponent {...defaultProps} onReady={onReady} />);
    
    expect(onReady).toHaveBeenCalledTimes(1);
  });
});
```

### Integration Testing

```typescript
import { renderInBento } from '@/test-utils/bento';

describe('MyComponent in Bento', () => {
  it('respects size constraints', () => {
    const { container } = renderInBento(
      <MyComponent title="Test" value={42} />,
      { w: 2, h: 2 }
    );
    
    const component = container.querySelector('.bento-component');
    expect(component).toHaveStyle({
      gridColumn: 'span 2',
      gridRow: 'span 2'
    });
  });
});
```

## Migration Guide

### Converting Existing Components

```typescript
// Before: Regular component
export const OldComponent = ({ title, value }) => {
  return (
    <div>
      <h3>{title}</h3>
      <p>{value}</p>
    </div>
  );
};

// After: Bento-compatible component
import { memo } from 'react';
import { BentoComponentProps } from '@/types/bento';

interface NewComponentProps extends BentoComponentProps {
  title: string;
  value: number;
}

export const NewComponent = memo<NewComponentProps>(({
  className,
  style,
  title,
  value
}) => {
  return (
    <div className={className} style={style}>
      <h3>{title}</h3>
      <p>{value}</p>
    </div>
  );
});

NewComponent.displayName = 'NewComponent';

// Add configuration
export const NewComponentConfig: ComponentConfig = {
  component: NewComponent,
  displayName: 'New Component',
  category: 'Custom',
  constraints: {
    minSize: { w: 2, h: 2 },
    maxSize: { w: 4, h: 4 }
  }
};
```

## Next Steps

1. Review [Component Registry](./04-component-registry.md) for registration details
2. Check [Testing Strategy](./08-testing-strategy.md) for testing guidelines
3. See [Data Models](./06-data-models.md) for type definitions
4. Explore [Grid API](./10-grid-api.md) for grid integration