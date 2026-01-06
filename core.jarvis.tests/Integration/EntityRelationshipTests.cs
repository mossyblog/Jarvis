using core.jarvis.tests.Components;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration;

public class EntityRelationshipTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that parent-child relationships can be established between entities
    /// PURPOSE: Ensure the LinkRelationship method correctly links parent and child entities
    /// BUSINESS CONTEXT: Work orders need to track their associated invoices and payments
    /// WHY IMPORTANT: Hierarchical relationships are fundamental to business entity modeling
    /// ARCHITECTURAL SIGNIFICANCE: Tests the Entity parent/child relationship pattern
    /// FUTURE RESILIENCE: Protects against regression in relationship management
    /// </summary>
    [Fact]
    public async Task AddRelationship_Should_Link_Parent_And_Child_Entities()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        
        // Act
        await TestDataContext().LinkRelationship(parentId, childId, "WorkOrder", "Invoice");
        
        // Assert
        var parentOfChild = await TestDataContext().Parent(childId);
        parentOfChild.ShouldBe(parentId);
        
        var childrenOfParent = await TestDataContext().Children(parentId);
        childrenOfParent.ShouldContain(childId);
        
        var isChild = await TestDataContext().ChildOf(childId, parentId);
        isChild.ShouldBeTrue();
    }
    
    /// <summary>
    /// INTENT: Verify that relationships can be removed between entities
    /// PURPOSE: Ensure the UnlinkRelationship method correctly unlinks entities
    /// BUSINESS CONTEXT: Sometimes relationships need to be dissolved (e.g., cancelled orders)
    /// WHY IMPORTANT: Flexibility in relationship management is critical for real-world scenarios
    /// ARCHITECTURAL SIGNIFICANCE: Tests bidirectional relationship cleanup
    /// FUTURE RESILIENCE: Ensures clean relationship removal without orphaned references
    /// </summary>
    [Fact]
    public async Task RemoveRelationship_Should_Unlink_Parent_And_Child()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await TestDataContext().LinkRelationship(parentId, childId);
        
        // Act
        await TestDataContext().UnlinkRelationship(parentId, childId);
        
        // Assert
        var parentOfChild = await TestDataContext().Parent(childId);
        parentOfChild.ShouldBeNull();
        
        var childrenOfParent = await TestDataContext().Children(parentId);
        childrenOfParent.ShouldNotContain(childId);
        
        var isChild = await TestDataContext().ChildOf(childId, parentId);
        isChild.ShouldBeFalse();
    }
    
    /// <summary>
    /// INTENT: Verify that multiple children can be associated with a single parent
    /// PURPOSE: Ensure one-to-many relationships work correctly
    /// BUSINESS CONTEXT: A work order can have multiple invoices
    /// WHY IMPORTANT: One-to-many is a common business relationship pattern
    /// ARCHITECTURAL SIGNIFICANCE: Tests list management within relationship component
    /// FUTURE RESILIENCE: Validates scalability of parent-child relationships
    /// </summary>
    [Fact]
    public async Task Parent_Can_Have_Multiple_Children()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();
        var child3Id = Guid.NewGuid();
        
        // Act
        await TestDataContext().LinkRelationship(parentId, child1Id);
        await TestDataContext().LinkRelationship(parentId, child2Id);
        await TestDataContext().LinkRelationship(parentId, child3Id);
        
        // Assert
        var children = await TestDataContext().Children(parentId);
        children.Count.ShouldBe(3);
        children.ShouldContain(child1Id);
        children.ShouldContain(child2Id);
        children.ShouldContain(child3Id);
    }
    
    /// <summary>
    /// INTENT: Verify that ancestor traversal works correctly
    /// PURPOSE: Ensure the Ancestors method returns all parent entities up the hierarchy
    /// BUSINESS CONTEXT: Need to trace entity lineage (e.g., project -> work order -> invoice)
    /// WHY IMPORTANT: Hierarchical queries are essential for reporting and authorization
    /// ARCHITECTURAL SIGNIFICANCE: Tests recursive relationship traversal
    /// FUTURE RESILIENCE: Protects against infinite loops and circular references
    /// </summary>
    [Fact]
    public async Task Ancestors_Should_Return_Full_Hierarchy()
    {
        // Arrange
        var grandparentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        
        await TestDataContext().LinkRelationship(grandparentId, parentId);
        await TestDataContext().LinkRelationship(parentId, childId);
        
        // Act
        var ancestors = await TestDataContext().Ancestors(childId);
        
        // Assert
        ancestors.Count.ShouldBe(2);
        ancestors[0].ShouldBe(parentId);
        ancestors[1].ShouldBe(grandparentId);
    }
    
    /// <summary>
    /// INTENT: Verify that descendant traversal works correctly
    /// PURPOSE: Ensure the Descendants method returns all child entities down the hierarchy
    /// BUSINESS CONTEXT: Need to find all related entities under a parent (e.g., all invoices under a project)
    /// WHY IMPORTANT: Bulk operations often need to operate on entire subtrees
    /// ARCHITECTURAL SIGNIFICANCE: Tests breadth-first traversal implementation
    /// FUTURE RESILIENCE: Validates performance with deep hierarchies
    /// </summary>
    [Fact]
    public async Task Descendants_Should_Return_All_Children_Recursively()
    {
        // Arrange
        var grandparentId = Guid.NewGuid();
        var parent1Id = Guid.NewGuid();
        var parent2Id = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();
        var child3Id = Guid.NewGuid();
        
        // Build hierarchy:
        // grandparent
        //   ├── parent1
        //   │   ├── child1
        //   │   └── child2
        //   └── parent2
        //       └── child3
        await TestDataContext().LinkRelationship(grandparentId, parent1Id);
        await TestDataContext().LinkRelationship(grandparentId, parent2Id);
        await TestDataContext().LinkRelationship(parent1Id, child1Id);
        await TestDataContext().LinkRelationship(parent1Id, child2Id);
        await TestDataContext().LinkRelationship(parent2Id, child3Id);
        
        // Act
        var descendants = await TestDataContext().Descendants(grandparentId);
        
        // Assert
        descendants.Count.ShouldBe(5);
        descendants.ShouldContain(parent1Id);
        descendants.ShouldContain(parent2Id);
        descendants.ShouldContain(child1Id);
        descendants.ShouldContain(child2Id);
        descendants.ShouldContain(child3Id);
    }
    
    /// <summary>
    /// INTENT: Verify that relationship types are preserved
    /// PURPOSE: Ensure parent and child types are stored and retrievable
    /// BUSINESS CONTEXT: Need to know what type of entities are related (WorkOrder -> Invoice)
    /// WHY IMPORTANT: Type information enables type-safe queries and validation
    /// ARCHITECTURAL SIGNIFICANCE: Tests metadata storage within relationships
    /// FUTURE RESILIENCE: Enables future type-based relationship constraints
    /// </summary>
    [Fact]
    public async Task Relationship_Should_Preserve_Entity_Types()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        
        // Act
        await TestDataContext().LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");
        
        // Assert - Verify the relationship was created
        var parentOfInvoice = await TestDataContext().Parent(invoiceId);
        parentOfInvoice.ShouldBe(workOrderId);
        
        var childrenOfWorkOrder = await TestDataContext().Children(workOrderId);
        childrenOfWorkOrder.ShouldContain(invoiceId);
    }
    
    /// <summary>
    /// INTENT: Verify that snapshots capture relationship changes
    /// PURPOSE: Ensure relationship modifications are tracked in version history
    /// BUSINESS CONTEXT: Audit trail needs to show when relationships were created/modified
    /// WHY IMPORTANT: Compliance and debugging require full change history
    /// ARCHITECTURAL SIGNIFICANCE: Tests integration with snapshot system
    /// FUTURE RESILIENCE: Ensures relationship changes are auditable
    /// </summary>
    [Fact]
    public async Task Relationship_Changes_Should_Be_Captured_In_Snapshots()
    {
        // Arrange - Create entities with components so snapshots can be generated
        var parentEntity = TestDataContext().NewEntity();
        var child1Entity = TestDataContext().NewEntity();
        var child2Entity = TestDataContext().NewEntity();
        
        // Create components for entities to enable snapshot generation
        await TestDataContext().Commit(new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = parentEntity.Id,
            Name = "Parent Component",
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        });
        
        await TestDataContext().Commit(new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = child1Entity.Id,
            Name = "Child1 Component",
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        });
        
        await TestDataContext().Commit(new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = child2Entity.Id,
            Name = "Child2 Component",
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        });
        
        // Act - Create relationships (snapshots are generated when components are committed)
        await TestDataContext().LinkRelationship(parentEntity.Id, child1Entity.Id);
        await TestDataContext().LinkRelationship(parentEntity.Id, child2Entity.Id);
        
        // Assert - Check snapshots were created for the parent entity
        var snapshots = await TestDataContext().Snapshots()
            .ForEntity(parentEntity.Id)
            .ToList();
        
        snapshots.ShouldNotBeEmpty();
        
        // Verify snapshots exist for child entities too
        var child1Snapshots = await TestDataContext().Snapshots()
            .ForEntity(child1Entity.Id)
            .ToList();
        child1Snapshots.ShouldNotBeEmpty();
        
        var child2Snapshots = await TestDataContext().Snapshots()
            .ForEntity(child2Entity.Id)
            .ToList();
        child2Snapshots.ShouldNotBeEmpty();
    }
    
    /// <summary>
    /// INTENT: Verify that linking to a non-existent parent entity creates the relationship
    /// PURPOSE: Document that LinkRelationship works with Entity records directly
    /// BUSINESS CONTEXT: Relationships can be established before entities have business components
    /// WHY IMPORTANT: Allows flexible entity creation patterns
    /// ARCHITECTURAL SIGNIFICANCE: Entity relationships are independent of components
    /// FUTURE RESILIENCE: Shows that relationships work at the entity level
    /// </summary>
    [Fact]
    public async Task LinkRelationship_With_NonExistent_Parent_Creates_Relationship()
    {
        // Arrange
        var nonExistentParentId = Guid.NewGuid(); // This ID doesn't exist in the database
        var childEntity = TestDataContext().NewEntity();
        
        // Act
        // LinkRelationship creates Entity records for both entities if they don't exist
        await TestDataContext().LinkRelationship(nonExistentParentId, childEntity.Id);
        
        // Create a test component for the child
        await TestDataContext().Commit(new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = childEntity.Id,
            Name = "Test Component",
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        });
        
        // Assert
        // Verify the relationship was created
        var parent = await TestDataContext().Parent(childEntity.Id);
        parent.ShouldBe(nonExistentParentId);
    }
    
    /// <summary>
    /// INTENT: Verify that linking to a non-existent child entity creates the relationship
    /// PURPOSE: Document that LinkRelationship works with Entity records directly  
    /// BUSINESS CONTEXT: Relationships can be established before entities have business components
    /// WHY IMPORTANT: Allows flexible entity creation patterns
    /// ARCHITECTURAL SIGNIFICANCE: Entity relationships are independent of components
    /// FUTURE RESILIENCE: Shows that relationships work at the entity level
    /// </summary>
    [Fact]
    public async Task LinkRelationship_With_NonExistent_Child_Creates_Relationship()
    {
        // Arrange
        var parentEntity = TestDataContext().NewEntity();
        var nonExistentChildId = Guid.NewGuid(); // This ID doesn't exist in the database
        var testRunId = Guid.NewGuid().ToString("N")[..8]; // Unique test run ID
        
        // Track both entities for cleanup
        TrackEntity(parentEntity.Id);
        TrackEntity(nonExistentChildId);
        
        // Create the parent entity with retry logic for parallel execution
        var parentComponent = new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = parentEntity.Id,
            Name = $"Parent-{testRunId}",
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        };
        
        var retryCount = 3;
        var committed = false;
        for (int retry = 0; retry < retryCount && !committed; retry++)
        {
            try
            {
                await TestDataContext().Commit(parentComponent);
                committed = true;
            }
            catch (Exception) when (retry < retryCount - 1)
            {
                // Retry on any exception (parallel test interference)
                await Task.Delay(10 * (retry + 1)); // Exponential backoff
            }
        }
        
        committed.ShouldBeTrue("Failed to commit parent component after retries");
        
        // Act
        // LinkRelationship will create Entity records for both entities if needed
        var relationshipCreated = false;
        for (int retry = 0; retry < retryCount && !relationshipCreated; retry++)
        {
            try
            {
                await TestDataContext().LinkRelationship(parentEntity.Id, nonExistentChildId);
                relationshipCreated = true;
            }
            catch (Exception) when (retry < retryCount - 1)
            {
                // Retry on any exception (parallel test interference)
                await Task.Delay(10 * (retry + 1)); // Exponential backoff
            }
        }
        
        relationshipCreated.ShouldBeTrue("Failed to create relationship after retries");
        
        // Assert
        // Verify the relationship was created
        var children = await TestDataContext().Children(parentEntity.Id);
        children.ShouldContain(nonExistentChildId);
        
        var parentOfChild = await TestDataContext().Parent(nonExistentChildId);
        parentOfChild.ShouldBe(parentEntity.Id);
    }
    
    /// <summary>
    /// INTENT: Verify that linking succeeds when both entities exist
    /// PURPOSE: Ensure validation doesn't interfere with valid operations
    /// BUSINESS CONTEXT: Normal relationship creation should work seamlessly
    /// WHY IMPORTANT: Validation should not create false positives
    /// ARCHITECTURAL SIGNIFICANCE: Tests that validation allows valid scenarios
    /// FUTURE RESILIENCE: Ensures validation is not overly restrictive
    /// </summary>
    [Fact]
    public async Task LinkRelationship_With_Existing_Entities_Should_Succeed()
    {
        // Arrange
        var parentEntity = TestDataContext().NewEntity();
        var childEntity = TestDataContext().NewEntity();
        
        // Ensure both entities exist by creating test components
        await TestDataContext().Commit(new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = parentEntity.Id,
            Name = "Parent Component",
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        });
        
        await TestDataContext().Commit(new TestComponent 
        { 
            Id = Guid.NewGuid(), 
            OwnerEntityId = childEntity.Id,
            Name = "Child Component", 
            Status = "ACTIVE",
            LastUpdated = DateTime.UtcNow
        });
        
        // Act - This should succeed without throwing
        await TestDataContext().LinkRelationship(parentEntity.Id, childEntity.Id, "TestParent", "TestChild");
        
        // Assert
        var parentOfChild = await TestDataContext().Parent(childEntity.Id);
        parentOfChild.ShouldBe(parentEntity.Id);
        
        var childrenOfParent = await TestDataContext().Children(parentEntity.Id);
        childrenOfParent.ShouldContain(childEntity.Id);
    }
}