using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.PluginArchitectureTests;

/// <summary>
/// Tests for plugin isolation and security boundaries.
/// Verifies that plugins are properly isolated and cannot interfere with each other.
/// All operations go through TestDataContext to test the complete stack.
/// </summary>
public class PluginIsolationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verifies that handler instances are isolated per entity.
    /// PURPOSE: Ensures plugins cannot share handler state.
    /// BUSINESS CONTEXT: Supports plugin security and isolation.
    /// WHY IMPORTANT: Prevents cross-entity data leakage.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler-per-entity contract.
    /// FUTURE RESILIENCE: Protects against regressions in handler isolation.
    /// </summary>
    [Fact]
    public void Plugins_Should_Have_Isolated_Handler_Instances()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        TrackEntity(entityId1);
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId2);
        
        // Act
        var handler1 = TestDataContext().For<TestHandler>(entityId1);
        var handler2 = TestDataContext().For<TestHandler>(entityId2);
        
        // Assert
        handler1.ShouldNotBeSameAs(handler2);
        handler1.ShouldNotBeNull();
        handler2.ShouldNotBeNull();
    }

    /// <summary>
    /// INTENT: Verifies that plugins cannot access data from other entities.
    /// PURPOSE: Ensures entity data is isolated.
    /// BUSINESS CONTEXT: Supports data privacy and security.
    /// WHY IMPORTANT: Prevents unauthorized data access.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces entity-level data boundaries.
    /// FUTURE RESILIENCE: Protects against regressions in data isolation.
    /// </summary>
    [Fact]
    public async Task Plugins_Should_Not_Access_Other_Entity_Data()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        TrackEntity(entityId1);
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId2);
        
        // Act
        await TestDataContext().For<TestHandler>(entityId1).Create("Entity1 Data", "ACTIVE");
        await TestDataContext().For<TestHandler>(entityId2).Create("Entity2 Data", "ACTIVE");
        var data1 = await TestDataContext().For<TestHandler>(entityId1).Get();
        var data2 = await TestDataContext().For<TestHandler>(entityId2).Get();
        
        // Assert
        data1.ShouldNotBeNull();
        data1.Name.ShouldBe("Entity1 Data");
        data1.OwnerEntityId.ShouldBe(entityId1);
        data2.ShouldNotBeNull();
        data2.Name.ShouldBe("Entity2 Data");
        data2.OwnerEntityId.ShouldBe(entityId2);
        TestDataContext().For<TestHandler>(entityId1).ShouldNotBeNull();
        TestDataContext().For<TestHandler>(entityId2).ShouldNotBeNull();
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId1);
        await TestDataContext().Remove<TestComponent>(entityId2);
    }

    /// <summary>
    /// INTENT: Verifies that DataContext validates handler access.
    /// PURPOSE: Ensures only registered handlers can be resolved.
    /// BUSINESS CONTEXT: Supports plugin registration safety.
    /// WHY IMPORTANT: Prevents runtime errors from missing handlers.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler registration contract.
    /// FUTURE RESILIENCE: Protects against regressions in handler validation.
    /// </summary>
    [Fact]
    public void Plugins_Should_Validate_Through_DataContext()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        // Act & Assert
        Should.Throw<ComponentNotFoundException>(() => 
            TestDataContext().For<NonExistentHandler>(entityId));
    }

    /// <summary>
    /// INTENT: Verifies that handler instances are not shared even for the same entity.
    /// PURPOSE: Ensures handler instantiation is per-request.
    /// BUSINESS CONTEXT: Supports stateless plugin design.
    /// WHY IMPORTANT: Prevents unintended state sharing.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler-per-request contract.
    /// FUTURE RESILIENCE: Protects against regressions in handler instancing.
    /// </summary>
    [Fact]
    public void Plugins_Should_Not_Share_Handler_Instances()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        // Act
        var handler1 = TestDataContext().For<TestHandler>(entityId);
        var handler2 = TestDataContext().For<TestHandler>(entityId);
        // Assert
        handler1.ShouldNotBeSameAs(handler2);
        handler1.ShouldNotBeNull();
        handler2.ShouldNotBeNull();
    }

    /// <summary>
    /// INTENT: Verifies that handler validation is enforced for data creation.
    /// PURPOSE: Ensures invalid data is rejected by handlers.
    /// BUSINESS CONTEXT: Supports data integrity and validation.
    /// WHY IMPORTANT: Prevents invalid data from being persisted.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler validation contract.
    /// FUTURE RESILIENCE: Protects against regressions in validation enforcement.
    /// </summary>
    [Fact]
    public async Task Plugins_Should_Validate_Data_Through_DataContext()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        // Act & Assert
        await Should.ThrowAsync<ValidationException>(async () =>
            await TestDataContext().For<TestHandler>(entityId).Create("", "ACTIVE")); // Empty name
    }
}

/// <summary>
/// Non-existent handler for testing error handling
/// </summary>
public abstract class NonExistentHandler : IComponentHandler
{
    public Guid EntityId { get; private set; }
    public void InitializeContext(Guid entityId) => EntityId = entityId;
    public abstract Task<IComponent> Get();
}