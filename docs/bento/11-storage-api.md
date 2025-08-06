# Storage API - Production Implementation

This document describes the production-ready data persistence and storage capabilities of the UIStudio APIs. All storage operations are fully implemented using the Jarvis ECS framework with PostgreSQL backend.

## Overview

The UIStudio storage system provides:
- **Production PostgreSQL integration** with automatic schema management
- **ECS-compliant data patterns** using Components, Handlers, and Systems
- **Automatic versioning** for all versioned components
- **Entity relationship management** via LinkRelationship
- **Optimistic concurrency control** with LastUpdated timestamps
- **Row-level security** with JWT-based access control

## Database Architecture

### Automatic Table Creation

All UIStudio components automatically create PostgreSQL tables with snake_case naming:

```sql
-- UIStudioPage → ui_studio_page
CREATE TABLE ui_studio_page (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    page_name TEXT NOT NULL,
    page_slug TEXT NOT NULL,
    page_type TEXT DEFAULT 'dynamic',
    description TEXT,
    is_published BOOLEAN DEFAULT false,
    is_default BOOLEAN DEFAULT false,
    metadata JSONB,
    tags TEXT,
    sort_order INTEGER DEFAULT 0,
    created_by_entity_id UUID NOT NULL,
    modified_by_entity_id UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    version INTEGER
);

-- UIStudioLayout → ui_studio_layout  
CREATE TABLE ui_studio_layout (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    layout_type TEXT DEFAULT 'grid',
    grid_config JSONB,
    responsive_config JSONB,
    breakpoint_settings JSONB,
    is_responsive BOOLEAN DEFAULT true,
    max_columns INTEGER DEFAULT 12,
    max_rows INTEGER DEFAULT 0,
    container_config TEXT,
    created_by_entity_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    version INTEGER
);

-- UIStudioComponentBinding → ui_studio_component_binding
CREATE TABLE ui_studio_component_binding (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    page_slug TEXT NOT NULL,
    component_type TEXT NOT NULL,
    component_instance_id TEXT NOT NULL,
    bound_component_type TEXT NOT NULL,
    field_mappings JSONB,
    data_source_config JSONB,
    position_config JSONB,
    style_config JSONB,
    behavior_config JSONB,
    is_visible BOOLEAN DEFAULT true,
    is_enabled BOOLEAN DEFAULT true,
    view_permissions TEXT,
    edit_permissions TEXT,
    sort_order INTEGER DEFAULT 0,
    created_by_entity_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    version INTEGER,
    schema_version TEXT DEFAULT '1.0'
);
```

### Indexes and Constraints

Production indexes for optimal performance:

```sql
-- Page indexes
CREATE UNIQUE INDEX idx_ui_studio_page_slug ON ui_studio_page(page_slug);
CREATE INDEX idx_ui_studio_page_owner ON ui_studio_page(owner_entity_id);
CREATE INDEX idx_ui_studio_page_published ON ui_studio_page(is_published);
CREATE INDEX idx_ui_studio_page_type ON ui_studio_page(page_type);

-- Component binding indexes
CREATE INDEX idx_ui_studio_binding_page_slug ON ui_studio_component_binding(page_slug);
CREATE INDEX idx_ui_studio_binding_component_type ON ui_studio_component_binding(component_type);
CREATE UNIQUE INDEX idx_ui_studio_binding_instance ON ui_studio_component_binding(page_slug, component_instance_id);

-- Layout indexes
CREATE INDEX idx_ui_studio_layout_owner ON ui_studio_layout(owner_entity_id);
CREATE INDEX idx_ui_studio_layout_type ON ui_studio_layout(layout_type);
```

## Data Access Patterns

### Handler-Based CRUD Operations

All data access uses the Jarvis Handler pattern:

```csharp
// Create a new page
var pageEntity = _dataContext.NewEntity();
var pageHandler = _dataContext.For<UIStudioPageHandler>(pageEntity.Id);

var page = new UIStudioPage
{
    OwnerEntityId = pageEntity.Id,
    PageName = "Analytics Dashboard",
    PageSlug = "analytics",
    CreatedByEntityId = userId,
    CreatedAt = DateTime.UtcNow,
    LastUpdated = DateTime.UtcNow
};

var createdPage = await pageHandler.CreatePage(page);
```

### Component Retrieval

```csharp
// Get a specific page
var pageHandler = _dataContext.For<UIStudioPageHandler>(pageEntityId);
var page = await pageHandler.Get();

// Get pages by owner
var pages = await pageHandler.GetByOwner(ownerEntityId);

// Try to get with null handling
var page = await pageHandler.TryGet();
if (page != null)
{
    // Process page
}
```

### Update Operations

```csharp
// Update with optimistic concurrency
var pageHandler = _dataContext.For<UIStudioPageHandler>(pageEntityId);
var page = await pageHandler.Get();

var updatedPage = page with 
{ 
    PageName = "Updated Dashboard",
    LastUpdated = DateTime.UtcNow 
};

await pageHandler.Commit(updatedPage);
```

### Safe Update with TryCommit

```csharp
// Handle concurrency conflicts gracefully
var success = await pageHandler.TryCommit(updatedPage);
if (!success)
{
    // Handle conflict - component was modified by another user
    var currentPage = await pageHandler.Get();
    // Merge changes or prompt user
}
```

## Entity Relationships

### LinkRelationship Storage Pattern

UIStudio uses the Jarvis relationship system for entity associations:

```csharp
// Create page with layout relationship
var pageEntity = _dataContext.NewEntity();
var layoutEntity = _dataContext.NewEntity();

// Create components
var page = await pageHandler.CreatePage(pageComponent);
var layout = await layoutHandler.CreateLayout(layoutComponent);

// Link entities (stored in separate relationship table)
await _dataContext.LinkRelationship(
    pageEntity.Id,      // parent entity
    layoutEntity.Id,    // child entity  
    "UIStudioPage",     // parent type
    "UIStudioLayout"    // child type
);
```

### Relationship Queries

```csharp
// Get all child entities of a page
var childEntityIds = await _dataContext.Children(pageEntityId);

// Get parent entity
var parentEntityId = await _dataContext.Parent(bindingEntityId);

// Check if entity is child of another
var isChild = await _dataContext.ChildOf(bindingEntityId, pageEntityId);

// Get all related entities with their components
var relatedEntities = await _dataContext.Query()
    .WithAll<UIStudioPage>(p => p.OwnerEntityId == pageEntityId)
    .ToEntityComponents();
```

## Version Control Storage

### Automatic Versioning

Versioned components automatically increment version numbers:

```csharp
public record UIStudioPage : IComponent, IVersionedComponent
{
    public int? Version { get; set; }
    // ... other properties
}

// Version is automatically incremented on each commit
var page = await pageHandler.Get(); // version = 1
var updatedPage = page with { PageName = "New Name" };
await pageHandler.Commit(updatedPage); // version = 2
```

### Manual Version Snapshots

```csharp
var versionEntity = _dataContext.NewEntity();
var versionHandler = _dataContext.For<UIStudioVersionHandler>(versionEntity.Id);

var version = new UIStudioVersion
{
    OwnerEntityId = versionEntity.Id,
    ResourceEntityId = pageEntityId,
    ResourceType = "page",
    VersionLabel = "v1.2.0",
    SnapshotData = await GetPageSnapshot(pageEntityId),
    CreatedByEntityId = userId
};

await versionHandler.CreateVersion(version);
```

### Version Snapshot Data Structure

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "resource_type": "page",
  "page": {
    "id": "456e7890-e89b-12d3-a456-426614174001",
    "pageName": "Dashboard",
    "pageSlug": "dashboard",
    "pageType": "dynamic",
    "isPublished": true
  },
  "layout": {
    "id": "789e1234-e89b-12d3-a456-426614174002", 
    "layoutType": "grid",
    "gridConfig": {
      "columns": 12,
      "gap": "16px"
    }
  },
  "component_bindings": [
    {
      "id": "abc12345-e89b-12d3-a456-426614174003",
      "componentType": "table",
      "componentInstanceId": "tasks-table",
      "boundComponentType": "TaskComponent"
    }
  ]
}
```

## Query and Filtering

### Component-Based Queries

```csharp
// Find published pages
var publishedPages = await _dataContext.Query()
    .WithAll<UIStudioPage>(p => p.IsPublished)
    .ToEntityComponents();

// Complex filtering
var filteredResults = await _dataContext.Query()
    .WithAll<UIStudioPage>(p => p.PageType == "dynamic" && p.IsPublished)
    .WithAll<UIStudioComponentBinding>(b => b.ComponentType == "table")
    .ToEntityComponents();
```

### Search Operations

```csharp
// Search in page names and descriptions
var searchResults = await _dataContext.Query()
    .WithAll<UIStudioPage>(p => 
        p.PageName.Contains(searchTerm) || 
        (p.Description != null && p.Description.Contains(searchTerm)))
    .ToEntityComponents();
```

### Pagination Support

```csharp
// Get paginated results
var query = _dataContext.Query()
    .WithAll<UIStudioPage>(p => p.IsPublished);

var results = await query.ToEntityComponents();
var paginatedResults = results
    .Skip(offset)
    .Take(limit)
    .ToList();
```

## Bulk Operations

### Bulk Component Binding Creation

```csharp
public async Task<List<IComponent>> BulkCreateBindings(List<UIStudioComponentBinding> bindings)
{
    var allComponents = new List<IComponent>();

    foreach (var binding in bindings)
    {
        var bindingEntity = _dataContext.NewEntity();
        var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(bindingEntity.Id);
        
        var bindingWithEntity = binding with { OwnerEntityId = bindingEntity.Id };
        var createdBinding = await bindingHandler.CreateBinding(bindingWithEntity);
        
        allComponents.Add(createdBinding);
    }

    return allComponents;
}
```

### Transactional Operations

```csharp
// Multiple operations within transaction scope
try
{
    var page = await pageHandler.CreatePage(pageComponent);
    var layout = await layoutHandler.CreateLayout(layoutComponent);
    
    await _dataContext.LinkRelationship(
        page.OwnerEntityId,
        layout.OwnerEntityId,
        "UIStudioPage",
        "UIStudioLayout"
    );
    
    // All operations succeed or fail together
}
catch (Exception ex)
{
    // Rollback handled automatically
    _logger.LogError(ex, "Failed to create page with layout");
    throw;
}
```

## Concurrency Control

### Optimistic Concurrency with LastUpdated

```csharp
// Component includes LastUpdated timestamp
public record UIStudioPage : IComponent
{
    public DateTime LastUpdated { get; set; }
    // ... other properties
}

// Update checks LastUpdated to prevent conflicts
var originalPage = await pageHandler.Get();
var updatedPage = originalPage with 
{ 
    PageName = "New Name",
    LastUpdated = DateTime.UtcNow // Must be newer than original
};

try
{
    await pageHandler.Commit(updatedPage);
}
catch (ConcurrencyException)
{
    // Handle conflict - component was modified by another user
}
```

### Version-Based Concurrency

```csharp
// For versioned components, version number is checked
var page = await pageHandler.Get(); // version = 5
var updatedPage = page with { PageName = "New Name" };

// Version automatically incremented: 5 → 6
await pageHandler.Commit(updatedPage);
```

## Security and Access Control

### Row-Level Security (RLS)

PostgreSQL RLS policies automatically applied based on JWT context:

```sql
-- Enable RLS on all tables
ALTER TABLE ui_studio_page ENABLE ROW LEVEL SECURITY;
ALTER TABLE ui_studio_layout ENABLE ROW LEVEL SECURITY;
ALTER TABLE ui_studio_component_binding ENABLE ROW LEVEL SECURITY;

-- Example RLS policy (configured automatically)
CREATE POLICY ui_studio_page_access ON ui_studio_page
    USING (
        created_by_entity_id = current_setting('app.current_user_id')::UUID OR
        owner_entity_id IN (
            SELECT entity_id FROM user_permissions 
            WHERE user_id = current_setting('app.current_user_id')::UUID
        )
    );
```

### Permission-Based Access

```csharp
// Check permissions before data operations
var permissionHandler = _dataContext.For<UIStudioPermissionHandler>(Guid.NewGuid());
var hasAccess = await permissionHandler.CheckPermission(
    resourceEntityId: pageEntityId,
    resourceType: "page",
    granteeEntityId: currentUserId,
    requiredLevel: "read"
);

if (!hasAccess)
{
    throw new UnauthorizedException("Insufficient permissions");
}
```

## Performance Optimizations

### Connection Pooling

Automatic PostgreSQL connection pooling via Npgsql:

```csharp
// Configured in DI registration
services.RegisterJarvis(LogLevel.Information, Configuration);
// Uses optimized connection pooling automatically
```

### Prepared Statements

All database operations use prepared statements for security and performance:

```csharp
// Handlers automatically use parameterized queries
var pages = await pageHandler.GetByOwner(ownerEntityId);
// Executes: SELECT * FROM ui_studio_page WHERE owner_entity_id = @ownerId
```

### Caching Strategies

```csharp
// Cache frequently accessed pages
private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

public async Task<UIStudioPage?> GetPageCached(Guid pageEntityId)
{
    var cacheKey = $"page:{pageEntityId}";
    
    if (_cache.TryGetValue(cacheKey, out UIStudioPage? cachedPage))
    {
        return cachedPage;
    }
    
    var pageHandler = _dataContext.For<UIStudioPageHandler>(pageEntityId);
    var page = await pageHandler.Get();
    
    if (page != null)
    {
        _cache.Set(cacheKey, page, TimeSpan.FromMinutes(10));
    }
    
    return page;
}
```

## Backup and Recovery

### Automated Snapshots

Version control system provides automatic backup:

```csharp
// Automatic version creation on significant changes
public async Task<UIStudioPage> UpdatePage(UIStudioPage updatedPage)
{
    // Create automatic snapshot before update
    var versionHandler = _dataContext.For<UIStudioVersionHandler>(Guid.NewGuid());
    await versionHandler.CreateAutoVersion(
        updatedPage.OwnerEntityId,
        "page",
        await GetPageSnapshot(updatedPage.OwnerEntityId),
        updatedPage.ModifiedByEntityId ?? updatedPage.CreatedByEntityId,
        "Automatic snapshot before update"
    );
    
    // Perform update
    var pageHandler = _dataContext.For<UIStudioPageHandler>(updatedPage.OwnerEntityId);
    return await pageHandler.Commit(updatedPage);
}
```

### Point-in-Time Recovery

```csharp
// Restore from version snapshot
public async Task<List<object>> RestoreFromVersion(Guid versionEntityId, Guid restoredByUserId)
{
    var versionHandler = _dataContext.For<UIStudioVersionHandler>(versionEntityId);
    var version = await versionHandler.Get();
    
    if (version?.SnapshotData == null)
        throw new InvalidOperationException("Version snapshot not found");
    
    // Restore page from snapshot data
    var pageData = version.SnapshotData["page"];
    var pageHandler = _dataContext.For<UIStudioPageHandler>(version.ResourceEntityId);
    var restoredPage = await pageHandler.RestoreFromSnapshot(pageData, restoredByUserId);
    
    return new List<object> { restoredPage };
}
```

## Production Deployment

### Database Migration

Automatic schema management on startup:

```csharp
// Tables created automatically when handlers are first used
var pageHandler = _dataContext.For<UIStudioPageHandler>(entityId);
// ui_studio_page table created if it doesn't exist
```

### Health Checks

```csharp
public async Task<bool> CheckStorageHealth()
{
    try
    {
        // Test basic database connectivity
        var testEntity = _dataContext.NewEntity();
        var pageHandler = _dataContext.For<UIStudioPageHandler>(testEntity.Id);
        
        // Attempt to query (will fail gracefully if DB is down)
        var testPage = await pageHandler.TryGet();
        return true;
    }
    catch
    {
        return false;
    }
}
```

### Monitoring

```csharp
// Built-in logging for all storage operations
public async Task<UIStudioPage> CreatePage(UIStudioPage page)
{
    _logger.LogInformation("Creating page {PageName} for entity {EntityId}", 
        page.PageName, page.OwnerEntityId);
    
    try
    {
        var createdPage = await _pageHandler.CreatePage(page);
        
        _logger.LogInformation("Successfully created page {PageId}", createdPage.Id);
        return createdPage;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create page {PageName}", page.PageName);
        throw;
    }
}
```

## Production Status

**✅ All storage capabilities are production-ready**:

- **PostgreSQL Integration**: Full production database support with automatic schema management
- **ECS Compliance**: All storage operations follow Jarvis ECS patterns
- **Concurrency Control**: Optimistic concurrency with automatic conflict detection
- **Version Control**: Complete versioning and snapshot capabilities
- **Relationships**: Proper entity relationship management with LinkRelationship
- **Security**: Row-level security with JWT-based access control  
- **Performance**: Connection pooling, prepared statements, and caching support
- **Monitoring**: Comprehensive logging and health checks
- **Backup**: Automatic versioning provides built-in backup capabilities

The storage layer is ready for production Bento Grid integration with full data persistence, version control, and security features.