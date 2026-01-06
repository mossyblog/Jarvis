using core.jarvis.Data;

namespace core.jarvis.tests.Components
{
    public record PaymentTestComponent : IComponent, IVersionedComponent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OwnerEntityId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int? Version { get; set; }
    }
}