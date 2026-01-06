using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.HandlerIntegrationTests;

/// <summary>
/// Integration tests for handler registration, resolution, and instantiation.
/// Tests the full chain from DataContext through handler registry to handler instances.
/// </summary>
public class HandlerRegistrationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verifies handler registration and resolution through DataContext.
    /// PURPOSE: Ensures robust component handler resolution.
    /// BUSINESS CONTEXT: Supports plugin registration and resolution.
    /// WHY IMPORTANT: All component access depends on this workflow.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms core handler resolution pattern.
    /// FUTURE RESILIENCE: Protects against regressions in handler resolution.
    /// </summary>
    [Fact]
    public void DataContext_ForT_ShouldResolveRegisteredHandler()
    {
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<TestHandler>(entityId);
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestHandler>();
    }

    /// <summary>
    /// INTENT: Verifies handler resolution by component type through DataContext.
    /// PURPOSE: Ensures robust component handler resolution by type.
    /// BUSINESS CONTEXT: Supports dynamic component type resolution.
    /// WHY IMPORTANT: Runtime component resolution depends on this pattern.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms type-based handler resolution.
    /// FUTURE RESILIENCE: Protects against regressions in type resolution.
    /// </summary>
    [Fact]
    public void DataContext_ForByType_ShouldResolveRegisteredHandler()
    {
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For(typeof(TestComponent), entityId);
        handler.ShouldNotBeNull();
        handler.ShouldBeAssignableTo<IComponentHandler<TestComponent>>();
    }

    /// <summary>
    /// INTENT: Verifies that resolution fails for unregistered handlers.
    /// PURPOSE: Ensures robust error handling for missing handlers.
    /// BUSINESS CONTEXT: Supports clear error messages for misconfiguration.
    /// WHY IMPORTANT: Helps diagnose plugin registration issues.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms handler resolution boundaries.
    /// FUTURE RESILIENCE: Protects against silent failures with missing handlers.
    /// </summary>
    [Fact]
    public void DataContext_ForT_ShouldThrowForUnregisteredHandler()
    {
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        Should.Throw<ComponentNotFoundException>(() => 
            TestDataContext().For<UnregisteredHandler>(entityId));
    }

    /// <summary>
    /// INTENT: Verifies that handler instances are not shared between resolutions.
    /// PURPOSE: Ensures stateless handler pattern is enforced.
    /// BUSINESS CONTEXT: Prevents state leakage between operations.
    /// WHY IMPORTANT: Supports concurrent operations on same entity.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms per-resolution handler instancing.
    /// FUTURE RESILIENCE: Protects against singleton handler patterns.
    /// </summary>
    [Fact]
    public void DataContext_ForT_ShouldCreateNewInstanceEachTime()
    {
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler1 = TestDataContext().For<TestHandler>(entityId);
        var handler2 = TestDataContext().For<TestHandler>(entityId);
        handler1.ShouldNotBeNull();
        handler2.ShouldNotBeNull();
        handler1.ShouldNotBeSameAs(handler2);
    }

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
        handler.OwnerEntityId.ShouldBe(entityId);
    }

    /// <summary>
    /// INTENT: Verifies that handlers registered with factory delegates pass correct entityId.
    /// PURPOSE: Ensures factory delegates properly propagate runtime parameters.
    /// BUSINESS CONTEXT: Supports entity-specific handler operations.
    /// WHY IMPORTANT: Entity isolation is critical for data integrity.
    /// ARCHITECTURAL SIGNIFICANCE: Validates runtime parameter passing in factory pattern.
    /// FUTURE RESILIENCE: Protects against entityId mismatches in handler operations.
    /// </summary>
    [Fact]
    public async Task HandlerRegistry_WithFactoryRegistration_ShouldUseCorrectEntityId()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act - Create a component via handler
        await handler.Create("TestEntity", "ACTIVE");

        // Assert - Verify the component was created with correct entityId
        var component = await handler.Get();
        component.ShouldNotBeNull();
        component.OwnerEntityId.ShouldBe(entityId);
        component.Name.ShouldBe("TestEntity");
    }

    /// <summary>
    /// INTENT: Verifies multiple entity handlers are properly isolated with factory pattern.
    /// PURPOSE: Ensures each entity gets its own handler instance with correct entityId.
    /// BUSINESS CONTEXT: Supports concurrent operations on multiple entities.
    /// WHY IMPORTANT: Entity isolation prevents cross-entity data corruption.
    /// ARCHITECTURAL SIGNIFICANCE: Validates multi-entity support in factory pattern.
    /// FUTURE RESILIENCE: Protects against entity mixing in concurrent scenarios.
    /// </summary>
    [Fact]
    public async Task HandlerRegistry_WithFactoryRegistration_ShouldIsolateMultipleEntities()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        // Act
        var handler1 = TestDataContext().For<TestHandler>(entityId1);
        var handler2 = TestDataContext().For<TestHandler>(entityId2);

        await handler1.Create("Entity1", "ACTIVE");
        await handler2.Create("Entity2", "INACTIVE");

        // Assert
        handler1.OwnerEntityId.ShouldBe(entityId1);
        handler2.OwnerEntityId.ShouldBe(entityId2);
        handler1.ShouldNotBe(handler2);

        var component1 = await handler1.Get();
        var component2 = await handler2.Get();

        component1.OwnerEntityId.ShouldBe(entityId1);
        component1.Name.ShouldBe("Entity1");
        component1.Status.ShouldBe("ACTIVE");

        component2.OwnerEntityId.ShouldBe(entityId2);
        component2.Name.ShouldBe("Entity2");
        component2.Status.ShouldBe("INACTIVE");
    }
}

// Add a minimal UnregisteredHandler for the missing handler test
// This handler intentionally does NOT implement IComponentHandler to test unregistered scenario
public class UnregisteredHandler : IComponentHandler
{
    public void InitializeContext(Guid entityId) => throw new NotImplementedException();
    public Task<IComponent> Get() => throw new NotImplementedException();
}