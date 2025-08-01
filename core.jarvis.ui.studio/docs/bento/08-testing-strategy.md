# Testing Strategy

## Overview

This document outlines the comprehensive testing strategy for the Bento Grid System, covering unit tests, integration tests, end-to-end tests, and performance testing.

## Testing Philosophy

```
┌─────────────────────────────────────────────────────────────────┐
│                      Testing Pyramid                              │
│                                                                   │
│                          E2E                                      │
│                         ╱───╲                                     │
│                        ╱     ╲       (5%)                         │
│                       ╱───────╲                                   │
│                      ╱         ╲                                  │
│                     ╱Integration╲    (15%)                        │
│                    ╱─────────────╲                                │
│                   ╱               ╲                               │
│                  ╱   Unit Tests    ╲  (80%)                       │
│                 ╱───────────────────╲                             │
│                ╱─────────────────────╲                            │
└─────────────────────────────────────────────────────────────────┘
```

## Test Categories

### 1. Unit Tests

#### Component Tests

```typescript
// Location: /src/components/bento/__tests__/
describe('BentoGrid', () => {
  describe('Rendering', () => {
    it('should render grid with correct columns', () => {
      const { container } = render(
        <BentoGrid columns={12} gap={8} components={[]} />
      );
      
      const grid = container.querySelector('.bento-grid');
      expect(grid).toHaveStyle({
        '--grid-columns': '12',
        '--grid-gap': '8px'
      });
    });
    
    it('should render components in correct positions', () => {
      const components = [
        {
          id: '1',
          componentType: 'MetricCard',
          position: { x: 0, y: 0, w: 3, h: 2 }
        }
      ];
      
      const { container } = render(
        <BentoGrid columns={12} gap={8} components={components} />
      );
      
      const component = container.querySelector('[data-component-id="1"]');
      expect(component).toHaveStyle({
        gridColumn: '1 / span 3',
        gridRow: '1 / span 2'
      });
    });
  });
  
  describe('Collision Detection', () => {
    it('should detect overlapping components', () => {
      const component1 = {
        id: '1',
        position: { x: 0, y: 0, w: 3, h: 2 }
      };
      
      const component2 = {
        id: '2',
        position: { x: 2, y: 1, w: 3, h: 2 }
      };
      
      expect(detectCollision(component2, [component1])).toBe(true);
    });
    
    it('should not detect non-overlapping components', () => {
      const component1 = {
        id: '1',
        position: { x: 0, y: 0, w: 3, h: 2 }
      };
      
      const component2 = {
        id: '2',
        position: { x: 3, y: 0, w: 3, h: 2 }
      };
      
      expect(detectCollision(component2, [component1])).toBe(false);
    });
  });
});
```

#### Service Tests

```typescript
// Location: /src/services/bento/__tests__/
describe('ComponentRegistry', () => {
  let registry: ComponentRegistry;
  
  beforeEach(() => {
    registry = new ComponentRegistry();
  });
  
  describe('Registration', () => {
    it('should register a component', () => {
      const config = {
        component: MetricCard,
        displayName: 'Metric Card',
        category: 'Analytics',
        constraints: {
          minSize: { w: 2, h: 2 },
          maxSize: { w: 4, h: 4 }
        }
      };
      
      registry.register('MetricCard', config);
      expect(registry.get('MetricCard')).toEqual(config);
    });
    
    it('should throw on duplicate registration', () => {
      registry.register('Test', { component: () => null });
      
      expect(() => {
        registry.register('Test', { component: () => null });
      }).toThrow('Component "Test" is already registered');
    });
  });
  
  describe('Retrieval', () => {
    it('should get components by category', () => {
      registry.register('Metric1', { 
        category: 'Analytics',
        component: () => null 
      });
      registry.register('Metric2', { 
        category: 'Analytics',
        component: () => null 
      });
      registry.register('Table1', { 
        category: 'Data',
        component: () => null 
      });
      
      const analytics = registry.getByCategory('Analytics');
      expect(analytics).toHaveLength(2);
    });
    
    it('should search components', () => {
      registry.register('MetricCard', { 
        displayName: 'Metric Card',
        tags: ['metric', 'kpi'],
        component: () => null 
      });
      
      const results = registry.search('metric');
      expect(results).toHaveLength(1);
      expect(results[0].displayName).toBe('Metric Card');
    });
  });
});
```

#### Utility Tests

```typescript
// Location: /src/utils/bento/__tests__/
describe('Grid Utilities', () => {
  describe('snapToGrid', () => {
    it('should snap position to grid', () => {
      const position = { x: 83, y: 147 };
      const gridSize = 20;
      
      expect(snapToGrid(position, gridSize)).toEqual({
        x: 80,
        y: 140
      });
    });
  });
  
  describe('validateBounds', () => {
    it('should validate position within bounds', () => {
      const position = { x: 0, y: 0, w: 3, h: 2 };
      expect(validateBounds(position, 12)).toBe(true);
      
      const invalidPosition = { x: 10, y: 0, w: 3, h: 2 };
      expect(validateBounds(invalidPosition, 12)).toBe(false);
    });
  });
  
  describe('packComponents', () => {
    it('should pack components efficiently', () => {
      const components = [
        { id: '1', position: { x: 0, y: 2, w: 2, h: 2 } },
        { id: '2', position: { x: 4, y: 4, w: 2, h: 2 } }
      ];
      
      const packed = packComponents(components);
      
      expect(packed[0].position.y).toBe(0); // Moved up
      expect(packed[1].position.y).toBe(0); // Moved up
      expect(packed[1].position.x).toBe(2); // Moved left
    });
  });
});
```

### 2. Integration Tests

#### Grid Editor Integration

```typescript
// Location: /src/tests/integration/
describe('Grid Editor Integration', () => {
  it('should handle drag and drop workflow', async () => {
    const { getByTestId, getByText } = render(
      <BentoProvider>
        <GridEditor gridId="test" device="desktop" />
      </BentoProvider>
    );
    
    // Drag from palette
    const metricCard = getByText('Metric Card');
    const grid = getByTestId('grid-canvas');
    
    await userEvent.drag(metricCard, grid);
    
    // Verify component added
    expect(getByTestId('component-1')).toBeInTheDocument();
    
    // Verify position
    const component = getByTestId('component-1');
    expect(component).toHaveStyle({
      gridColumn: '1 / span 2'
    });
  });
  
  it('should update properties in real-time', async () => {
    const { getByTestId, getByLabelText } = render(
      <BentoProvider>
        <GridEditor gridId="test" device="desktop" />
      </BentoProvider>
    );
    
    // Select component
    const component = getByTestId('component-1');
    await userEvent.click(component);
    
    // Update property
    const titleInput = getByLabelText('Title');
    await userEvent.clear(titleInput);
    await userEvent.type(titleInput, 'New Title');
    
    // Verify update
    expect(component).toHaveTextContent('New Title');
  });
});
```

#### Data Flow Integration

```typescript
describe('Data Binding Integration', () => {
  it('should bind data to components', async () => {
    const mockApi = {
      getMetric: jest.fn().mockResolvedValue({ value: 42 })
    };
    
    const { getByTestId } = render(
      <ApiProvider value={mockApi}>
        <PageRenderer pageId="test-page" />
      </ApiProvider>
    );
    
    await waitFor(() => {
      const metricCard = getByTestId('metric-card-1');
      expect(metricCard).toHaveTextContent('42');
    });
    
    expect(mockApi.getMetric).toHaveBeenCalledWith('sales.total');
  });
  
  it('should refresh data on interval', async () => {
    jest.useFakeTimers();
    
    const mockApi = {
      getMetric: jest.fn()
        .mockResolvedValueOnce({ value: 42 })
        .mockResolvedValueOnce({ value: 43 })
    };
    
    render(
      <ApiProvider value={mockApi}>
        <PageRenderer pageId="test-page" />
      </ApiProvider>
    );
    
    // Initial load
    await waitFor(() => {
      expect(mockApi.getMetric).toHaveBeenCalledTimes(1);
    });
    
    // Advance time
    jest.advanceTimersByTime(60000);
    
    await waitFor(() => {
      expect(mockApi.getMetric).toHaveBeenCalledTimes(2);
    });
    
    jest.useRealTimers();
  });
});
```

### 3. End-to-End Tests

#### Page Creation Flow

```typescript
// Location: /e2e/bento/
describe('Page Creation E2E', () => {
  beforeEach(async () => {
    await page.goto('/admin/pages');
    await page.waitForSelector('[data-testid="page-list"]');
  });
  
  it('should create a new page', async () => {
    // Click new page button
    await page.click('[data-testid="new-page-button"]');
    
    // Fill page details
    await page.fill('[name="displayName"]', 'Test Dashboard');
    await page.fill('[name="route"]', '/test-dashboard');
    
    // Select layout
    await page.click('[data-testid="layout-standard"]');
    
    // Configure security
    await page.click('[data-testid="role-user"]');
    
    // Save page
    await page.click('[data-testid="save-page"]');
    
    // Verify navigation
    await expect(page).toHaveURL('/admin/pages/edit/');
    
    // Edit layout
    await page.click('[data-testid="edit-layout"]');
    
    // Drag component
    const metricCard = await page.$('[data-testid="palette-metric-card"]');
    const grid = await page.$('[data-testid="grid-canvas"]');
    
    await metricCard.dragTo(grid);
    
    // Save grid
    await page.click('[data-testid="save-grid"]');
    
    // Publish page
    await page.click('[data-testid="publish-page"]');
    
    // Verify page is live
    await page.goto('/test-dashboard');
    await expect(page).toHaveSelector('[data-testid="metric-card"]');
  });
});
```

#### Grid Editing E2E

```typescript
describe('Grid Editing E2E', () => {
  it('should handle complex grid operations', async () => {
    await page.goto('/admin/pages/edit/dashboard');
    await page.click('[data-testid="edit-layout"]');
    
    // Add multiple components
    for (let i = 0; i < 5; i++) {
      const component = await page.$(`[data-testid="palette-metric-card"]`);
      const grid = await page.$('[data-testid="grid-canvas"]');
      await component.dragTo(grid);
    }
    
    // Resize component
    const resizeHandle = await page.$('[data-testid="resize-handle-se"]');
    await resizeHandle.drag({ x: 100, y: 50 });
    
    // Move component
    const component = await page.$('[data-testid="component-1"]');
    await component.drag({ x: 200, y: 0 });
    
    // Delete component
    await page.click('[data-testid="component-2"]');
    await page.keyboard.press('Delete');
    
    // Undo operation
    await page.keyboard.press('Control+z');
    
    // Verify component restored
    await expect(page).toHaveSelector('[data-testid="component-2"]');
    
    // Save changes
    await page.click('[data-testid="save-grid"]');
    
    // Verify persistence
    await page.reload();
    await expect(page).toHaveSelector('[data-testid="component-1"]');
    await expect(page).toHaveSelector('[data-testid="component-2"]');
  });
});
```

### 4. Performance Tests

#### Render Performance

```typescript
// Location: /src/tests/performance/
describe('Render Performance', () => {
  it('should render 100 components in under 100ms', async () => {
    const components = Array.from({ length: 100 }, (_, i) => ({
      id: `comp-${i}`,
      componentType: 'MetricCard',
      position: {
        x: (i % 12) * 1,
        y: Math.floor(i / 12) * 2,
        w: 1,
        h: 2
      }
    }));
    
    const startTime = performance.now();
    
    render(
      <BentoGrid columns={12} gap={8} components={components} />
    );
    
    const endTime = performance.now();
    const renderTime = endTime - startTime;
    
    expect(renderTime).toBeLessThan(100);
  });
  
  it('should handle rapid updates efficiently', async () => {
    const { rerender } = render(
      <BentoGrid columns={12} gap={8} components={[]} />
    );
    
    const updates = [];
    
    // Measure update performance
    for (let i = 0; i < 50; i++) {
      const startTime = performance.now();
      
      rerender(
        <BentoGrid 
          columns={12} 
          gap={8} 
          components={[{ 
            id: '1', 
            position: { x: i % 12, y: 0, w: 1, h: 1 } 
          }]} 
        />
      );
      
      const endTime = performance.now();
      updates.push(endTime - startTime);
    }
    
    const avgUpdateTime = updates.reduce((a, b) => a + b) / updates.length;
    expect(avgUpdateTime).toBeLessThan(16); // 60fps threshold
  });
});
```

#### Memory Tests

```typescript
describe('Memory Performance', () => {
  it('should not leak memory on component unmount', async () => {
    const measureMemory = () => {
      if (performance.memory) {
        return performance.memory.usedJSHeapSize;
      }
      return 0;
    };
    
    const initialMemory = measureMemory();
    
    // Mount and unmount many times
    for (let i = 0; i < 100; i++) {
      const { unmount } = render(
        <BentoGrid columns={12} gap={8} components={[]} />
      );
      unmount();
    }
    
    // Force garbage collection (if available)
    if (global.gc) {
      global.gc();
    }
    
    await new Promise(resolve => setTimeout(resolve, 100));
    
    const finalMemory = measureMemory();
    const memoryIncrease = finalMemory - initialMemory;
    
    // Allow for some increase but not linear with iterations
    expect(memoryIncrease).toBeLessThan(1000000); // 1MB
  });
});
```

## Test Infrastructure

### Test Utilities

```typescript
// Location: /src/test-utils/bento/
export const renderInGrid = (
  component: React.ReactElement,
  size: Size = { w: 2, h: 2 }
) => {
  const gridComponent: GridComponent = {
    id: 'test-component',
    componentType: 'TestComponent',
    position: { x: 0, y: 0, ...size }
  };
  
  return render(
    <BentoGrid columns={12} gap={8} components={[gridComponent]}>
      {component}
    </BentoGrid>
  );
};

export const createMockRegistry = () => {
  const registry = new ComponentRegistry();
  
  // Register common test components
  registry.register('TestComponent', {
    component: ({ children }) => <div>{children}</div>,
    displayName: 'Test Component',
    category: 'Test',
    constraints: {
      minSize: { w: 1, h: 1 },
      maxSize: { w: 12, h: 12 }
    }
  });
  
  return registry;
};

export const mockDragEvent = (
  source: HTMLElement,
  target: HTMLElement
) => {
  const dataTransfer = {
    setData: jest.fn(),
    getData: jest.fn().mockReturnValue('component-id'),
    effectAllowed: 'move'
  };
  
  fireEvent.dragStart(source, { dataTransfer });
  fireEvent.dragEnter(target, { dataTransfer });
  fireEvent.dragOver(target, { dataTransfer });
  fireEvent.drop(target, { dataTransfer });
  fireEvent.dragEnd(source, { dataTransfer });
};
```

### Mock Data Builders

```typescript
// Location: /src/test-utils/bento/builders/
export const buildPage = (overrides?: Partial<BentoPage>): BentoPage => ({
  id: 'page-1',
  displayName: 'Test Page',
  route: '/test',
  layoutId: 'layout-1',
  status: PageStatus.Draft,
  version: 1,
  bindings: {
    security: { isPublic: false },
    visibility: { showInNavigation: true }
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'user-1',
  updatedBy: 'user-1',
  ...overrides
});

export const buildGrid = (overrides?: Partial<BentoGrid>): BentoGrid => ({
  id: 'grid-1',
  name: 'Test Grid',
  device: DeviceType.Desktop,
  columns: 12,
  gap: 8,
  components: [],
  settings: {},
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides
});

export const buildComponent = (
  overrides?: Partial<GridComponent>
): GridComponent => ({
  id: 'component-1',
  componentType: 'MetricCard',
  position: { x: 0, y: 0, w: 3, h: 2 },
  props: {},
  ...overrides
});
```

## Testing Best Practices

### 1. Component Testing
- Test components in isolation
- Use data-testid for reliable selection
- Test user interactions
- Verify accessibility

### 2. Integration Testing
- Test data flow between components
- Verify state management
- Test error scenarios
- Check loading states

### 3. E2E Testing
- Test complete user flows
- Use realistic data
- Test on multiple viewports
- Verify persistence

### 4. Performance Testing
- Set performance budgets
- Test with realistic data volumes
- Monitor memory usage
- Profile render times

## CI/CD Integration

### Test Pipeline

```yaml
# .github/workflows/bento-tests.yml
name: Bento Tests

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npm run test:unit -- --coverage
      - uses: codecov/codecov-action@v3

  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npm run test:integration

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npx playwright install
      - run: npm run test:e2e

  performance-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npm run test:performance
      - uses: actions/upload-artifact@v3
        with:
          name: performance-results
          path: performance-results.json
```

## Test Coverage Goals

### Coverage Targets
- Statements: 85%
- Branches: 80%
- Functions: 85%
- Lines: 85%

### Critical Path Coverage
- Grid rendering: 95%
- Drag and drop: 90%
- Data binding: 90%
- Security: 100%

## Next Steps

1. Set up test infrastructure
2. Write initial test suites
3. Configure CI/CD pipeline
4. Establish performance baselines
5. Create test documentation