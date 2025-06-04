# Handler Pattern

Jarvis ECS uses a handler-based architecture. Each component type has a corresponding handler that encapsulates all business logic for that component.

## Key Concepts

- **ComponentHandler<TComponent>**: Base class for handlers, provides common logic and access to the database and logging.
- **IComponentHandler**: Interface for all handlers, ensures a consistent API.

## Example

```csharp
public class MyComponentHandler : ComponentHandler<MyComponent>
{
    public MyComponentHandler(Guid entityId, Supabase.Client client, ILogger logger)
        : base(entityId, client, logger) { }

    // Implement business logic here
}
```

Handlers are resolved via the registry and used to perform operations on components for a specific entity. 