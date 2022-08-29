namespace AetherCompass.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CompassTypeAttribute : Attribute
    {
        public readonly CompassType Type;
        public CompassTypeAttribute(CompassType type)
        {
            Type = type;
        }
    }


    public enum CompassType : byte
    {
        Unknown = 0,
        Standard,
        Experimental,
        Debug,
        Invalid,
    }
}
