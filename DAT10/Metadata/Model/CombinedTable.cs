using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Metadata.Model
{
    public class CombinedTable : Table
    {
        public bool IsCombinedTable = true;
        public List<Table> TableReference;
        public CombinedTable(string name, string schema, int rowCount, List<Table> tableReferences) : base(name, schema, rowCount)
        {
            TableReference = tableReferences;
        }
    }
}
