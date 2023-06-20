using System.Collections;
using FJarvis.Data.Traits;

namespace FJarvis.Data.Data
{
    public class EntityQueryDesc
    {
        public TraitType[] All { get; set; } = Array.Empty<TraitType>();
        public TraitType[] Any { get; set; } = Array.Empty<TraitType>();
        public  TraitType[]  None { get; set; }= Array.Empty<TraitType>();

        public string ToBitmask()
        {
            return string.Empty;
        }
    }
    
    public class EntityQuery<T> where T : EntityInfo
    {
        private readonly Func<T, bool> _predicate;

        public EntityQuery(Func<T, bool> predicate)
        {
            _predicate = predicate;
        }

        public HashSet<T> Execute(HashSet<T> entities)
        {
            return new HashSet<T>(entities.Where(_predicate));
        }
    }
}