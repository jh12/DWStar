using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;
using DAT10.Utils;
using QuickGraph;
using QuickGraph.Algorithms;
using SimpleLogger;
using static DAT10.Utils.DimensionExtensions;

namespace DAT10.Modules.Generation
{
    public class ETLScriptGenerator : IGeneration
    {
        public string Name { get; } = "DW Population";
        public string Description { get; } = "Generates SQL script that transfers data from OSSs to the DW, and also generates data for any new surrogate key, time dimension table, and date dimension table.";

        public async Task Generate(StarModel starModel, string resultPath)
        {
            string path = Path.Combine(resultPath, $"ETL_{starModel.FactTable.Name}.sql");
            string scriptBreak = Environment.NewLine + Environment.NewLine;
            List<string> scripts = new List<string>();

            // Truncate fact table
            scripts.Add($"TRUNCATE TABLE {starModel.FactTable.Name};");

            // Generate insert statements for dimensions
            foreach (var dimension in starModel.Dimensions.Where(d => !d.IsRolePlaying))
            {
                scripts.Add(GenerateInsert((dynamic)dimension));
            }

            // Generate insert statement for fact table
            scripts.Add(GenerateInsert(starModel.FactTable, starModel.Dimensions));

            // Write all scripts to file.
            File.WriteAllText(path, string.Join(scriptBreak, scripts));
            Logger.Log(Logger.Level.Info, $"Created ETL scripts for '{starModel.FactTable.Name}' located at '{path}'.");
        }

        /// <summary>
        /// Generate INSERT statement for fact table values
        /// </summary>
        /// <param name="factTable">Fact table to create statement for</param>
        /// <returns>INSERT statement</returns>
        private string GenerateInsert(FactTable factTable, List<Dimension> dimensions)
        {
            StringBuilder builder = new StringBuilder();

            // Begin creation of insert statement
            builder.AppendLine($"INSERT INTO {factTable.GetFullName()} ");

            // Create a list of all columns used in projection. Starting with foreign keys and then measures
            List<string> columnNames = new List<string>();
            columnNames.AddRange(factTable.Relations.Where(r => Equals(r.LinkTable, factTable))
                    .SelectMany(r => r.LinkColumns.Select(c => c.GetName())));
            columnNames.AddRange(factTable.Columns.Where(c => c.ColumnType.HasFlag(StarColumnType.NumericMeasure) || c.ColumnType.HasFlag(StarColumnType.DescriptiveMeasure)).Select(c => c.GetNameWithAlias()));

            // Add project list to INSERT statement
            builder.Append($"({string.Join(", ", columnNames)}) ");

            // List of columns that are returned from SELECT statement. These include aliases for the column names.
            List<string> selectColumns = new List<string>();
            selectColumns.AddRange(factTable.Relations.Where(r => Equals(r.LinkTable, factTable))
                .SelectMany(r => r.AnchorColumns.Select(c => c.GetNameAsAlias(r.AnchorTable))));
            selectColumns.AddRange(factTable.Columns.Where(c => c.ColumnType.HasFlag(StarColumnType.NumericMeasure) || c.ColumnType.HasFlag(StarColumnType.DescriptiveMeasure)).Select(c => c.ColumnRef.GetNameWithAlias()));

            // Generate the SELECT statement and append to query
            var generateSelect = GenerateSelect(factTable.GetOriginalTables(), selectColumns);
            builder.AppendLine(generateSelect);

            // Add JOIN to SELECT statement
            var generateJoin = GenerateJoin(factTable, dimensions);
            builder.AppendLine(generateJoin);

            return builder.ToString();
        }

        /// <summary>
        /// Utility class used to store a relation across a common model and a star model
        /// </summary>
        public class CrossReference
        {
            // Common model related
            public Table LinkTbl { get; }
            public List<Column> LinkCols { get; }

            // Star model related
            public Dimension AnchorTbl { get; }
            public List<StarColumn> AnchorCols { get; }

            public CrossReference(Table linkTbl, List<Column> linkCols, Dimension anchorTbl, List<StarColumn> anchorCols)
            {
                LinkTbl = linkTbl;
                LinkCols = linkCols;
                AnchorTbl = anchorTbl;
                AnchorCols = anchorCols;
            }
        }

        /// <summary>
        /// Generate an INNER JOIN statement between a linkTable and a list of dimensions
        /// </summary>
        /// <param name="linkTable">Table linking dimensions</param>
        /// <param name="dimensions">Dimensions to use in JOIN statement</param>
        /// <returns>INNER JOIN between linkTable and dimensions</returns>
        private string GenerateJoin(StarModelTableBase linkTable, List<Dimension> dimensions)
        {
            // The table already containing values
            Table tableWithValues = linkTable.TableReference;
            // All the relations, that the table defines a link in
            List<Relation> relations = tableWithValues.Relations.Where(r => r.LinkTable == tableWithValues).ToList();

            List<CrossReference> crossModelReferences = new List<CrossReference>();

            // Add crossreference for each time or date dimension
            foreach (var starRelation in linkTable.Relations)
            {
                if (starRelation.AnchorTable is TimeDimension || starRelation.AnchorTable is DateDimension)
                {
                    crossModelReferences.Add(new CrossReference(tableWithValues, starRelation.LinkColumns.Select(sc => sc.ColumnRef).ToList(), (Dimension)starRelation.AnchorTable, starRelation.AnchorColumns));
                }

                if (starRelation.AnchorTable is JunkDimension)
                {
                    crossModelReferences.Add(new CrossReference(
                        tableWithValues,
                        starRelation.AnchorTable.Columns.Where(c => !c.ColumnType.HasFlag(StarColumnType.Key)).Select(sc => sc.ColumnRef).ToList(),
                        (JunkDimension)starRelation.AnchorTable,
                        starRelation.AnchorTable.Columns.Where(c => !c.ColumnType.HasFlag(StarColumnType.Key)).ToList())
                        );
                }
            }

            // Iterate through relations and find crossreferences
            foreach (var relation in relations)
            {
                var anchorDimension = dimensions.FirstOrDefault(d => Equals(d.TableReference, relation.AnchorTable));

                crossModelReferences.Add(new CrossReference(tableWithValues, relation.LinkColumns, anchorDimension, anchorDimension.Columns.Where(c => relation.AnchorColumns.Contains(c.ColumnRef)).ToList()));
            }

            List<string> joins = new List<string>();

            // Build an INNER JOIN for each crossreference
            foreach (var r in crossModelReferences)
            {
                string join = $" INNER JOIN {r.AnchorTbl.GetNameAsAlias()} ON ";

                // Create a list of all join clauses in this relation
                List<string> clauses = new List<string>();
                for (var i = 0; i < r.LinkCols.Count; i++)
                {
                    // Check if anchor column can contain null values
                    bool cannotContainNulls = r.AnchorTbl.Constraints.NotNullables.Any(c => c.Column == r.AnchorCols[i]);

                    // If temporal, then define special join clause
                    if (r.LinkCols[i].DataType.IsTemporal())
                    {
                        if (r.AnchorTbl is DateDimension)
                            clauses.Add(GenerateDateDimensionJoin(r.LinkCols.First(), (DateDimension) r.AnchorTbl));
                        else if (r.AnchorTbl is TimeDimension)
                            clauses.Add(GenerateTimeDimensionJoin(r.LinkCols.First(), (TimeDimension) r.AnchorTbl));
                    }
                    else
                    {
                        if (cannotContainNulls)
                            clauses.Add($"{r.LinkCols[i].GetNameWithAlias()} = {r.AnchorCols[i].GetNameWithAlias()}");
                        else
                            clauses.Add(
                                $"({r.LinkCols[i].GetNameWithAlias()} = {r.AnchorCols[i].GetNameWithAlias()} OR {r.LinkCols[i].GetNameWithAlias()} IS NULL AND {r.AnchorCols[i].GetNameWithAlias()} IS NULL)");
                    }
                }

                join += string.Join(" AND ", clauses);
                joins.Add(join);
            }

            return string.Join("\r\n", joins);
        }

        #region Dimensions

        /// <summary>
        /// Generate INSERT statement for dimension values
        /// </summary>
        /// <param name="dimension">Dimension to create statement for</param>
        /// <returns>INSERT statement</returns>
        private string GenerateInsert(Dimension dimension)
        {
            var originalTables = dimension.GetOriginalTables();

            string table = $"{dimension.Name}";
            string columns = string.Join(", ", originalTables.SelectMany(t => t.Columns.Select(c => c.GetName())));
            string select = GenerateSelect(originalTables, dimension is JunkDimension);

            return $"DELETE FROM {table};{Environment.NewLine}INSERT INTO {table} ({columns}) {select}";
        }

        /// <summary>
        /// Create a script, that generates all dates between 1/1 and the 12/31 in the current year
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns>Script to populate dimension</returns>
        private string GenerateInsert(DateDimension dimension)
        {
            int year = DateTime.Now.Year;
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            return GenerateDateDimension(startDate, endDate);
        }

        /// <summary>
        /// Create a script, that generates all seconds between 00:00 and 23:59
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns>Script to populate dimension</returns>
        private string GenerateInsert(TimeDimension dimension)
        {
            return GenerateTimeDimension();
        }

        private string GenerateSelect(List<ColumnOrigin> columnOrigins, bool isDistinct)
        {
            return GenerateSelect(columnOrigins, null, isDistinct);
        }

        /// <summary>
        /// Generate the SELECT statement for fact tables or dimensions
        /// </summary>
        /// <param name="columnOrigins">ColumnOrigins for fact table or dimension</param>
        /// <param name="projectionColumns">(Optional) List of columns to use in projection</param>
        /// <returns>SELECT statement</returns>
        private string GenerateSelect(List<ColumnOrigin> columnOrigins, List<string> projectionColumns, bool isDistinct = false)
        {
            // If only composed of one table
            if (columnOrigins.Count == 1)
            {
                var origin = columnOrigins.FirstOrDefault();

                if (projectionColumns != null)
                    return $"SELECT {(isDistinct ? "DISTINCT " : "")}{string.Join(", ", projectionColumns)} FROM {origin.Table.GetNameAsAlias()}";

                return $"SELECT {(isDistinct ? "DISTINCT " : "")}{origin.ColumnsString()} FROM {origin.Table.GetNameAsAlias()}";
            }

            // Sort origins according to how they reference each other.
            var sortedOrigins = SortTablesFromReferences(columnOrigins);

            string selectPart;
            string fromPart;

            selectPart = string.Join(", ", projectionColumns ?? columnOrigins.Select(co => co.ColumnsString()));

            StringBuilder builder = new StringBuilder();

            // Create SELECT statment
            var firstTable = sortedOrigins.First();

            // FROM TABLE xx
            fromPart = $"FROM {firstTable.Table.GetNameAsAlias()}";

            // Create a FULL OUTER JOIN for the remaning tables
            for (var i = 1; i < sortedOrigins.Count; i++)
            {
                var origin = sortedOrigins[i];

                List<string> clauses = new List<string>();

                // Iterate through all relations
                foreach (var relation in origin.Table.Relations.Where(r => r.AnchorTable != r.LinkTable && origin.Table == r.AnchorTable))
                {
                    // If relation is not relevant to the join, then skip it
                    if (!sortedOrigins.Any(c => c.Table == relation.LinkTable))
                        continue;

                    // For each column relation add the equijoin condition
                    for (var j = 0; j < relation.AnchorColumns.Count; j++)
                    {
                        clauses.Add($"{relation.LinkColumns[j].GetNameWithAlias()} = {relation.AnchorColumns[j].GetNameWithAlias()}");
                    }
                }

                builder.AppendLine($"FULL OUTER JOIN {origin.Table.GetNameAsAlias()} ON {string.Join(" AND ", clauses)}");
            }

            return $"SELECT {(isDistinct ? "DISTINCT " : "")}{selectPart} {fromPart} {builder}";
        }

        /// <summary>
        /// Sort a list of ColumnOrigins based on the references between the tables.
        /// </summary>
        /// <param name="columnOrigins">ColumnOrigins to sort</param>
        /// <returns>Sorted ColumnOrigins</returns>
        private List<ColumnOrigin> SortTablesFromReferences(List<ColumnOrigin> columnOrigins)
        {
            var referenceGraph = new BidirectionalGraph<Table, Edge<Table>>();

            var tables = columnOrigins.Select(c => c.Table).ToList();

            // List of tables that is used in the columnorigins
            HashSet<Table> relevantTables = new HashSet<Table>(tables);

            // Add vertices to graph
            referenceGraph.AddVertexRange(tables);

            // Add edges to graph
            // Edges are added between T1 and T2, iff. T1 has a linking reference to T2
            foreach (var table in tables)
            {
                foreach (var outRelation in table.Relations.Where(r => r.LinkTable == table && r.LinkTable != r.AnchorTable))
                {
                    if (!relevantTables.Contains(outRelation.AnchorTable))
                        continue;

                    referenceGraph.AddEdge(new Edge<Table>(outRelation.LinkTable, outRelation.AnchorTable));
                }
            }

            // Topological sort
            var topologicalSort = referenceGraph.TopologicalSort();
            // Associate tables with their ordering
            var tableOrderings = topologicalSort.Select((table, index) => new KeyValuePair<Table, int>(table, index)).ToDictionary(pair => pair.Key, pair => pair.Value);

            // Order column origins by the ordering in tableOrderings
            return columnOrigins.OrderBy(origin => tableOrderings[origin.Table]).ToList();
        }

        #endregion

        #region Date and time dimension

        /// <summary>
        /// Create a script to populate the date dimension
        /// </summary>
        /// <param name="start">Starting date</param>
        /// <param name="end">Ending date</param>
        /// <returns>Script that generates all dates between start and end</returns>
        public string GenerateDateDimension(DateTime start, DateTime end)
        {
            return $"BEGIN TRANSACTION;\r\n" +
                   $"SET LANGUAGE british;\r\n" +
                   $"\r\n" +
                   $"DECLARE @StartDate DATETIME = \'{start.ToShortDateString()}\' -- Adjust to fit your data\r\n" +
                   $"DECLARE @EndDate DATETIME = \'{end.ToShortDateString()}\' -- Adjust to fit your data\r\n" +
                   $"\r\n" +
                   $"DECLARE @CurrentDate AS DATETIME = @StartDate\r\n" +
                   $"\r\n" +
                   $"DELETE FROM [dbo].[Dim_Date];\r\n" + // Delete old values
                   $"\r\n" +
                   $"INSERT INTO [dbo].[Dim_Date] VALUES (\'Unknown\', 00, \'Unknown\', 00, 00, \'Unknown\', 0000, 0, NULL); -- Dummy date\r\n" + // Add dummy value
                   $"\r\n" +
                   $"-- Populate dates\r\n" +
                   $"WHILE @CurrentDate <= @EndDate\r\n" + // Loop through all dates
                   $"BEGIN\r\n" +
                   $"\tINSERT INTO [dbo].[Dim_Date]\r\n" +
                   $"\tSELECT\r\n" +
                   $"\t\tCONVERT(char(10), @CurrentDate, 103), -- FullDate\r\n" +
                   $"\t\tDATEPART(DD, @CurrentDate), -- Day\r\n" +
                   $"\t\tDATENAME(DW, @CurrentDate), -- NameOfDay\r\n" +
                   $"\t\tDATEPART(WW, @CurrentDate), -- Week\r\n" +
                   $"\t\tDATEPART(MM, @CurrentDate), -- Month\r\n" +
                   $"\t\tDATENAME(MM, @CurrentDate), -- NameOfMonth\r\n" +
                   $"\t\tDATEPART(YY, @CurrentDate), -- Year\t\r\n" +
                   $"\t\t0, -- IsHoliday\r\n" +
                   $"\t\tNULL -- Holiday\r\n" +
                   $"\t\r\n" +
                   $"\tSET @CurrentDate = DATEADD(DD, 1, @CurrentDate) -- Increment date\r\n" +
                   $"END\r\n" +
                   $"\r\n" +
                   $"-- Set holidays\r\n" + // Set a holiday (easter)
                   $"UPDATE [dbo].[Dim_Date]\r\n" +
                   $"\tSET Holiday = \'Easter\' WHERE [Month] = 4 AND [Day] = 21;\r\n" +
                   $"\r\n" +
                   $"UPDATE [dbo].[Dim_Date]\r\n" + // Update IsHoliday to be 1 if Holiday is not null.
                   $"\tSET IsHoliday = 0 WHERE Holiday IS NULL;\r\n" +
                   $"UPDATE [dbo].[Dim_Date]\r\n" +
                   $"\tSET IsHoliday = 1 WHERE Holiday IS NOT NULL;\r\n" +
                   $"COMMIT TRANSACTION;\r\n" +
                   $"GO";
        }

        /// <summary>
        /// Create a script to populate time dimension
        /// </summary>
        /// <returns></returns>
        public string GenerateTimeDimension()
        {
            return $"BEGIN TRANSACTION;\r\n" +
                   $"SET LANGUAGE british;\r\n" +
                   $"\r\n" +
                   $"DECLARE @StartDate DATETIME2 = \'01/01/2017\'\r\n" +
                   $"DECLARE @EndDate DATETIME2 = \'01/01/2017 23:59:59\'\r\n" +
                   $"\r\n" +
                   $"DECLARE @CurrentDate AS DATETIME2 = @StartDate\r\n" +
                   $"\r\n" +
                   $"DELETE FROM [dbo].[Dim_Time];\r\n" + // Delete old values
                   $"\r\n" +
                   $"-- Populate times\r\n" +
                   $"WHILE @CurrentDate <= @EndDate\r\n" + // Loop through all seconds of a day
                   $"BEGIN\r\n" +
                   $"\tINSERT INTO [dbo].[Dim_Time]\r\n" +
                   $"\tSELECT DATEPART(HH, @CurrentDate),\r\n" +
                   $"\t\t   DATEPART(MI, @CurrentDate),\r\n" +
                   $"\t\t   DATEPART(SS, @CurrentDate)\r\n" +
                   $"\r\n" +
                   $"\tSET @CurrentDate = DATEADD(SS, 1, @CurrentDate) -- Increment seconds\r\n" +
                   $"END\r\n" +
                   $"COMMIT TRANSACTION;\r\n" +
                   $"GO";
        }

        /// <summary>
        /// Create a join condition between a datetime and a date dimension
        /// </summary>
        /// <param name="timestampColumn">Datetime column</param>
        /// <param name="dateDimension">Date dimension</param>
        /// <returns>Join condition</returns>
        public string GenerateDateDimensionJoin(Column timestampColumn, DateDimension dateDimension)
        {
            return $"CONVERT(char(10), {timestampColumn.GetNameWithAlias()}, 103) = {dateDimension.Columns.First(c => c.Name == "FullDate").GetNameWithAlias(dateDimension)}";
        }

        /// <summary>
        /// Create a join condition between a datetime and a time dimension
        /// </summary>
        /// <param name="timestampColumn">Datetime column</param>
        /// <param name="timeDimension">Time dimension</param>
        /// <returns>Join condition</returns>
        public string GenerateTimeDimensionJoin(Column timestampColumn, TimeDimension timeDimension)
        {
            return $"DATEPART(HOUR, {timestampColumn.GetNameWithAlias()}) = {timeDimension.Columns.First(c => c.Name == "Hour").GetNameWithAlias(timeDimension)} AND " +
                   $"DATEPART(MINUTE, {timestampColumn.GetNameWithAlias()}) = {timeDimension.Columns.First(c => c.Name == "Minute").GetNameWithAlias(timeDimension)} AND " +
                   $"DATEPART(SECOND, {timestampColumn.GetNameWithAlias()}) = {timeDimension.Columns.First(c => c.Name == "Second").GetNameWithAlias(timeDimension)}";
        }

        #endregion
    }
}
