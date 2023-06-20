using FJarvis.Data.Traits;

namespace FJarvis.Data.Data
{
    public class TraitType
    {
        private readonly bool _readonly;
        public ITrait Type { get; private set; }

        private TraitType(ITrait type, bool readOnly)
        {
            Type = type;
            _readonly = readOnly;
        }

        public static TraitType ReadWrite<T>() where T : ITrait, new()
        {
            return new TraitType(new T(), false);
        }

        public static TraitType ReadOnly<T>() where T : ITrait, new()
        {
            return new TraitType(new T(), true);
        }
    }

}