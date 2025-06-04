# Jarvis.Core

Jarvis.Core is an Entity Component System (ECS) framework designed for building scalable and modular applications. It provides a flexible architecture for managing entities and their components.

## Features

- **Entity Component System**: A pattern that enables clean separation of data (components) from logic
- **Supabase PostgreSQL Storage**: Persistent storage for entities and components using Supabase
- **Hierarchical Entity Structure**: Support for parent-child relationships between entities
- **Query Capabilities**: Expressive API for retrieving and manipulating entity data

## Installation

Add a reference to the Jarvis.Core library in your project:

```bash
dotnet add reference path/to/core.jarvis/core.jarvis.csproj
```

Or via NuGet (if published):

```bash
dotnet add package core.jarvis
```

## Configuration

### Supabase Setup

Configure Supabase connection in your application:

```csharp
// Add to your dependency injection setup
services.AddJarvisECS(options =>
{
    options.UseSupabaseStorage(config =>
    {
        config.Url = "https://your-supabase-url.supabase.co";
        config.Key = "your-supabase-api-key";
    });
});
```

### Required Database Schema

Create the necessary tables in your Supabase instance:

```sql
CREATE TABLE entities (
  id UUID PRIMARY KEY,
  type TEXT,
  created_at TIMESTAMPTZ,
  parent_id UUID NULL REFERENCES entities(id),
  children_ids UUID[] NULL,
  root_id UUID NULL
);

-- Create tables for each component type
CREATE TABLE your_component_name (
  id UUID PRIMARY KEY,
  entity_id UUID NOT NULL UNIQUE REFERENCES entities(id),
  -- component-specific fields
);
```

## Usage Examples

### Creating Entities and Components

```csharp
// Get the entity context from dependency injection
var entityContext = serviceProvider.GetRequiredService<IEntityContext>();

// Create a new entity
var entity = await entityContext.CreateEntityAsync();

// Add a component to the entity
await entity.AddComponentAsync(new YourComponent
{
    Property1 = "value",
    Property2 = 42
});

// Save changes
await entityContext.SaveChangesAsync();
```

### Querying Entities

```csharp
// Using the Supabase client directly (recommended approach)
var components = await dataContext.From<YourComponent>()
    .Where(x => x.Property1 == "value")
    .Order(x => x.Property2)
    .Get();

// Legacy query approach (marked as obsolete)
var entities = await entityContext.Query()
    .WithComponent<YourComponent>(c => c.Property1 == "value")
    .OrderBy(e => e.GetComponent<YourComponent>().Property2)
    .ToListAsync();
```

## Documentation

For more detailed documentation and migration guides, see:

- [Supabase Storage Usage Guide](docs/SupabaseStorageUsage.md)
- [Migration to Supabase](docs/MigrationToSupabase.md)

## License

[License information]