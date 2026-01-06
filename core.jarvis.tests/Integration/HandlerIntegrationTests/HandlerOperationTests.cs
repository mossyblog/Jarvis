using core.jarvis.Exceptions;
using core.jarvis.tests.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.HandlerIntegrationTests;

/// <summary>
/// Integration tests for handler business operations and error handling.
/// Tests actual handler methods and business logic execution.
/// </summary>
public class HandlerOperationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Validates that the handler retrieves an existing component correctly.
    /// PURPOSE: Ensures the handler can fetch and return a component from the data store.
    /// BUSINESS CONTEXT: Used when business logic needs to access component details for an entity.
    /// WHY IMPORTANT: Incorrect retrieval could lead to business logic operating on stale or missing data.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms the handler's integration with the data access layer.
    /// FUTURE RESILIENCE: Ensures future changes to retrieval logic do not break basic get operations.
    /// </summary>
    [Fact]
    public async Task Handler_Get_WithExistingComponent_ShouldReturnComponent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent
        {
            OwnerEntityId = entityId,
            Name = "Handler Test",
            Status = "ACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();
        // Act
        var handler = TestDataContext().For<TestHandler>(entityId);
        var component = await handler.Get();
        // Assert
        component.ShouldNotBeNull();
        component.OwnerEntityId.ShouldBe(entityId);
        component.Name.ShouldBe("Handler Test");
        component.Status.ShouldBe("ACTIVE");
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }

    /// <summary>
    /// INTENT: Validates that the handler throws an EntityNotFoundException when the component does not exist.
    /// PURPOSE: Ensures proper error handling for missing data scenarios.
    /// BUSINESS CONTEXT: Prevents business logic from proceeding with invalid or non-existent entities.
    /// WHY IMPORTANT: Avoids silent failures and ensures clear error reporting.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the handler's error propagation and exception management.
    /// FUTURE RESILIENCE: Protects against future changes that might mask missing entity errors.
    /// </summary>
    [Fact]
    public void Handler_Get_WithNonExistentComponent_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        // Act & Assert
        Should.Throw<EntityNotFoundException>(() => TestDataContext().For<TestHandler>(entityId).Get().GetAwaiter().GetResult());
    }

    /// <summary>
    /// INTENT: Validates that the Activate business operation updates the component's status to ACTIVE.
    /// PURPOSE: Ensures business operations correctly modify component state.
    /// BUSINESS CONTEXT: Used when activating entities in business workflows.
    /// WHY IMPORTANT: Incorrect state transitions could lead to business process failures.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms the handler's ability to update and persist state changes.
    /// FUTURE RESILIENCE: Ensures future changes to activation logic do not break state transitions.
    /// </summary>
    [Fact]
    public async Task Handler_BusinessOperation_Activate_ShouldUpdateComponentStatus()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "Test Component",
            Status = "INACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();

        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act
        var result = await handler.Activate();

        // Assert
        result.ShouldBeTrue();

        // Verify the component was updated in the database
        var updatedComponent = await handler.Get();
        
        updatedComponent.ShouldNotBeNull();
        updatedComponent.Status.ShouldBe("ACTIVE");

        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }

    /// <summary>
    /// INTENT: Validates that activating an already active component throws a BusinessRuleException.
    /// PURPOSE: Ensures business rules are enforced and invalid operations are blocked.
    /// BUSINESS CONTEXT: Prevents redundant or conflicting state changes in business processes.
    /// WHY IMPORTANT: Maintains data integrity and enforces business constraints.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the handler's business rule enforcement.
    /// FUTURE RESILIENCE: Protects against future changes that might weaken business rule checks.
    /// </summary>
    [Fact]
    public async Task Handler_BusinessOperation_ActivateAlreadyActive_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "Test Component",
            Status = "ACTIVE" // Already active
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();

        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act & Assert
        var ex = await Should.ThrowAsync<BusinessRuleException>(() => handler.Activate());
        ex.Message.ShouldContain("already active");

        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }

    /// <summary>
    /// INTENT: Validates that the Deactivate business operation updates the component's status to INACTIVE.
    /// PURPOSE: Ensures business operations can deactivate entities as required.
    /// BUSINESS CONTEXT: Used when deactivating entities in business workflows.
    /// WHY IMPORTANT: Incorrect deactivation could leave entities in an invalid state.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms the handler's ability to update and persist deactivation.
    /// FUTURE RESILIENCE: Ensures future changes to deactivation logic do not break state transitions.
    /// </summary>
    [Fact]
    public async Task Handler_BusinessOperation_Deactivate_ShouldUpdateComponentStatus()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "Test Component",
            Status = "ACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();

        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act
        var result = await handler.Deactivate("Test deactivation");

        // Assert
        result.ShouldBeTrue();

        // Verify the component was updated in the database
        var updatedComponent = await handler.Get();
        
        updatedComponent.ShouldNotBeNull();
        updatedComponent.Status.ShouldBe("INACTIVE");

        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }

    /// <summary>
    /// INTENT: Validates that deactivating with an empty reason throws a ValidationException.
    /// PURPOSE: Ensures input validation is enforced for business operations.
    /// BUSINESS CONTEXT: Prevents incomplete or invalid business actions due to missing required information.
    /// WHY IMPORTANT: Maintains data quality and enforces business process requirements.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the handler's input validation logic.
    /// FUTURE RESILIENCE: Protects against future changes that might weaken validation checks.
    /// </summary>
    [Fact]
    public async Task Handler_BusinessOperation_DeactivateWithEmptyReason_ShouldThrowValidationException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "Test Component",
            Status = "ACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();

        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => handler.Deactivate(""));
        ex.Message.ShouldContain("reason");

        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }

    /// <summary>
    /// INTENT: Validates that multiple sequential operations maintain data consistency.
    /// PURPOSE: Ensures the handler can process a sequence of business operations without corrupting state.
    /// BUSINESS CONTEXT: Used in workflows where entities undergo multiple state changes.
    /// WHY IMPORTANT: Prevents data corruption and ensures reliable business process execution.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms the handler's transactional integrity and state management.
    /// FUTURE RESILIENCE: Ensures future changes do not introduce state inconsistencies in multi-operation scenarios.
    /// </summary>
    [Fact]
    public async Task Handler_MultipleOperations_ShouldMaintainDataConsistency()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "Test Component",
            Status = "INACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();

        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act - Perform multiple operations in sequence
        await handler.Activate();
        await handler.Deactivate("End of test");

        // Assert - Verify final state
        var finalComponent = await handler.Get();
        
        finalComponent.ShouldNotBeNull();
        finalComponent.Status.ShouldBe("INACTIVE");

        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
}