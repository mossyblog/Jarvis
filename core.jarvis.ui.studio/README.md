# Jarvis UI Studio

A sophisticated visual interface builder powered by the Bento Grid System. Build beautiful, responsive layouts using drag-and-drop components with three distinct architectural patterns: Dynamic, Fixed, and Hybrid pages.

## 🚀 Features

- **Visual Page Builder**: Drag-and-drop interface for rapid layout creation
- **Three Page Types**: Choose the right architecture for your needs
- **Responsive Design**: Mobile-first approach with device-specific optimizations
- **Component Registry**: Reusable component library with consistent design
- **Real-time Collaboration**: Multiple users can edit simultaneously
- **Performance Optimized**: Smart rendering and efficient grid calculations
- **Touch-Friendly**: Native touch gestures and mobile interactions
- **Accessibility First**: WCAG compliant components and interactions

## 📋 Quick Start

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Run linting
npm run lint

# Run tests
npm test
```

## 🏗️ Architecture Overview

### Page Types

Jarvis UI Studio supports three distinct page architecture patterns:

1. **🔧 Dynamic Pages** - User-created pages using the visual page builder
2. **🏗️ Fixed Pages** - Developer-created pages with hardcoded layouts  
3. **⚡ Hybrid Pages** - Mix of fixed layout with configurable dynamic regions

> 📖 **[Read the complete Page Types and Architecture Guide →](./docs/bento/09-page-types-and-architecture.md)**

### When to Use Each Type

| Use Case | Dynamic | Fixed | Hybrid |
|----------|---------|-------|--------|
| Business user self-service | ✅ | ❌ | ⚖️ |
| High performance required | ⚖️ | ✅ | ✅ |
| Frequent layout changes | ✅ | ❌ | ⚖️ |
| Complex custom interactions | ❌ | ✅ | ⚖️ |
| Multi-tenant customization | ⚖️ | ❌ | ✅ |
| Rapid prototyping | ✅ | ❌ | ⚖️ |

## 🎨 Component System

### Registry-Based Components

All dynamic components are sourced from the component registry:

```typescript
// Component registration
const COMPONENT_REGISTRY = {
  'metric-card': MetricCard,
  'chart': Chart,
  'table': DataTable,
  'kpi': KPIWidget,
  // ... more components
};

// Usage in dynamic pages
<ComponentRenderer
  component={gridComponent}
  gridSize={{ w: 4, h: 2 }}
  deviceType={DeviceType.Desktop}
/>
```

### Available Components

- **📊 Data Visualization**: Charts, gauges, KPI cards, metrics
- **📝 Content**: Text blocks, headings, rich text editors
- **🔢 Data Display**: Tables, lists, grids, filters
- **🎮 Interactive**: Buttons, forms, input controls
- **🖼️ Media**: Images, videos, galleries, carousels
- **📐 Layout**: Cards, containers, spacers, dividers

## 🖱️ Bento Grid System

The core of Jarvis UI Studio is the Bento Grid - a powerful, responsive grid system inspired by Apple's design language.

### Key Features

- **Magnetic Snapping**: Components snap to grid positions intelligently
- **Smart Drop Zones**: Strategic placement suggestions during drag operations
- **Progressive Grid Visibility**: Grid appears only when needed
- **Multi-Device Support**: Responsive breakpoints for all screen sizes
- **Touch Gestures**: Native mobile interactions with haptic feedback

### Grid Configuration

```typescript
interface BentoGrid {
  columns: number;        // Grid columns (default: 12)
  gap: number;           // Gap between components (px)
  rowHeight?: number;    // Fixed row height (optional)
  components: GridComponent[];
}

interface GridComponent {
  id: string;
  componentType: string;
  position: { x: number; y: number; w: number; h: number };
  props: Record<string, any>;
  responsive?: {
    mobile?: GridPosition;
    tablet?: GridPosition;
    desktop?: GridPosition;
  };
}
```

## 📱 Mobile Experience

### Touch-First Design

- **Long Press**: Enter drag mode on mobile devices
- **Pinch to Zoom**: Scale grid for detailed editing
- **Swipe Gestures**: Access component palette and tools
- **Bottom Sheet**: Mobile-optimized component picker
- **Haptic Feedback**: Physical feedback for interactions

### Mobile Optimizations

```typescript
// Mobile-specific configurations
const mobileConfig = {
  activationConstraint: {
    distance: 10,        // Minimum drag distance
    delay: 150,          // Touch hold delay
    tolerance: 8         // Touch tolerance
  },
  enablePinch: true,     // Pinch to zoom
  enableSwipe: true,     // Swipe gestures
  enableLongPress: true  // Long press for drag mode
};
```

## 🎯 Development Workflow

### Dynamic Page Development

1. **Design Phase**: Define layout requirements in the visual builder
2. **Component Selection**: Choose components from the registry
3. **Data Configuration**: Map data sources and configure fields
4. **Responsive Testing**: Test across device breakpoints
5. **Publication**: Deploy configuration to users

### Fixed Page Development

1. **Requirements Analysis**: Gather functional requirements
2. **Component Design**: Create custom React components
3. **Layout Implementation**: Build responsive layouts
4. **Business Logic**: Implement custom hooks and state management
5. **Testing & QA**: Comprehensive testing pipeline

### Hybrid Page Development

1. **Template Design**: Create fixed structure with dynamic slots
2. **Constraint Definition**: Define rules for dynamic regions
3. **User Training**: Guide business users on customization
4. **Integration Testing**: Validate fixed + dynamic combinations

## 🛠️ Configuration

### Theme System

```typescript
// Tailwind configuration
module.exports = {
  theme: {
    extend: {
      colors: {
        // Semantic color tokens
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: "hsl(var(--primary))",
        secondary: "hsl(var(--secondary))",
        // ... more semantic tokens
      },
      spacing: {
        // 8px grid system
        'xs': '0.25rem',   // 4px
        'sm': '0.5rem',    // 8px
        'md': '1rem',      // 16px
        'lg': '1.5rem',    // 24px
        'xl': '2rem',      // 32px
        '2xl': '3rem',     // 48px
      }
    }
  }
};
```

### Component Configuration

```typescript
// Component type definitions
interface ComponentConfig {
  type: string;
  name: string;
  icon: React.ComponentType;
  category: ComponentCategory;
  defaultProps: Record<string, any>;
  schema: JSONSchema7;
  constraints: {
    minSize: { w: number; h: number };
    maxSize: { w: number; h: number };
    allowedDevices: DeviceType[];
  };
}
```

## 🔧 Advanced Features

### Performance Optimizations

- **Component Lazy Loading**: Dynamic imports for better bundle splitting
- **Grid Virtualization**: Efficient rendering of large layouts
- **Smart Re-rendering**: Memoized components with dependency tracking
- **Throttled Updates**: Optimized drag preview updates

### Accessibility

- **Keyboard Navigation**: Full keyboard support for all interactions
- **Screen Reader Support**: ARIA labels and semantic markup
- **Focus Management**: Logical focus flow during editing
- **Color Contrast**: WCAG AA compliant color combinations

### Internationalization

```typescript
// i18n support
const messages = {
  'en': {
    'component.metric-card.title': 'Metric Card',
    'grid.empty-state': 'Drag components here to start building'
  },
  'es': {
    'component.metric-card.title': 'Tarjeta de Métrica',
    'grid.empty-state': 'Arrastra componentes aquí para comenzar'
  }
};
```

## 📊 Analytics & Monitoring

### Usage Tracking

```typescript
// Track page builder events
analytics.track('page_builder_event', {
  action: 'component_added',
  componentType: 'metric-card',
  pageType: 'dynamic',
  timestamp: Date.now()
});

// Performance monitoring
const observer = new PerformanceObserver((list) => {
  // Track Core Web Vitals
  list.getEntries().forEach(entry => {
    analytics.track('web_vital', {
      metric: entry.name,
      value: entry.value,
      pageType: 'dynamic'
    });
  });
});
```

## 🧪 Testing Strategy

### Component Testing

```typescript
// Component tests
describe('MetricCard', () => {
  it('renders with correct data', () => {
    render(<MetricCard title="Revenue" value={50000} />);
    expect(screen.getByText('Revenue')).toBeInTheDocument();
    expect(screen.getByText('$50,000')).toBeInTheDocument();
  });
});

// Grid system tests
describe('BentoGrid', () => {
  it('handles drag and drop correctly', async () => {
    const { dragAndDrop } = renderBentoGrid();
    await dragAndDrop('component-1', { x: 2, y: 2 });
    expect(onComponentMove).toHaveBeenCalledWith('component-1', { x: 2, y: 2 });
  });
});
```

### Visual Regression Testing

```bash
# Storybook visual testing
npm run test:visual

# Cross-browser testing
npm run test:cross-browser

# Accessibility testing
npm run test:a11y
```

## 📖 Documentation

### Core Documentation

- **[Page Types and Architecture](./docs/bento/09-page-types-and-architecture.md)** - Comprehensive guide to page patterns
- **[Brand Compliance](./docs/brand-compliance.md)** - Design system guidelines and validation
- **[Component Library](./src/components/ui/)** - Reusable UI components
- **[Typography System](./TYPOGRAPHY_AUDIT_SUMMARY.md)** - Typography scales and guidelines

### Development Guides

- **[Sizing System](./SIZING-SYSTEM.md)** - Spacing and sizing conventions
- **[Testing Setup](./TEST_SETUP.md)** - Testing configuration and practices
- **[Brand Compliance Checker](./README-brand-compliance.md)** - Automated design validation

## 🤝 Contributing

### Development Setup

1. Clone the repository
2. Install dependencies: `npm install`
3. Start development server: `npm run dev`
4. Open http://localhost:5173

### Code Quality

- **ESLint**: Automated code linting with TypeScript rules
- **Prettier**: Code formatting with consistent style
- **Brand Compliance**: Automated design system validation
- **Type Safety**: Full TypeScript coverage

### Pull Request Process

1. Create feature branch from `master`
2. Implement changes with tests
3. Run full test suite: `npm test`
4. Ensure brand compliance: `npm run brand-check:strict`
5. Submit PR with detailed description

## 📜 License

Licensed under the MIT License. See LICENSE file for details.

## 🙋 Support

- **Documentation**: Check the `/docs` folder for detailed guides
- **Issues**: Report bugs and feature requests via GitHub Issues
- **Discussions**: Join community discussions for questions and ideas

---

**Built with ❤️ using React, TypeScript, Tailwind CSS, and the power of visual creativity.**