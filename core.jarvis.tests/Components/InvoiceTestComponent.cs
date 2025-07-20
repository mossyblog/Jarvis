using core.jarvis.Data;

namespace core.jarvis.tests.Components
{
    public record InvoiceTestComponent : IComponent, IVersionedComponent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OwnerEntityId { get; set; }
        public Guid WorkOrderId { get; set; } // To conceptually link to WorkOrder
        public string InvoiceNumber { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int? Version { get; set; }
    }
} 