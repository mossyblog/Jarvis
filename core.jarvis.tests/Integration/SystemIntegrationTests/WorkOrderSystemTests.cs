using core.jarvis.Exceptions;
using core.jarvis.Systems;
using core.jarvis.tests.Examples.WorkOrder;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace core.jarvis.tests.Integration.SystemIntegrationTests;

/// <summary>
/// Integration tests for WorkOrderHandler through the System layer.
/// Demonstrates complex business workflow orchestration.
/// </summary>
public class WorkOrderSystemTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify complete work order lifecycle through System orchestration.
    /// PURPOSE: Ensure System pattern works for complex business workflows.
    /// BUSINESS CONTEXT: Work orders follow a defined state machine through their lifecycle.
    /// WHY IMPORTANT: Validates that System can handle multi-step business processes.
    /// ARCHITECTURAL SIGNIFICANCE: Demonstrates System's ability to orchestrate complex handlers.
    /// FUTURE RESILIENCE: Ensures business workflows remain testable without HTTP context.
    /// </summary>
    [Fact]
    public async Task System_WorkOrderLifecycle_CompletesSuccessfully()
    {
        // Arrange
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Act - Create draft work order
        var draft = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.CreateDraft("Fix server room AC", WorkOrderPriority.High, 4.5m));
        
        // Assert draft created
        draft.ShouldNotBeNull();
        draft.Status.ShouldBe(WorkOrderStatus.Draft);
        draft.Description.ShouldBe("Fix server room AC");
        draft.Priority.ShouldBe(WorkOrderPriority.High);
        draft.EstimatedHours.ShouldBe(4.5m);
        draft.WorkOrderNumber.ShouldStartWith("WO-");
        
        // Act - Submit for approval
        var submitted = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.Submit());
        
        // Assert submitted
        submitted.Status.ShouldBe(WorkOrderStatus.Submitted);
        
        // Act - Approve
        var approverId = Guid.NewGuid();
        var approved = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.Approve(approverId));
        
        // Assert approved
        approved.Status.ShouldBe(WorkOrderStatus.Approved);
        approved.ApprovedByAccountId.ShouldBe(approverId);
        approved.ApprovedDate.ShouldNotBeNull();
        
        // Act - Assign technician
        var technicianId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.AddDays(2);
        var assigned = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.AssignTo(technicianId, scheduledDate));
        
        // Assert assigned
        assigned.AssignedToAccountId.ShouldBe(technicianId);
        assigned.ScheduledDate.ShouldBe(scheduledDate);
        
        // Act - Start work
        var started = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.StartWork());
        
        // Assert started
        started.Status.ShouldBe(WorkOrderStatus.InProgress);
        
        // Act - Complete work
        var completed = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.CompleteWork(3.5m, "AC unit repaired and tested"));
        
        // Assert completed
        completed.Status.ShouldBe(WorkOrderStatus.Completed);
        completed.ActualHours.ShouldBe(3.5m);
        completed.CompletedDate.ShouldNotBeNull();
        completed.Notes.ShouldContain("AC unit repaired and tested");
    }
    
    /// <summary>
    /// INTENT: Verify business rule enforcement through System layer.
    /// PURPOSE: Ensure System properly propagates business rule violations.
    /// BUSINESS CONTEXT: Work orders must follow valid state transitions.
    /// WHY IMPORTANT: Validates that business rules are enforced through System.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms System doesn't mask handler exceptions.
    /// FUTURE RESILIENCE: Ensures business rules remain consistent.
    /// </summary>
    [Fact]
    public async Task System_InvalidStateTransition_ThrowsBusinessRuleException()
    {
        // Arrange
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Create draft
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.CreateDraft("Test work order", WorkOrderPriority.Normal, 2m));
        
        // Act & Assert - Try to start work without approval
        await Should.ThrowAsync<BusinessRuleException>(async () =>
        {
            await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
                entityId,
                handler => handler.StartWork());
        });
    }
    
    /// <summary>
    /// INTENT: Verify work order statistics calculation through System.
    /// PURPOSE: Test that reporting methods work through System orchestration.
    /// BUSINESS CONTEXT: Management needs work order performance metrics.
    /// WHY IMPORTANT: Validates that read-only operations work through System.
    /// ARCHITECTURAL SIGNIFICANCE: Shows System handles different return types.
    /// FUTURE RESILIENCE: Ensures reporting remains functional.
    /// </summary>
    [Fact]
    public async Task System_GetWorkOrderStatistics_ReturnsCorrectMetrics()
    {
        // Arrange
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Create and complete a work order
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.CreateDraft("Calculate metrics", WorkOrderPriority.Low, 10m));
        
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.Submit());
        
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.Approve(Guid.NewGuid()));
        
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.AssignTo(Guid.NewGuid(), DateTime.UtcNow.AddDays(1)));
        
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.StartWork());
        
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.CompleteWork(8m, "Completed efficiently"));
        
        // Act - Get statistics
        var stats = await system.ExecuteHandlerWithResult<WorkOrderHandler, WorkOrderStats>(
            entityId,
            handler => handler.GetStatistics());
        
        // Assert
        stats.ShouldNotBeNull();
        stats.TotalCount.ShouldBeGreaterThan(0);
        stats.CompletedCount.ShouldBe(1);
        stats.InProgressCount.ShouldBe(0);
        stats.TotalHours.ShouldBe(8m);
    }
    
    /// <summary>
    /// INTENT: Verify work order cancellation through System.
    /// PURPOSE: Test alternative workflow paths through System.
    /// BUSINESS CONTEXT: Work orders can be cancelled at various stages.
    /// WHY IMPORTANT: Validates that System handles all business scenarios.
    /// ARCHITECTURAL SIGNIFICANCE: Shows System flexibility for different workflows.
    /// FUTURE RESILIENCE: Ensures edge cases remain supported.
    /// </summary>
    [Fact]
    public async Task System_CancelWorkOrder_UpdatesStatusWithReason()
    {
        // Arrange
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Create and submit work order
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.CreateDraft("To be cancelled", WorkOrderPriority.Normal, 5m));
        
        await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.Submit());
        
        // Act - Cancel
        var cancelled = await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
            entityId,
            handler => handler.Cancel("Customer changed requirements"));
        
        // Assert
        cancelled.Status.ShouldBe(WorkOrderStatus.Cancelled);
        cancelled.CancellationReason.ShouldBe("Customer changed requirements");
    }
    
    /// <summary>
    /// INTENT: Verify validation rules are enforced through System.
    /// PURPOSE: Ensure input validation works through System layer.
    /// BUSINESS CONTEXT: Work orders require valid data for operations.
    /// WHY IMPORTANT: Validates that validation exceptions propagate correctly.
    /// ARCHITECTURAL SIGNIFICANCE: Shows System doesn't interfere with validation.
    /// FUTURE RESILIENCE: Ensures data quality rules remain enforced.
    /// </summary>
    [Fact]
    public async Task System_InvalidWorkOrderData_ThrowsValidationException()
    {
        // Arrange
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Act & Assert - Invalid estimated hours
        var exception = await Should.ThrowAsync<Exception>(async () =>
        {
            await system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
                entityId,
                handler => handler.CreateDraft("Invalid", WorkOrderPriority.Normal, -5m));
        });
        
        exception.Message.ShouldContain("Validation failed for estimatedHours");
    }
}