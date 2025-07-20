# PostgreSQL Integration

Jarvis ECS uses PostgreSQL for persistent storage of entities and components via the `supabase/postgres` Docker image which provides PostgreSQL with useful extensions.

## Configuration

Configure PostgreSQL connection in your DI setup:

```csharp
services.RegisterJarvis();

// Connection string configuration (appsettings.json)
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres"
  }
}
```

## Required Database Schema

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