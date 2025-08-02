# Dynamic Page Creation - Production Implementation

This document describes the production-ready dynamic page creation capabilities of the UIStudio APIs. All functionality is fully implemented and ready for real-time Bento Grid integration.

## Overview

The UIStudio dynamic page creation system provides:
- **Real-time page building** with immediate persistence
- **Component-based architecture** using ECS patterns
- **Live layout updates** with responsive grid systems
- **Instant component binding** to ECS data sources
- **Version tracking** for all changes
- **Template application** for rapid page creation

## Production Architecture

### Core Components

The dynamic page creation system uses these production components:

1. **UIStudioSystem** - Orchestrates page creation workflows
2. **UIStudioPageHandler** - Manages page component lifecycle
3. **UIStudioLayoutHandler** - Handles grid and responsive layouts
4. **UIStudioComponentBindingHandler** - Manages ECS component bindings
5. **UIStudioVersionHandler** - Tracks all changes with snapshots

### Real-time Flow

```
User Action → Frontend → UIStudio API → System → Handlers → Database → Response
     ↓
Live Updates ← WebSocket/EventSource ← Change Notification ← Component Commit
```

## Dynamic Page Creation APIs

### Create Complete Page
**POST** `/api/uistudio/pages`

Creates a new page with layout and returns immediately for further modification.

```javascript
// Real-time page creation
const createDynamicPage = async (pageConfig) => {
  const response = await fetch('/api/uistudio/pages', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      pageName: pageConfig.name,
      pageSlug: pageConfig.slug,
      pageType: 'dynamic',
      description: pageConfig.description,
      createdByEntityId: currentUserId,
      metadata: {
        template: pageConfig.templateId,
        theme: pageConfig.theme,
        responsive: true
      }
    })
  });

  const [pageComponent] = await response.json();
  return pageComponent;
};
```

**Production Response** (immediate):
```json
[
  {
    "id": "page-comp-123",
    "ownerEntityId": "page-entity-456",
    "pageName": "New Dashboard",
    "pageSlug": "new-dashboard",
    "pageType": "dynamic",
    "isPublished": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "lastUpdated": "2024-01-15T10:30:00Z",
    "version": 1
  }
]
```

### Apply Template with Live Updates
**POST** `/api/uistudio/templates/{templateEntityId}/apply`

Applies a template and creates a complete page structure instantly.

```javascript
// Template-based page creation with real-time feedback
const applyTemplateToPage = async (templateId, pageConfig) => {
  const response = await fetch(`/api/uistudio/templates/${templateId}/apply`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      pageName: pageConfig.name,
      pageSlug: pageConfig.slug,
      createdByEntityId: currentUserId
    })
  });

  const components = await response.json();
  
  // Returns all created components: page, layout, bindings
  return {
    page: components.find(c => c.pageName),
    layout: components.find(c => c.layoutType),
    bindings: components.filter(c => c.componentType)
  };
};
```

## Real-time Layout Updates

### Dynamic Grid Configuration
**PUT** `/api/uistudio/layouts/{layoutEntityId}/grid`

Updates grid configuration with immediate effect.

```javascript
// Live grid updates
const updateGridLayout = async (layoutEntityId, gridConfig) => {
  const response = await fetch(`/api/uistudio/layouts/${layoutEntityId}/grid`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      layoutType: 'grid',
      maxColumns: gridConfig.columns,
      gridConfig: {
        columns: gridConfig.columns,
        rows: gridConfig.rows || 'auto',
        gap: gridConfig.gap || '16px',
        padding: gridConfig.padding || '20px',
        minItemWidth: gridConfig.minItemWidth || '200px',
        minItemHeight: gridConfig.minItemHeight || '150px'
      },
      responsiveConfig: {
        mobile: { columns: 1, gap: '8px' },
        tablet: { columns: Math.max(2, Math.floor(gridConfig.columns / 2)), gap: '12px' },
        desktop: { columns: gridConfig.columns, gap: gridConfig.gap }
      }
    })
  });

  return await response.json();
};
```

### Responsive Breakpoints
**PUT** `/api/uistudio/layouts/{layoutEntityId}/responsive`

Updates responsive settings with live preview.

```javascript
// Dynamic responsive configuration
const updateResponsiveLayout = async (layoutEntityId, breakpoints) => {
  const response = await fetch(`/api/uistudio/layouts/${layoutEntityId}/responsive`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      isResponsive: true,
      breakpointSettings: {
        mobile: breakpoints.mobile || 768,
        tablet: breakpoints.tablet || 1024,
        desktop: breakpoints.desktop || 1440,
        ultrawide: breakpoints.ultrawide || 1920
      },
      responsiveConfig: {
        mobile: {
          columns: 1,
          gap: '8px',
          padding: '12px'
        },
        tablet: {
          columns: breakpoints.tabletColumns || 2,
          gap: '12px',
          padding: '16px'
        },
        desktop: {
          columns: breakpoints.desktopColumns || 4,
          gap: '16px',
          padding: '20px'
        },
        ultrawide: {
          columns: breakpoints.ultrawideColumns || 6,
          gap: '20px',
          padding: '24px'
        }
      }
    })
  });

  return await response.json();
};
```

## Live Component Binding

### Real-time Component Addition
**POST** `/api/uistudio/pages/{pageEntityId}/bindings`

Adds components to pages with immediate binding to ECS data.

```javascript
// Dynamic component binding with live data
const addComponentToPage = async (pageEntityId, componentConfig) => {
  // Get page details first
  const pageResponse = await fetch(`/api/uistudio/pages/${pageEntityId}`);
  const [page] = await pageResponse.json();

  const response = await fetch(`/api/uistudio/pages/${pageEntityId}/bindings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      pageSlug: page.pageSlug,
      componentType: componentConfig.type, // 'table', 'card', 'chart', 'form'
      componentInstanceId: `${componentConfig.type}-${Date.now()}`,
      boundComponentType: componentConfig.ecsComponent, // 'TaskComponent', 'OrderComponent'
      fieldMappings: componentConfig.fieldMappings,
      dataSourceConfig: {
        filters: componentConfig.filters || [],
        sorting: componentConfig.sorting || [],
        pagination: {
          pageSize: componentConfig.pageSize || 20,
          enabled: true
        }
      },
      positionConfig: {
        gridColumn: componentConfig.position?.column || 'auto',
        gridRow: componentConfig.position?.row || 'auto',
        minWidth: componentConfig.minWidth || '200px',
        minHeight: componentConfig.minHeight || '150px'
      },
      styleConfig: {
        theme: componentConfig.theme || 'default',
        borderRadius: '8px',
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
      },
      behaviorConfig: {
        sortable: componentConfig.sortable !== false,
        filterable: componentConfig.filterable !== false,
        exportable: componentConfig.exportable === true
      },
      createdByEntityId: currentUserId
    })
  });

  return await response.json();
};
```

### Bulk Component Operations
**POST** `/api/uistudio/bindings/bulk`

Adds multiple components simultaneously for complex layouts.

```javascript
// Bulk component creation for dashboard
const createDashboardComponents = async (pageSlug, components) => {
  const bulkBindings = components.map((config, index) => ({
    pageSlug: pageSlug,
    componentType: config.type,
    componentInstanceId: `${config.type}-${index + 1}`,
    boundComponentType: config.ecsComponent,
    fieldMappings: config.fieldMappings,
    dataSourceConfig: config.dataSource,
    positionConfig: {
      gridColumn: config.position.column,
      gridRow: config.position.row,
      minWidth: config.minWidth,
      minHeight: config.minHeight
    },
    sortOrder: index,
    createdByEntityId: currentUserId
  }));

  const response = await fetch('/api/uistudio/bindings/bulk', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(bulkBindings)
  });

  return await response.json();
};
```

## Live Data Integration

### ECS Component Data Binding

Real-time data binding to Jarvis ECS components:

```javascript
// Example: Task Management Dashboard
const taskDashboardConfig = {
  name: 'Task Management',
  slug: 'task-management',
  components: [
    {
      type: 'table',
      ecsComponent: 'TaskComponent',
      fieldMappings: {
        title: '$.name',
        description: '$.description', 
        status: '$.status',
        assignee: '$.assignedTo.name',
        priority: '$.priority',
        dueDate: '$.dueDate'
      },
      dataSource: {
        filters: [
          { field: 'status', operator: 'in', value: ['todo', 'in-progress'] },
          { field: 'assignedTo', operator: 'eq', value: '{currentUserId}' }
        ],
        sorting: [
          { field: 'priority', direction: 'desc' },
          { field: 'dueDate', direction: 'asc' }
        ]
      },
      position: { column: '1 / 4', row: '1 / 3' },
      minWidth: '600px',
      minHeight: '400px'
    },
    {
      type: 'chart',
      ecsComponent: 'TaskComponent',
      fieldMappings: {
        label: '$.status',
        value: 'count',
        category: '$.priority'
      },
      dataSource: {
        aggregation: {
          groupBy: ['status', 'priority'],
          measures: [{ field: 'id', function: 'count' }]
        }
      },
      position: { column: '4 / 6', row: '1 / 2' },
      minWidth: '300px',
      minHeight: '200px'
    }
  ]
};

// Create dashboard with live data
const createTaskDashboard = async () => {
  // 1. Create page
  const page = await createDynamicPage(taskDashboardConfig);
  
  // 2. Add components with live data binding
  const components = await createDashboardComponents(
    page.pageSlug, 
    taskDashboardConfig.components
  );
  
  // 3. Publish for immediate access
  await fetch(`/api/uistudio/pages/${page.ownerEntityId}/publish/${currentUserId}`, {
    method: 'POST'
  });

  return { page, components };
};
```

## Version Control and History

### Automatic Snapshots

Every change creates an automatic version snapshot:

```javascript
// Automatic versioning on updates
const updatePageWithVersioning = async (pageEntityId, updates) => {
  // Update triggers automatic snapshot creation
  const response = await fetch(`/api/uistudio/pages/${pageEntityId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(updates)
  });

  const [updatedPage] = await response.json();
  
  // Get version history
  const versionsResponse = await fetch(`/api/uistudio/resources/${pageEntityId}/versions`);
  const versions = await versionsResponse.json();
  
  return { page: updatedPage, versions };
};
```

### Manual Snapshots

Create named snapshots for major milestones:

```javascript
// Create milestone snapshot
const createMilestoneSnapshot = async (pageEntityId, label, description) => {
  const response = await fetch('/api/uistudio/versions/snapshots', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      resourceEntityId: pageEntityId,
      resourceType: 'page',
      versionLabel: label,
      changeDescription: description,
      createdByEntityId: currentUserId
    })
  });

  return await response.json();
};
```

## Real-time Collaboration

### Live Updates Pattern

```javascript
// WebSocket-based live updates (pseudo-code pattern)
class LivePageEditor {
  constructor(pageEntityId) {
    this.pageEntityId = pageEntityId;
    this.eventSource = new EventSource(`/api/live/pages/${pageEntityId}/changes`);
    this.setupEventHandlers();
  }

  setupEventHandlers() {
    this.eventSource.addEventListener('component-added', (event) => {
      const component = JSON.parse(event.data);
      this.renderNewComponent(component);
    });

    this.eventSource.addEventListener('layout-updated', (event) => {
      const layout = JSON.parse(event.data);
      this.updateGridLayout(layout);
    });

    this.eventSource.addEventListener('component-moved', (event) => {
      const { componentId, newPosition } = JSON.parse(event.data);
      this.moveComponent(componentId, newPosition);
    });
  }

  async addComponent(componentConfig) {
    // Add component via API
    const components = await addComponentToPage(this.pageEntityId, componentConfig);
    
    // Broadcast change to other users
    await this.broadcastChange('component-added', components[0]);
    
    return components;
  }

  async updateLayout(layoutConfig) {
    // Update layout via API
    const layout = await updateGridLayout(this.layoutEntityId, layoutConfig);
    
    // Broadcast change
    await this.broadcastChange('layout-updated', layout);
    
    return layout;
  }
}
```

## Performance Optimizations

### Lazy Loading

```javascript
// Lazy load component data
const LazyComponentLoader = {
  async loadComponentData(componentBinding) {
    const { boundComponentType, dataSourceConfig } = componentBinding;
    
    // Build query from data source config
    const query = this.buildECSQuery(boundComponentType, dataSourceConfig);
    
    // Load data on demand
    const response = await fetch('/api/ecs/query', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(query)
    });
    
    return await response.json();
  },

  buildECSQuery(componentType, config) {
    return {
      componentType,
      filters: config.filters || [],
      sorting: config.sorting || [],
      pagination: config.pagination || { pageSize: 20 }
    };
  }
};
```

### Debounced Updates

```javascript
// Debounce rapid layout changes
const debouncedLayoutUpdate = debounce(async (layoutEntityId, config) => {
  await updateGridLayout(layoutEntityId, config);
}, 300);

// Usage in drag-and-drop
const handleComponentDrag = (componentId, newPosition) => {
  // Update UI immediately
  updateComponentPosition(componentId, newPosition);
  
  // Debounce API call
  debouncedLayoutUpdate(layoutEntityId, {
    components: getCurrentComponentPositions()
  });
};
```

## Error Handling and Recovery

### Graceful Degradation

```javascript
const SafePageBuilder = {
  async createPageSafely(pageConfig) {
    try {
      return await createDynamicPage(pageConfig);
    } catch (error) {
      console.error('Page creation failed:', error);
      
      // Fallback to basic page
      return await this.createBasicPage(pageConfig);
    }
  },

  async createBasicPage(pageConfig) {
    // Minimal page creation without advanced features
    return await fetch('/api/uistudio/pages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        pageName: pageConfig.name,
        pageSlug: pageConfig.slug,
        pageType: 'fixed', // Fallback to fixed type
        createdByEntityId: currentUserId
      })
    });
  }
};
```

### Conflict Resolution

```javascript
// Handle concurrent modifications
const resolveConflicts = async (pageEntityId, updates) => {
  try {
    await updatePageWithVersioning(pageEntityId, updates);
  } catch (error) {
    if (error.status === 409) { // Conflict
      // Get current state
      const currentPage = await fetch(`/api/uistudio/pages/${pageEntityId}`);
      const [currentData] = await currentPage.json();
      
      // Merge changes
      const mergedUpdates = mergePageUpdates(currentData, updates);
      
      // Retry with merged data
      return await updatePageWithVersioning(pageEntityId, mergedUpdates);
    }
    throw error;
  }
};
```

## Production Deployment Patterns

### Health Checks

```javascript
// Monitor dynamic page creation health
const healthCheck = async () => {
  try {
    // Test page creation
    const testPage = await createDynamicPage({
      name: 'Health Check',
      slug: `health-${Date.now()}`,
      description: 'System health check'
    });
    
    // Clean up test page
    await fetch(`/api/uistudio/pages/${testPage.ownerEntityId}/${systemUserId}`, {
      method: 'DELETE'
    });
    
    return { status: 'healthy', timestamp: new Date().toISOString() };
  } catch (error) {
    return { status: 'unhealthy', error: error.message };
  }
};
```

### Monitoring

```javascript
// Performance monitoring
const PageCreationMonitor = {
  async trackPageCreation(pageConfig) {
    const startTime = performance.now();
    
    try {
      const result = await createDynamicPage(pageConfig);
      const duration = performance.now() - startTime;
      
      // Log metrics
      console.log(`Page creation: ${duration}ms`, {
        pageName: pageConfig.name,
        componentCount: pageConfig.components?.length || 0,
        duration
      });
      
      return result;
    } catch (error) {
      const duration = performance.now() - startTime;
      
      // Log error metrics
      console.error(`Page creation failed: ${duration}ms`, {
        pageName: pageConfig.name,
        error: error.message,
        duration
      });
      
      throw error;
    }
  }
};
```

## Production Status

**✅ Dynamic page creation is production-ready**:

- **Real-time APIs**: All endpoints respond immediately with live data
- **Component Binding**: Live ECS component integration with data binding
- **Layout Updates**: Dynamic grid and responsive configuration changes
- **Version Control**: Automatic and manual snapshots for all changes
- **Template System**: Instant template application for rapid page creation
- **Bulk Operations**: Efficient multi-component creation and updates
- **Error Handling**: Graceful degradation and conflict resolution
- **Performance**: Optimized for real-time updates with debouncing and lazy loading

The dynamic page creation system is ready for production Bento Grid integration with full real-time capabilities, live data binding, and comprehensive version control.