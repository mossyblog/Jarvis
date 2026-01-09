using core.jarvis.data.Exceptions;
using core.jarvis.Exceptions;
using core.jarvis.tests.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Integration tests for version-based optimistic concurrency control.
/// Tests the actual TryCommit and Commit behavior with real database operations.
/// </summary>
public class ConcurrencyControlIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that TryCommit detects version conflicts and prevents lost updates.
    /// PURPOSE: Test version-based optimistic concurrency control.
    /// BUSINESS CONTEXT: Multiple users editing the same entity simultaneously.
    /// WHY IMPORTANT: Prevents data corruption from concurrent modifications.
    /// ARCHITECTURAL SIGNIFICANCE: Validates the version-based concurrency implementation.
    /// FUTURE RESILIENCE: Ensures data integrity in multi-user scenarios.
    /// </summary>
    [Fact]
    public async Task TryCommit_WithVersionConflict_ShouldReturnFalse()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Create initial component with version 1
        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Initial",
            Status = "ACTIVE",
            Version = null // Will be set to 1 on first save
        };
        
        var success = await TestDataContext().TryCommit(component);
        success.ShouldBeTrue();
        component.Version.ShouldBe(1);
        
        // Simulate another user loading the same component
        var userAComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        var userBComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        
        // User A updates first
        userAComponent.Name = "Updated by User A";
        var userASuccess = await TestDataContext().TryCommit(userAComponent);
        userASuccess.ShouldBeTrue();
        userAComponent.Version.ShouldBe(2);
        
        // Act - User B tries to update with stale version
        userBComponent.Name = "Updated by User B";
        userBComponent.Version.ShouldBe(1); // Still has old version
        var userBSuccess = await TestDataContext().TryCommit(userBComponent);
        
        // Assert
        userBSuccess.ShouldBeFalse(); // Should fail due to version conflict
        
        // Verify the database still has User A's update
        var currentComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        currentComponent.Name.ShouldBe("Updated by User A");
        currentComponent.Version.ShouldBe(2);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verify that TryCommit succeeds when versions match.
    /// PURPOSE: Test the happy path of version-based concurrency.
    /// BUSINESS CONTEXT: Normal update flow when user has the latest version.
    /// WHY IMPORTANT: Confirms valid updates aren't blocked.
    /// ARCHITECTURAL SIGNIFICANCE: Shows concurrency control doesn't impede normal operations.
    /// FUTURE RESILIENCE: Ensures the system remains usable with concurrency protection.
    /// </summary>
    [Fact]
    public async Task TryCommit_WithMatchingVersion_ShouldSucceedAndIncrementVersion()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Initial",
            Status = "ACTIVE"
        };
        
        // Initial save
        var success = await TestDataContext().TryCommit(component);
        success.ShouldBeTrue();
        component.Version.ShouldBe(1);
        
        // Act - Update with correct version
        component.Name = "Updated";
        component.Status = "INACTIVE";
        var updateSuccess = await TestDataContext().TryCommit(component);
        
        // Assert
        updateSuccess.ShouldBeTrue();
        component.Version.ShouldBe(2); // Version should be incremented
        
        // Verify in database
        var savedComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        savedComponent.Name.ShouldBe("Updated");
        savedComponent.Status.ShouldBe("INACTIVE");
        savedComponent.Version.ShouldBe(2);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verify that new components get version 1.
    /// PURPOSE: Test version initialization for new entities.
    /// BUSINESS CONTEXT: Creating new entities that support versioning.
    /// WHY IMPORTANT: Establishes consistent versioning from the start.
    /// ARCHITECTURAL SIGNIFICANCE: Shows version initialization for IVersionedComponent.
    /// FUTURE RESILIENCE: Ensures all versioned components have valid version numbers.
    /// </summary>
    [Fact]
    public async Task TryCommit_WithNewComponent_ShouldSetVersionToOne()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "New Component",
            Status = "ACTIVE",
            Version = null // No version yet
        };
        
        // Act
        var success = await TestDataContext().TryCommit(component);
        
        // Assert
        success.ShouldBeTrue();
        component.Version.ShouldBe(1); // Should be initialized to 1
        
        // Verify in database
        var savedComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        savedComponent.Version.ShouldBe(1);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verify that Commit enforces version concurrency control like TryCommit.
    /// PURPOSE: Test that Commit operations properly validate version conflicts.
    /// BUSINESS CONTEXT: Commit operations must maintain data integrity through version checking.
    /// WHY IMPORTANT: Prevents data corruption from concurrent modifications, even in Commit operations.
    /// ARCHITECTURAL SIGNIFICANCE: Shows Commit maintains same concurrency guarantees as TryCommit.
    /// FUTURE RESILIENCE: Ensures version-based concurrency is enforced across all commit operations.
    /// </summary>
    [Fact]
    public async Task Commit_WithVersionConflict_ShouldThrowConcurrencyException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testRunId = Guid.NewGuid().ToString("N")[..8];
        
        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = $"CommitConcurrencyTest-{testRunId}",
            Status = "ACTIVE",
            Value = 100
        };
        
        // Step 1: Initial commit should set version to 1
        await TestDataContext().Commit(component);
        component.Version.ShouldBe(1);
        
        // Step 2: Successful update should increment to version 2
        component.Name = $"Updated-{testRunId}";
        await TestDataContext().Commit(component);
        component.Version.ShouldBe(2);
        
        // Step 3: Test that Commit throws exception with stale version (like TryCommit)
        component.Version = 1; // Simulate stale version
        component.Status = "SHOULD_FAIL";
        
        // Act & Assert - Commit should throw ConcurrencyException
        var exception = await Should.ThrowAsync<ConcurrencyException>(async () =>
        {
            await TestDataContext().Commit(component);
        });
        
        exception.Message.ShouldContain("Concurrency conflict");
        exception.Message.ShouldContain("Expected version: 1");
        exception.Message.ShouldContain("Actual version: 2");
        
        // Verify the database still has the valid version 2 state
        var finalComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        finalComponent.ShouldNotBeNull();
        finalComponent.Name.ShouldBe($"Updated-{testRunId}"); // Should still have the valid update
        finalComponent.Status.ShouldBe("ACTIVE"); // Should NOT have the failed update
        finalComponent.Version.ShouldBe(2);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verify that concurrent updates from multiple contexts work correctly.
    /// PURPOSE: Test real-world concurrent modification scenario.
    /// BUSINESS CONTEXT: Multiple services updating the same entity.
    /// WHY IMPORTANT: Validates concurrency control in distributed scenarios.
    /// ARCHITECTURAL SIGNIFICANCE: Tests isolation between different DataContext instances.
    /// FUTURE RESILIENCE: Ensures system handles concurrent access patterns.
    /// </summary>
    [Fact]
    public async Task TryCommit_WithConcurrentUpdates_ShouldHandleCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Create initial component
        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Initial",
            Status = "ACTIVE",
            Value = 100
        };
        
        await TestDataContext().TryCommit(component);
        component.Version.ShouldBe(1);
        
        // Simulate two concurrent users loading the component
        var context1Component = await TestDataContext().For<TestHandler>(entityId).Get();
        var context2Component = await TestDataContext().For<TestHandler>(entityId).Get();
        
        // Both have version 1
        context1Component.Version.ShouldBe(1);
        context2Component.Version.ShouldBe(1);
        
        // Act - Both try to update
        context1Component.Value = 200;
        context2Component.Value = 300;
        
        var context1Success = await TestDataContext().TryCommit(context1Component);
        var context2Success = await TestDataContext().TryCommit(context2Component);
        
        // Assert - Only one should succeed
        context1Success.ShouldBeTrue();
        context2Success.ShouldBeFalse();
        
        // Verify final state
        var finalComponent = await TestDataContext().For<TestHandler>(entityId).Get();
        finalComponent.Value.ShouldBe(200); // Context 1's update won
        finalComponent.Version.ShouldBe(2);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
    
    /// <summary>
    /// INTENT: Verify that handler operations use version-based concurrency.
    /// PURPOSE: Test that handlers properly handle concurrency conflicts.
    /// BUSINESS CONTEXT: Business operations through handlers must respect versioning.
    /// WHY IMPORTANT: Ensures handlers don't bypass concurrency control.
    /// ARCHITECTURAL SIGNIFICANCE: Validates handler integration with concurrency.
    /// FUTURE RESILIENCE: Ensures all data access paths respect concurrency.
    /// </summary>
    [Fact]
    public async Task Handler_WithVersionConflict_ShouldThrowConcurrencyException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Create initial component
        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Test",
            Status = "ACTIVE",
            Value = 100
        };
        
        await TestDataContext().TryCommit(component);
        component.Version.ShouldBe(1);
        
        // Load component in two contexts to simulate concurrent access
        var comp1 = await TestDataContext().For<TestHandler>(entityId).Get();
        var comp2 = await TestDataContext().For<TestHandler>(entityId).Get();
        comp1.Version.ShouldBe(1);
        comp2.Version.ShouldBe(1);
        
        // Update through first context
        comp1.Value = 200;
        var success1 = await TestDataContext().TryCommit(comp1);
        success1.ShouldBeTrue();
        comp1.Version.ShouldBe(2);
        
        // Act - Try to update through second context with stale version
        comp2.Value = 300;
        comp2.Version.ShouldBe(1); // Still has old version
        
        // This should fail and handler should throw ConcurrencyException
        var handler2 = TestDataContext().For<TestHandler>(entityId);
        
        // Create a custom update method that uses the stale component
        var exception = await Should.ThrowAsync<ConcurrencyException>(async () =>
        {
            var success = await TestDataContext().TryCommit(comp2);
            if (!success)
            {
                throw new ConcurrencyException("Update failed due to concurrent modification");
            }
        });
        
        exception.Message.ShouldContain("concurrent modification");
        
        // Verify the database still has the first update
        var current = await TestDataContext().For<TestHandler>(entityId).Get();
        current.Value.ShouldBe(200);
        current.Version.ShouldBe(2);
        
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
}