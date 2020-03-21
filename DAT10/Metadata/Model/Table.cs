using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Metadata.Model
{
    [DebuggerDisplay("{" + nameof(Schema) + "}.{" + nameof(Name) + "}")]
    public class Table
    {
        #region Properties

        /// <summary>
        /// Name of table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Original name of table (read-only)
        /// </summary>
        public string Originalname { get; }

        /// <summary>
        /// Schema of table
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// List of columns
        /// </summary>
        public List<Column> Columns { get; set; }

        /// <summary>
        /// Relations between tables
        /// </summary>
        public List<Relation> Relations { get; set; }

        /// <summary>
        /// List of constraints for columns
        /// </summary>
        protected Constraints Constraints { get; set; }

        // Reference to parent database
        public Database Database { get; set; }

        //Approximate table size (num. of column)
        public int RowCount { get; set; }

        #endregion

        public Table(string name, string schema, int rowCount)
        {
            Name = name;
            Originalname = name;
            Schema = schema;
            RowCount = rowCount;

            Columns = new List<Column>();
            Relations = new List<Relation>();
            Constraints = new Constraints();
        }

        public void AddColumn(params Column[] columns)
        {
            foreach (var column in columns)
            {
                Columns.Add(column);
                column.Table = this;
            }
        }

        public string Rel
            =>
            string.Join("|",
                Relations.Select(
                    r =>
                        string.Join(", ",
                            $@"{{{string.Join(", ", r.AnchorColumns.Select(c => c.Name))}}} <- {{{string.Join(", ",
                                r.LinkColumns.Select(c => c.Name))}}} ({r.Cardinality})")));

        public PrimaryKey PrimaryKey
        {
            get
            {
                var first = Constraints.PrimaryKeys.GroupBy(pk => pk.Columns, HashSet<Column>.CreateSetComparer())
                    .Select(g => new
                    {
                        Columns = g.Key,
                        AvgConfidence = g.Average(g1 => g1.Confidence)
                    })
                    .OrderByDescending(o => o.AvgConfidence)
                    .FirstOrDefault();

                if (first != null)
                    return new PrimaryKey(new List<Column>(first.Columns), first.AvgConfidence);

                return null;
            }
        }

        public Unique IsUnique(Column column)
        {
            var first = Constraints.Uniques.Where(u => u.Columns.Contains(column)).GroupBy(uq => uq.Columns, HashSet<Column>.CreateSetComparer())
                .Select(g => new
                {
                    Columns = g.Key,
                    AvgConfidence = g.Average(g1 => g1.Confidence)
                })
                .OrderByDescending(o => o.AvgConfidence)
                .FirstOrDefault();

            if (first != null)
                return new Unique(new List<Column>(first.Columns), first.AvgConfidence);

            return null;

        }

        public NotNullable IsNotNullable(Column column)
        {
            var first = Constraints.NotNullables.Where(nn => nn.Column == column).GroupBy(uq => uq.Column)
                .Select(g => new
                {
                    Columns = g.Key,
                    AvgConfidence = g.Average(g1 => g1.Confidence)
                })
                .OrderByDescending(o => o.AvgConfidence)
                .FirstOrDefault();

            if (first != null)
                return new NotNullable(first.Columns, first.AvgConfidence);

            return null;
        }

        public IEnumerable<PrimaryKey> PrimaryKeys
        {
            get { return Constraints.PrimaryKeys; }
        }

        public IEnumerable<NotNullable> NotNullables
        {
            get { return Constraints.NotNullables; }
        }

        public IEnumerable<Unique> Uniques
        {
            get { return Constraints.Uniques; }
        }

        #region AddCandidates

        public void AddPrimaryCandidate(PrimaryKey candidate)
        {
            Constraints.PrimaryKeys.Add(candidate);
        }

        public void AddUniqueCandidate(Unique candidate)
        {
            Constraints.Uniques.Add(candidate);
        }

        public void AddNotNullableCandidate(NotNullable candidate)
        {
            Constraints.NotNullables.Add(candidate);
        }

        #endregion

    }
}