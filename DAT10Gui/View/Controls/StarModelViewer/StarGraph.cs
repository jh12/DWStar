using System.Collections.Generic;
using System.Linq;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;
using QuickGraph;

namespace DAT10Gui.View.Controls.StarModelViewer
{
    public class StarGraph : BidirectionalGraph<StarNode, StarEdge>
    {
        public StarModel BasedOn { get; }

        public StarGraph(StarModel model)
        {
            BasedOn = model;
            Dictionary<StarModelTableBase, StarNode> nodes = new Dictionary<StarModelTableBase, StarNode>();

            nodes[model.FactTable] = new FactNode(model.FactTable);

            foreach (var dimension in model.Dimensions)
            {
                if(!dimension.IsRolePlaying)
                    nodes[dimension] = new DimensionNode((Dimension) dimension.ActualTable);
            }

            AddVertexRange(nodes.Values);

            List<StarNode> facts = nodes.Where(n => n.Key is FactTable).Select(kv => kv.Value).ToList();
            List<StarNode> dimensions = nodes.Where(n => n.Key is Dimension).Select(kv => kv.Value).ToList();

            foreach (DimensionNode dimension in dimensions)
            {
                foreach (var relation in dimension.Dimension.Relations)
                {
                    var linkNode = nodes[relation.LinkTable.ActualTable];

                    AddEdge(new StarEdge(linkNode, dimension));
                }
            }
        }
    }

    public class StarEdge : Edge<StarNode>
    {
        public StarEdge(StarNode source, StarNode target) : base(source, target)
        {
        }
    }

    public abstract class StarNode
    {
        public string Name => Table.Name;
        public List<StarColumn> Columns => Table.Columns;

        protected StarModelTableBase Table;

        protected StarNode(StarModelTableBase table)
        {
            Table = table;
        }
    }

    public class FactNode : StarNode
    {
        public FactTable FactTable => (FactTable) Table;

        public FactNode(FactTable table) : base(table)
        {
        }
    }

    public class DimensionNode : StarNode
    {
        public Dimension Dimension => (Dimension) Table;

        public DimensionNode(Dimension table) : base(table)
        {
        }
    }
}
