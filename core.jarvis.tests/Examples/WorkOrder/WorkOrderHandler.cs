using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Examples.WorkOrder;

/// <summary>
/// Example handler demonstrating complex business workflow orchestration.
/// Shows state machine transitions, validation, and business rules.
/// </summary>
public class WorkOrderHandler : ComponentHandler<FakeWorkOrderComponent>
{
    private readonly ILogger<WorkOrderHandler> _logger;
    
    public WorkOrderHandler(IDataContext dataContext, ILogger<WorkOrderHandler> logger) 
        : base(dataContext, logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a new work order in draft status.
    /// </summary>
    public async Task<FakeWorkOrderComponent> CreateDraft(string description, WorkOrderPriority priority, decimal estimatedHours)
    {
        Guard.AgainstEmpty(description, nameof(description));
        Guard.AgainstOutOfRange(estimatedHours, 0.01m, 10000m, nameof(estimatedHours));

        var workOrder = new FakeWorkOrderComponent
        {
            OwnerEntityId = OwnerEntityId,
            WorkOrderNumber = GenerateWorkOrderNumber(),
            Description = description,
            Status = WorkOrderStatus.Draft,
            Priority = priority,
            EstimatedHours = estimatedHours,
            CreatedAt = DateTime.UtcNow
        };

        await TryCommit(workOrder);
        _logger.LogInformation("Created draft work order {WorkOrderNumber}", workOrder.WorkOrderNumber);

        return workOrder;
    }
    
    /// <summary>
    /// Submits a draft work order for approval.
    /// </summary>
    public async Task<FakeWorkOrderComponent> Submit()
    {
        var workOrder = await GetRequired();
        Ensure(workOrder.Status == WorkOrderStatus.Draft,
            $"Can only submit draft work orders. Current status: {workOrder.Status}");

        var updated = workOrder with { Status = WorkOrderStatus.Submitted };

        await TryCommit(updated);
        _logger.LogInformation("Submitted work order {WorkOrderNumber} for approval", workOrder.WorkOrderNumber);

        return updated;
    }
    
    /// <summary>
    /// Approves a submitted work order.
    /// </summary>
    public async Task<FakeWorkOrderComponent> Approve(Guid approverId)
    {
        var workOrder = await GetRequired();
        Ensure(workOrder.Status == WorkOrderStatus.Submitted,
            $"Can only approve submitted work orders. Current status: {workOrder.Status}");

        var updated = workOrder with
        {
            Status = WorkOrderStatus.Approved,
            ApprovedByAccountId = approverId,
            ApprovedDate = DateTime.UtcNow
        };

        await TryCommit(updated);
        _logger.LogInformation("Work order {WorkOrderNumber} approved by {ApproverId}",
            workOrder.WorkOrderNumber, approverId);

        return updated;
    }
    
    /// <summary>
    /// Assigns work order to a technician.
    /// </summary>
    public async Task<FakeWorkOrderComponent> AssignTo(Guid technicianId, DateTime scheduledDate)
    {
        var workOrder = await GetRequired();
        Ensure(workOrder.Status == WorkOrderStatus.Approved,
            $"Can only assign approved work orders. Current status: {workOrder.Status}");
        Ensure(scheduledDate >= DateTime.UtcNow, "Scheduled date must be in the future");

        var updated = workOrder with
        {
            Status = WorkOrderStatus.Assigned,
            AssignedToAccountId = technicianId,
            ScheduledDate = scheduledDate
        };

        await TryCommit(updated);
        _logger.LogInformation("Work order {WorkOrderNumber} assigned to {TechnicianId}",
            workOrder.WorkOrderNumber, technicianId);

        return updated;
    }
    
    /// <summary>
    /// Starts work on an assigned work order.
    /// </summary>
    public async Task<FakeWorkOrderComponent> StartWork()
    {
        var workOrder = await GetRequired();
        Ensure(workOrder.Status == WorkOrderStatus.Assigned,
            $"Can only start assigned work orders. Current status: {workOrder.Status}");

        var updated = workOrder with { Status = WorkOrderStatus.InProgress };

        await TryCommit(updated);
        _logger.LogInformation("Started work on order {WorkOrderNumber}", workOrder.WorkOrderNumber);

        return updated;
    }
    
    /// <summary>
    /// Completes work order with actual hours and notes.
    /// </summary>
    public async Task<FakeWorkOrderComponent> CompleteWork(decimal actualHours, string completionNotes)
    {
        Guard.AgainstOutOfRange(actualHours, 0.01m, 10000m, nameof(actualHours));

        var workOrder = await GetRequired();
        Ensure(workOrder.Status == WorkOrderStatus.InProgress,
            $"Can only complete in-progress work orders. Current status: {workOrder.Status}");

        var updated = workOrder with
        {
            Status = WorkOrderStatus.Completed,
            ActualHours = actualHours,
            Notes = completionNotes,
            CompletedDate = DateTime.UtcNow
        };

        await TryCommit(updated);
        _logger.LogInformation("Completed work order {WorkOrderNumber} in {ActualHours} hours",
            workOrder.WorkOrderNumber, actualHours);

        return updated;
    }
    
    /// <summary>
    /// Cancels a work order with reason.
    /// </summary>
    public async Task<FakeWorkOrderComponent> Cancel(string reason)
    {
        Guard.AgainstEmpty(reason, nameof(reason));

        var workOrder = await GetRequired();
        Ensure(workOrder.Status != WorkOrderStatus.Completed && workOrder.Status != WorkOrderStatus.Cancelled,
            $"Cannot cancel work order in status: {workOrder.Status}");

        var updated = workOrder with
        {
            Status = WorkOrderStatus.Cancelled,
            CancellationReason = reason
        };

        await TryCommit(updated);
        _logger.LogInformation("Cancelled work order {WorkOrderNumber}: {Reason}",
            workOrder.WorkOrderNumber, reason);

        return updated;
    }

    private string GenerateWorkOrderNumber()
    {
        return $"WO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}