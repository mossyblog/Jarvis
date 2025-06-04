# 🔧 C# Project Change Request Template

## 🧾 Metadata

- **Change Request Title**: `Handler Instantiation via DI Factory Delegates`
- **Author**: `Jarvis Core Team`
- **Date Created**: `2024-12-19`
- **Status**: `Draft`
- **Target Branch/Environment**: `develop`
- **Related Tickets/References**:
  - `ComponentHandlerRegistry.cs refactor`
  - `Integration test failures with ActivatorUtilities`

---

## 🎯 Objective

**Replace `ActivatorUtilities.CreateInstance` with DI factory delegate pattern for handler instantiation.**  
> This change eliminates brittle reflection-based handler construction and enables clean, type-safe creation of handlers that require runtime parameters (such as `entityId`). This is a refactor to improve maintainability, testability, and align with .NET DI best practices.

---

## 📦 Scope of Change

### 1. **Affected Components/Namespaces**

List the primary components and namespaces that will be affected:
- `core.jarvis.Data.ComponentHandlerRegistry`
- `core.jarvis.Data.IComponentHandlerRegistry`
- `core.jarvis.tests.Helpers.IntegrationTestBase`
- All handler implementations requiring runtime parameters

### 2. **New Classes / Interfaces (if any)**

No new classes or interfaces required. This change uses existing .NET DI factory delegate patterns.

### 3. **Modified Classes / Methods**

For each class or method to be changed:

<pre>
<b>// BEFORE - ComponentHandlerRegistry.CreateHandler</b>
private IComponentHandler CreateHandler(Type handlerType, Guid entityId)
{
    var client = _serviceProvider.GetRequiredService<Supabase.Client>();
    var handler = ActivatorUtilities.CreateInstance(
        _serviceProvider,
        handlerType,
        entityId,
        client);
    return (IComponentHandler)handler;
}

<b>// AFTER - ComponentHandlerRegistry.CreateHandler</b>
private IComponentHandler CreateHandler(Type handlerType, Guid entityId)
{
    // Try factory pattern first (for handlers needing runtime parameters)
    var factoryType = typeof(Func<,>).MakeGenericType(typeof(Guid), handlerType);
    var factory = _serviceProvider.GetService(factoryType);
    
    if (factory != null)
    {
        var handler = ((Delegate)factory).DynamicInvoke(entityId);
        if (handler is IComponentHandler componentHandler)
            return componentHandler;
    }

    // Fallback to standard DI resolution
    var standardHandler = _serviceProvider.GetService(handlerType);
    if (standardHandler is IComponentHandler standardComponentHandler)
        return standardComponentHandler;

    throw new ComponentNotFoundException(
        $"No factory or standard registration found for handler {handlerType.Name}");
}
</pre>

<pre>
<b>// BEFORE - Handler Registration (IntegrationTestBase)</b>
_registry.Register<TestComponent, TestHandler>();
// Handler created via ActivatorUtilities with hardcoded constructor signature

<b>// AFTER - Handler Registration (IntegrationTestBase)</b>
services.AddTransient<Func<Guid, TestHandler>>(sp =>
    id => new TestHandler(
        id, 
        sp.GetRequiredService<Supabase.Client>(),
        sp.GetRequiredService<ILogger<TestHandler>>()));
_registry.Register<TestComponent, TestHandler>();
</pre>

---

## ✅ Acceptance Criteria

List the **business** and **technical** criteria that must be satisfied:
- [ ] Handlers can be instantiated with runtime parameters (entityId) via factory delegates
- [ ] Backward compatibility maintained for handlers not requiring runtime parameters
- [ ] All existing integration tests continue to pass
- [ ] Clear error messages when handler registration is missing
- [ ] Handler instances remain isolated per entity
- [ ] Support for async initialization patterns
- [ ] No reflection or dynamic constructor logic in user code
- [ ] Improved testability with easier dependency mocking

---

## 🧪 Testing Plan

### Unit Tests
- `HandlerRegistry_WithFactoryRegistration_ShouldResolveHandlerWithEntityId()`
- `HandlerRegistry_WithStandardRegistration_ShouldResolveHandler()`
- `HandlerRegistry_WithMissingRegistration_ShouldThrowComponentNotFoundException()`
- `HandlerRegistry_MultipleEntities_ShouldCreateIsolatedInstances()`

### Integration Tests
- `HandlerRegistry_WithAsyncInitialization_ShouldSupportAsyncSetup()`
- `HandlerRegistry_FactoryWithDependencies_ShouldInjectAllServices()`
- All existing handler integration tests must continue to pass

### Manual Tests (If Required)
- Verify handler creation performance compared to ActivatorUtilities
- Test error messages for misconfigured handlers during development

---

## 📚 Data Changes

> No database schema changes required. This is purely a code refactor.

- **New Tables**: None
- **Altered Tables**: None
- **Migrations**: None

---

## 🔐 Security & AuthZ Impact

> No security or authorization changes. Handler instantiation remains internal to the framework.

- No new permission boundaries
- No changes to authentication or authorization logic
- Handler access patterns remain unchanged

---

## ⏱️ Performance Considerations

> Potential performance improvements by eliminating reflection.

- Eliminates reflection overhead from `ActivatorUtilities.CreateInstance`
- Factory delegate invocation is faster than dynamic constructor resolution
- Memory usage remains similar (one handler instance per entity)
- No impact on concurrency behavior

---

## 🔄 Backward Compatibility

> Full backward compatibility maintained through dual registration support.

- Maintains support for handlers not requiring runtime parameters via standard DI
- Existing handler interfaces and contracts unchanged
- No breaking changes to public APIs
- Migration can be done incrementally per handler type

---

## 🧠 AI Notes for Code Generation

### 1. **Constraints**
- All handlers must implement `IComponentHandler<TComponent>`
- Factory delegates must follow pattern: `Func<Guid, THandler>`
- No hardcoded dependencies in handler constructors
- All dependencies must be resolved via DI container

### 2. **Naming Conventions**
- PascalCase for handler classes (e.g., `InvoiceHandler`)
- Factory registration follows pattern: `AddTransient<Func<Guid, HandlerType>>`
- Async initialization methods must end with `Async`
- Handler methods follow existing naming conventions

### 3. **Architecture Pattern**
- Maintain Clean Architecture principles
- Handler registry remains internal implementation detail
- Use dependency injection for all handler dependencies
- Support both factory and standard registration patterns

---

## 🔄 Rollback Strategy

> If factory delegate pattern causes issues, revert to ActivatorUtilities approach.

- Revert `ComponentHandlerRegistry.CreateHandler` method to original implementation
- Remove factory delegate registrations from `IntegrationTestBase`
- Restore original handler constructor requirements
- All existing tests should pass with original implementation

---

## 📎 Related Files to Update

- [ ] `/core.jarvis/Data/ComponentHandlerRegistry.cs`
- [ ] `/core.jarvis.tests/Helpers/IntegrationTestBase.cs`
- [ ] `/docs/architecture/handler-patterns.md` (if exists)
- [ ] `/docs/getting-started/handler-development.md` (if exists)
- [ ] All test files using handler registration patterns

---

## 🧠 Final Checklist for AI Execution

- [ ] Update `ComponentHandlerRegistry.CreateHandler` with factory-aware implementation
- [ ] Add fallback to standard DI resolution for backward compatibility
- [ ] Update test handler registrations in `IntegrationTestBase`
- [ ] Ensure all handler constructors support dependency injection
- [ ] Add comprehensive error handling with clear messages
- [ ] Include XML documentation for all modified methods
- [ ] Write integration tests covering both factory and standard registration patterns
- [ ] Validate all existing tests continue to pass
- [ ] Support async initialization pattern for complex handlers
- [ ] Maintain handler instance isolation per entity

---

## 📋 Test Scenarios (Compliance with Guidelines.md)

All test scenarios must use `IntegrationTestBase`, `TestDataContext()`, and Shouldly assertions:

### Factory Registration and Resolution
```csharp
/// <summary>
/// INTENT: Verifies that handlers registered with factory delegates can be resolved correctly.
/// PURPOSE: Ensures the DI factory pattern works for handlers requiring runtime parameters.
/// BUSINESS CONTEXT: Supports business handlers that need entity-specific initialization.
/// WHY IMPORTANT: Core functionality for entity-scoped handler operations.
/// ARCHITECTURAL SIGNIFICANCE: Validates the factory delegate registration pattern.
/// FUTURE RESILIENCE: Protects against regressions in handler factory resolution.
/// </summary>
[Fact]
public void HandlerRegistry_WithFactoryRegistration_ShouldResolveHandlerWithEntityId()
{
    // Arrange
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    // Act
    var handler = TestDataContext().For<TestHandler>(entityId);

    // Assert
    handler.ShouldNotBeNull();
    handler.ShouldBeOfType<TestHandler>();
    handler.EntityId.ShouldBe(entityId);
}
```

### Standard DI Registration Fallback
```csharp
/// <summary>
/// INTENT: Verifies that handlers registered without factories can still be resolved.
/// PURPOSE: Ensures backward compatibility with handlers not requiring runtime parameters.
/// BUSINESS CONTEXT: Supports system-level handlers that don't need entity context.
/// WHY IMPORTANT: Maintains compatibility with existing handler patterns.
/// ARCHITECTURAL SIGNIFICANCE: Validates dual registration pattern support.
/// FUTURE RESILIENCE: Protects against breaking changes for simple handlers.
/// </summary>
[Fact]
public void HandlerRegistry_WithStandardRegistration_ShouldResolveHandler()
{
    // Arrange
    var entityId = Guid.NewGuid();

    // Act
    var handler = TestDataContext().For<SystemConfigHandler>(entityId);

    // Assert
    handler.ShouldNotBeNull();
    handler.ShouldBeOfType<SystemConfigHandler>();
}
```

### Error Handling for Missing Registration
```csharp
/// <summary>
/// INTENT: Verifies appropriate error when no factory or standard registration exists.
/// PURPOSE: Ensures clear error messages for misconfigured handlers.
/// BUSINESS CONTEXT: Helps developers identify registration issues during development.
/// WHY IMPORTANT: Provides actionable error messages for troubleshooting.
/// ARCHITECTURAL SIGNIFICANCE: Validates error handling in the registry pattern.
/// FUTURE RESILIENCE: Protects against silent failures in handler resolution.
/// </summary>
[Fact]
public void HandlerRegistry_WithMissingRegistration_ShouldThrowComponentNotFoundException()
{
    // Arrange
    var entityId = Guid.NewGuid();

    // Act & Assert
    var exception = Should.Throw<ComponentNotFoundException>(() => 
        TestDataContext().For<UnregisteredHandler>(entityId));
    
    exception.Message.ShouldContain("No factory or standard registration found");
    exception.Message.ShouldContain("UnregisteredHandler");
}
```

---

> **NOTE**: This change request eliminates brittle reflection-based handler construction while maintaining full backward compatibility. The factory delegate pattern provides clean, type-safe handler instantiation with proper dependency injection support. 