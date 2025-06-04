# Supabase Integration

Jarvis ECS uses Supabase for persistent storage of entities and components.

## Configuration

Add Supabase to your DI setup:

```csharp
services.AddJarvisECS(options =>
{
    options.UseSupabaseStorage(config =>
    {
        config.Url = "https://your-supabase-url.supabase.co";
        config.Key = "your-supabase-api-key";
    });
});
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