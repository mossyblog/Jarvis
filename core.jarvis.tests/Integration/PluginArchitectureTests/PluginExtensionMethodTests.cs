using core.jarvis.Data;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.PluginArchitectureTests;

/// <summary>
/// Tests for plugin extension methods. 
/// Verifies that plugins can provide domain-specific extension methods for DataContext.
/// Example: dataContext.Invoice(entityId) instead of dataContext.For<InvoiceHandler>(entityId)
/// </summary>
public class PluginExtensionMethodTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verifies the basic extension method pattern for DataContext.
    /// PURPOSE: Ensures plugins can provide fluent extension methods.
    /// BUSINESS CONTEXT: Supports domain-specific API style.
    /// WHY IMPORTANT: Provides user-friendly API for business context.
    /// ARCHITECTURAL SIGNIFICANCE: Enables domain-specific language.
    /// FUTURE RESILIENCE: Extension pattern should be preserved.
    /// </summary>
    [Fact]
    public async Task DataContextExtension_Method_Should_Return_Handler()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Act
        var handler = TestDataContext().TestComponent(entityId);
        await handler.Create("Extension Method Test", "ACTIVE");
        var retrieved = await TestDataContext().TestComponent(entityId).Get();
        
        // Assert
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestHandler>();
        
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Extension Method Test");
        retrieved.Status.ShouldBe("ACTIVE");
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verifies multiple extension methods working together.
    /// PURPOSE: Ensures plugins can provide multiple domain APIs.
    /// BUSINESS CONTEXT: Supports multi-component workflows.
    /// WHY IMPORTANT: Demonstrates component orchestration.
    /// ARCHITECTURAL SIGNIFICANCE: Shows plugin ecosystem extensibility.
    /// FUTURE RESILIENCE: Multi-extension pattern should be preserved.
    /// </summary>
    [Fact]
    public async Task Multiple_ExtensionMethods_Should_Work_Together()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Act
        await TestDataContext().TestComponent(entityId).Create("Component1", "ACTIVE");
        await TestDataContext().PositionComponent(entityId).Create(100, 200);
        await TestDataContext().VelocityComponent(entityId).Create(10, 20);
        
        var component = await TestDataContext().TestComponent(entityId).Get();
        var position = await TestDataContext().PositionComponent(entityId).Get();
        var velocity = await TestDataContext().VelocityComponent(entityId).Get();
        
        // Assert
        component.ShouldNotBeNull();
        component.Name.ShouldBe("Component1");
        
        position.ShouldNotBeNull();
        position.X.ShouldBe(100);
        position.Y.ShouldBe(200);
        
        velocity.ShouldNotBeNull();
        velocity.DeltaX.ShouldBe(10);
        velocity.DeltaY.ShouldBe(20);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
        await TestDataContext().Remove<PositionComponent>(entityId);
        await TestDataContext().Remove<VelocityComponent>(entityId);
    }
}

// Extension methods to enable fluent API style
public static class TestDataContextExtensions
{
    // These extension methods create a more fluent domain-specific API
    public static TestHandler TestComponent(this IDataContext dataContext, Guid entityId)
        => dataContext.For<TestHandler>(entityId);
    public static PositionTestHandler PositionComponent(this IDataContext dataContext, Guid entityId)
        => dataContext.For<PositionTestHandler>(entityId);
    public static VelocityTestHandler VelocityComponent(this IDataContext dataContext, Guid entityId)
        => dataContext.For<VelocityTestHandler>(entityId);
}
