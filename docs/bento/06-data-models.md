# Data Models - Production Implementation

This document describes the production-ready data models used in the UIStudio API implementation. All models are fully implemented and follow Jarvis ECS patterns.

## Overview

The UIStudio APIs use well-defined data models that implement the Jarvis ECS framework interfaces. These are **production models**, not mockups or prototypes.

## Core Interfaces

All UIStudio components implement these Jarvis interfaces:

```csharp
public interface IComponent
{
    Guid Id { get; init; }
    Guid OwnerEntityId { get; set; }
    DateTime LastUpdated { get; set; }
}

public interface IVersionedComponent : IComponent
{
    int? Version { get; set; }
}
```

## UIStudioPage Model

**Production Implementation**: `core.jarvis.api.Models.UIStudioPage`

```csharp
public record UIStudioPage : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    
    // Page Identity
    public string PageName { get; init; } = string.Empty;
    public string PageSlug { get; init; } = string.Empty;
    public string PageType { get; init; } = "dynamic"; // "dynamic", "fixed", "hybrid"
    public string? Description { get; init; }
    
    // Publishing State
    public bool IsPublished { get; init; } = false;
    public bool IsDefault { get; init; } = false;
    
    // Metadata and Configuration
    public Dictionary<string, object>? Metadata { get; init; }
    public string? Tags { get; init; }
    public int SortOrder { get; init; } = 0;
    
    // Audit Trail
    public Guid CreatedByEntityId { get; init; }
    public Guid? ModifiedByEntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }
}
```

**Database Mapping**: `ui_studio_page` table (automatic snake_case conversion)

### Key Features:
- **Page Types**: Supports dynamic (data-driven), fixed (static), and hybrid pages
- **Publishing**: Built-in publishing workflow with state tracking
- **Metadata**: Flexible JSON metadata for SEO, custom properties
- **Versioning**: Full version control support

## UIStudioLayout Model

**Production Implementation**: `core.jarvis.api.Models.UIStudioLayout`

```csharp
public record UIStudioLayout : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    
    // Layout Identity
    public string LayoutName { get; init; } = string.Empty;
    public string LayoutType { get; init; } = "bento"; // "bento", "grid", "flex", "absolute"
    
    // Layout Configuration
    public Dictionary<string, object>? GridConfig { get; init; }
    public Dictionary<string, object>? ResponsiveBreakpoints { get; init; }
    public Dictionary<string, object>? ContainerSettings { get; init; }
    
    // Styling
    public string? CssClasses { get; init; }
    public string? CustomStyles { get; init; }
    
    // Template Support
    public bool IsTemplate { get; init; } = false;
    public string? TemplateCategory { get; init; }
    public string? ThumbnailUrl { get; init; }
    
    // Audit Trail
    public Guid CreatedByEntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }
    public string SchemaVersion { get; init; } = "1.0";
}
```

**Database Mapping**: `ui_studio_layout` table

### Grid Configuration Example:
```json
{
  "type": "bento",
  "columns": 12,
  "rows": "auto",
  "gap": "16px",
  "padding": "20px",
  "minItemWidth": "200px",
  "minItemHeight": "150px"
}
```

### Responsive Breakpoints Example:
```json
{
  "mobile": { "columns": 1, "gap": "8px", "maxWidth": "640px" },
  "tablet": { "columns": 2, "gap": "12px", "maxWidth": "768px" },
  "desktop": { "columns": 4, "gap": "16px", "maxWidth": "1024px" },
  "ultrawide": { "columns": 6, "gap": "20px", "maxWidth": "1536px" }
}
```

## UIStudioComponentBinding Model

**Production Implementation**: `core.jarvis.api.Models.UIStudioComponentBinding`

```csharp
public record UIStudioComponentBinding : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    
    // Component Identity
    public string PageSlug { get; init; } = string.Empty;
    public string ComponentType { get; init; } = string.Empty; // "table", "card", "chart", "form"
    public string ComponentInstanceId { get; init; } = string.Empty;
    
    // ECS Binding
    public string BoundComponentType { get; init; } = string.Empty; // ECS component name
    public Dictionary<string, object>? FieldMappings { get; init; }
    public Dictionary<string, object>? DataSourceConfig { get; init; }
    
    // UI Configuration
    public Dictionary<string, object>? PositionConfig { get; init; }
    public Dictionary<string, object>? StyleConfig { get; init; }
    public Dictionary<string, object>? BehaviorConfig { get; init; }
    
    // State and Permissions
    public bool IsVisible { get; init; } = true;
    public bool IsEnabled { get; init; } = true;
    public string? ViewPermissions { get; init; }
    public string? EditPermissions { get; init; }
    public int SortOrder { get; init; } = 0;
    
    // Audit Trail
    public Guid CreatedByEntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }
    public string SchemaVersion { get; init; } = "1.0";
}
```

**Database Mapping**: `ui_studio_component_binding` table

### Field Mappings Example:
```json
{
  "title": "$.name",
  "description": "$.description",
  "status": "$.status",
  "assignee": "$.assignedTo.name",
  "dueDate": "$.dueDate",
  "priority": "$.priority"
}
```

### Data Source Configuration Example:
```json
{
  "componentType": "TaskComponent",
  "filters": [
    { "field": "status", "operator": "eq", "value": "active" },
    { "field": "assignedTo", "operator": "eq", "value": "{currentUserId}" }
  ],
  "sorting": [
    { "field": "priority", "direction": "desc" },
    { "field": "dueDate", "direction": "asc" }
  ],
  "pagination": {
    "pageSize": 20,
    "enabled": true
  }
}
```

### Position Configuration Example:
```json
{
  "gridColumn": "1 / 3",
  "gridRow": "1 / 2",
  "minWidth": "300px",
  "minHeight": "200px",
  "zIndex": 1,
  "sticky": false
}
```

## UIStudioTemplate Model

**Status**: ⚠️ **MODEL NOT YET IMPLEMENTED** - Referenced in UIStudioSystem but model file missing

**Expected Implementation**: `core.jarvis.api.Models.UIStudioTemplate`

```csharp
// TODO: Create this model file
public record UIStudioTemplate : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    
    // Template Identity
    public string TemplateName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TemplateType { get; init; } = "page"; // "page", "layout", "component"
    public string? Category { get; init; }
    
    // Template Configuration
    public Dictionary<string, object>? TemplateData { get; init; }
    public Dictionary<string, object>? DefaultValues { get; init; }
    public string? PreviewImage { get; init; }
    
    // Usage and Access
    public bool IsPublic { get; init; } = false;
    public string? Tags { get; init; }
    public int UsageCount { get; init; } = 0;
    
    // Audit Trail
    public Guid CreatedByEntityId { get; init; }
    public Guid? ModifiedByEntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }
}
```

## UIStudioPermission Model

**Status**: ⚠️ **MODEL NOT YET IMPLEMENTED** - Referenced in UIStudioSystem but model file missing

**Expected Implementation**: `core.jarvis.api.Models.UIStudioPermission`

```csharp
public record UIStudioPermission : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    
    // Permission Definition
    public Guid ResourceEntityId { get; init; }
    public string ResourceType { get; init; } = "page"; // "page", "layout", "template"
    public Guid GranteeEntityId { get; init; }
    public string PermissionLevel { get; init; } = "read"; // "read", "write", "admin"
    
    // Permission Metadata
    public string? Reason { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; } = true;
    
    // Audit Trail
    public Guid GrantedByEntityId { get; init; }
    public DateTime GrantedAt { get; init; }
    public DateTime LastUpdated { get; set; }
}
```

## UIStudioVersion Model

**Status**: ⚠️ **MODEL NOT YET IMPLEMENTED** - Referenced in UIStudioSystem but model file missing

**Expected Implementation**: `core.jarvis.api.Models.UIStudioVersion`

```csharp
public record UIStudioVersion : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    
    // Version Identity
    public Guid ResourceEntityId { get; init; }
    public string ResourceType { get; init; } = "page";
    public string VersionLabel { get; init; } = string.Empty;
    public string VersionType { get; init; } = "manual"; // "manual", "auto", "published"
    
    // Version Data
    public Dictionary<string, object>? SnapshotData { get; init; }
    public string? ChangeDescription { get; init; }
    public string? ChangeReason { get; init; }
    
    // Version Metadata
    public bool IsPublished { get; init; } = false;
    public DateTime? PublishedAt { get; init; }
    
    // Audit Trail
    public Guid CreatedByEntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; set; }
}
```

## Entity Relationships

**Production Pattern**: Uses `LinkRelationship` for entity associations

### Page → Layout Relationship
```csharp
// Link page to its layout
await _dataContext.LinkRelationship(
    pageEntityId,          // parent
    layoutEntityId,        // child
    "UIStudioPage",        // parent type
    "UIStudioLayout"       // child type
);
```

### Page → Component Bindings Relationship
```csharp
// Link page to component binding
await _dataContext.LinkRelationship(
    pageEntityId,              // parent
    bindingEntityId,           // child
    "UIStudioPage",            // parent type
    "UIStudioComponentBinding" // child type
);
```

### Query Related Entities
```csharp
// Get all child entities of a page
var childEntityIds = await _dataContext.Children(pageEntityId);

// Get parent entity of a binding
var parentEntityId = await _dataContext.Parent(bindingEntityId);

// Check if entity is child of another
var isChild = await _dataContext.ChildOf(bindingEntityId, pageEntityId);
```

## Database Schema

All models use automatic PostgreSQL table creation with snake_case conversion:

- `UIStudioPage` → `ui_studio_page`
- `UIStudioLayout` → `ui_studio_layout` 
- `UIStudioComponentBinding` → `ui_studio_component_binding`
- `UIStudioTemplate` → `ui_studio_template`
- `UIStudioPermission` → `ui_studio_permission`
- `UIStudioVersion` → `ui_studio_version`

### Common Columns
All tables include:
- `id` (uuid, primary key)
- `owner_entity_id` (uuid, required)
- `last_updated` (timestamp)
- `version` (integer, for versioned components)

## Data Validation

### Required Fields
- All components require `OwnerEntityId`
- Pages require `PageName`, `PageSlug`, `CreatedByEntityId`
- Bindings require `ComponentType`, `BoundComponentType`, `CreatedByEntityId`
- Layouts require `CreatedByEntityId`

### Business Rules
- Page slugs must be unique within tenant
- Component instance IDs must be unique within page
- Permission levels: "read", "write", "admin"
- Page types: "dynamic", "fixed", "hybrid"

## Implementation Status

### ✅ **Fully Implemented Models**:
- **UIStudioPage**: Complete CRUD operations with publishing workflow
- **UIStudioLayout**: Grid configuration and responsive design
- **UIStudioComponentBinding**: ECS component field mappings
- **Entity Relationships**: Proper LinkRelationship usage throughout
- **Advanced Querying**: Filtering, search, and pagination
- **Bulk Operations**: Efficient batch processing

### ⚠️ **Models Requiring Implementation**:
- **UIStudioTemplate**: Referenced in UIStudioSystem but model files missing
- **UIStudioPermission**: Referenced in UIStudioSystem but model files missing
- **UIStudioVersion**: Referenced in UIStudioSystem but model files missing
- **Related Handlers**: Template, Permission, and Version handlers need creation

### 🟠 **Next Steps**:
1. Create missing model files in `/core.jarvis.api/Models/`
2. Implement corresponding handlers in `/core.jarvis.api/Handlers/`
3. Register handlers in dependency injection
4. Add API endpoints in Functions
5. Update tests for new components

For implementation examples of working models, see [UIStudio API Reference](10-uistudio-api-reference.md).