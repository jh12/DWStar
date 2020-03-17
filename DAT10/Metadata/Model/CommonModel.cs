using System.Collections.Generic;
using System.Linq;
using DAT10.Core.GenericGraph;

namespace DAT10.Metadata.Model
{
    public class CommonModel
    {
        private static int NextID;

        /// <summary>
        /// ID of where the common model originated from.
        /// Permutations of a common model should therefore
        /// have the same value here, as the original version.
        /// </summary>
        public int OriginID { get; } 

        public string Suffix { get; set; }

        public string GuiName => string.IsNullOrEmpty(Suffix) ? OriginID.ToString() : OriginID + Suffix;

        /// <summary>
        /// Tables in common model
        /// </summary>
        public List<Table> Tables { get; set; }

        public CommonModel(List<Table> tables)
        {
            Tables = new List<Table>(tables);
            OriginID = NextID++;
        }

        /// <summary>
        /// Add relations of tables to an undirected graph representing the relations between tables
        /// </summary>
        /// <param name="relationGraph">Relationship graph</param>
        public void AddRelationsToGraph(UnDirectedGraph<Table> relationGraph)
        {
            Dictionary<Table, GenericNode<Table>> nodes = new Dictionary<Table, GenericNode<Table>>();

            // Create graph nodes for each table
            foreach (var table in Tables)
            {
                nodes[table] = new GenericNode<Table>(table);
            }

            // Add vertices to graph
            relationGraph.AddVertexRange(nodes.Values);

            // Add edges to graph
            foreach (var node in nodes.Values)
            {
                foreach (var relation in node.Data.Relations)
                {
                    var anchorNode = nodes[relation.AnchorTable];
                    // If current node is anchor, then don't add an edge. Prevents duplicate edges
                    if(node.Data.Equals(anchorNode.Data))
                        continue;

                    relationGraph.AddEdge(new GenericEdge<Table>(node, anchorNode));
                }
            }
        }
    }
}
