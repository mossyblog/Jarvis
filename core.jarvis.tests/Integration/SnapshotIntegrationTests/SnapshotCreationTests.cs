using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.SnapshotIntegrationTests;

public class SnapshotCreationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that Commit automatically creates snapshots
    /// PURPOSE: Ensure snapshot functionality works transparently
    /// BUSINESS CONTEXT: Audit trail requirement for all data changes
    /// WHY IMPORTANT: Core feature must work without code changes
    /// ARCHITECTURAL SIGNIFICANCE: Validates fire-and-forget pattern
    /// FUTURE RESILIENCE: Protects against breaking automatic snapshotting
    /// </summary>
    [Fact]
    public async Task Commit_Should_Create_Initial_Snapshot()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Test", 
            Value = 100 
        };
        
        // Act
        await TestDataContext().Commit(component);
        
        // Allow async snapshot operation to complete
        await Task.Delay(500);
        
        // Assert
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        snapshotList.Count.ShouldBe(1);
        snapshotList[0].Operation.ShouldBe("CREATE"); // First commit is treated as CREATE
        snapshotList[0].Version.ShouldBe(1);
    }
    
    /// <summary>
    /// INTENT: Verify that updates capture previous state
    /// PURPOSE: Ensure we can track all historical changes
    /// BUSINESS CONTEXT: Compliance requires full change history
    /// WHY IMPORTANT: Must capture state before changes
    /// ARCHITECTURAL SIGNIFICANCE: Validates pre-commit snapshot capture
    /// FUTURE RESILIENCE: Ensures historical data integrity
    /// </summary>
    [Fact]
    public async Task Commit_Should_Capture_Previous_State_On_Update()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Original", 
            Value = 100 
        };
        await TestDataContext().Commit(component);
        
        // Allow first snapshot to complete
        await Task.Delay(500);
        
        // Act
        component.Name = "Updated";
        component.Value = 200;
        await TestDataContext().Commit(component);
        
        // Allow second snapshot to complete
        await Task.Delay(500);
        
        // Assert
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        snapshotList.Count.ShouldBe(2);
        
        var firstSnapshot = snapshotList[0].Deserialize<TestComponent>();
        firstSnapshot.Name.ShouldBe("Original");
        firstSnapshot.Value.ShouldBe(100);
        firstSnapshot.Version.ShouldBe(1);
        
        var secondSnapshot = snapshotList[1].Deserialize<TestComponent>();
        secondSnapshot.Name.ShouldBe("Original");  // UPDATE snapshot captures state BEFORE the update
        secondSnapshot.Value.ShouldBe(100);        // So it still has the original values  
        secondSnapshot.Version.ShouldBe(1);        // With version 1 (before increment)
    }
    
    /// <summary>
    /// INTENT: Verify that TryCommit creates snapshots on success
    /// PURPOSE: Ensure concurrency-safe commits also create snapshots
    /// BUSINESS CONTEXT: All updates must be audited
    /// WHY IMPORTANT: TryCommit is commonly used for safe updates
    /// ARCHITECTURAL SIGNIFICANCE: Validates snapshot integration with optimistic concurrency
    /// FUTURE RESILIENCE: Ensures all commit paths are covered
    /// </summary>
    [Fact]
    public async Task TryCommit_Should_Create_Snapshot_On_Success()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Initial", 
            Value = 50 
        };
        
        // Act
        var success = await TestDataContext().TryCommit(component);
        
        // Allow snapshot to complete
        await Task.Delay(500);
        
        // Assert
        success.ShouldBe(true);
        
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        snapshotList.Count.ShouldBe(1);
        snapshotList[0].Operation.ShouldBe("CREATE");
        snapshotList[0].Version.ShouldBe(1);
    }
}