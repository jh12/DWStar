using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Core.GenericGraph
{
    public class GenericNode<T> : IComparable
    {
        public T Data;

        public GenericNode(T data)
        {
            Data = data;
        }

        public int CompareTo(object obj)
        {
            return obj.GetHashCode().CompareTo(GetHashCode());
        }
    }
}
