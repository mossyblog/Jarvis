using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.SnapshotIntegrationTests;

public class SnapshotRestoreTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify components can be restored to previous versions
    /// PURPOSE: Enable rollback functionality
    /// BUSINESS CONTEXT: Error recovery and audit investigations
    /// WHY IMPORTANT: Must be able to recover from mistakes
    /// ARCHITECTURAL SIGNIFICANCE: Validates restore pattern
    /// FUTURE RESILIENCE: Ensures recovery capability remains intact
    /// </summary>
    [Fact]
    public async Task Should_Restore_Component_To_Previous_Version()
    {
        // Arrange - Create component with multiple versions
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Version1", 
            Value = 1 
        };
        await TestDataContext().Commit(component);
        
        component.Name = "Version2";
        component.Value = 2;
        await TestDataContext().Commit(component);
        
        component.Name = "Version3";
        component.Value = 3;
        await TestDataContext().Commit(component);
        
        // Act - Restore to version 2
        var snapshotRecord = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshotRecord.ShouldNotBeNull();
        var snapshots = snapshotRecord.GetSnapshots();
        var v2Snapshot = snapshots.FirstOrDefault(s => s.Version == 2);
        v2Snapshot.ShouldNotBeNull();
        
        var restoredComponent = v2Snapshot.Deserialize<TestComponent>();
        restoredComponent.ShouldNotBeNull();
        
        // Update the component ID to match the current one for commit
        restoredComponent = new TestComponent
        {
            Id = component.Id,
            OwnerEntityId = restoredComponent.OwnerEntityId,
            Name = restoredComponent.Name,
            Value = restoredComponent.Value,
            Version = component.Version // Use current version, will be incremented
        };
        
        await TestDataContext().Commit(restoredComponent);
        
        // Assert
        var current = await TestDataContext()
            .For<TestHandler>(entityId)
            .Get();
            
        current.Name.ShouldBe("Version2");
        current.Value.ShouldBe(2);
        current.Version.ShouldBe(4); // Should be version 4 after restore
        
        // Verify new snapshot was created
        var finalSnapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        var finalSnapshotList = finalSnapshots.GetSnapshots();
        finalSnapshotList.Count.ShouldBe(4);
    }
    
    /// <summary>
    /// INTENT: Verify restore by timestamp functionality
    /// PURPOSE: Enable time-based recovery
    /// BUSINESS CONTEXT: Restore to specific point in time
    /// WHY IMPORTANT: Common recovery scenario
    /// ARCHITECTURAL SIGNIFICANCE: Validates timestamp-based queries
    /// FUTURE RESILIENCE: Ensures time-based recovery works
    /// </summary>
    [Fact]
    public async Task Should_Restore_Component_By_Timestamp()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Morning", 
            Value = 10 
        };
        await TestDataContext().Commit(component);
        
        // Capture timestamp after first commit
        var midTime = DateTime.UtcNow;
        
        component.Name = "Afternoon";
        component.Value = 20;
        await TestDataContext().Commit(component);
        
        // Act
        var snapshotRecord = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshotRecord.ShouldNotBeNull();
        var snapshots = snapshotRecord.GetSnapshots();
        
        // Find snapshot before midTime
        var morningSnapshot = snapshots
            .Where(s => s.Timestamp < midTime)
            .LastOrDefault();
            
        morningSnapshot.ShouldNotBeNull();
        var restoredComponent = morningSnapshot.Deserialize<TestComponent>();
        
        // Assert
        restoredComponent.Name.ShouldBe("Morning");
        restoredComponent.Value.ShouldBe(10);
    }
    
    /// <summary>
    /// INTENT: Verify undo functionality (restore to previous version)
    /// PURPOSE: Enable simple undo operations
    /// BUSINESS CONTEXT: Quick rollback of last change
    /// WHY IMPORTANT: Common user requirement
    /// ARCHITECTURAL SIGNIFICANCE: Validates array-based snapshot access
    /// FUTURE RESILIENCE: Ensures undo pattern works correctly
    /// </summary>
    [Fact]
    public async Task Should_Undo_Last_Change()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Before", 
            Value = 100 
        };
        await TestDataContext().Commit(component);
        
        component.Name = "After";
        component.Value = 200;
        await TestDataContext().Commit(component);
        
        // Act - Get previous version
        var snapshotRecord = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        var snapshots = snapshotRecord.GetSnapshots();
        snapshots.Count.ShouldBeGreaterThan(1);
        
        // Get second to last snapshot (previous version)
        var previousSnapshot = snapshots[snapshots.Count - 2];
        var previousComponent = previousSnapshot.Deserialize<TestComponent>();
        
        // Assert
        previousComponent.Name.ShouldBe("Before");
        previousComponent.Value.ShouldBe(100);
    }
}