using System.Collections.Generic;
using System.Linq;

namespace DAT10.Metadata.Model
{
    public class Constraints
    {
        #region Properties

        /// <summary>
        /// Primary keys for table
        /// </summary>
        public List<PrimaryKey> PrimaryKeys { get; set; } = new List<PrimaryKey>();

        /// <summary>
        /// Non-null columns for table
        /// </summary>
        public List<NotNullable> NotNullables { get; set; } = new List<NotNullable>();

        /// <summary>
        /// Unique constraints on columns
        /// </summary>
        public List<Unique> Uniques { get; set; } = new List<Unique>();

        #endregion

        /// <summary>
        /// Check if column is non-nullable
        /// </summary>
        /// <param name="column">Column</param>
        /// <returns>Constraint related to column</returns>
        public NotNullable IsNotNullable(Column column)
        {
            return NotNullables.FirstOrDefault(n => n.Column == column);
        }

        /// <summary>
        /// Check if column(s) has an unique constraint
        /// </summary>
        /// <param name="columns">List of columns that should participate in uniqueness constraint</param>
        /// <returns>Constraint related to columns</returns>
        public Unique IsUnique(params Column[] columns)
        {
            HashSet<Column> input = new HashSet<Column>(columns);

            return Uniques.FirstOrDefault(u => new HashSet<Column>(u.Columns).SetEquals(input));
        }

        public string PKs => string.Join("|", PrimaryKeys.Select(p => string.Join(", ", p.Columns.Select(c => c.Name))));
        public string UQs => string.Join("|", Uniques.Select(p => string.Join(", ", p.Columns.Select(c => c.Name))));
        public string NLs => string.Join(", ", NotNullables.Select(p => p.Column.Name));
    }

    public class Unique
    {
        public HashSet<Column> Columns { get; set; }
        public float Confidence { get; set; }

        public Unique(List<Column> columns, float confidence)
        {
            Columns = new HashSet<Column>(columns);
            Confidence = confidence;
        }
    }

    public class NotNullable
    {
        public Column Column { get; set; }
        public float Confidence { get; set; }

        public NotNullable(Column column, float confidence)
        {
            Column = column;
            Confidence = confidence;
        }
    }

    public class PrimaryKey
    {
        public HashSet<Column> Columns { get; set; }
        public float Confidence { get; set; }

        public PrimaryKey(Column column, float confidence)
        {
            Columns = new HashSet<Column> {column};
            Confidence = confidence;
        }

        public PrimaryKey(List<Column> columns, float confidence)
        {
            Columns = new HashSet<Column>(columns);
            Confidence = confidence;
        }
    }
}