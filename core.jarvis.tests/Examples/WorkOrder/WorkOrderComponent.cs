using core.jarvis.Data;

namespace core.jarvis.tests.Examples.WorkOrder;

/// <summary>
/// Example component for testing work order functionality.
/// Demonstrates a complex business entity with state machine.
/// </summary>
public record WorkOrderComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public WorkOrderStatus Status { get; init; } = WorkOrderStatus.Draft;
    public WorkOrderPriority Priority { get; init; } = WorkOrderPriority.Normal;
    
    public Guid? AssignedToAccountId { get; init; }
    public DateTime? ScheduledDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    
    public decimal EstimatedHours { get; init; }
    public decimal ActualHours { get; init; }
    
    public string? Notes { get; init; }
    
    public Guid? ApprovedByAccountId { get; init; }
    public DateTime? ApprovedDate { get; init; }
    
    public string? CancellationReason { get; init; }
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public enum WorkOrderStatus
{
    Draft,
    Submitted,
    Approved,
    Assigned,
    InProgress,
    Completed,
    Cancelled
}

public enum WorkOrderPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public class WorkOrderStats
{
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal AverageCompletionTime { get; set; }
    public decimal TotalHours { get; set; }
}