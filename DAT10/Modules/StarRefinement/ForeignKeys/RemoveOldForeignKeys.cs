using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;

namespace DAT10.Modules.StarRefinement.ForeignKeys
{
    public class RemoveOldForeignKeys : IStarRefinement
    {
        public string Name { get; } = "Remove old foreign keys";
        public string Description { get; } = "Remove foreign keys from data source";

        public StarModel Refine(StarModel starModel)
        {
            RemoveForeignKeyColumns(starModel.FactTable);

            foreach (var dimension in starModel.Dimensions.Where(d => !(d is DateDimension || d is TimeDimension)))
            {
                RemoveForeignKeyColumns(dimension);
            }

            return starModel;
        }

        private void RemoveForeignKeyColumns(StarModelTableBase table)
        {
            // Iterate through all relations where this table contain the foreign key(s)
            foreach (var linkRelation in table.TableReference.Relations.Where(r => Equals(r.LinkTable, table.TableReference)))
            {
                // Remove each foreign key from table
                foreach (var linkColumn in linkRelation.LinkColumns)
                {
                    table.Columns.RemoveAll(c => Equals(c.ColumnRef, linkColumn));
                }
            }
        }
    }
}
