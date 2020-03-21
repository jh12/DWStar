using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;

namespace DAT10.Modules.StarRefinement.OrderColumns
{
    public class OrderColumns : IStarRefinement
    {
        public string Name { get; } = "Order Columns";
        public string Description { get; } = "Changes ordinal of columns such that primary keys come first, and all other columns are alphabetically sorted.";
        public StarModel Refine(StarModel starModel)
        {
            // Order columns
            FactTable fact = starModel.FactTable;
            fact.Columns = fact.Columns.OrderByDescending(x => (short)x.ColumnType & 3).ThenBy(x => x.Name).ToList();
            foreach (Dimension dim in starModel.Dimensions)
            {
                if(dim is TimeDimension || dim is DateDimension)
                    continue;
                
                dim.Columns = dim.Columns.OrderByDescending(x => (short)x.ColumnType & 3).ThenBy(x => x.Name).ToList();
            }
            return starModel;
        }
    }
}
