using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using DAT10.Utils;

namespace DAT10.Modules.Refinement.Relations
{
    public class CSVRelations : RefinementModuleBase
    {
        public override string Name { get; } = "Primary Key Naming Pattern";
        public override string Description { get; } = "Finds primary key candidates based on the similarity between the column name and the table name, and whether the column contains any of the keywords: ID, Key, and No, and Ref.";

        public PluralizationService pService;

        public List<string> Identifiers = new List<string>() {"id", "no", "key"};

        public CSVRelations() : base(CommonDependency.Name, CommonDependency.Relations)
        {
            pService = PluralizationService.CreateService(new CultureInfo("EN-US"));
        }

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {
            for (var index = 0; index < commonModel.Tables.Count; index++)
            {
                Table t1 = commonModel.Tables[index];
                string t1Name = pService.Singularize(t1.Name);

                for (var i = index+1; i < commonModel.Tables.Count; i++)
                {
                    Table t2 = commonModel.Tables[i];

                    // If both table is equal, OR there already exists a relation between the two tables
                    if (t1 == t2 || t1.HasRelationToTable(t2))
                        continue;

                    string t2Name = pService.Singularize(t2.Name);

                    foreach (Column c1 in t1.Columns)
                    {
                        if (!IsIdentifier(c1.Name))
                            continue;

                        foreach (Column c2 in t2.Columns)
                        {
                            if (!IsIdentifier(c2.Name))
                                continue;

                            if (c1.Name == c2.Name)
                            {
                                bool c1PrimaryKey = c1.Name == t1Name + "ID";
                                bool c2PrimaryKey = c2.Name == t2Name + "ID";
                                if (c1PrimaryKey)
                                {
                                    Relation r = new Relation(t1, t2, new List<Column>() {c1}, new List<Column>() {c2},
                                        Cardinality.ManyToOne);
                                    t1.AddRelation(r);
                                }
                                else if (c2PrimaryKey)
                                {
                                    Relation r = new Relation(t2, t1, new List<Column>() {c2}, new List<Column>() {c1},
                                        Cardinality.ManyToOne);
                                    t1.AddRelation(r);
                                }
                            }
                        }
                    }
                }
            }
            return commonModel;
        }

        private bool IsIdentifier(string name)
        {
            return Identifiers.Any(x => name.ToLower().Contains(x));
        }
    }
}
//        public override async Task<CommonModel> Refine(CommonModel commonModel)
//        {
//            // TODO: Rough way of finding relations from CSV file
//            Parallel.For(0, commonModel.Tables.Count, async (i) =>
//            {
//                var t1 = commonModel.Tables[i];
//                for (var i1 = i+1; i1 < commonModel.Tables.Count; i1++)
//                {
//                    var t2 = commonModel.Tables[i1];
//                    await FindRelationWithTable(t1, t2);

//                }
//            });

//            return commonModel;
//        }

//        private async Task FindRelationWithTable(Table t1, Table t2)
//        {
//            //Logger.Log(Logger.Level.Info, $"{t1.Name},\t{t2.Name}");

//            foreach (Column c1 in t1.Columns.Where(
//                x => ColumnCheck(x.DataType.Type) && t1.Constraints.PrimaryKeys
//                         .SelectMany(y => y.Columns)
//                         .Count(y => y == x) == 0))
//            {
//                foreach (Column c2 in t2.Columns.Where(x => ColumnCheck(x.DataType.Type) && t2.Constraints.PrimaryKeys
//                                                                .SelectMany(y => y.Columns)
//                                                                .Count(y => y == x) != 0))
//                {
//                    var countRows = CountRows(t1, t2, c1, c2);
//                    //Logger.Log(Logger.Level.Debug, $"{t1.Name}.{c1.Name}-{t2.Name}.{c2.Name} = {countRows},{t1.RowCount},{t2.RowCount}");
//                    if (Math.Min(t1.RowCount, t2.RowCount) < countRows)
//                    {
//                        CreateRelation(t1, t2, c1, c2);
//                    }
//                }
//            }
//        }

//        private void CreateRelation(Table t1, Table t2, Column c1, Column c2)
//        {

//            Logger.Log(Logger.Level.Debug, $"Created relation between '{t1.Name}.{c1.Name}' -> '{t2.Name}.{c2.Name}'");
//        }

//        private int CountRows(Table linkTable, Table anchorTable, Column linkColumn, Column anchorColumn)
//        {
//            List<string[]> linkRows = _dataSampleService.GetDataSampleAsync(linkTable.RowCount, linkColumn).Result;
//            List<string[]> anchorRows = _dataSampleService.GetDataSampleAsync(anchorTable.RowCount, anchorColumn).Result;
//            DataTable dtLink = new DataTable(linkTable.Name);
//            DataTable dtAnchor = new DataTable(anchorTable.Name);
//            DataSet ds = new DataSet();

//            dtLink.Columns.Add(linkColumn.Name, typeof(int));
//            dtAnchor.Columns.Add(anchorColumn.Name, typeof(int));

//            ds.Tables.Add(dtLink);
//            ds.Tables.Add(dtAnchor);

//            foreach (string[] row in linkRows)
//            {
//                if (row[0] == "")
//                    continue;
//                dtLink.Rows.Add(row);
//            }

//            foreach (string[] row in anchorRows)
//            {
//                if (row[0] == "")
//                    continue;
//                dtAnchor.Rows.Add(row);
//            }

//            var test = JoinDataTables(dtLink, dtAnchor,
//                (row1, row2) =>
//                    row1.Field<int>($"{linkColumn.Name}") == row2.Field<int>($"{anchorColumn.Name}"));

//            return test.Rows.Count;
//        }

//        private DataTable JoinDataTables(DataTable t1, DataTable t2, params Func<DataRow, DataRow, bool>[] joinOn)
//        {
//            DataTable result = new DataTable();
//            foreach (DataColumn col in t1.Columns)
//            {
//                if (result.Columns[col.ColumnName] == null)
//                    result.Columns.Add(col.ColumnName, col.DataType);
//            }
//            foreach (DataColumn col in t2.Columns)
//            {
//                if (result.Columns[col.ColumnName] == null)
//                    result.Columns.Add(col.ColumnName, col.DataType);
//            }
//            foreach (DataRow row1 in t1.Rows)
//            {
//                var joinRows = t2.AsEnumerable().Where(row2 =>
//                {
//                    return joinOn.All(parameter => parameter(row1, row2));
//                });
//                foreach (DataRow fromRow in joinRows)
//                {
//                    DataRow insertRow = result.NewRow();
//                    foreach (DataColumn col1 in t1.Columns)
//                    {
//                        insertRow[col1.ColumnName] = row1[col1.ColumnName];
//                    }
//                    foreach (DataColumn col2 in t2.Columns)
//                    {
//                        insertRow[col2.ColumnName] = fromRow[col2.ColumnName];
//                    }
//                    result.Rows.Add(insertRow);
//                }
//            }
//            return result;
//        }

//        private bool ColumnCheck(OleDbType type)
//        {
//            return type == OleDbType.BigInt || type == OleDbType.Integer;
//        }
//    }
//}

