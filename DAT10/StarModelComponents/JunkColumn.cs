using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.StarModelComponents
{
    public class JunkColumn : StarColumn
    {
        public StarColumn MovedColumn;

        public JunkColumn(StarColumn movedColumn) : base(movedColumn.ColumnRef)
        {
            MovedColumn = movedColumn;
        }
    }
}
