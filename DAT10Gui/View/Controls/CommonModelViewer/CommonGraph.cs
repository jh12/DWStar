using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Utils;
using QuickGraph;

namespace DAT10Gui.View.Controls.CommonModelViewer
{
    public sealed class CommonGraph : BidirectionalGraph<CommonNode, CommonEgde>
    {
        public CommonModel BasedOn { get; }

        public CommonGraph(CommonModel commonModel)
        {
            BasedOn = commonModel;

            Dictionary<Table, CommonNode> nodes = new Dictionary<Table, CommonNode>();

            foreach (var table in commonModel.Tables)
            {
                nodes[table] = new CommonNode(table);
            }

            AddVertexRange(nodes.Values);

            foreach (var node in nodes.Values)
            {
                foreach (var relation in node.Table.Relations)
                {
                    var anchorNode = nodes[relation.AnchorTable];
                    if (node.Table.Equals(anchorNode.Table))
                        continue;

                    AddEdge(new CommonEgde(node, anchorNode, relation));
                }
            }
        }
    }

    public class CommonEgde : Edge<CommonNode>
    {
        public Relation Relation;
        public string Cardinality => Relation.Cardinality.GetDescription();

        public CommonEgde(CommonNode source, CommonNode target, Relation relation) : base(source, target)
        {
            Relation = relation;
        }
    }

    public class CommonNode
    {
        public Table Table { get; private set; }

        public CommonNode(Table table)
        {
            Table = table;
        }
    }
}
