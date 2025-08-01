# Getting Started with Bento

This guide will walk you through the Bento Grid System from a developer's perspective.

## Prerequisites

- Jarvis UI Studio development environment
- Node.js 18+ and npm/pnpm
- Basic understanding of React and TypeScript
- Familiarity with the existing component structure

## Quick Start Overview

```
1. Create a Component → 2. Register Component → 3. Create Page → 4. Design Layout → 5. Deploy
```

## Step 1: Creating a Bento-Compatible Component

### Basic Component Structure

Every Bento component must follow this pattern:

```typescript
// src/components/dashboard/MyWidget.tsx
import { memo } from 'react';
import { BentoComponentProps } from '@/types/bento';

interface MyWidgetProps extends BentoComponentProps {
  title: string;
  value: number;
  trend?: 'up' | 'down' | 'stable';
}

export const MyWidget = memo<MyWidgetProps>(({ 
  title, 
  value, 
  trend = 'stable',
  className,
  style 
}) => {
  return (
    <div className={className} style={style}>
      <h3>{title}</h3>
      <p>{value}</p>
      {trend && <span>Trend: {trend}</span>}
    </div>
  );
});

MyWidget.displayName = 'MyWidget';

// Component metadata for Bento
export const MyWidgetMeta = {
  displayName: 'My Widget',
  category: 'Analytics',
  icon: '📊',
  defaultProps: {
    title: 'Widget Title',
    value: 0,
    trend: 'stable'
  },
  constraints: {
    minSize: { w: 2, h: 2 },
    maxSize: { w: 6, h: 4 }
  }
};
```

### Key Requirements

1. **Extend BentoComponentProps**: Ensures compatibility with grid system
2. **Use React.memo**: Optimizes rendering performance
3. **Export metadata**: Tells Bento about your component
4. **Handle className and style**: Required for positioning

## Step 2: Registering Your Component

### Add to Component Registry

```typescript
// src/components/bento/registry.ts
import { MyWidget, MyWidgetMeta } from '../dashboard/MyWidget';
import { MetricCard, MetricCardMeta } from '../dashboard/MetricCard';

export const componentRegistry = {
  MyWidget: {
    component: MyWidget,
    ...MyWidgetMeta
  },
  MetricCard: {
    component: MetricCard,
    ...MetricCardMeta
  },
  // ... other components
};
```

## Step 3: Creating a Page

### Using the Page Builder UI

```
1. Navigate to /admin/pages
2. Click "Create New Page"
3. Fill in page details:
   - Display Name: "My Dashboard"
   - Route: "/my-dashboard"
   - Security: Select required roles
4. Click "Design Layout"
```

### Page Configuration Structure

```typescript
const myPage: BentoPage = {
  id: 'page-001',
  displayName: 'My Dashboard',
  route: '/my-dashboard',
  layoutId: 'layout-standard',
  bindings: {
    security: {
      requiredRoles: ['user'],
      requiredPermissions: ['view-dashboard']
    },
    visibility: {
      showInNavigation: true,
      navigationOrder: 10,
      icon: '📊'
    }
  }
};
```

## Step 4: Designing the Layout

### Visual Editor Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│  Grid Editor - My Dashboard                    [Desktop│▼] [Save]│
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  1. Drag components from palette to grid                         │
│  2. Resize by dragging handles                                   │
│  3. Configure props in properties panel                          │
│  4. Preview in different device modes                            │
│  5. Save when satisfied                                           │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Grid Positioning

Components are positioned using grid units:

```
Desktop: 12 columns
Tablet:  8 columns  
Mobile:  4 columns

Position: { x: 0, y: 0, w: 3, h: 2 }
         x = column start (0-based)
         y = row start (0-based)  
         w = width in columns
         h = height in rows
```

## Step 5: Component Development Best Practices

### 1. Make Components Self-Contained

```typescript
// ✅ Good - Self-contained with clear props
export const StatusCard = ({ status, count, label }) => {
  return (
    <Card>
      <CardHeader>{label}</CardHeader>
      <CardContent>
        <Badge variant={status}>{count}</Badge>
      </CardContent>
    </Card>
  );
};

// ❌ Bad - Depends on external context
export const StatusCard = () => {
  const { status, count } = useContext(DashboardContext);
  // ...
};
```

### 2. Define Clear Size Constraints

```typescript
export const ComponentMeta = {
  constraints: {
    minSize: { w: 2, h: 2 }, // Minimum 2x2 grid units
    maxSize: { w: 12, h: 6 }, // Maximum full width, 6 rows
    aspectRatio: 16/9, // Optional: maintain aspect ratio
    resizable: {
      horizontal: true,
      vertical: false // Only resize horizontally
    }
  }
};
```

### 3. Handle Different Size Modes

```typescript
export const AdaptiveComponent = ({ size, ...props }) => {
  // size = { w: 4, h: 3 } from grid
  
  const isCompact = size.w <= 2;
  const isMedium = size.w > 2 && size.w <= 6;
  const isLarge = size.w > 6;
  
  if (isCompact) {
    return <CompactView {...props} />;
  }
  
  if (isMedium) {
    return <MediumView {...props} />;
  }
  
  return <LargeView {...props} />;
};
```

## Step 6: Testing Your Components

### Unit Testing

```typescript
import { render } from '@testing-library/react';
import { MyWidget } from './MyWidget';

describe('MyWidget', () => {
  it('renders with required props', () => {
    const { getByText } = render(
      <MyWidget 
        title="Test Widget" 
        value={42}
      />
    );
    
    expect(getByText('Test Widget')).toBeInTheDocument();
    expect(getByText('42')).toBeInTheDocument();
  });
  
  it('respects size constraints', () => {
    expect(MyWidgetMeta.constraints.minSize).toEqual({ w: 2, h: 2 });
  });
});
```

### Grid Testing

```typescript
import { renderInGrid } from '@/test-utils/bento';

describe('MyWidget in Grid', () => {
  it('renders correctly in small space', () => {
    const { container } = renderInGrid(
      <MyWidget title="Test" value={42} />,
      { w: 2, h: 2 }
    );
    
    expect(container.firstChild).toHaveStyle({
      gridColumn: 'span 2',
      gridRow: 'span 2'
    });
  });
});
```

## Common Patterns

### 1. Data-Connected Components

```typescript
export const LiveMetricCard = ({ 
  dataSource, 
  refreshInterval = 60000,
  ...props 
}) => {
  const [data, setData] = useState(null);
  
  useEffect(() => {
    const fetchData = async () => {
      const result = await apiService.getMetric(dataSource);
      setData(result);
    };
    
    fetchData();
    const interval = setInterval(fetchData, refreshInterval);
    
    return () => clearInterval(interval);
  }, [dataSource, refreshInterval]);
  
  return <MetricCard {...props} value={data?.value || 0} />;
};
```

### 2. Responsive Components

```typescript
export const ResponsiveChart = ({ size, data }) => {
  const chartHeight = size.h * 100; // 100px per grid unit
  const showLegend = size.w >= 4; // Only show legend if wide enough
  
  return (
    <Chart 
      data={data} 
      height={chartHeight}
      showLegend={showLegend}
      compact={size.w < 3}
    />
  );
};
```

### 3. Interactive Components

```typescript
export const InteractiveWidget = ({ onAction, ...props }) => {
  const handleClick = () => {
    // Bento will pass through event handlers
    onAction?.({ 
      component: 'InteractiveWidget',
      action: 'click',
      data: props 
    });
  };
  
  return (
    <div onClick={handleClick}>
      {/* Component content */}
    </div>
  );
};
```

## Next Steps

1. **Explore Components**: Check the [Component Registry](./04-component-registry.md) documentation
2. **Learn Grid System**: Understand the [Grid System](./03-grid-system.md) in detail
3. **Advanced Features**: Dive into [Data Models](./06-data-models.md) for complex scenarios
4. **Build Your First Page**: Follow the [Page Builder](./05-page-builder.md) guide

## Troubleshooting

### Component Not Appearing in Palette
- Ensure component is exported from registry
- Check that metadata is properly defined
- Verify no TypeScript errors in component

### Layout Not Saving
- Check browser console for errors
- Ensure all required fields are filled
- Verify user has appropriate permissions

### Component Not Rendering
- Confirm component handles className and style props
- Check for JavaScript errors in component
- Verify component is properly memoized