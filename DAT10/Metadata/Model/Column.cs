using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace DAT10.Metadata.Model
{
    [DebuggerDisplay("{BestName.Name} {DataType.Type}")]
    public class Column
    {
        protected static int NextAvailableID = 1;

        #region Properties

        /// <summary>
        /// Internal ID of column
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Ordinal of column in table
        /// </summary>
        public int Ordinal { get; }

        /// <summary>
        /// Original name of column (read-only)
        /// </summary>
        public string OriginalName { get; }

        /// <summary>
        /// List of name candidates
        /// </summary>
        protected List<NameCandidate> Names { get; set; } = new List<NameCandidate>();

        /// <summary>
        /// List of datatype candidates
        /// </summary>
        protected List<DatatypeCandidate> Datatypes { get; set; } = new List<DatatypeCandidate>();
        
        /// <summary>
        /// Reference to parent table
        /// </summary>
        public Table Table { get; set; }

        #endregion

        #region Helper properties
        /// <summary>
        /// Returns the most likely name
        /// </summary>
        public string Name
        {
            get
            {
                return
                    Names.GroupBy(n => new {n.Name, n.Confidence})
                        .Select(g => new {g.Key.Name, AvgConfidence = g.Average(g1 => g1.Confidence)})
                        .OrderByDescending(o => o.AvgConfidence)
                        .FirstOrDefault()?.Name;
            }
        }

        public NameCandidate BestName
        {
            get { return Names.OrderByDescending(n => n.Confidence).First(); }
        }

        /// <summary>
        /// Returns the most likely datatype
        /// </summary>
        public DataType DataType
        {
            get {
                return
                  Datatypes.GroupBy(d => new { d.Datatype, d.Confidence })
                      .Select(g => new { g.Key.Datatype, AvgConfidence = g.Average(g1 => g1.Confidence) })
                      .OrderByDescending(o => o.AvgConfidence)
                      .FirstOrDefault()?.Datatype;
            }
            //get { return Datatypes.OrderByDescending(n => n.Confidence).First().Datatype; }
        }

        #endregion

        public Column(int ordinal, string originalName)
        {
            ID = NextAvailableID++;
            Ordinal = ordinal;
            OriginalName = originalName;
        }

        public Column AddNameCandidate(string name, float confidence)
        {
            Names.Add(new NameCandidate(name, confidence));
            return this;
        }

        public Column AddDatatypeCandidate(DataType datatype, float confidence)
        {
            Datatypes.Add(new DatatypeCandidate(datatype, confidence));
            return this;
        }

        public bool IsKey()
        {
            return Table.PrimaryKey.Columns.Contains(this) || Table.Relations.Any(tableRelation => tableRelation.LinkColumns.Contains(this));
            //return
            //    Table.PrimaryKeys.Any(constraintsPrimaryKey => constraintsPrimaryKey.Columns.Contains(this)) ||
            //    Table.Relations.Any(tableRelation => tableRelation.LinkColumns.Contains(this));
        }

        public bool IsPrimaryKey()
        {
            return Table.PrimaryKey?.Columns.Contains(this) ?? false;
            //return
            //    Table.PrimaryKeys.Any(constraintsPrimaryKey => constraintsPrimaryKey.Columns.Contains(this));
        }

        public bool IsForeignKey()
        {
            return Table.Relations.Any(tableRelation => tableRelation.LinkColumns.Contains(this));
        }

        public string KeyString
        {
            get
            {
                var constraints = new List<string>
                {
                    IsPrimaryKey() ? "PK" : string.Empty,
                    IsForeignKey() ? "FK" : string.Empty,
                };

                return string.Join("|", constraints.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        public string ConstraintString
        {
            get
            {
                var constraints = new List<string>
                {
                    Table.IsNotNullable(this) != null ? "NN" : string.Empty,
                    Table.IsUnique(this) != null ? "UQ" : string.Empty,
                };

                return string.Join("|", constraints.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }
    }

    [DebuggerDisplay("{Name} ({Confidence*100}%)")]
    public struct NameCandidate
    {
        public string Name { get; }
        public float Confidence { get; }

        public NameCandidate(string name, float confidence)
        {
            Name = name;
            Confidence = confidence;
        }
    }

    [DebuggerDisplay("{Datatype.Type} ({Confidence*100}%)")]
    public struct DatatypeCandidate
    {
        public DataType Datatype { get; }
        public float Confidence { get; }

        public DatatypeCandidate(DataType datatype, float confidence)
        {
            Datatype = datatype;
            Confidence = confidence;
        }
    }
}
