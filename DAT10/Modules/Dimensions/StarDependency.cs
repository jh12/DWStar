using System;

namespace DAT10.Modules.Dimensions
{
    [Flags]
    public enum StarDependency : short
    {
        None = 0,
        Dimensions = 1,
        Hierarchies = 2,
        Measures = 4,
        ALL = Dimensions | Hierarchies | Measures
    }
}
