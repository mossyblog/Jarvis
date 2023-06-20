using FJarvis.Data.Data;
using Serilog.Events;

namespace FJarvis.Data.Traits
{
    public class JournalInfo : IEntity
    {
        public JournalInfo()
        {
            Id = default;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public HashSet<HeaderInfo> Headers { get; set; } = new HashSet<HeaderInfo>();
        
        public HashSet<LogEvent> Entries { get; set; } = new HashSet<LogEvent>();
        public HashSet<EntityInfo> Entities { get; set; } = new HashSet<EntityInfo>();
        public HashSet<ITrait> Traits { get; set; } = new HashSet<ITrait>();
        
    }
}