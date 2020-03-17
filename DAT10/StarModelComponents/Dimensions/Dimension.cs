using System.Collections.Generic;
using System.Diagnostics;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents.Dimensions
{
    [DebuggerDisplay("{Name}")]
    public class Dimension : StarModelTableBase
    {
        public Dimension(Table tableRef) : base(tableRef)
        {
        }

        public Dimension(StarModelTableBase basedOn, bool isRolePlaying = true) : base(basedOn, isRolePlaying)
        {
        }

        public Dimension(string name, List<StarColumn> columns, Table tableReference) : base(name, columns, tableReference)
        {
        }
    }
}
