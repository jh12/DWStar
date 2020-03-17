using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Modules.Refinement
{
    [Flags]
    public enum CommonDependency : short
    {
        None = 0,
        Name = 1,
        DataType = 2,
        NonNullable = 4,
        Unique = 8,
        PrimaryKey = 16,
        Relations = 32,
        Cardinality = 64,
        ALL = Name | DataType | NonNullable | Unique | PrimaryKey | Relations | Cardinality
    }
}
