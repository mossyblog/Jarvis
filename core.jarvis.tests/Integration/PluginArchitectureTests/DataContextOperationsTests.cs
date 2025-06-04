using core.jarvis.Exceptions;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.PluginArchitectureTests;

/// <summary>
/// Tests for DataContext operations beyond handler management.
/// Tests the TryCommit(), Remove(), and Remove() methods.
/// </summary>
public class DataContextOperationsTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verifies that DataContext can save components directly.
    /// PURPOSE: Ensures components can be saved without handlers.
    /// BUSINESS CONTEXT: Supports utility operations and bulk saves.
    /// WHY IMPORTANT: Enables operations outside handler business logic.
    /// ARCHITECTURAL SIGNIFICANCE: Shows direct component management.
    /// FUTURE RESILIENCE: Direct save pattern should be preserved.
    /// </summary>
    [Fact]
    public async Task DataContext_Should_TryCommit_Components_Directly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent
        {
            OwnerEntityId = entityId,
            Name = "Direct Save",
            Status = "ACTIVE"
        };
        
        // Act
        var success = await TestDataContext().TryCommit(component);
        success.ShouldBeTrue();
        var retrieved = await TestDataContext().For<TestHandler>(entityId).Get();
        
        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Direct Save");
        retrieved.Status.ShouldBe("ACTIVE");
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verifies that DataContext can remove components by entity.
    /// PURPOSE: Ensures entity cleanup works correctly.
    /// BUSINESS CONTEXT: Supports entity deletion and cleanup.
    /// WHY IMPORTANT: Prevents orphaned components and data leaks.
    /// ARCHITECTURAL SIGNIFICANCE: Shows proper entity lifecycle management.
    /// FUTURE RESILIENCE: Entity cleanup pattern should be preserved.
    /// </summary>
    [Fact]
    public async Task DataContext_Should_Remove_Components_ByEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        await TestDataContext().For<TestHandler>(entityId).Create("To Be Removed", "ACTIVE");
        
        // Verify component exists
        var beforeRemoval = await TestDataContext().For<TestHandler>(entityId).Get();
        beforeRemoval.ShouldNotBeNull();
        
        // Act
        await TestDataContext().Remove<TestComponent>(entityId);
        
        // Assert
        try
        {
            // Should throw because the component is no longer there
            await TestDataContext().For<TestHandler>(entityId).Get();
            throw new Exception("Expected exception not thrown");
        }
        catch (EntityNotFoundException)
        {
            // Expected - this is the correct behavior
        }
    }
}
