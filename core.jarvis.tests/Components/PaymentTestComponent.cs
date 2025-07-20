using core.jarvis.Data;

namespace core.jarvis.tests.Components
{
    public record PaymentTestComponent : IComponent, IVersionedComponent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OwnerEntityId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int? Version { get; set; }
    }
}