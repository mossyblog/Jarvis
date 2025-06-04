# Your First Handler

This guide walks you through creating your first entity and component handler in Jarvis ECS.

## 1. Configure Dependency Injection

Add Jarvis ECS to your DI setup:

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

## 2. Create an Entity and Add a Component

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