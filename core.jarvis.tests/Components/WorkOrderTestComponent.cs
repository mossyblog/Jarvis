using core.jarvis.Data;

namespace core.jarvis.tests.Components
{
    public record WorkOrderTestComponent : IComponent, IVersionedComponent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OwnerEntityId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPrePaymentRequired { get; set; }
        public string WorkOrderId { get; set; } = string.Empty;
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int? Version { get; set; }
    }
} 