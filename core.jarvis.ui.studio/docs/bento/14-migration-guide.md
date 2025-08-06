# Migration Guide

## Overview

This guide provides step-by-step instructions for migrating existing pages and components to the Bento Grid System.

## Migration Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    Migration Phases                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Phase 1: Component Preparation (Week 1-2)                       │
│  ├─ Audit existing components                                    │
│  ├─ Create Bento wrappers                                        │
│  └─ Register in component registry                               │
│                                                                   │
│  Phase 2: Page Analysis (Week 2-3)                              │
│  ├─ Map current page structures                                  │
│  ├─ Identify layout patterns                                     │
│  └─ Plan grid configurations                                     │
│                                                                   │
│  Phase 3: Gradual Migration (Week 3-6)                          │
│  ├─ Start with simple pages                                      │
│  ├─ Convert complex layouts                                      │
│  └─ Maintain dual system temporarily                            │
│                                                                   │
│  Phase 4: Cutover (Week 7-8)                                    │
│  ├─ Switch routing to Bento                                      │
│  ├─ Deprecate old pages                                         │
│  └─ Clean up legacy code                                        │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Component Migration

### Step 1: Analyze Existing Component

```typescript
// Original component
export const DashboardMetric = ({ 
  title, 
  value, 
  trend, 
  onClick 
}) => {
  return (
    <div className="dashboard-metric" onClick={onClick}>
      <h3>{title}</h3>
      <div className="value">{value}</div>
      {trend && <TrendIndicator trend={trend} />}
    </div>
  );
};
```

### Step 2: Create Bento Wrapper

```typescript
// Bento-compatible wrapper
import { memo } from 'react';
import { BentoComponentProps } from '@/types/bento';
import { DashboardMetric as OriginalMetric } from './legacy/DashboardMetric';

interface BentoDashboardMetricProps extends BentoComponentProps {
  title: string;
  value: number;
  trend?: 'up' | 'down' | 'stable';
  onClick?: () => void;
}

export const BentoDashboardMetric = memo<BentoDashboardMetricProps>(({
  // Bento props
  className,
  style,
  gridSize,
  onAction,
  
  // Original props
  title,
  value,
  trend,
  onClick
}) => {
  // Adapt callbacks
  const handleClick = () => {
    onClick?.();
    onAction?.({
      action: 'click',
      component: 'DashboardMetric',
      data: { title, value }
    });
  };
  
  // Wrap original component
  return (
    <div className={className} style={style}>
      <OriginalMetric
        title={title}
        value={value}
        trend={trend}
        onClick={handleClick}
      />
    </div>
  );
});

BentoDashboardMetric.displayName = 'BentoDashboardMetric';
```

### Step 3: Add Component Configuration

```typescript
// Component configuration
export const BentoDashboardMetricConfig: ComponentConfig = {
  component: BentoDashboardMetric,
  displayName: 'Dashboard Metric',
  category: 'Analytics',
  icon: '📊',
  
  defaultProps: {
    title: 'Metric',
    value: 0,
    trend: 'stable'
  },
  
  constraints: {
    minSize: { w: 2, h: 2 },
    maxSize: { w: 4, h: 3 },
    defaultSize: { w: 3, h: 2 }
  },
  
  // Map legacy props if needed
  propMapping: {
    'header': 'title', // Old prop name -> new prop name
    'amount': 'value'
  }
};
```

### Step 4: Register Component

```typescript
// Add to registry
import { componentRegistry } from '@/components/bento/registry';

componentRegistry.register(
  'DashboardMetric',
  BentoDashboardMetricConfig
);
```

## Page Migration

### Step 1: Analyze Current Page

```typescript
// Original page component
export const DashboardPage = () => {
  const { data, loading } = useDashboardData();
  
  if (loading) return <LoadingSpinner />;
  
  return (
    <DashboardLayout>
      <div className="dashboard-header">
        <h1>Dashboard</h1>
      </div>
      
      <div className="metrics-row">
        <DashboardMetric 
          title="Revenue" 
          value={data.revenue} 
          trend="up" 
        />
        <DashboardMetric 
          title="Users" 
          value={data.users} 
          trend="up" 
        />
        <DashboardMetric 
          title="Orders" 
          value={data.orders} 
          trend="stable" 
        />
      </div>
      
      <div className="charts-section">
        <SalesChart data={data.sales} />
        <UserActivityChart data={data.activity} />
      </div>
      
      <div className="table-section">
        <RecentOrdersTable orders={data.recentOrders} />
      </div>
    </DashboardLayout>
  );
};
```

### Step 2: Create Grid Layout

```typescript
// Define Bento layout
const dashboardLayout: LayoutConfig = {
  name: 'Dashboard Layout',
  description: 'Standard dashboard with metrics, charts, and table',
  category: 'standard',
  
  grids: {
    desktop: {
      columns: 12,
      rows: 10,
      gap: 8,
      components: [
        // Header
        {
          id: 'header',
          componentType: 'PageHeader',
          position: { x: 0, y: 0, w: 12, h: 1 },
          props: { title: 'Dashboard' }
        },
        
        // Metrics row
        {
          id: 'metric-revenue',
          componentType: 'DashboardMetric',
          position: { x: 0, y: 1, w: 4, h: 2 },
          props: { title: 'Revenue' },
          bindings: {
            dataSource: 'dashboard',
            dataPath: 'revenue'
          }
        },
        {
          id: 'metric-users',
          componentType: 'DashboardMetric',
          position: { x: 4, y: 1, w: 4, h: 2 },
          props: { title: 'Users' },
          bindings: {
            dataSource: 'dashboard',
            dataPath: 'users'
          }
        },
        {
          id: 'metric-orders',
          componentType: 'DashboardMetric',
          position: { x: 8, y: 1, w: 4, h: 2 },
          props: { title: 'Orders' },
          bindings: {
            dataSource: 'dashboard',
            dataPath: 'orders'
          }
        },
        
        // Charts
        {
          id: 'sales-chart',
          componentType: 'SalesChart',
          position: { x: 0, y: 3, w: 6, h: 4 },
          bindings: {
            dataSource: 'dashboard',
            dataPath: 'sales'
          }
        },
        {
          id: 'activity-chart',
          componentType: 'UserActivityChart',
          position: { x: 6, y: 3, w: 6, h: 4 },
          bindings: {
            dataSource: 'dashboard',
            dataPath: 'activity'
          }
        },
        
        // Table
        {
          id: 'orders-table',
          componentType: 'RecentOrdersTable',
          position: { x: 0, y: 7, w: 12, h: 3 },
          bindings: {
            dataSource: 'dashboard',
            dataPath: 'recentOrders'
          }
        }
      ]
    },
    
    tablet: {
      columns: 8,
      components: [
        // Responsive layout for tablet
        // Metrics stack to 2x2
        // Charts stack vertically
      ]
    },
    
    mobile: {
      columns: 4,
      components: [
        // Single column layout
        // All components stack vertically
      ]
    }
  }
};
```

### Step 3: Create Page Configuration

```typescript
// Bento page configuration
const dashboardPage: PageConfig = {
  displayName: 'Dashboard',
  route: '/dashboard',
  layoutId: 'dashboard-layout',
  
  bindings: {
    security: {
      requiredRoles: ['user'],
      requiredPermissions: ['view-dashboard']
    },
    
    visibility: {
      showInNavigation: true,
      navigationOrder: 1,
      icon: '📊'
    },
    
    data: {
      sources: [
        {
          id: 'dashboard',
          type: 'api',
          endpoint: '/api/dashboard',
          refresh: {
            interval: 60000 // 1 minute
          }
        }
      ]
    }
  }
};
```

### Step 4: Set Up Routing

```typescript
// Update routing configuration
import { PageRenderer } from '@/components/bento/PageRenderer';

// Add Bento route
<Route 
  path="/dashboard" 
  element={
    <BentoProtectedRoute pageId="dashboard-page">
      <PageRenderer pageId="dashboard-page" />
    </BentoProtectedRoute>
  } 
/>

// Keep legacy route during transition
<Route 
  path="/dashboard-legacy" 
  element={<DashboardPage />} 
/>
```

## Data Migration

### Migrating Data Sources

```typescript
// Original data hook
const useDashboardData = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    fetch('/api/dashboard')
      .then(res => res.json())
      .then(setData)
      .finally(() => setLoading(false));
  }, []);
  
  return { data, loading };
};

// Bento data configuration
const dashboardDataBinding: DataBindingConfig = {
  fields: [
    {
      name: 'revenue',
      type: 'number',
      source: 'api',
      path: '/api/dashboard',
      transform: {
        type: 'map',
        expression: 'data.revenue'
      }
    },
    {
      name: 'users',
      type: 'number',
      source: 'api',
      path: '/api/dashboard',
      transform: {
        type: 'map',
        expression: 'data.users'
      }
    }
  ],
  
  refresh: {
    interval: 60000,
    triggers: [
      {
        type: 'visibility',
        source: 'document',
        condition: 'document.visibilityState === "visible"'
      }
    ]
  }
};
```

## Layout Pattern Migration

### Common Layout Patterns

```typescript
// Pattern 1: Header + Content + Sidebar
const withSidebarLayout: LayoutTemplate = {
  name: 'With Sidebar',
  zones: [
    {
      id: 'header',
      bounds: { x: 0, y: 0, w: 12, h: 1 },
      type: 'header',
      locked: true
    },
    {
      id: 'sidebar',
      bounds: { x: 0, y: 1, w: 3, h: 9 },
      type: 'sidebar',
      accepts: ['Navigation', 'Filters']
    },
    {
      id: 'content',
      bounds: { x: 3, y: 1, w: 9, h: 9 },
      type: 'content'
    }
  ]
};

// Pattern 2: Dashboard Grid
const dashboardGridLayout: LayoutTemplate = {
  name: 'Dashboard Grid',
  zones: [
    {
      id: 'metrics',
      bounds: { x: 0, y: 0, w: 12, h: 2 },
      type: 'custom',
      accepts: ['MetricCard'],
      maxComponents: 4
    },
    {
      id: 'visualizations',
      bounds: { x: 0, y: 2, w: 12, h: 5 },
      type: 'custom',
      accepts: ['Chart', 'Graph']
    },
    {
      id: 'details',
      bounds: { x: 0, y: 7, w: 12, h: 3 },
      type: 'custom',
      accepts: ['Table', 'List']
    }
  ]
};
```

## Migration Tools

### 1. Component Scanner

```typescript
// Scan project for components to migrate
class ComponentScanner {
  async scanProject(): Promise<ComponentScanResult[]> {
    const components: ComponentScanResult[] = [];
    
    // Find all component files
    const files = await glob('src/components/**/*.{tsx,jsx}');
    
    for (const file of files) {
      const content = await fs.readFile(file, 'utf-8');
      const ast = parse(content);
      
      // Find React components
      const componentInfo = this.extractComponentInfo(ast);
      
      if (componentInfo) {
        components.push({
          file,
          name: componentInfo.name,
          props: componentInfo.props,
          hasState: componentInfo.hasState,
          dependencies: componentInfo.dependencies,
          difficulty: this.assessMigrationDifficulty(componentInfo)
        });
      }
    }
    
    return components;
  }
  
  private assessMigrationDifficulty(
    info: ComponentInfo
  ): 'easy' | 'medium' | 'hard' {
    let score = 0;
    
    // Factors that increase difficulty
    if (info.hasState) score += 2;
    if (info.dependencies.length > 3) score += 1;
    if (info.props.length > 10) score += 1;
    if (info.usesContext) score += 2;
    if (info.hasEffects) score += 1;
    
    if (score <= 2) return 'easy';
    if (score <= 4) return 'medium';
    return 'hard';
  }
}
```

### 2. Layout Converter

```typescript
// Convert existing layouts to Bento format
class LayoutConverter {
  convertFromJSX(jsxLayout: string): BentoGrid {
    const ast = parse(jsxLayout);
    const components: GridComponent[] = [];
    
    // Analyze JSX structure
    this.traverseJSX(ast, (node, depth, index) => {
      if (this.isComponent(node)) {
        const position = this.inferPosition(node, depth, index);
        const size = this.inferSize(node);
        
        components.push({
          id: generateId(),
          componentType: node.name,
          position: { ...position, ...size },
          props: this.extractProps(node)
        });
      }
    });
    
    return {
      id: generateId(),
      name: 'Converted Layout',
      device: 'desktop',
      columns: 12,
      gap: 8,
      components,
      settings: {}
    };
  }
  
  private inferPosition(
    node: any, 
    depth: number, 
    index: number
  ): { x: number; y: number } {
    // Simple heuristic: use depth for y, index for x
    return {
      x: (index * 3) % 12,
      y: depth * 2
    };
  }
  
  private inferSize(node: any): { w: number; h: number } {
    // Infer from className or default
    const className = node.props?.className || '';
    
    if (className.includes('full-width')) {
      return { w: 12, h: 2 };
    }
    if (className.includes('half-width')) {
      return { w: 6, h: 2 };
    }
    if (className.includes('third-width')) {
      return { w: 4, h: 2 };
    }
    
    return { w: 3, h: 2 }; // Default
  }
}
```

### 3. Migration Validator

```typescript
// Validate migrated components and pages
class MigrationValidator {
  async validateComponent(
    original: any,
    migrated: BentoComponent
  ): Promise<ValidationReport> {
    const issues: ValidationIssue[] = [];
    
    // Check prop compatibility
    const originalProps = this.extractProps(original);
    const migratedProps = migrated.props || {};
    
    for (const prop of originalProps) {
      if (!(prop in migratedProps)) {
        issues.push({
          type: 'warning',
          message: `Missing prop: ${prop}`,
          suggestion: `Add ${prop} to component configuration`
        });
      }
    }
    
    // Check functionality
    const testResults = await this.runComponentTests(
      original,
      migrated
    );
    
    issues.push(...testResults.issues);
    
    return {
      component: migrated.componentType,
      valid: issues.filter(i => i.type === 'error').length === 0,
      issues
    };
  }
  
  async validatePage(
    originalRoute: string,
    bentoPageId: string
  ): Promise<ValidationReport> {
    const issues: ValidationIssue[] = [];
    
    // Visual regression test
    const screenshots = await this.captureScreenshots(
      originalRoute,
      `/bento/${bentoPageId}`
    );
    
    const diff = await this.compareScreenshots(
      screenshots.original,
      screenshots.bento
    );
    
    if (diff.percentage > 5) {
      issues.push({
        type: 'warning',
        message: `Visual differences detected: ${diff.percentage}%`,
        details: diff.report
      });
    }
    
    // Functional tests
    const functionalTests = await this.runFunctionalTests(
      originalRoute,
      bentoPageId
    );
    
    issues.push(...functionalTests.issues);
    
    return {
      page: bentoPageId,
      valid: issues.filter(i => i.type === 'error').length === 0,
      issues
    };
  }
}
```

## Migration Checklist

### Pre-Migration
- [ ] Audit all components
- [ ] Identify shared layouts
- [ ] Document data sources
- [ ] Plan migration phases
- [ ] Set up Bento infrastructure

### Component Migration
- [ ] Create Bento wrappers
- [ ] Add size constraints
- [ ] Configure data bindings
- [ ] Register in component registry
- [ ] Write component tests

### Page Migration
- [ ] Map page structure to grid
- [ ] Create layout configuration
- [ ] Set up page security
- [ ] Configure data sources
- [ ] Test responsive behavior

### Validation
- [ ] Visual regression tests
- [ ] Functional tests
- [ ] Performance comparison
- [ ] Accessibility audit
- [ ] User acceptance testing

### Cutover
- [ ] Update routing
- [ ] Redirect old URLs
- [ ] Monitor analytics
- [ ] Gather feedback
- [ ] Remove legacy code

## Rollback Plan

### Preparation
```typescript
// Feature flag for gradual rollout
const useBentoPage = (pageId: string): boolean => {
  const flags = useFeatureFlags();
  const user = useAuth();
  
  // Gradual rollout strategy
  if (flags.bentoFullRollout) return true;
  if (flags.bentoBetaUsers && user.isBeta) return true;
  if (flags.bentoPages?.includes(pageId)) return true;
  
  return false;
};

// Dual routing
<Route 
  path="/dashboard" 
  element={
    useBentoPage('dashboard') 
      ? <BentoPageRenderer pageId="dashboard" />
      : <LegacyDashboard />
  } 
/>
```

### Quick Rollback
```typescript
// Emergency rollback switch
const ROLLBACK_FLAG = 'bento:emergency:rollback';

const shouldUseLegacy = (): boolean => {
  return localStorage.getItem(ROLLBACK_FLAG) === 'true';
};

// Monitor for issues
const monitorBentoHealth = () => {
  const errorThreshold = 0.05; // 5% error rate
  const performanceThreshold = 2000; // 2s load time
  
  if (metrics.errorRate > errorThreshold ||
      metrics.pageLoadTime > performanceThreshold) {
    // Auto-rollback
    localStorage.setItem(ROLLBACK_FLAG, 'true');
    notifyOps('Bento auto-rollback triggered');
  }
};
```

## Support and Resources

### Documentation
- Migration guide (this document)
- Component cookbook
- Layout patterns library
- Troubleshooting guide

### Tools
- Component scanner
- Layout converter
- Migration validator
- Visual regression tester

### Support Channels
- #bento-migration Slack channel
- Weekly migration office hours
- Migration dashboard
- Issue tracker

## Next Steps

1. Complete [Getting Started](./01-getting-started.md) guide
2. Review [Component API](./09-component-api.md) for wrapper patterns
3. Study [Page Builder](./05-page-builder.md) for page creation
4. Check [Testing Strategy](./08-testing-strategy.md) for validation