using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using PrimaryKey = DAT10.StarModelComponents.PrimaryKey;

namespace DAT10.Modules.StarRefinement.FactRelations
{
    public class FactRelationColumns : IStarRefinement
    {
        public string Name { get; } = "Fact relation columns";
        public string Description { get; } = "Create columns in fact table for dimension relations";

        public StarModel Refine(StarModel starModel)
        {
            var factTable = starModel.FactTable;

            int ordinalOffset = 0;

            List<StarColumn> foreignColumns = new List<StarColumn>();

            // Create a column for each relation that lacks link columns
            foreach (var relation in factTable.Relations.Where(r => Equals(r.LinkTable, factTable) && !r.HasLinkColumns()))
            {
                // Create column
                var foreignColumn = new StarColumn(++ordinalOffset, $"{relation.AnchorTable.Name}_Key", new DataType(OleDbType.Integer), StarColumnType.Key);
                foreignColumn.TableRef = factTable;

                // Add to list of foreign columns, and add to relation
                foreignColumns.Add(foreignColumn);
                relation.LinkColumns.Add(foreignColumn);
            }

            // Shift ordinals of existing columns and insert foreign columns
            factTable.Columns.ForEach(c => c.Ordinal += ordinalOffset);
            foreignColumns.ForEach(c => factTable.Columns.Insert(c.Ordinal-1, c));

            factTable.Constraints.PrimaryKey = new PrimaryKey(foreignColumns);

            return starModel;
        }
    }
}
