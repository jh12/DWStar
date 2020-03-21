using System.Collections.Generic;
using System.Diagnostics;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents.Dimensions;

namespace DAT10.StarModelComponents
{
    [DebuggerDisplay("{FactTable.Name}")]
    public class StarModel
    {
        //(facttable, confidence)-tupler
        public FactTable FactTable;
        public List<Dimension> Dimensions;
        public CommonModel OriginCommonModel { get; }

        public StarModel(FactTable factTable, CommonModel originCommonModel)
        {
            FactTable = factTable;
            Dimensions = new List<Dimension>();
            OriginCommonModel = originCommonModel;
        }

        public StarModel(FactTable factTable, List<Dimension> dimensions, CommonModel originCommonModel)
        {
            FactTable = factTable;
            Dimensions = dimensions;
            OriginCommonModel = originCommonModel;
        }
    }
}
