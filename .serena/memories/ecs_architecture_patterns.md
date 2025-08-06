# ECS Architecture Patterns

## Core Principles
The Jarvis framework follows Entity Component System (ECS) architecture with these key patterns:

## Handler Pattern
```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    public OrderHandler(IDataContext dataContext, Guid ownerEntityId) 
        : base(dataContext, ownerEntityId) { }
    
    // Business logic methods here
}
```

## System + Handler Rules
1. **Systems** orchestrate workflows, return `List<IComponent>`
2. **Handlers** manage CRUD for one component type, return `TComponent`
3. **Functions** are thin HTTP adapters, delegate to Systems
4. **Components** are immutable records with no logic

## Dependency Injection Setup
```csharp
// In Program.cs - BOTH registrations required!
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>(); // Required for DataContext.For<T>()
```

## Entity Relationships
```csharp
// Link parent-child
await dataContext.LinkRelationship(parentId, childId, "Order", "OrderItem");

// Query relationships
var children = await dataContext.Children(parentId);
var parent = await dataContext.Parent(childId);
```

## Cross-Component Queries
```csharp
var results = await dataContext.Query()
    .WithAll<OrderComponent>(o => o.Status == "PENDING")
    .WithAll<CustomerComponent>(c => c.IsActive)
    .ToEntityComponents();
```

## Common Patterns
- Use `TryCommit` for graceful concurrency handling
- Track `OwnerEntityId`, not component `Id` in tests
- Clean up child entities before parents
- Use `IVersionedComponent` for critical data

## Anti-patterns to Avoid
- ❌ Direct component references (use LinkRelationship)
- ❌ Business logic in Functions
- ❌ Custom result objects from handlers
- ❌ Handlers accepting individual field parameters
- ❌ Handlers orchestrating other handlers