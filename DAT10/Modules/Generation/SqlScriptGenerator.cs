using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;
using Microsoft.SqlServer.Management.Smo;
using QuickGraph;
using SimpleLogger;

namespace DAT10.Modules.Generation
{
    public class SqlScriptGenerator : IGeneration
    {
        public string Name { get; } = "Create Script Generation";
        public string Description { get; } = "Generates SQL script that creates the DW.";

        public async Task Generate(StarModel starModel, string resultPath)
        {
            Dictionary<StarModelTableBase, Table> tableObjects = new Dictionary<StarModelTableBase, Table>();
            Dictionary<StarColumn, Column> columnObjects = new Dictionary<StarColumn, Column>();

            var server = new Server();
            var database = new Database(server, "DW");

            // Create scripter
            var scripter = new Scripter(server);
            scripter.Options.ScriptDrops = false;
            scripter.Options.IncludeHeaders = true;
            scripter.Options.DriPrimaryKey = true;
            scripter.Options.Indexes = true;
            scripter.Options.DriAll = true;
            scripter.Options.NoIdentities = false;
            scripter.Options.FileName = Path.Combine(resultPath, $"DW from {starModel.FactTable.Name}.sql");

            List<StarModelTableBase> tables = new List<StarModelTableBase>();

            tables.Add(starModel.FactTable);
            tables.AddRange(starModel.Dimensions.Where(d => !d.IsRolePlaying));

            // Create tables
            foreach (var table in tables)
            {
                var tbl = new Table(database, table.Name);

                foreach (var column in table.Columns)//.OrderByDescending(c => c.ColumnType.HasFlag(StarColumnType.Key)).ThenBy(c => c.Ordinal))
                {
                    var col = new Column(tbl, column.Name, FromDatatype(column.DataType));
                    columnObjects[column] = col;

                    col.Nullable = !table.Constraints.NotNullables.Any(n => Equals(n.Column, column));

                    // Set column to identity if surrogate key
                    if (column.ColumnType.HasFlag(StarColumnType.SurrogateKey))
                    {
                        col.Identity = true;
                        col.IdentityIncrement = 1;
                        col.IdentitySeed = 1;
                    }

                    tbl.Columns.Add(col);
                }

                // Create primary key
                CreatePrimaryKey(columnObjects, table, tbl);

                tableObjects[table] = tbl;
            }

            // Create foreign keys
            CreateForeignKeys(tableObjects, columnObjects, tables);

            StringBuilder builder = new StringBuilder();

            try
            {
                var stringCollection = scripter.Script(tableObjects.Values.ToArray());

                Logger.Log(Logger.Level.Info, $"Created SQL script for '{starModel.FactTable.Name}' located at '{scripter.Options.FileName}'.");

                foreach (var s in stringCollection)
                {
                    builder.AppendLine(s);
                }

                string result = builder.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            
        }

        private static void CreateForeignKeys(Dictionary<StarModelTableBase, Table> tableObjects, Dictionary<StarColumn, Column> columnObjects, List<StarModelTableBase> tables)
        {
            foreach (var table in tables)
            {
                foreach (var relation in table.Relations.Where(r => Equals(r.LinkTable, table)))
                {
                    var anchorTable = tableObjects[relation.AnchorTable.ActualTable];
                    var linkTable = tableObjects[relation.LinkTable.ActualTable];

                    ForeignKey foreignKey;

                    // Create constraint
                    if (relation.AnchorTable is DateDimension)
                        foreignKey = new ForeignKey(linkTable, $"FK_{linkTable.Name}_{relation.LinkColumns[0].Name}_{anchorTable.Name}");
                    else if(relation.AnchorTable is TimeDimension)
                        foreignKey = new ForeignKey(linkTable, $"FK_{linkTable.Name}_{relation.LinkColumns[0].Name}_{anchorTable.Name}");
                    else
                        foreignKey = new ForeignKey(linkTable, $"FK_{linkTable.Name}_{anchorTable.Name}");


                    foreignKey.ReferencedTable = anchorTable.Name;

                    // Add columns to constraint
                    for (var i = 0; i < relation.LinkColumns.Count; i++)
                    {
                        var foreignColumnObj = columnObjects[relation.LinkColumns[i]];
                        var referencedColumnObj = columnObjects[relation.AnchorColumns[i]];

                        var foreignKeyColumn = new ForeignKeyColumn(foreignKey, foreignColumnObj.Name, referencedColumnObj.Name);
                        foreignKey.Columns.Add(foreignKeyColumn);
                    }

                    linkTable.ForeignKeys.Add(foreignKey);
                }
            }
        }

        private static void CreatePrimaryKey(Dictionary<StarColumn, Column> columnObjects, StarModelTableBase table, Table tbl)
        {
            // Only use the first primary key
            //var starColumns = table.Constraints.PrimaryKey.Columns;
            var keyStarColumns = table.Columns.Where(c => c.ColumnType.HasFlag(StarColumnType.Key)).ToList();

            if (keyStarColumns != null && keyStarColumns.Any())
            {
                var keyColumns = columnObjects.Where(pair => keyStarColumns.Contains(pair.Key)).Select(pair => pair.Value);

                // Create primary key index for the key columns
                Index index = new Index(tbl, $"PK_{tbl.Name}");
                index.IndexKeyType = IndexKeyType.DriPrimaryKey;

                foreach (var keyColumn in keyColumns)
                {
                    keyColumn.Nullable = false;
                    index.IndexedColumns.Add(new IndexedColumn(index, keyColumn.Name));
                }

                // Add index to table
                tbl.Indexes.Add(index);
            }
        }

        /// <summary>
        /// Create a SQL datatype from a common model datatype
        /// </summary>
        /// <param name="datatype">Common model datatype</param>
        /// <returns>SQL datatype</returns>
        private DataType FromDatatype(Metadata.Model.DataType datatype)
        {
            switch (datatype.Type)
            {
                case System.Data.OleDb.OleDbType.BigInt:
                    return new DataType(SqlDataType.BigInt);
                case System.Data.OleDb.OleDbType.Binary:
                    return new DataType(SqlDataType.Binary, datatype.Length.GetValueOrDefault(255));
                case System.Data.OleDb.OleDbType.Boolean:
                    return new DataType(SqlDataType.Bit);
                case System.Data.OleDb.OleDbType.Char:
                    return new DataType(SqlDataType.Char, datatype.Length.GetValueOrDefault(255));
                case System.Data.OleDb.OleDbType.Currency:
                    return new DataType(SqlDataType.Money);
                case System.Data.OleDb.OleDbType.Date:
                    return new DataType(SqlDataType.DateTime2);
                case System.Data.OleDb.OleDbType.DBDate:
                    return new DataType(SqlDataType.DateTime2);
                case System.Data.OleDb.OleDbType.DBTime:
                    return new DataType(SqlDataType.Time);
                case System.Data.OleDb.OleDbType.DBTimeStamp:
                    return new DataType(SqlDataType.DateTime2);
                case System.Data.OleDb.OleDbType.Decimal:
                    return new DataType(SqlDataType.Decimal, datatype.Precision.GetValueOrDefault(10), datatype.Scale.GetValueOrDefault(3));
                case System.Data.OleDb.OleDbType.Double:
                    return new DataType(SqlDataType.Float);
                case System.Data.OleDb.OleDbType.Integer:
                    return new DataType(SqlDataType.Int);
                case System.Data.OleDb.OleDbType.LongVarBinary:
                    return new DataType(SqlDataType.VarBinaryMax);
                case System.Data.OleDb.OleDbType.LongVarChar:
                    return new DataType(SqlDataType.VarCharMax);
                case System.Data.OleDb.OleDbType.LongVarWChar:
                    return new DataType(SqlDataType.NVarCharMax);
                case System.Data.OleDb.OleDbType.Numeric:
                    return new DataType(SqlDataType.Numeric, datatype.Precision.GetValueOrDefault(10), datatype.Scale.GetValueOrDefault(3));
                case System.Data.OleDb.OleDbType.Single:
                    return new DataType(SqlDataType.Real);
                case System.Data.OleDb.OleDbType.SmallInt:
                    return new DataType(SqlDataType.SmallInt);
                case System.Data.OleDb.OleDbType.TinyInt:
                    return new DataType(SqlDataType.TinyInt);
                case System.Data.OleDb.OleDbType.UnsignedBigInt:
                    return new DataType(SqlDataType.BigInt);
                case System.Data.OleDb.OleDbType.UnsignedInt:
                    return new DataType(SqlDataType.Int);
                case System.Data.OleDb.OleDbType.UnsignedSmallInt:
                    return new DataType(SqlDataType.SmallInt);
                case System.Data.OleDb.OleDbType.UnsignedTinyInt:
                    return new DataType(SqlDataType.TinyInt);
                case System.Data.OleDb.OleDbType.VarBinary:
                    return new DataType(SqlDataType.VarBinary, datatype.Length.GetValueOrDefault(255));
                case System.Data.OleDb.OleDbType.VarChar:
                    return new DataType(SqlDataType.VarChar, datatype.Length.GetValueOrDefault(255));
                case System.Data.OleDb.OleDbType.VarNumeric:
                    return new DataType(SqlDataType.Decimal);
                case System.Data.OleDb.OleDbType.VarWChar:
                    return new DataType(SqlDataType.NVarChar, datatype.Length.GetValueOrDefault(255));
                case System.Data.OleDb.OleDbType.WChar:
                    return new DataType(SqlDataType.NChar, datatype.Length.GetValueOrDefault(255));
                default:
                    return new DataType(SqlDataType.NText);
            }
        }
    }
}
