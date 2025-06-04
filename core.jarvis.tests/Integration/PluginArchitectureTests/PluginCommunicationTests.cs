using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.PluginArchitectureTests;

/// <summary>
/// Tests for plugin communication and coordination between plugins.
/// </summary>
public class PluginCommunicationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verifies that plugins can communicate through shared entity data.
    /// PURPOSE: Ensures components from different plugins can work together.
    /// BUSINESS CONTEXT: Enables cross-domain workflows.
    /// WHY IMPORTANT: Supports plugin ecosystem integration.
    /// ARCHITECTURAL SIGNIFICANCE: Shows ECS component coordination.
    /// FUTURE RESILIENCE: Entity-based communication pattern should be preserved.
    /// </summary>
    [Fact]
    public async Task Plugins_Should_Coordinate_Through_SharedEntities()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Act
        await TestDataContext().For<TestHandler>(entityId).Create("Communication Test", "ACTIVE");
        await TestDataContext().For<PositionTestHandler>(entityId).Create(100, 200);
        
        var testComp = await TestDataContext().For<TestHandler>(entityId).Get();
        var position = await TestDataContext().For<PositionTestHandler>(entityId).Get();
        
        // Assert
        testComp.ShouldNotBeNull();
        position.ShouldNotBeNull();
        testComp.OwnerEntityId.ShouldBe(position.OwnerEntityId);
        await CleanupEntity(entityId);
    }

    private async Task CleanupEntity(Guid entityId)
    {
        await TestDataContext().Remove<TestComponent>(entityId);
        await TestDataContext().Remove<PositionComponent>(entityId);
        await TestDataContext().Remove<VelocityComponent>(entityId);
    }
}
