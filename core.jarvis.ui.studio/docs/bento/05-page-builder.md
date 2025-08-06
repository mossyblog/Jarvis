# Page Builder

## Overview

The Page Builder is the visual interface for creating and managing Bento pages. It provides a comprehensive UI for page configuration, layout design, and component arrangement.

## Page Builder Interface

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Page Builder - New Page                              [Preview] [Publish] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌─────────────────────────┐  ┌────────────────────────────────────────┐│
│  │     Page Settings        │  │          Visual Preview                ││
│  │                          │  │                                        ││
│  │ Display Name:            │  │  ┌──────────────────────────────────┐ ││
│  │ [Dashboard___________]   │  │  │        Dashboard                 │ ││
│  │                          │  │  │                                  │ ││
│  │ Route:                   │  │  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐  │ ││
│  │ [/dashboard__________]   │  │  │  │ A  │ │ B  │ │ C  │ │ D  │  │ ││
│  │                          │  │  │  └────┘ └────┘ └────┘ └────┘  │ ││
│  │ Layout:                  │  │  │                                  │ ││
│  │ [Standard Layout    ▼]   │  │  │  ┌──────────────────────────┐  │ ││
│  │                          │  │  │  │          Chart           │  │ ││
│  │ ─────────────────────    │  │  │  └──────────────────────────┘  │ ││
│  │                          │  │  └──────────────────────────────────┘ ││
│  │ 🔒 Security              │  │                                        ││
│  │                          │  │  Device: [Desktop ▼] [Edit Layout]    ││
│  │ Required Roles:          │  └────────────────────────────────────────┘│
│  │ [✓] User                 │                                             │
│  │ [ ] Admin                │  ┌────────────────────────────────────────┐│
│  │ [ ] Super Admin          │  │          Page Structure                ││
│  │                          │  │                                        ││
│  │ Required Permissions:    │  │  Page: Dashboard                       ││
│  │ [+ Add Permission]       │  │  └─ Layout: Standard Layout            ││
│  │                          │  │     ├─ Desktop Grid (12 cols)          ││
│  │ ─────────────────────    │  │     ├─ Tablet Grid (8 cols)            ││
│  │                          │  │     └─ Mobile Grid (4 cols)            ││
│  │ 📍 Navigation            │  │                                        ││
│  │                          │  │  Components: 5                         ││
│  │ [✓] Show in Navigation   │  │  Last Modified: 2 mins ago             ││
│  │                          │  └────────────────────────────────────────┘│
│  │ Icon: [📊_____________]  │                                             │
│  │                          │                                             │
│  │ Order: [10_____]         │                                             │
│  │                          │                                             │
│  └─────────────────────────┘                                             │
└─────────────────────────────────────────────────────────────────────────┘
```

## Page Creation Workflow

### Step 1: Basic Configuration

```typescript
interface PageBasicConfig {
  displayName: string;    // User-friendly name
  route: string;          // URL path
  layoutId: string;       // Selected layout template
  description?: string;   // Optional description
}

// Validation rules
const validatePageConfig = (config: PageBasicConfig): ValidationResult => {
  const errors: string[] = [];
  
  // Display name validation
  if (!config.displayName || config.displayName.length < 3) {
    errors.push('Display name must be at least 3 characters');
  }
  
  // Route validation
  if (!config.route.startsWith('/')) {
    errors.push('Route must start with /');
  }
  
  if (!/^\/[a-z0-9-\/]*$/.test(config.route)) {
    errors.push('Route can only contain lowercase letters, numbers, hyphens, and slashes');
  }
  
  return {
    valid: errors.length === 0,
    errors
  };
};
```

### Step 2: Security Configuration

```
┌─────────────────────────────────────────────────────────────────┐
│  Security Settings                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Access Control:                                                  │
│  ○ Public (No authentication required)                            │
│  ● Protected (Authentication required)                            │
│                                                                   │
│  Role-Based Access:                                               │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ [✓] User          │ Basic user access                       │ │
│  │ [ ] Admin         │ Administrative access                  │ │
│  │ [ ] Super Admin   │ Full system access                     │ │
│  │ [✓] Analyst       │ Data analysis access                   │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  Permission-Based Access:                                         │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ • view-dashboard                                [Remove]     │ │
│  │ • manage-users                                  [Remove]     │ │
│  └─────────────────────────────────────────────────────────────┘ │
│  [+ Add Permission]                                               │
│                                                                   │
│  Advanced Rules:                                                  │
│  [+ Add Custom Rule]                                              │
└─────────────────────────────────────────────────────────────────┘
```

### Step 3: Layout Selection

```
┌─────────────────────────────────────────────────────────────────┐
│  Select Layout Template                                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Standard Layout │  │  Two Column     │  │  Full Width     │ │
│  │                 │  │                 │  │                 │ │
│  │ ┌─┬─┬─┬─┐      │  │ ┌─────┬───┐    │  │ ┌─────────────┐ │ │
│  │ ├─┴─┴─┴─┤      │  │ │     │   │    │  │ │             │ │ │
│  │ │       │      │  │ │     │   │    │  │ │             │ │ │
│  │ └───────┘      │  │ │     │   │    │  │ │             │ │ │
│  │                 │  │ └─────┴───┘    │  │ └─────────────┘ │ │
│  │ [Select]        │  │ [Select]       │  │ [Select]        │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Dashboard       │  │  Marketing      │  │  Custom         │ │
│  │                 │  │                 │  │                 │ │
│  │ ┌─┬─┬─┬─┐      │  │ ┌───────────┐  │  │                 │ │
│  │ ├─┼─┼─┼─┤      │  │ ├───┬───┬───┤  │  │ [Create New]    │ │
│  │ ├─┴─┴─┴─┤      │  │ │   │   │   │  │  │                 │ │
│  │ └───────┘      │  │ └───┴───┴───┘  │  │                 │ │
│  │                 │  │                 │  │                 │ │
│  │ [Select]        │  │ [Select]       │  │                 │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Step 4: Grid Design

Once a layout is selected, users can design the grid:

```
┌─────────────────────────────────────────────────────────────────┐
│  Grid Designer - Dashboard                    [Desktop│▼] [Save] │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  [Component Palette]  │            Grid Canvas                   │
│                       │                                          │
│  Drag components  →   │  Drop zones appear when dragging        │
│                       │                                          │
│                       │  ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐     │
│                       │  │                                 │     │
│                       │  │  Drop component here           │     │
│                       │  │                                 │     │
│                       │  └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘     │
│                       │                                          │
└─────────────────────────────────────────────────────────────────┘
```

## Page Management Features

### Page List View

```
┌─────────────────────────────────────────────────────────────────┐
│  Pages                                    [+ New Page] [Import]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Search: [_____________________] Filter: [All Pages ▼]          │
│                                                                   │
│  ┌──────────────┬──────────┬────────────┬──────────┬─────────┐ │
│  │ Name         │ Route    │ Layout     │ Status   │ Actions │ │
│  ├──────────────┼──────────┼────────────┼──────────┼─────────┤ │
│  │ 📊 Dashboard │ /        │ Standard   │ ● Live   │ ⋯       │ │
│  │ 👥 Users     │ /users   │ Two Column │ ● Live   │ ⋯       │ │
│  │ 📈 Analytics │ /stats   │ Full Width │ ○ Draft  │ ⋯       │ │
│  │ ⚙️ Settings  │ /settings│ Standard   │ ● Live   │ ⋯       │ │
│  └──────────────┴──────────┴────────────┴──────────┴─────────┘ │
│                                                                   │
│  Showing 4 of 4 pages                                            │
└─────────────────────────────────────────────────────────────────┘
```

### Page Actions

```typescript
interface PageActions {
  edit: (pageId: string) => void;
  duplicate: (pageId: string) => void;
  delete: (pageId: string) => void;
  publish: (pageId: string) => void;
  unpublish: (pageId: string) => void;
  preview: (pageId: string, device?: DeviceType) => void;
  export: (pageId: string) => void;
  viewHistory: (pageId: string) => void;
}
```

## Advanced Features

### 1. Page Templates

```typescript
interface PageTemplate {
  id: string;
  name: string;
  description: string;
  thumbnail: string;
  config: {
    layout: BentoLayout;
    defaultComponents: GridComponent[];
    suggestedBindings?: PageBindings;
  };
  category: 'dashboard' | 'admin' | 'marketing' | 'custom';
}

// Example templates
const templates: PageTemplate[] = [
  {
    id: 'analytics-dashboard',
    name: 'Analytics Dashboard',
    description: 'KPI metrics and charts for data analysis',
    thumbnail: '/templates/analytics.png',
    config: {
      layout: analyticsLayout,
      defaultComponents: [
        { type: 'MetricCard', position: { x: 0, y: 0, w: 3, h: 2 } },
        { type: 'MetricCard', position: { x: 3, y: 0, w: 3, h: 2 } },
        { type: 'ChartWidget', position: { x: 0, y: 2, w: 6, h: 4 } }
      ]
    },
    category: 'dashboard'
  }
];
```

### 2. Page Versioning

```
┌─────────────────────────────────────────────────────────────────┐
│  Page History - Dashboard                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Current Version: v12 (Live)                                      │
│                                                                   │
│  ┌────────┬──────────────┬─────────────┬────────────┬─────────┐ │
│  │ Version│ Date         │ Author      │ Changes    │ Actions │ │
│  ├────────┼──────────────┼─────────────┼────────────┼─────────┤ │
│  │ v12    │ 2 hours ago  │ John Doe    │ Added chart│ Current │ │
│  │ v11    │ Yesterday    │ Jane Smith  │ Layout fix │ Restore │ │
│  │ v10    │ 3 days ago   │ John Doe    │ New metrics│ Restore │ │
│  └────────┴──────────────┴─────────────┴────────────┴─────────┘ │
│                                                                   │
│  [Compare Versions] [Export History]                              │
└─────────────────────────────────────────────────────────────────┘
```

### 3. Collaboration Features

```typescript
interface CollaborationFeatures {
  // Real-time editing
  presence: {
    showActiveUsers: boolean;
    highlightUserCursors: boolean;
    userColors: Record<string, string>;
  };
  
  // Comments and annotations
  comments: {
    enabled: boolean;
    threads: CommentThread[];
  };
  
  // Change tracking
  changes: {
    trackChanges: boolean;
    requireApproval: boolean;
    approvers: string[];
  };
}
```

### 4. Preview Modes

```
┌─────────────────────────────────────────────────────────────────┐
│  Preview Mode                                [Exit Preview]       │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Device: [Desktop ▼] [Tablet] [Mobile]                           │
│  Data: [Live ▼] [Sample] [Empty]                                 │
│  Theme: [Light ▼] [Dark]                                          │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    Preview Frame                             │ │
│  │  ┌───────────────────────────────────────────────────────┐  │ │
│  │  │                                                       │  │ │
│  │  │              (Page renders here)                      │  │ │
│  │  │                                                       │  │ │
│  │  └───────────────────────────────────────────────────────┘  │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  [Share Preview] [Full Screen] [Debug Info]                      │
└─────────────────────────────────────────────────────────────────┘
```

## Page Builder API

### Core Methods

```typescript
interface PageBuilderAPI {
  // Page operations
  createPage(config: PageConfig): Promise<BentoPage>;
  updatePage(pageId: string, updates: Partial<PageConfig>): Promise<BentoPage>;
  deletePage(pageId: string): Promise<void>;
  
  // Publishing
  publishPage(pageId: string): Promise<void>;
  unpublishPage(pageId: string): Promise<void>;
  schedulePage(pageId: string, publishDate: Date): Promise<void>;
  
  // Versioning
  getPageHistory(pageId: string): Promise<PageVersion[]>;
  restoreVersion(pageId: string, versionId: string): Promise<void>;
  
  // Import/Export
  exportPage(pageId: string): Promise<PageExport>;
  importPage(data: PageExport): Promise<BentoPage>;
  
  // Validation
  validatePage(config: PageConfig): ValidationResult;
  checkRouteAvailability(route: string): Promise<boolean>;
}
```

### Event Handling

```typescript
// Page Builder events
enum PageBuilderEvent {
  PageCreated = 'page:created',
  PageUpdated = 'page:updated',
  PageDeleted = 'page:deleted',
  PagePublished = 'page:published',
  LayoutChanged = 'layout:changed',
  ComponentAdded = 'component:added',
  ComponentRemoved = 'component:removed'
}

// Event listeners
pageBuilder.on(PageBuilderEvent.PagePublished, (event) => {
  console.log(`Page ${event.pageId} published`);
  invalidateCache(event.route);
  updateNavigation();
});
```

## Integration with Navigation

### Auto-Navigation Updates

```typescript
// When a page is published
const updateNavigation = async (page: BentoPage) => {
  if (page.bindings.visibility.showInNavigation) {
    const navItem: NavigationItem = {
      id: page.id,
      label: page.displayName,
      href: page.route,
      icon: page.bindings.visibility.icon,
      requiredPermission: page.bindings.security.requiredPermissions?.[0],
      order: page.bindings.visibility.navigationOrder
    };
    
    await navigationService.addOrUpdate(navItem);
  }
};
```

### Dynamic Route Registration

```typescript
// Register routes dynamically
const registerPageRoutes = (pages: BentoPage[]) => {
  pages.forEach(page => {
    router.addRoute({
      path: page.route,
      component: PageRenderer,
      props: { pageId: page.id },
      guards: page.bindings.security
    });
  });
};
```

## Best Practices

### 1. Page Naming Conventions
- Use descriptive display names
- Keep routes short and semantic
- Follow URL best practices (lowercase, hyphens)

### 2. Security First
- Always define access controls
- Use least privilege principle
- Test with different user roles

### 3. Performance Considerations
- Limit components per page
- Use lazy loading for heavy components
- Optimize for mobile first

### 4. User Experience
- Provide clear feedback during operations
- Show loading states
- Handle errors gracefully

## Next Steps

1. Explore [Data Models](./06-data-models.md) for detailed type definitions
2. Review [Grid System](./03-grid-system.md) for layout details
3. Check [Security Model](./12-security-model.md) for access control
4. See [Implementation Plan](./07-implementation-plan.md) for development phases