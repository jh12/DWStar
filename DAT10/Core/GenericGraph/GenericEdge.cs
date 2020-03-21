using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Core.GenericGraph
{
    public class GenericEdge<T> : QuickGraph.Edge<GenericNode<T>>
    {
        public GenericEdge(GenericNode<T> source, GenericNode<T> target) : base(source, target)
        {
        }
    }
}
