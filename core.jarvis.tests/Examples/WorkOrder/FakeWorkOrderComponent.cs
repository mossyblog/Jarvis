using core.jarvis.Data;

namespace core.jarvis.tests.Examples.WorkOrder;

/// <summary>
/// Work order status enum representing the state machine.
/// </summary>
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

/// <summary>
/// Work order priority levels.
/// </summary>
public enum WorkOrderPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Statistics for work orders (entity-scoped).
/// </summary>
public class WorkOrderStats
{
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal TotalHours { get; set; }
    public decimal AverageCompletionTime { get; set; }
}

/// <summary>
/// Component representing a work order entity.
/// Demonstrates state machine pattern with complex business rules.
/// </summary>
public record FakeWorkOrderComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Normal;

    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public Guid? ApprovedByAccountId { get; set; }
    public Guid? AssignedToAccountId { get; set; }

    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
}
