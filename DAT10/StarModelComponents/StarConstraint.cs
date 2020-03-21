using System.Collections.Generic;

namespace DAT10.StarModelComponents
{
    public class StarConstraints
    {
        /// <summary>
        /// Primary keys for table
        /// </summary>
        public PrimaryKey PrimaryKey { get; set; } = new PrimaryKey();

        /// <summary>
        /// Non-null columns for table
        /// </summary>
        public List<NotNullable> NotNullables { get; set; } = new List<NotNullable>();

        /// <summary>
        /// Unique constraints on columns
        /// </summary>
        public List<Unique> Uniques { get; set; } = new List<Unique>();
    }

    public class Unique
    {
        public IList<StarColumn> Columns { get; set; }

        public Unique(StarColumn column)
        {
            Columns = new List<StarColumn>{column};
        }

        public Unique(IList<StarColumn> columns)
        {
            Columns = columns;
        }
    }

    public class NotNullable
    {
        public StarColumn Column { get; set; }

        public NotNullable(StarColumn column)
        {
            Column = column;
        }
    }

    public class PrimaryKey
    {
        public List<StarColumn> Columns { get; set; }

        public PrimaryKey()
        {
            Columns = new List<StarColumn>();
        }

        public PrimaryKey(StarColumn column)
        {
            Columns = new List<StarColumn> { column };
        }

        public PrimaryKey(List<StarColumn> columns)
        {
            Columns = columns;
        }
    }
}
