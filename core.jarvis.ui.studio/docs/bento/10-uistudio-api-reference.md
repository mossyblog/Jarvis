# UIStudio API Reference Guide

## Overview

The UIStudio API follows the Jarvis Entity Component System (ECS) architecture pattern: **API → System → Handler → Component**. This document provides comprehensive guidance for both backend C# implementation and frontend TypeScript integration.

### Architecture Pattern

```
Azure Function (API) → UIStudioSystem → ComponentHandler → UIStudioComponent
```

- **Azure Functions**: HTTP endpoints that handle requests and responses
- **Systems**: Orchestrate complex operations across multiple handlers
- **Handlers**: Manage CRUD operations for specific component types
- **Components**: Immutable records implementing `IComponent`

## Core Principles

### 1. Entity Component System (ECS)
- **Entities**: Just GUIDs - the identity of things
- **Components**: Pure data structures implementing `IComponent`
- **Systems**: Business logic that orchestrates multiple handlers
- **Handlers**: Type-specific CRUD operations

### 2. Immutable Components
All UIStudio components are immutable `record` types:

```csharp
public record UIStudioPage : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public string PageName { get; init; } = string.Empty;
    // ... other properties
}
```

### 3. Handler Pattern
Each component type has a dedicated handler:

```csharp
public class UIStudioPageHandler : ComponentHandler<UIStudioPage>
{
    public async Task<UIStudioPage> CreatePage(UIStudioPage page) { /* ... */ }
    public async Task<UIStudioPage> UpdatePage(UIStudioPage page) { /* ... */ }
    // ... other operations
}
```

## API Endpoints Reference

### Page Management

#### Create Page
**POST** `/api/uistudio/pages`

Creates a new UIStudio page with layout and optional template. Follows pure Jarvis pattern - accepts IComponent object directly.

**Request Body (UIStudioPage IComponent):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "pageName": "My Dashboard",
  "pageSlug": "dashboard",
  "pageType": "dynamic",
  "description": "Main application dashboard",
  "layoutConfig": {
    "type": "bento",
    "columns": 12,
    "breakpoints": {
      "sm": 640,
      "md": 768,
      "lg": 1024,
      "xl": 1280
    }
  },
  "isPublished": false,
  "createdByEntityId": "123e4567-e89b-12d3-a456-426614174000",
  "templateEntityId": "987fcdeb-51a2-43d1-b456-426614174000",
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

**Response (201 Created) - List<IComponent>:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "pageName": "My Dashboard",
    "pageSlug": "dashboard",
    "pageType": "dynamic",
    "isPublished": false,
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
]
```

#### Update Page
**PUT** `/api/uistudio/pages/{pageEntityId}`

Accepts complete UIStudioPage IComponent object with updated values.

**Request Body (Updated UIStudioPage IComponent):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "pageName": "Updated Dashboard",
  "pageSlug": "dashboard",
  "pageType": "dynamic",
  "description": "Updated description",
  "layoutConfig": {
    "type": "bento",
    "columns": 16,
    "breakpoints": {
      "sm": 640,
      "md": 768,
      "lg": 1024,
      "xl": 1280
    }
  },
  "isPublished": false,
  "lastUpdated": "2024-01-15T11:45:00Z"
}
```

#### Publish Page
**PUT** `/api/uistudio/pages/{pageEntityId}`

Publishes page by updating the UIStudioPage IComponent with isPublished = true.

**Request Body (UIStudioPage IComponent with isPublished = true):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "pageName": "My Dashboard",
  "pageSlug": "dashboard",
  "pageType": "dynamic",
  "isPublished": true,
  "lastUpdated": "2024-01-15T12:00:00Z"
}
```

#### Delete Page
**DELETE** `/api/uistudio/pages/{pageEntityId}`

Deletes page by entity ID. No request body required.

**Response (200 OK) - List<IComponent> (empty):**
```json
[]
```

#### Duplicate Page
**POST** `/api/uistudio/pages`

Create new page by submitting a complete UIStudioPage IComponent object with new ID and modified values.

**Request Body (New UIStudioPage IComponent):**
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "660e8400-e29b-41d4-a716-446655440001",
  "pageName": "Copy of Dashboard",
  "pageSlug": "dashboard-copy",
  "pageType": "dynamic",
  "description": "Copied from original dashboard",
  "layoutConfig": {
    "type": "bento",
    "columns": 12
  },
  "isPublished": false,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

### Query Operations

#### Get Pages with Filtering
**GET** `/api/uistudio/pages`

**Query Parameters (simple values only):**
- `pageType` (string): Filter by type ("dynamic", "fixed", "hybrid")
- `isPublished` (boolean): Filter by published status
- `createdByEntityId` (Guid): Filter by creator entity ID

**Example:**
```
GET /api/uistudio/pages?pageType=dynamic&isPublished=true&createdByEntityId=123e4567-e89b-12d3-a456-426614174000
```

**Response - List<IComponent>:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "pageName": "Dashboard",
    "pageSlug": "dashboard",
    "pageType": "dynamic",
    "isPublished": true,
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
]
```

#### Get Page Details
**GET** `/api/uistudio/pages/{pageEntityId}`

Returns single UIStudioPage IComponent by entity ID.

**Response - List<IComponent> (single page):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "pageName": "Dashboard",
    "pageSlug": "dashboard",
    "pageType": "dynamic",
    "description": "Main application dashboard",
    "layoutConfig": {
      "type": "bento",
      "columns": 12,
      "gap": 16
    },
    "isPublished": true,
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
]
```

#### Get Published Pages (Public)
**GET** `/api/uistudio/pages/published`

Returns all published pages. Uses simple Guid-only filtering.

**Response - List<IComponent>:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "pageName": "Public Dashboard",
    "pageSlug": "public-dashboard",
    "pageType": "dynamic",
    "isPublished": true,
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
]
```

### Layout Operations

#### Update Layout
**PUT** `/api/uistudio/layouts/{layoutEntityId}`

Updates layout by submitting complete UIStudioLayout IComponent object.

**Request Body (UIStudioLayout IComponent):**
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "660e8400-e29b-41d4-a716-446655440001",
  "layoutName": "Updated Grid Layout",
  "layoutType": "bento",
  "gridConfig": {
    "columns": 16,
    "gap": 20,
    "breakpoints": {
      "sm": 640,
      "md": 768,
      "lg": 1024,
      "xl": 1280,
      "2xl": 1536
    }
  },
  "lastUpdated": "2024-01-15T11:30:00Z"
}
```

#### Create Layout
**POST** `/api/uistudio/layouts`

Creates layout by submitting complete UIStudioLayout IComponent object.

**Request Body (UIStudioLayout IComponent):**
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "660e8400-e29b-41d4-a716-446655440001",
  "layoutName": "Modern Grid Layout",
  "layoutType": "bento",
  "gridConfig": {
    "columns": 12,
    "gap": 16,
    "breakpoints": {
      "sm": 640,
      "md": 768,
      "lg": 1024
    }
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

#### Update Layout Responsive Settings
**PUT** `/api/uistudio/layouts/{layoutEntityId}`

Update layout by submitting complete UIStudioLayout IComponent with updated responsive settings.

**Request Body (Complete UIStudioLayout IComponent):**
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "660e8400-e29b-41d4-a716-446655440001",
  "layoutName": "Modern Grid Layout",
  "layoutType": "bento",
  "gridConfig": {
    "columns": 12,
    "gap": 16,
    "breakpoints": {
      "sm": 640,
      "md": 768,
      "lg": 1024,
      "xl": 1280,
      "2xl": 1536
    }
  },
  "lastUpdated": "2024-01-15T11:45:00Z"
}
```

### Component Binding Operations

Component bindings connect ECS components to UI elements in the grid.

#### Create Component Binding
**POST** `/api/uistudio/bindings`

Creates component binding by submitting UIStudioComponentBinding IComponent object.

**Request Body (UIStudioComponentBinding IComponent):**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "770e8400-e29b-41d4-a716-446655440001",
  "pageEntityId": "550e8400-e29b-41d4-a716-446655440000",
  "componentType": "MetricCard",
  "gridArea": "1 / 1 / 2 / 4",
  "fieldMappings": {
    "title": "Revenue",
    "value": "$12,345",
    "trend": "+5.2%",
    "icon": "DollarSign"
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

#### Bulk Manage Bindings
**POST** `/api/uistudio/bindings/bulk`

Submit multiple UIStudioComponentBinding IComponent objects in a single array.

**Request Body (Array of UIStudioComponentBinding IComponents):**
```json
[
  {
    "id": "770e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "770e8400-e29b-41d4-a716-446655440001",
    "pageEntityId": "550e8400-e29b-41d4-a716-446655440000",
    "componentType": "KPICard",
    "gridArea": "2 / 1 / 3 / 3",
    "fieldMappings": {
      "title": "Conversion Rate",
      "value": "3.2%"
    },
    "lastUpdated": "2024-01-15T10:30:00Z"
  },
  {
    "id": "880e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "880e8400-e29b-41d4-a716-446655440001",
    "pageEntityId": "550e8400-e29b-41d4-a716-446655440000",
    "componentType": "MetricCard",
    "gridArea": "1 / 1 / 2 / 3",
    "fieldMappings": {
      "title": "Updated Title",
      "value": "$15,000"
    },
    "lastUpdated": "2024-01-15T11:00:00Z"
  }
]
```

#### Get Page Bindings
**GET** `/api/uistudio/bindings`

**Query Parameters:**
- `pageEntityId` (Guid): Filter by page entity ID
- `componentType` (string): Filter by component type

#### Update Binding
**PUT** `/api/uistudio/bindings/{bindingEntityId}`

Updates binding by submitting complete UIStudioComponentBinding IComponent object.

**Request Body (Complete UIStudioComponentBinding IComponent):**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "770e8400-e29b-41d4-a716-446655440001",
  "pageEntityId": "550e8400-e29b-41d4-a716-446655440000",
  "componentType": "MetricCard",
  "gridArea": "1 / 5 / 2 / 8",
  "fieldMappings": {
    "title": "Updated Metric",
    "value": "2,468"
  },
  "lastUpdated": "2024-01-15T11:30:00Z"
}
```

### Template Operations

#### Create Template
**POST** `/api/uistudio/templates`

Creates template by submitting UIStudioTemplate IComponent object.

**Request Body (UIStudioTemplate IComponent):**
```json
{
  "id": "990e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "990e8400-e29b-41d4-a716-446655440001",
  "pageEntityId": "550e8400-e29b-41d4-a716-446655440000",
  "templateName": "Executive Dashboard",
  "category": "dashboard",
  "description": "Template for executive-level dashboards",
  "isPublic": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

#### Apply Template
**POST** `/api/uistudio/pages`

Apply template by creating new UIStudioPage IComponent based on template data.

**Request Body (New UIStudioPage IComponent from template):**
```json
{
  "id": "AA0e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "AA0e8400-e29b-41d4-a716-446655440001",
  "pageName": "Q1 Executive Dashboard",
  "pageSlug": "q1-exec-dashboard",
  "pageType": "dynamic",
  "description": "Q1 specific dashboard",
  "layoutConfig": {
    "type": "bento",
    "columns": 16
  },
  "templateEntityId": "990e8400-e29b-41d4-a716-446655440000",
  "isPublished": false,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

#### Get Templates
**GET** `/api/uistudio/templates`

**Query Parameters:**
- `category` (string): Filter by category
- `isPublic` (boolean): Filter by public status

**Response - List<IComponent>:**
```json
[
  {
    "id": "990e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "990e8400-e29b-41d4-a716-446655440001",
    "templateName": "Executive Dashboard",
    "category": "dashboard",
    "description": "Template for executive-level dashboards",
    "isPublic": true,
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
]
```

### Permission Management

#### Grant Permission
**POST** `/api/uistudio/permissions`

Grants permission by submitting UIStudioPermission IComponent object.

**Request Body (UIStudioPermission IComponent):**
```json
{
  "id": "BB0e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "BB0e8400-e29b-41d4-a716-446655440001",
  "resourceEntityId": "550e8400-e29b-41d4-a716-446655440000",
  "resourceType": "page",
  "granteeEntityId": "123e4567-e89b-12d3-a456-426614174000",
  "granteeType": "user",
  "permissionLevel": "edit",
  "grantedByEntityId": "987fcdeb-51a2-43d1-b456-426614174000",
  "expiresAt": "2024-12-31T23:59:59Z",
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

**Permission Levels:**
- `view`: Read-only access
- `edit`: Modify content
- `admin`: Full control including permissions
- `owner`: Full ownership (cannot be revoked)

#### Update Permission
**PUT** `/api/uistudio/permissions/{permissionEntityId}`

Updates permission by submitting complete UIStudioPermission IComponent object.

**Request Body (Complete UIStudioPermission IComponent):**
```json
{
  "id": "BB0e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "BB0e8400-e29b-41d4-a716-446655440001",
  "resourceEntityId": "550e8400-e29b-41d4-a716-446655440000",
  "resourceType": "page",
  "granteeEntityId": "123e4567-e89b-12d3-a456-426614174000",
  "granteeType": "user",
  "permissionLevel": "admin",
  "grantedByEntityId": "987fcdeb-51a2-43d1-b456-426614174000",
  "expiresAt": "2025-06-30T23:59:59Z",
  "lastUpdated": "2024-01-15T11:30:00Z"
}
```

#### Revoke Permission
**DELETE** `/api/uistudio/permissions/{permissionEntityId}`

Revokes permission by entity ID. No request body required.

**Response (200 OK) - List<IComponent> (empty):**
```json
[]
```

#### Get Resource Permissions
**GET** `/api/uistudio/permissions`

**Query Parameters:**
- `resourceEntityId` (Guid): Filter by resource entity ID
- `resourceType` (string): Filter by resource type

**Response - List<IComponent>:**
```json
[
  {
    "id": "BB0e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "BB0e8400-e29b-41d4-a716-446655440001",
    "resourceEntityId": "550e8400-e29b-41d4-a716-446655440000",
    "resourceType": "page",
    "granteeEntityId": "123e4567-e89b-12d3-a456-426614174000",
    "permissionLevel": "edit",
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
]
```

### Search Operations

#### Cross-Resource Search
**GET** `/api/uistudio/search`

Searches across UIStudio components using simple Guid-based filtering.

**Query Parameters:**
- `resourceType` (string): Component type to search ("page", "template", "layout")
- `searchTerm` (string): Simple text search in name fields

**Example:**
```
GET /api/uistudio/search?resourceType=page&searchTerm=dashboard
```

**Response - List<IComponent>:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "pageName": "Executive Dashboard",
    "pageSlug": "executive-dashboard",
    "pageType": "dynamic",
    "description": "Main dashboard for executives",
    "isPublished": true,
    "lastUpdated": "2024-01-20T14:15:00Z"
  }
]
```

## Authentication and Authorization

### JWT-Based Authentication
All API endpoints (except published pages) require JWT authentication:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Row Level Security (RLS)
The API uses PostgreSQL Row Level Security with JWT context:

```csharp
// JWT token automatically provides user context for RLS
var pageHandler = _dataContext.For<UIStudioPageHandler>(entityId);
var page = await pageHandler.Get(); // Automatically filtered by user access
```

### Permission Levels
- **Anonymous**: Access to published pages only
- **Function**: Standard API access with JWT
- **Admin**: Full administrative access

## Error Handling and Status Codes

### Standard HTTP Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200  | OK | Successful operation |
| 201  | Created | Resource created successfully |
| 400  | Bad Request | Invalid request parameters |
| 401  | Unauthorized | Authentication required |
| 403  | Forbidden | Insufficient permissions |
| 404  | Not Found | Resource not found |
| 409  | Conflict | Resource conflict (e.g., duplicate slug) |
| 500  | Internal Server Error | Server error |

### Error Response Format

```json
{
  "error": {
    "message": "Page with slug 'dashboard' already exists",
    "code": "RESOURCE_CONFLICT",
    "details": {
      "field": "pageSlug",
      "value": "dashboard"
    }
  }
}
```

### Common Error Codes

- `AUTH_INVALID_CREDENTIALS`: Invalid login credentials
- `AUTH_INVALID_TOKEN`: Invalid or expired JWT token
- `RESOURCE_NOT_FOUND`: Requested resource doesn't exist
- `RESOURCE_CONFLICT`: Resource conflict (duplicate slug, etc.)
- `VALIDATION_ERROR`: Request validation failed
- `PERMISSION_DENIED`: Insufficient permissions
- `NETWORK_ERROR`: Network connectivity issues

## Frontend Integration Patterns

### TypeScript Service Layer

#### API Service Pattern (Pure Jarvis)
```typescript
// Direct IComponent interfaces
interface UIStudioPage extends IComponent {
  pageName: string;
  pageSlug: string;
  pageType: string;
  description?: string;
  layoutConfig?: Record<string, unknown>;
  isPublished: boolean;
  templateEntityId?: string;
  createdAt: string;
}

interface UIStudioLayout extends IComponent {
  layoutName: string;
  layoutType: string;
  gridConfig: Record<string, unknown>;
}

class UIStudioApiService {
  private readonly baseUrl = '/api/uistudio';

  async createPage(page: UIStudioPage): Promise<UIStudioPage[]> {
    const response = await fetch(`${this.baseUrl}/pages`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify(page)
    });

    if (!response.ok) {
      throw new APIError(await response.json());
    }

    return response.json();
  }

  async getPages(filters?: { pageType?: string; isPublished?: boolean; createdByEntityId?: string }): Promise<UIStudioPage[]> {
    const params = new URLSearchParams();
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined) {
          params.append(key, String(value));
        }
      });
    }

    const response = await fetch(`${this.baseUrl}/pages?${params}`);
    return response.json();
  }

  async updatePage(pageEntityId: string, page: UIStudioPage): Promise<UIStudioPage[]> {
    const response = await fetch(`${this.baseUrl}/pages/${pageEntityId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify(page)
    });

    return response.json();
  }

  private getAuthToken(): string {
    return localStorage.getItem('access_token') || '';
  }
}
```

#### React Hook Pattern (Pure Jarvis)
```typescript
function useUIStudioPages() {
  const [pages, setPages] = useState<UIStudioPage[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPages = useCallback(async (filters?: { pageType?: string; isPublished?: boolean; createdByEntityId?: string }) => {
    setLoading(true);
    setError(null);
    
    try {
      const result = await apiService.getPages(filters);
      setPages(result); // Direct array from API
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load pages');
    } finally {
      setLoading(false);
    }
  }, []);

  const createPage = useCallback(async (page: UIStudioPage) => {
    setLoading(true);
    try {
      const newPages = await apiService.createPage(page);
      setPages(prev => [...prev, ...newPages]);
      return newPages;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create page');
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const updatePage = useCallback(async (pageEntityId: string, page: UIStudioPage) => {
    setLoading(true);
    try {
      const updatedPages = await apiService.updatePage(pageEntityId, page);
      setPages(prev => prev.map(p => p.ownerEntityId === pageEntityId ? updatedPages[0] : p));
      return updatedPages;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update page');
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    pages,
    loading,
    error,
    loadPages,
    createPage,
    updatePage
  };
}
```

#### Component Usage (Pure Jarvis)
```typescript
function PageManager() {
  const { pages, loading, error, loadPages, createPage, updatePage } = useUIStudioPages();
  const [filters, setFilters] = useState({
    pageType: 'dynamic',
    isPublished: true
  });

  useEffect(() => {
    loadPages(filters);
  }, [loadPages, filters]);

  const handleCreatePage = async (pageData: Partial<UIStudioPage>) => {
    try {
      const newPage: UIStudioPage = {
        id: crypto.randomUUID(),
        ownerEntityId: crypto.randomUUID(),
        pageName: pageData.pageName || '',
        pageSlug: pageData.pageSlug || '',
        pageType: pageData.pageType || 'dynamic',
        description: pageData.description,
        layoutConfig: pageData.layoutConfig,
        isPublished: false,
        createdAt: new Date().toISOString(),
        lastUpdated: new Date().toISOString()
      };
      
      await createPage(newPage);
      toast.success('Page created successfully');
    } catch (error) {
      toast.error('Failed to create page');
    }
  };

  const handleUpdatePage = async (pageEntityId: string, updates: Partial<UIStudioPage>) => {
    try {
      const existingPage = pages.find(p => p.ownerEntityId === pageEntityId);
      if (!existingPage) return;
      
      const updatedPage: UIStudioPage = {
        ...existingPage,
        ...updates,
        lastUpdated: new Date().toISOString()
      };
      
      await updatePage(pageEntityId, updatedPage);
      toast.success('Page updated successfully');
    } catch (error) {
      toast.error('Failed to update page');
    }
  };

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage message={error} />;

  return (
    <div>
      <PageFilters filters={filters} onChange={setFilters} />
      <PageList pages={pages} onUpdate={handleUpdatePage} />
      <CreatePageForm onSubmit={handleCreatePage} />
    </div>
  );
}
```

## GraphQL vs REST API Usage

### When to Use REST (Azure Functions)
- **CRUD Operations**: Creating, updating, deleting resources
- **Complex Business Logic**: Operations that span multiple components
- **File Uploads**: Binary data handling
- **Authentication**: Login, logout, token refresh
- **Permissions**: Granting, revoking access

**Example - Page Creation:**
```typescript
// REST for complex operations
const newPage = await fetch('/api/uistudio/pages', {
  method: 'POST',
  body: JSON.stringify({
    pageName: 'Dashboard',
    pageSlug: 'dashboard',
    layoutConfig: { columns: 12 },
    templateEntityId: 'template-123'
  })
});
```

### When to Use GraphQL
- **Data Queries**: Fetching specific fields
- **Relationships**: Loading related data in single request
- **Real-time Subscriptions**: Live data updates
- **Performance**: Minimizing over-fetching

**Example - Page Details with Relations:**
```typescript
// GraphQL for efficient queries
const query = `
  query GetPageDetails($pageId: UUID!) {
    uiStudioPageCollection(filter: { id: { eq: $pageId } }) {
      edges {
        node {
          id
          pageName
          pageSlug
          layout: layoutEntityId {
            layoutName
            gridConfig
          }
          bindings: componentBindingCollection {
            edges {
              node {
                componentType
                gridArea
                fieldMappings
              }
            }
          }
        }
      }
    }
  }
`;

const result = await graphqlService.executeQuery(query, { pageId });
```

### Hybrid Approach
Use REST for mutations and GraphQL for queries:

```typescript
class UIStudioService {
  // Use REST for mutations
  async createPage(data: CreatePageRequest) {
    return this.restClient.post('/api/uistudio/pages', data);
  }

  async updatePage(id: string, data: UpdatePageRequest) {
    return this.restClient.put(`/api/uistudio/pages/${id}`, data);
  }

  // Use GraphQL for queries
  async getPageWithDetails(id: string) {
    return this.graphqlClient.query(`
      query GetPage($id: UUID!) {
        page: uiStudioPageCollection(filter: { id: { eq: $id } }) {
          edges {
            node {
              id
              pageName
              pageSlug
              isPublished
              layout { gridConfig }
              bindings { componentType gridArea }
            }
          }
        }
      }
    `, { id });
  }
}
```

## Performance Considerations

### Caching Strategies

#### Server-Side Caching
```csharp
public class UIStudioPageHandler : ComponentHandler<UIStudioPage>
{
    private readonly IMemoryCache _cache;

    public async Task<List<UIStudioPage>> GetPublishedPages()
    {
        return await _cache.GetOrCreateAsync("published_pages", async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            
            var query = DataContext.Query()
                .WithAll<UIStudioPage>(p => p.IsPublished);
            
            var results = await query.ToEntityComponents();
            // ... fetch and return pages
        });
    }
}
```

#### Client-Side Caching
```typescript
class CachedUIStudioService {
  private cache = new Map<string, { data: unknown; expires: number }>();
  private readonly TTL = 5 * 60 * 1000; // 5 minutes

  async getPages(filters?: PageQueryFilters): Promise<PagedResult<UIStudioPage>> {
    const cacheKey = this.getCacheKey('pages', filters);
    const cached = this.cache.get(cacheKey);
    
    if (cached && cached.expires > Date.now()) {
      return cached.data as PagedResult<UIStudioPage>;
    }

    const data = await this.apiService.getPages(filters);
    
    this.cache.set(cacheKey, {
      data,
      expires: Date.now() + this.TTL
    });

    return data;
  }

  private getCacheKey(operation: string, params?: unknown): string {
    return `${operation}_${JSON.stringify(params)}`;
  }
}
```

### Pagination Best Practices

#### Cursor-Based Pagination
```typescript
interface PaginationCursor {
  lastId: string;
  lastUpdated: string;
}

async function loadMorePages(cursor?: PaginationCursor) {
  const params = new URLSearchParams({
    limit: '20',
    ...(cursor && {
      after_id: cursor.lastId,
      after_updated: cursor.lastUpdated
    })
  });

  const response = await fetch(`/api/uistudio/pages?${params}`);
  const result = await response.json();
  
  return {
    pages: result.items,
    nextCursor: result.items.length > 0 ? {
      lastId: result.items[result.items.length - 1].id,
      lastUpdated: result.items[result.items.length - 1].lastUpdated
    } : null
  };
}
```

#### Virtual Scrolling
```typescript
function VirtualPageList() {
  const [pages, setPages] = useState<UIStudioPage[]>([]);
  const [cursor, setCursor] = useState<PaginationCursor | null>(null);
  const [loading, setLoading] = useState(false);

  const loadMore = useCallback(async () => {
    if (loading) return;
    
    setLoading(true);
    try {
      const result = await loadMorePages(cursor);
      setPages(prev => [...prev, ...result.pages]);
      setCursor(result.nextCursor);
    } finally {
      setLoading(false);
    }
  }, [cursor, loading]);

  return (
    <InfiniteScroll
      hasMore={cursor !== null}
      loadMore={loadMore}
      threshold={100}
    >
      {pages.map(page => (
        <PageCard key={page.id} page={page} />
      ))}
    </InfiniteScroll>
  );
}
```

### Optimistic Updates

```typescript
function useOptimisticPageUpdates() {
  const [pages, setPages] = useState<UIStudioPage[]>([]);

  const updatePage = useCallback(async (id: string, updates: Partial<UIStudioPage>) => {
    // Optimistic update
    setPages(prev => prev.map(page => 
      page.id === id ? { ...page, ...updates } : page
    ));

    try {
      // Actual API call
      const updatedPage = await apiService.updatePage(id, updates);
      
      // Replace with server response
      setPages(prev => prev.map(page => 
        page.id === id ? updatedPage : page
      ));
    } catch (error) {
      // Revert optimistic update on error
      setPages(prev => prev.map(page => 
        page.id === id ? { ...page, ...updates } : page
      ));
      throw error;
    }
  }, []);

  return { pages, updatePage };
}
```

## Testing Examples and Patterns

### Backend Integration Tests

```csharp
[Fact]
public async Task Should_Create_Page_With_Layout()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var request = new CreatePageRequest
    {
        PageName = "Test Dashboard",
        PageSlug = "test-dashboard",
        PageType = "dynamic",
        LayoutConfig = new Dictionary<string, object>
        {
            ["type"] = "bento",
            ["columns"] = 12
        },
        CreatedByEntityId = Guid.NewGuid()
    };

    // Act
    var function = new UIStudioFunction(_uiStudioSystem, _logger);
    var httpRequest = CreateMockRequest(request);
    var response = await function.CreatePage(httpRequest);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.Created);
    
    var responseBody = await GetResponseBody<List<IComponent>>(response);
    responseBody.ShouldNotBeEmpty();
    responseBody.ShouldContain(c => c is UIStudioPage);
    responseBody.ShouldContain(c => c is UIStudioLayout);

    TrackEntity(entityId);
}

[Fact]
public async Task Should_Validate_Required_Fields()
{
    // Arrange
    var request = new CreatePageRequest
    {
        PageName = "", // Invalid
        PageSlug = "test-slug",
        CreatedByEntityId = Guid.Empty // Invalid
    };

    // Act
    var function = new UIStudioFunction(_uiStudioSystem, _logger);
    var httpRequest = CreateMockRequest(request);
    var response = await function.CreatePage(httpRequest);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
}
```

### Frontend Unit Tests

```typescript
// API Service Tests
describe('UIStudioApiService', () => {
  let service: UIStudioApiService;
  let mockFetch: jest.MockedFunction<typeof fetch>;

  beforeEach(() => {
    mockFetch = jest.fn();
    global.fetch = mockFetch;
    service = new UIStudioApiService();
  });

  test('should create page successfully', async () => {
    // Arrange
    const request: CreatePageRequest = {
      pageName: 'Test Page',
      pageSlug: 'test-page',
      createdByEntityId: 'user-123'
    };

    const expectedResponse = [{
      id: 'page-123',
      ownerEntityId: 'entity-123',
      pageName: 'Test Page',
      pageSlug: 'test-page'
    }];

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 201,
      json: () => Promise.resolve(expectedResponse)
    } as Response);

    // Act
    const result = await service.createPage(request);

    // Assert
    expect(mockFetch).toHaveBeenCalledWith('/api/uistudio/pages', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': expect.stringContaining('Bearer')
      },
      body: JSON.stringify(request)
    });

    expect(result).toEqual(expectedResponse);
  });

  test('should handle API errors', async () => {
    // Arrange
    const request: CreatePageRequest = {
      pageName: 'Test Page',
      pageSlug: 'test-page',
      createdByEntityId: 'user-123'
    };

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: () => Promise.resolve({
        error: {
          message: 'Page slug already exists',
          code: 'RESOURCE_CONFLICT'
        }
      })
    } as Response);

    // Act & Assert
    await expect(service.createPage(request))
      .rejects
      .toThrow('Page slug already exists');
  });
});
```

### Component Tests

```typescript
// React Hook Tests
describe('useUIStudioPages', () => {
  test('should load pages successfully', async () => {
    // Arrange
    const mockPages = [
      { id: '1', pageName: 'Page 1' },
      { id: '2', pageName: 'Page 2' }
    ];

    jest.spyOn(apiService, 'getPages').mockResolvedValue({
      items: mockPages,
      totalCount: 2,
      hasMore: false
    });

    // Act
    const { result } = renderHook(() => useUIStudioPages());
    
    await act(async () => {
      await result.current.loadPages();
    });

    // Assert
    expect(result.current.pages).toEqual(mockPages);
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBe(null);
  });

  test('should handle loading states', async () => {
    // Arrange
    let resolvePromise: (value: unknown) => void;
    const promise = new Promise(resolve => {
      resolvePromise = resolve;
    });

    jest.spyOn(apiService, 'getPages').mockReturnValue(promise);

    // Act
    const { result } = renderHook(() => useUIStudioPages());
    
    act(() => {
      result.current.loadPages();
    });

    // Assert loading state
    expect(result.current.loading).toBe(true);

    // Complete the promise
    await act(async () => {
      resolvePromise({ items: [], totalCount: 0, hasMore: false });
    });

    expect(result.current.loading).toBe(false);
  });
});
```

### End-to-End Tests

```typescript
// Playwright E2E Tests
test.describe('UIStudio Page Management', () => {
  test('should create and publish page', async ({ page }) => {
    // Navigate to page creation
    await page.goto('/pages/create');
    
    // Fill form
    await page.fill('[data-testid="page-name"]', 'E2E Test Page');
    await page.fill('[data-testid="page-slug"]', 'e2e-test-page');
    await page.selectOption('[data-testid="page-type"]', 'dynamic');
    
    // Configure layout
    await page.click('[data-testid="layout-config"]');
    await page.fill('[data-testid="grid-columns"]', '16');
    
    // Create page
    await page.click('[data-testid="create-page"]');
    
    // Verify creation
    await expect(page.locator('[data-testid="success-message"]'))
      .toContainText('Page created successfully');
    
    // Publish page
    await page.click('[data-testid="publish-page"]');
    
    // Verify publication
    await expect(page.locator('[data-testid="page-status"]'))
      .toContainText('Published');
    
    // Verify public access
    await page.goto('/pages/published');
    await expect(page.locator('[data-testid="page-list"]'))
      .toContainText('E2E Test Page');
  });

  test('should handle validation errors', async ({ page }) => {
    await page.goto('/pages/create');
    
    // Try to create without required fields
    await page.click('[data-testid="create-page"]');
    
    // Verify validation errors
    await expect(page.locator('[data-testid="error-message"]'))
      .toContainText('Page name is required');
    await expect(page.locator('[data-testid="error-message"]'))
      .toContainText('Page slug is required');
  });
});
```

## Best Practices Summary

### Backend (C#)
1. **Follow ECS Pattern**: API → System → Handler → Component
2. **Use Immutable Records**: All components should be `record` types
3. **Implement Proper Validation**: Validate at API layer and business logic layer
4. **Handle Concurrency**: Use `IVersionedComponent` for conflict detection
5. **Comprehensive Logging**: Log all operations with correlation IDs
6. **Security First**: Implement proper authentication and authorization

### Frontend (TypeScript)
1. **Type Safety**: Define comprehensive TypeScript interfaces
2. **Error Handling**: Implement consistent error handling patterns
3. **Loading States**: Provide visual feedback for all async operations
4. **Caching Strategy**: Cache frequently accessed data appropriately
5. **Performance**: Use pagination, virtual scrolling, and optimistic updates
6. **Testing**: Write comprehensive unit, integration, and E2E tests

### Integration
1. **API Consistency**: Maintain consistent request/response patterns
2. **Documentation**: Keep API documentation up-to-date
3. **Versioning**: Plan for API versioning from the start
4. **Monitoring**: Implement comprehensive monitoring and alerting
5. **Security**: Use HTTPS, validate all inputs, implement proper CORS

This comprehensive API reference provides the foundation for building robust UIStudio applications using the Jarvis ECS framework with proper architectural patterns and best practices.