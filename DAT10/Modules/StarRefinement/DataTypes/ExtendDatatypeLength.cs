using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.StarModelComponents;

namespace DAT10.Modules.StarRefinement.DataTypes
{
    public class ExtendDatatypeLength : IStarRefinement
    {
        public string Name { get; } = "Extend data type length";
        public string Description { get; } = "Extends the data type length of strings with 10";

        public StarModel Refine(StarModel starModel)
        {
            foreach (var column in starModel.FactTable.Columns)
            {
                if(!column.DataType.IsString())
                    continue;

                column.DataType.Length += 10;
            }

            foreach (var dimension in starModel.Dimensions)
            {
                foreach (var column in dimension.Columns)
                {
                    if (!column.DataType.IsString())
                        continue;

                    column.DataType.Length += 10;
                }
            }

            return starModel;
        }
    }
}
