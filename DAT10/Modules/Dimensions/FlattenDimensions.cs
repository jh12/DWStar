using System.Collections.Generic;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;

namespace DAT10.Modules.Dimensions
{
    public class FlattenDimensions : DimensionalModuleBase
    {
        public override string Name { get; } = "Flatten Dimensions";

        public override string Description { get; } =
            "All tables with a one-to-many connection to the fact table are recognized as a dimension.Each table with a one-to-many or one-to-one connection to that dimension table is then combined with it. This is done with both direct or indirect connections.";

        /*
         * Classification entities are entities which are related
            to component entities by a chain of one-to-many relationships
            that is, they are functionally dependent on a
            component entity (directly or transitively).
         * 
         */

        public FlattenDimensions() : base(StarDependency.None, StarDependency.Dimensions)
        {
        }


        public override StarModel TranslateModel(StarModel starModel)
        {
            //All fact tables across all star models
            FactTable factTable = starModel.FactTable;
            List<Dimension> temporaryDimensions = starModel.Dimensions;

            // Case 1, if dimensions have been found, simply flatten them
            // Case 2, if dimensions have *not* been found, find all connected tables to the fact table and flatten them.

            // Case 2
            if (temporaryDimensions.Count == 0)
            {
                List<Table> tables =
                    factTable.TableReference.Relations
                        .Where(x => factTable.TableReference == x.LinkTable && x.Cardinality == Cardinality.ManyToOne &&
                                    x.AnchorTable != factTable.TableReference)
                        .Select(x => x.AnchorTable)
                        .ToList();

                temporaryDimensions = tables.Select(table => new Dimension(table)).ToList();
            }

            // Case 1
            foreach (var dim in temporaryDimensions)
            {
                var tableRef = dim.TableReference;
                var newColumns = Flatten(tableRef, new List<Table>() {tableRef});
                dim.Columns.AddRange(newColumns);
                dim.Columns.ForEach(x => x.TableRef = dim);
            }

            factTable.Relations.Clear();

            // Add relations from fact table to all dimensions
            foreach (var dimension in temporaryDimensions)
            {
                var starRelation = new StarRelation(dimension, factTable, Cardinality.ManyToOne);
                factTable.Relations.Add(starRelation);
                dimension.Relations.Add(starRelation);
            }

            starModel.Dimensions.AddRange(temporaryDimensions);

            return starModel;
        }

        private List<StarColumn> Flatten(Table tableRef, List<Table> visited)
        {
            List<Table> tablesVisited = new List<Table>();
            tablesVisited.AddRange(visited);

            var columns = new List<StarColumn>();

            List<Table> tables =
                tableRef.Relations.ToList()
                    .Where(x => x.LinkTable == tableRef && x.Cardinality == Cardinality.ManyToOne &&
                                x.AnchorTable != tableRef)
                    .Select(x => x.AnchorTable)
                    .ToList();

            foreach (var table in tables.Where(x=> !tablesVisited.Contains(x)).ToList())
            {
                tablesVisited.Add(table);
                var innerColumns = Flatten(table, tablesVisited);

                innerColumns.ForEach(x => x.Name = $"{table.Name}_{x.Name}");

                columns.AddRange(innerColumns);

                foreach (var c in table.Columns)
                {
                    //if (!c.IsPrimaryKey())
                    {
                        columns.Add(new StarColumn(c, $"{table.Name}_{c.Name}"));

                    }
                }
            }

            return columns;
        }
    }
}
