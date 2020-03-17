using System.Collections.Generic;
using System.Linq;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Dimensions
{
    public class IsMeasure : DimensionalModuleBase
    {
        public override string Name { get; } = "Measures Numeric";
        public override string Description { get; } = "Find numeric measures by looking for columns that represent a numeric value.";

        public IsMeasure() : base(StarDependency.None, StarDependency.Measures)
        {
        }

        public override StarModel TranslateModel(StarModel starModel)
        {
            //TODO: overvej om man kan gøre sådan, at man finder dimensions først, og så bruger den i stedet for dette her.
            //TODO: Det ser pt. på alm. table references.
            //All fact tables across all star models
            //List<FactTable> allFactTables = starModels.Select(sm => sm.FactTable).ToList();

            //Within fact table -> check whether a column is a measure or not.
            List<StarColumn> nnColumns = starModel.FactTable.Columns.Where(c => c.WasNotNull()).ToList();

            //Assumption: If a column in the fact table is a foreign key to a dimension -> not a measure
            foreach (StarColumn col in nnColumns)
            {
                //If the current column is not a foreign key for any dimension of the fact table ...
                bool isForeignKeyFlag = starModel.Dimensions.Any(dim => col.WasForeignKeyInDimension(dim));
                //... then it is a measure
                if (isForeignKeyFlag == false)
                {
                    col.ColumnType |= col.DataType.IsNumeric()
                        ? StarColumnType.NumericMeasure
                        : StarColumnType.DescriptiveMeasure;
                }
            }

            return starModel;
        }
    }
}
