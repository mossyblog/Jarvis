using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using core.jarvis.Systems;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Linq;

namespace core.jarvis.tests.Integration.SystemIntegrationTests;

/// <summary>
/// Integration tests for HandlerSystem implementation.
/// Tests handler orchestration through the System layer.
/// </summary>
public class HandlerSystemTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify HandlerSystem can execute handler methods that return components.
    /// PURPOSE: Ensure the System layer properly orchestrates handler execution.
    /// BUSINESS CONTEXT: System layer abstracts handler execution from Azure Functions.
    /// WHY IMPORTANT: Validates that business logic can be tested without HTTP context.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms System layer properly delegates to handlers.
    /// FUTURE RESILIENCE: Ensures System pattern works for component retrieval operations.
    /// </summary>
    [Fact]
    public async Task HandlerSystem_ExecuteHandler_ReturnsComponent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "System Test",
            Status = "ACTIVE"
        };
        await TestDataContext().Commit(testComponent);
        
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        
        // Act
        var result = await system.ExecuteHandler<TestHandler, TestComponent>(
            entityId,
            handler => handler.Get());
        
        // Assert
        result.ShouldNotBeNull();
        result.OwnerEntityId.ShouldBe(entityId);
        result.Name.ShouldBe("System Test");
        result.Status.ShouldBe("ACTIVE");
    }
    
    /// <summary>
    /// INTENT: Verify HandlerSystem can execute handler methods with no return value.
    /// PURPOSE: Ensure System layer handles void operations correctly.
    /// BUSINESS CONTEXT: Many handler operations modify state without returning values.
    /// WHY IMPORTANT: Validates that command-style operations work through System layer.
    /// ARCHITECTURAL SIGNIFICANCE: Tests System's ability to handle different method signatures.
    /// FUTURE RESILIENCE: Ensures pattern works for state-changing operations.
    /// </summary>
    [Fact]
    public async Task HandlerSystem_ExecuteHandler_VoidOperation_CompletesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Void Test",
            Status = "INACTIVE"
        };
        await TestDataContext().Commit(testComponent);
        
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        
        // Act
        await system.ExecuteHandler<TestHandler>(
            entityId,
            handler => handler.Activate());
        
        // Assert - verify state was changed
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.OwnerEntityId == entityId);
        var results = await query.ToEntityComponents();
        results.Count.ShouldBe(1);
        var updated = results.Values.First().Get<TestComponent>();
        updated.ShouldNotBeNull();
        updated.Status.ShouldBe("ACTIVE");
    }
    
    /// <summary>
    /// INTENT: Verify HandlerSystem properly propagates exceptions from handlers.
    /// PURPOSE: Ensure error handling works correctly through System layer.
    /// BUSINESS CONTEXT: Business rule violations must bubble up to calling code.
    /// WHY IMPORTANT: Proper error propagation is critical for application reliability.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms System doesn't swallow handler exceptions.
    /// FUTURE RESILIENCE: Ensures error handling patterns remain consistent.
    /// </summary>
    [Fact]
    public async Task HandlerSystem_ExecuteHandler_PropagatesExceptions()
    {
        // Arrange
        var entityId = Guid.NewGuid(); // Non-existent entity
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        
        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await system.ExecuteHandler<TestHandler, TestComponent>(
                entityId,
                handler => handler.Get());
        });
    }
    
    /// <summary>
    /// INTENT: Verify HandlerSystem can create new components without handlers.
    /// PURPOSE: Test the CreateComponent method for simple entity creation.
    /// BUSINESS CONTEXT: Component creation is a common operation that doesn't require handler logic.
    /// WHY IMPORTANT: Validates that simple CRUD operations don't need custom handlers.
    /// ARCHITECTURAL SIGNIFICANCE: Shows System can handle both handler and non-handler operations.
    /// FUTURE RESILIENCE: Ensures component creation pattern remains simple.
    /// </summary>
    [Fact]
    public async Task HandlerSystem_CreateComponent_CreatesNewEntity()
    {
        // Arrange
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        var component = new TestComponent
        {
            Name = "Created Test",
            Status = "NEW"
        };
        
        // Act
        var entityId = await system.CreateComponent(component);
        TrackEntity(entityId);
        
        // Assert
        entityId.ShouldNotBe(Guid.Empty);
        
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.OwnerEntityId == entityId);
        var results = await query.ToEntityComponents();
        results.Count.ShouldBe(1);
        var created = results.Values.First().Get<TestComponent>();
        created.ShouldNotBeNull();
        created.Name.ShouldBe("Created Test");
        created.Status.ShouldBe("NEW");
        created.OwnerEntityId.ShouldBe(entityId);
    }
    
    /// <summary>
    /// INTENT: Verify HandlerSystem works with domain-specific handler methods.
    /// PURPOSE: Test that custom business methods can be called through System.
    /// BUSINESS CONTEXT: Handlers often have domain-specific methods beyond basic CRUD.
    /// WHY IMPORTANT: Validates the pattern works for real business operations.
    /// ARCHITECTURAL SIGNIFICANCE: Shows how domain logic integrates with System layer.
    /// FUTURE RESILIENCE: Ensures pattern scales to complex business operations.
    /// </summary>
    [Fact]
    public async Task HandlerSystem_ExecuteHandler_CallsDomainSpecificMethods()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Domain Test",
            Status = "ACTIVE"
        };
        await TestDataContext().Commit(testComponent);
        
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        
        // Act - Call a domain-specific method (Deactivate with reason)
        await system.ExecuteHandler<TestHandler>(
            entityId,
            handler => handler.Deactivate("System test deactivation"));
        
        // Assert - verify persistence
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.OwnerEntityId == entityId);
        var results = await query.ToEntityComponents();
        results.Count.ShouldBe(1);
        var updated = results.Values.First().Get<TestComponent>();
        updated.ShouldNotBeNull();
        updated.Status.ShouldBe("INACTIVE");
    }
}