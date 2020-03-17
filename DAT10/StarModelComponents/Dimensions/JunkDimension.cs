using System.Collections.Generic;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents.Dimensions
{
    public class JunkDimension : Dimension
    {
        public JunkDimension(Table tableRef) : base(tableRef)
        {
        }

        public JunkDimension(StarModelTableBase basedOn, bool isRolePlaying = true) : base(basedOn, isRolePlaying)
        {
        }

        public JunkDimension(string name, List<StarColumn> columns, Table tableReference) : base(name, columns, tableReference)
        {
        }
    }
}
