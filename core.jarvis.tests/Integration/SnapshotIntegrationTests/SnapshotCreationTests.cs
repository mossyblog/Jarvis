using core.jarvis.tests.Components;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace core.jarvis.tests.Integration.SnapshotIntegrationTests;

public class SnapshotCreationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that Commit automatically creates snapshots for versioned components
    /// PURPOSE: Ensure snapshot functionality works transparently for initial component creation
    /// BUSINESS CONTEXT: Audit trail requirement for all data changes
    /// WHY IMPORTANT: Core feature must work without code changes
    /// ARCHITECTURAL SIGNIFICANCE: Validates automatic snapshot creation on commit
    /// FUTURE RESILIENCE: Protects against breaking automatic snapshotting
    /// </summary>
    [Fact]
    public async Task Commit_Should_Create_Initial_Snapshot_For_Versioned_Component()
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

        // Debug: Check what component ID we're looking for
        Logger().LogInformation("Looking for snapshots for component ID: {ComponentId}", component.Id);
        Logger().LogInformation("Component type: {ComponentType}", typeof(TestComponent).Name);

        // Assert - Snapshot should be available immediately after commit
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();

        snapshots.ShouldNotBeNull($"No snapshots found for component {component.Id}. Component type: {typeof(TestComponent).Name}");

        // Debug: Check what's in the snapshots JSON
        Logger().LogInformation("Snapshots JSON: {SnapshotsJson}", snapshots.Snapshots);
        Logger().LogInformation("Snapshot record ID: {SnapshotId}", snapshots.Id);
        Logger().LogInformation("Snapshot record EntityId: {EntityId}", snapshots.EntityId);
        Logger().LogInformation("Snapshot record ComponentType: {ComponentType}", snapshots.ComponentType);
        Logger().LogInformation("Snapshot record ComponentId: {ComponentId}", snapshots.ComponentId);

        var snapshotList = snapshots.GetSnapshots();
        Logger().LogInformation("Retrieved {SnapshotCount} snapshots from JSON", snapshotList.Count);

        snapshotList.Count.ShouldBe(1);
        snapshotList[0].Operation.ShouldBe("CREATE"); // First commit is treated as CREATE
        snapshotList[0].Version.ShouldBe(1);

        // Verify snapshot contains correct data
        var snapshotData = snapshotList[0].Deserialize<TestComponent>();
        snapshotData.ShouldNotBeNull();
        snapshotData.Name.ShouldBe("Test");
        snapshotData.Value.ShouldBe(100);
    }
    
    /// <summary>
    /// INTENT: Verify that updates capture previous state before modification
    /// PURPOSE: Ensure we can track all historical changes with pre-update snapshots
    /// BUSINESS CONTEXT: Compliance requires full change history
    /// WHY IMPORTANT: Must capture state before changes for audit trails
    /// ARCHITECTURAL SIGNIFICANCE: Validates pre-commit snapshot capture on updates
    /// FUTURE RESILIENCE: Ensures historical data integrity
    /// </summary>
    [Fact]
    public async Task Commit_Should_Capture_Previous_State_On_Update()
    {
        // Arrange - Create initial component
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Original", 
            Value = 100 
        };
        await TestDataContext().Commit(component);
        
        // Verify initial snapshot was created
        var initialSnapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
        initialSnapshots.ShouldNotBeNull();
        initialSnapshots.GetSnapshots().Count.ShouldBe(1);
        
        // Act - Update the component
        component.Name = "Updated";
        component.Value = 200;
        await TestDataContext().Commit(component);
        
        // Assert - Should have 2 snapshots: CREATE + UPDATE
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        
        // Debug info
        Logger().LogInformation("Found {Count} snapshots for component {ComponentId}", snapshotList.Count, component.Id);
        Logger().LogInformation("Snapshots JSON length: {Length}", snapshots.Snapshots?.Length ?? 0);
        Logger().LogInformation("Snapshots JSON content: {Content}", snapshots.Snapshots?.Substring(0, Math.Min(200, snapshots.Snapshots?.Length ?? 0)));
        foreach (var snapshot in snapshotList)
        {
            Logger().LogInformation("Snapshot: Operation={Operation}, Version={Version}, Timestamp={Timestamp}", 
                snapshot.Operation, snapshot.Version, snapshot.Timestamp);
        }
        
        snapshotList.Count.ShouldBe(2);
        
        // First snapshot: Original CREATE operation
        var createSnapshot = snapshotList[0];
        createSnapshot.Operation.ShouldBe("CREATE");
        createSnapshot.Version.ShouldBe(1);
        var createData = createSnapshot.Deserialize<TestComponent>();
        createData.Name.ShouldBe("Original");
        createData.Value.ShouldBe(100);
        
        // Second snapshot: Pre-update state (captures state BEFORE the update)
        var updateSnapshot = snapshotList[1];
        updateSnapshot.Operation.ShouldBe("UPDATE");
        updateSnapshot.Version.ShouldBe(1); // Version before increment
        var updateData = updateSnapshot.Deserialize<TestComponent>();
        updateData.Name.ShouldBe("Original");  // Pre-update values
        updateData.Value.ShouldBe(100);        // Pre-update values
    }
    
    /// <summary>
    /// INTENT: Verify that TryCommit creates snapshots on successful operations
    /// PURPOSE: Ensure concurrency-safe commits also create snapshots
    /// BUSINESS CONTEXT: All updates must be audited regardless of commit method
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
        
        // Assert
        success.ShouldBe(true);
        
        // Snapshot should be available immediately after successful TryCommit
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
            
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        snapshotList.Count.ShouldBe(1);
        snapshotList[0].Operation.ShouldBe("CREATE");
        snapshotList[0].Version.ShouldBe(1);
        
        // Verify snapshot data integrity
        var snapshotData = snapshotList[0].Deserialize<TestComponent>();
        snapshotData.ShouldNotBeNull();
        snapshotData.Name.ShouldBe("Initial");
        snapshotData.Value.ShouldBe(50);
    }
    
    /// <summary>
    /// INTENT: Verify that multiple sequential updates create proper snapshot history
    /// PURPOSE: Ensure complete audit trail for multiple changes
    /// BUSINESS CONTEXT: Complex workflows require tracking multiple state transitions
    /// WHY IMPORTANT: Must maintain correct chronological order of changes
    /// ARCHITECTURAL SIGNIFICANCE: Validates snapshot ordering and version tracking
    /// FUTURE RESILIENCE: Protects against snapshot ordering issues
    /// </summary>
    [Fact]
    public async Task Multiple_Sequential_Updates_Should_Create_Complete_Snapshot_History()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Version1", 
            Value = 10 
        };
        
        // Act & Assert - Create initial component
        await TestDataContext().Commit(component);
        
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
        snapshots.ShouldNotBeNull();
        snapshots.GetSnapshots().Count.ShouldBe(1);
        
        // Act & Assert - First update
        component.Name = "Version2";
        component.Value = 20;
        await TestDataContext().Commit(component);
        
        snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
        snapshots.ShouldNotBeNull();
        snapshots.GetSnapshots().Count.ShouldBe(2);
        
        // Act & Assert - Second update
        component.Name = "Version3";
        component.Value = 30;
        await TestDataContext().Commit(component);
        
        // Final verification - Should have 3 snapshots total
        snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        snapshotList.Count.ShouldBe(3);
        
        // Verify chronological order and data integrity
        var createSnapshot = snapshotList[0];
        createSnapshot.Operation.ShouldBe("CREATE");
        createSnapshot.Version.ShouldBe(1);
        var createData = createSnapshot.Deserialize<TestComponent>();
        createData.Name.ShouldBe("Version1");
        createData.Value.ShouldBe(10);
        
        var firstUpdateSnapshot = snapshotList[1];
        firstUpdateSnapshot.Operation.ShouldBe("UPDATE");
        firstUpdateSnapshot.Version.ShouldBe(1); // Pre-update version
        var firstUpdateData = firstUpdateSnapshot.Deserialize<TestComponent>();
        firstUpdateData.Name.ShouldBe("Version1"); // Pre-update state
        firstUpdateData.Value.ShouldBe(10);
        
        var secondUpdateSnapshot = snapshotList[2];
        secondUpdateSnapshot.Operation.ShouldBe("UPDATE");
        secondUpdateSnapshot.Version.ShouldBe(2); // Pre-update version
        var secondUpdateData = secondUpdateSnapshot.Deserialize<TestComponent>();
        secondUpdateData.Name.ShouldBe("Version2"); // Pre-update state
        secondUpdateData.Value.ShouldBe(20);
    }
    
    /// <summary>
    /// INTENT: Verify that TryCommit handles concurrent updates correctly with snapshots
    /// PURPOSE: Ensure snapshot creation works properly with optimistic concurrency control
    /// BUSINESS CONTEXT: Concurrent operations must maintain data integrity
    /// WHY IMPORTANT: Version conflicts should not break snapshot functionality
    /// ARCHITECTURAL SIGNIFICANCE: Validates snapshot behavior under concurrency scenarios
    /// FUTURE RESILIENCE: Ensures robust behavior under concurrent load
    /// </summary>
    [Fact]
    public async Task TryCommit_Should_Handle_Version_Conflicts_Correctly()
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
        
        // Create initial component
        await TestDataContext().Commit(component);
        
        // Simulate concurrent modification scenario
        // First, get the current component to simulate another process reading it
        var staleComponent = new TestComponent
        {
            Id = component.Id,
            OwnerEntityId = entityId,
            Name = "Stale Update",
            Value = 200,
            Version = 1 // Stale version
        };
        
        // Act - Update the component first (this should succeed)
        component.Name = "Fresh Update";
        component.Value = 150;
        var firstSuccess = await TestDataContext().TryCommit(component);
        
        // Try to commit the stale component (this should fail due to version conflict)
        var secondSuccess = await TestDataContext().TryCommit(staleComponent);
        
        // Assert
        firstSuccess.ShouldBe(true);
        secondSuccess.ShouldBe(false); // Should fail due to version conflict
        
        // Verify only the successful commit created snapshots
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefault();
        snapshots.ShouldNotBeNull();
        var snapshotList = snapshots.GetSnapshots();
        
        // Should have CREATE + UPDATE snapshots (failed commit should not create snapshot)
        snapshotList.Count.ShouldBe(2);
        
        var createSnapshot = snapshotList[0];
        createSnapshot.Operation.ShouldBe("CREATE");
        
        var updateSnapshot = snapshotList[1];
        updateSnapshot.Operation.ShouldBe("UPDATE");
        var updateData = updateSnapshot.Deserialize<TestComponent>();
        updateData.Name.ShouldBe("Original"); // Pre-update state
        updateData.Value.ShouldBe(100);
    }
}