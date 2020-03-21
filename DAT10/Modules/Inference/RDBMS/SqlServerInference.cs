using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Utils;
using SimpleLogger;

namespace DAT10.Modules.Inference.RDBMS
{
    public class SqlServerInference : InferenceModuleBase
    {
        public override string Name { get; } = "SQL Metadata";
        public override string Description { get; } = "Retrieves table names, column names, ordinals, data types, nullability, uniqueness, primary keys, and foreign keys from an SQL server.";

        private SqlServerInferenceConfiguration _configuration;

        public SqlServerInference(DataSampleService sampleService) : base(sampleService)
        {
            _configuration = new SqlServerInferenceConfiguration();
        }

        public override bool IsValidConnection(string connection)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            try
            {
                builder.ConnectionString = connection;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        protected override CommonModel GetSchemaForConnection(string connectionstring)
        {
            if (connectionstring == null)
                throw new NullReferenceException("No connectionstring specified.");


            using (var conn = new SqlConnection(connectionstring))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    Logger.Log(e);
                    throw;
                }

                var databases = new List<Database> {GetDatabase(conn)};
                return new CommonModel(databases.SelectMany(db => db.Tables).ToList());
            }
        }

        #region Database
        /// <summary>
        /// Get database
        /// </summary>
        /// <param name="conn">Connection to a database</param>
        /// <returns>Database</returns>
        private Database GetDatabase(IDbConnection conn)
        {
            Database database = new Database(conn.Database, conn.ConnectionString, "SQL");

            // Get tables
            database.Tables = GetTables(conn, database);

            // Get relations between tables
            GetRelations(conn, database);

            return database;
        }

        /// <summary>
        /// Get relations between tables in the supplied database
        /// </summary>
        /// <param name="conn">Connection to a database</param>
        /// <param name="database">Database</param>
        /// <returns>The result is a database object with added relations on tables.</returns>
        private void GetRelations(IDbConnection conn, Database database)
        {
            List<RelationTuple> relations = new List<RelationTuple>();

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    // Get relations in database
                    cmd.CommandText =
                        "SELECT " +
                        "    fk.name 'FK Name', " +
                        "    tp.name 'LinkTable', " +
                        "    SCHEMA_NAME(tp.schema_id) 'LinkSchema', " +
                        "    cp.name LinkColumn, " +
                        "    tr.name 'AnchorTable', " +
                        "    SCHEMA_NAME(tr.schema_id) 'AnchorSchema', " +
                        "    cr.name AnchorColumn " +
                        "FROM " +
                        "    sys.foreign_keys fk " +
                        "INNER JOIN " +
                        "    sys.tables tp ON fk.parent_object_id = tp.object_id " +
                        "INNER JOIN " +
                        "    sys.tables tr ON fk.referenced_object_id = tr.object_id " +
                        "INNER JOIN " +
                        "    sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id " +
                        "INNER JOIN " +
                        "    sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id " +
                        "INNER JOIN " +
                        "    sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id;";

                    // Execute query and construct relationships
                    using (var reader = cmd.ExecuteReader())
                    using (DataTable data = new DataTable())
                    {
                        data.Load(reader);

                        // Read through relations
                        foreach (DataRow row in data.Rows)
                        {
                            // Get tables related to relation
                            var anchorTable = database.Tables.First(t => t.Name == row.Field<string>("AnchorTable") && t.Schema == row.Field<string>("AnchorSchema"));
                            var linkTable = database.Tables.First(t => t.Name == row.Field<string>("LinkTable") && t.Schema == row.Field<string>("LinkSchema"));

                            var anchorColumn = anchorTable.Columns.First(c => c.Name == row.Field<string>("AnchorColumn"));
                            var linkColumn = linkTable.Columns.First(c => c.Name == row.Field<string>("LinkColumn"));

                            relations.Add(new RelationTuple(row.Field<string>("FK Name"), anchorColumn, linkColumn, anchorTable, linkTable));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e);
                throw;
            }

            // Group relations by foreignkey constraint name
            var relationGroups = relations.GroupBy(c => c.FKName).ToDictionary(g => g, g => g.ToList());

            // For each grouping of foreign keys
            foreach (var grouping in relationGroups.Values)
            {
                var firstTuple = grouping.First();

                var relation = new Relation(
                    firstTuple.AnchorTable,
                    firstTuple.LinkTable,
                    grouping.Select(g => g.Anchor).ToList(),
                    grouping.Select(g => g.Link).ToList(),
                    Cardinality.Unknown
                );

                // Add relation to both anchor and link table
                firstTuple.AnchorTable.AddRelation(relation);
            }

        }

        /// <summary>
        /// Handy intermediate object to store information about a relation
        /// </summary>
        private struct RelationTuple
        {
            public string FKName { get; }
            public Column Anchor { get; }
            public Column Link { get; }
            public Table AnchorTable { get; }
            public Table LinkTable { get; }

            public RelationTuple(string fkName, Column anchor, Column link, Table anchorTable, Table linkTable)
            {
                FKName = fkName;
                Anchor = anchor;
                Link = link;
                AnchorTable = anchorTable;
                LinkTable = linkTable;
            }
        }

        #endregion

        #region Table

        /// <summary>
        /// Get tables associated with a connection, and thereby a specific database
        /// </summary>
        /// <param name="conn">Connection to a database</param>
        /// <returns>A list of tables</returns>
        private List<Table> GetTables(IDbConnection conn, Database database)
        {
            List<Table> tables = new List<Table>();

            // Retrieve tables associated with connection
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME != 'sysdiagrams'";

                if (!_configuration.IncludeViews)
                    cmd.CommandText += " AND TABLE_TYPE != 'VIEW'";

                // Execute query and construct tables
                using (var reader = cmd.ExecuteReader())
                using (DataTable data = new DataTable())
                {
                    data.Load(reader);
                    foreach (DataRow row in data.Rows)
                    {
                        //Get the number of rows for the table
                        int rowCount = GetTableRowCount(conn, (string)row["TABLE_SCHEMA"], (string)row["TABLE_NAME"]);

                        tables.Add(new Table((string)row["TABLE_NAME"], (string)row["TABLE_SCHEMA"], rowCount));
                    }
                }
            }

            // Get columns for each table
            foreach (var table in tables)
            {
                table.Columns = GetColumns(conn, table);
                table.Database = database;

                GetConstraints(conn, table);
            }

            return tables;
        }

        /// <summary>
        /// Retrieves the number of rows in a table using COUNT(*)
        /// </summary>
        /// <param name="conn">The database the table is within</param>
        /// <param name="schemaName">Name of the schema</param>
        /// <param name="tableName">Name of the table</param>
        /// <returns>number of rows in the table</returns>
        private int GetTableRowCount(IDbConnection conn, string schemaName, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                // COUNT(*) gets the number of rows.
                cmd.CommandText = $"SELECT COUNT(*) AS count FROM [{schemaName}].[{tableName}];";

                return (int) cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Get constraints for columns, currently primary keys and unique columns
        /// </summary>
        /// <param name="conn">Connection string</param>
        /// <param name="table">Table to get constraints from</param>
        /// <returns>A modified table object with added constraints</returns>
        private void GetConstraints(IDbConnection conn, Table table)
        {
            List<ConstraintTuple> constraints = new List<ConstraintTuple>();

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    // Get primary key and unique constraints
                    cmd.CommandText =
                        $"SELECT CONSTRAINT_NAME, " +
                        $"       COLUMN_NAME, " +
                        $"       CAST(OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') AS bit) AS IsPrimaryKey, " +
                        $"       CAST(OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsUniqueCnst') AS bit) AS IsUnique " +
                        $"FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                        $"WHERE TABLE_CATALOG = '{table.Database.Name}' AND " +
                        $"      TABLE_SCHEMA = '{table.Schema}' AND " +
                        $"      TABLE_NAME = '{table.Name}'; ";

                    // Execute query and construct columns
                    using (var reader = cmd.ExecuteReader())
                    using (DataTable data = new DataTable())
                    {
                        data.Load(reader);

                        // Read through primary keys and unique values
                        foreach (DataRow row in data.Rows)
                        {
                            var column = table.Columns.First(c => c.Name == row.Field<string>("COLUMN_NAME"));

                            bool isPrimaryKey = row.Field<bool>("IsPrimaryKey");
                            bool isUnqiue = row.Field<bool>("IsUnique");
                            string constraintName = row.Field<string>("CONSTRAINT_NAME");

                            constraints.Add(new ConstraintTuple(isPrimaryKey, isUnqiue, constraintName, column));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e);
                throw;
            }

            // Group constraints by constraint name, since multiple columns can participate in primary key and unique constraints
            var constraintGroups = constraints.GroupBy(c => c.ConstraintName).ToDictionary(g => g, g => g.ToList());

            foreach (var grouping in constraintGroups.Values)
            {
                // If primary key constraint
                if (grouping.First().IsPrimaryKey)
                {
                    table.AddPrimaryCandidate(new PrimaryKey(grouping.Select(g => g.Column).ToList(), 1f));
                }

                // If primary key constraint
                if (grouping.First().IsUnique)
                {
                    table.AddUniqueCandidate(new Unique(grouping.Select(g => g.Column).ToList(), 1f));
                }
            }
        }

        /// <summary>
        /// Handy intermidiate class for constraints computation
        /// </summary>
        private struct ConstraintTuple
        {
            public bool IsPrimaryKey { get; }
            public bool IsUnique { get; }
            public string ConstraintName { get; }
            public Column Column { get; }

            public ConstraintTuple(bool isPrimaryKey, bool isUnique, string constraintName, Column column)
            {
                IsPrimaryKey = isPrimaryKey;
                IsUnique = isUnique;
                ConstraintName = constraintName;
                Column = column;
            }
        }

        #endregion

        #region Column

        /// <summary>
        /// Get columns for the specified table
        /// </summary>
        /// <param name="conn">Connection to database</param>
        /// <param name="table">Table to retrieve columns from</param>
        /// <returns>List of columns for table</returns>
        private List<Column> GetColumns(IDbConnection conn, Table table)    
        {
            List<Column> columns = new List<Column>();

            // Retrieve columns associated with table
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    // Join statement from http://stackoverflow.com/a/30583879
                    cmd.CommandText =
                        $"SELECT c.name, c.column_id, c.max_length, c.[precision], c.scale, c.is_nullable, t.name AS datatype " +
                        $"FROM sys.columns c JOIN sys.types t ON c.system_type_id = t.user_type_id or(c.system_type_id = 240 and c.user_type_id = t.user_type_id) " +
                        $"WHERE object_id = object_id('{table.Schema}.{table.Name}');";

                    // Execute query and construct columns
                    using (var reader = cmd.ExecuteReader())
                    using (DataTable data = new DataTable())
                    {
                        data.Load(reader);

                        // Iterate through all columns
                        foreach (DataRow row in data.Rows)
                        {
                            Column column = new Column(row.Field<int>("column_id"), row.Field<string>("name"));
                            column.Table = table;

                            // Add name candidate to column
                            column.AddNameCandidate(row.Field<string>("name"), 1f);

                            // Try to map DB specific datatype into a common datatype
                            try
                            {
                                DataType dt = DataType.FromSQLDataType(row.Field<string>("datatype"), row.Field<short>("max_length"), row.Field<byte>("precision"), row.Field<byte>("scale"));

                                column.AddDatatypeCandidate(dt, 0.95f); // Because of the mapping, then we allow for some uncertainty
                            }
                            catch(DataType.DataTypeConversionException e)
                            {
                                Logger.Log(Logger.Level.Warning, $"{table.Name}.{column.Name}: {e.Message}");
                                // If no conversion for the datatype is found, then assume that the data can fit in a WChar datatype
                                // Because of this assumption, then we give it a low propability of being correct
                                column.AddDatatypeCandidate(new DataType(OleDbType.WChar), 0.1f);
                            }

                            // Add nullable constraint to table
                            if ((bool)row["is_nullable"])
                                table.AddNotNullableCandidate(new NotNullable(column, 1f));

                            columns.Add(column);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e);
                throw;
            }

            return columns;
        }

        #endregion

        public override string SupportedSourceType()
        {
            return "SQL Server";
        }

        public override async Task<List<string>> GetDataSampleAsync(Column column, int amount = 100)
        {
            var table = column.Table;

            List<string> values = new List<string>(amount);

            using (var conn = new SqlConnection(table.Database.ConnectionString))
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT TOP {amount} [{column.OriginalName}] " +
                                      $"FROM [{table.Schema}].[{table.Originalname}]";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    using(var data = new DataTable())
                    {
                        data.Load(reader);

                        // Convert values to strings
                        foreach (DataRow row in data.Rows)
                        {
                            values.Add(row.ItemArray[0].ToString());
                        }
                    }
                }
            }

            return values;
        }

        public override async Task<List<string[]>> GetDataSampleAsync(int amount = 100, params Column[] columns)
        {
            var table = columns[0].Table;

            List<string[]> values = new List<string[]>(amount);

            using (var conn = new SqlConnection(table.Database.ConnectionString))
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT TOP {amount} {string.Join(", ", columns.Select(c => $"[{c.Name}]"))} " +
                                      $"FROM [{table.Schema}].[{table.Originalname}]";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    using (var data = new DataTable())
                    {
                        data.Load(reader);

                        foreach (DataRow row in data.Rows)
                        {
                            // Convert values to string arrays
                            string[] strings = row.ItemArray.Select(a => a.ToString()).ToArray();

                            values.Add(strings);
                        }
                    }
                }
            }

            return values;
        }
    }
}
