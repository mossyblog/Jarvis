# Migration Guide

Step-by-step guidance for migrating to Jarvis ECS from existing systems.

## 📋 Migration Paths

### From Traditional N-Tier Architecture

If you're coming from a typical Service/Repository pattern:

1. **Map Services to Handlers**
   ```csharp
   // Before: Service class
   public class OrderService
   {
       public async Task<Order> CreateOrder(OrderDto dto) { }
   }
   
   // After: Handler
   public class OrderHandler : ComponentHandler<OrderComponent>
   {
       public async Task<OrderComponent> CreateOrder(...) { }
   }
   ```

2. **Convert Entities to Components**
   ```csharp
   // Before: Entity with logic
   public class Order
   {
       public void CalculateTotal() { }
   }
   
   // After: Pure data component
   public record OrderComponent : IComponent, IVersionedComponent
   {
       public Guid Id { get; init; }
       public Guid OwnerEntityId { get; set; }
       public int TotalAmountCents { get; init; }
       public int? Version { get; set; }
   }
   ```

3. **Extract Business Logic to Handlers**
   - Move validation to handler methods
   - Move calculations to handler logic
   - Keep components as pure data

### From WorkingSet Architecture

See [Migrating from WorkingSet](from-workingset.md) for detailed steps.

Key differences:
- WorkingSet → IDataContext
- Aggregate operations → Handler methods
- Direct DB access → Component handlers

### From Microservices

1. **Consolidate Related Services**
   - Group related microservices into handlers
   - Use components for shared data models
   - Maintain service boundaries via handler organization

2. **Replace Service Communication**
   ```csharp
   // Before: HTTP calls between services
   var order = await httpClient.GetAsync<Order>("/orders/123");
   
   // After: Direct handler access
   var order = await dataContext.For<OrderHandler>(orderId).Get();
   ```

## 🔄 Version Upgrades

### v1.x to v2.0

See [Breaking Changes](breaking-changes.md) for complete list.

Major changes:
- `IWorkingSet` → `IDataContext`
- Handler registration changes
- New transaction API

Migration script:
```bash
# Update package references
dotnet remove package core.jarvis --version 1.*
dotnet add package core.jarvis --version 2.0.0

# Update namespaces
find . -name "*.cs" -exec sed -i 's/IWorkingSet/IDataContext/g' {} \;
```

## 🏗️ Migration Strategy

### 1. Incremental Migration (Recommended)

Migrate one feature at a time:

```csharp
// Phase 1: Create handler alongside existing service
public class OrderHandler : ComponentHandler<OrderComponent>
{
    private readonly ILegacyOrderService _legacy;
    
    public async Task<OrderComponent> Get()
    {
        // Wrap legacy service temporarily
        var legacyOrder = await _legacy.GetOrder(OwnerEntityId);
        return MapToComponent(legacyOrder);
    }
}
```

### 2. Parallel Run

Run both systems side-by-side:
- New features use Jarvis
- Existing features remain unchanged
- Gradually migrate as you touch code

### 3. Big Bang (Not Recommended)

Complete rewrite - only for small systems.

## ✅ Migration Checklist

### Pre-Migration
- [ ] Inventory existing entities and services
- [ ] Map to components and handlers
- [ ] Plan data migration strategy
- [ ] Set up test environment

### During Migration
- [ ] Create components for each entity
- [ ] Implement handlers for business logic
- [ ] Migrate data to new schema
- [ ] Update dependency injection
- [ ] Rewrite tests

### Post-Migration
- [ ] Verify all functionality
- [ ] Performance testing
- [ ] Update documentation
- [ ] Remove legacy code

## 🛠️ Tools and Scripts

### Entity to Component Converter
```csharp
public static class MigrationHelper
{
    public static T ConvertToComponent<T>(object entity) where T : IComponent, new()
    {
        var component = new T();
        // Map properties
        foreach (var prop in typeof(T).GetProperties())
        {
            var entityProp = entity.GetType().GetProperty(prop.Name);
            if (entityProp != null)
            {
                prop.SetValue(component, entityProp.GetValue(entity));
            }
        }
        return component;
    }
}
```

### Database Migration
```sql
-- Example: Convert entity table to component format
ALTER TABLE orders RENAME TO order_component;
ALTER TABLE order_component 
  ADD COLUMN owner_entity_id UUID NOT NULL,
  ADD COLUMN created_at TIMESTAMPTZ DEFAULT NOW(),
  ADD COLUMN updated_at TIMESTAMPTZ DEFAULT NOW();
```

## 🚨 Common Pitfalls

1. **Don't Mix Patterns**
   - Either use handlers or services, not both
   - Avoid hybrid approaches

2. **Don't Add Logic to Components**
   - Components are data only
   - All logic goes in handlers

3. **Don't Skip Tests**
   - Rewrite tests for new architecture
   - Test both old and new during migration

## 📚 Resources

- [Architecture Overview](../architecture/ecs-principles.md)
- [Handler Development](../guides/handler-development.md)
- [Breaking Changes](breaking-changes.md)
- [WorkingSet Migration](from-workingset.md)

## 💬 Need Help?

- Review [Examples](../getting-started/examples/)
- Check [GitHub Discussions](https://github.com/yourusername/jarvis/discussions)
- Contact support for migration assistance 