# 🔧 C# Project Change Request Template

## 🧾 Metadata

- **Change Request Title**: `Real-time Event Subscriptions for Jarvis Handler Communication`
- **Author**: `Jarvis Core Team`
- **Date Created**: `2024-12-19`
- **Status**: `Draft`
- **Target Branch/Environment**: `develop`
- **Related Tickets/References**:
  - `Handler architecture refactor`
  - `Cross-handler communication requirements`
  - `Real-time integration`

---

## 🎯 Objective

**Enable handlers to communicate via Real-time subscriptions to database table changes and refactor component entity identification.**  
> This change allows handlers to subscribe to Real-time notifications when components are updated, enabling reactive cross-handler communication without manual event publishing. Additionally, this refactors components to use only `OwnerEntityId` for entity identification, removing the confusing `EntityId` field that is not used.

---

## 📦 Scope of Change

### 1. **Affected Components/Namespaces**

List the primary components and namespaces that will be affected:
- `core.jarvis.Data.ComponentHandler<TComponent>`
- `core.jarvis.Data.ComponentHandlerRegistry`
- `core.jarvis.ServiceCollectionExtensions`
- `core.jarvis.Data.IComponent` (entity identification refactor)
- All component implementations (remove EntityId field)
- All handler implementations that need cross-handler communication

### 2. **New Classes / Interfaces (if any)**

Specify class/interface names and brief description:
- `IEventHandler<TComponent>`: Interface for handlers that can subscribe to Real-time component changes
- `EventSubscriptionManager`: Manages Real-time subscriptions per entity and component type
- `IComponentSubscription`: Abstraction for managing individual Real-time subscriptions

### 3. **Refactored Classes / Interfaces**

Component entity identification refactor:
- `IComponent`: Remove EntityId property, keep only OwnerEntityId
- All component implementations: Remove EntityId field/property

### 4. **Modified Classes / Methods**

For each class or method to be changed:

<pre>
<b>// BEFORE - ComponentHandler base class</b>
public abstract class ComponentHandler<TComponent> : IComponentHandler<TComponent>
{
    protected ComponentHandler(
        Guid entityId, 
        IDataContext dataContext,
        ILogger logger)
    {
        // ... existing constructor
    }
}

<b>// AFTER - ComponentHandler with Real-time subscription capability</b>
public abstract class ComponentHandler<TComponent> : IComponentHandler<TComponent>
{
    protected EventSubscriptionManager EventManager { get; }
    
    protected ComponentHandler(
        Guid entityId, 
        IDataContext dataContext,
        EventSubscriptionManager eventManager,
        ILogger logger)
    {
        // ... existing constructor + event manager
        EventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
    }
    
    protected async Task SubscribeToComponent<TOtherComponent>(Action<TOtherComponent> onChanged)
        where TOtherComponent : IComponent
    {
        await EventManager.SubscribeToComponent<TOtherComponent>(EntityId, onChanged);
    }
}
</pre>

<pre>
<b>// BEFORE - Factory delegate registration</b>
services.AddTransient<Func<Guid, TestHandler>>(sp =>
    entityId => new TestHandler(
        entityId,
        sp.GetRequiredService<IDataContext>(),
        sp.GetRequiredService<ILogger<TestHandler>>()));

<b>// AFTER - Factory delegate with event subscription manager</b>
services.AddTransient<Func<Guid, TestHandler>>(sp =>
    entityId => new TestHandler(
        entityId,
        sp.GetRequiredService<IDataContext>(),
        sp.GetRequiredService<EventSubscriptionManager>(),
        sp.GetRequiredService<ILogger<TestHandler>>()));
</pre>

<pre>
<b>// BEFORE - Component with both EntityId and OwnerEntityId</b>
public interface IComponent
{
    Guid Id { get; }
    Guid EntityId { get; set; }
    Guid OwnerEntityId { get; set; }
    // ... other properties
}

public class TestComponent : IComponent
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid OwnerEntityId { get; set; }
    // ... other properties
}

<b>// AFTER - Component with only OwnerEntityId</b>
public interface IComponent
{
    Guid Id { get; }
    Guid OwnerEntityId { get; set; }
    // ... other properties
}

public class TestComponent : IComponent
{
    public Guid Id { get; set; }
    public Guid OwnerEntityId { get; set; }
    // ... other properties
}
</pre>

---

## ✅ Acceptance Criteria

List the **business** and **technical** criteria that must be satisfied:

**Real-time Event Subscriptions:**
- [ ] Handlers can subscribe to Real-time changes on any component table
- [ ] Subscriptions are automatically filtered by OwnerEntityId
- [ ] Multiple handlers can subscribe to the same component changes
- [ ] Real-time subscriptions are properly cleaned up when handlers are disposed
- [ ] Subscription failures provide clear error messages
- [ ] All existing handler implementations continue to work without modification
- [ ] Event handling is async and non-blocking
- [ ] Memory efficient subscription management per entity
- [ ] Integration with existing factory delegate DI pattern
- [ ] Support for subscribing to multiple component types per handler

**Component Entity Identification Refactor:**
- [ ] All components use only OwnerEntityId for entity identification
- [ ] EntityId property is completely removed from IComponent interface
- [ ] All component implementations remove EntityId field/property
- [ ] Real-time subscriptions filter by OwnerEntityId instead of EntityId
- [ ] All handler logic uses OwnerEntityId consistently
- [ ] Database queries filter by OwnerEntityId where previously using EntityId
- [ ] No references to EntityId remain in component-related code

---

## 🧪 Testing Plan

### Unit Tests

**Real-time Event Subscriptions:**
- `EventSubscriptionManager_Subscribe_CreatesCorrectChannel()`
- `EventSubscriptionManager_Subscribe_FiltersToOwnerEntityId()`
- `EventSubscriptionManager_Unsubscribe_CleansUpChannel()`
- `ComponentHandler_SubscribeToComponent_CallsEventManager()`
- `ComponentHandler_MultipleSubscriptions_HandlesCorrectly()`

**Component Entity Identification:**
- `Component_WithoutEntityId_ShouldOnlyHaveOwnerEntityId()`
- `ComponentHandler_UsesOwnerEntityId_ForFiltering()`
- `DataContext_QueriesUseOwnerEntityId_NotEntityId()`

### Integration Tests

**Real-time Event Subscriptions:**
- `HandlerRegistry_WithEventCapableHandler_ShouldResolveWithEventManager()`
- `RealTimeSubscription_ComponentUpdate_ShouldNotifySubscribedHandlers()`
- `RealTimeSubscription_MultipleHandlers_ShouldNotifyAll()`
- `RealTimeSubscription_OwnerEntityFiltering_ShouldOnlyNotifyCorrectEntity()`
- `RealTimeSubscription_Cleanup_ShouldUnsubscribeOnDispose()`

**Component Entity Identification:**
- `ComponentCRUD_WithOwnerEntityId_ShouldFilterCorrectly()`
- `HandlerOperations_UseOwnerEntityId_ForAllQueries()`
- `DatabaseSchema_EntityIdRemoved_OnlyOwnerEntityIdExists()`

### Manual Tests (If Required)
- Verify Real-time subscription performance with multiple handlers
- Test subscription cleanup when handlers are disposed
- Validate entity filtering works correctly across different entities

---

## 📚 Data Changes

> Component entity identification refactor may require database schema changes.

- **New Tables**: None
- **Altered Tables**: All component tables (remove EntityId column if it exists)
- **Migrations**: Remove EntityId columns from component tables, ensure OwnerEntityId exists and is indexed

---

## 🔐 Security & AuthZ Impact

> No security or authorization changes. Real-time subscriptions use existing database security.

- Real-time subscriptions inherit existing database RLS policies
- No new permission boundaries introduced
- Entity filtering prevents cross-entity data access
- Subscription access controlled by existing authentication

---

## ⏱️ Performance Considerations

> Real-time subscriptions should be efficient and not impact handler performance.

- Real-time subscriptions add minimal memory overhead per entity
- WebSocket connections are reused across multiple subscriptions
- Entity filtering happens at the subscription level, not application level
- Subscription cleanup prevents memory leaks
- Non-blocking async event handling

---

## 🔄 Backward Compatibility

> **BREAKING CHANGE**: Component EntityId removal is a breaking change requiring migration.

**Real-time Event Subscriptions (Non-breaking):**
- Existing `ComponentHandler<TComponent>` constructors remain valid
- New constructor overload adds `EventSubscriptionManager` parameter
- Handlers not using Real-time subscriptions continue to work unchanged
- Factory delegate pattern enhanced but not broken
- Real-time capabilities are opt-in, not required

**Component Entity Identification (Breaking):**
- **BREAKING**: EntityId property removed from IComponent interface
- **BREAKING**: All component implementations must remove EntityId field
- **BREAKING**: Database queries using EntityId must be updated to use OwnerEntityId
- **BREAKING**: Any code referencing component.EntityId will fail to compile
- Migration required for existing databases with EntityId columns

---

## 🧠 AI Notes for Code Generation

### 1. **Constraints**
- All handlers must implement `IComponentHandler<TComponent>`
- Real-time subscriptions must filter by OwnerEntityId automatically
- No hardcoded dependencies in handler constructors
- All dependencies must be resolved via DI container
- Subscription cleanup must be handled properly to prevent memory leaks

### 2. **Naming Conventions**
- PascalCase for handler classes (e.g., `InvoiceHandler`)
- Subscription method: `SubscribeToComponent<TComponent>(Action<TComponent> onChanged)`
- Event handler interface: `IEventHandler<TComponent>`
- Async methods must end with `Async`
- Component table names follow convention: `{ComponentName}s` (e.g., `invoices`)

### 3. **Architecture Pattern**
- Maintain Clean Architecture principles
- Handler registry remains internal implementation detail
- Use dependency injection for all handler dependencies
- Support both factory and standard registration patterns
- Real-time subscriptions should be entity-scoped for isolation
- Components must use only OwnerEntityId for entity identification
- All EntityId references must be removed from component-related code

---

## 🔄 Rollback Strategy

> **WARNING**: EntityId removal is difficult to rollback due to breaking changes.

**Real-time Event Subscriptions (Easy rollback):**
- Revert `ComponentHandler<TComponent>` base class to original implementation
- Remove `EventSubscriptionManager` dependency from factory delegate registrations
- Remove Real-time subscription services and related classes
- All existing tests should pass with original implementation

**Component Entity Identification (Difficult rollback):**
- **DIFFICULT**: Re-add EntityId property to IComponent interface
- **DIFFICULT**: Re-add EntityId field to all component implementations
- **DIFFICULT**: Restore database EntityId columns via migration
- **DIFFICULT**: Update all code to use EntityId where OwnerEntityId was changed
- **RISK**: Data loss if EntityId columns are dropped and need to be restored

---

## 📎 Related Files to Update

- [ ] `/core.jarvis/Data/ComponentHandler.cs`
- [ ] `/core.jarvis/ServiceCollectionExtensions.cs`
- [ ] `/core.jarvis.tests/Helpers/IntegrationTestBase.cs`
- [ ] `/docs/architecture/handler-patterns.md`
- [ ] `/docs/guides/handler-development.md`

---

## 🧠 Final Checklist for AI Execution

**Real-time Event Subscriptions:**
- [ ] Update `ComponentHandler<TComponent>` base class with Real-time subscription capability
- [ ] Create `IEventHandler<TComponent>` interface for Real-time event subscription
- [ ] Implement `EventSubscriptionManager` for managing Real-time subscriptions
- [ ] Update factory delegate registration patterns to include `EventSubscriptionManager`
- [ ] Add comprehensive error handling with clear messages for subscription failures
- [ ] Include XML documentation for all new methods and interfaces
- [ ] Write integration tests covering Real-time subscription scenarios
- [ ] Support proper cleanup of Real-time subscriptions
- [ ] Maintain handler instance isolation per entity

**Component Entity Identification Refactor:**
- [ ] Remove EntityId property from IComponent interface
- [ ] Remove EntityId field from all component implementations
- [ ] Update all component queries to use OwnerEntityId instead of EntityId
- [ ] Update Real-time subscription filtering to use OwnerEntityId
- [ ] Create database migration to remove EntityId columns
- [ ] Update all handler logic to use OwnerEntityId consistently
- [ ] Ensure all tests pass with EntityId removal
- [ ] Update documentation to reflect OwnerEntityId usage
- [ ] Verify no EntityId references remain in codebase

---

## 📋 Example Usage (Framework Demonstration)

All examples use test components to demonstrate framework capabilities without implementing business logic:

### Component Update Triggering Real-time Events
```csharp
public class TestHandler : ComponentHandler<TestComponent>
{
    public async Task PerformTestOperation()
    {
        // Business logic here
        var component = await GetRequired();
        component.Status = "PROCESSED";
        component.ProcessedAt = DateTime.UtcNow;
        
        // Save triggers Supabase Real-time notifications automatically
        await DataContext.SaveComponent(component);
        
        // All subscribed handlers will be notified via Real-time
    }
}
```

### Handler Subscribing to Real-time Component Changes
```csharp
public class RelatedTestHandler : ComponentHandler<RelatedTestComponent>, 
    IEventHandler<TestComponent>
{
    public async Task InitializeAsync()
    {
        // Subscribe to Real-time changes for TestComponent with this OwnerEntityId
        await SubscribeToComponent<TestComponent>(OnTestComponentChanged);
    }
    
    private async Task OnTestComponentChanged(TestComponent component)
    {
        if (component.Status == "PROCESSED")
        {
            // React to test component processing
            var related = await GetRequired();
            related.RelatedStatus = "UPDATED";
            await DataContext.SaveComponent(related);
        }
    }
    
    public async Task DisposeAsync()
    {
        // Cleanup subscriptions
        await EventManager.UnsubscribeFromComponent<TestComponent>(EntityId);
    }
}
```

### Multiple Handlers Reacting to Same Component
```csharp
public class AuditTestHandler : ComponentHandler<AuditTestComponent>,
    IEventHandler<TestComponent>
{
    public async Task InitializeAsync()
    {
        // Multiple handlers can subscribe to the same component changes
        await SubscribeToComponent<TestComponent>(OnTestComponentChanged);
    }
    
    private async Task OnTestComponentChanged(TestComponent component)
    {
        // Create audit log for any test component changes
        var audit = CreateComponent<AuditTestComponent>();
        audit.Action = $"TestComponent status changed to {component.Status}";
        audit.Timestamp = DateTime.UtcNow;
        
        await DataContext.SaveComponent(audit);
    }
}
```

---

> **NOTE**: This change request focuses on Supabase Real-time subscriptions for handler communication. No database triggers are used - handlers react to Real-time notifications when components are updated through normal DataContext operations. 