using FJarvis.Data.Traits;
using FJarvis.Data.Utils;

namespace FJarvis.Data.Data
{
    public struct HeaderInfo
    {
        public Guid EntityId { get; set; }
        public string Bitmask { get; set; }
        
        public Guid CorrelationId { get; set; }
    }
    
    public static class HeaderInfoExtensions
    {
        public static bool HasTrait<T>(this HeaderInfo header) where T : ITrait 
        {
            var flags = EntityHelper.DecompressBitmask(header.Bitmask);
            var typeDefault = default(T);
            return flags[typeDefault.Index];
        }

        public static bool HasTrait<T>(this HashSet<HeaderInfo> headers) where T : ITrait
        {
            return headers.Any(header => header.HasTrait<T>());
        }
    }
}