using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Utils;
using Microsoft.VisualBasic.FileIO;
using QuickGraph;
using SimpleLogger;
using Column = DAT10.Metadata.Model.Column;

namespace DAT10.Modules.Inference.FlatFiles
{
    public class CSVFileInference : InferenceModuleBase
    {
        private CSVFileConfiguration _configuration;

        private Dictionary<string, bool> TableHasHeaders = new Dictionary<string, bool>();
        
        public CSVFileInference(DataSampleService sampleService) : base(sampleService)
        {
            _configuration = new CSVFileConfiguration();
        }

        public override string Name { get; } = "CSV Metadata";
        public override string Description { get; } = "Retrieves tables names, column names (if headers are present), data types, and ordinals from CSV files.";

        public override bool IsValidConnection(string connection)
        {
            return Directory.Exists(connection);
        }

        // TODO: Does only work for simple CSV files. If commas or qoutes are used in the columns, then it crashes
        protected override CommonModel GetSchemaForConnection(string connectionstring)
        {
            List<Table> tables = new List<Table>();

            // Get all csv files in folder
            foreach (var filepath in Directory.GetFiles(connectionstring, "*.csv"))
            {
                var table = new Table(Path.GetFileNameWithoutExtension(filepath), "CSV", 0);
                table.Database = new Database(Path.GetDirectoryName(connectionstring), connectionstring, "CSV");

                // Find potential headers
                string[] names;
                bool headersExists;
                ContainsHeaders(filepath, out headersExists, out names);
                TableHasHeaders.Add(table.GetFullName(), headersExists);
                
                // Best datatypes found
                DataType[] currDataTypes = null;

                int currentRow = 0;
                //using (var streamReader = File.OpenText(filepath))
                using (TextFieldParser streamReader = new TextFieldParser(filepath))
                {
                    streamReader.TextFieldType = FieldType.Delimited;
                    streamReader.SetDelimiters(_configuration.RowDelimiter);
                    streamReader.HasFieldsEnclosedInQuotes = true;
                    streamReader.TrimWhiteSpace = false;
                    // Iterate through rows to examine
                    while (!streamReader.EndOfData && currentRow < _configuration.RowsToExamine)
                    {
                        // If headers exists, ignore the first row of data
                        if (headersExists && currentRow == 0)
                        {
                            streamReader.ReadLine();
                            currentRow++;
                            continue;
                        }

                        //var line = streamReader.ReadLine();
                        //var columnValues = SplitLine(line);
                        var columnValues = streamReader.ReadFields();

                        // Set data types array
                        var rowDataTypes = new DataType[columnValues.Length];

                        // Create data types for all values
                        for (int i = 0; i < columnValues.Length; i++)
                        {
                            rowDataTypes[i] = CreateDatatype(columnValues[i]);
                        }

                        // Update currDataTypes with the widened version of this rows datatypes
                        currDataTypes = Widen(rowDataTypes, currDataTypes);

                        currentRow++;
                    }

                    // Iterate through rest to get row count
                    while (!streamReader.EndOfData)
                    {
                        streamReader.ReadLine();
                        currentRow++;
                    }

                    // If no rows are available, but the file has headers.
                    if (currDataTypes == null && headersExists)
                    {
                        currDataTypes = new DataType[names.Length];

                        // Generate varwchar columns for each column
                        for (var i = 0; i < currDataTypes.Length; i++)
                        {
                            currDataTypes[i] = new DataType(OleDbType.VarWChar);
                        }
                    }

                    // If any datatypes are found, then add columns for them
                    if (currDataTypes != null)
                    {
                        for (var i = 0; i < currDataTypes.Length; i++)
                        {
                            Column column = new Column(i, string.Empty);

                            if (headersExists)
                                column.AddNameCandidate(names[i], 1f);
                            else
                                column.AddNameCandidate($"Column_{i + 1}", 0.01f);

                            column.AddDatatypeCandidate(currDataTypes[i], 0.5f);
                            table.AddColumn(column);
                        }
                    }
                }
                // Ensure rowcount is correct based on whether the file has headers or not    
                table.RowCount = headersExists ? currentRow - 1 : currentRow;
                tables.Add(table);
            }

            return new CommonModel(tables);
        }

        private void ContainsHeaders(string filepath, out bool containsHeaders, out string[] names)
        {
            List<OleDbType[]> alldatatypes = new List<OleDbType[]>();
            List<string> possibleHeaders = new List<string>();

            using (TextFieldParser parser = new TextFieldParser(filepath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(_configuration.RowDelimiter);
                parser.HasFieldsEnclosedInQuotes = true;

                int currentRow = 0;

                while (!parser.EndOfData && currentRow < _configuration.RowsToExamine)
                {
                    //Process row
                    string[] fields = parser.ReadFields();
                    OleDbType[] datatypes = new OleDbType[fields.Length];
                    for (int index = 0; index < fields.Length; index++)
                    {
                        string field = fields[index];
                        var datatype = GetDataType(field);
                        datatypes[index] = datatype;
                    }
                    alldatatypes.Add(datatypes);

                    if (currentRow == 0)
                    {
                        possibleHeaders.AddRange(fields);
                    }

                    currentRow++;
                }
            }

            bool isFirstRowChars = alldatatypes[0].ToList().Count(x => (x == OleDbType.VarWChar)) == alldatatypes[0].Length;

            bool allWChars = true;
            foreach (OleDbType[] alldatatype in alldatatypes)
            {
                var count = alldatatype.Count(x => x == OleDbType.VarWChar);
                if (count != alldatatype.Length)
                {
                    allWChars = false;
                }
            }


            if ((isFirstRowChars && !allWChars) || alldatatypes.Count == 1)
            {
                containsHeaders = true;
                names = possibleHeaders.ToArray();
            }
            else
            {
                containsHeaders = false;
                names = null;
            }
        }

        /// <summary>
        /// Widen currDataTypes enough to ensure that the datatypes in rowDataTypes fits in it
        /// </summary>
        /// <param name="rowDataTypes">Datatypes of row</param>
        /// <param name="currDataTypes">The currently acceptable datatypes</param>
        /// <returns>Widened datatypes</returns>
        private DataType[] Widen(DataType[] rowDataTypes, DataType[] currDataTypes)
        {
            if (currDataTypes == null)
                return rowDataTypes;

            for (var i = 0; i < rowDataTypes.Length; i++)
            {
                rowDataTypes[i] = Widen(rowDataTypes[i], currDataTypes[i]);
            }

            return rowDataTypes;
        }

        /// <summary>
        /// Widen datatype
        /// </summary>
        /// <param name="one">Datatype one</param>
        /// <param name="two">Datatype two</param>
        /// <returns>Widened datatype</returns>
        private DataType Widen(DataType one, DataType two)
        {
            OleDbType type = DataType.CommonType(one.Type, two.Type);

            one.Type = type;
            one.Length = Math.Max(one.Length.GetValueOrDefault(0), two.Length.GetValueOrDefault(0));
            one.Precision = Math.Max(one.Precision.GetValueOrDefault(0), two.Precision.GetValueOrDefault(0));
            one.Scale = Math.Max(one.Scale.GetValueOrDefault(0), two.Scale.GetValueOrDefault(0));

            return one;
        }

        /// <summary>
        /// Get datatype from string
        /// </summary>
        /// <param name="value">Value to determine datatype form</param>
        /// <returns>Suggested datatype</returns>
        // TODO: Extend to use hierarchies?
        private OleDbType GetDataType(string value)
        {
            // Required out parameters. Please ignore
            int ignoredInt;
            double ignoredDouble;
            decimal ignoredDecimal;
            DateTime ignoredDateTime;
            bool ignoredBool;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture.NumberFormat, out ignoredInt))
                return OleDbType.Integer;

            if (double.TryParse(value, out ignoredDouble))
                return OleDbType.Double;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out ignoredDecimal))
                return OleDbType.Decimal;

            if(DateTime.TryParse(value, out ignoredDateTime))
                return OleDbType.DBTimeStamp;

            if (bool.TryParse(value, out ignoredBool))
                return OleDbType.Boolean;

            return OleDbType.VarWChar;
        }

        /// <summary>
        /// Create datatype from value
        /// </summary>
        /// <param name="value">Value to create datatype from</param>
        /// <returns>Datatype</returns>
        private DataType CreateDatatype(string value)
        {
            var oleDbType = GetDataType(value);
            switch (oleDbType)
            {
                case OleDbType.Integer:
                    return new DataType(oleDbType, value.Length, null, null);
                case OleDbType.WChar:
                case OleDbType.VarWChar:
                    return new DataType(oleDbType, value.Length*2, null, null);
                case OleDbType.Decimal:
                    return new DataType(oleDbType, value.Length - 1);
                case OleDbType.Double:
                    return new DataType(oleDbType, value.Length - 1);
                case OleDbType.DBTimeStamp:
                    return new DataType(oleDbType);
                case OleDbType.Boolean:
                    return new DataType(oleDbType);
                default:
                    return new DataType(OleDbType.Error, null, null, null);
            }
        }

        public override async Task<List<string>> GetDataSampleAsync(Column column, int amount = 100)
        {
            var table = column.Table;
            List<string> values = new List<string>(Math.Min(amount, table.RowCount));

            await Task.Run(() =>
            {
                //using (
                //    var streamReader =
                //        File.OpenText(Path.Combine(table.Database.ConnectionString, $"{table.Originalname}.csv")))
                //{
                using (var streamReader = new TextFieldParser(Path.Combine(table.Database.ConnectionString, $"{table.Originalname}.csv")))
                { 
                    streamReader.TextFieldType = FieldType.Delimited;
                    streamReader.SetDelimiters(",");
                    streamReader.HasFieldsEnclosedInQuotes = true;
                    while (!streamReader.EndOfData && amount > 0)
                    {
                        if (streamReader.LineNumber == 1 && TableHasHeaders[table.GetFullName()])
                        {
                            streamReader.ReadLine();
                            continue;
                        }
                        //var line = streamReader.ReadLine();
                        //var columnValues = SplitLine(line);
                        var columnValues = streamReader.ReadFields();
                        values.Add(columnValues[column.Ordinal]);
                        amount--;
                    }
                }
            });

            return values;
        }

        public override async Task<List<string[]>> GetDataSampleAsync(int amount = 100, params Column[] columns)
        {
            var table = columns.FirstOrDefault().Table;
            List<string[]> values = new List<string[]>(Math.Min(amount, table.RowCount));

            await Task.Run(() =>
            {
                //using (
                //    var streamReader =
                //        File.OpenText(Path.Combine(table.Database.ConnectionString, $"{table.Originalname}.csv")))
                //{
                using (var streamReader = new TextFieldParser(Path.Combine(table.Database.ConnectionString, $"{table.Originalname}.csv")))
                {
                    streamReader.TextFieldType = FieldType.Delimited;
                    streamReader.SetDelimiters(",");
                    streamReader.HasFieldsEnclosedInQuotes = true;
                    while (!streamReader.EndOfData && amount > 0)
                    {
                        if (streamReader.LineNumber == 1 && TableHasHeaders[table.GetFullName()])
                        {
                            streamReader.ReadLine();
                            continue;
                        } 

                        var columnValues = streamReader.ReadFields();

                        List<string> rowValues = new List<string>();

                        foreach (Column column in columns)
                        {
                            rowValues.Add(columnValues[column.Ordinal]);
                        }

                        values.Add(rowValues.ToArray());

                        amount--;
                    }
                }
            });

            return values;
        }

        public override string SupportedSourceType()
        {
            return "CSV";
        }
    }
}
