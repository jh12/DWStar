using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;

namespace DAT10.Core.GenericGraph
{
    public class UnDirectedGraph<T> : UndirectedGraph<GenericNode<T>, GenericEdge<T>>
    {
        public UnDirectedGraph()
        {
        }

        public UnDirectedGraph(bool allowParallelEdges) : base(allowParallelEdges)
        {
        }
    }
}
