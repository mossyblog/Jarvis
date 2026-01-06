using core.jarvis.Data;
using core.jarvis.Exceptions;
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
        if (string.IsNullOrWhiteSpace(description))
            throw new ValidationException("description", "Description cannot be empty");
        if (estimatedHours <= 0)
            throw new ValidationException("estimatedHours", "Estimated hours must be positive");
        
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
        
        await DataContext.Commit(workOrder);
        _logger.LogInformation("Created draft work order {WorkOrderNumber}", workOrder.WorkOrderNumber);
        
        return workOrder;
    }
    
    /// <summary>
    /// Submits a draft work order for approval.
    /// </summary>
    public async Task<FakeWorkOrderComponent> Submit()
    {
        var workOrder = await GetRequired();
        
        if (workOrder.Status != WorkOrderStatus.Draft)
        {
            throw new BusinessRuleException("INVALID_STATE", $"Can only submit draft work orders. Current status: {workOrder.Status}");
        }
        
        var updated = workOrder with
        {
            Status = WorkOrderStatus.Submitted,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
        _logger.LogInformation("Submitted work order {WorkOrderNumber} for approval", workOrder.WorkOrderNumber);
        
        return updated;
    }
    
    /// <summary>
    /// Approves a submitted work order.
    /// </summary>
    public async Task<FakeWorkOrderComponent> Approve(Guid approverId)
    {
        var workOrder = await GetRequired();
        
        if (workOrder.Status != WorkOrderStatus.Submitted)
        {
            throw new BusinessRuleException("INVALID_STATE", $"Can only approve submitted work orders. Current status: {workOrder.Status}");
        }
        
        var updated = workOrder with
        {
            Status = WorkOrderStatus.Approved,
            ApprovedByAccountId = approverId,
            ApprovedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
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
        
        if (workOrder.Status != WorkOrderStatus.Approved)
        {
            throw new BusinessRuleException("INVALID_STATE", $"Can only assign approved work orders. Current status: {workOrder.Status}");
        }
        
        if (scheduledDate < DateTime.UtcNow)
        {
            throw new BusinessRuleException("INVALID_DATE", "Scheduled date must be in the future");
        }
        
        var updated = workOrder with
        {
            Status = WorkOrderStatus.Assigned,
            AssignedToAccountId = technicianId,
            ScheduledDate = scheduledDate,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
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
        
        if (workOrder.Status != WorkOrderStatus.Assigned)
        {
            throw new BusinessRuleException("INVALID_STATE", $"Can only start assigned work orders. Current status: {workOrder.Status}");
        }
        
        var updated = workOrder with
        {
            Status = WorkOrderStatus.InProgress,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
        _logger.LogInformation("Started work on order {WorkOrderNumber}", workOrder.WorkOrderNumber);
        
        return updated;
    }
    
    /// <summary>
    /// Completes work order with actual hours and notes.
    /// </summary>
    public async Task<FakeWorkOrderComponent> CompleteWork(decimal actualHours, string completionNotes)
    {
        var workOrder = await GetRequired();
        
        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new BusinessRuleException("INVALID_STATE", $"Can only complete in-progress work orders. Current status: {workOrder.Status}");
        }
        
        if (actualHours <= 0)
            throw new ValidationException("actualHours", "Actual hours must be positive");
        
        var updated = workOrder with
        {
            Status = WorkOrderStatus.Completed,
            ActualHours = actualHours,
            Notes = completionNotes,
            CompletedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
        _logger.LogInformation("Completed work order {WorkOrderNumber} in {ActualHours} hours", 
            workOrder.WorkOrderNumber, actualHours);
        
        return updated;
    }
    
    /// <summary>
    /// Cancels a work order with reason.
    /// </summary>
    public async Task<FakeWorkOrderComponent> Cancel(string reason)
    {
        var workOrder = await GetRequired();
        
        if (workOrder.Status == WorkOrderStatus.Completed || workOrder.Status == WorkOrderStatus.Cancelled)
        {
            throw new BusinessRuleException("INVALID_STATE", $"Cannot cancel work order in status: {workOrder.Status}");
        }
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("reason", "Cancellation reason is required");
        
        var updated = workOrder with
        {
            Status = WorkOrderStatus.Cancelled,
            CancellationReason = reason,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
        _logger.LogInformation("Cancelled work order {WorkOrderNumber}: {Reason}", 
            workOrder.WorkOrderNumber, reason);
        
        return updated;
    }
    
    /// <summary>
    /// Gets work order statistics.
    /// </summary>
    public async Task<WorkOrderStats> GetStatistics()
    {
        var allWorkOrders = await DataContext.Query()
            .WithAll<FakeWorkOrderComponent>(wo => true)
            .ToEntityComponents();
            
        var workOrders = allWorkOrders
            .Select(kv => kv.Value.Get<FakeWorkOrderComponent>())
            .Where(wo => wo != null)
            .ToList();
            
        var stats = new WorkOrderStats
        {
            TotalCount = workOrders.Count(),
            CompletedCount = workOrders.Count(wo => wo!.Status == WorkOrderStatus.Completed),
            InProgressCount = workOrders.Count(wo => wo!.Status == WorkOrderStatus.InProgress),
            OverdueCount = workOrders.Count(wo => 
                wo!.ScheduledDate.HasValue && 
                wo.ScheduledDate < DateTime.UtcNow && 
                wo.Status != WorkOrderStatus.Completed),
            TotalHours = workOrders.Where(wo => wo!.Status == WorkOrderStatus.Completed).Sum(wo => wo!.ActualHours)
        };
        
        if (stats.CompletedCount > 0)
        {
            var completedWorkOrders = workOrders.Where(wo => wo!.Status == WorkOrderStatus.Completed && wo.CompletedDate.HasValue);
            stats.AverageCompletionTime = (decimal)completedWorkOrders
                .Average(wo => (wo!.CompletedDate!.Value - wo.CreatedAt).TotalHours);
        }
        
        return stats;
    }
    
    private string GenerateWorkOrderNumber()
    {
        return $"WO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}