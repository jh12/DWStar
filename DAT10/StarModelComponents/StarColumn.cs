using System;
using System.Diagnostics;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents.Dimensions;

namespace DAT10.StarModelComponents
{
    [DebuggerDisplay("SC: {Name}")]
    public class StarColumn
    {
        
        public int Id { get; set; }
        public int Ordinal { get; set; }
        public String Name { get; set; }
        //Bruger datatypen fra common model
        public DataType DataType { get; set; }

        public StarColumnType ColumnType { get; set; }

        //Reference to the column that it originated from, from the common model.
        public Column ColumnRef;

        public StarModelTableBase TableRef { get; set; }

        public StarColumn() { }

        public StarColumn(Column column)
        {
            Id = column.ID;
            Name = column.Name;
            DataType = column.DataType;
            Ordinal = column.Ordinal;

            //Ref to orignal column
            ColumnRef = column;
        }

        public StarColumn(int ordinal, string name, DataType dataType, StarColumnType columnType)
        {
            Ordinal = ordinal;
            Name = name;
            DataType = dataType;
            ColumnType = columnType;
        }

        public StarColumn(Column column, string newName)
        {
            Id = column.ID;
            Name = newName;
            DataType = column.DataType;
            Ordinal = column.Ordinal;

            //Ref to orignal column
            ColumnRef = column;
            
        }

        /// <summary>
        /// Helper method to determine if the column from the common model was not null
        /// </summary>
        /// <returns></returns>
        public bool WasNotNull()
        {
            return ColumnRef
                .Table
                .NotNullables
                .Select(c => c.Column)
                .Contains(ColumnRef);
        }

        /// <summary>
        /// Helper method for checking if a columns is a foreign key for a table (given as argument)
        /// Useful for star phase.
        /// </summary>
        /// <param name="table">Table that will be checked against (link table)</param>
        /// <returns></returns>
        public bool WasForeignKeyInDimension(Dimension dimension)
        {
            return ColumnRef
                .Table
                .Relations
                .Where(r => r.AnchorTable == dimension.TableReference && r.LinkColumns.Contains(ColumnRef))
                .ToList().Count >= 1;
        }
    }
}
