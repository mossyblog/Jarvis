using core.jarvis.tests.Components;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Examples;

/// <summary>
/// Demonstrates how to use EntityRelationship component to track
/// parent-child relationships between WorkOrders and Invoices.
/// </summary>
public class WorkOrderInvoiceExample : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Demonstrate how to establish and query parent-child relationships
    /// PURPOSE: Show real-world usage of EntityRelationship component
    /// BUSINESS CONTEXT: Work orders can have multiple invoices associated with them
    /// WHY IMPORTANT: Hierarchical relationships are common in business applications
    /// ARCHITECTURAL SIGNIFICANCE: Shows how ECS pattern handles entity relationships
    /// FUTURE RESILIENCE: Provides pattern for any parent-child relationship needs
    /// </summary>
    [Fact]
    public async Task Example_WorkOrder_With_Multiple_Invoices()
    {
        // Create a work order
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrderTestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = workOrderId,
            WorkOrderId = "WO-2024-001",
            WorkOrderNumber = "WO-2024-001",
            Description = "Install new HVAC system",
            Status = "In Progress",
            IsPrePaymentRequired = false
        };
        await TestDataContext().Commit(workOrder);
        
        // Create multiple invoices for the work order
        var invoice1Id = Guid.NewGuid();
        var invoice1 = new InvoiceTestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = invoice1Id,
            WorkOrderId = workOrderId,
            InvoiceNumber = "INV-2024-001",
            Amount = 5000,
            Status = "Pending",
            DueDate = DateTime.UtcNow.AddDays(30)
        };
        await TestDataContext().Commit(invoice1);
        
        var invoice2Id = Guid.NewGuid();
        var invoice2 = new InvoiceTestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = invoice2Id,
            WorkOrderId = workOrderId,
            InvoiceNumber = "INV-2024-002",
            Amount = 3000,
            Status = "Paid"
        };
        await TestDataContext().Commit(invoice2);
        
        // Establish parent-child relationships
        await TestDataContext().LinkRelationship(workOrderId, invoice1Id, "WorkOrder", "Invoice");
        await TestDataContext().LinkRelationship(workOrderId, invoice2Id, "WorkOrder", "Invoice");
        
        // Query relationships
        var invoiceIds = await TestDataContext().Children(workOrderId);
        invoiceIds.Count.ShouldBe(2);
        invoiceIds.ShouldContain(invoice1Id);
        invoiceIds.ShouldContain(invoice2Id);
        
        // Check parent from invoice perspective
        var workOrderIdFromInvoice = await TestDataContext().Parent(invoice1Id);
        workOrderIdFromInvoice.ShouldBe(workOrderId);
        
        // Verify relationship check
        var isChild = await TestDataContext().ChildOf(invoice1Id, workOrderId);
        isChild.ShouldBeTrue();
    }
    
    /// <summary>
    /// INTENT: Demonstrate hierarchical queries across multiple levels
    /// PURPOSE: Show how to navigate complex entity hierarchies
    /// BUSINESS CONTEXT: Projects contain work orders which contain invoices
    /// WHY IMPORTANT: Multi-level hierarchies are common in enterprise applications
    /// ARCHITECTURAL SIGNIFICANCE: Tests recursive relationship traversal
    /// FUTURE RESILIENCE: Ensures deep hierarchies are supported
    /// </summary>
    [Fact]
    public async Task Example_Multi_Level_Hierarchy()
    {
        // Create a project -> work order -> invoice hierarchy
        var projectId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        
        // Establish relationships
        await TestDataContext().LinkRelationship(projectId, workOrderId, "Project", "WorkOrder");
        await TestDataContext().LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");
        
        // Get all ancestors of invoice (should include work order and project)
        var ancestors = await TestDataContext().Ancestors(invoiceId);
        ancestors.Count.ShouldBe(2);
        ancestors[0].ShouldBe(workOrderId); // Immediate parent
        ancestors[1].ShouldBe(projectId);   // Grandparent
        
        // Get all descendants of project (should include work order and invoice)
        var descendants = await TestDataContext().Descendants(projectId);
        descendants.Count.ShouldBe(2);
        descendants.ShouldContain(workOrderId);
        descendants.ShouldContain(invoiceId);
    }
    
    /// <summary>
    /// INTENT: Demonstrate relationship removal when cancelling work orders
    /// PURPOSE: Show how to properly clean up relationships
    /// BUSINESS CONTEXT: Cancelled work orders may need to disassociate invoices
    /// WHY IMPORTANT: Relationship cleanup is crucial for data integrity
    /// ARCHITECTURAL SIGNIFICANCE: Tests bidirectional relationship updates
    /// FUTURE RESILIENCE: Ensures relationships can be modified over time
    /// </summary>
    [Fact]
    public async Task Example_Remove_Invoice_From_WorkOrder()
    {
        // Setup work order with invoice
        var workOrderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        await TestDataContext().LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");
        
        // Verify relationship exists
        var childrenBefore = await TestDataContext().Children(workOrderId);
        childrenBefore.ShouldContain(invoiceId);
        
        // Remove the relationship (e.g., invoice was cancelled)
        await TestDataContext().UnlinkRelationship(workOrderId, invoiceId);
        
        // Verify relationship is removed
        var childrenAfter = await TestDataContext().Children(workOrderId);
        childrenAfter.ShouldNotContain(invoiceId);
        
        var parentAfter = await TestDataContext().Parent(invoiceId);
        parentAfter.ShouldBeNull();
    }
    
    /// <summary>
    /// INTENT: Demonstrate querying entities with their relationships
    /// PURPOSE: Show how to combine entity queries with relationship data
    /// BUSINESS CONTEXT: Reports often need to show work orders with invoice counts
    /// WHY IMPORTANT: Efficient querying of related data is critical for performance
    /// ARCHITECTURAL SIGNIFICANCE: Shows integration between EntityQuery and relationships
    /// FUTURE RESILIENCE: Provides pattern for complex relationship-based queries
    /// </summary>
    [Fact]
    public async Task Example_Query_WorkOrders_With_Invoice_Count()
    {
        // Create multiple work orders with varying invoice counts
        var workOrder1Id = Guid.NewGuid();
        var workOrder2Id = Guid.NewGuid();
        
        // WorkOrder 1 has 3 invoices
        for (int i = 0; i < 3; i++)
        {
            var invoiceId = Guid.NewGuid();
            await TestDataContext().LinkRelationship(workOrder1Id, invoiceId, "WorkOrder", "Invoice");
        }
        
        // WorkOrder 2 has 1 invoice
        await TestDataContext().LinkRelationship(workOrder2Id, Guid.NewGuid(), "WorkOrder", "Invoice");
        
        // Query invoice counts
        var workOrder1Invoices = await TestDataContext().Children(workOrder1Id);
        var workOrder2Invoices = await TestDataContext().Children(workOrder2Id);
        
        workOrder1Invoices.Count.ShouldBe(3);
        workOrder2Invoices.Count.ShouldBe(1);
        
        // In a real application, you might combine this with entity queries:
        // var workOrders = await TestDataContext().Query()
        //     .WithAll<WorkOrderTestComponent>()
        //     .ToEntityComponents();
        // 
        // foreach (var wo in workOrders)
        // {
        //     var invoiceCount = (await TestDataContext().Children(wo.EntityId)).Count;
        //     // Use invoice count for reporting...
        // }
    }
}