using System;
using System.Data.OleDb;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using PrimaryKey = DAT10.StarModelComponents.PrimaryKey;

namespace DAT10.Modules.StarRefinement.SurrogateKeys
{
    public class SurrogateKeyCreator : IStarRefinement
    {
        public string Name { get; } = "Add Surrogate Key";
        public string Description { get; } = "Add surrogate keys to each dimension table.";

        public StarModel Refine(StarModel starModel)
        {
            AddSurrogateKey(starModel.FactTable);

            // Add surrogate keys to all dimensions
            foreach (var dimension in starModel.Dimensions.Where(d => !d.HasKey()))
            {
                AddSurrogateKey(dimension);
            }

            return starModel;
        }

        private static void AddSurrogateKey(StarModelTableBase dimension)
        {
            // Increment ordinal of all columns
            dimension.Columns.ForEach(c => c.Ordinal++);

            var surrogateKey = new StarColumn(1, "SurKey", new DataType(OleDbType.Integer), StarColumnType.Key | StarColumnType.SurrogateKey);
            surrogateKey.TableRef = dimension;

            // Insert surrogatekey column
            dimension.Columns.Insert(0, surrogateKey);

            // Add new surrogatekey to all anchor relations that lacks an anchorcolumn
            foreach (var relation in dimension.Relations.Where(r => Equals(r.AnchorTable, dimension) && !r.HasAnchorColumns()))
            {
                relation.AnchorColumns.Add(surrogateKey);
            }

            dimension.Constraints.PrimaryKey.Columns.Add(surrogateKey);
            dimension.Constraints.NotNullables.Add(new StarModelComponents.NotNullable(surrogateKey));
        }
    }
}
