using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using DAT10.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DAT10.Metadata.Model
{
    [DebuggerDisplay("{Type} L:{Length} P:{Precision} S:{Scale}")]
    public class DataType
    {
        /// <summary>
        /// Datatype hierarchy
        /// </summary>
        public static DataTypeHierarchy Hierarchy { get; }

        #region Properties

        /// <summary>
        /// Type of datatype
        /// </summary>
        public OleDbType Type { get; set; }

        /// <summary>
        /// Length of datatype
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Precision of datatype
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Scale of datatype
        /// </summary>
        public int? Scale { get; set; }

        #endregion

        public DataType(OleDbType type)
        {
            Type = type;
        }

        public DataType(OleDbType type, int? length)
        {
            Type = type;
            Length = length;
        }

        public DataType(OleDbType type, int? length, int? precision, int? scale)
        {
            Type = type;

            if (length > 0 && (type == OleDbType.WChar || type == OleDbType.VarWChar || type == OleDbType.LongVarWChar))
                length /= 2;

            Length = length > 0 ? length : null;
            Precision = precision > 0 ? precision : null;
            Scale = scale > 0 ? scale : null;
        }

        /// <summary>
        /// Convert SQL server datatype to DataType instance
        /// </summary>
        /// <param name="sqlType">SQL Server datatype</param>
        /// <returns>DataType instance</returns>
        public static DataType FromSQLDataType(string sqlType, int? length, int? precision, int? scale)
        {
            SqlDbType result;
            if (!SqlDbType.TryParse(sqlType, true, out result))
                throw new DataTypeConversionException($"Could not convert from '{sqlType}' to a valid datatype.");

            return new DataType(_sqlToOleDbMap[result], length, precision, scale);
        }

        /// <summary>
        /// Map from SQL Server datatype to OleDB datatype
        /// </summary>
        private static Dictionary<SqlDbType, OleDbType> _sqlToOleDbMap = new Dictionary<SqlDbType, OleDbType>
        {
            [SqlDbType.BigInt] = OleDbType.BigInt,
            [SqlDbType.Binary] = OleDbType.Binary,
            [SqlDbType.Bit] = OleDbType.Boolean,
            [SqlDbType.Char] = OleDbType.Char,
            [SqlDbType.Date] = OleDbType.Date,
            [SqlDbType.DateTime] = OleDbType.DBTimeStamp,
            [SqlDbType.DateTime2] = OleDbType.DBTimeStamp,
            [SqlDbType.DateTimeOffset] = OleDbType.DBTimeStamp,
            [SqlDbType.Decimal] = OleDbType.Decimal,
            //[SqlDbType.]                  = OleDbType., <-- filestream?
            [SqlDbType.Float] = OleDbType.Single,
            [SqlDbType.Image] = OleDbType.LongVarBinary,
            [SqlDbType.Int] = OleDbType.Integer,
            [SqlDbType.Money] = OleDbType.Decimal,
            [SqlDbType.NChar] = OleDbType.WChar,
            [SqlDbType.NText] = OleDbType.LongVarWChar,
            //[SqlDbType.]                  = OleDbType., <-- numeric?
            [SqlDbType.NVarChar] = OleDbType.VarWChar,
            [SqlDbType.Real] = OleDbType.Single,
            //[SqlDbType.]                  = OleDbType., <-- rowversion?
            [SqlDbType.SmallDateTime] = OleDbType.DBTimeStamp,
            [SqlDbType.SmallInt] = OleDbType.SmallInt,
            [SqlDbType.SmallMoney] = OleDbType.Single,
            //[SqlDbType.]                  = OleDbType., <-- sqlvariant
            [SqlDbType.Text] = OleDbType.LongVarChar,
            [SqlDbType.Time] = OleDbType.DBTime,
            [SqlDbType.Timestamp] = OleDbType.DBTimeStamp,
            [SqlDbType.TinyInt] = OleDbType.TinyInt,
            [SqlDbType.UniqueIdentifier] = OleDbType.Guid,
            [SqlDbType.VarBinary] = OleDbType.VarBinary,
            [SqlDbType.VarChar] = OleDbType.VarChar,
            [SqlDbType.Xml] = OleDbType.LongVarChar,
        };

        /// <summary>
        /// Check if datatype is numeric
        /// </summary>
        /// <returns>True if numeric, otherwise false</returns>
        public bool IsNumeric()
        {
            switch (Type)
            {
                case OleDbType.BigInt:
                case OleDbType.Currency:
                case OleDbType.Decimal:
                case OleDbType.Double:
                case OleDbType.Integer:
                case OleDbType.Numeric:
                case OleDbType.Single:
                case OleDbType.SmallInt:
                case OleDbType.TinyInt:
                case OleDbType.UnsignedBigInt:
                case OleDbType.UnsignedInt:
                case OleDbType.UnsignedSmallInt:
                case OleDbType.UnsignedTinyInt:
                case OleDbType.VarNumeric:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if datatype is a string
        /// </summary>
        /// <returns>True if string, otherwise false</returns>
        public bool IsString()
        {
            switch (Type)
            {
                case OleDbType.BSTR:
                case OleDbType.Char:
                case OleDbType.LongVarChar:
                case OleDbType.LongVarWChar:
                case OleDbType.VarChar:
                case OleDbType.VarWChar:
                case OleDbType.WChar:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if datatype is a temporal (date or time related)
        /// </summary>
        /// <returns>True if temporal, otherwise false</returns>
        public bool IsTemporal()
        {
            switch (Type)
            {
                case OleDbType.Date:
                case OleDbType.DBDate:
                case OleDbType.DBTime:
                case OleDbType.DBTimeStamp:
                    return true;
                default:
                    return false;
            }
        }

        protected bool Equals(DataType other)
        {
            return Type == other.Type && Length == other.Length && Precision == other.Precision && Scale == other.Scale;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DataType)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Type;
                hashCode = (hashCode * 397) ^ Length.GetHashCode();
                hashCode = (hashCode * 397) ^ Precision.GetHashCode();
                hashCode = (hashCode * 397) ^ Scale.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{Type}";
        }

        [Serializable]
        public class DataTypeConversionException : Exception
        {
            public DataTypeConversionException()
            {
            }

            public DataTypeConversionException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Return the oledbtype that the two parameters have in common
        /// </summary>
        /// <param name="one">OleDB type one</param>
        /// <param name="two">OleDB type one</param>
        /// <returns>Common OleDB type</returns>
        public static OleDbType CommonType(OleDbType one, OleDbType two)
        {
            // Trivial case: If both are equal
            if (one == two)
                return one;

            if (one == OleDbType.Error)
                return two;

            if (two == OleDbType.Error)
                return one;

            // Find ancestors of one and two
            List<OleDbType> oneAncestors = FindAncestors(one, Hierarchy.Root);
            List<OleDbType> twoAncestors = FindAncestors(two, Hierarchy.Root);

            // Reverse since the function returns the a list of ancestors radiating from the parameter
            oneAncestors.Reverse();
            twoAncestors.Reverse();

            // Walk down the list and return if the pair does not match
            // E.g. 
            // string  = string (true)
            // decimal = decimal (true)
            // integer = double (false) => return decimal 
            int i = 0;
            for (; i < Math.Min(oneAncestors.Count, twoAncestors.Count); i++)
            {
                if (oneAncestors[i] != twoAncestors[i])
                    return oneAncestors[i - 1];
            }

            return oneAncestors[i-1];
        }

        /// <summary>
        /// Find ancestors of a datatype. Essentially walk down a tree and search for a specific node
        /// </summary>
        /// <param name="datatype">Datatype to search for</param>
        /// <param name="currNode">Current node in the tree</param>
        /// <returns>A list of ancestors of the datatype</returns>
        private static List<OleDbType> FindAncestors(OleDbType datatype, HierarchyNode currNode)
        {
            if (currNode.OlebDbType == datatype)
                return new List<OleDbType> { datatype };

            // Terminate if looking at leaf
            if (currNode.Children == null)
                return null;

            // Visit children
            foreach (var currNodeChild in currNode.Children)
            {
                var oleDbTypes = FindAncestors(datatype, currNodeChild);
                if (oleDbTypes != null && oleDbTypes.Count > 0)
                {
                    oleDbTypes.Add(currNode.OlebDbType);
                    return oleDbTypes;
                }
            }

            return new List<OleDbType>();
        }

        static DataType()
        {
            Hierarchy = CreateDefaultHierarchy();

            // JSON serialization of hierarchies
            //var hierarchyPath = Path.Combine(ModuleEngine.ProgramPath, "Datatype hierarchy.json");

            //if (!File.Exists(hierarchyPath))
            //{
            //    Hierarchy = CreateDefaultHierarchy();

            //    using (var fileStream = File.Create(hierarchyPath))
            //    using (var writer = new StreamWriter(fileStream))
            //    {
            //        writer.Write(JsonConvert.SerializeObject(Hierarchy, new JsonSerializerSettings {Converters = new[] {new StringEnumConverter()}}));
            //    }

            //    return;
            //}

            //using (var fileStream = File.OpenRead(hierarchyPath))
            //using (var reader = new StreamReader(fileStream))
            //{
            //    string json = reader.ReadToEnd();
            //    Hierarchy = JsonConvert.DeserializeObject<DataTypeHierarchy>(json);
            //}
        }

        private static DataTypeHierarchy CreateDefaultHierarchy()
        {
            // String hierarchy
            var varwchar = new HierarchyNode(OleDbType.VarWChar);
            var hierarchy = new DataTypeHierarchy(varwchar);

            var boolean = new HierarchyNode(OleDbType.Boolean);
            varwchar.Children.Add(boolean);

            var datetime = new HierarchyNode(OleDbType.DBTimeStamp);
            varwchar.Children.Add(datetime);

            // Decimal hierarchy
            var @decimal = new HierarchyNode(OleDbType.Decimal);
            varwchar.Children.Add(@decimal);

            // Double hierarchy
            var @double = new HierarchyNode(OleDbType.Double);
            @decimal.Children.Add(@double);

            var single = new HierarchyNode(OleDbType.Single);
            @double.Children.Add(single);

            // Integer hierarchy
            var bigint = new HierarchyNode(OleDbType.BigInt);
            @double.Children.Add(bigint);

            var integer = new HierarchyNode(OleDbType.Integer);
            bigint.Children.Add(integer);

            var smallint = new HierarchyNode(OleDbType.SmallInt);
            integer.Children.Add(smallint);

            var tinyint = new HierarchyNode(OleDbType.TinyInt);
            smallint.Children.Add(tinyint);

            return hierarchy;
        }
    }
}