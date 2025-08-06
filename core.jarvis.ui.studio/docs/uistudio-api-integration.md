# UIStudio API Integration Guide

This guide provides comprehensive documentation for integrating with the UIStudio APIs using our TypeScript client and React Query hooks.

## Table of Contents

1. [Overview](#overview)
2. [Installation & Setup](#installation--setup)
3. [API Client](#api-client)
4. [React Query Hooks](#react-query-hooks)
5. [Error Handling](#error-handling)
6. [Type Definitions](#type-definitions)
7. [Examples](#examples)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

## Overview

The UIStudio API integration provides a complete TypeScript client for all UIStudio operations with the following features:

- **Type-safe operations** with full TypeScript support
- **React Query integration** for caching, optimistic updates, and real-time capabilities
- **Comprehensive error handling** with user-friendly messages and retry logic
- **Authentication integration** with automatic token management
- **Optimistic updates** for better user experience
- **Bulk operations** for performance optimization

### Architecture

```
Frontend Components
       ↓
React Query Hooks (useUIStudio.ts)
       ↓
UIStudio API Client (uistudioApiClient.ts)
       ↓
UIStudio REST APIs (/api/uistudio/*)
       ↓
Jarvis ECS Backend
```

## Installation & Setup

### 1. Dependencies

The following packages are required and already installed:

```json
{
  "@tanstack/react-query": "^5.84.1",
  "@tanstack/react-query-devtools": "^5.84.1"
}
```

### 2. Provider Setup

Wrap your application with the query provider:

```tsx
import React from 'react';
import { UIStudioQueryProvider } from './providers/QueryProvider';
import { AuthProvider } from './contexts/AuthContext';

function App() {
  return (
    <AuthProvider>
      <UIStudioQueryProvider showDevtools={true}>
        <YourAppComponents />
      </UIStudioQueryProvider>
    </AuthProvider>
  );
}
```

### 3. Configuration

The API client uses sensible defaults but can be customized:

```tsx
import { createUIStudioApiClient } from './services/api/uistudioApiClient';

const customClient = createUIStudioApiClient({
  baseUrl: '/api/uistudio',
  timeout: 10000,
  retryConfig: {
    maxAttempts: 3,
    baseDelay: 1000,
    exponentialBackoff: true
  },
  enableLogging: process.env.NODE_ENV === 'development'
});
```

## API Client

### Basic Usage

The API client provides methods for all UIStudio operations:

```tsx
import { uistudioApiClient } from './services/api/uistudioApiClient';

// Create a page
const pageResponse = await uistudioApiClient.createPage({
  pageName: 'Dashboard',
  pageSlug: 'dashboard',
  pageType: 'dynamic',
  createdByEntityId: userId
});

// Get page bindings
const bindings = await uistudioApiClient.getPageBindings(pageEntityId);

// Update layout grid
await uistudioApiClient.updateLayoutGrid(layoutEntityId, {
  columns: 12,
  gap: '16px',
  padding: '20px'
});
```

### Available Methods

#### Page Management
- `createPage(request)` - Create a new page
- `updatePage(id, request)` - Update an existing page
- `getPage(id)` - Get a specific page
- `getPagesByOwner(ownerId)` - Get pages by owner
- `getPublishedPages(query?)` - Get published pages with filtering
- `publishPage(pageId, publisherId)` - Publish a page
- `deletePage(pageId, deleterId)` - Delete a page
- `duplicatePage(pageId, request)` - Duplicate a page

#### Layout Management
- `createLayout(request)` - Create a new layout
- `updateLayout(id, request)` - Update a layout
- `updateLayoutGrid(id, config)` - Update grid configuration
- `updateLayoutResponsive(id, config)` - Update responsive configuration
- `getLayout(id)` - Get a specific layout

#### Component Bindings
- `createBinding(request)` - Create a component binding
- `createBindingsBulk(requests)` - Create multiple bindings
- `createPageBindings(pageId, requests)` - Create page-specific bindings
- `updateBinding(id, request)` - Update a binding
- `deleteBinding(id)` - Delete a binding
- `getPageBindings(pageId)` - Get bindings for a page

#### Template Management
- `createTemplate(request)` - Create a new template
- `updateTemplate(id, request)` - Update a template
- `getTemplate(id)` - Get a specific template
- `getTemplatesByOwner(ownerId)` - Get templates by owner
- `applyTemplate(templateId, request)` - Apply a template

#### Version Control
- `createVersionSnapshot(request)` - Create a version snapshot
- `rollbackToVersion(versionId, userId)` - Rollback to a version
- `publishVersion(versionId, userId)` - Publish a version
- `getVersionHistory(resourceId, query?)` - Get version history

## React Query Hooks

### Page Management Hooks

```tsx
import { 
  useUIStudioPagesByOwner,
  useCreateUIStudioPage,
  useUpdateUIStudioPage,
  usePublishUIStudioPage 
} from './hooks/useUIStudio';

function PageManager({ userId }: { userId: string }) {
  // Get user's pages
  const { data: pages, isLoading, error } = useUIStudioPagesByOwner(userId);
  
  // Create page mutation
  const createPageMutation = useCreateUIStudioPage({
    onSuccess: (data) => {
      console.log('Page created:', data[0]);
    }
  });
  
  // Update page mutation with optimistic updates
  const updatePageMutation = useUpdateUIStudioPage(pageId, {
    onSuccess: () => {
      console.log('Page updated');
    }
  });

  const handleCreatePage = async () => {
    await createPageMutation.mutateAsync({
      pageName: 'New Page',
      pageSlug: 'new-page',
      pageType: 'dynamic',
      createdByEntityId: userId
    });
  };

  return (
    <div>
      {/* Your UI components */}
    </div>
  );
}
```

### Layout and Binding Hooks

```tsx
import { 
  useUIStudioPageBindings,
  useCreateUIStudioBinding,
  useUpdateUIStudioLayoutGrid 
} from './hooks/useUIStudio';

function BentoGridEditor({ pageId, layoutId }: { pageId: string; layoutId: string }) {
  // Get page bindings
  const { data: bindings } = useUIStudioPageBindings(pageId);
  
  // Create binding mutation
  const createBindingMutation = useCreateUIStudioBinding();
  
  // Update grid with optimistic updates
  const updateGridMutation = useUpdateUIStudioLayoutGrid(layoutId);

  const handleAddComponent = async (componentType: string) => {
    await createBindingMutation.mutateAsync({
      pageSlug: 'example-page',
      componentType,
      componentInstanceId: `${componentType}-${Date.now()}`,
      boundComponentType: `${componentType}Component`,
      createdByEntityId: userId
    });
  };

  const handleGridUpdate = async (config: GridConfig) => {
    await updateGridMutation.mutateAsync(config);
  };

  return (
    <div>
      {/* Grid editor UI */}
    </div>
  );
}
```

### Template and Version Control Hooks

```tsx
import { 
  useUIStudioTemplatesByOwner,
  useApplyUIStudioTemplate,
  useUIStudioVersionHistory,
  useCreateUIStudioVersionSnapshot 
} from './hooks/useUIStudio';

function TemplateManager({ userId }: { userId: string }) {
  const { data: templates } = useUIStudioTemplatesByOwner(userId);
  const applyTemplateMutation = useApplyUIStudioTemplate(templateId);
  const { data: versions } = useUIStudioVersionHistory(resourceId);
  const createSnapshotMutation = useCreateUIStudioVersionSnapshot();

  return (
    <div>
      {/* Template and version management UI */}
    </div>
  );
}
```

## Error Handling

### Error Types

The system provides specific error types for different scenarios:

```tsx
import { 
  UIStudioError,
  UIStudioNetworkError,
  UIStudioAuthError,
  UIStudioPermissionError,
  UIStudioValidationError,
  getUserFriendlyMessage 
} from './utils/uistudioErrors';

// Error handling in components
function MyComponent() {
  const { data, error } = useUIStudioPage(pageId);

  if (error) {
    const userMessage = getUserFriendlyMessage(error);
    
    return (
      <div className="error-state">
        <p>{userMessage}</p>
        {error instanceof UIStudioNetworkError && (
          <button onClick={() => refetch()}>Retry</button>
        )}
      </div>
    );
  }

  return <div>{/* Success UI */}</div>;
}
```

### Automatic Error Recovery

The hooks include automatic error handling:

```tsx
const { handleError } = useUIStudioErrorHandler();

const mutation = useCreateUIStudioPage({
  onError: (error) => {
    const { userMessage, recoveryConfig } = handleError(error, 'create_page');
    
    // The error is automatically logged and recovery strategy applied
    if (recoveryConfig.showToast) {
      showToast(userMessage);
    }
  }
});
```

## Type Definitions

### Core Types

```tsx
// Page types
interface UIStudioPage {
  id: string;
  ownerEntityId: string;
  pageName: string;
  pageSlug: string;
  pageType: 'static' | 'dynamic';
  isPublished: boolean;
  createdAt: string;
  lastUpdated: string;
  // ... additional fields
}

// Binding types
interface UIStudioComponentBinding {
  id: string;
  ownerEntityId: string;
  pageSlug: string;
  componentType: string;
  boundComponentType: string;
  fieldMappings?: Record<string, string>;
  dataSourceConfig?: DataSourceConfig;
  positionConfig?: PositionConfig;
  // ... additional fields
}

// Request types
interface CreatePageRequest {
  pageName: string;
  pageSlug: string;
  pageType: 'static' | 'dynamic';
  createdByEntityId: string;
  description?: string;
  metadata?: Record<string, unknown>;
  tags?: string;
}
```

### Query Keys

Use the provided query keys for cache invalidation:

```tsx
import { uistudioKeys } from './hooks/useUIStudio';

// Invalidate specific queries
queryClient.invalidateQueries({ queryKey: uistudioKeys.page(pageId) });
queryClient.invalidateQueries({ queryKey: uistudioKeys.pageBindings(pageId) });
queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() });
```

## Examples

### Complete Page Builder

```tsx
function PageBuilder({ userId }: { userId: string }) {
  const [currentPage, setCurrentPage] = useState<UIStudioPage | null>(null);
  
  const { data: pages } = useUIStudioPagesByOwner(userId);
  const { data: bindings } = useUIStudioPageBindings(currentPage?.ownerEntityId!, {
    enabled: !!currentPage
  });
  
  const createPageMutation = useCreateUIStudioPage({
    onSuccess: (data) => setCurrentPage(data[0])
  });
  
  const createBindingMutation = useCreateUIStudioBinding();

  return (
    <div className="page-builder">
      <PageList 
        pages={pages}
        onSelectPage={setCurrentPage}
        onCreatePage={(data) => createPageMutation.mutate(data)}
      />
      
      {currentPage && (
        <BentoGrid
          page={currentPage}
          bindings={bindings}
          onAddComponent={(binding) => createBindingMutation.mutate(binding)}
        />
      )}
    </div>
  );
}
```

### Real-time Updates

```tsx
function RealtimePageEditor({ pageId }: { pageId: string }) {
  // Refetch every 30 seconds for real-time updates
  const { data: page } = useUIStudioPage(pageId, {
    refetchInterval: 30000,
    refetchIntervalInBackground: true
  });
  
  const { data: bindings } = useUIStudioPageBindings(pageId, {
    refetchInterval: 30000
  });

  return (
    <div>
      {/* Real-time page editor */}
    </div>
  );
}
```

## Best Practices

### 1. Query Configuration

```tsx
// Use appropriate stale times for different data types
const { data: staticConfig } = useUIStudioTemplate(templateId, {
  staleTime: 30 * 60 * 1000, // 30 minutes for templates
  refetchOnWindowFocus: false
});

const { data: userPages } = useUIStudioPagesByOwner(userId, {
  staleTime: 5 * 60 * 1000, // 5 minutes for user data
  refetchOnWindowFocus: true
});
```

### 2. Optimistic Updates

```tsx
const updatePageMutation = useUpdateUIStudioPage(pageId, {
  onMutate: async (variables) => {
    // Cancel outgoing refetches
    await queryClient.cancelQueries({ queryKey: uistudioKeys.page(pageId) });
    
    // Snapshot previous value
    const previousPage = queryClient.getQueryData(uistudioKeys.page(pageId));
    
    // Optimistically update
    queryClient.setQueryData(uistudioKeys.page(pageId), (old) => 
      old ? [{ ...old[0], ...variables }] : old
    );
    
    return { previousPage };
  },
  onError: (err, variables, context) => {
    // Rollback on error
    if (context?.previousPage) {
      queryClient.setQueryData(uistudioKeys.page(pageId), context.previousPage);
    }
  }
});
```

### 3. Error Boundaries

```tsx
function UIStudioErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary
      fallback={({ error, retry }) => (
        <div className="error-boundary">
          <h2>Something went wrong</h2>
          <p>{getUserFriendlyMessage(error)}</p>
          <button onClick={retry}>Try Again</button>
        </div>
      )}
    >
      {children}
    </ErrorBoundary>
  );
}
```

### 4. Performance Optimization

```tsx
// Use React.memo for component optimization
const BentoComponent = React.memo(({ binding }: { binding: UIStudioComponentBinding }) => {
  return <div>{/* Component UI */}</div>;
});

// Batch multiple operations
const bulkCreateMutation = useCreateUIStudioBindingsBulk({
  onSuccess: () => {
    // Single cache invalidation for all created bindings
    queryClient.invalidateQueries({ queryKey: uistudioKeys.bindings() });
  }
});
```

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   ```tsx
   // Ensure tokens are properly stored
   import { getStoredTokens } from './utils/tokenUtils';
   
   const { accessToken } = getStoredTokens();
   if (!accessToken) {
     // Redirect to login
   }
   ```

2. **Network Timeouts**
   ```tsx
   // Increase timeout for slow operations
   const customClient = createUIStudioApiClient({
     timeout: 30000 // 30 seconds
   });
   ```

3. **Cache Issues**
   ```tsx
   // Clear specific caches
   const { invalidateAll } = useInvalidateUIStudioQueries();
   
   const handleRefresh = () => {
     invalidateAll();
   };
   ```

4. **Type Errors**
   ```tsx
   // Use type guards for runtime validation
   import { isUIStudioPage } from './types/uistudio';
   
   if (isUIStudioPage(data)) {
     // TypeScript knows this is a UIStudioPage
   }
   ```

### Debug Tools

1. **React Query DevTools**
   ```tsx
   // Already enabled in development
   <UIStudioQueryProvider showDevtools={true}>
     <App />
   </UIStudioQueryProvider>
   ```

2. **Logging**
   ```tsx
   // Enable detailed logging
   const client = createUIStudioApiClient({
     enableLogging: true
   });
   ```

3. **Health Check**
   ```tsx
   const { data: health } = useUIStudioHealthCheck();
   console.log('API Status:', health?.status);
   ```

### Performance Monitoring

```tsx
// Monitor query performance
const { data, dataUpdatedAt, isFetching } = useUIStudioPage(pageId);

console.log('Last updated:', new Date(dataUpdatedAt));
console.log('Currently fetching:', isFetching);
```

## API Endpoints Reference

All endpoints are relative to `/api/uistudio`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/pages` | Create page |
| PUT | `/pages/{id}` | Update page |
| GET | `/pages/{id}` | Get page |
| GET | `/pages/by-owner/{id}` | Get pages by owner |
| GET | `/pages/published` | Get published pages |
| POST | `/pages/{id}/publish/{userId}` | Publish page |
| DELETE | `/pages/{id}/{userId}` | Delete page |
| POST | `/layouts` | Create layout |
| PUT | `/layouts/{id}/grid` | Update grid config |
| POST | `/bindings` | Create binding |
| POST | `/bindings/bulk` | Bulk create bindings |
| GET | `/pages/{id}/bindings` | Get page bindings |
| POST | `/templates` | Create template |
| POST | `/templates/{id}/apply` | Apply template |
| POST | `/versions/snapshots` | Create version |
| GET | `/resources/{id}/versions` | Get version history |

For complete API documentation, see [UIStudio API Reference](../docs/bento/10-uistudio-api-reference.md).