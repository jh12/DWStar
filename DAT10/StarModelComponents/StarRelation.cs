using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents
{
    /// <summary>
    /// Relation between tables in the star model
    /// NB: An instance must be referenced both by the anchor and link table!
    /// </summary>
    [DebuggerDisplay("{LinkTable.Name} --> {AnchorTable.Name}#{AnchorTable.ID}")]
    public class StarRelation
    {
        /// <summary>
        /// Table containing (primary) key(s)
        /// </summary>
        public StarModelTableBase AnchorTable { get; set; }

        /// <summary>
        /// Table containing the foreign key(s)
        /// </summary>
        public StarModelTableBase LinkTable { get; set; }

        /// <summary>
        /// (Primary) key column(s) of anchor table
        /// </summary>
        public List<StarColumn> AnchorColumns { get; set; }

        /// <summary>
        /// Foreign key column(s) of link table
        /// </summary>
        public List<StarColumn> LinkColumns { get; set; }

        /// <summary>
        /// Cardinality in relation to anchor column(s)
        /// It is therefore seldom possible to have a many-to-one relationship, as the foreign key
        /// would then be on the "one" side
        /// </summary>
        public Cardinality Cardinality { get; set; }

        public bool RequiresSurrogateKeys() => AnchorColumns.Count == 0 && LinkColumns.Count == 0;
        public bool HasAnchorColumns() => LinkColumns.Count > 0;
        public bool HasLinkColumns() => LinkColumns.Count > 0;

        public StarRelation(StarModelTableBase anchorTable, StarModelTableBase linkTable, Cardinality cardinality)
        {
            AnchorTable = anchorTable;
            LinkTable = linkTable;
            LinkColumns = new List<StarColumn>();
            AnchorColumns = new List<StarColumn>();
            Cardinality = cardinality;
        }

        public StarRelation(StarModelTableBase anchorTable, StarModelTableBase linkTable, List<StarColumn> anchorColumns, List<StarColumn> linkColumns, Cardinality cardinality)
        {
            AnchorTable = anchorTable;
            LinkTable = linkTable;
            AnchorColumns = anchorColumns;
            LinkColumns = linkColumns;
            Cardinality = cardinality;
        }
    }
}
