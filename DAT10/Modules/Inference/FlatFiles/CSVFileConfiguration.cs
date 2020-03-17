using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Modules.Inference.FlatFiles
{
    public class CSVFileConfiguration
    {
        // How many rows to examine when getting metadata
        public int RowsToExamine = 10;

        public string RowDelimiter = ",";
    }
}
