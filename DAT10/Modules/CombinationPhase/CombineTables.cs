using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Core;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;
using DAT10.Modules.Multidimensional;
using DAT10.StarModelComponents;
using SimpleLogger;

namespace DAT10.Modules.CombinationPhase
{
    public class CombineTables : ICombinationModule
    {
        private readonly DataSampleService _dataSampleService;
        public string Name { get; } = "Combine Tables";
        public string Description { get; } = "Combines two tables in the attempt to create a more appropriate fact table.";

        public CombineTables(DataSampleService dataSampleService)
        {
            _dataSampleService = dataSampleService;
        }

        public List<CommonModel> Combine(CommonModel commonModel)
        {
            List<CommonModel> cms = new List<CommonModel>() {commonModel};
            var allDistinctRelations = new List<Relation>();
            foreach (List<Relation> relation in commonModel.Tables.Select(x => x.Relations).ToList())
            {
                allDistinctRelations.AddRange(relation);
            }

            allDistinctRelations = allDistinctRelations.Distinct().ToList();

            foreach (Relation tableReferenceRelation in allDistinctRelations)
            {
                Table linkTable = tableReferenceRelation.LinkTable;
                Table anchorTable = tableReferenceRelation.AnchorTable;
                int keys = 0;
                int numerics = 0;
                int descriptive = 0;
                int anchorManyToOne = 0;
                foreach (Column linkTableColumn in linkTable.Columns)
                {
                    if (linkTableColumn.IsKey())
                    {
                        keys++;
                    }
                    else if (linkTableColumn.DataType.IsNumeric())
                    {
                        numerics++;
                    }
                    else
                    {
                        descriptive++;
                    }
                }

                anchorManyToOne =
                    anchorTable.Relations.Count(
                        x => x.Cardinality == Cardinality.ManyToOne && x.LinkTable == anchorTable && x.LinkTable != x.AnchorTable);

                if (descriptive == 0 && numerics <= 3 && keys <= 3 && anchorManyToOne >= 1)
                {
                    //Logger.Log(Logger.Level.Debug,
                    //    $"Combined Table '{tableReferenceRelation.LinkTable.Name}' with '{tableReferenceRelation.AnchorTable.Name}'");
                    cms.AddRange(CombineTwoTables(commonModel, commonModel.Tables.IndexOf(tableReferenceRelation.LinkTable),
                        commonModel.Tables.IndexOf(tableReferenceRelation.AnchorTable)));
                }
            }
            return cms;
        }

        private Dictionary<int, int> duplicationCount = new Dictionary<int, int>();

        private List<CommonModel> CombineTwoTables(CommonModel commonModel, int linkTableIndex, int anchorTableIndex)
        {
            var cm = Utils.DeepCopyExtension.Copy(commonModel);

            if (!duplicationCount.ContainsKey(cm.OriginID))
                duplicationCount[cm.OriginID] = 0;
            else
                duplicationCount[cm.OriginID]++;

            cm.Suffix = $".{(char)(65 + duplicationCount[cm.OriginID])}";

            var linkTable = cm.Tables[linkTableIndex];
            var anchorTable = cm.Tables[anchorTableIndex];
            // Find all tables directly related to the two tables
            List<Table> allRelatedTables = new List<Table>();
            List<Relation> relations = linkTable.Relations.Concat(anchorTable.Relations).ToList();
            foreach (Relation relation in relations)
            {
                if (relation.LinkTable == linkTable && relation.AnchorTable == anchorTable ||
                    relation.LinkTable == anchorTable && relation.AnchorTable == linkTable)
                    continue;
                else if (relation.LinkTable == linkTable || relation.LinkTable == anchorTable)
                    allRelatedTables.Add(relation.AnchorTable);
                else
                    allRelatedTables.Add(relation.AnchorTable);
            }

            // Count the number of rows when joined
            var linkAnchorRelation = linkTable.Relations.First(x => x.AnchorTable == anchorTable);
            int amountOfRows;
            if (linkAnchorRelation.AnchorColumns.Count > 1)
            {
                amountOfRows = -1;
            }
            else
            {
                Stopwatch timer = Stopwatch.StartNew();
                amountOfRows = CountRows(linkAnchorRelation, linkTable, anchorTable);
                timer.Stop();
                Logger.Log(Logger.Level.Debug, $"Joining '{linkTable.Name}' and '{anchorTable.Name}' gave '{amountOfRows}' rows and took {timer.Elapsed}");
            }

            // Make the new tables and then assign necessary columns, relations, and constraints
            Table combinedTable = new CombinedTable($"{linkTable.Name}_{anchorTable.Name}", linkTable.Schema, amountOfRows, new List<Table> {commonModel.Tables[linkTableIndex], commonModel.Tables[anchorTableIndex]});

            // Constraints TODO: make better
            Constraints cs = new Constraints();
            cs.NotNullables.AddRange(linkTable.NotNullables);
            foreach (var notnull in anchorTable.NotNullables)
                cs.NotNullables.Add(notnull);

            cs.PrimaryKeys.Add(linkTable.PrimaryKey);
            cs.PrimaryKeys.Add(anchorTable.PrimaryKey);


            cs.Uniques.AddRange(linkTable.Uniques);
            foreach (var unique in anchorTable.Uniques)
                cs.Uniques.Add(unique);

            // Columns
            List<Column> columns = anchorTable.Columns.Concat(linkTable.Columns).ToList();
            combinedTable.Columns.AddRange(columns);
            // remove dubplicates that occurs from primary key to foreignkey
            foreach (Column linkColumn in linkTable.Relations.Where(x => x.AnchorTable == anchorTable).ToList().First().LinkColumns)
            {
                combinedTable.Columns.Remove(linkColumn);
            }
            // add table names if duplicates occurs
            //combinedTable.Columns.Where(x => combinedTable.Columns.Count(y => y.Name == x.Name) > 1)
            //    .ToList()
            //    .ForEach(x => x.Names = new List<NameCandidate>() { new NameCandidate($"{x.Table.Name}_{x.Name}", 1f) });

            // Relations
            foreach (Table table in allRelatedTables)
            {
                List<Relation> newRelations = new List<Relation>();
                List<Relation> oldRelations = new List<Relation>();
                int madeDoubleConnection = 0;

                foreach (Relation tableRelation in table.Relations)
                {
                    if (tableRelation.LinkTable != linkTable && tableRelation.LinkTable != anchorTable)
                        continue;

                    Table anchor;
                    List<Column> anchorColumns;
                    Table link;
                    List<Column> linkColumns;

                    Cardinality c = tableRelation.Cardinality;
                    if (tableRelation.LinkTable == linkTable || tableRelation.LinkTable == anchorTable)
                    {
                        link = combinedTable;
                        linkColumns = tableRelation.LinkColumns;
                        anchor = tableRelation.AnchorTable;
                        anchorColumns = tableRelation.AnchorColumns;
                    }
                    else
                    {
                        link = tableRelation.LinkTable;
                        linkColumns = tableRelation.LinkColumns;
                        anchor = combinedTable;
                        anchorColumns = tableRelation.AnchorColumns;
                    }

                    var r = new Relation(anchor, link, anchorColumns, linkColumns, c);
                    newRelations.Add(r);
                    oldRelations.Add(tableRelation);
                    madeDoubleConnection++;
                }

                for (int index = 0; index < newRelations.Count; index++)
                {
                    var newRelation = newRelations[index];
                    var oldRelation = oldRelations[index];
                    table.Relations.Remove(oldRelation);
                    table.Relations.Add(newRelation);
                    combinedTable.Relations.Add(newRelation);
                }

                if (madeDoubleConnection > 1)
                {
                    Logger.Log(Logger.Level.Error, $"Two tables where combined with too many connection made between them '{linkTable.Name}' and '{anchorTable.Name}'.");
                    //throw new DoubleConnectionException($"Two relations were made to combined table from table: {table.Name}");
                }
            }

            // Column

            cm.Tables.Remove(linkTable);
            cm.Tables.Remove(anchorTable);
            cm.Tables.Add(combinedTable);

            return new List<CommonModel>() { cm };
        }

        private int CountRows(Relation linkAnchorRelation, Table linkTable, Table anchorTable)
        {
            List<string[]> linkRows = _dataSampleService.GetDataSampleAsync(linkAnchorRelation.LinkTable.RowCount, linkAnchorRelation.LinkColumns.ToArray()).Result;
            List<string[]> anchorRows = _dataSampleService.GetDataSampleAsync(linkAnchorRelation.AnchorTable.RowCount, linkAnchorRelation.AnchorColumns.ToArray()).Result;
            DataTable dtLink = new DataTable(linkTable.Name);
            DataTable dtAnchor = new DataTable(anchorTable.Name);
            DataSet ds = new DataSet();

            foreach (Column linkColumn in linkAnchorRelation.LinkColumns)
            {
                dtLink.Columns.Add(linkColumn.Name, typeof(int));
            }
            foreach (Column anchorColumn in linkAnchorRelation.AnchorColumns)
            {
                dtAnchor.Columns.Add(anchorColumn.Name, typeof(int));
            }

            ds.Tables.Add(dtLink);
            ds.Tables.Add(dtAnchor);

            foreach (string[] row in linkRows)
            {
                dtLink.Rows.Add(row);
            }

            foreach (string[] row in anchorRows)
            {
                dtAnchor.Rows.Add(row);
            }

            var test = JoinDataTables(dtLink, dtAnchor,
                (row1, row2) =>
                    row1.Field<int>($"{linkAnchorRelation.LinkColumns.First().Name}") == row2.Field<int>($"{linkAnchorRelation.AnchorColumns.First().Name}"));

            return test.Rows.Count;
        }

        private DataTable JoinDataTables(DataTable t1, DataTable t2, params Func<DataRow, DataRow, bool>[] joinOn)
        {
            DataTable result = new DataTable();
            foreach (DataColumn col in t1.Columns)
            {
                if (result.Columns[col.ColumnName] == null)
                    result.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataColumn col in t2.Columns)
            {
                if (result.Columns[col.ColumnName] == null)
                    result.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataRow row1 in t1.Rows)
            {
                var joinRows = t2.AsEnumerable().Where(row2 =>
                {
                    return joinOn.All(parameter => parameter(row1, row2));
                });
                foreach (DataRow fromRow in joinRows)
                {
                    DataRow insertRow = result.NewRow();
                    foreach (DataColumn col1 in t1.Columns)
                    {
                        insertRow[col1.ColumnName] = row1[col1.ColumnName];
                    }
                    foreach (DataColumn col2 in t2.Columns)
                    {
                        insertRow[col2.ColumnName] = fromRow[col2.ColumnName];
                    }
                    result.Rows.Add(insertRow);
                }
            }
            return result;
        }

    }



    public class DoubleConnectionException : Exception
    {
        public DoubleConnectionException(string message) : base(message)
        {
        }
    }
}
