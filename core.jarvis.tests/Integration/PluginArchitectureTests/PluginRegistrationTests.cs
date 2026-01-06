using core.jarvis.Data;
using core.jarvis.tests.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.PluginArchitectureTests;

/// <summary>
/// Tests for plugin discovery, registration, and lifecycle management.
/// These tests verify that plugins can be dynamically loaded and registered.
/// </summary>
public class PluginRegistrationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verifies registered handler types can be resolved.
    /// PURPOSE: Ensures plugin system supports handler resolution.
    /// BUSINESS CONTEXT: Supports extensible plugin architectures.
    /// WHY IMPORTANT: Prevents handler registration conflicts.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler registration contract.
    /// FUTURE RESILIENCE: Protects against regressions in handler registration.
    /// </summary>
    [Fact]
    public void PluginRegistration_Should_Support_Registered_Handler_Types()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        // DEBUG: Print type info

        // Act
        var testHandler = TestDataContext().For<TestHandler>(entityId);
        var positionHandler = TestDataContext().For<PositionTestHandler>(entityId);
        var velocityHandler = TestDataContext().For<VelocityTestHandler>(entityId);

        // Assert
        testHandler.ShouldNotBeNull();
        testHandler.ShouldBeOfType<TestHandler>();
        positionHandler.ShouldNotBeNull();
        positionHandler.ShouldBeOfType<PositionTestHandler>();
        velocityHandler.ShouldNotBeNull();
        velocityHandler.ShouldBeOfType<VelocityTestHandler>();
    }

    /// <summary>
    /// INTENT: Verifies handler instances are isolated per entity.
    /// PURPOSE: Ensures handler instances are not shared.
    /// BUSINESS CONTEXT: Supports entity isolation in plugins.
    /// WHY IMPORTANT: Prevents cross-entity state leakage.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler instance isolation.
    /// FUTURE RESILIENCE: Protects against handler instance sharing bugs.
    /// </summary>
    [Fact]
    public void PluginRegistration_Should_Isolate_Handler_Instances()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();

        // Act
        var handler1 = TestDataContext().For<TestHandler>(entityId1);
        var handler2 = TestDataContext().For<TestHandler>(entityId2);

        // Assert
        handler1.ShouldNotBeSameAs(handler2);
        handler1.ShouldNotBeNull();
        handler2.ShouldNotBeNull();
    }

    /// <summary>
    /// INTENT: Verifies handler compatibility is validated.
    /// PURPOSE: Ensures only compatible handlers can be resolved.
    /// BUSINESS CONTEXT: Supports plugin handler safety.
    /// WHY IMPORTANT: Prevents invalid handler resolution.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler compatibility contract.
    /// FUTURE RESILIENCE: Protects against handler compatibility regressions.
    /// </summary>
    [Fact]
    public void PluginRegistration_Should_Validate_Handler_Compatibility()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var handler = TestDataContext().For<TestHandler>(entityId);

        // Assert
        handler.ShouldNotBeNull();
        handler.ShouldBeAssignableTo<IComponentHandler<TestComponent>>();
        handler.ShouldBeAssignableTo<IComponentHandler>();
    }
}

