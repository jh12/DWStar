using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Metadata.Model
{
    [DebuggerDisplay("{LinkTable.Name} --> {AnchorTable.Name}")]
    public class Relation
    {
        #region Properties

        /// <summary>
        /// Table containing (primary) key(s)
        /// </summary>
        public Table AnchorTable { get; set; }

        /// <summary>
        /// Table containing the foreign key(s)
        /// </summary>
        public Table LinkTable { get; set; }

        /// <summary>
        /// (Primary) key column(s) of anchor table
        /// </summary>
        public List<Column> AnchorColumns { get; set; }

        /// <summary>
        /// Foreign key column(s) of link table
        /// </summary>
        public List<Column> LinkColumns { get; set; }

        /// <summary>
        /// Cardinality in relation to anchor column(s)
        /// It is therefore seldom possible to have a many-to-one relationship, as the foreign key
        /// would then be on the "one" side
        /// </summary>
        public Cardinality Cardinality { get; set; }

        #endregion

        public Relation(Table anchorTable, Table linkTable, List<Column> anchorColumns, List<Column> linkColumns, Cardinality cardinality)
        {
            AnchorColumns = anchorColumns;
            LinkColumns = linkColumns;
            Cardinality = cardinality;
            AnchorTable = anchorTable;
            LinkTable = linkTable;
        }

        public override string ToString()
        {
            return LinkTable.Name + "#" + AnchorTable.Name;
        }
    }

    /// <summary>
    /// Cardinality of relation.
    /// </summary>
    public enum Cardinality : byte
    {
        [Description("1:1")]
        OneToOne,
        [Description("1:M")]
        OneToMany,
        [Description("M:1")]
        ManyToOne,
        [Description("M:M")]
        ManyToMany,
        [Description("Unknown")]
        Unknown
    }
}
