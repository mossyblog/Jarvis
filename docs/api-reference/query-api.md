# Query API

Jarvis ECS provides a fluent query API for retrieving entities and their components.

## Key Methods

- `With<T>(filter)`: Filter entities by a component property.
- `Include<T>()`: Eager load a component type.
- `ToEntityIds()`: Get matching entity IDs.
- `ToEntityComponents()`: Get entities and their loaded components.

## Example

```csharp
var query = dataContext.Query()
    .With<MyComponent>(c => c.Property1 == "value")
    .Include<OtherComponent>();

var entityIds = await query.ToEntityIds();
var entities = await query.ToEntityComponents();
```

This approach avoids N+1 query problems and supports batching and parallel loading. 