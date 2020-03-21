using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Modules;

namespace DAT10.Core
{
    /// <summary>
    /// Class used to represent module dependencies and thereby generating an ordering of them
    /// </summary>
    internal class DependencyGraph
    {
        private List<Node> _nodes;
        private Dictionary<Node, List<Edge>> _edges; // Adjacency list

        public DependencyGraph(List<IDependent> modules)
        {
            // Create nodes
            _nodes = modules.Select(m => new Node(m)).ToList();

            _edges = new Dictionary<Node, List<Edge>>();
            CreateEdges();
        }

        /// <summary>
        /// Create edges based on dependencies on other nodes
        /// </summary>
        private void CreateEdges()
        {
            foreach (var node in _nodes)
            {
                _edges[node] = _nodes.Where(n => !n.Equals(node) // Ensure no dependencies on self
                                                 && (node.Module.Requires & n.Module.Affects) > 0) // Check if node has a dependency on n. If so, create an edge from node to n.
                                     .Select(e => new Edge(e, node)).ToList();
            }
        }

        #region Ordering
        /// <summary>
        /// Sorts the nodes topologically
        /// </summary>
        /// <returns>Reversed topological sorted list</returns>
        public List<IDependent> TopologicalSort()
        {
            List<DFSNode> dfsNodes = _nodes.Select(n => new DFSNode(n)).ToList();

            DepthFirstSearch(dfsNodes);

            // Order by finishing times and reverse list to represent least dependent node first
            return dfsNodes.OrderByDescending(n => n.FinishedTime).Select(dfsnode => dfsnode.Node.Module).Reverse().ToList();
        }

        /// <summary>
        /// Performs a Depth First Search on a list of nodes
        /// Reference: DFS Algorithm taken from introduction to algorithms book, page 604 
        /// </summary>
        /// <param name="DFSNodes">Vertices to perform search on</param>
        private void DepthFirstSearch(List<DFSNode> DFSNodes)
        {
            int time = 0;

            foreach (var dfsNode in DFSNodes)
            {
                // White = have not visited yet
                if (dfsNode.Color == DFSNode.Colors.White)
                    time = DFSVisit(DFSNodes, dfsNode, time);
            }
        }

        /// <summary>
        /// Performs the visit part of Depth First Search
        /// Reference: DFS Algorithm taken from introduction to algorithms book, page 604 
        /// </summary>
        /// <param name="nodes">List of nodes</param>
        /// <param name="n">Node to visit</param>
        /// <param name="time">Time taken to visit node</param>
        /// <returns></returns>
        private int DFSVisit(List<DFSNode> nodes, DFSNode n, int time)
        {
            // Increment time, assign discovered time (n.DiscoverTime) and new color to node
            // Gray = Visited, but isn't finished with this node yet
            time++;
            n.DiscoverTime = time;
            n.Color = DFSNode.Colors.Grey;

            foreach (var edge in _edges[n.Node])
            {
                // Get the first node, which is pointed to by the current node. Other ordering could also be used
                var dfsNode = nodes.First(dfsnode => dfsnode.Node == edge.In);

                if (dfsNode.Color == DFSNode.Colors.White)
                    time = DFSVisit(nodes, dfsNode, time);
            }

            // We've finished visiting this node, and is able to set the color to black
            // and set the finish time (v.FinisedTime)
            n.Color = DFSNode.Colors.Black;
            time++;
            n.FinishedTime = time;
            return time;
        }


        /// <summary>
        /// Wrapper which is used to perform Depth First Search on the graph
        /// </summary>
        private class DFSNode
        {
            public Node Node;
            public int DiscoverTime;
            public int FinishedTime;
            public Colors Color;

            public DFSNode(Node node)
            {
                Node = node;
            }

            public enum Colors
            {
                White, // Not visited
                Grey,  // Discovered
                Black  // Visited
            }
        }

        #endregion

        private class Edge
        {
            public Node In;
            public Node Out;

            public Edge(Node @in, Node @out)
            {
                In = @in;
                Out = @out;
            }
        }

        private class Node
        {
            public IDependent Module;

            public Node(IDependent module)
            {
                Module = module;
            }
        }
    }
}
