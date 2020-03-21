using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;
using Unique = DAT10.StarModelComponents.Unique;

namespace DAT10.Modules.StarRefinement.JunkDimensions
{
    public class AddJunkDimension : IStarRefinement
    {
        public string Name { get; } = "Add Junk Dimension";
        public string Description { get; } = "Moves the descriptive columns from the fact table to a junk dimension.";
        public StarModel Refine(StarModel starModel)
        {
            FactTable fact = starModel.FactTable;

            // Create new dimension
            List<StarColumn> stringColumns = new List<StarColumn>();

            if (fact.Columns.Count(x => x.DataType.IsString()) == 0)
                return starModel;

            var surrogateKey = new StarColumn(1, "SurKey", new DataType(OleDbType.Integer), StarColumnType.Key | StarColumnType.SurrogateKey);
            stringColumns.Add(surrogateKey);

            var junkColumns = fact.Columns.Where(x => x.DataType.IsString()).Select(c => new JunkColumn(c));
            stringColumns.AddRange(junkColumns);

            JunkDimension dimension = new JunkDimension($"Junk_{fact.Name}", stringColumns, fact.TableReference);
            surrogateKey.TableRef = dimension;
            dimension.Constraints.PrimaryKey.Columns.Add(surrogateKey);

            dimension.Constraints.Uniques.Add(new Unique(stringColumns));

            // Fact relation to new dimension
            var foreignColumn = new StarColumn(0, $"{dimension.Name}_Key", new DataType(OleDbType.Integer), StarColumnType.Key);
            foreignColumn.TableRef = fact;
            fact.Columns.Add(foreignColumn);

            // Remove string columns from fact table
            starModel.FactTable.Columns.RemoveAll(c => c.DataType.IsString());

            StarRelation r = new StarRelation(dimension, starModel.FactTable, new List<StarColumn>() { surrogateKey}, new List<StarColumn>() { foreignColumn }, Cardinality.ManyToOne);
            fact.Relations.Add(r);
            dimension.Relations.Add(r);

            starModel.Dimensions.Add(dimension);

            return starModel;
        }
    }
}
